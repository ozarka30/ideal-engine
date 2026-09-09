using System.Text;

namespace CompanyWars.Sim;

/// <summary>FNV-1a, 32-bit, over UTF-8 bytes (SIMULATION_SPEC.md §16.3, §16.4).</summary>
public static class Fnv1a
{
    private const uint Offset = 2166136261u;
    private const uint Prime = 16777619u;

    public static uint Hash(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        uint h = Offset;
        unchecked
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                h *= Prime;
            }
        }
        return h;
    }

    /// <summary>"0x" plus eight lowercase hex digits.</summary>
    public static string Format(uint hash) => "0x" + hash.ToString("x8");
}
