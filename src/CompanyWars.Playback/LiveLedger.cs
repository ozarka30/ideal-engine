using CompanyWars.Sim;

namespace CompanyWars.Playback;

/// <summary>One line of the live ledger.</summary>
public sealed record LedgerLine(long Tick, string Side, string Kind, string Text, long Count, bool Rollup);

/// <summary>
/// The live ledger (D-08): a view over the entries that coalesces same-(source, ability, kind) entries within
/// 1.0 s and admits at most four new lines per second per side; what does not fit becomes one roll-up line.
/// Lines are computed per one-second window from the entries in that window, so the view is a pure function of
/// the tick and reproduces identically on every run and after any seek.
/// </summary>
public static class LiveLedger
{
    public const int LinesPerSecond = 4;
    public const int VisibleLines = 6;

    /// <summary>The lines a side's panel shows at <paramref name="tick"/>: the newest first, at most <see cref="VisibleLines"/>.</summary>
    public static IReadOnlyList<LedgerLine> Visible(MatchView view, string side, long tick)
    {
        long window = view.Rules.TicksPerSecond;
        var lines = new List<LedgerLine>();
        long currentWindow = tick / window;
        for (long w = currentWindow; w >= 0 && lines.Count < VisibleLines; w--)
        {
            long from = w * window;
            long to = Math.Min(from + window - 1, tick);
            List<LedgerLine> windowLines = WindowLines(view, side, from, to, w == currentWindow);
            for (int i = windowLines.Count - 1; i >= 0 && lines.Count < VisibleLines; i--) lines.Add(windowLines[i]);
        }
        return lines;
    }

    /// <summary>Every line the side's panel would ever show, in order — the measurement behind the line-budget invariant.</summary>
    public static List<LedgerLine> All(MatchView view, string side)
    {
        long window = view.Rules.TicksPerSecond;
        var lines = new List<LedgerLine>();
        for (long from = 0; from <= view.Result.EndTick; from += window)
        {
            lines.AddRange(WindowLines(view, side, from, Math.Min(from + window - 1, view.Result.EndTick), false));
        }
        return lines;
    }

    private static List<LedgerLine> WindowLines(MatchView view, string side, long from, long to, bool partial)
    {
        var groups = new List<(string Key, List<LedgerEntry> Entries)>();
        var byKey = new Dictionary<string, List<LedgerEntry>>(StringComparer.Ordinal);
        foreach (LedgerEntry e in view.EntriesBetween(from, to))
        {
            if (!Shows(e, side)) continue;
            string key = e.Kind + "|" + e.SourceSide + "|" + e.SourceUnit + "|" + e.AbilityId;
            if (!byKey.TryGetValue(key, out List<LedgerEntry>? list))
            {
                list = new List<LedgerEntry>();
                byKey[key] = list;
                groups.Add((key, list));
            }
            list.Add(e);
        }
        var lines = new List<LedgerLine>();
        int budget = LinesPerSecond;
        int shown = 0;
        long hidden = 0;
        foreach ((string _, List<LedgerEntry> entries) in groups)
        {
            if (shown < budget)
            {
                lines.Add(Line(view, side, entries));
                shown++;
            }
            else
            {
                hidden += entries.Count;
            }
        }
        if (hidden > 0)
        {
            // The roll-up takes the fourth slot: three named lines and one "+N more" beats four and a hidden fifth.
            LedgerLine last = lines[lines.Count - 1];
            lines[lines.Count - 1] = new LedgerLine(last.Tick, side, "rollup", $"+{hidden + last.Count} more", hidden + last.Count, true);
        }
        _ = partial;
        return lines;
    }

    /// <summary>Which entries belong on a side's panel: what the side did, and what happened to it.</summary>
    private static bool Shows(LedgerEntry e, string side)
    {
        switch (e.Kind)
        {
            case "banner":
                return true;
            case "regen":
                return e.TargetSide == side && e.GoodwillDelta != 0;
            case "morale":
                return e.TargetSide == side;
            default:
                return e.SourceSide == side;
        }
    }

    private static LedgerLine Line(MatchView view, string side, List<LedgerEntry> entries)
    {
        LedgerEntry first = entries[0];
        long count = entries.Count;
        string text;
        switch (first.Kind)
        {
            case "push":
            case "anomaly":
                {
                    long raw = 0, overflow = 0, share = 0;
                    foreach (LedgerEntry e in entries) { raw += e.Raw; overflow += e.Overflow; share += Math.Abs(e.ShareDelta); }
                    string what = first.Kind == "anomaly" ? (Array.IndexOf(first.Tags, "self_cost") >= 0 ? "self" : "anomaly") : "push";
                    text = $"{Source(view, first)} {what} {raw}{(overflow > 0 ? $" ▸{overflow}" : string.Empty)}{(share > 0 ? $" +{share}sp" : string.Empty)}{Times(count)}";
                    break;
                }
            case "restore":
                {
                    long applied = 0;
                    foreach (LedgerEntry e in entries) applied += e.GoodwillDelta;
                    text = $"{Source(view, first)} restore +{applied}{Times(count)}";
                    break;
                }
            case "regen":
                {
                    long applied = 0;
                    foreach (LedgerEntry e in entries) applied += e.GoodwillDelta;
                    text = $"regen +{applied}{Times(count)}";
                    break;
                }
            case "morale":
                {
                    long cap = 0;
                    foreach (LedgerEntry e in entries) cap += e.CapDelta;
                    string why = Array.IndexOf(first.Tags, "burnout") >= 0 ? "burnout" : Ability(first);
                    text = $"morale {why} cap {cap}{Times(count)}";
                    break;
                }
            case "status":
                {
                    string status = StatusName(first);
                    long stacks = 0;
                    foreach (LedgerEntry e in entries) stacks += e.Stacks;
                    string verb = stacks < 0 ? "cleanse" : (Array.IndexOf(first.Tags, "overtime_expired") >= 0 ? "hangover" : "apply");
                    text = $"{Source(view, first)} {verb} {status} {(stacks >= 0 ? "+" : string.Empty)}{stacks}{Times(count)}";
                    break;
                }
            case "retrigger":
                text = $"{Source(view, first)} retrigger → {Target(view, first)}{Times(count)}";
                break;
            case "whiff":
                text = $"{Source(view, first)} whiff{(first.Tags.Length > 0 ? " " + first.Tags[0] : string.Empty)}{Times(count)}";
                break;
            case "banner":
                text = first.Raw switch { 0 => "— QUARTER OPEN —", 1 => "— MONTH 2 —", 2 => "— CRUNCH —", _ => "— THE BELL —" };
                break;
            default:
                text = first.Kind;
                break;
        }
        return new LedgerLine(first.Tick, side, first.Kind, text, count, false);
    }

    private static string Times(long count) => count > 1 ? $" ×{count}" : string.Empty;

    private static string Source(MatchView view, LedgerEntry e)
    {
        if (e.SourceUnit >= 0 && view.Unit(e.SourceUnit) is UnitInfo u) return $"{FloorName(u.Unit.FloorIndex)} {u.Name}";
        if (e.AbilityId.StartsWith("furn.", StringComparison.Ordinal) || e.AbilityId.StartsWith("room.", StringComparison.Ordinal)) return $"{FloorName(e.SourceFloor)} {Ability(e)}";
        return Ability(e);
    }

    private static string Target(MatchView view, LedgerEntry e)
    {
        if (e.TargetUnits.Length > 0 && view.Unit(e.TargetUnits[0]) is UnitInfo u) return u.Name;
        return e.TargetSide;
    }

    private static string Ability(LedgerEntry e)
    {
        int dot = e.AbilityId.IndexOf('.');
        return dot < 0 ? e.AbilityId : e.AbilityId[(dot + 1)..].Replace('_', ' ');
    }

    private static string StatusName(LedgerEntry e)
    {
        foreach (string t in e.Tags)
        {
            if (t.StartsWith("status.", StringComparison.Ordinal)) return t[7..];
        }
        return "status";
    }

    public static string FloorName(long index) => index switch { -1 => "B1", 0 => "G", 1 => "1F", 2 => "2F", 3 => "3F", _ => "?" };
}
