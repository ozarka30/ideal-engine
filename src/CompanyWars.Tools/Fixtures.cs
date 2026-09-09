using System.Text.Json;
using System.Text.Json.Nodes;
using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Tools;

/// <summary>One conformance fixture (SIMULATION_SPEC.md §19): the inputs and the recorded result.</summary>
public sealed record SimFixture(string Name, string Description, uint Seed, long Round, TowerSnapshot SnapshotA, TowerSnapshot SnapshotB, EmployeeDef[]? ContentOverlay, MatchResult Expected);

public static class Fixtures
{
    public static int Run(string[] args)
    {
        string cmd = args.Length > 0 ? args[0] : "check";
        string root = args.Length > 1 ? args[1] : RepoRoot.Find();
        ContentDb db = ContentLoader.Load(root);
        string dir = Path.Combine(root, "fixtures", "sim");
        switch (cmd)
        {
            case "record":
                {
                    Directory.CreateDirectory(dir);
                    foreach (FixtureInput input in FixtureCatalog.All(db))
                    {
                        var errors = new List<string>();
                        ContentValidator.SnapshotStructure(db, input.SnapshotA, input.Name + ".A", errors);
                        ContentValidator.SnapshotStructure(db, input.SnapshotB, input.Name + ".B", errors);
                        errors.RemoveAll(e => input.ContentOverlay != null && e.Contains("unknown employee", StringComparison.Ordinal));
                        if (errors.Count > 0) throw new ContentException(errors);
                        MatchResult result = Simulate(db, input);
                        var fixture = new SimFixture(input.Name, input.Description, input.Seed, input.Round, input.SnapshotA, input.SnapshotB, input.ContentOverlay, result);
                        File.WriteAllText(Path.Combine(dir, input.Name + ".json"), JsonSerializer.Serialize(fixture, ContentJson.Indented) + "\n");
                        Console.WriteLine($"{input.Name}: {result.Winner} at {result.EndTick}, {result.Entries.Length} entries, {result.StateHash}");
                    }
                    return 0;
                }
            case "check":
                {
                    int failures = 0;
                    foreach (string path in Directory.GetFiles(dir, "*.json").OrderBy(p => p, StringComparer.Ordinal))
                    {
                        SimFixture fixture = Load(path);
                        MatchResult actual = Simulate(db, new FixtureInput(fixture.Name, fixture.Description, fixture.Seed, fixture.Round, fixture.SnapshotA, fixture.SnapshotB, fixture.ContentOverlay));
                        string? diff = Compare(fixture.Expected, actual);
                        if (diff == null)
                        {
                            Console.WriteLine($"{fixture.Name}: ok {actual.StateHash}");
                        }
                        else
                        {
                            failures++;
                            Console.WriteLine($"{fixture.Name}: MISMATCH {diff}");
                        }
                    }
                    return failures == 0 ? 0 : 1;
                }
            default:
                Console.Error.WriteLine("usage: fixtures <record|check> [repoRoot]");
                return 2;
        }
    }

    public static SimFixture Load(string path)
    {
        return JsonSerializer.Deserialize<SimFixture>(File.ReadAllText(path), ContentJson.Options) ?? throw new InvalidOperationException("bad fixture " + path);
    }

    public static MatchResult Simulate(ContentDb db, FixtureInput input)
    {
        ContentTable table = db.ToContentTable();
        if (input.ContentOverlay != null && input.ContentOverlay.Length > 0)
        {
            var employees = new Dictionary<string, EmployeeDef>(table.Employees, StringComparer.Ordinal);
            foreach (EmployeeDef e in input.ContentOverlay) employees[e.Id] = e;
            table = new ContentTable(table.ContentVersion, employees, table.Rooms, table.Furniture, table.Riders, table.Modifiers, table.Founders, table.Statuses, table.Floors);
        }
        return Simulator.Simulate(input.Seed, input.SnapshotA, input.SnapshotB, db.RuleSetFor(input.Round), table);
    }

    /// <summary>Null when identical; otherwise a one-line description of the first difference.</summary>
    public static string? Compare(MatchResult expected, MatchResult actual)
    {
        if (expected.StateHash != actual.StateHash) return $"stateHash {expected.StateHash} vs {actual.StateHash}; " + FirstEntryDiff(expected, actual);
        string e = JsonSerializer.Serialize(expected, ContentJson.Options);
        string a = JsonSerializer.Serialize(actual, ContentJson.Options);
        if (e != a) return "same stateHash but different serialisation; " + FirstEntryDiff(expected, actual);
        return null;
    }

    private static string FirstEntryDiff(MatchResult expected, MatchResult actual)
    {
        int n = Math.Min(expected.Entries.Length, actual.Entries.Length);
        for (int i = 0; i < n; i++)
        {
            string e = JsonSerializer.Serialize(expected.Entries[i], ContentJson.Options);
            string a = JsonSerializer.Serialize(actual.Entries[i], ContentJson.Options);
            if (e != a) return $"entry {i} differs: expected {e} actual {a}";
        }
        if (expected.Entries.Length != actual.Entries.Length) return $"entry count {expected.Entries.Length} vs {actual.Entries.Length}";
        return $"header differs: winner {expected.Winner}/{actual.Winner} endTick {expected.EndTick}/{actual.EndTick} share {expected.FinalShare}/{actual.FinalShare}";
    }
}

public sealed record FixtureInput(string Name, string Description, uint Seed, long Round, TowerSnapshot SnapshotA, TowerSnapshot SnapshotB, EmployeeDef[]? ContentOverlay);
