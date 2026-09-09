using System;
using System.Collections.Generic;
using System.IO;
using CompanyWars.Content;
using CompanyWars.Manifest;
using CompanyWars.Playback;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// The autoload that holds exactly one active screen (ARCHITECTURE.md §7), the loaded content and manifest,
/// the greybox textures and the pixel font, and the fight chosen on the picker. In screenshot mode it walks the
/// fixture list, capturing each screen after three frames, and quits.
/// </summary>
public partial class ScreenRouter : Node
{
    public static ScreenRouter Instance { get; private set; } = null!;

    public ContentDb Content { get; private set; } = null!;
    public ManifestReport Manifest { get; private set; } = null!;
    public GreyboxTextures Textures { get; private set; } = null!;
    public ManifestLayout Layout { get; private set; } = null!;
    public PixelFont Font { get; private set; } = null!;
    public string RepoRoot { get; private set; } = string.Empty;

    public string RivalA { get; set; } = "rival.boss_parent_company";
    public string RivalB { get; set; } = "rival.boss_compliance_office";
    public uint Seed { get; set; } = 1;
    public MatchView? LastView { get; set; }

    public bool ScreenshotMode { get; private set; }
    private readonly Queue<(string Name, string Scene)> _shots = new();
    private Node? _active;

    public override void _Ready()
    {
        Instance = this;
        RepoRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
        PixelDiscipline.Assert();
        Content = ContentLoader.Load(RepoRoot);
        Manifest = ManifestValidator.Validate(RepoRoot, Content);
        if (Manifest.Errors.Count > 0) throw new InvalidOperationException("manifest invalid: " + string.Join("; ", Manifest.Errors));
        Textures = new GreyboxTextures(RepoRoot, Manifest);
        Layout = new ManifestLayout(Manifest.Manifest);
        Font = new PixelFont();
        ScreenshotMode = Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshots") >= 0;
        if (ScreenshotMode)
        {
            _shots.Enqueue(("greybox", "res://scenes/Greybox.tscn"));
            _shots.Enqueue(("battle", "res://scenes/Battle.tscn"));
            _shots.Enqueue(("autopsy", "res://scenes/Autopsy.tscn"));
            CallDeferred(nameof(NextShot));
        }
        else
        {
            CallDeferred(nameof(GoDeferred), "res://scenes/Picker.tscn");
        }
    }

    public void Go(string scenePath) => CallDeferred(nameof(GoDeferred), scenePath);

    private void GoDeferred(string scenePath)
    {
        _active?.QueueFree();
        var scene = GD.Load<PackedScene>(scenePath);
        _active = scene.Instantiate();
        GetTree().Root.AddChild(_active);
    }

    /// <summary>Runs the chosen fight once and stores its view. The sim is called here and nowhere else in the client.</summary>
    public MatchView Fight()
    {
        ScriptedRival a = Content.Rival(RivalA);
        ScriptedRival b = Content.Rival(RivalB);
        RuleSet rules = Content.RuleSetFor(Math.Max(a.Round, b.Round));
        ContentTable table = Content.ToContentTable();
        MatchResult result = Simulator.Simulate(Seed, a.Snapshot, b.Snapshot, rules, table);
        LastView = new MatchView(result, a.Snapshot, b.Snapshot, rules, table);
        return LastView;
    }

    public string RivalName(string id) => Content.Rival(id).Name;

    // ---------------------------------------------------------------- screenshot fixtures

    private string _currentShot = string.Empty;

    private void NextShot()
    {
        if (_shots.Count == 0)
        {
            GetTree().Quit();
            return;
        }
        (string name, string scene) = _shots.Dequeue();
        _currentShot = name;
        GoDeferred(scene);
    }

    /// <summary>Called by a screen once it has rendered its fixture state; saves 2x and 3x PNGs and moves on.</summary>
    public void Capture()
    {
        if (!ScreenshotMode) return;
        string dir = Path.Combine(RepoRoot, "game", "__screenshots__", "actual");
        Directory.CreateDirectory(dir);
        Image image = GetViewport().GetTexture().GetImage();
        foreach (int scale in new[] { 2, 3 })
        {
            Image scaled = (Image)image.Duplicate();
            scaled.Resize(image.GetWidth() * scale, image.GetHeight() * scale, Image.Interpolation.Nearest);
            scaled.SavePng(Path.Combine(dir, $"{_currentShot}_{scale}x.png"));
        }
        CallDeferred(nameof(NextShot));
    }
}

/// <summary>Rects come from the manifest at runtime (ARCHITECTURE.md §7). Side B's mirror of a side-A layout is a rule, not a number.</summary>
public sealed class ManifestLayout
{
    private readonly Dictionary<string, ManifestEntry> _entries = new();

    public ManifestLayout(SpriteManifest manifest)
    {
        foreach (ManifestEntry e in manifest.Entries) _entries[e.Id] = e;
        CanvasW = (int)manifest.Canvas.W;
        CanvasH = (int)manifest.Canvas.H;
        Tile = (int)manifest.TileSize;
    }

    public int CanvasW { get; }
    public int CanvasH { get; }
    public int Tile { get; }

    public ManifestEntry Entry(string id) => _entries.TryGetValue(id, out ManifestEntry? e) ? e : throw new InvalidOperationException($"{id} is not in the manifest");

    public Vector2I Size(string id)
    {
        ManifestEntry e = Entry(id);
        return new Vector2I((int)e.Sprite.W, (int)e.Sprite.H);
    }

    /// <summary>The entry's rect on the logical canvas: its layout point is where its anchor sits.</summary>
    public Rect2I Rect(string id)
    {
        ManifestEntry e = Entry(id);
        if (e.Layout == null) throw new InvalidOperationException($"{id} has no layout in the manifest");
        return At(id, (int)e.Layout.X, (int)e.Layout.Y);
    }

    public Rect2I Mirror(Rect2I r) => new(CanvasW - r.Position.X - r.Size.X, r.Position.Y, r.Size.X, r.Size.Y);

    public Rect2I Side(string id, string side) => side == "A" ? Rect(id) : Mirror(Rect(id));

    /// <summary>A rect for an entry placed by anchor at (x, y).</summary>
    public Rect2I At(string id, int x, int y)
    {
        ManifestEntry e = Entry(id);
        int w = (int)e.Sprite.W, h = (int)e.Sprite.H;
        int ax = (int)Math.Round(e.Sprite.Anchor.X * w);
        int ay = (int)Math.Round(e.Sprite.Anchor.Y * h);
        return new Rect2I(x - ax, y - ay, w, h);
    }
}

/// <summary>font.ui.8 at 1x and font.ui.16 at exactly 2x, filtering off (ART_PIPELINE.md §9). The real face is a manifest slot; this is its fallback.</summary>
public sealed class PixelFont
{
    public PixelFont()
    {
        Font fallback = ThemeDB.FallbackFont;
        if (fallback is FontFile file)
        {
            var f = (FontFile)file.Duplicate();
            f.Antialiasing = TextServer.FontAntialiasing.None;
            f.Hinting = TextServer.Hinting.Normal;
            f.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
            f.Oversampling = 1.0f;
            Face = f;
        }
        else
        {
            Face = fallback;
        }
    }

    public Font Face { get; }

    public int Small => 8;
    public int Large => 16;

    public void Draw(CanvasItem c, int x, int y, string text, int size, Color color, HorizontalAlignment align = HorizontalAlignment.Left, int width = -1)
    {
        int ascent = (int)Math.Round(Face.GetAscent(size));
        c.DrawString(Face, new Vector2(x, y + ascent), text, align, width, size, color);
    }

    public int Width(string text, int size) => (int)Math.Ceiling(Face.GetStringSize(text, HorizontalAlignment.Left, -1, size).X);
}

/// <summary>Palette tones by name and by ledger kind.</summary>
public static class Tones
{
    public static Color Fill(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Fill);
    public static Color Border(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Border);
    public static Color Hatch(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Hatch);
    public static Color Text(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Text);

    public static string ForKind(string kind) => kind switch
    {
        "push" => "operations",
        "anomaly" => "anomalous",
        "morale" => "people",
        "restore" => "support",
        "regen" => "support",
        "status" => "interface",
        "retrigger" => "support",
        "whiff" => "invalid",
        "banner" => "structure",
        _ => "interface",
    };
}
