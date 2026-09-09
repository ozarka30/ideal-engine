using CompanyWars.Sim;

namespace CompanyWars.Content;

/// <summary>The referential, snapshot-structure and constructibility checks of CONTENT_SCHEMA.md §12.</summary>
public static class ContentValidator
{
    private static readonly Dictionary<string, string> PrefixByType = new(StringComparer.Ordinal)
    {
        ["employee"] = "emp.",
        ["room"] = "room.",
        ["furniture"] = "furn.",
        ["recipe"] = "recipe.",
        ["status"] = "status.",
        ["rider"] = "rider.",
        ["modifier"] = "mod.",
        ["floor"] = "floor.",
        ["founder"] = "founder.",
        ["rival"] = "rival.",
    };

    public static void Validate(ContentDb db, List<string> errors)
    {
        Referential(db, errors);
        foreach (ScriptedRival rival in db.ScriptedRivals)
        {
            SnapshotStructure(db, rival.Snapshot, rival.Id, errors);
            if (rival.Gimmick != null && Array.IndexOf(rival.Snapshot.Globals.Modifiers, rival.Gimmick) < 0)
            {
                errors.Add($"{rival.Id}: gimmick {rival.Gimmick} is not in the snapshot's modifiers");
            }
            if (!rival.ExemptFromBudget) Constructibility(db, rival.Snapshot, rival.Round, rival.Id, errors);
        }
    }

    // ---------------------------------------------------------------- referential

    private static void Referential(ContentDb db, List<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        void Id(string id, string type)
        {
            if (!ids.Add(id)) errors.Add($"duplicate id {id}");
            if (!id.StartsWith(PrefixByType[type], StringComparison.Ordinal)) errors.Add($"{id}: prefix does not match type {type}");
        }
        foreach (EmployeeDef e in db.Employees) Id(e.Id, "employee");
        foreach (RoomDef r in db.Rooms) Id(r.Id, "room");
        foreach (FurnitureDef f in db.Furniture) Id(f.Id, "furniture");
        foreach (Recipe r in db.Recipes) Id(r.Id, "recipe");
        foreach (StatusDef s in db.Statuses) Id(s.Id, "status");
        foreach (RiderDef r in db.Riders) Id(r.Id, "rider");
        foreach (ModifierDef m in db.Modifiers) Id(m.Id, "modifier");
        foreach (FloorDef f in db.Floors) Id(f.Id, "floor");
        foreach (FounderDef f in db.Founders) Id(f.Id, "founder");
        foreach (Template t in db.Templates.Templates) Id(t.Id, "rival");
        foreach (ScriptedRival r in db.ScriptedRivals) Id(r.Id, "rival");

        // First occurrence wins; duplicates were already reported above.
        var employees = FirstById(db.Employees, e => e.Id);
        var rooms = FirstById(db.Rooms, r => r.Id);
        var furniture = FirstById(db.Furniture, f => f.Id);
        var statuses = FirstById(db.Statuses, s => s.Id);
        var floors = FirstById(db.Floors, f => f.Id);
        var modifiers = FirstById(db.Modifiers, m => m.Id);
        var riders = FirstById(db.Riders, r => r.Id);
        var founders = FirstById(db.Founders, f => f.Id);
        var recipes = FirstById(db.Recipes, r => r.Id);
        var rivals = FirstById(db.ScriptedRivals, r => r.Id);

        void Effects(string owner, Effect[] effects)
        {
            foreach (Effect e in effects)
            {
                if (e.Status != null && !statuses.ContainsKey(e.Status)) errors.Add($"{owner}: unknown status {e.Status}");
                if (e.Then != null && !statuses.ContainsKey(e.Then.Status)) errors.Add($"{owner}: unknown status {e.Then.Status}");
                if (e.Stat == "floorOutput" && e.Floor != "*" && (e.Floor == null || !floors.ContainsKey(e.Floor))) errors.Add($"{owner}: unknown floor {e.Floor}");
                if (e.Override == "floorSelector" && (e.To?.Name == null || Array.IndexOf(Vocabulary.FloorSelectors, e.To.Name) < 0)) errors.Add($"{owner}: floorSelector override to unknown selector");
                if (e.Override == "tenureTier" && e.To?.Tier == null) errors.Add($"{owner}: tenureTier override needs a tier");
            }
        }

        foreach (EmployeeDef e in db.Employees)
        {
            int abilities = e.Effects.Count(x => x.On == "ability");
            if (abilities != 1) errors.Add($"{e.Id}: has {abilities} ability effects, needs exactly one");
            Effects(e.Id, e.Effects);
            if (e.Placement != null)
            {
                foreach (string f in e.Placement.Floors)
                {
                    if (!floors.ContainsKey(f)) errors.Add($"{e.Id}: placement names unknown floor {f}");
                }
            }
            if (!e.InShop && !db.Recipes.Any(r => r.Result.Kind == "employee" && r.Result.DefId == e.Id))
            {
                errors.Add($"{e.Id}: inShop is false but no recipe results in it");
            }
        }
        foreach (RoomDef r in db.Rooms)
        {
            Effects(r.Id, r.Effects);
            foreach (string f in r.Floors)
            {
                if (!floors.ContainsKey(f)) errors.Add($"{r.Id}: unknown floor {f}");
            }
            if (r.Kind != "reception")
            {
                string tiles = (r.Footprint.W * r.Footprint.H).ToString();
                if (!db.Economy.RoomCostByTiles.TryGetValue(tiles, out long cost) || cost != r.Cost)
                {
                    errors.Add($"{r.Id}: cost {r.Cost} does not equal economy.roomCostByTiles[{tiles}]");
                }
            }
        }
        foreach (FurnitureDef f in db.Furniture)
        {
            Effects(f.Id, f.Effects);
            if (f.Floors != null)
            {
                foreach (string fl in f.Floors)
                {
                    if (!floors.ContainsKey(fl)) errors.Add($"{f.Id}: unknown floor {fl}");
                }
            }
        }
        foreach (RiderDef r in db.Riders) Effects(r.Id, r.Effects);
        foreach (ModifierDef m in db.Modifiers) Effects(m.Id, m.Effects);
        foreach (FounderDef f in db.Founders) Effects(f.Id, f.Effects);

        foreach (Recipe r in db.Recipes)
        {
            foreach (RecipeInput i in r.Inputs)
            {
                if (i.Match.DefId != null && !employees.ContainsKey(i.Match.DefId) && !furniture.ContainsKey(i.Match.DefId))
                {
                    errors.Add($"{r.Id}: input names unknown definition {i.Match.DefId}");
                }
            }
            if (r.Context != null && !rooms.ContainsKey(r.Context.Room)) errors.Add($"{r.Id}: unknown context room {r.Context.Room}");
            if ((r.Result.Kind == "roomTier" || r.Result.Kind == "roomTenure") && r.Context == null) errors.Add($"{r.Id}: a {r.Result.Kind} result needs a context");
            if (r.Result.DefId != null && !employees.ContainsKey(r.Result.DefId) && !furniture.ContainsKey(r.Result.DefId))
            {
                errors.Add($"{r.Id}: result names unknown definition {r.Result.DefId}");
            }
        }

        foreach (Template t in db.Templates.Templates)
        {
            foreach (string g in t.GimmickPool)
            {
                if (!modifiers.TryGetValue(g, out ModifierDef? m)) errors.Add($"{t.Id}: unknown gimmick {g}");
                else if (!m.RivalOnly) errors.Add($"{t.Id}: gimmickPool contains {g}, which is not rivalOnly");
            }
            foreach (string f in t.FounderPool)
            {
                if (!founders.ContainsKey(f)) errors.Add($"{t.Id}: unknown founder {f}");
            }
            foreach (ShopListEntry e in t.Rooms)
            {
                if (!rooms.ContainsKey(e.DefId)) errors.Add($"{t.Id}: unknown room {e.DefId}");
            }
            foreach (ShopListEntry e in t.Staff)
            {
                if (!employees.ContainsKey(e.DefId)) errors.Add($"{t.Id}: unknown employee {e.DefId}");
            }
            foreach (string f in t.Layout.FloorPreference)
            {
                if (!floors.ContainsKey(f)) errors.Add($"{t.Id}: unknown floor {f}");
            }
        }
        foreach (KeyValuePair<string, string[]> kv in db.Templates.LeaseSchedule.ByRound)
        {
            foreach (string f in kv.Value)
            {
                if (!floors.ContainsKey(f)) errors.Add($"leaseSchedule: unknown floor {f}");
            }
        }

        foreach (MapAct act in db.Map.Acts)
        {
            int fights = act.Columns.Count(c => c == 'F' || c == 'B');
            long span = act.Rounds[1] - act.Rounds[0] + 1;
            if (fights != span) errors.Add($"map act {act.Act}: F+B count {fights} does not equal its round span {span}");
            if (!rivals.ContainsKey(act.Boss)) errors.Add($"map act {act.Act}: unknown boss {act.Boss}");
            if (act.ScriptedFightsFirstRun != null)
            {
                foreach (string r in act.ScriptedFightsFirstRun)
                {
                    if (!rivals.ContainsKey(r)) errors.Add($"map act {act.Act}: unknown scripted fight {r}");
                }
            }
            if (act.ConsultantAlwaysOffers != null && !recipes.ContainsKey(act.ConsultantAlwaysOffers)) errors.Add($"map act {act.Act}: unknown recipe {act.ConsultantAlwaysOffers}");
        }

        for (int i = 0; i < db.Economy.Income.Table.Length; i++)
        {
            long round = i + 1;
            long expected = db.Economy.Income.Constant + (round + 1) / 2 * db.Economy.Income.PerTwoRounds;
            if (db.Economy.Income.Table[i] != expected) errors.Add($"economy.income.table[{i}] is {db.Economy.Income.Table[i]}, formula gives {expected}");
        }
        foreach (string e in db.Economy.StartingRoster)
        {
            if (!employees.ContainsKey(e)) errors.Add($"economy: unknown starting roster employee {e}");
        }
        if (!floors.ContainsKey(db.Economy.StartingRosterFloor)) errors.Add("economy: unknown startingRosterFloor");

        foreach (Mode m in db.Modes)
        {
            if (m.Map != null && m.Map != db.Map.Id) errors.Add($"{m.Id}: unknown map {m.Map}");
        }
        foreach (BossCounter bc in db.Balance.BossCounters)
        {
            if (!rivals.ContainsKey(bc.Boss)) errors.Add($"balance: unknown boss {bc.Boss}");
        }
        foreach (ScriptedRival r in db.ScriptedRivals)
        {
            if (r.Gimmick != null && !modifiers.ContainsKey(r.Gimmick)) errors.Add($"{r.Id}: unknown gimmick {r.Gimmick}");
            if (r.Snapshot.ContentVersion != db.ContentVersion) errors.Add($"{r.Id}: snapshot contentVersion {r.Snapshot.ContentVersion} is not {db.ContentVersion}");
            if (r.Snapshot.Round != r.Round) errors.Add($"{r.Id}: snapshot round {r.Snapshot.Round} is not the rival's round {r.Round}");
            foreach (string mod in r.Snapshot.Globals.Modifiers)
            {
                if (!modifiers.ContainsKey(mod)) errors.Add($"{r.Id}: unknown modifier {mod}");
            }
            foreach (RiderRef rr in r.Snapshot.Globals.Riders)
            {
                if (!riders.ContainsKey(rr.RiderId)) errors.Add($"{r.Id}: unknown rider {rr.RiderId}");
            }
            if (!founders.ContainsKey(r.Snapshot.Globals.FounderId)) errors.Add($"{r.Id}: unknown founder {r.Snapshot.Globals.FounderId}");
        }
    }

    private static Dictionary<string, T> FirstById<T>(IEnumerable<T> items, Func<T, string> id)
    {
        var d = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (T item in items) d.TryAdd(id(item), item);
        return d;
    }

    // ---------------------------------------------------------------- snapshot structure (SIMULATION_SPEC §5.1 plus placement rules)

    public static void SnapshotStructure(ContentDb db, TowerSnapshot snap, string label, List<string> errors)
    {
        var employees = FirstById(db.Employees, e => e.Id);
        var rooms = FirstById(db.Rooms, r => r.Id);
        var furniture = FirstById(db.Furniture, f => f.Id);
        var seenFloors = new HashSet<long>();
        var instances = new HashSet<string>(StringComparer.Ordinal);
        var ridden = new HashSet<string>(snap.Globals.Riders.Select(r => r.InstanceId), StringComparer.Ordinal);

        foreach (SnapshotFloor sf in snap.Floors)
        {
            if (!seenFloors.Add(sf.Index)) { errors.Add($"{label}: floor index {sf.Index} repeats"); continue; }
            FloorDef? floorDef = db.Floors.FirstOrDefault(f => f.Index == sf.Index);
            if (floorDef == null) { errors.Add($"{label}: floor index {sf.Index} is not in floors.json"); continue; }
            if (sf.Grid.W != floorDef.Grid.W || sf.Grid.H != floorDef.Grid.H) errors.Add($"{label}: floor {sf.Index} grid {sf.Grid.W}x{sf.Grid.H} does not match floors.json");

            var placedRooms = new List<(SnapshotRoom Room, RoomDef Def)>();
            foreach (SnapshotRoom sr in sf.Rooms)
            {
                if (!rooms.TryGetValue(sr.DefId, out RoomDef? rdef)) { errors.Add($"{label}: unknown room {sr.DefId}"); continue; }
                if (sr.Rect.Length != 4) { errors.Add($"{label}: room {sr.RoomId} rect must have four values"); continue; }
                long c = sr.Rect[0], r = sr.Rect[1], w = sr.Rect[2], h = sr.Rect[3];
                if (Array.IndexOf(rdef.Floors, floorDef.Id) < 0) errors.Add($"{label}: room {sr.RoomId} ({sr.DefId}) is not legal on {floorDef.Id}");
                if (Array.IndexOf(floorDef.RoomKinds, rdef.Kind) < 0) errors.Add($"{label}: room kind {rdef.Kind} is not legal on {floorDef.Id}");
                if (w != rdef.Footprint.W || h != rdef.Footprint.H) errors.Add($"{label}: room {sr.RoomId} is {w}x{h}, definition says {rdef.Footprint.W}x{rdef.Footprint.H}");
                if (c < 0 || r < 0 || c + w > sf.Grid.W || r + h > sf.Grid.H) errors.Add($"{label}: room {sr.RoomId} leaves its grid");
                if (!rdef.LandingLegal && c == 0) errors.Add($"{label}: room {sr.RoomId} includes the landing column, which {sr.DefId} forbids");
                foreach ((SnapshotRoom o, RoomDef _) in placedRooms)
                {
                    if (c < o.Rect[0] + o.Rect[2] && o.Rect[0] < c + w && r < o.Rect[1] + o.Rect[3] && o.Rect[1] < r + h)
                    {
                        errors.Add($"{label}: rooms {o.RoomId} and {sr.RoomId} overlap");
                    }
                }
                placedRooms.Add((sr, rdef));
            }

            var tiles = new HashSet<(long, long)>();
            var occupantsByRoom = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (SnapshotOccupant so in sf.Occupants)
            {
                if (!instances.Add(so.InstanceId)) errors.Add($"{label}: instanceId {so.InstanceId} repeats");
                if (so.Tile.Length != 2) { errors.Add($"{label}: occupant {so.InstanceId} tile must have two values"); continue; }
                long col = so.Tile[0], row = so.Tile[1];
                long fw = 1, fh = 1;
                if (so.Kind == "employee")
                {
                    if (!employees.TryGetValue(so.DefId, out EmployeeDef? edef)) { errors.Add($"{label}: unknown employee {so.DefId}"); continue; }
                    if (edef.Placement != null && Array.IndexOf(edef.Placement.Floors, floorDef.Id) < 0) errors.Add($"{label}: {so.InstanceId} ({so.DefId}) may not stand on {floorDef.Id}");
                    if (sf.Index == -1 && !edef.Extraplanar) errors.Add($"{label}: {so.InstanceId} is not extraplanar and stands on B1");
                    if (edef.Extraplanar && edef.InShop && !ridden.Contains(so.InstanceId)) errors.Add($"{label}: shop-bought extraplanar {so.InstanceId} has no rider");
                    SnapshotRoom? inRoom = placedRooms.Select(p => p.Room).FirstOrDefault(p => col >= p.Rect[0] && col < p.Rect[0] + p.Rect[2] && row >= p.Rect[1] && row < p.Rect[1] + p.Rect[3]);
                    if (inRoom != null)
                    {
                        occupantsByRoom.TryGetValue(inRoom.RoomId, out long n);
                        occupantsByRoom[inRoom.RoomId] = n + 1;
                    }
                }
                else if (so.Kind == "furniture")
                {
                    if (!furniture.TryGetValue(so.DefId, out FurnitureDef? fdef)) { errors.Add($"{label}: unknown furniture {so.DefId}"); continue; }
                    fw = fdef.Footprint.W;
                    fh = fdef.Footprint.H;
                    if (fdef.Floors != null && Array.IndexOf(fdef.Floors, floorDef.Id) < 0) errors.Add($"{label}: furniture {so.InstanceId} ({so.DefId}) is not legal on {floorDef.Id}");
                    bool inside = placedRooms.Any(p => col >= p.Room.Rect[0] && col + fw <= p.Room.Rect[0] + p.Room.Rect[2] && row >= p.Room.Rect[1] && row + fh <= p.Room.Rect[1] + p.Room.Rect[3]);
                    if (!inside) errors.Add($"{label}: furniture {so.InstanceId} does not lie wholly inside one room");
                }
                else
                {
                    errors.Add($"{label}: occupant {so.InstanceId} has unknown kind {so.Kind}");
                    continue;
                }
                for (long dy = 0; dy < fh; dy++)
                {
                    for (long dx = 0; dx < fw; dx++)
                    {
                        if (col + dx >= sf.Grid.W || row + dy >= sf.Grid.H) errors.Add($"{label}: occupant {so.InstanceId} leaves its grid");
                        if (!tiles.Add((col + dx, row + dy))) errors.Add($"{label}: two occupants share tile ({col + dx},{row + dy}) on floor {sf.Index}");
                    }
                }
            }
            foreach ((SnapshotRoom sr, RoomDef rdef) in placedRooms)
            {
                if (rdef.MaxOccupants.HasValue && occupantsByRoom.TryGetValue(sr.RoomId, out long n) && n > rdef.MaxOccupants.Value)
                {
                    errors.Add($"{label}: room {sr.RoomId} holds {n} employees, maximum {rdef.MaxOccupants.Value}");
                }
            }
            if (sf.Index == -1 && sf.Occupants.Length > 0 && !snap.Globals.LeasedB1) errors.Add($"{label}: B1 has occupants but leasedB1 is false");
        }
        foreach (RiderRef rr in snap.Globals.Riders)
        {
            if (!instances.Contains(rr.InstanceId)) errors.Add($"{label}: rider {rr.RiderId} names unknown instance {rr.InstanceId}");
        }
    }

    // ---------------------------------------------------------------- constructibility (D-20)

    /// <summary>Starting budget plus the income of every round up to and including <paramref name="round"/>.</summary>
    public static long CumulativeIncome(ContentDb db, long round)
    {
        long total = db.Economy.StartingBudget;
        for (int i = 0; i < round && i < db.Economy.Income.Table.Length; i++) total += db.Economy.Income.Table[i];
        return total;
    }

    /// <summary>The total cost of a snapshot's rooms, staff, furniture and leases.</summary>
    public static long SnapshotCost(ContentDb db, TowerSnapshot snap)
    {
        var employees = FirstById(db.Employees, e => e.Id);
        var rooms = FirstById(db.Rooms, r => r.Id);
        var furniture = FirstById(db.Furniture, f => f.Id);
        long cost = 0;
        foreach (SnapshotFloor sf in snap.Floors)
        {
            FloorDef? floorDef = db.Floors.FirstOrDefault(f => f.Index == sf.Index);
            if (floorDef != null && !floorDef.Starting && (sf.Rooms.Length > 0 || sf.Occupants.Length > 0 || (sf.Index == -1 && snap.Globals.LeasedB1))) cost += floorDef.Lease;
            foreach (SnapshotRoom sr in sf.Rooms)
            {
                if (rooms.TryGetValue(sr.DefId, out RoomDef? rdef)) cost += rdef.Cost;
            }
            foreach (SnapshotOccupant so in sf.Occupants)
            {
                if (so.Kind == "employee" && employees.TryGetValue(so.DefId, out EmployeeDef? edef))
                {
                    string key = edef.Extraplanar ? "extraplanar" : edef.Tier.ToString();
                    if (edef.Cost > 0) cost += edef.Cost;
                    else if (db.Economy.EmployeeCost.TryGetValue(key, out long c)) cost += c;
                }
                else if (so.Kind == "furniture" && furniture.TryGetValue(so.DefId, out FurnitureDef? fdef))
                {
                    cost += fdef.Cost;
                }
            }
        }
        return cost;
    }

    public static void Constructibility(ContentDb db, TowerSnapshot snap, long round, string label, List<string> errors)
    {
        long cost = SnapshotCost(db, snap);
        long budget = CumulativeIncome(db, round);
        if (cost > budget) errors.Add($"{label}: costs {cost} but cumulative income to round {round} is {budget}");
    }
}
