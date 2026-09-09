using System.Text.Json;
using System.Text.Json.Nodes;
using CompanyWars.Sim;
using Json.Schema;

namespace CompanyWars.Content;

/// <summary>
/// Reads <c>content/index.json</c>, validates every file against its <c>$def</c>, runs the referential
/// checks in CONTENT_SCHEMA.md §12, and returns a frozen <see cref="ContentDb"/>.
/// </summary>
public static class ContentLoader
{
    public const string SchemaId = "https://companywars.invalid/schema/content.schema.json";

    /// <summary>Loads and fully validates. Throws <see cref="ContentException"/> listing every error found.</summary>
    public static ContentDb Load(string repoRoot)
    {
        var errors = new List<string>();
        ContentDb db = LoadFiles(repoRoot, errors);
        if (errors.Count > 0) throw new ContentException(errors);
        ContentValidator.Validate(db, errors);
        if (errors.Count > 0) throw new ContentException(errors);
        return db;
    }

    /// <summary>Loads without the referential checks; schema errors still throw. For tests of the validator itself.</summary>
    public static ContentDb LoadUnchecked(string repoRoot)
    {
        var errors = new List<string>();
        ContentDb db = LoadFiles(repoRoot, errors);
        if (errors.Count > 0) throw new ContentException(errors);
        return db;
    }

    private static ContentDb LoadFiles(string repoRoot, List<string> errors)
    {
        string contentDir = Path.Combine(repoRoot, "content");
        string indexPath = Path.Combine(contentDir, "index.json");
        JsonNode indexNode = ParseFile(indexPath);
        var schema = new SchemaSet(Path.Combine(repoRoot, "schema", "content.schema.json"));
        schema.Check("Index", indexNode, "index.json", errors);
        ContentIndex index = Deserialize<ContentIndex>(indexNode);

        var byDef = new Dictionary<string, List<(string Path, JsonNode Node)>>(StringComparer.Ordinal);
        foreach (IndexFile f in index.Files)
        {
            string path = Path.Combine(contentDir, f.Path);
            if (!File.Exists(path)) { errors.Add($"index.json names a missing file: {f.Path}"); continue; }
            JsonNode node = ParseFile(path);
            schema.Check(f.Def, node, f.Path, errors);
            if (!byDef.TryGetValue(f.Def, out List<(string, JsonNode)>? list)) byDef[f.Def] = list = new List<(string, JsonNode)>();
            list.Add((f.Path, node));
        }
        if (errors.Count > 0) throw new ContentException(errors);

        T One<T>(string def)
        {
            if (!byDef.TryGetValue(def, out List<(string Path, JsonNode Node)>? list) || list.Count != 1)
            {
                throw new ContentException($"index.json must name exactly one file with def {def}");
            }
            return Deserialize<T>(list[0].Node);
        }

        var rivals = new List<ScriptedRival>();
        if (byDef.TryGetValue("ScriptedRival", out List<(string Path, JsonNode Node)>? rivalNodes))
        {
            foreach ((string _, JsonNode node) in rivalNodes) rivals.Add(Deserialize<ScriptedRival>(node));
        }

        return new ContentDb
        {
            Index = index,
            Rules = One<RulesFile>("RuleSet"),
            Economy = One<EconomyFile>("Economy"),
            Floors = One<FloorFile>("FloorFile").Floors,
            Statuses = One<StatusFile>("StatusFile").Statuses,
            Employees = One<EmployeeFile>("EmployeeFile").Employees,
            Rooms = One<RoomFile>("RoomFile").Rooms,
            Furniture = One<FurnitureFile>("FurnitureFile").Furniture,
            Recipes = One<RecipeFile>("RecipeFile").Recipes,
            Riders = One<RiderFile>("RiderFile").Riders,
            Modifiers = One<ModifierFile>("ModifierFile").Modifiers,
            Shop = One<ShopFile>("Shop"),
            Modes = One<ModeFile>("ModeFile").Modes,
            Map = One<CampaignMap>("CampaignMap"),
            Tutorial = One<TutorialFile>("Tutorial"),
            Founders = One<FounderFile>("FounderFile").Founders,
            Balance = One<BalanceFile>("Balance"),
            Templates = One<TemplateFile>("TemplateFile"),
            ScriptedRivals = rivals.ToArray(),
        };
    }

    public static JsonNode ParseFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return JsonNode.Parse(stream, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow })
            ?? throw new ContentException($"{path}: empty document");
    }

    public static T Deserialize<T>(JsonNode node)
    {
        return node.Deserialize<T>(ContentJson.Options) ?? throw new ContentException($"could not deserialise {typeof(T).Name}");
    }

    /// <summary>The content schema with a per-<c>$def</c> entry point.</summary>
    public sealed class SchemaSet
    {
        private readonly JsonSchema _root;
        private readonly Dictionary<string, JsonSchema> _byDef = new(StringComparer.Ordinal);
        private readonly EvaluationOptions _options;

        public SchemaSet(string schemaPath)
        {
            _root = SchemaCache.Load(schemaPath, SchemaId);
            _options = new EvaluationOptions { OutputFormat = OutputFormat.List };
        }

        public bool Check(string def, JsonNode node, string label, List<string> errors)
        {
            if (!_byDef.TryGetValue(def, out JsonSchema? s))
            {
                s = JsonSchema.FromText("{\"$ref\":\"" + SchemaId + "#/$defs/" + def + "\"}");
                _byDef[def] = s;
            }
            EvaluationResults result = s.Evaluate(JsonSerializer.SerializeToElement(node), _options);
            if (result.IsValid) return true;
            int count = 0;
            foreach (EvaluationResults d in result.Details ?? new List<EvaluationResults>())
            {
                if (d.IsValid || d.Errors == null) continue;
                foreach (KeyValuePair<string, string> e in d.Errors)
                {
                    errors.Add($"{label}: {d.InstanceLocation} {e.Key}: {e.Value}");
                    if (++count >= 20) { errors.Add($"{label}: further schema errors omitted"); return false; }
                }
            }
            if (count == 0) errors.Add($"{label}: does not validate against {def}");
            return false;
        }
    }
}
