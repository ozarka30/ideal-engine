using CompanyWars.Content;
using CompanyWars.Sim;
using CompanyWars.Tools;
using Xunit;

namespace CompanyWars.Playback.Tests;

public class PlaybackTests
{
    private static readonly string Root = RepoRoot.Find(AppContext.BaseDirectory);
    private static readonly Lazy<ContentDb> Db = new(() => ContentLoader.Load(Root));

    private static MatchView Fight(string rivalA, string rivalB, uint seed = 1)
    {
        ContentDb db = Db.Value;
        ScriptedRival a = db.Rival(rivalA);
        ScriptedRival b = db.Rival(rivalB);
        RuleSet rules = db.RuleSetFor(Math.Max(a.Round, b.Round));
        MatchResult r = Simulator.Simulate(seed, a.Snapshot, b.Snapshot, rules, db.ToContentTable());
        return new MatchView(r, a.Snapshot, b.Snapshot, rules, db.ToContentTable());
    }

    [Fact]
    public void ClockAdvancesByAccumulatorNotByFrame()
    {
        var clock = new PlaybackClock(20, 1199);
        Assert.Equal(0, clock.Advance(16_000));   // 16 ms: less than one tick
        Assert.Equal(1, clock.Advance(34_000));   // 50 ms total: one tick
        clock.SetSpeed(4);
        Assert.Equal(8, clock.Advance(100_000));  // 100 ms at 4x: eight ticks
        clock.Skip();
        Assert.True(clock.Finished);
        Assert.Equal(0, clock.Advance(1_000_000));
    }

    [Fact]
    public void DerivedLoyaltyAndRevenueMatchRecomputationFromEntries()
    {
        MatchView v = Fight("rival.boss_parent_company", "rival.boss_compliance_office");
        for (long t = 0; t <= v.Result.EndTick; t += 37)
        {
            long la = v.CapAtStartA + v.Result.Entries.Where(e => e.TargetSide == "A" && e.Tick <= t).Sum(e => e.LoyaltyDelta);
            Assert.Equal(la, v.FrameA(t).Loyalty);
        }
        Assert.Equal(v.Result.FinalLoyalty.A, v.FrameA(v.Result.EndTick).Loyalty);
        Assert.Equal(v.Result.FinalCap.B, v.FrameB(v.Result.EndTick).Cap);
        Assert.Equal(v.Result.FinalRevenue.A, v.FrameA(v.Result.EndTick).Revenue);
        Assert.Equal(v.Result.FinalRevenue.B, v.FrameB(v.Result.EndTick).Revenue);
    }

    [Fact]
    public void LedgerNeverExceedsFourNewLinesPerSecondOnAnyScriptedMatchup()
    {
        // ROADMAP M1 exit criterion: measured, not eyeballed, over every ordered pair of scripted rivals.
        ContentDb db = Db.Value;
        long worstRaw = 0;
        foreach (ScriptedRival a in db.ScriptedRivals)
        {
            foreach (ScriptedRival b in db.ScriptedRivals)
            {
                MatchView v = Fight(a.Id, b.Id, 7);
                foreach (string side in new[] { "A", "B" })
                {
                    List<LedgerLine> lines = LiveLedger.All(v, side);
                    var perSecond = lines.GroupBy(l => l.Tick / v.Rules.TicksPerSecond).Select(g => g.Count());
                    Assert.True(perSecond.All(n => n <= LiveLedger.LinesPerSecond), $"{a.Id} vs {b.Id} side {side}: {perSecond.Max()} lines in one second");
                    long raw = v.Result.Entries.Count(e => e.SourceSide == side || e.TargetSide == side);
                    worstRaw = Math.Max(worstRaw, raw);
                }
            }
        }
        Assert.True(worstRaw > 0);
    }

    [Fact]
    public void CoalescingMergesSameSourceWithinASecond()
    {
        MatchView v = Fight("rival.boss_parent_company", "rival.boss_compliance_office");
        List<LedgerLine> all = LiveLedger.All(v, "A");
        Assert.Contains(all, l => l.Count > 1 || l.Rollup);
        IReadOnlyList<LedgerLine> visible = LiveLedger.Visible(v, "A", 400);
        Assert.True(visible.Count <= LiveLedger.VisibleLines);
        Assert.True(visible.All(l => l.Tick <= 400));
    }

    [Fact]
    public void VisibleLinesAreAPureFunctionOfTheTick()
    {
        MatchView v = Fight("rival.tut_03_cram_school", "rival.tut_04_print_works");
        string Render(long t) => string.Join("\n", LiveLedger.Visible(v, "B", t).Select(l => l.Text));
        string once = Render(600);
        _ = Render(1100);
        _ = Render(0);
        Assert.Equal(once, Render(600));
    }

    [Fact]
    public void AutopsyProducesThreeFindingsAndConsistentBars()
    {
        MatchView v = Fight("rival.boss_parent_company", "rival.boss_compliance_office");
        string[] findings = Autopsy.Findings(v, "Parent Company", "Compliance Office");
        Assert.Equal(3, findings.Length);
        Assert.All(findings, f => Assert.False(string.IsNullOrWhiteSpace(f)));
        long[] timeline = Autopsy.Timeline(v, 60);
        Assert.Equal(60, timeline.Length);
        Assert.All(timeline, s => Assert.InRange(s, 0, 1000));
        FloorTotals[] bars = Autopsy.FloorBars(v);
        Assert.Equal(5, bars.Length);
        Assert.Equal(v.Result.TotalSales.A, v.Result.Entries.Where(e => e.Kind == "sales" && e.SourceSide == "A").Sum(e => e.RevenueDelta));
        Assert.True(bars.Sum(b => b.A) >= v.Result.TotalSales.A);
        Assert.Equal(v.Result.Entries.Length, Autopsy.Filter(v, new HashSet<string>(), new HashSet<string>(), new HashSet<long>()).Count);
        Assert.All(Autopsy.Filter(v, new HashSet<string> { "sales" }, new HashSet<string>(), new HashSet<long>()), e => Assert.Equal("sales", e.Kind));
    }

    [Fact]
    public void UnitBarsAreOrderedAndNeverExceedTheFloorTotals()
    {
        MatchView v = Fight("rival.boss_parent_company", "rival.boss_compliance_office");
        foreach (string side in new[] { "A", "B" })
        {
            UnitTotals[] staff = Autopsy.UnitBars(v, side, 100);
            Assert.NotEmpty(staff);
            for (int i = 1; i < staff.Length; i++) Assert.True(staff[i - 1].Total >= staff[i].Total);
            long floors = 0;
            foreach (FloorTotals f in Autopsy.FloorBars(v)) floors += side == "A" ? f.A : f.B;
            long units = 0;
            foreach (UnitTotals u in staff) units += u.Total;
            Assert.True(units <= floors, $"{side}: units {units} > floors {floors}");
            Assert.True(units > 0);
        }
        Assert.Equal(3, Autopsy.UnitBars(v, "A", 3).Length);
    }
}
