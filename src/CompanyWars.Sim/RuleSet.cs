using System.Text;

namespace CompanyWars.Sim;

/// <summary>
/// Every constant in SIMULATION_SPEC.md §3, plus the match round and content version (§4.3).
/// Values come from content/rules.json, content/floors.json and content/statuses.json;
/// the sim never reads files, so <c>CompanyWars.Content</c> builds this.
/// </summary>
public sealed record RuleSet(
    long Round,
    string ContentVersion,
    long SchemaVersion,
    // §3.1 Time
    long TicksPerSecond,
    long QuarterTicks,
    long[] MonthStart,
    long[] PushMult,
    long[] RegenMult,
    // §3.2 Goodwill and Market Share
    long GoodwillBaseConstant,
    long GoodwillBasePerRound,
    long RegenBasePermille,
    long RegenInterval,
    long RegenSuppressWindow,
    long SuppressThresholdPermille,
    long MoraleInterval,
    long MoralePerStack,
    long MoraleRatePermille,
    long AnomalySelfCostPermille,
    long ShareTotal,
    long ShareStart,
    long[] SpPerPushPermille,
    // §3.3 Floors and rooms. Floor arrays are indexed by FLOOR_INDEX + 1 (B1, G, F1, F2, F3).
    string[] FloorIds,
    long[] FloorMult,
    long CorridorMult,
    long[] TenureTierRounds,
    long TenureStepPermille,
    long ReceptionCapPerOccupant,
    long PortalEmployeeCapTax,
    long B1LeaseCapTax,
    // §3.4 Status effects
    long BurnoutMax,
    long BurnoutPushPenaltyPermille,
    long OvertimeMax,
    long OvertimeDuration,
    long OvertimeRatePermille,
    long BureaucracyMax,
    long BureaucracyDuration,
    long BureaucracyRatePermille,
    long RetriggerDepthMax)
{
    public long GoodwillBase(long round) => GoodwillBaseConstant + GoodwillBasePerRound * round;

    /// <summary>The largest month index whose start tick is at or before <paramref name="tick"/>.</summary>
    public int Month(long tick)
    {
        int m = 0;
        for (int i = 0; i < MonthStart.Length; i++)
        {
            if (MonthStart[i] <= tick) m = i;
        }
        return m;
    }

    public long FloorMultFor(int floorIndex) => FloorMult[floorIndex + 1];

    public int FloorIndexOf(string floorId)
    {
        for (int i = 0; i < FloorIds.Length; i++)
        {
            if (FloorIds[i] == floorId) return i - 1;
        }
        return int.MinValue;
    }

    /// <summary>Canonical serialisation for the replay header (§16.4): keys in the order §3 lists them.</summary>
    public string Canonical()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        Canon.Field(sb, "round", Round); sb.Append(',');
        Canon.Field(sb, "contentVersion", ContentVersion); sb.Append(',');
        Canon.Field(sb, "schemaVersion", SchemaVersion); sb.Append(',');
        Canon.Field(sb, "TICKS_PER_SECOND", TicksPerSecond); sb.Append(',');
        Canon.Field(sb, "QUARTER_TICKS", QuarterTicks); sb.Append(',');
        Canon.Field(sb, "MONTH_START", MonthStart); sb.Append(',');
        Canon.Field(sb, "PUSH_MULT", PushMult); sb.Append(',');
        Canon.Field(sb, "REGEN_MULT", RegenMult); sb.Append(',');
        sb.Append("\"GOODWILL_BASE\":{");
        Canon.Field(sb, "constant", GoodwillBaseConstant); sb.Append(',');
        Canon.Field(sb, "perRound", GoodwillBasePerRound); sb.Append("},");
        Canon.Field(sb, "REGEN_BASE_PERMILLE", RegenBasePermille); sb.Append(',');
        Canon.Field(sb, "REGEN_INTERVAL", RegenInterval); sb.Append(',');
        Canon.Field(sb, "REGEN_SUPPRESS_WINDOW", RegenSuppressWindow); sb.Append(',');
        Canon.Field(sb, "SUPPRESS_THRESHOLD_PERMILLE", SuppressThresholdPermille); sb.Append(',');
        Canon.Field(sb, "MORALE_INTERVAL", MoraleInterval); sb.Append(',');
        Canon.Field(sb, "MORALE_PER_STACK", MoralePerStack); sb.Append(',');
        Canon.Field(sb, "MORALE_RATE_PERMILLE", MoraleRatePermille); sb.Append(',');
        Canon.Field(sb, "ANOMALY_SELF_COST_PERMILLE", AnomalySelfCostPermille); sb.Append(',');
        Canon.Field(sb, "SHARE_TOTAL", ShareTotal); sb.Append(',');
        Canon.Field(sb, "SHARE_START", ShareStart); sb.Append(',');
        Canon.Field(sb, "SP_PER_PUSH_PERMILLE", SpPerPushPermille); sb.Append(',');
        sb.Append("\"FLOOR_MULT\":{");
        for (int i = 0; i < FloorIds.Length; i++)
        {
            if (i > 0) sb.Append(',');
            Canon.Field(sb, FloorIds[i], FloorMult[i]);
        }
        sb.Append("},");
        Canon.Field(sb, "CORRIDOR_MULT", CorridorMult); sb.Append(',');
        Canon.Field(sb, "TENURE_TIER_ROUNDS", TenureTierRounds); sb.Append(',');
        Canon.Field(sb, "TENURE_STEP_PERMILLE", TenureStepPermille); sb.Append(',');
        Canon.Field(sb, "RECEPTION_CAP_PER_OCCUPANT", ReceptionCapPerOccupant); sb.Append(',');
        Canon.Field(sb, "PORTAL_EMPLOYEE_CAP_TAX", PortalEmployeeCapTax); sb.Append(',');
        Canon.Field(sb, "B1_LEASE_CAP_TAX", B1LeaseCapTax); sb.Append(',');
        Canon.Field(sb, "BURNOUT_MAX", BurnoutMax); sb.Append(',');
        Canon.Field(sb, "BURNOUT_PUSH_PENALTY_PERMILLE", BurnoutPushPenaltyPermille); sb.Append(',');
        Canon.Field(sb, "OVERTIME_MAX", OvertimeMax); sb.Append(',');
        Canon.Field(sb, "OVERTIME_DURATION", OvertimeDuration); sb.Append(',');
        Canon.Field(sb, "OVERTIME_RATE_PERMILLE", OvertimeRatePermille); sb.Append(',');
        Canon.Field(sb, "BUREAUCRACY_MAX", BureaucracyMax); sb.Append(',');
        Canon.Field(sb, "BUREAUCRACY_DURATION", BureaucracyDuration); sb.Append(',');
        Canon.Field(sb, "BUREAUCRACY_RATE_PERMILLE", BureaucracyRatePermille); sb.Append(',');
        Canon.Field(sb, "RETRIGGER_DEPTH_MAX", RetriggerDepthMax);
        sb.Append('}');
        return sb.ToString();
    }
}
