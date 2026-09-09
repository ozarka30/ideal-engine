using System;
using System.Collections.Generic;
using System.IO;
using CompanyWars.Content;
using CompanyWars.Manifest;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// M0's one screen: the greybox renderer drawing one manifest entry as a labelled rectangle at its exact size,
/// in its palette tone, on the 640x360 logical canvas (ART_PIPELINE.md §4, ARCHITECTURE.md §7).
/// In screenshot mode the router captures it after three frames (game/__screenshots__/greybox_*.png).
/// </summary>
public partial class GreyboxScreen : Node2D
{
    private const string EntryId = "emp.junior_dev";

    private ManifestReport? _report;
    private ManifestEntry? _entry;
    private GreyboxTextures? _textures;
    private string _status = string.Empty;
    private int _frames;
    private bool _screenshots;
    private bool _captured;

    public override void _Ready()
    {
        ScreenRouter r = ScreenRouter.Instance;
        _report = r.Manifest;
        _textures = r.Textures;
        _entry = Array.Find(_report.Manifest.Entries, e => e.Id == EntryId) ?? throw new InvalidOperationException(EntryId + " is not in the manifest");
        _status = $"content {r.Content.ContentVersion} · manifest {_report.Manifest.ManifestVersion} · {_report.Coverage.WithArt}/{_report.Coverage.Gated} with art";
        _screenshots = r.ScreenshotMode;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_entry == null || _textures == null) return;
        // One entry, drawn once at 1x in the tile it occupies and once at exactly 2x beside it. Every
        // position is an integer logical pixel; the manifest supplies every size.
        var list = new RenderList();
        list.Add(floorIndex: 1, anchorTileRow: 1, entry: _entry, x: 64, y: 96, scale: 1);
        list.Add(floorIndex: 1, anchorTileRow: 1, entry: _entry, x: 160, y: 96, scale: 2);
        foreach (RenderList.Item item in list.Sorted())
        {
            Texture2D tex = _textures.For(item.Entry);
            var rect = new Rect2(item.X, item.Y, item.Entry.Sprite.W * item.Scale, item.Entry.Sprite.H * item.Scale);
            DrawTextureRect(tex, rect, tile: false);
        }
        DrawString(ThemeDB.FallbackFont, new Vector2(8, 350), _status, HorizontalAlignment.Left, -1, 8, new Color(0.91f, 0.9f, 0.88f));
    }

    public override void _Process(double delta)
    {
        if (!_screenshots) return;
        _frames++;
        if (_frames < 3 || _captured) return;
        _captured = true;
        ScreenRouter.Instance.Capture();
    }
}

/// <summary>The manifest's pixel rules, asserted at start-up (ART_PIPELINE.md §9).</summary>
public static class PixelDiscipline
{
    public static void Assert()
    {
        Check(ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32() == 640, "viewport width must be 640");
        Check(ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32() == 360, "viewport height must be 360");
        // canvas_items, not viewport (D-67): sprites still scale by the integer factor with nearest filtering, and text
        // renders at the window's resolution so the vector faces are smooth rather than blocky.
        Check(ProjectSettings.GetSetting("display/window/stretch/mode").AsString() == "canvas_items", "stretch mode must be canvas_items");
        Check(ProjectSettings.GetSetting("display/window/stretch/scale_mode").AsString() == "integer", "stretch scale must be integer");
        Check(ProjectSettings.GetSetting("rendering/textures/canvas_textures/default_texture_filter").AsInt32() == 0, "texture filter must be nearest");
        Check(ProjectSettings.GetSetting("rendering/2d/snap/snap_2d_transforms_to_pixel").AsBool(), "2D transforms must snap to pixels");
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException("pixel discipline: " + message);
    }
}

/// <summary>Every visible manifest entry as (sortKey, entry, x, y), drawn in the five-key total order (ART_PIPELINE.md §3).</summary>
public sealed class RenderList
{
    public readonly record struct Item(long FloorIndex, long AnchorTileRow, long SortBias, long TileCol, string Id, ManifestEntry Entry, int X, int Y, int Scale);

    private readonly List<Item> _items = new();

    public void Add(long floorIndex, long anchorTileRow, ManifestEntry entry, int x, int y, int scale)
    {
        _items.Add(new Item(floorIndex, anchorTileRow, entry.SortBias, x / 32, entry.Id, entry, x, y, scale));
    }

    public IEnumerable<Item> Sorted()
    {
        var sorted = new List<Item>(_items);
        sorted.Sort((a, b) => ManifestMath.CompareDrawOrder((a.FloorIndex, a.AnchorTileRow, a.SortBias, a.TileCol, a.Id), (b.FloorIndex, b.AnchorTileRow, b.SortBias, b.TileCol, b.Id)));
        return sorted;
    }
}

/// <summary>
/// Per manifest entry, either the file at <c>sprite.asset</c> or a synthesised placeholder at the declared size
/// (ART_PIPELINE.md §4.1): footprint fill, hatched overhang, 1px border, anchor cross. Labels wait for the pixel font.
/// </summary>
public sealed class GreyboxTextures
{
    private readonly string _root;
    private readonly ManifestReport _report;
    private readonly Dictionary<string, Texture2D> _cache = new();

    public GreyboxTextures(string root, ManifestReport report)
    {
        _root = root;
        _report = report;
    }

    public Texture2D For(ManifestEntry e)
    {
        if (_cache.TryGetValue(e.Id, out Texture2D? cached)) return cached;
        Texture2D tex = _report.Present.Contains(e.Id) ? LoadFile(e) : Placeholder(e);
        _cache[e.Id] = tex;
        return tex;
    }

    private Texture2D LoadFile(ManifestEntry e)
    {
        var img = new Image();
        Error err = img.Load(Path.Combine(_root, e.Sprite.Asset));
        if (err != Error.Ok) throw new InvalidOperationException($"{e.Id}: could not load {e.Sprite.Asset}");
        return ImageTexture.CreateFromImage(img);
    }

    private Texture2D Placeholder(ManifestEntry e)
    {
        Tone tone = _report.Palette.Tones.TryGetValue(e.Category, out Tone? t) ? t : _report.Palette.Tones["invalid"];
        PaletteRendering r = _report.Palette.Rendering;
        int w = (int)e.Sprite.W;
        int h = (int)e.Sprite.H;
        var img = Image.CreateEmpty(Math.Max(1, w), Math.Max(1, h), false, Image.Format.Rgba8);
        Color fill = Color.FromHtml(tone.Fill);
        Color border = Color.FromHtml(tone.Border);
        Color hatch = Color.FromHtml(tone.Hatch);
        Color text = Color.FromHtml(tone.Text);
        Overhang o = e.Overhang ?? new Overhang(0, 0, 0, 0);
        int fx0 = (int)o.Left, fy0 = (int)o.Top, fx1 = w - (int)o.Right, fy1 = h - (int)o.Bottom;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inFootprint = x >= fx0 && x < fx1 && y >= fy0 && y < fy1;
                Color c;
                if (inFootprint || e.Footprint == null)
                {
                    c = fill;
                    c.A = r.FootprintAlpha / 1000f;
                }
                else
                {
                    bool onHatch = ((x + y) % (int)r.HatchPitchPx) == 0;
                    c = hatch;
                    c.A = onHatch ? r.OverhangAlpha / 1000f : 0f;
                }
                if (x == 0 || y == 0 || x == w - 1 || y == h - 1) c = border;
                img.SetPixel(x, y, c);
            }
        }
        // Anchor cross, 3x3, in the text tone.
        int ax = (int)Math.Round(e.Sprite.Anchor.X * (w - 1));
        int ay = (int)Math.Round(e.Sprite.Anchor.Y * (h - 1));
        for (int d = -1; d <= 1; d++)
        {
            Put(img, ax + d, ay, text, w, h);
            Put(img, ax, ay + d, text, w, h);
        }
        return ImageTexture.CreateFromImage(img);
    }

    private static void Put(Image img, int x, int y, Color c, int w, int h)
    {
        if (x >= 0 && y >= 0 && x < w && y < h) img.SetPixel(x, y, c);
    }
}
