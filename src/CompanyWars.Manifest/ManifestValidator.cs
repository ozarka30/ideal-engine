using System.Text.Json;
using System.Text.Json.Nodes;
using CompanyWars.Content;
using CompanyWars.Sim;
using Json.Schema;

namespace CompanyWars.Manifest;

public sealed record CoverageBucket(long Gated, long WithArt);

public sealed record Coverage(
    string ContentVersion,
    long ManifestVersion,
    long Total,
    long Gated,
    long WithArt,
    long Invalid,
    SortedDictionary<string, CoverageBucket> ByTier,
    SortedDictionary<string, CoverageBucket> ByScreen,
    SortedDictionary<string, CoverageBucket> ByKind,
    long VerifyPending);

public sealed class ManifestReport
{
    public required SpriteManifest Manifest { get; init; }
    public required GreyboxPalette Palette { get; init; }
    public required List<string> Errors { get; init; }
    public required Coverage Coverage { get; init; }
    public required HashSet<string> Present { get; init; }

    public string CoverageJson() => JsonSerializer.Serialize(Coverage, ManifestJson.Indented) + "\n";
}

/// <summary>The validation check of ART_PIPELINE.md §5. Absence of an asset file is valid; a wrong size on a present one is not.</summary>
public static class ManifestValidator
{
    public const string SchemaId = "https://companywars.invalid/schema/manifest.schema.json";

    private static readonly string[] ExteriorScreens = { "battle", "map", "menu" };
    private static readonly string[] InteriorScreens = { "build", "shop", "battle.inset", "autopsy", "reward", "codex" };

    public static SpriteManifest Load(string repoRoot)
    {
        string path = Path.Combine(repoRoot, "manifest", "sprites.json");
        return JsonSerializer.Deserialize<SpriteManifest>(File.ReadAllText(path), ManifestJson.Options) ?? throw new InvalidOperationException("empty manifest");
    }

    public static GreyboxPalette LoadPalette(string repoRoot, SpriteManifest manifest)
    {
        string path = Path.Combine(repoRoot, manifest.Palette);
        return JsonSerializer.Deserialize<GreyboxPalette>(File.ReadAllText(path), ManifestJson.Options) ?? throw new InvalidOperationException("empty palette");
    }

    public static ManifestReport Validate(string repoRoot, ContentDb content)
    {
        var errors = new List<string>();
        string manifestPath = Path.Combine(repoRoot, "manifest", "sprites.json");

        // 1. Schema
        JsonSchema schema = SchemaCache.Load(Path.Combine(repoRoot, "schema", "manifest.schema.json"), SchemaId);
        JsonNode node = JsonNode.Parse(File.ReadAllText(manifestPath)) ?? throw new InvalidOperationException("empty manifest");
        EvaluationResults result = schema.Evaluate(JsonSerializer.SerializeToElement(node), new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            foreach (EvaluationResults d in result.Details ?? new List<EvaluationResults>())
            {
                if (d.IsValid || d.Errors == null) continue;
                foreach (KeyValuePair<string, string> e in d.Errors) errors.Add($"schema: {d.InstanceLocation} {e.Key}: {e.Value}");
            }
        }

        SpriteManifest manifest = Load(repoRoot);
        GreyboxPalette palette = LoadPalette(repoRoot, manifest);
        if (manifest.ContentVersion != content.ContentVersion) errors.Add($"manifest contentVersion {manifest.ContentVersion} does not match content {content.ContentVersion}");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var assets = new HashSet<string>(StringComparer.Ordinal);
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (ManifestEntry e in manifest.Entries)
        {
            // 4. Uniqueness
            if (!ids.Add(e.Id)) errors.Add($"{e.Id}: duplicate id");
            if (!assets.Add(e.Sprite.Asset)) errors.Add($"{e.Id}: duplicate asset path {e.Sprite.Asset}");
            if (!palette.Tones.ContainsKey(e.Category)) errors.Add($"{e.Id}: category {e.Category} is not a palette tone");

            // 2. Derived fields
            Overhang? derived = ManifestMath.DeriveOverhang(e, manifest.TileSize);
            if (derived != e.Overhang) errors.Add($"{e.Id}: stored overhang {Describe(e.Overhang)} differs from derived {Describe(derived)}");

            // 5. Perspective
            if (e.Perspective == "topdown" && e.Screens.Any(s => ExteriorScreens.Contains(s))) errors.Add($"{e.Id}: topdown entry lists an exterior screen");
            if (e.Perspective == "exterior" && e.Screens.Any(s => InteriorScreens.Contains(s))) errors.Add($"{e.Id}: exterior entry lists an interior screen");

            // 6. Dimensions of present files
            string file = Path.Combine(repoRoot, e.Sprite.Asset);
            if (File.Exists(file))
            {
                present.Add(e.Id);
                if (!PngHeader.TryReadSize(file, out long w, out long h))
                {
                    errors.Add($"{e.Id}: {e.Sprite.Asset} is not a readable PNG");
                }
                else if (e.Sprite.SourceRect != null)
                {
                    long[] r = e.Sprite.SourceRect;
                    if (r[2] != e.Sprite.W || r[3] != e.Sprite.H) errors.Add($"{e.Id}: sourceRect {r[2]}x{r[3]} does not match sprite {e.Sprite.W}x{e.Sprite.H}");
                    if (r[0] + r[2] > w || r[1] + r[3] > h) errors.Add($"{e.Id}: sourceRect leaves the {w}x{h} image");
                }
                else if (e.Sprite.Frames != null && e.Sprite.Frames.Count > 0)
                {
                    long needW = 0, needH = 0;
                    foreach (long[] f in e.Sprite.Frames.Values)
                    {
                        long count = f.Length > 2 ? f[2] : 1;
                        needW = Math.Max(needW, (f[0] + count) * e.Sprite.W);
                        needH = Math.Max(needH, (f[1] + 1) * e.Sprite.H);
                    }
                    if (w < needW || h < needH) errors.Add($"{e.Id}: {e.Sprite.Asset} is {w}x{h}, frames need at least {needW}x{needH}");
                }
                else if (e.Perspective == "ui")
                {
                    // Chrome is not pixel art (D-76): it may ship at any integer multiple of its
                    // declared size and is drawn down into that rect with a smooth filter.
                    if (w % e.Sprite.W != 0 || h % e.Sprite.H != 0 || w / e.Sprite.W != h / e.Sprite.H)
                    {
                        errors.Add($"{e.Id}: {e.Sprite.Asset} is {w}x{h}, not an integer multiple of {e.Sprite.W}x{e.Sprite.H}");
                    }
                }
                else if (w != e.Sprite.W || h != e.Sprite.H)
                {
                    errors.Add($"{e.Id}: {e.Sprite.Asset} is {w}x{h}, manifest says {e.Sprite.W}x{e.Sprite.H}");
                }
            }
        }

        // 3. References from content
        foreach ((string owner, string reference) in ContentReferences(content))
        {
            if (!ids.Contains(reference)) errors.Add($"{owner}: {reference} has no manifest entry");
        }

        return new ManifestReport
        {
            Manifest = manifest,
            Palette = palette,
            Errors = errors,
            Coverage = ComputeCoverage(manifest, present, errors.Count),
            Present = present,
        };
    }

    public static IEnumerable<(string Owner, string Reference)> ContentReferences(ContentDb content)
    {
        foreach (EmployeeDef e in content.Employees) yield return (e.Id, e.Sprite);
        foreach (RoomDef r in content.Rooms) yield return (r.Id, r.Tile);
        foreach (FurnitureDef f in content.Furniture) yield return (f.Id, f.Sprite);
        foreach (FounderDef f in content.Founders)
        {
            yield return (f.Id, f.Portrait);
            yield return (f.Id, f.Badge);
        }
        foreach (KeyValuePair<string, NodeKind> kv in content.Map.NodeKinds) yield return ("map." + kv.Key, kv.Value.Icon);
    }

    private static Coverage ComputeCoverage(SpriteManifest m, HashSet<string> present, long invalid)
    {
        var byTier = new SortedDictionary<string, CoverageBucket>(StringComparer.Ordinal);
        var byScreen = new SortedDictionary<string, CoverageBucket>(StringComparer.Ordinal);
        var byKind = new SortedDictionary<string, CoverageBucket>(StringComparer.Ordinal);
        long gated = 0, withArt = 0, verifyPending = 0;
        void Bump(SortedDictionary<string, CoverageBucket> d, string key, bool art)
        {
            d.TryGetValue(key, out CoverageBucket? b);
            b ??= new CoverageBucket(0, 0);
            d[key] = new CoverageBucket(b.Gated + 1, b.WithArt + (art ? 1 : 0));
        }
        foreach (ManifestEntry e in m.Entries)
        {
            if (e.Verify) verifyPending++;
            if (!e.ReleaseGate) continue;
            bool art = present.Contains(e.Id);
            gated++;
            if (art) withArt++;
            Bump(byTier, e.Visibility.ToString(), art);
            Bump(byKind, e.Kind, art);
            foreach (string s in e.Screens) Bump(byScreen, s, art);
        }
        return new Coverage(m.ContentVersion, m.ManifestVersion, m.Entries.Length, gated, withArt, invalid, byTier, byScreen, byKind, verifyPending);
    }

    private static string Describe(Overhang? o) => o == null ? "null" : $"[{o.Left},{o.Top},{o.Right},{o.Bottom}]";
}
