namespace CompanyWars.Tools;

/// <summary>Stage 6 of ARCHITECTURE.md §9.2: every committed screenshot fixture must be byte-identical to the rendered one.</summary>
public static class Screenshots
{
    public static int Compare(string root)
    {
        string expectedDir = Path.Combine(root, "game", "__screenshots__");
        string actualDir = Path.Combine(expectedDir, "actual");
        int failures = 0;
        string[] expected = Directory.GetFiles(expectedDir, "*.png");
        if (expected.Length == 0)
        {
            Console.Error.WriteLine("no screenshot fixtures committed under game/__screenshots__");
            return 1;
        }
        foreach (string e in expected.OrderBy(p => p, StringComparer.Ordinal))
        {
            string a = Path.Combine(actualDir, Path.GetFileName(e));
            if (!File.Exists(a))
            {
                failures++;
                Console.WriteLine($"{Path.GetFileName(e)}: not rendered");
                continue;
            }
            bool same = File.ReadAllBytes(e).AsSpan().SequenceEqual(File.ReadAllBytes(a));
            Console.WriteLine($"{Path.GetFileName(e)}: {(same ? "ok" : "DIFFERS")}");
            if (!same) failures++;
        }
        return failures == 0 ? 0 : 1;
    }
}
