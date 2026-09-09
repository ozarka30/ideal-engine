using Xunit;

namespace CompanyWars.Sim.Tests;

/// <summary>SIMULATION_SPEC.md §20, asserted independently of any recorded fixture.</summary>
public class WorkedTraceTests
{
    private static MatchResult Run() => Simulator.Simulate(1u, Builders.MirrorJunior("e_a"), Builders.MirrorJunior("e_b"), TestContent.Db.RuleSetFor(1), TestContent.Db.ToContentTable());

    [Fact]
    public void MirrorEndsInADrawAtTheBellWithTheSpecifiedAggregates()
    {
        MatchResult r = Run();
        Assert.Equal("draw", r.Winner);
        Assert.Equal(1199, r.EndTick);
        Assert.Equal(5000, r.FinalShare);
        Assert.Equal(new SideValues(0, 0), r.FinalGoodwill);
        Assert.Equal(new SideValues(700, 700), r.FinalCap);
        Assert.Equal(new SideValues(1131, 1131), r.TotalPush);
    }

    [Fact]
    public void MirrorHasNinetyEntriesInTheSpecifiedMix()
    {
        MatchResult r = Run();
        Assert.Equal(90, r.Entries.Length);
        Assert.Equal(4, r.Entries.Count(e => e.Kind == "banner"));
        Assert.Equal(58, r.Entries.Count(e => e.Kind == "regen"));
        Assert.Equal(28, r.Entries.Count(e => e.Kind == "push"));
        for (int i = 0; i < r.Entries.Length; i++) Assert.Equal(i, r.Entries[i].Seq);
    }

    [Fact]
    public void MirrorFollowsTheTraceTickByTick()
    {
        MatchResult r = Run();
        LedgerEntry regen40 = r.Entries.First(e => e.Kind == "regen" && e.Tick == 40 && e.SourceSide == "A");
        Assert.Equal(21, regen40.Raw);
        Assert.Equal(0, regen40.GoodwillDelta);

        LedgerEntry[] pushes80 = r.Entries.Where(e => e.Kind == "push" && e.Tick == 80).ToArray();
        Assert.Equal(2, pushes80.Length);
        Assert.Equal("A", pushes80[0].SourceSide); // even tick: A first
        Assert.Equal(54, pushes80[0].Raw);
        Assert.Equal(-54, pushes80[0].GoodwillDelta);

        LedgerEntry regen80 = r.Entries.First(e => e.Kind == "regen" && e.Tick == 80 && e.SourceSide == "B");
        Assert.Contains("suppressed", regen80.Tags);
        Assert.Equal(0, regen80.Raw);

        LedgerEntry regen120 = r.Entries.First(e => e.Kind == "regen" && e.Tick == 120 && e.SourceSide == "A");
        Assert.Equal(21, regen120.GoodwillDelta);

        Assert.Equal(75, r.Entries.First(e => e.Kind == "push" && e.Tick == 400).Raw);
        Assert.Equal(12, r.Entries.First(e => e.Kind == "regen" && e.Tick == 440).Raw);
        Assert.Equal(108, r.Entries.First(e => e.Kind == "push" && e.Tick == 800).Raw);
        Assert.Equal(4, r.Entries.First(e => e.Kind == "regen" && e.Tick == 840).Raw);

        LedgerEntry[] pushes960 = r.Entries.Where(e => e.Kind == "push" && e.Tick == 960).ToArray();
        Assert.Equal(-45, pushes960[0].GoodwillDelta);
        Assert.Equal(63, pushes960[0].Overflow);
        Assert.Equal(504, pushes960[0].ShareDelta);
        Assert.Equal(-504, pushes960[1].ShareDelta);

        Assert.Equal(new long[] { 80, 160, 240, 320, 400, 480, 560, 640, 720, 800, 880, 960, 1040, 1120 }, r.Entries.Where(e => e.Kind == "push" && e.SourceSide == "A").Select(e => e.Tick).ToArray());
    }

    [Fact]
    public void GoodwillAtMonthEndsMatchesTheTrace()
    {
        MatchResult r = Run();
        long GoodwillAfter(long tick) => 700 + r.Entries.Where(e => e.TargetSide == "A" && e.Tick <= tick).Sum(e => e.GoodwillDelta);
        Assert.Equal(568, GoodwillAfter(399));
        Assert.Equal(253, GoodwillAfter(799));
        Assert.Equal(45, GoodwillAfter(959));
    }

    [Fact]
    public void TwoRunsAreByteIdentical()
    {
        MatchResult a = Run();
        MatchResult b = Run();
        Assert.Equal(a.StateHash, b.StateHash);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(a, CompanyWars.Content.ContentJson.Options), System.Text.Json.JsonSerializer.Serialize(b, CompanyWars.Content.ContentJson.Options));
    }
}
