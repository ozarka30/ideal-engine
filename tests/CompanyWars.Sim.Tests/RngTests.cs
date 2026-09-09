using Xunit;

namespace CompanyWars.Sim.Tests;

public class RngTests
{
    [Fact]
    public void Mulberry32MatchesTheCanonicalSequence()
    {
        // Canonical mulberry32: seed 0 → first outputs of the reference JavaScript implementation.
        var rng = new Mulberry32(0);
        uint first = rng.Next();
        Assert.Equal(1144304738u, first); // Math.imul-based reference, seed 0
        var rng2 = new Mulberry32(0xDEADBEEFu);
        Assert.Equal(new Mulberry32(0xDEADBEEFu).Next(), rng2.Next());
    }

    [Fact]
    public void DrawIsExactAndConsumesOnDrawOne()
    {
        var rng = new Mulberry32(7);
        uint before = rng.State;
        Assert.Equal(0u, rng.Draw(1));
        Assert.NotEqual(before, rng.State);
        for (int i = 0; i < 1000; i++) Assert.InRange(rng.Draw(5), 0u, 4u);
    }

    [Fact]
    public void Fnv1aMatchesKnownVectors()
    {
        Assert.Equal(0x811c9dc5u, Fnv1a.Hash(string.Empty));
        Assert.Equal(0xe40c292cu, Fnv1a.Hash("a"));
        Assert.Equal("0xe40c292c", Fnv1a.Format(Fnv1a.Hash("a")));
    }
}
