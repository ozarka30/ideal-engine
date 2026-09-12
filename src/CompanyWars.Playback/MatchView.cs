using CompanyWars.Sim;

namespace CompanyWars.Playback;

/// <summary>What one firm looks like at a tick, derived from the ledger (D-35, D-85).</summary>
public readonly record struct FirmFrame(long Loyalty, long Cap, long Revenue, bool Suppressed, bool Broken);

/// <summary>What a unit is called on screen.</summary>
public sealed record UnitInfo(OrderedUnit Unit, string Name, string Dept);

/// <summary>
/// Playback state is derived, not stored (ARCHITECTURE.md §3): Loyalty at tick t is capAtStart plus the side's
/// loyaltyDelta for entries with tick &lt;= t, and Revenue is the side's revenueDelta, where a negative revenueDelta
/// on one side is the same amount gained by the other (SIMULATION_SPEC.md §16.1). This precomputes per-tick frames
/// once so the screen can seek freely.
/// </summary>
public sealed class MatchView
{
    private readonly FirmFrame[] _a;
    private readonly FirmFrame[] _b;
    private readonly int[] _firstEntryAtTick;

    public MatchView(MatchResult result, TowerSnapshot a, TowerSnapshot b, RuleSet rules, ContentTable content)
    {
        Result = result;
        Rules = rules;
        Units = BuildUnits(a, b, content);
        SnapshotA = a;
        SnapshotB = b;
        CapAtStartA = InitialCap(result, "A");
        CapAtStartB = InitialCap(result, "B");
        long ticks = rules.QuarterTicks;
        _a = new FirmFrame[ticks];
        _b = new FirmFrame[ticks];
        _firstEntryAtTick = new int[ticks + 1];

        long la = CapAtStartA, lb = CapAtStartB, ca = CapAtStartA, cb = CapAtStartB, ra = 0, rb = 0;
        bool sa = false, sb = false;
        int i = 0;
        for (long t = 0; t < ticks; t++)
        {
            _firstEntryAtTick[t] = i;
            while (i < result.Entries.Length && result.Entries[i].Tick == t)
            {
                LedgerEntry e = result.Entries[i];
                if (e.TargetSide == "A")
                {
                    la += e.LoyaltyDelta; ca += e.CapDelta; ra += e.RevenueDelta;
                    if (e.RevenueDelta < 0) rb -= e.RevenueDelta;
                }
                else if (e.TargetSide == "B")
                {
                    lb += e.LoyaltyDelta; cb += e.CapDelta; rb += e.RevenueDelta;
                    if (e.RevenueDelta < 0) ra -= e.RevenueDelta;
                }
                if (e.Kind == "regen")
                {
                    bool suppressed = Array.IndexOf(e.Tags, "suppressed") >= 0;
                    if (e.TargetSide == "A") sa = suppressed; else sb = suppressed;
                }
                i++;
            }
            _a[t] = new FirmFrame(la, ca, ra, sa, la == 0);
            _b[t] = new FirmFrame(lb, cb, rb, sb, lb == 0);
        }
        _firstEntryAtTick[ticks] = i;
    }

    public MatchResult Result { get; }
    public RuleSet Rules { get; }
    public TowerSnapshot SnapshotA { get; }
    public TowerSnapshot SnapshotB { get; }
    public IReadOnlyList<UnitInfo> Units { get; }
    public long CapAtStartA { get; }
    public long CapAtStartB { get; }

    public FirmFrame FrameA(long tick) => _a[Clamp(tick)];
    public FirmFrame FrameB(long tick) => _b[Clamp(tick)];
    public int Month(long tick) => Rules.Month(Clamp(tick));

    /// <summary>Side A's share of the quarter's takings so far, in permille; 500 before either firm has earned.</summary>
    public long RevenueShare(long tick)
    {
        long a = FrameA(tick).Revenue, b = FrameB(tick).Revenue;
        return a + b == 0 ? 500 : a * 1000 / (a + b);
    }

    /// <summary>The entries emitted at exactly this tick.</summary>
    public ArraySegment<LedgerEntry> EntriesAt(long tick)
    {
        long t = Clamp(tick);
        return new ArraySegment<LedgerEntry>(Result.Entries, _firstEntryAtTick[t], _firstEntryAtTick[t + 1] - _firstEntryAtTick[t]);
    }

    /// <summary>The entries with tick in [from, to].</summary>
    public ArraySegment<LedgerEntry> EntriesBetween(long from, long to)
    {
        long f = Clamp(Math.Max(0, from));
        long t = Clamp(to);
        if (t < f) return new ArraySegment<LedgerEntry>(Result.Entries, 0, 0);
        return new ArraySegment<LedgerEntry>(Result.Entries, _firstEntryAtTick[f], _firstEntryAtTick[t + 1] - _firstEntryAtTick[f]);
    }

    /// <summary>The first tick at which the side's Loyalty reached zero, or -1. From then on its Revenue was open to Poaching.</summary>
    public long BreakTick(string side)
    {
        FirmFrame[] frames = side == "A" ? _a : _b;
        for (long t = 0; t <= Result.EndTick && t < frames.Length; t++)
        {
            if (frames[t].Broken) return t;
        }
        return -1;
    }

    public UnitInfo? Unit(long unitIndex) => unitIndex >= 0 && unitIndex < Units.Count ? Units[(int)unitIndex] : null;

    private long Clamp(long tick) => Math.Clamp(tick, 0, Rules.QuarterTicks - 1);

    /// <summary>The cap before any entry: the recorded final cap less every capDelta the side took.</summary>
    private static long InitialCap(MatchResult result, string side)
    {
        long cap = side == "A" ? result.FinalCap.A : result.FinalCap.B;
        foreach (LedgerEntry e in result.Entries)
        {
            if (e.TargetSide == side) cap -= e.CapDelta;
        }
        return cap;
    }

    private static IReadOnlyList<UnitInfo> BuildUnits(TowerSnapshot a, TowerSnapshot b, ContentTable content)
    {
        var list = new List<UnitInfo>();
        foreach (OrderedUnit u in UnitOrdering.Canonical(a, b))
        {
            string name = content.Employees.TryGetValue(u.DefId, out EmployeeDef? def) ? def.Name : u.DefId;
            string dept = def?.Dept ?? string.Empty;
            list.Add(new UnitInfo(u, name, dept));
        }
        return list;
    }
}
