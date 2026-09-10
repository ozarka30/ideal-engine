using CompanyWars.Build;
using Godot;

namespace CompanyWars.Game;

/// <summary>The run summary: outcome, the fight history, back to the menu.</summary>
public partial class SummaryScreen : Node2D
{
    public override void _Ready() => QueueRedraw();

    private SceneLayout? _at;

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        ManifestLayout L = r.Layout;
        PixelFont font = r.Font;
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        RunState? run = r.Run;
        if (run == null) return;
        _at ??= new SceneLayout(this);
        DrawRect(new Rect2(_at.Rect("banner").Position, _at.Rect("banner").Size), Tones.Fill("interface"));
        font.Draw(this, _at.X("title"), _at.Y("title"), Run.Summary(r.Content, run), font.Large, Tones.Text("interface"));
        int y = _at.Y("headings");
        int[] cols = { 8, 48, 96, 160, 216 };
        string[] heads = { "ROUND", "RESULT", "SHARE", "TIME", "RIVAL" };
        for (int i = 0; i < heads.Length; i++) font.Draw(this, cols[i], y, heads[i], font.Small, Tones.Hatch("interface"));
        y += 10;
        foreach (FightRecord f in run.History)
        {
            string res = f.Winner == "A" ? "WON" : f.Winner == "B" ? "LOST" : "DRAW";
            string tone = f.Winner == "A" ? "operations" : f.Winner == "B" ? "invalid" : "interface";
            Color c = Tones.Fill(tone).Lightened(0.4f);
            font.Draw(this, cols[0], y, $"Q{f.Round}", font.Small, c);
            font.Draw(this, cols[1], y, res, font.Small, c);
            font.Draw(this, cols[2], y, $"{f.FinalShare / 100}.{f.FinalShare % 100 / 10}%", font.Small, c, HorizontalAlignment.Right, 48);
            font.Draw(this, cols[3], y, CompanyWars.Playback.Autopsy.Seconds(f.EndTick), font.Small, c, HorizontalAlignment.Right, 40);
            font.Draw(this, cols[4], y, $"{f.RivalName} [{f.RivalArchetype}]", font.Small, c);
            y += 10;
        }
        Vector2I size = L.Size("ui.menu.button");
        _menu = new Rect2I((L.CanvasW - size.X) / 2, L.CanvasH - size.Y - 8, size.X, size.Y);
        Ui.Button(this, _menu, "MENU", "operations");
    }

    private Rect2I _menu;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        bool go = @event is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter or Key.Escape };
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mb && _menu.Grow(Hits.TouchSlop).HasPoint(new Vector2I((int)mb.Position.X, (int)mb.Position.Y))) go = true;
        if (go)
        {
            ScreenRouter.Instance.Run = null;
            ScreenRouter.Instance.Go("res://scenes/Menu.tscn");
        }
    }
}
