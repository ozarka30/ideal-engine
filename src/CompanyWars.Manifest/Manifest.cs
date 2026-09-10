using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CompanyWars.Manifest;

// The sprite manifest (ART_PIPELINE.md §1). Records mirror schema/manifest.schema.json.

public sealed record SpriteManifest(
    long ManifestVersion,
    string ContentVersion,
    long TileSize,
    Canvas Canvas,
    long[] Scales,
    string AssetRoot,
    string Palette,
    Dictionary<string, string> VisibilityTiers,
    ManifestEntry[] Entries);

public sealed record Canvas(long W, long H);

public sealed record ManifestEntry(
    string Id,
    string Kind,
    string Category,
    string Label,
    FootprintTiles? Footprint,
    SpriteSpec Sprite,
    long SortBias,
    string Perspective,
    string[] Screens,
    long Visibility,
    string Reads,
    string? CandidateSource,
    bool Verify,
    bool ReleaseGate,
    Layout? Layout,
    string? Note,
    Overhang? Overhang);

public sealed record FootprintTiles(long W, long H);

public sealed record SpriteSpec(long W, long H, Anchor Anchor, string Asset, long[]? SourceRect, Dictionary<string, long[]>? Frames);

public sealed record Anchor(double X, double Y);

public sealed record Layout(long X, long Y);

public sealed record Overhang(long Left, long Top, long Right, long Bottom);

public sealed record GreyboxPalette(string Id, string Note, Dictionary<string, Tone> Tones, PaletteGround Ground, PaletteRendering Rendering);

/// <summary>What every screen sits on, outside the seven tones: Deep Slate Olive from the scheme.</summary>
public sealed record PaletteGround(string Backdrop, string Source);

public sealed record Tone(string Fill, string Border, string Hatch, string Text, string Source);

public sealed record PaletteRendering(long FootprintAlpha, long OverhangAlpha, long BorderPx, long HatchPitchPx, string LabelFont, string[] LabelFallback, long DotSize);

public static class ManifestJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.Strict,
    };

    public static JsonSerializerOptions Indented { get; } = new(Options) { WriteIndented = true };
}

public static class ManifestMath
{
    /// <summary>Derives the overhang from footprint, sprite and anchor (ART_PIPELINE.md §2.1). Null for non-grid kinds.</summary>
    public static Overhang? DeriveOverhang(ManifestEntry e, long tileSize)
    {
        if (e.Footprint == null) return null;
        long tw = e.Footprint.W * tileSize;
        long th = e.Footprint.H * tileSize;
        // ax * tw - ax * sw with exact rational arithmetic in thousandths: anchors are 0, 0.5 or 1 in practice.
        long ax = (long)Math.Round(e.Sprite.Anchor.X * 1000);
        long ay = (long)Math.Round(e.Sprite.Anchor.Y * 1000);
        long sx = FloorDiv(ax * tw - ax * e.Sprite.W, 1000);
        long sy = FloorDiv(ay * th - ay * e.Sprite.H, 1000);
        return new Overhang(
            Math.Max(0, -sx),
            Math.Max(0, -sy),
            Math.Max(0, sx + e.Sprite.W - tw),
            Math.Max(0, sy + e.Sprite.H - th));
    }

    private static long FloorDiv(long a, long b)
    {
        long q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
        return q;
    }

    /// <summary>The five-key draw order (ART_PIPELINE.md §3).</summary>
    public static int CompareDrawOrder((long FloorIndex, long AnchorTileRow, long SortBias, long TileCol, string Id) a, (long FloorIndex, long AnchorTileRow, long SortBias, long TileCol, string Id) b)
    {
        int c = a.FloorIndex.CompareTo(b.FloorIndex);
        if (c != 0) return c;
        c = a.AnchorTileRow.CompareTo(b.AnchorTileRow);
        if (c != 0) return c;
        c = a.SortBias.CompareTo(b.SortBias);
        if (c != 0) return c;
        c = a.TileCol.CompareTo(b.TileCol);
        if (c != 0) return c;
        return string.CompareOrdinal(a.Id, b.Id);
    }
}

/// <summary>Reads the IHDR of a PNG without decoding it.</summary>
public static class PngHeader
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static bool TryReadSize(string path, out long width, out long height)
    {
        width = 0;
        height = 0;
        try
        {
            using FileStream s = File.OpenRead(path);
            var buf = new byte[24];
            if (s.Read(buf, 0, 24) < 24) return false;
            for (int i = 0; i < 8; i++)
            {
                if (buf[i] != Signature[i]) return false;
            }
            if (buf[12] != (byte)'I' || buf[13] != (byte)'H' || buf[14] != (byte)'D' || buf[15] != (byte)'R') return false;
            width = ((long)buf[16] << 24) | ((long)buf[17] << 16) | ((long)buf[18] << 8) | buf[19];
            height = ((long)buf[20] << 24) | ((long)buf[21] << 16) | ((long)buf[22] << 8) | buf[23];
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
