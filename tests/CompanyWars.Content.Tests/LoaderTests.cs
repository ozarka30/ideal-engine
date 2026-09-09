using System.Text.Json;
using System.Text.Json.Nodes;
using CompanyWars.Sim;
using CompanyWars.Tools;
using Xunit;

namespace CompanyWars.Content.Tests;

public class LoaderTests
{
    private static readonly string Root = RepoRoot.Find(AppContext.BaseDirectory);

    [Fact]
    public void CommittedContentLoadsAndValidates()
    {
        ContentDb db = ContentLoader.Load(Root);
        Assert.Equal(40, db.Employees.Length);
        Assert.Equal(16, db.Rooms.Length);
        Assert.Equal(14, db.Furniture.Length);
        Assert.Equal(40, db.Recipes.Length);
        Assert.Equal(12, db.Riders.Length);
        Assert.Equal(17, db.Modifiers.Length);
        Assert.Equal(8, db.Founders.Length);
        Assert.Equal(6, db.Templates.Templates.Length);
        Assert.Equal(9, db.ScriptedRivals.Length);
    }

    [Fact]
    public void EveryContentFileRoundTripsThroughTheRecords()
    {
        // ARCHITECTURE.md §6: a schema change without a matching record change fails the build.
        ContentDb db = ContentLoader.Load(Root);
        string contentDir = Path.Combine(Root, "content");
        foreach (IndexFile f in db.Index.Files)
        {
            JsonNode original = ContentLoader.ParseFile(Path.Combine(contentDir, f.Path));
            Type type = TypeFor(f.Def);
            object record = original.Deserialize(type, ContentJson.Options)!;
            JsonNode roundTripped = JsonSerializer.SerializeToNode(record, type, ContentJson.Options)!;
            Assert.True(JsonNode.DeepEquals(ContentJson.StripNulls(original), ContentJson.StripNulls(roundTripped)), $"{f.Path} does not round-trip through {type.Name}");
        }
    }

    [Fact]
    public void RuleSetMatchesTheSpecConstants()
    {
        RuleSet r = ContentLoader.Load(Root).RuleSetFor(8);
        Assert.Equal(1400, r.GoodwillBase(8));
        Assert.Equal(new long[] { 0, 400, 800, 1160 }, r.MonthStart);
        Assert.Equal(900, r.FloorMultFor(0));
        Assert.Equal(1450, r.FloorMultFor(3));
        Assert.Equal(-1, r.FloorIndexOf("floor.b1"));
        Assert.Equal(5, r.BurnoutMax);
        Assert.Equal(50, r.BurnoutPushPenaltyPermille);
        Assert.Equal(60, r.OvertimeDuration);
        Assert.Equal(200, r.BureaucracyRatePermille);
        Assert.Equal(0, r.Month(399));
        Assert.Equal(1, r.Month(400));
        Assert.Equal(3, r.Month(1199));
    }

    [Fact]
    public void SimContentTableResolvesEveryDefinitionKind()
    {
        ContentTable t = ContentLoader.Load(Root).ToContentTable();
        Assert.True(t.Employees.ContainsKey("emp.junior_dev"));
        Assert.True(t.Rooms.ContainsKey("room.reception"));
        Assert.True(t.Furniture.ContainsKey("furn.whiteboard"));
        Assert.True(t.Statuses.ContainsKey(Vocabulary.StatusFrozen));
        Assert.True(t.Founders.ContainsKey("founder.sato"));
    }

    [Theory]
    [MemberData(nameof(RejectionCases))]
    public void EachRejectionFixtureIsRejectedForTheRightReason(string name)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Rejections", name);
        string expected = File.ReadAllText(Path.Combine(dir, "expect.txt")).Trim();
        string scratch = RejectionRoot.Build(Root, Path.Combine(dir, "patch.json"));
        try
        {
            ContentException ex = Assert.Throws<ContentException>(() => ContentLoader.Load(scratch));
            Assert.True(ex.Message.Contains(expected, StringComparison.Ordinal), $"{name}: expected an error containing '{expected}' but got:\n{ex.Message}");
        }
        finally
        {
            Directory.Delete(scratch, recursive: true);
        }
    }

    public static IEnumerable<object[]> RejectionCases()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Rejections");
        foreach (string d in Directory.GetDirectories(dir).OrderBy(x => x, StringComparer.Ordinal)) yield return new object[] { Path.GetFileName(d) };
    }

    private static Type TypeFor(string def) => def switch
    {
        "RuleSet" => typeof(RulesFile),
        "Economy" => typeof(EconomyFile),
        "FloorFile" => typeof(FloorFile),
        "StatusFile" => typeof(StatusFile),
        "EmployeeFile" => typeof(EmployeeFile),
        "RoomFile" => typeof(RoomFile),
        "FurnitureFile" => typeof(FurnitureFile),
        "RecipeFile" => typeof(RecipeFile),
        "RiderFile" => typeof(RiderFile),
        "ModifierFile" => typeof(ModifierFile),
        "Shop" => typeof(ShopFile),
        "ModeFile" => typeof(ModeFile),
        "CampaignMap" => typeof(CampaignMap),
        "Tutorial" => typeof(TutorialFile),
        "FounderFile" => typeof(FounderFile),
        "Balance" => typeof(BalanceFile),
        "TemplateFile" => typeof(TemplateFile),
        "ScriptedRival" => typeof(ScriptedRival),
        _ => throw new InvalidOperationException("unknown def " + def),
    };
}

/// <summary>Builds a scratch content root from the committed one with a rejection patch applied.</summary>
public static class RejectionRoot
{
    public static string Build(string root, string patchPath)
    {
        string scratch = Path.Combine(Path.GetTempPath(), "cw-content-" + Guid.NewGuid().ToString("N"));
        CopyDir(Path.Combine(root, "content"), Path.Combine(scratch, "content"));
        CopyDir(Path.Combine(root, "schema"), Path.Combine(scratch, "schema"));
        JsonObject patch = (JsonObject)JsonNode.Parse(File.ReadAllText(patchPath))!;
        string file = Path.Combine(scratch, "content", patch["file"]!.GetValue<string>());
        JsonNode doc = JsonNode.Parse(File.ReadAllText(file))!;
        foreach (JsonNode? opNode in (JsonArray)patch["ops"]!)
        {
            JsonObject op = (JsonObject)opNode!;
            string[] path = op["path"]!.GetValue<string>().TrimStart('/').Split('/');
            JsonNode parent = Resolve(doc, path[..^1]);
            string last = path[^1];
            if (op.ContainsKey("append"))
            {
                ((JsonArray)parent).Add(op["append"]?.DeepClone());
            }
            else if (op.ContainsKey("delete"))
            {
                if (parent is JsonObject o) o.Remove(last); else ((JsonArray)parent).RemoveAt(int.Parse(last));
            }
            else
            {
                JsonNode? value = op["set"]?.DeepClone();
                if (parent is JsonObject o) o[last] = value; else ((JsonArray)parent)[int.Parse(last)] = value;
            }
        }
        File.WriteAllText(file, doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return scratch;
    }

    /// <summary>JSON-pointer-like navigation; an array segment of the form <c>@id</c> selects the element whose <c>id</c> matches.</summary>
    private static JsonNode Resolve(JsonNode node, string[] path)
    {
        JsonNode cur = node;
        foreach (string seg in path)
        {
            if (cur is JsonArray arr)
            {
                if (seg.StartsWith('@'))
                {
                    string id = seg[1..];
                    cur = arr.First(x => x!["id"]!.GetValue<string>() == id)!;
                }
                else
                {
                    cur = arr[int.Parse(seg)]!;
                }
            }
            else
            {
                cur = cur[seg]!;
            }
        }
        return cur;
    }

    private static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        foreach (string d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }
}
