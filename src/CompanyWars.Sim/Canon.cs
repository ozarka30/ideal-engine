using System.Text;

namespace CompanyWars.Sim;

/// <summary>Canonical JSON writing for the hashes in SIMULATION_SPEC.md §16.4. No whitespace, decimal integers, JSON-escaped strings.</summary>
internal static class Canon
{
    public static void Str(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
    }

    public static void Key(StringBuilder sb, string key)
    {
        Str(sb, key);
        sb.Append(':');
    }

    public static void Field(StringBuilder sb, string key, long value)
    {
        Key(sb, key);
        sb.Append(value);
    }

    public static void Field(StringBuilder sb, string key, bool value)
    {
        Key(sb, key);
        sb.Append(value ? "true" : "false");
    }

    public static void Field(StringBuilder sb, string key, string value)
    {
        Key(sb, key);
        Str(sb, value);
    }

    public static void Field(StringBuilder sb, string key, long[] values)
    {
        Key(sb, key);
        sb.Append('[');
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(values[i]);
        }
        sb.Append(']');
    }

    public static void Field(StringBuilder sb, string key, string[] values)
    {
        Key(sb, key);
        sb.Append('[');
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            Str(sb, values[i]);
        }
        sb.Append(']');
    }
}
