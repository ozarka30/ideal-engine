using CompanyWars.Build;
using Godot;

namespace CompanyWars.Game;

/// <summary>The run summary: outcome, the fight history, back to the menu.</summary>
public partial class SummaryScreen : Node2D
{
    public override void _Ready() => QueueRedraw();

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        ManifestLayout L = r.Layout;
        PixelFont font = r.Font;
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        RunState? run = r.Run;
        if (run == null) return;
        DrawRect(new Rect2(0, 0, L.CanvasW, 24), Tones.Fill("interface"));
        font.Draw(this, 8, 4, Run.Summary(r.Content, run), font.Large, Tones.Text("interface"));
        int y = 40;
        font.Draw(this, 8, y, "ROUND  RESULT  SHARE   TICK   RIVAL", font.Small, Tones.Hatch("interface"));
        y += 10;
        foreach (FightRecord f in run.History)
        {
            string res = f.Winner == "A" ? "WON " : f.Winner == "B" ? "LOST" : "DRAW";
            string tone = f.Winner == "A" ? "operations" : f.Winner == "B" ? "people" : "interface";
            font.Draw(this, 8, y, $"Q{f.Round,2}     {res}    {f.FinalShare / 100,3}.{f.FinalShare % 100 / 10}%  {f.EndTick,4}   {f.RivalName} [{f.RivalArchetype}]", font.Small, Tones.Fill(tone).Lightened(0.4f));
            y += 10;
        }
        font.Draw(this, 8, 340, "click anywhere to return to the menu", font.Small, Tones.Hatch("interface"));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } || @event is InputEventKey { Pressed: true })
        {
            ScreenRouter.Instance.Run = null;
            ScreenRouter.Instance.Go("res://scenes/Menu.tscn");
        }
    }
}
