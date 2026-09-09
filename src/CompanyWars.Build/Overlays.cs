using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>What the build screen shows the moment something is placed (GAME_DESIGN.md §5.2, §20): aura badges, link lines, tier markers.</summary>
public static class Overlays
{
    /// <summary>The room aura an employee stands in, as permille including Tenure, for its ability kind; 1000 when nothing applies.</summary>
    public static long AuraPermille(ContentDb db, TowerSnapshot tower, SnapshotFloor floor, SnapshotOccupant employee)
    {
        SnapshotRoom? room = Legality.RoomAt(floor, employee.Tile[0], employee.Tile[1]);
        if (room == null) return db.Rules.Floors.CorridorMult;
        RoomDef rdef = db.Rooms.First(r => r.Id == room.DefId);
        EmployeeDef def = db.Employees.First(e => e.Id == employee.DefId);
        string kind = def.Effects.First(e => e.On == "ability").Do;
        long tier = db.Rules.Tenure.TierRounds.Count(t => t <= room.TenureRounds);
        long mult = 1000;
        foreach (Effect e in rdef.Effects)
        {
            if (e.On != "static" || e.Do != "stat" || e.Stat != kind || e.Permille == null) continue;
            if (e.FromTier.HasValue && tier < e.FromTier.Value) continue;
            if (e.UntilTier.HasValue && tier >= e.UntilTier.Value) continue;
            if (e.Subject?.Dept != null && Array.IndexOf(e.Subject.Dept, def.Dept) < 0 && (def.CountsAsDept == null || Array.IndexOf(e.Subject.Dept, def.CountsAsDept) < 0)) continue;
            if (e.Subject?.NotDept != null && Array.IndexOf(e.Subject.NotDept, def.Dept) >= 0) continue;
            mult = mult * (e.Permille.Value + db.Rules.Tenure.StepPermille * tier) / 1000;
        }
        return mult;
    }

    public static string AuraBadge(long permille) => $"×{permille / 1000}.{permille % 1000 / 100}{(permille % 100 / 10 != 0 ? (permille % 100 / 10).ToString() : string.Empty)}";

    /// <summary>The employees a piece of furniture triggers: those adjacent to any of its tiles that its effects' subjects or own-targets name.</summary>
    public static List<SnapshotOccupant> Linked(ContentDb db, SnapshotFloor floor, SnapshotOccupant furniture)
    {
        FurnitureDef fdef = db.Furniture.First(f => f.Id == furniture.DefId);
        var tiles = new List<(long, long)>();
        for (long dy = 0; dy < fdef.Footprint.H; dy++)
        {
            for (long dx = 0; dx < fdef.Footprint.W; dx++) tiles.Add((furniture.Tile[0] + dx, furniture.Tile[1] + dy));
        }
        var result = new List<SnapshotOccupant>();
        foreach (SnapshotOccupant o in floor.Occupants)
        {
            if (o.Kind != "employee") continue;
            bool adjacent = tiles.Any(t => Math.Abs(t.Item1 - o.Tile[0]) + Math.Abs(t.Item2 - o.Tile[1]) == 1);
            if (!adjacent) continue;
            EmployeeDef def = db.Employees.First(e => e.Id == o.DefId);
            bool affected = false;
            foreach (Effect e in fdef.Effects)
            {
                string[]? dept = e.Subject?.Dept ?? e.Target?.Dept;
                if (dept == null || Array.IndexOf(dept, def.Dept) >= 0 || (def.CountsAsDept != null && Array.IndexOf(dept, def.CountsAsDept) >= 0)) { affected = true; break; }
            }
            if (affected) result.Add(o);
        }
        return result;
    }

    public static long Tier(ContentDb db, SnapshotRoom room) => db.Rules.Tenure.TierRounds.Count(t => t <= room.TenureRounds);
}
