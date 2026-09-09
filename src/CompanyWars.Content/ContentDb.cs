using CompanyWars.Sim;

namespace CompanyWars.Content;

/// <summary>The loaded, validated, immutable content database (ARCHITECTURE.md §6).</summary>
public sealed class ContentDb
{
    public required ContentIndex Index { get; init; }
    public required RulesFile Rules { get; init; }
    public required EconomyFile Economy { get; init; }
    public required FloorDef[] Floors { get; init; }
    public required StatusDef[] Statuses { get; init; }
    public required EmployeeDef[] Employees { get; init; }
    public required RoomDef[] Rooms { get; init; }
    public required FurnitureDef[] Furniture { get; init; }
    public required Recipe[] Recipes { get; init; }
    public required RiderDef[] Riders { get; init; }
    public required ModifierDef[] Modifiers { get; init; }
    public required ShopFile Shop { get; init; }
    public required Mode[] Modes { get; init; }
    public required CampaignMap Map { get; init; }
    public required TutorialFile Tutorial { get; init; }
    public required FounderDef[] Founders { get; init; }
    public required BalanceFile Balance { get; init; }
    public required TemplateFile Templates { get; init; }
    public required ScriptedRival[] ScriptedRivals { get; init; }

    public string ContentVersion => Index.ContentVersion;

    private ContentTable? _table;

    /// <summary>The resolved definitions the sim consumes.</summary>
    public ContentTable ToContentTable()
    {
        return _table ??= new ContentTable(
            ContentVersion,
            Employees.ToDictionary(e => e.Id, StringComparer.Ordinal),
            Rooms.ToDictionary(r => r.Id, StringComparer.Ordinal),
            Furniture.ToDictionary(f => f.Id, StringComparer.Ordinal),
            Riders.ToDictionary(r => r.Id, StringComparer.Ordinal),
            Modifiers.ToDictionary(m => m.Id, StringComparer.Ordinal),
            Founders.ToDictionary(f => f.Id, StringComparer.Ordinal),
            Statuses.ToDictionary(s => s.Id, StringComparer.Ordinal),
            Floors.ToDictionary(f => f.Id, StringComparer.Ordinal));
    }

    /// <summary>The sim's <see cref="RuleSet"/> for a round (SIMULATION_SPEC.md §4.3): rules.json, floors.json and statuses.json combined.</summary>
    public RuleSet RuleSetFor(long round)
    {
        FloorDef[] byIndex = Floors.OrderBy(f => f.Index).ToArray();
        if (byIndex.Length != 5 || byIndex[0].Index != -1 || byIndex[4].Index != 3) throw new ContentException("floors.json must define exactly the five floor slots -1..3");
        StatusDef burnout = Status(Vocabulary.StatusBurnout);
        StatusDef overtime = Status(Vocabulary.StatusOvertime);
        StatusDef bureaucracy = Status(Vocabulary.StatusBureaucracy);
        return new RuleSet(
            Round: round,
            ContentVersion: ContentVersion,
            SchemaVersion: Rules.SchemaVersion,
            TicksPerSecond: Rules.Time.TicksPerSecond,
            QuarterTicks: Rules.Time.QuarterTicks,
            MonthStart: Rules.Time.MonthStart,
            PushMult: Rules.Time.PushMult,
            RegenMult: Rules.Time.RegenMult,
            GoodwillBaseConstant: Rules.Goodwill.BaseCap.Constant,
            GoodwillBasePerRound: Rules.Goodwill.BaseCap.PerRound,
            RegenBasePermille: Rules.Goodwill.RegenBasePermille,
            RegenInterval: Rules.Goodwill.RegenInterval,
            RegenSuppressWindow: Rules.Goodwill.RegenSuppressWindow,
            SuppressThresholdPermille: Rules.Goodwill.SuppressThresholdPermille,
            MoraleInterval: Rules.Morale.Interval,
            MoralePerStack: Rules.Morale.PerStack,
            MoraleRatePermille: Rules.Morale.RatePermille,
            AnomalySelfCostPermille: Rules.Anomaly.SelfCostPermille,
            ShareTotal: Rules.Share.Total,
            ShareStart: Rules.Share.Start,
            SpPerPushPermille: Rules.Share.SpPerPushPermille,
            FloorIds: byIndex.Select(f => f.Id).ToArray(),
            FloorMult: byIndex.Select(f => f.OutputPermille).ToArray(),
            CorridorMult: Rules.Floors.CorridorMult,
            TenureTierRounds: Rules.Tenure.TierRounds,
            TenureStepPermille: Rules.Tenure.StepPermille,
            ReceptionCapPerOccupant: Rules.Portal.ReceptionCapPerOccupant,
            PortalEmployeeCapTax: Rules.Portal.EmployeeCapTax,
            B1LeaseCapTax: Rules.Portal.B1LeaseCapTax,
            BurnoutMax: burnout.MaxStacks,
            BurnoutPushPenaltyPermille: burnout.PushPenaltyPermillePerStack,
            OvertimeMax: overtime.MaxStacks,
            OvertimeDuration: overtime.DurationTicks ?? 0,
            OvertimeRatePermille: overtime.CooldownRatePermillePerStack,
            BureaucracyMax: bureaucracy.MaxStacks,
            BureaucracyDuration: bureaucracy.DurationTicks ?? 0,
            BureaucracyRatePermille: -bureaucracy.CooldownRatePermillePerStack,
            RetriggerDepthMax: Rules.Retrigger.DepthMax);
    }

    private StatusDef Status(string id)
    {
        foreach (StatusDef s in Statuses)
        {
            if (s.Id == id) return s;
        }
        throw new ContentException($"statuses.json lacks {id}");
    }

    public FloorDef FloorByIndex(long index)
    {
        foreach (FloorDef f in Floors)
        {
            if (f.Index == index) return f;
        }
        throw new ContentException($"no floor with index {index}");
    }

    public ScriptedRival Rival(string id)
    {
        foreach (ScriptedRival r in ScriptedRivals)
        {
            if (r.Id == id) return r;
        }
        throw new ContentException($"no scripted rival {id}");
    }
}

public sealed class ContentException : Exception
{
    public ContentException(string message) : base(message)
    {
    }

    public ContentException(IReadOnlyList<string> errors) : base(string.Join("\n", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; } = Array.Empty<string>();
}
