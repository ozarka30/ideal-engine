namespace CompanyWars.Sim;

/// <summary>Integer arithmetic helpers (SIMULATION_SPEC.md §2). Exact integer quotients with mathematical floor.</summary>
internal static class Arith
{
    public static long FloorDiv(long a, long b)
    {
        long q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
        return q;
    }

    /// <summary><c>floor(v * m / 1000)</c>.</summary>
    public static long Permille(long v, long m) => FloorDiv(v * m, 1000);

    public static long Clamp(long v, long lo, long hi) => System.Math.Max(lo, System.Math.Min(hi, v));

    public static long Min(long a, long b) => a < b ? a : b;

    public static long Max(long a, long b) => a > b ? a : b;
}
