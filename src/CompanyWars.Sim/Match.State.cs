using System.Collections.Generic;

namespace CompanyWars.Sim;

internal enum Side
{
    A = 0,
    B = 1,
}

internal static class SideExtensions
{
    public static string Name(this Side s) => s == Side.A ? "A" : "B";

    public static Side Other(this Side s) => s == Side.A ? Side.B : Side.A;
}

/// <summary>The kinds whose value runs the §9.2 pipeline; each indexes <see cref="Unit.Aura"/>.</summary>
internal static class Kind
{
    public const int Sales = 0;
    public const int Poach = 1;
    public const int Curse = 2;
    public const int Pr = 3;
    public const int Count = 4;
}

/// <summary>An <c>afterFire</c> effect attached to a unit at setup (SIMULATION_SPEC.md §9.4).</summary>
internal readonly record struct ExtraEffect(Effect Effect, string SourceId, long EveryN);

internal sealed class Unit
{
    public required int UnitIndex { get; init; }
    public required int IndexWithinSide { get; init; }
    public required Side Side { get; init; }
    public required int FloorIndex { get; init; }
    public required int Col { get; init; }
    public required int Row { get; init; }
    public required EmployeeDef Def { get; init; }
    public required string InstanceId { get; init; }
    public required Effect Ability { get; init; }
    public required string AbilityId { get; init; }

    public RoomState? Room { get; set; }
    public List<Unit> Adjacent { get; } = new();
    public List<ExtraEffect> Extras { get; } = new();

    // §5.4 derived stats
    public long[] CdBaseByMonth { get; } = new long[4];
    public long CdMult { get; set; } = 1000;
    public long CdProgress { get; set; }
    public long[] Aura { get; } = { 1000, 1000, 1000, 1000 };
    public long FlatSales { get; set; }
    public long FloorMult { get; set; } = 1000;
    public long RetriggerBonus { get; set; } = 1000;
    public long PassiveMult { get; set; } = 1000;
    public long? BurnoutOverride { get; set; }
    public long BurnoutDelta { get; set; }
    public bool BurnoutImmune { get; set; }
    public long BurnoutMax { get; set; }
    public bool BureaucracyImmune { get; set; }
    public bool FrozenImmune { get; set; }
    public bool Untargetable { get; set; }
    public bool OvertimePermanent { get; set; }
    public bool CannotBeRetriggered { get; set; }
    public bool WholeFloorAdjacency { get; set; }
    public bool CapProtected { get; set; }
    public long SelfCostDelta { get; set; }
    public long SelfCostPermille { get; set; }
    public Dictionary<string, long> StacksBonus { get; } = new();
    public string? FloorSelectorOverride { get; set; }
    public long OwnCap { get; set; }
    public long GrantedCap { get; set; }
    public long OwnRegen { get; set; }
    public long GrantedRegen { get; set; }
    public long CapContribution { get; set; }
    public long RegenContribution { get; set; }

    // §12.1 status state
    public long FireCount { get; set; }
    public long Burnout { get; set; }
    public List<long> Overtime { get; } = new();
    public List<long> Bureaucracy { get; } = new();
    public long FrozenUntil { get; set; } = -1;

    public long AbilityBaseConstant => Ability.Value?.Constant ?? 0;

    public long CdTotal(int month) => Arith.Permille(CdBaseByMonth[month], CdMult);

    public bool HasDept(string dept) => Def.Dept == dept || Def.CountsAsDept == dept;

    public bool MatchesFilter(string[]? dept, string[]? notDept, string? tag)
    {
        if (dept != null)
        {
            bool any = false;
            foreach (string d in dept)
            {
                if (HasDept(d)) { any = true; break; }
            }
            if (!any) return false;
        }
        if (notDept != null)
        {
            foreach (string d in notDept)
            {
                if (HasDept(d)) return false;
            }
        }
        if (tag != null && System.Array.IndexOf(Def.Tags, tag) < 0) return false;
        return true;
    }
}

internal sealed class FurnitureState
{
    public required int FurnIndex { get; init; }
    public required Side Side { get; init; }
    public required int FloorIndex { get; init; }
    public required FurnitureDef Def { get; init; }
    public required string InstanceId { get; init; }
    public required List<(int Col, int Row)> Tiles { get; init; }
    public RoomState? Room { get; set; }
    public List<Unit> Adjacent { get; } = new();
}

internal sealed class RoomState
{
    public required Side Side { get; init; }
    public required int FloorIndex { get; init; }
    public required string RoomId { get; init; }
    public required RoomDef Def { get; init; }
    public required int Col { get; init; }
    public required int Row { get; init; }
    public required int W { get; init; }
    public required int H { get; init; }
    public required long TenureRounds { get; init; }
    public long Tier { get; set; }
    public List<Unit> Occupants { get; } = new();
    public List<FurnitureState> Furniture { get; } = new();

    public bool Contains(int col, int row) => col >= Col && col < Col + W && row >= Row && row < Row + H;

    public bool Overlaps(RoomState o) => Col < o.Col + o.W && o.Col < Col + W && Row < o.Row + o.H && o.Row < Row + H;

    /// <summary>Tier gating (CONTENT_SCHEMA.md §3.7): active while <c>fromTier &lt;= tier &lt; untilTier</c>.</summary>
    public bool EffectActive(Effect e)
    {
        if (e.FromTier.HasValue && Tier < e.FromTier.Value) return false;
        if (e.UntilTier.HasValue && Tier >= e.UntilTier.Value) return false;
        return true;
    }
}

internal sealed class FloorState
{
    public required int Index { get; init; }
    public required int W { get; init; }
    public required int H { get; init; }
    public List<Unit> Units { get; } = new();
    public List<RoomState> Rooms { get; } = new();
    public List<FurnitureState> Furniture { get; } = new();
}

internal sealed class Firm
{
    public required Side Side { get; init; }
    public required TowerSnapshot Snapshot { get; init; }
    public List<Unit> Units { get; } = new();
    public List<FurnitureState> Furniture { get; } = new();
    public List<RoomState> Rooms { get; } = new();
    public SortedDictionary<int, FloorState> Floors { get; } = new();
    public List<(Unit Unit, RiderDef Def)> Riders { get; } = new();
    public FounderDef? Founder { get; set; }
    public List<ModifierDef> Modifiers { get; } = new();

    // §5.5 firm stats
    public long Cap { get; set; }
    public long CapAtStart { get; set; }
    public long Loyalty { get; set; }
    public long RegenPerEvent { get; set; }
    public long SuppressThreshold { get; set; }
    public long LastSuppressTick { get; set; } = -1000;
    public long Revenue { get; set; }
    public long TotalSales { get; set; }
    public long CapFlat { get; set; }
    public long CapMult { get; set; } = 1000;
    public long RegenFlat { get; set; }
    public long ProtectedCap { get; set; }
    public List<(int FloorIndex, long Permille)> FloorOutput { get; } = new();
    public bool RegenNeverSuppressed { get; set; }
    public bool ReceptionDisabled { get; set; }
    public bool EveryFloorMostPopulated { get; set; }
    public bool FloorSelectorMirror { get; set; }

    public List<Unit> UnitsOnFloor(int index) => Floors.TryGetValue(index, out FloorState? f) ? f.Units : new List<Unit>();
}

/// <summary>Where an entry comes from: a unit, a piece of furniture, a room, or the firm itself.</summary>
internal readonly struct Source
{
    public Source(Side side, Unit? unit, int floor, string abilityId)
    {
        Side = side;
        Unit = unit;
        Floor = floor;
        AbilityId = abilityId;
    }

    public Side Side { get; }
    public Unit? Unit { get; }
    public int Floor { get; }
    public string AbilityId { get; }

    public static Source OfUnit(Unit u, string abilityId) => new(u.Side, u, u.FloorIndex, abilityId);

    public static Source OfFurniture(FurnitureState f) => new(f.Side, null, f.FloorIndex, f.Def.Id);

    public static Source OfRoom(RoomState r) => new(r.Side, null, r.FloorIndex, r.Def.Id);

    public static Source OfFirm(Side side, string abilityId) => new(side, null, -2, abilityId);
}
