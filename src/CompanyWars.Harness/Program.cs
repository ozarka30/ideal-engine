using System.Diagnostics;
using CompanyWars.Build;
using CompanyWars.Content;
using CompanyWars.Sim;
using CompanyWars.Tools;

namespace CompanyWars.Harness;

public sealed record MatchRow(long Round, string ArchA, string ArchB, uint Seed, string Winner, long SettleTick, long FirstMoneyTick, long MaxHitRevenue);

public static class Program
{
    public static int Main(string[] args)
    {
        string root = RepoRoot.Find();
        long[] rounds = { 1, 6, 12, 16 };
        int seeds = -1;
        bool json = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--rounds" && i + 1 < args.Length) rounds = args[++i].Split(',').Select(long.Parse).ToArray();
            else if (args[i] == "--seeds" && i + 1 < args.Length) seeds = int.Parse(args[++i]);
            else if (args[i] == "--json") json = true;
        }
        ContentDb db = ContentLoader.Load(root);
        if (seeds < 0) seeds = (int)db.Balance.Seeds.Smoke;
        var rows = new List<MatchRow>();
        var sw = Stopwatch.StartNew();
        long matches = 0;
        foreach (long round in rounds)
        {
            RuleSet rules = db.RuleSetFor(round);
            ContentTable table = db.ToContentTable();
            // The field population: every template expanded at the round at 1000 permille, one tower per seed.
            var field = new List<(string Arch, Rival Rival, uint Seed)>();
            for (uint seed = 1; seed <= seeds; seed++)
            {
                foreach (Template t in db.Templates.Templates)
                {
                    field.Add((t.Archetype, TemplateExpander.Expand(db, t.Id, round, seed * 7919u + (uint)t.Id.Length, false, 1000), seed));
                }
            }
            // field vs field: each tower against every tower of a different seed, ordered pairs, mirror included in fight_length.
            foreach ((string archA, Rival a, uint seedA) in field)
            {
                foreach ((string archB, Rival b, uint seedB) in field)
                {
                    if (seedA == seedB) continue;
                    if ((seedA + seedB) % 4 != 0) continue; // thin the pairing so smoke stays under a minute
                    uint seed = Run.Hash(seedA, round, seedB);
                    MatchResult r = Simulator.Simulate(seed, a.Snapshot, b.Snapshot, rules, table);
                    matches++;
                    long first = -1, maxHit = 0, revA = 0, revB = 0, settle = 0;
                    foreach (LedgerEntry e in r.Entries)
                    {
                        if (e.RevenueDelta == 0) continue;
                        if (first < 0) first = e.Tick;
                        maxHit = Math.Max(maxHit, Math.Abs(e.RevenueDelta));
                        // A negative delta is a transfer: the other firm gains what this one lost (SIMULATION_SPEC §16.1).
                        bool toA = e.TargetSide == "A";
                        if (toA) revA += e.RevenueDelta; else revB += e.RevenueDelta;
                        if (e.RevenueDelta < 0) { if (toA) revB -= e.RevenueDelta; else revA -= e.RevenueDelta; }
                        bool winnerAhead = r.Winner == "A" ? revA > revB : r.Winner == "B" && revB > revA;
                        if (!winnerAhead) settle = e.Tick;
                    }
                    rows.Add(new MatchRow(round, archA, archB, seed, r.Winner, settle, first, maxHit));
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"harness: {matches} matches in {sw.ElapsedMilliseconds} ms ({(matches > 0 ? sw.ElapsedMilliseconds * 1000.0 / matches : 0):F1} µs/match), rounds {string.Join(",", rounds)}, {seeds} seeds");
        int failures = 0;
        failures += LateSwing(db, rows);
        failures += BarMovesEarly(db, rows);
        failures += ArchetypeBand(db, rows);
        if (json) File.WriteAllText(Path.Combine(root, "harness_smoke.json"), System.Text.Json.JsonSerializer.Serialize(rows, ContentJson.Indented));
        Console.WriteLine(failures == 0 ? "all invariants pass" : $"{failures} invariant(s) FAIL");
        return failures == 0 ? 0 : 1;
    }

    private static long Median(IEnumerable<long> xs)
    {
        long[] a = xs.OrderBy(x => x).ToArray();
        return a.Length == 0 ? 0 : a[a.Length / 2];
    }

    private static long P90(IEnumerable<long> xs)
    {
        long[] a = xs.OrderBy(x => x).ToArray();
        return a.Length == 0 ? 0 : a[Math.Min(a.Length - 1, (int)(a.Length * 0.9))];
    }

    private static int LateSwing(ContentDb db, List<MatchRow> rows)
    {
        Invariant inv = db.Balance.Invariants.First(i => i.Id == "inv.late_swing");
        long lo = inv.Threshold[0][0].GetInt64(), hi = inv.Threshold[0][1].GetInt64();
        long drawMax = inv.Threshold[1].GetInt64();
        int failures = 0;
        foreach (IGrouping<long, MatchRow> g in rows.GroupBy(r => r.Round).OrderBy(g => g.Key))
        {
            long crunch = db.RuleSetFor(g.Key).MonthStart[2];
            var decided = g.Where(r => r.Winner != "draw").ToList();
            long swing = decided.Count == 0 ? 0 : decided.Count(r => r.SettleTick >= crunch) * 1000L / decided.Count;
            long draw = g.Count(r => r.Winner == "draw") * 1000L / g.Count();
            bool ok = swing >= lo && swing <= hi && draw <= drawMax;
            if (!ok) failures++;
            Console.WriteLine($"  inv.late_swing r{g.Key,2}: won from behind after Crunch {swing}‰ (want {lo}–{hi}), draw {draw}‰ (≤{drawMax}) {(ok ? "ok" : "FAIL")}");
        }
        return failures;
    }

    private static int BarMovesEarly(ContentDb db, List<MatchRow> rows)
    {
        Invariant inv = db.Balance.Invariants.First(i => i.Id == "inv.bar_moves_early");
        long medMax = inv.Threshold[0].GetInt64(), p90Max = inv.Threshold[1].GetInt64();
        int failures = 0;
        foreach (IGrouping<long, MatchRow> g in rows.GroupBy(r => r.Round).OrderBy(g => g.Key))
        {
            var moved = g.Where(r => r.FirstMoneyTick >= 0).Select(r => r.FirstMoneyTick).ToList();
            long median = moved.Count > 0 ? Median(moved) : 1200;
            long p90 = moved.Count > 0 ? P90(moved) : 1200;
            bool ok = median <= medMax && p90 <= p90Max;
            if (!ok) failures++;
            Console.WriteLine($"  inv.bar_moves_early r{g.Key,2}: first move median {median} (≤{medMax}), p90 {p90} (≤{p90Max}) {(ok ? "ok" : "FAIL")}");
        }
        return failures;
    }

    private static int ArchetypeBand(ContentDb db, List<MatchRow> rows)
    {
        Invariant inv = db.Balance.Invariants.First(i => i.Id == "inv.archetype_band");
        long lo = inv.Threshold[0].GetInt64(), hi = inv.Threshold[1].GetInt64();
        int failures = 0;
        foreach (IGrouping<long, MatchRow> g in rows.GroupBy(r => r.Round).OrderBy(g => g.Key))
        {
            var parts = new List<string>();
            foreach (string arch in db.Templates.Templates.Select(t => t.Archetype))
            {
                var asA = g.Where(r => r.ArchA == arch && r.ArchB != arch).Select(r => r.Winner == "A" ? 1000L : r.Winner == "draw" ? 500L : 0L);
                var asB = g.Where(r => r.ArchB == arch && r.ArchA != arch).Select(r => r.Winner == "B" ? 1000L : r.Winner == "draw" ? 500L : 0L);
                long[] all = asA.Concat(asB).ToArray();
                long rate = all.Length == 0 ? 500 : all.Sum() / all.Length;
                bool ok = rate >= lo && rate <= hi;
                if (!ok) failures++;
                parts.Add($"{arch} {rate}‰{(ok ? string.Empty : "!")}");
            }
            Console.WriteLine($"  inv.archetype_band r{g.Key,2} (want {lo}–{hi}): {string.Join(", ", parts)}");
        }
        return failures;
    }
}
