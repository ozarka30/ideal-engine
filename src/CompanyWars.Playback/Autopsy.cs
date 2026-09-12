using CompanyWars.Sim;

namespace CompanyWars.Playback;

/// <summary>Totals a floor's units earned or took, per side (GAME_DESIGN.md §19.3 per-floor bars).</summary>
public sealed record FloorTotals(long FloorIndex, long A, long B);

/// <summary>One employee's total Sales + Poach + Curse + Scandal, for the STAFF view of the autopsy bars (D-68).</summary>
public sealed record UnitTotals(long UnitIndex, string Name, long FloorIndex, long Total);

/// <summary>The autopsy's derived views: the lead timeline, the per-floor bars, the three findings, the filtered ledger.</summary>
public static class Autopsy
{
    public static readonly string[] KindFilters = { "sales", "poach", "scandal", "curse", "regen", "status" };

    private static bool IsOutput(LedgerEntry e) => e.Kind is "sales" or "poach" or "curse" or "scandal" && Array.IndexOf(e.Tags, "self_cost") < 0;

    /// <summary>Side A's share of the takings, in permille, sampled at the start of each of <paramref name="columns"/> equal spans of the quarter.</summary>
    public static long[] Timeline(MatchView view, int columns)
    {
        var samples = new long[columns];
        long span = view.Rules.QuarterTicks / columns;
        for (int c = 0; c < columns; c++)
        {
            long t = Math.Min(c * span, view.Result.EndTick);
            samples[c] = view.RevenueShare(t);
        }
        return samples;
    }

    /// <summary>Sales, Poach, Curse and Scandal raw from each side's units, by the floor the unit stood on. Five rows, B1 to 3F.</summary>
    public static FloorTotals[] FloorBars(MatchView view)
    {
        var a = new long[5];
        var b = new long[5];
        foreach (LedgerEntry e in view.Result.Entries)
        {
            if (!IsOutput(e)) continue;
            long floor = e.SourceUnit >= 0 && view.Unit(e.SourceUnit) is UnitInfo u ? u.Unit.FloorIndex : e.SourceFloor;
            if (floor < -1 || floor > 3) continue;
            if (e.SourceSide == "A") a[floor + 1] += e.Raw; else if (e.SourceSide == "B") b[floor + 1] += e.Raw;
        }
        var rows = new FloorTotals[5];
        for (int i = 0; i < 5; i++) rows[i] = new FloorTotals(i - 1, a[i], b[i]);
        return rows;
    }

    /// <summary>The side's employees by output, most first, at most <paramref name="top"/>: the chart players sell by.</summary>
    public static UnitTotals[] UnitBars(MatchView view, string side, int top)
    {
        var totals = new Dictionary<long, long>();
        foreach (LedgerEntry e in view.Result.Entries)
        {
            if (!IsOutput(e)) continue;
            if (e.SourceSide != side || e.SourceUnit < 0) continue;
            totals[e.SourceUnit] = totals.GetValueOrDefault(e.SourceUnit) + e.Raw;
        }
        return totals.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(top)
            .Select(kv => new UnitTotals(kv.Key, view.Unit(kv.Key)?.Name ?? $"unit {kv.Key}", view.Unit(kv.Key)?.Unit.FloorIndex ?? 0, kv.Value))
            .ToArray();
    }

    /// <summary>Three findings, each a short sentence, in order of what most explains the result.</summary>
    public static string[] Findings(MatchView view, string nameA, string nameB)
    {
        MatchResult r = view.Result;
        var findings = new List<string>();
        long breakA = view.BreakTick("A");
        long breakB = view.BreakTick("B");
        string winner = r.Winner == "A" ? nameA : r.Winner == "B" ? nameB : "nobody";

        // 1. The break: when a firm's Revenue became open to Poaching.
        if (breakA < 0 && breakB < 0) findings.Add("Neither firm's Loyalty broke; the Bell decided it on Revenue made.");
        else if (breakA >= 0 && breakB >= 0) findings.Add($"Both firms' Loyalty broke: {(breakA < breakB ? nameA : nameB)} first at {Seconds(Math.Min(breakA, breakB))}, the other at {Seconds(Math.Max(breakA, breakB))}.");
        else findings.Add($"{(breakA >= 0 ? nameA : nameB)}'s Loyalty broke at {Seconds(Math.Max(breakA, breakB))}; its Revenue was open to Poaching from then on.");

        // 2. Scandal and cap erosion.
        long capLossA = view.CapAtStartA - r.FinalCap.A;
        long capLossB = view.CapAtStartB - r.FinalCap.B;
        if (capLossA > 0 || capLossB > 0)
        {
            bool aWorse = capLossA >= capLossB;
            long loss = aWorse ? capLossA : capLossB;
            long start = aWorse ? view.CapAtStartA : view.CapAtStartB;
            findings.Add($"Scandal eroded {(aWorse ? nameA : nameB)}'s Loyalty cap by {loss} of {start} ({loss * 100 / Math.Max(1, start)}%).");
        }

        // 3. Where the money came from.
        FloorTotals[] floors = FloorBars(view);
        string side = r.Winner == "B" ? "B" : "A";
        long best = -1, bestFloor = 0, total = 0;
        foreach (FloorTotals f in floors)
        {
            long v = side == "A" ? f.A : f.B;
            total += v;
            if (v > best) { best = v; bestFloor = f.FloorIndex; }
        }
        if (total > 0) findings.Add($"{(side == "A" ? nameA : nameB)}'s {LiveLedger.FloorName(bestFloor)} did {best * 100 / total}% of its work.");

        // 4. Suppression, as a fallback finding.
        if (findings.Count < 3)
        {
            long suppressed = 0, events = 0;
            foreach (LedgerEntry e in r.Entries)
            {
                if (e.Kind != "regen" || e.TargetSide != (r.Winner == "A" ? "B" : "A")) continue;
                events++;
                if (Array.IndexOf(e.Tags, "suppressed") >= 0) suppressed++;
            }
            if (events > 0) findings.Add($"The loser's clients were kept from drifting back for {suppressed} of {events} events.");
        }
        if (findings.Count < 3) findings.Add($"Result: {winner} at the Bell, ¥{r.FinalRevenue.A} to ¥{r.FinalRevenue.B}.");
        return findings.GetRange(0, Math.Min(3, findings.Count)).ToArray();
    }

    /// <summary>The full ledger filtered by kind chips, side chips and floor chips (empty set = everything).</summary>
    public static List<LedgerEntry> Filter(MatchView view, ISet<string> kinds, ISet<string> sides, ISet<long> floors)
    {
        var list = new List<LedgerEntry>();
        foreach (LedgerEntry e in view.Result.Entries)
        {
            if (kinds.Count > 0 && !kinds.Contains(e.Kind)) continue;
            if (sides.Count > 0 && !sides.Contains(e.SourceSide) && !sides.Contains(e.TargetSide)) continue;
            if (floors.Count > 0)
            {
                long floor = e.SourceUnit >= 0 && view.Unit(e.SourceUnit) is UnitInfo u ? u.Unit.FloorIndex : e.SourceFloor;
                if (!floors.Contains(floor)) continue;
            }
            list.Add(e);
        }
        return list;
    }

    public static string Seconds(long tick) => $"{tick / 20}.{tick % 20 * 5 / 10}s";

    public static string ResultBanner(MatchView view, long round, string perspectiveSide)
    {
        MatchResult r = view.Result;
        string outcome = r.Winner == "draw" ? "DRAW" : r.Winner == perspectiveSide ? "WON" : "LOST";
        long mine = perspectiveSide == "A" ? r.FinalRevenue.A : r.FinalRevenue.B;
        long theirs = perspectiveSide == "A" ? r.FinalRevenue.B : r.FinalRevenue.A;
        return $"Q{round} · {outcome} · ¥{mine:N0} TO ¥{theirs:N0}";
    }
}
