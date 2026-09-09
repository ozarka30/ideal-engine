namespace CompanyWars.Sim;

/// <summary>
/// The match generator (SIMULATION_SPEC.md §17). One per match, seeded once.
/// This listing is normative (D-61): all arithmetic unchecked, all values uint.
/// Only <c>random_floor</c> and <c>random</c> selectors may call <see cref="Draw"/>.
/// </summary>
public sealed class Mulberry32
{
    private uint _state;

    public Mulberry32(uint seed)
    {
        _state = seed;
    }

    /// <summary>The generator's 32-bit state after the last draw (the seed if none).</summary>
    public uint State => _state;

    /// <summary>The next 32-bit value.</summary>
    public uint Next()
    {
        unchecked
        {
            _state += 0x6D2B79F5u;
            uint t = _state;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return t ^ (t >> 14);
        }
    }

    /// <summary>A value in 0..n-1. Exact; no rejection sampling. Consumes one value even when n is 1.</summary>
    public uint Draw(uint n)
    {
        unchecked
        {
            return (uint)(((ulong)Next() * n) >> 32);
        }
    }
}
