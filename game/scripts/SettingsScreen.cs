using System;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// Display settings the Deck and desktop need before any store check (D-68): fullscreen, and the window's
/// integer scale when windowed. Every control is a button (D-47); nothing here takes text (D-55). Saved to
/// user://settings.cfg and applied at start; never applied in screenshot or drive mode, so fixtures stay fixed.
/// </summary>
public partial class SettingsScreen : Node2D
{
    private readonly Hits _hits = new();
    private DisplaySettings _settings = DisplaySettings.Load();

    public override void _Ready() => QueueRedraw();

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        ManifestLayout L = r.Layout;
        _hits.Clear();
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        r.Font.Draw(this, 0, 64, "SETTINGS", r.Font.Large, Tones.Text("interface").Inverted(), HorizontalAlignment.Center, L.CanvasW);
        Vector2I size = L.Size("ui.menu.button");
        int x = (L.CanvasW - size.X) / 2;

        r.Font.Draw(this, x, 120, "Display", r.Font.Small, Tones.Hatch("interface"));
        var full = new Rect2I(x, 132, size.X, size.Y);
        Ui.Button(this, full, _settings.Fullscreen ? "FULLSCREEN · ON" : "FULLSCREEN · OFF", _settings.Fullscreen ? "operations" : "interface");
        _hits.Add(full, () => Change(s => s.Fullscreen = !s.Fullscreen), "Toggle fullscreen");

        r.Font.Draw(this, x, 164, "Window scale", r.Font.Small, Tones.Hatch("interface"));
        int chipW = (size.X - 8) / 3;
        for (int i = 0; i < 3; i++)
        {
            int scale = i + 2;
            var chip = new Rect2I(x + i * (chipW + 4), 176, chipW, size.Y);
            Ui.Button(this, chip, $"{scale}×", _settings.Scale == scale ? "operations" : "interface", !_settings.Fullscreen);
            _hits.Add(chip, () => Change(s => s.Scale = scale), $"Window at {scale}× the 640×360 canvas");
        }
        r.Font.Draw(this, x, 200, _settings.Fullscreen ? "Scale applies when windowed" : $"{L.CanvasW * _settings.Scale} × {L.CanvasH * _settings.Scale} window", r.Font.Small, Tones.Hatch("interface"));

        var back = new Rect2I(x, 248, size.X, size.Y);
        Ui.Button(this, back, "BACK", "support");
        _hits.Add(back, () => r.Go("res://scenes/Menu.tscn"), "Back to the menu");
        r.Font.Draw(this, 0, L.CanvasH - 12, $"build {r.BuildTag}", r.Font.Small, Tones.Hatch("interface"), HorizontalAlignment.Right, L.CanvasW - 8);
    }

    private void Change(Action<DisplaySettings> edit)
    {
        edit(_settings);
        _settings.Save();
        _settings.Apply(ScreenRouter.Instance);
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) { ScreenRouter.Instance.Go("res://scenes/Menu.tscn"); return; }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
        _hits.At(p)?.Click();
    }
}

/// <summary>Fullscreen and integer window scale, persisted in user://settings.cfg.</summary>
public sealed class DisplaySettings
{
    private const string PathCfg = "user://settings.cfg";

    public bool Fullscreen { get; set; }
    public int Scale { get; set; } = 2;

    public static DisplaySettings Load()
    {
        var s = new DisplaySettings();
        var cfg = new ConfigFile();
        if (cfg.Load(PathCfg) == Error.Ok)
        {
            s.Fullscreen = cfg.GetValue("display", "fullscreen", false).AsBool();
            s.Scale = Math.Clamp(cfg.GetValue("display", "scale", 2).AsInt32(), 2, 4);
        }
        return s;
    }

    public void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("display", "fullscreen", Fullscreen);
        cfg.SetValue("display", "scale", Scale);
        cfg.Save(PathCfg);
    }

    /// <summary>Phones keep their own window; everywhere else the window follows the settings.</summary>
    public void Apply(Node node)
    {
        if (OS.HasFeature("mobile")) return;
        Window w = node.GetWindow();
        if (Fullscreen)
        {
            w.Mode = Window.ModeEnum.Fullscreen;
            return;
        }
        w.Mode = Window.ModeEnum.Windowed;
        ManifestLayout L = ScreenRouter.Instance.Layout;
        w.Size = new Vector2I(L.CanvasW * Scale, L.CanvasH * Scale);
    }
}
