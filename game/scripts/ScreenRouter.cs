using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CompanyWars.Content;
using CompanyWars.Manifest;
using CompanyWars.Playback;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// The autoload that holds exactly one active screen (ARCHITECTURE.md §7), the loaded content and manifest,
/// the greybox textures and the pixel font, and the fight chosen on the picker. In screenshot mode it walks the
/// fixture list, capturing each screen after three frames, and quits. In drive mode (<c>--drive script.json</c>)
/// it plays a list of steps — taps, keys, waits, screenshots — so an agent can operate the game from outside
/// through the Godot MCP server and read the result back as files and stdout lines.
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

    // The run (ARCHITECTURE.md §4.1) and the fight the battle screen plays next.
    public CompanyWars.Build.RunState? Run { get; set; }
    public CompanyWars.Build.BuildState? Building { get; set; }
    public CompanyWars.Build.Rival? CurrentRival { get; set; }
    public FightSpec? CurrentFight { get; set; }
    public string LastFounderId { get; set; } = "founder.sato";

    public bool ScreenshotMode { get; private set; }
    public bool DriveMode { get; private set; }

    /// <summary>The commit the build was made from (builds.yml writes build.txt into the data pack), or "dev".</summary>
    public string BuildTag { get; private set; } = "dev";
    private readonly Queue<(string Name, string Scene)> _shots = new();
    private readonly Queue<JsonElement> _drive = new();
    private int _driveWait;
    private int _driveStep;
    private Node? _active;

    public override void _Ready()
    {
        Instance = this;
        RepoRoot = FindDataRoot();
        PixelDiscipline.Assert();
        Content = ContentLoader.Load(RepoRoot);
        Manifest = ManifestValidator.Validate(RepoRoot, Content);
        if (Manifest.Errors.Count > 0) throw new InvalidOperationException("manifest invalid: " + string.Join("; ", Manifest.Errors));
        Textures = new GreyboxTextures(RepoRoot, Manifest);
        Layout = new ManifestLayout(Manifest.Manifest);
        Font = new PixelFont();
        string[] userArgs = OS.GetCmdlineUserArgs();
        ScreenshotMode = Array.IndexOf(userArgs, "--screenshots") >= 0;
        int driveArg = Array.IndexOf(userArgs, "--drive");
        if (driveArg >= 0 && driveArg + 1 < userArgs.Length)
        {
            LoadDrive(userArgs[driveArg + 1]);
        }
        else if (File.Exists(Path.Combine(RepoRoot, "tools", "dev", "drive", "current.json")))
        {
            // The MCP server's run_project passes no arguments: a script parked at this path drives the run instead.
            LoadDrive(Path.Combine("tools", "dev", "drive", "current.json"));
        }
        string tagFile = Path.Combine(RepoRoot, "build.txt");
        if (File.Exists(tagFile)) BuildTag = File.ReadAllText(tagFile).Trim();
        if (!ScreenshotMode && !DriveMode) DisplaySettings.Load().Apply(this);
        if (ScreenshotMode)
        {
            _shots.Enqueue(("greybox", "res://scenes/Greybox.tscn"));
            _shots.Enqueue(("battle", "res://scenes/Battle.tscn"));
            _shots.Enqueue(("autopsy", "res://scenes/Autopsy.tscn"));
            _shots.Enqueue(("founder", "res://scenes/Founder.tscn"));
            _shots.Enqueue(("build", "res://scenes/Build.tscn"));
            _shots.Enqueue(("build_inspect", "res://scenes/Build.tscn"));
            // The build fixture: a run seeded 1, round 1, as the founder screen would start it.
            Run = CompanyWars.Build.Run.New(Content, "mode.ranked", 1, "founder.sato", null);
            Building = CompanyWars.Build.BuildReducer.OpenRound(Content, Run);
            CurrentRival = CompanyWars.Build.Run.RivalFor(Content, Run);
            CallDeferred(nameof(NextShot));
        }
        else
        {
            CallDeferred(nameof(GoDeferred), "res://scenes/Menu.tscn");
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

    /// <summary>The debug picker's fight: two scripted rivals.</summary>
    public FightSpec PickerFight()
    {
        ScriptedRival a = Content.Rival(RivalA);
        ScriptedRival b = Content.Rival(RivalB);
        return new FightSpec(a.Snapshot, b.Snapshot, a.Name, b.Name, Seed, Math.Max(a.Round, b.Round), false);
    }

    /// <summary>Runs the current fight once and stores its view. The sim is called here and nowhere else in the client.</summary>
    public MatchView Fight()
    {
        FightSpec f = CurrentFight ?? PickerFight();
        CurrentFight = f;
        RuleSet rules = Content.RuleSetFor(f.Round);
        ContentTable table = Content.ToContentTable();
        MatchResult result = Simulator.Simulate(f.Seed, f.A, f.B, rules, table);
        LastView = new MatchView(result, f.A, f.B, rules, table);
        return LastView;
    }

    public string NameA => CurrentFight?.NameA ?? Content.Rival(RivalA).Name;
    public string NameB => CurrentFight?.NameB ?? Content.Rival(RivalB).Name;
    public long FightRound => CurrentFight?.Round ?? Math.Max(Content.Rival(RivalA).Round, Content.Rival(RivalB).Round);

    // ---------------------------------------------------------------- the run

    public void StartRun(string founderId)
    {
        LastFounderId = founderId;
        uint seed = (uint)Random.Shared.Next(); // the only non-deterministic input in the client: the run seed itself
        Run = CompanyWars.Build.Run.New(Content, "mode.ranked", seed, founderId, null);
        OpenRound();
    }

    public void OpenRound()
    {
        Building = CompanyWars.Build.BuildReducer.OpenRound(Content, Run!);
        CurrentRival = CompanyWars.Build.Run.RivalFor(Content, Run!);
        Go("res://scenes/Build.tscn");
    }

    /// <summary>Ready: commit, expand the rival, fight.</summary>
    public void ReadyUp()
    {
        Run = CompanyWars.Build.BuildReducer.Commit(Content, Building!);
        CompanyWars.Build.Rival rival = CurrentRival ?? CompanyWars.Build.Run.RivalFor(Content, Run);
        CurrentFight = new FightSpec(Run.Tower, rival.Snapshot, Run.FirmName, rival.Name, CompanyWars.Build.Run.FightSeed(Run), Run.Round, true);
        Go("res://scenes/Battle.tscn");
    }

    /// <summary>After the autopsy of a run fight: strikes, bonus, next round or the summary.</summary>
    public void AfterFight()
    {
        if (Run == null || CurrentRival == null || LastView == null) { Go("res://scenes/Menu.tscn"); return; }
        Run = CompanyWars.Build.Run.AfterFight(Content, Run, CurrentRival, LastView.Result);
        CurrentFight = null;
        if (Run.Over) Go("res://scenes/Summary.tscn");
        else OpenRound();
    }

    /// <summary>
    /// Where content/, schema/ and manifest/ live: the repository in development (the project is at repo/game),
    /// or beside the executable in an export, or inside the macOS bundle's Resources (ARCHITECTURE.md §2).
    /// </summary>
    private static string FindDataRoot()
    {
        var candidates = new List<string>();
        string res = ProjectSettings.GlobalizePath("res://");
        if (!string.IsNullOrEmpty(res)) candidates.Add(Path.GetFullPath(Path.Combine(res, "..")));
        string exeDir = Path.GetDirectoryName(OS.GetExecutablePath()) ?? string.Empty;
        if (exeDir.Length > 0)
        {
            candidates.Add(exeDir);
            candidates.Add(Path.GetFullPath(Path.Combine(exeDir, "..")));
            candidates.Add(Path.GetFullPath(Path.Combine(exeDir, "..", "Resources")));
        }
        foreach (string c in candidates)
        {
            if (File.Exists(Path.Combine(c, "content", "index.json")) && File.Exists(Path.Combine(c, "manifest", "sprites.json"))) return c;
        }
        // An export: the data rides inside the resource pack under res://data (ARCHITECTURE.md §2) and is copied
        // to user://data, which is a real directory on every platform including Android, so the libraries can
        // read it with System.IO exactly as they read the repository.
        if (DirAccess.DirExistsAbsolute("res://data/content"))
        {
            CopyTree("res://data", "user://data");
            return ProjectSettings.GlobalizePath("user://data");
        }
        throw new InvalidOperationException("content/ and manifest/ not found beside the project, beside the executable, or packed under res://data; looked in: " + string.Join(", ", candidates));
    }

    private static void CopyTree(string from, string to)
    {
        DirAccess.MakeDirRecursiveAbsolute(to);
        using DirAccess dir = DirAccess.Open(from) ?? throw new InvalidOperationException("cannot open " + from);
        dir.IncludeHidden = false;
        dir.ListDirBegin();
        for (string name = dir.GetNext(); name.Length > 0; name = dir.GetNext())
        {
            string src = from + "/" + name;
            string dst = to + "/" + name;
            if (dir.CurrentIsDir())
            {
                CopyTree(src, dst);
            }
            else if (!name.EndsWith(".import", StringComparison.Ordinal))
            {
                using Godot.FileAccess input = Godot.FileAccess.Open(src, Godot.FileAccess.ModeFlags.Read) ?? throw new InvalidOperationException("cannot read " + src);
                using Godot.FileAccess output = Godot.FileAccess.Open(dst, Godot.FileAccess.ModeFlags.Write) ?? throw new InvalidOperationException("cannot write " + dst);
                output.StoreBuffer(input.GetBuffer((long)input.GetLength()));
            }
        }
        dir.ListDirEnd();
    }

    // ---------------------------------------------------------------- drive mode

    /// <summary>
    /// A drive script is a JSON array of steps, run one per few frames, in order:
    /// <c>{"run": {"founder": "founder.sato", "seed": 1}}</c> starts a run with a fixed seed;
    /// <c>{"scene": "res://scenes/Menu.tscn"}</c> opens a screen; <c>{"tap": [x, y]}</c> taps a canvas point;
    /// <c>{"key": "Enter"}</c> presses a key by its Godot name; <c>{"wait": 30}</c> waits frames;
    /// <c>{"shot": "name"}</c> saves <c>game/__screenshots__/drive/name.png</c> at the window's size; <c>{"quit": true}</c> exits.
    /// Every step prints a <c>[drive]</c> line to stdout, which the MCP server's debug output relays.
    /// </summary>
    private void LoadDrive(string path)
    {
        string full = Path.IsPathRooted(path) ? path : Path.Combine(RepoRoot, path);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(full));
        foreach (JsonElement step in doc.RootElement.EnumerateArray()) _drive.Enqueue(step.Clone());
        DriveMode = true;
        _driveWait = 6; // let the menu draw before the first step
        GD.Print($"[drive] loaded {_drive.Count} steps from {full}");
    }

    public override void _Process(double delta)
    {
        if (!DriveMode) return;
        if (_driveWait > 0) { _driveWait--; return; }
        if (_drive.Count == 0) { GD.Print("[drive] done"); DriveMode = false; GetTree().Quit(); return; }
        JsonElement step = _drive.Dequeue();
        _driveStep++;
        _driveWait = 4;
        try
        {
            DriveStep(step);
        }
        catch (Exception ex)
        {
            GD.Print($"[drive] step {_driveStep} failed: {ex.Message}");
        }
    }

    private void DriveStep(JsonElement step)
    {
        if (step.TryGetProperty("wait", out JsonElement wait)) { _driveWait = wait.GetInt32(); GD.Print($"[drive] {_driveStep} wait {_driveWait}"); return; }
        if (step.TryGetProperty("scene", out JsonElement scene)) { GD.Print($"[drive] {_driveStep} scene {scene.GetString()}"); Go(scene.GetString()!); return; }
        if (step.TryGetProperty("run", out JsonElement run))
        {
            string founder = run.TryGetProperty("founder", out JsonElement f) ? f.GetString()! : "founder.sato";
            uint seed = run.TryGetProperty("seed", out JsonElement sd) ? sd.GetUInt32() : 1u;
            LastFounderId = founder;
            Run = CompanyWars.Build.Run.New(Content, "mode.ranked", seed, founder, null);
            GD.Print($"[drive] {_driveStep} run {founder} seed {seed}");
            OpenRound();
            return;
        }
        if (step.TryGetProperty("tap", out JsonElement tap))
        {
            int x = tap[0].GetInt32(), y = tap[1].GetInt32();
            GD.Print($"[drive] {_driveStep} tap {x},{y}");
            DriveTap(x, y);
            return;
        }
        if (step.TryGetProperty("key", out JsonElement key))
        {
            string name = key.GetString()!;
            if (!Enum.TryParse(name, true, out Key code)) throw new InvalidOperationException($"unknown key {name}");
            GD.Print($"[drive] {_driveStep} key {code}");
            Input.ParseInputEvent(new InputEventKey { Keycode = code, PhysicalKeycode = code, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = code, PhysicalKeycode = code, Pressed = false });
            return;
        }
        if (step.TryGetProperty("shot", out JsonElement shot))
        {
            string dir = Path.Combine(RepoRoot, "game", "__screenshots__", "drive");
            Directory.CreateDirectory(dir);
            Image image = GetViewport().GetTexture().GetImage();
            string file = Path.Combine(dir, $"{shot.GetString()}.png");
            image.SavePng(file);
            GD.Print($"[drive] {_driveStep} shot {file}");
            return;
        }
        if (step.TryGetProperty("quit", out _)) { GD.Print($"[drive] {_driveStep} quit"); _drive.Clear(); _driveWait = 0; return; }
        throw new InvalidOperationException("unknown step " + step.GetRawText());
    }

    /// <summary>A left click at a logical-canvas point, mapped through the integer stretch to window pixels.</summary>
    private void DriveTap(int x, int y)
    {
        Vector2I win = GetWindow().Size;
        int scale = Math.Max(1, Math.Min(win.X / Layout.CanvasW, win.Y / Layout.CanvasH));
        var offset = new Vector2((win.X - Layout.CanvasW * scale) / 2f, (win.Y - Layout.CanvasH * scale) / 2f);
        var pos = offset + new Vector2(x * scale + scale / 2f, y * scale + scale / 2f);
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = pos, GlobalPosition = pos });
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = pos, GlobalPosition = pos });
    }

    // ---------------------------------------------------------------- screenshot fixtures

    private string _currentShot = string.Empty;

    /// <summary>The fixture being rendered, so a screen can open in the state the fixture shows; empty outside screenshot mode.</summary>
    public string CurrentShot => _currentShot;

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
        // The window is the fixture: run once at 1280x720 for the 2x set and once at 1920x1080 for 3x (--resolution).
        Image image = GetViewport().GetTexture().GetImage();
        int scale = Math.Max(1, image.GetWidth() / Layout.CanvasW);
        image.SavePng(Path.Combine(dir, $"{_currentShot}_{scale}x.png"));
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

/// <summary>
/// The two faces (D-66, D-67): Honey Pigeon for body text at the 8 px line and Honeyblot Caps at the 16 px line for
/// headers and at the 32 px line for a screen's own title (D-75), loaded as vector fonts and rendered at the window's resolution, antialiased. Text is the one thing on
/// screen that is not pixel art; the canvas_items stretch mode keeps sprites integer-scaled and lets text be smooth.
/// Godot's own fallback face stands in for a missing file.
/// </summary>
public sealed class PixelFont
{
    private readonly Font _small;
    private readonly Font _large;

    public PixelFont()
    {
        _small = LoadVector("res://fonts/HoneyPigeon.ttf") ?? Fallback();
        _large = LoadVector("res://fonts/honeyblot_caps.ttf") ?? Fallback();
    }

    private static Font? LoadVector(string path)
    {
        if (!ResourceLoader.Exists(path)) return null;
        var face = (FontFile)GD.Load<FontFile>(path).Duplicate();
        face.Antialiasing = TextServer.FontAntialiasing.Gray;
        face.Hinting = TextServer.Hinting.None;
        face.SubpixelPositioning = TextServer.SubpixelPositioning.Auto;
        return face;
    }

    private static Font Fallback() => ThemeDB.FallbackFont;

    public Font Face => _small;

    public int Small => 8;
    public int Large => 16;

    /// <summary>A screen's own title (D-75): Honeyblot Caps at a 32px line, exactly 2x Large.</summary>
    public int Title => 32;

    /// <summary>The body face is the 8 px line; anything larger is a header in caps, such as a card's price (D-82).</summary>
    private Font FaceFor(int size) => size > Small ? _large : _small;

    /// <summary>Draws with (x, y) as the top-left of the line box: an 8 px line at size 8, exactly 16 at size 16.</summary>
    public void Draw(CanvasItem c, int x, int y, string text, int size, Color color, HorizontalAlignment align = HorizontalAlignment.Left, int width = -1)
    {
        Font face = FaceFor(size);
        int ascent = (int)Math.Round(face.GetAscent(size));
        c.DrawString(face, new Vector2(x, y + ascent), text, align, width, size, color);
    }

    public int Width(string text, int size) => (int)Math.Ceiling(FaceFor(size).GetStringSize(text, HorizontalAlignment.Left, -1, size).X);
}

/// <summary>Palette tones by name and by ledger kind.</summary>
public static class Tones
{
    public static Color Fill(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Fill);
    public static Color Border(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Border);
    public static Color Hatch(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Hatch);
    public static Color Text(string tone) => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Tones[tone].Text);

    /// <summary>The backdrop every screen sits on, outside the seven tones.</summary>
    public static Color Ground() => Color.FromHtml(ScreenRouter.Instance.Manifest.Palette.Ground.Backdrop);

    /// <summary>
    /// Secondary text on a panel of the same tone. It is the border colour, not the hatch: hatch is the
    /// lighter shade the greybox draws overhang with, and on a light fill it is invisible -- under the
    /// current palette `interface` hatch is luminance 194 against a fill of 169, a difference nobody can
    /// read. Border is 100, which they can.
    /// </summary>
    public static Color Muted(string tone) => Border(tone);

    public static string ForKind(string kind) => kind switch
    {
        "sales" => "operations",
        "poach" => "people",
        "curse" => "anomalous",
        "scandal" => "people",
        "pr" => "support",
        "regen" => "support",
        "status" => "interface",
        "retrigger" => "support",
        "whiff" => "invalid",
        "banner" => "structure",
        _ => "interface",
    };
}
