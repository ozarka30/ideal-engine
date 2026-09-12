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
        Assert.Equal(new SideValues(1131, 1131), r.FinalRevenue);
        Assert.Equal(new SideValues(700, 700), r.FinalLoyalty);
        Assert.Equal(new SideValues(700, 700), r.FinalCap);
        Assert.Equal(new SideValues(1131, 1131), r.TotalSales);
    }

    [Fact]
    public void MirrorHasNinetyEntriesInTheSpecifiedMix()
    {
        MatchResult r = Run();
        Assert.Equal(90, r.Entries.Length);
        Assert.Equal(4, r.Entries.Count(e => e.Kind == "banner"));
        Assert.Equal(58, r.Entries.Count(e => e.Kind == "regen"));
        Assert.Equal(28, r.Entries.Count(e => e.Kind == "sales"));
        for (int i = 0; i < r.Entries.Length; i++) Assert.Equal(i, r.Entries[i].Seq);
    }

    [Fact]
    public void MirrorFollowsTheTraceTickByTick()
    {
        MatchResult r = Run();
        LedgerEntry regen40 = r.Entries.First(e => e.Kind == "regen" && e.Tick == 40 && e.SourceSide == "A");
        Assert.Equal(21, regen40.Raw);
        Assert.Equal(0, regen40.LoyaltyDelta);

        LedgerEntry[] sales80 = r.Entries.Where(e => e.Kind == "sales" && e.Tick == 80).ToArray();
        Assert.Equal(2, sales80.Length);
        Assert.Equal("A", sales80[0].SourceSide); // even tick: A first
        Assert.Equal("A", sales80[0].TargetSide); // Sales land on the seller's own firm
        Assert.Equal(54, sales80[0].Raw);
        Assert.Equal(54, sales80[0].RevenueDelta);

        // Nobody Poaches, so nothing suppresses regen.
        LedgerEntry regen80 = r.Entries.First(e => e.Kind == "regen" && e.Tick == 80 && e.SourceSide == "B");
        Assert.DoesNotContain("suppressed", regen80.Tags);
        Assert.Equal(21, regen80.Raw);

        Assert.Equal(75, r.Entries.First(e => e.Kind == "sales" && e.Tick == 400).Raw);
        Assert.Equal(12, r.Entries.First(e => e.Kind == "regen" && e.Tick == 440).Raw);
        Assert.Equal(108, r.Entries.First(e => e.Kind == "sales" && e.Tick == 800).Raw);
        Assert.Equal(4, r.Entries.First(e => e.Kind == "regen" && e.Tick == 840).Raw);

        Assert.Equal(new long[] { 80, 160, 240, 320, 400, 480, 560, 640, 720, 800, 880, 960, 1040, 1120 }, r.Entries.Where(e => e.Kind == "sales" && e.SourceSide == "A").Select(e => e.Tick).ToArray());
    }

    [Fact]
    public void RevenueAtMonthEndsMatchesTheTrace()
    {
        MatchResult r = Run();
        long RevenueAfter(long tick) => r.Entries.Where(e => e.TargetSide == "A" && e.Tick <= tick).Sum(e => e.RevenueDelta);
        Assert.Equal(216, RevenueAfter(399));
        Assert.Equal(591, RevenueAfter(799));
        Assert.Equal(1131, RevenueAfter(1199));
        Assert.All(r.Entries, e => Assert.Equal(0, e.LoyaltyDelta));
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
