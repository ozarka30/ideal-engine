using System;
using System.Collections.Generic;
using System.Text;

namespace CompanyWars.Sim;

internal sealed partial class Match
{
    private readonly RuleSet _rules;
    private readonly ContentTable _content;
    private readonly Firm[] _firms = new Firm[2];
    private readonly List<Unit> _units = new();
    private readonly List<FurnitureState> _furniture = new();
    private readonly Mulberry32 _rng;
    private readonly List<LedgerEntry> _entries = new();
    private string _winner = "draw";
    private long _endTick;

    public Match(uint seed, TowerSnapshot a, TowerSnapshot b, RuleSet rules, ContentTable content)
    {
        _rules = rules;
        _content = content;
        _rng = new Mulberry32(seed);

        if (a.ContentVersion != rules.ContentVersion || b.ContentVersion != rules.ContentVersion || content.ContentVersion != rules.ContentVersion)
        {
            throw new SimulationInputException("contentVersion mismatch between rules, content and snapshots");
        }

        _firms[0] = BuildFirm(Side.A, a);
        _firms[1] = BuildFirm(Side.B, b);
        OrderUnits();
        foreach (Firm firm in _firms) ComputeAdjacency(firm);
        foreach (Firm firm in _firms) ApplyStaticEffects(firm);
        foreach (Firm firm in _firms) DeriveFirm(firm);
    }

    private Firm A => _firms[0];

    private Firm B => _firms[1];

    private Firm Opponent(Firm f) => _firms[1 - (int)f.Side];

    // ---------------------------------------------------------------- §5.1 validation and tower construction

    private Firm BuildFirm(Side side, TowerSnapshot snap)
    {
        var firm = new Firm { Side = side, Snapshot = snap };
        var seenInstances = new HashSet<string>(StringComparer.Ordinal);
        var seenRoomIds = new HashSet<string>(StringComparer.Ordinal);
        var units = new Dictionary<string, Unit>(StringComparer.Ordinal);

        foreach (SnapshotFloor sf in snap.Floors)
        {
            int index = checked((int)sf.Index);
            if (index < -1 || index > 3) throw new SimulationInputException($"{side}: floor index {index} out of range");
            if (firm.Floors.ContainsKey(index)) throw new SimulationInputException($"{side}: floor index {index} appears twice");
            var floor = new FloorState { Index = index, W = checked((int)sf.Grid.W), H = checked((int)sf.Grid.H) };
            firm.Floors[index] = floor;

            foreach (SnapshotRoom sr in sf.Rooms)
            {
                if (!_content.Rooms.TryGetValue(sr.DefId, out RoomDef? rdef)) throw new SimulationInputException($"{side}: unknown room definition {sr.DefId}");
                if (sr.Rect.Length < 4) throw new SimulationInputException($"{side}: room {sr.RoomId} rect malformed");
                if (!seenRoomIds.Add(sr.RoomId)) throw new SimulationInputException($"{side}: roomId {sr.RoomId} appears twice");
                var room = new RoomState
                {
                    Side = side,
                    FloorIndex = index,
                    RoomId = sr.RoomId,
                    Def = rdef,
                    Col = checked((int)sr.Rect[0]),
                    Row = checked((int)sr.Rect[1]),
                    W = checked((int)sr.Rect[2]),
                    H = checked((int)sr.Rect[3]),
                    TenureRounds = sr.TenureRounds,
                };
                if (room.W < 1 || room.H < 1 || room.Col < 0 || room.Row < 0 || room.Col + room.W > floor.W || room.Row + room.H > floor.H)
                {
                    throw new SimulationInputException($"{side}: room {sr.RoomId} leaves its grid");
                }
                foreach (RoomState other in floor.Rooms)
                {
                    if (other.Overlaps(room)) throw new SimulationInputException($"{side}: rooms {other.RoomId} and {sr.RoomId} overlap");
                }
                room.Tier = TierFor(sr.TenureRounds);
                floor.Rooms.Add(room);
            }

            var occupiedTiles = new HashSet<(int, int)>();
            foreach (SnapshotOccupant so in sf.Occupants)
            {
                if (so.Tile.Length < 2) throw new SimulationInputException($"{side}: occupant {so.InstanceId} tile malformed");
                int col = checked((int)so.Tile[0]);
                int row = checked((int)so.Tile[1]);
                if (!seenInstances.Add(so.InstanceId)) throw new SimulationInputException($"{side}: instanceId {so.InstanceId} appears twice");
                if (so.Kind == "employee")
                {
                    if (!_content.Employees.TryGetValue(so.DefId, out EmployeeDef? edef)) throw new SimulationInputException($"{side}: unknown employee definition {so.DefId}");
                    if (col < 0 || row < 0 || col >= floor.W || row >= floor.H) throw new SimulationInputException($"{side}: occupant {so.InstanceId} outside its grid");
                    if (!occupiedTiles.Add((col, row))) throw new SimulationInputException($"{side}: two occupants share tile ({col},{row}) on floor {index}");
                    Effect? ability = null;
                    foreach (Effect e in edef.Effects)
                    {
                        if (e.On == "ability") { ability = e; break; }
                    }
                    if (ability == null) throw new SimulationInputException($"{side}: employee {so.DefId} has no ability effect");
                    var unit = new Unit
                    {
                        UnitIndex = -1,
                        IndexWithinSide = -1,
                        Side = side,
                        FloorIndex = index,
                        Col = col,
                        Row = row,
                        Def = edef,
                        InstanceId = so.InstanceId,
                        Ability = ability,
                        AbilityId = AbilityIdFor(edef, ability),
                    };
                    floor.Units.Add(unit);
                    units[so.InstanceId] = unit;
                }
                else if (so.Kind == "furniture")
                {
                    if (!_content.Furniture.TryGetValue(so.DefId, out FurnitureDef? fdef)) throw new SimulationInputException($"{side}: unknown furniture definition {so.DefId}");
                    var tiles = new List<(int, int)>();
                    for (int dy = 0; dy < fdef.Footprint.H; dy++)
                    {
                        for (int dx = 0; dx < fdef.Footprint.W; dx++)
                        {
                            int c = col + dx;
                            int r = row + dy;
                            if (c < 0 || r < 0 || c >= floor.W || r >= floor.H) throw new SimulationInputException($"{side}: furniture {so.InstanceId} outside its grid");
                            if (!occupiedTiles.Add((c, r))) throw new SimulationInputException($"{side}: two occupants share tile ({c},{r}) on floor {index}");
                            tiles.Add((c, r));
                        }
                    }
                    var furn = new FurnitureState { FurnIndex = -1, Side = side, FloorIndex = index, Def = fdef, InstanceId = so.InstanceId, Tiles = tiles };
                    floor.Furniture.Add(furn);
                }
                else
                {
                    throw new SimulationInputException($"{side}: occupant kind {so.Kind} unknown");
                }
            }
        }

        // Room membership; furniture must be wholly inside one room.
        foreach (FloorState floor in firm.Floors.Values)
        {
            foreach (Unit u in floor.Units)
            {
                foreach (RoomState room in floor.Rooms)
                {
                    if (room.Contains(u.Col, u.Row)) { u.Room = room; room.Occupants.Add(u); break; }
                }
            }
            foreach (FurnitureState f in floor.Furniture)
            {
                RoomState? inside = null;
                foreach (RoomState room in floor.Rooms)
                {
                    bool all = true;
                    foreach ((int c, int r) in f.Tiles)
                    {
                        if (!room.Contains(c, r)) { all = false; break; }
                    }
                    if (all) { inside = room; break; }
                }
                if (inside == null) throw new SimulationInputException($"{side}: furniture {f.InstanceId} is not inside a room");
                f.Room = inside;
                inside.Furniture.Add(f);
            }
        }

        if (firm.Floors.TryGetValue(-1, out FloorState? b1) && (b1.Units.Count > 0 || b1.Furniture.Count > 0) && !snap.Globals.LeasedB1)
        {
            throw new SimulationInputException($"{side}: B1 has occupants but leasedB1 is false");
        }

        if (!_content.Founders.TryGetValue(snap.Globals.FounderId, out FounderDef? founder)) throw new SimulationInputException($"{side}: unknown founder {snap.Globals.FounderId}");
        firm.Founder = founder;
        foreach (string modId in snap.Globals.Modifiers)
        {
            if (!_content.Modifiers.TryGetValue(modId, out ModifierDef? mdef)) throw new SimulationInputException($"{side}: unknown modifier {modId}");
            firm.Modifiers.Add(mdef);
        }
        var ridden = new HashSet<string>(StringComparer.Ordinal);
        foreach (RiderRef rr in snap.Globals.Riders)
        {
            if (!_content.Riders.TryGetValue(rr.RiderId, out RiderDef? rdef)) throw new SimulationInputException($"{side}: unknown rider {rr.RiderId}");
            if (!units.TryGetValue(rr.InstanceId, out Unit? ru)) throw new SimulationInputException($"{side}: rider {rr.RiderId} names unknown instance {rr.InstanceId}");
            firm.Riders.Add((ru, rdef));
            ridden.Add(rr.InstanceId);
        }
        foreach (Unit u in units.Values)
        {
            // §5.1: an extraplanar employee has no rider. Ritual results (inShop false) are hired without
            // a rider (CONTENT_SCHEMA.md §12 "every shop-bought extraplanar unit has a rider"), so only
            // shop-bought extraplanar units are required to carry one.
            if (u.Def.Extraplanar && u.Def.InShop && !ridden.Contains(u.InstanceId))
            {
                throw new SimulationInputException($"{side}: extraplanar employee {u.InstanceId} has no rider");
            }
        }
        return firm;
    }

    private long TierFor(long tenureRounds)
    {
        long tier = 0;
        foreach (long t in _rules.TenureTierRounds)
        {
            if (t <= tenureRounds) tier++;
        }
        return tier;
    }

    /// <summary><c>ability.&lt;slug of the ability name&gt;</c>; the definition's local name when the ability is unnamed.</summary>
    internal static string AbilityIdFor(EmployeeDef def, Effect ability)
    {
        string name = ability.Name ?? def.Id.Substring(def.Id.IndexOf('.') + 1);
        var sb = new StringBuilder("ability.");
        bool sep = false;
        foreach (char ch in name)
        {
            char c = ch;
            if (c >= 'A' && c <= 'Z') c = (char)(c + 32);
            bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
            if (ok)
            {
                if (sep && sb.Length > 8) sb.Append('_');
                sb.Append(c);
                sep = false;
            }
            else
            {
                sep = true;
            }
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------- §5.2 canonical ordering

    private void OrderUnits()
    {
        int unitIndex = 0;
        int furnIndex = 0;
        foreach (Firm firm in _firms)
        {
            int within = 0;
            foreach (FloorState floor in firm.Floors.Values) // SortedDictionary: floor index ascending
            {
                floor.Units.Sort((x, y) => x.Row != y.Row ? x.Row.CompareTo(y.Row) : x.Col.CompareTo(y.Col));
                var ordered = new List<Unit>(floor.Units.Count);
                foreach (Unit u in floor.Units)
                {
                    var nu = new Unit
                    {
                        UnitIndex = unitIndex++,
                        IndexWithinSide = within++,
                        Side = u.Side,
                        FloorIndex = u.FloorIndex,
                        Col = u.Col,
                        Row = u.Row,
                        Def = u.Def,
                        InstanceId = u.InstanceId,
                        Ability = u.Ability,
                        AbilityId = u.AbilityId,
                        Room = u.Room,
                    };
                    ordered.Add(nu);
                }
                floor.Units.Clear();
                floor.Units.AddRange(ordered);
                foreach (RoomState room in floor.Rooms)
                {
                    room.Occupants.Clear();
                    foreach (Unit u in ordered)
                    {
                        if (u.Room == room) room.Occupants.Add(u);
                    }
                }
                firm.Units.AddRange(ordered);
                _units.AddRange(ordered);

                floor.Furniture.Sort((x, y) => x.Tiles[0].Row != y.Tiles[0].Row ? x.Tiles[0].Row.CompareTo(y.Tiles[0].Row) : x.Tiles[0].Col.CompareTo(y.Tiles[0].Col));
                var orderedF = new List<FurnitureState>(floor.Furniture.Count);
                foreach (FurnitureState f in floor.Furniture)
                {
                    var nf = new FurnitureState { FurnIndex = furnIndex++, Side = f.Side, FloorIndex = f.FloorIndex, Def = f.Def, InstanceId = f.InstanceId, Tiles = f.Tiles, Room = f.Room };
                    orderedF.Add(nf);
                }
                floor.Furniture.Clear();
                floor.Furniture.AddRange(orderedF);
                foreach (RoomState room in floor.Rooms)
                {
                    room.Furniture.Clear();
                    foreach (FurnitureState f in orderedF)
                    {
                        if (f.Room == room) room.Furniture.Add(f);
                    }
                }
                firm.Furniture.AddRange(orderedF);
                _furniture.AddRange(orderedF);

                // Room order for periodic and banner iteration: floor ascending, roomId ascending (§11.3).
                floor.Rooms.Sort((x, y) => string.CompareOrdinal(x.RoomId, y.RoomId));
                firm.Rooms.AddRange(floor.Rooms);
            }
            // Riders were bound to the pre-ordering Unit objects; rebind by instance id.
            for (int i = 0; i < firm.Riders.Count; i++)
            {
                (Unit old, RiderDef def) = firm.Riders[i];
                foreach (Unit u in firm.Units)
                {
                    if (u.InstanceId == old.InstanceId) { firm.Riders[i] = (u, def); break; }
                }
            }
        }
    }

    // ---------------------------------------------------------------- §5.3 adjacency

    internal static bool TilesAdjacent(int fa, int ca, int ra, int fb, int cb, int rb)
    {
        if (fa == fb)
        {
            int dc = Math.Abs(ca - cb);
            int dr = Math.Abs(ra - rb);
            return (dc == 1 && dr == 0) || (dc == 0 && dr == 1);
        }
        return ca == 0 && cb == 0 && Math.Abs(fa - fb) == 1;
    }

    private static void ComputeAdjacency(Firm firm)
    {
        foreach (Unit u in firm.Units)
        {
            foreach (Unit v in firm.Units)
            {
                if (u != v && TilesAdjacent(u.FloorIndex, u.Col, u.Row, v.FloorIndex, v.Col, v.Row)) u.Adjacent.Add(v);
            }
        }
        foreach (FurnitureState f in firm.Furniture)
        {
            foreach (Unit v in firm.Units)
            {
                foreach ((int c, int r) in f.Tiles)
                {
                    if (TilesAdjacent(f.FloorIndex, c, r, v.FloorIndex, v.Col, v.Row)) { f.Adjacent.Add(v); break; }
                }
            }
        }
    }

    // ---------------------------------------------------------------- §5.4–§5.5, §6.4 static effects

    private enum SourceKind
    {
        Room,
        Furniture,
        Employee,
        Rider,
        Founder,
        Modifier,
    }

    private readonly record struct StaticSource(SourceKind Kind, Unit? Unit, RoomState? Room, FurnitureState? Furniture, Effect[] Effects);

    private List<StaticSource> StaticSources(Firm firm)
    {
        // Order: rooms, furniture, employees, riders, founder, modifiers. Chained permille multipliers
        // are applied in this order; sums and flags are order-free.
        var list = new List<StaticSource>();
        foreach (RoomState r in firm.Rooms) list.Add(new StaticSource(SourceKind.Room, null, r, null, r.Def.Effects));
        foreach (FurnitureState f in firm.Furniture) list.Add(new StaticSource(SourceKind.Furniture, null, null, f, f.Def.Effects));
        foreach (Unit u in firm.Units) list.Add(new StaticSource(SourceKind.Employee, u, null, null, u.Def.Effects));
        foreach ((Unit u, RiderDef d) in firm.Riders) list.Add(new StaticSource(SourceKind.Rider, u, null, null, d.Effects));
        if (firm.Founder != null) list.Add(new StaticSource(SourceKind.Founder, null, null, null, firm.Founder.Effects));
        foreach (ModifierDef m in firm.Modifiers) list.Add(new StaticSource(SourceKind.Modifier, null, null, null, m.Effects));
        return list;
    }

    private static string DefaultSubjectScope(SourceKind kind) => kind switch
    {
        SourceKind.Room => "occupants",
        SourceKind.Furniture => "adjacent",
        SourceKind.Employee => "self",
        SourceKind.Rider => "self",
        _ => "firm",
    };

    /// <summary>The units an effect's subject names, in canonical order.</summary>
    private List<Unit> SubjectUnits(Firm firm, StaticSource src, SubjectSpec? subject)
    {
        string scope = subject?.Scope ?? DefaultSubjectScope(src.Kind);
        var result = new List<Unit>();
        IEnumerable<Unit> pool;
        switch (scope)
        {
            case "self":
                pool = src.Unit != null ? new[] { src.Unit } : Array.Empty<Unit>();
                break;
            case "adjacent":
                if (src.Unit != null) pool = src.Unit.Adjacent;
                else if (src.Furniture != null) pool = src.Furniture.Adjacent;
                else if (src.Room != null) pool = AdjacentToAny(firm, src.Room.Occupants);
                else pool = Array.Empty<Unit>();
                break;
            case "sameFloor":
                {
                    int? fl = src.Unit?.FloorIndex ?? src.Furniture?.FloorIndex ?? src.Room?.FloorIndex;
                    pool = fl.HasValue ? firm.UnitsOnFloor(fl.Value) : Array.Empty<Unit>();
                    break;
                }
            case "occupants":
                if (src.Room != null) pool = src.Room.Occupants;
                else if (src.Unit?.Room != null) pool = src.Unit.Room.Occupants;
                else if (src.Furniture?.Room != null) pool = src.Furniture.Room.Occupants;
                else pool = Array.Empty<Unit>();
                break;
            case "all":
                pool = firm.Units;
                break;
            default:
                pool = Array.Empty<Unit>();
                break;
        }
        foreach (Unit u in pool)
        {
            if (u.MatchesFilter(subject?.Dept, subject?.NotDept, subject?.Tag)) result.Add(u);
        }
        result.Sort((x, y) => x.UnitIndex.CompareTo(y.UnitIndex));
        return result;
    }

    private static List<Unit> AdjacentToAny(Firm firm, List<Unit> seeds)
    {
        var set = new HashSet<Unit>();
        foreach (Unit s in seeds)
        {
            foreach (Unit a in s.Adjacent) set.Add(a);
        }
        var list = new List<Unit>();
        foreach (Unit u in firm.Units)
        {
            if (set.Contains(u)) list.Add(u);
        }
        return list;
    }

    private void ApplyStaticEffects(Firm firm)
    {
        List<StaticSource> sources = StaticSources(firm);

        // Pass 1: overrides. Old Money's tenureTier must land before room stats are read.
        foreach (StaticSource src in sources)
        {
            foreach (Effect e in src.Effects)
            {
                if (e.On != "static" || e.Do != "override") continue;
                if (src.Room != null && !src.Room.EffectActive(e)) continue;
                string scope = e.Subject?.Scope ?? DefaultSubjectScope(src.Kind);
                if (e.Override == "tenureTier" && scope == "allRooms")
                {
                    foreach (RoomState r in firm.Rooms) r.Tier = e.To?.Tier ?? r.Tier;
                }
                else if (e.Override == "floorSelector")
                {
                    foreach (Unit u in SubjectUnits(firm, src, e.Subject)) u.FloorSelectorOverride = e.To?.Name;
                }
            }
        }

        // Pass 2: flags.
        foreach (StaticSource src in sources)
        {
            foreach (Effect e in src.Effects)
            {
                if (e.On != "static" || e.Do != "flag") continue;
                if (src.Room != null && !src.Room.EffectActive(e)) continue;
                string scope = e.Subject?.Scope ?? DefaultSubjectScope(src.Kind);
                if (scope == "firm")
                {
                    switch (e.Flag)
                    {
                        case "regenNeverSuppressed": firm.RegenNeverSuppressed = true; break;
                        case "receptionDisabled": firm.ReceptionDisabled = true; break;
                        case "everyFloorMostPopulated": firm.EveryFloorMostPopulated = true; break;
                        case "floorSelectorMirror": firm.FloorSelectorMirror = true; break;
                        default: break; // build-phase flags
                    }
                    continue;
                }
                foreach (Unit u in SubjectUnits(firm, src, e.Subject))
                {
                    switch (e.Flag)
                    {
                        case "untargetable": u.Untargetable = true; break;
                        case "bureaucracyImmune": u.BureaucracyImmune = true; break;
                        case "frozenImmune": u.FrozenImmune = true; break;
                        case "burnoutImmune": u.BurnoutImmune = true; break;
                        case "overtimePermanent": u.OvertimePermanent = true; break;
                        case "cannotBeRetriggered": u.CannotBeRetriggered = true; break;
                        case "wholeFloorAdjacency": u.WholeFloorAdjacency = true; break;
                        case "capProtected": u.CapProtected = true; break;
                        default: break; // build-phase flags
                    }
                }
            }
        }

        // Pass 3: stats.
        foreach (StaticSource src in sources)
        {
            foreach (Effect e in src.Effects)
            {
                if (e.On != "static" || e.Do != "stat") continue;
                if (src.Room != null && !src.Room.EffectActive(e)) continue;
                string scope = e.Subject?.Scope ?? DefaultSubjectScope(src.Kind);
                long amount = e.Amount ?? 0;
                long permille = e.Permille ?? 1000;
                if (scope == "firm")
                {
                    switch (e.Stat)
                    {
                        case "loyaltyCap": firm.CapFlat += amount; break;
                        case "loyaltyCapMult": firm.CapMult = Arith.Permille(firm.CapMult, permille); break;
                        case "regenPerEvent": firm.RegenFlat += amount; break;
                        case "floorOutput":
                            firm.FloorOutput.Add((e.Floor == "*" ? int.MinValue : _rules.FloorIndexOf(e.Floor ?? string.Empty), permille));
                            break;
                        default: break; // build-phase stats
                    }
                    continue;
                }
                if (src.Room != null && src.Room.Def.Kind == "reception" && firm.ReceptionDisabled && e.Stat == "loyaltyCap") continue;
                bool fromRoom = src.Kind == SourceKind.Room;
                bool fromEmployee = src.Kind == SourceKind.Employee;
                long tenureStep = fromRoom ? _rules.TenureStepPermille * src.Room!.Tier : 0;
                foreach (Unit u in SubjectUnits(firm, src, e.Subject))
                {
                    switch (e.Stat)
                    {
                        case "sales": u.Aura[Kind.Sales] = Arith.Permille(u.Aura[Kind.Sales], permille + tenureStep); break;
                        case "poach": u.Aura[Kind.Poach] = Arith.Permille(u.Aura[Kind.Poach], permille + tenureStep); break;
                        case "curse": u.Aura[Kind.Curse] = Arith.Permille(u.Aura[Kind.Curse], permille + tenureStep); break;
                        case "pr": u.Aura[Kind.Pr] = Arith.Permille(u.Aura[Kind.Pr], permille + tenureStep); break;
                        case "flatSales": u.FlatSales += amount; break;
                        case "cooldown": u.CdMult = Arith.Permille(u.CdMult, permille); break;
                        case "loyaltyCap":
                            if (fromEmployee) u.OwnCap += amount; else u.GrantedCap += amount;
                            break;
                        case "regenPerEvent":
                            if (fromEmployee) u.OwnRegen += amount; else u.GrantedRegen += amount;
                            break;
                        case "passiveMult": u.PassiveMult = Arith.Permille(u.PassiveMult, permille); break;
                        case "statusStacksBonus":
                            if (e.Status != null)
                            {
                                u.StacksBonus.TryGetValue(e.Status, out long cur);
                                u.StacksBonus[e.Status] = cur + amount;
                            }
                            break;
                        case "burnoutMaxOverride": u.BurnoutOverride = u.BurnoutOverride.HasValue ? Arith.Min(u.BurnoutOverride.Value, amount) : amount; break;
                        case "burnoutMaxDelta": u.BurnoutDelta += amount; break;
                        case "curseSelfCost": u.SelfCostDelta += e.Permille ?? amount; break;
                        case "retriggerBonus": u.RetriggerBonus = Arith.Permille(u.RetriggerBonus, permille); break;
                        case "floorOutput":
                            firm.FloorOutput.Add((e.Floor == "*" ? int.MinValue : _rules.FloorIndexOf(e.Floor ?? string.Empty), permille));
                            break;
                        default: break; // build-phase stats
                    }
                }
            }
        }

        // Derived per-unit values.
        foreach (Unit u in firm.Units)
        {
            for (int m = 0; m < 4; m++)
            {
                long ticks = u.Def.CooldownTicks;
                if (u.Def.CooldownTicksByMonth != null && u.Def.CooldownTicksByMonth.TryGetValue(m.ToString(), out long ov)) ticks = ov;
                u.CdBaseByMonth[m] = ticks * 1000;
            }
            if (u.Room == null)
            {
                for (int k = 0; k < Kind.Count; k++) u.Aura[k] = Arith.Permille(u.Aura[k], _rules.CorridorMult);
            }
            u.CdProgress = Arith.Permille(u.CdTotal(0), u.Def.InitialProgressPermille);
            u.BurnoutMax = u.BurnoutImmune ? 0 : Arith.Max(0, (u.BurnoutOverride ?? _rules.BurnoutMax) + u.BurnoutDelta);
            long fm = _rules.FloorMultFor(u.FloorIndex);
            foreach ((int fl, long p) in firm.FloorOutput)
            {
                if (fl == int.MinValue || fl == u.FloorIndex) fm = Arith.Permille(fm, p);
            }
            u.FloorMult = fm;
            u.SelfCostPermille = Arith.Max(0, _rules.CurseSelfCostPermille + u.SelfCostDelta);
            u.CapContribution = Arith.Permille(u.OwnCap, u.PassiveMult) + u.GrantedCap;
            u.RegenContribution = Arith.Permille(u.OwnRegen, u.PassiveMult) + u.GrantedRegen;
            if (u.CapProtected) firm.ProtectedCap += u.CapContribution;
        }

        // §9.4 extra effects: the definition's own afterFire effects, then the enclosing room's
        // Tier clauses whose subject matches, then adjacent furniture's, in furnIndex order.
        foreach (Unit u in firm.Units)
        {
            foreach (Effect e in u.Def.Effects)
            {
                if (e.On == "afterFire") u.Extras.Add(new ExtraEffect(e, u.AbilityId, e.EveryN ?? 1));
            }
        }
        foreach (StaticSource src in sources)
        {
            if (src.Kind != SourceKind.Room && src.Kind != SourceKind.Furniture) continue;
            foreach (Effect e in src.Effects)
            {
                if (e.On != "afterFire") continue;
                if (src.Room != null && !src.Room.EffectActive(e)) continue;
                string sourceId = src.Room?.Def.Id ?? src.Furniture!.Def.Id;
                foreach (Unit u in SubjectUnits(firm, src, e.Subject)) u.Extras.Add(new ExtraEffect(e, sourceId, e.EveryN ?? 1));
            }
        }
    }

    private void DeriveFirm(Firm firm)
    {
        long cap = _rules.LoyaltyBase(_rules.Round);
        int extraplanar = 0;
        foreach (Unit u in firm.Units)
        {
            cap += u.CapContribution;
            if (u.Def.Extraplanar) extraplanar++;
        }
        cap -= _rules.PortalEmployeeCapTax * extraplanar;
        if (firm.Snapshot.Globals.LeasedB1) cap -= _rules.B1LeaseCapTax;
        cap += firm.CapFlat;
        cap = Arith.Permille(cap, firm.CapMult);
        cap = Arith.Max(cap, 1);
        firm.Cap = cap;
        firm.CapAtStart = cap;

        long regen = Arith.Permille(cap, _rules.RegenBasePermille);
        foreach (Unit u in firm.Units) regen += u.RegenContribution;
        regen += firm.RegenFlat;
        firm.RegenPerEvent = regen;

        firm.Loyalty = cap;
        firm.SuppressThreshold = Arith.Permille(cap, _rules.SuppressThresholdPermille);
        firm.LastSuppressTick = -1000;
        firm.Revenue = 0;
        firm.TotalSales = 0;
    }
}
