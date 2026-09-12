using Xunit;

namespace CompanyWars.Sim.Tests;

/// <summary>SIMULATION_SPEC.md §9.3 (D-87): Sales earn in proportion to Loyalty over the starting cap.</summary>
public class LoyaltySalesTests
{
    [Fact]
    public void APoachedFirmEarnsLessThanItSells()
    {
        TowerSnapshot poacher = Builders.Tower(1, "founder.okada", new[]
        {
            Builders.Ground(),
            Builders.Floor(1, 5, 3, Array.Empty<SnapshotRoom>(), Builders.Emp("emp.counsel", 2, 1, "e_a")),
        });
        MatchResult r = Simulator.Simulate(1u, poacher, Builders.MirrorJunior("e_b"), TestContent.Db.RuleSetFor(1), TestContent.Db.ToContentTable());
        LedgerEntry[] sales = r.Entries.Where(e => e.Kind == "sales" && e.SourceSide == "B").ToArray();
        Assert.All(sales, e => Assert.InRange(e.RevenueDelta, 0, e.Raw));
        Assert.Contains(sales, e => e.RevenueDelta < e.Raw);
        Assert.Equal(r.TotalSales.B, sales.Sum(e => e.RevenueDelta));
    }
}
