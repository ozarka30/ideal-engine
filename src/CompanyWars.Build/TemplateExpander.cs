using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>
/// Expands a rival template into a snapshot, deterministically from (templateId, round, seed), by the nine steps of
/// CONTENT_SCHEMA.md §11.2 (D-39). Its outputs are content: the same code seeds the ranked ghost pool.
/// </summary>
public static class TemplateExpander
{
    public static Rival Expand(ContentDb db, string templateId, long round, uint seed, bool gimmick = false, long? budgetPermilleOverride = null)
    {
        Template t = db.Templates.Templates.First(x => x.Id == templateId);
        var rng = new Mulberry32(seed);                                                    // 1. seed
        string name = t.NamePool[(int)rng.Draw((uint)t.NamePool.Length)];
        string founder = t.FounderPool[(int)rng.Draw((uint)t.FounderPool.Length)];

        long budget = ContentValidator.CumulativeIncome(db, round) * (budgetPermilleOverride ?? t.BudgetPermille) / 1000;   // 2. budget

        var floors = new List<SnapshotFloor>();                                            // 3. lease
        var owned = new List<FloorDef>();
        foreach (FloorDef f in db.Floors.Where(f => f.Starting).OrderBy(f => f.Index)) owned.Add(f);
        string[] leased = Array.Empty<string>();
        long bestKey = -1;
        foreach (KeyValuePair<string, string[]> kv in db.Templates.LeaseSchedule.ByRound)
        {
            long key = long.Parse(kv.Key);
            if (key <= round && key > bestKey) { bestKey = key; leased = kv.Value; }
        }
        bool leasedB1 = false;
        foreach (string id in leased)
        {
            FloorDef f = db.Floors.First(x => x.Id == id);
            if (f.RequiresPortal) continue; // the portal is a campaign event; templates lease B1 only when a mode opens it (M4)
            budget -= f.Lease;
            owned.Add(f);
        }
        foreach (FloorDef f in owned.OrderBy(f => f.Index))
        {
            floors.Add(new SnapshotFloor(f.Index, new Footprint(f.Grid.W, f.Grid.H), BuildReducer.FixedRooms(f), Array.Empty<SnapshotOccupant>()));
        }
        var tower = new TowerSnapshot(1, db.ContentVersion, round, floors.ToArray(), new SnapshotGlobals(founder, Array.Empty<string>(), Array.Empty<RiderRef>(), leasedB1));

        // 4. rooms: draw weighted until rooms have consumed 40% of the remaining budget or nothing fits
        long roomBudget = budget * 40 / 100;
        long spentOnRooms = 0;
        var roomsPlaced = new List<string>();
        var roomPool = t.Rooms.Where(e => e.MinRound <= round).ToList();
        while (roomPool.Count > 0 && spentOnRooms < roomBudget)
        {
            ShopListEntry pick = Weighted(roomPool, rng);
            RoomDef def = db.Rooms.First(r => r.Id == pick.DefId);
            if (def.Cost > budget - spentOnRooms) { roomPool.Remove(pick); continue; }
            (long Floor, long Col, long Row)? spot = FirstRoomSpot(db, tower, def, t.Layout.FloorPreference);
            if (spot == null) { roomPool.Remove(pick); continue; }
            tower = PlaceRoom(tower, def, spot.Value.Floor, spot.Value.Col, spot.Value.Row, roomsPlaced.Count);
            roomsPlaced.Add(def.Id);
            spentOnRooms += def.Cost;
        }
        budget -= spentOnRooms;

        // 5. staff: draw weighted until budget is below the cheapest remaining entry
        var staff = new List<EmployeeDef>();
        var staffPool = t.Staff.Where(e => e.MinRound <= round).ToList();
        while (staffPool.Count > 0)
        {
            long cheapest = staffPool.Min(e => Economy.EmployeePrice(db, db.Employees.First(x => x.Id == e.DefId)));
            if (budget < cheapest) break;
            ShopListEntry pick = Weighted(staffPool, rng);
            EmployeeDef def = db.Employees.First(x => x.Id == pick.DefId);
            long price = Economy.EmployeePrice(db, def);
            if (price > budget) { staffPool.Remove(pick); continue; }
            staff.Add(def);
            budget -= price;
        }

        // 6–9. place, tenure, gimmick, validate; on failure drop the last purchase and place again
        for (int attempt = 0; attempt < 3; attempt++)
        {
            TowerSnapshot placed = Place(db, tower, staff, t.Layout.FillOrder, round);
            placed = Tenure(placed, round);
            if (gimmick && t.GimmickPool.Length > 0)
            {
                string g = t.GimmickPool[(int)rng.Draw((uint)t.GimmickPool.Length)];
                placed = placed with { Globals = placed.Globals with { Modifiers = new[] { g } } };
            }
            var errors = new List<string>();
            ContentValidator.SnapshotStructure(db, placed, templateId, errors);
            if (errors.Count == 0) return new Rival(name, t.Archetype, templateId, placed);
            if (staff.Count == 0) break;
            staff.RemoveAt(staff.Count - 1);
        }
        ScriptedRival fallback = db.ScriptedRivals.Where(r => !r.ExemptFromBudget).OrderBy(r => Math.Abs(r.Round - round)).ThenBy(r => r.Id, StringComparer.Ordinal).First();
        return new Rival(fallback.Name, fallback.Archetype, fallback.Id, fallback.Snapshot);
    }

    private static ShopListEntry Weighted(List<ShopListEntry> pool, Mulberry32 rng)
    {
        long total = 0;
        foreach (ShopListEntry e in pool) total += e.Weight;
        long roll = rng.Draw((uint)total);
        foreach (ShopListEntry e in pool)
        {
            if (roll < e.Weight) return e;
            roll -= e.Weight;
        }
        return pool[^1];
    }

    private static IEnumerable<SnapshotFloor> FloorsInPreference(ContentDb db, TowerSnapshot tower, string[] preference)
    {
        var seen = new HashSet<long>();
        foreach (string id in preference)
        {
            FloorDef f = db.Floors.First(x => x.Id == id);
            SnapshotFloor? sf = tower.Floors.FirstOrDefault(x => x.Index == f.Index);
            if (sf != null && seen.Add(sf.Index)) yield return sf;
        }
        foreach (SnapshotFloor sf in tower.Floors.OrderBy(x => x.Index))
        {
            if (seen.Add(sf.Index)) yield return sf;
        }
    }

    private static (long, long, long)? FirstRoomSpot(ContentDb db, TowerSnapshot tower, RoomDef def, string[] preference)
    {
        foreach (SnapshotFloor sf in FloorsInPreference(db, tower, preference))
        {
            for (long row = 0; row + def.Footprint.H <= sf.Grid.H; row++)
            {
                for (long col = 0; col + def.Footprint.W <= sf.Grid.W; col++)
                {
                    try
                    {
                        Legality.CheckRoomPlacement(db, tower, def, sf.Index, col, row, null);
                        return (sf.Index, col, row);
                    }
                    catch (BuildException)
                    {
                    }
                }
            }
        }
        return null;
    }

    private static TowerSnapshot PlaceRoom(TowerSnapshot tower, RoomDef def, long floorIndex, long col, long row, int index)
    {
        var floors = tower.Floors.Select(f =>
        {
            if (f.Index != floorIndex) return f;
            var room = new SnapshotRoom($"t{index}_{def.Id[(def.Id.IndexOf('.') + 1)..]}", def.Id, new[] { col, row, def.Footprint.W, def.Footprint.H }, 0);
            return f with { Rooms = f.Rooms.Append(room).ToArray() };
        }).ToArray();
        return tower with { Floors = floors };
    }

    /// <summary>Step 6: departments in fill order; a tile in a room that boosts the department, then any room tile, then the landing column, then the corridor. Same floor first, floors in preference order.</summary>
    private static TowerSnapshot Place(ContentDb db, TowerSnapshot tower, List<EmployeeDef> staff, string[] fillOrder, long round)
    {
        var occupants = tower.Floors.ToDictionary(f => f.Index, f => new List<SnapshotOccupant>(f.Occupants));
        var order = new List<EmployeeDef>();
        foreach (string dept in fillOrder) order.AddRange(staff.Where(s => s.Dept == dept));
        order.AddRange(staff.Where(s => !order.Contains(s)));

        bool reserveReception = staff.Any(s => s.Dept == "legal" || s.Dept == "sales");
        int n = 0;
        var placedIds = new List<string>();
        foreach (EmployeeDef def in order)
        {
            string id = $"r_{round:D2}_{n++:D3}";
            (long Floor, long Col, long Row)? spot = null;
            if (reserveReception && (def.Dept == "legal" || def.Dept == "sales"))
            {
                spot = SpotInRoomKind(db, tower, occupants, def, "reception");
                if (spot != null) reserveReception = false;
            }
            spot ??= SpotInBoostingRoom(db, tower, occupants, def);
            spot ??= SpotInAnyRoom(db, tower, occupants, def);
            spot ??= SpotOnLanding(db, tower, occupants, def);
            spot ??= SpotInCorridor(db, tower, occupants, def);
            if (spot == null) continue;
            occupants[spot.Value.Floor].Add(new SnapshotOccupant(new[] { spot.Value.Col, spot.Value.Row }, "employee", def.Id, id, Array.Empty<string>()));
            placedIds.Add(id);
        }
        var floors = tower.Floors.Select(f => f with { Occupants = occupants[f.Index].ToArray() }).ToArray();
        return tower with { Floors = floors };
    }

    private static bool Legal(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def, SnapshotFloor sf, long col, long row)
    {
        FloorDef fdef = db.FloorByIndex(sf.Index);
        if (def.Placement != null && Array.IndexOf(def.Placement.Floors, fdef.Id) < 0) return false;
        if (sf.Index == -1 && !def.Extraplanar) return false;
        if (occ[sf.Index].Any(o => o.Tile[0] == col && o.Tile[1] == row)) return false;
        SnapshotRoom? room = Legality.RoomAt(sf, col, row);
        if (room != null)
        {
            RoomDef rdef = db.Rooms.First(r => r.Id == room.DefId);
            if (rdef.MaxOccupants.HasValue && occ[sf.Index].Count(o => o.Kind == "employee" && Economy.Inside(o, room)) >= rdef.MaxOccupants.Value) return false;
        }
        return true;
    }

    private static IEnumerable<SnapshotFloor> Ordered(TowerSnapshot tower) => tower.Floors.OrderBy(f => f.Index < 0 ? 99 : f.Index == 1 ? 0 : f.Index);

    private static (long, long, long)? SpotInRoomKind(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def, string kind)
    {
        foreach (SnapshotFloor sf in Ordered(tower))
        {
            foreach (SnapshotRoom room in sf.Rooms)
            {
                if (db.Rooms.First(r => r.Id == room.DefId).Kind != kind) continue;
                (long, long, long)? s = SpotInRoom(db, tower, occ, def, sf, room);
                if (s != null) return s;
            }
        }
        return null;
    }

    private static (long, long, long)? SpotInRoom(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def, SnapshotFloor sf, SnapshotRoom room)
    {
        for (long r = room.Rect[1]; r < room.Rect[1] + room.Rect[3]; r++)
        {
            for (long c = room.Rect[0]; c < room.Rect[0] + room.Rect[2]; c++)
            {
                if (Legal(db, tower, occ, def, sf, c, r)) return (sf.Index, c, r);
            }
        }
        return null;
    }

    private static bool Boosts(RoomDef rdef, EmployeeDef def)
    {
        foreach (Effect e in rdef.Effects)
        {
            if (e.On != "static" || e.Do != "stat" || (e.Permille ?? 0) <= 1000) continue;
            if (e.Subject?.Dept != null && Array.IndexOf(e.Subject.Dept, def.Dept) < 0 && (def.CountsAsDept == null || Array.IndexOf(e.Subject.Dept, def.CountsAsDept) < 0)) continue;
            if (e.Subject?.NotDept != null && Array.IndexOf(e.Subject.NotDept, def.Dept) >= 0) continue;
            return true;
        }
        return false;
    }

    private static (long, long, long)? SpotInBoostingRoom(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def)
    {
        foreach (SnapshotFloor sf in Ordered(tower))
        {
            foreach (SnapshotRoom room in sf.Rooms)
            {
                if (!Boosts(db.Rooms.First(r => r.Id == room.DefId), def)) continue;
                (long, long, long)? s = SpotInRoom(db, tower, occ, def, sf, room);
                if (s != null) return s;
            }
        }
        return null;
    }

    private static (long, long, long)? SpotInAnyRoom(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def)
    {
        foreach (SnapshotFloor sf in Ordered(tower))
        {
            foreach (SnapshotRoom room in sf.Rooms)
            {
                if (db.Rooms.First(r => r.Id == room.DefId).Kind == "reception") continue;
                (long, long, long)? s = SpotInRoom(db, tower, occ, def, sf, room);
                if (s != null) return s;
            }
        }
        return null;
    }

    private static (long, long, long)? SpotOnLanding(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def)
    {
        foreach (SnapshotFloor sf in Ordered(tower))
        {
            for (long r = 0; r < sf.Grid.H; r++)
            {
                if (Legality.RoomAt(sf, 0, r) == null && Legal(db, tower, occ, def, sf, 0, r)) return (sf.Index, 0, r);
            }
        }
        return null;
    }

    private static (long, long, long)? SpotInCorridor(ContentDb db, TowerSnapshot tower, Dictionary<long, List<SnapshotOccupant>> occ, EmployeeDef def)
    {
        foreach (SnapshotFloor sf in Ordered(tower))
        {
            for (long r = 0; r < sf.Grid.H; r++)
            {
                for (long c = 1; c < sf.Grid.W; c++)
                {
                    if (Legality.RoomAt(sf, c, r) == null && Legal(db, tower, occ, def, sf, c, r)) return (sf.Index, c, r);
                }
            }
        }
        return null;
    }

    /// <summary>Step 7: each room gets min(round − 1, index × 2): earlier purchases are older, and no rival room outruns a player's.</summary>
    private static TowerSnapshot Tenure(TowerSnapshot tower, long round)
    {
        int index = 0;
        var floors = tower.Floors.Select(f => f with
        {
            Rooms = f.Rooms.Select(r => r.RoomId.StartsWith("t", StringComparison.Ordinal) && r.RoomId.Length > 1 && char.IsDigit(r.RoomId[1])
                ? r with { TenureRounds = Math.Max(0, Math.Min(round - 1, index++ * 2)) }
                : r).ToArray(),
        }).ToArray();
        return tower with { Floors = floors };
    }
}
