using Godot;

namespace CompanyWars.Game;

/// <summary>The menu: a new run, or the debug fight picker.</summary>
public partial class MenuScreen : Node2D
{
    private Rect2I _newRun;
    private Rect2I _settings;
    private Rect2I _debug;

    public override void _Ready() => QueueRedraw();

    private SceneLayout? _at;

    /// <summary>Text across a slot's width, so centring and right-alignment follow the node (D-81).</summary>
    private void Band(string slot, string text, int size, Color color, HorizontalAlignment align = HorizontalAlignment.Center)
    {
        Rect2I r = _at!.Rect(slot);
        ScreenRouter.Instance.Font.Draw(this, r.Position.X, r.Position.Y, text, size, color, align, r.Size.X);
    }

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        ManifestLayout L = r.Layout;
        _at ??= new SceneLayout(this);
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        Band("title", "COMPANY WARS", r.Font.Large, Tones.Text("interface").Inverted());
        Band("subtitle", "greybox · M2 vertical slice", r.Font.Small, Tones.Hatch("interface"));
        Vector2I size = L.Size("ui.menu.button");
        _newRun = new Rect2I(_at.X("new_run"), _at.Y("new_run"), size.X, size.Y);
        _settings = new Rect2I(_at.X("settings"), _at.Y("settings"), size.X, size.Y);
        _debug = new Rect2I(_at.X("debug"), _at.Y("debug"), size.X, size.Y);
        Ui.Button(this, _newRun, "NEW RUN", "operations");
        Ui.Button(this, _settings, "SETTINGS", "interface");
        Ui.Button(this, _debug, "DEBUG FIGHT", "interface");
        Band("prompt", "Enter starts a run", r.Font.Small, Tones.Hatch("interface"));
        // The build tag (D-68): a tester's report names a commit, not a weekday.
        Band("build_tag", $"build {r.BuildTag}", r.Font.Small, Tones.Hatch("interface"), HorizontalAlignment.Right);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter }) { ScreenRouter.Instance.Go("res://scenes/Founder.tscn"); return; }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
        if (_newRun.Grow(Hits.TouchSlop).HasPoint(p)) ScreenRouter.Instance.Go("res://scenes/Founder.tscn");
        else if (_settings.Grow(Hits.TouchSlop).HasPoint(p)) ScreenRouter.Instance.Go("res://scenes/Settings.tscn");
        else if (_debug.Grow(Hits.TouchSlop).HasPoint(p)) ScreenRouter.Instance.Go("res://scenes/Picker.tscn");
    }
}
