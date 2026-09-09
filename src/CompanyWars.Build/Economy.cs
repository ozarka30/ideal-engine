using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>Income, prices, severance, fees, leases and upkeep (GAME_DESIGN.md §6; CONTENT_SCHEMA.md §3.5 economy stats).</summary>
public static class Economy
{
    /// <summary>The modifier that carries this round's unpaid upkeep into the fight, one copy per unpaid ¥1 (D-31).</summary>
    public const string UnpaidUpkeepModifier = "mod.unpaid_upkeep";

    public static long RoundIncome(ContentDb db, long round) => db.Economy.Income.Table[(int)Math.Clamp(round - 1, 0, db.Economy.Income.Table.Length - 1)];

    /// <summary>Sales passives and every other <c>income</c> economy stat in the tower.</summary>
    public static long Passives(ContentDb db, TowerSnapshot tower) => SumStat(db, tower, "income");

    /// <summary>Floor upkeep plus every <c>upkeep</c> economy stat (a rider's Overhead).</summary>
    public static long Upkeep(ContentDb db, TowerSnapshot tower)
    {
        long total = 0;
        foreach (SnapshotFloor f in tower.Floors) total += db.FloorByIndex(f.Index).UpkeepBudget;
        return total + SumStat(db, tower, "upkeep");
    }

    public static long RerollCost(ContentDb db, TowerSnapshot tower) => db.Shop.RerollCost + SumStat(db, tower, "rerollCost");

    public static long Severance(ContentDb db, TowerSnapshot tower, EmployeeDef def)
    {
        string key = def.Extraplanar ? "extraplanar" : def.Tier.ToString();
        long fee = db.Economy.Severance.TryGetValue(key, out long v) ? v : 0;
        fee += SumStat(db, tower, "severance");
        long mult = 1000;
        foreach ((Effect e, string _) in EconomyEffects(db, tower))
        {
            if (e.Stat == "severanceMult") mult = mult * (e.Permille ?? 1000) / 1000;
        }
        return Math.Max(0, fee * mult / 1000);
    }

    public static long EmployeePrice(ContentDb db, EmployeeDef def)
    {
        if (def.Cost > 0) return def.Cost;
        string key = def.Extraplanar ? "extraplanar" : def.Tier.ToString();
        return db.Economy.EmployeeCost.TryGetValue(key, out long v) ? v : 0;
    }

    public static long RenovationFee(ContentDb db, long round) => RoundIncome(db, round);

    public static long RelocationFee(ContentDb db, long round) => RoundIncome(db, round);

    /// <summary>Every economy-phase effect in the tower with its owner, in tower order: employees, rooms (per matching occupant), riders, modifiers.</summary>
    public static IEnumerable<(Effect Effect, string Owner)> EconomyEffects(ContentDb db, TowerSnapshot tower)
    {
        var employees = db.Employees.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var rooms = db.Rooms.ToDictionary(r => r.Id, StringComparer.Ordinal);
        foreach (SnapshotFloor f in tower.Floors)
        {
            foreach (SnapshotOccupant o in f.Occupants)
            {
                if (o.Kind != "employee" || !employees.TryGetValue(o.DefId, out EmployeeDef? def)) continue;
                foreach (Effect e in def.Effects)
                {
                    if (e.On == "economy") yield return (e, o.InstanceId);
                }
                foreach (SnapshotRoom r in f.Rooms)
                {
                    if (!Inside(o, r) || !rooms.TryGetValue(r.DefId, out RoomDef? rdef)) continue;
                    foreach (Effect e in rdef.Effects)
                    {
                        if (e.On != "economy") continue;
                        if (e.Subject?.Dept != null && Array.IndexOf(e.Subject.Dept, def.Dept) < 0 && (def.CountsAsDept == null || Array.IndexOf(e.Subject.Dept, def.CountsAsDept) < 0)) continue;
                        yield return (e, o.InstanceId);
                    }
                }
            }
        }
        var riders = db.Riders.ToDictionary(r => r.Id, StringComparer.Ordinal);
        foreach (RiderRef rr in tower.Globals.Riders)
        {
            if (!riders.TryGetValue(rr.RiderId, out RiderDef? rdef)) continue;
            foreach (Effect e in rdef.Effects)
            {
                if (e.On == "economy") yield return (e, rr.InstanceId);
            }
        }
        var modifiers = db.Modifiers.ToDictionary(m => m.Id, StringComparer.Ordinal);
        foreach (string id in tower.Globals.Modifiers)
        {
            if (!modifiers.TryGetValue(id, out ModifierDef? mdef)) continue;
            foreach (Effect e in mdef.Effects)
            {
                if (e.On == "economy") yield return (e, id);
            }
        }
    }

    private static long SumStat(ContentDb db, TowerSnapshot tower, string stat)
    {
        long total = 0;
        foreach ((Effect e, string _) in EconomyEffects(db, tower))
        {
            if (e.Do == "stat" && e.Stat == stat) total += e.Amount ?? 0;
        }
        return total;
    }

    public static bool Inside(SnapshotOccupant o, SnapshotRoom r) => o.Tile[0] >= r.Rect[0] && o.Tile[0] < r.Rect[0] + r.Rect[2] && o.Tile[1] >= r.Rect[1] && o.Tile[1] < r.Rect[1] + r.Rect[3];

    /// <summary>Whether an employee carries a build-phase flag through a rider (Tenured: cannot be laid off; Landing Only).</summary>
    public static bool HasFlag(ContentDb db, TowerSnapshot tower, string instanceId, string flag)
    {
        foreach ((Effect e, string owner) in EconomyEffects(db, tower))
        {
            if (owner == instanceId && e.Do == "flag" && e.Flag == flag) return true;
        }
        return false;
    }
}
