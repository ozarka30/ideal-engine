using Godot;

namespace CompanyWars.Game;

/// <summary>The menu: a new run, or the debug fight picker.</summary>
public partial class MenuScreen : Node2D
{
    private Rect2I _newRun;
    private Rect2I _debug;

    public override void _Ready() => QueueRedraw();

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        ManifestLayout L = r.Layout;
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        r.Font.Draw(this, 0, 96, "COMPANY WARS", r.Font.Large, Tones.Text("interface").Inverted(), HorizontalAlignment.Center, L.CanvasW);
        r.Font.Draw(this, 0, 116, "greybox · M2 vertical slice", r.Font.Small, Tones.Hatch("interface"), HorizontalAlignment.Center, L.CanvasW);
        Vector2I size = L.Size("ui.menu.button");
        _newRun = new Rect2I((L.CanvasW - size.X) / 2, 160, size.X, size.Y);
        _debug = new Rect2I((L.CanvasW - size.X) / 2, 184, size.X, size.Y);
        Ui.Button(this, _newRun, "NEW RUN", "operations");
        Ui.Button(this, _debug, "DEBUG FIGHT", "interface");
        r.Font.Draw(this, 0, 220, "Enter starts a run", r.Font.Small, Tones.Hatch("interface"), HorizontalAlignment.Center, L.CanvasW);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter }) { ScreenRouter.Instance.Go("res://scenes/Founder.tscn"); return; }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
        if (_newRun.Grow(Hits.TouchSlop).HasPoint(p)) ScreenRouter.Instance.Go("res://scenes/Founder.tscn");
        else if (_debug.Grow(Hits.TouchSlop).HasPoint(p)) ScreenRouter.Instance.Go("res://scenes/Picker.tscn");
    }
}
