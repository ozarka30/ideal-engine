using System.Collections.Generic;
using CompanyWars.Content;
using Godot;

namespace CompanyWars.Game;

/// <summary>M1's throwaway debug rival picker: two lists of the scripted rivals, a seed stepper, a FIGHT button. No text input (D-55).</summary>
public partial class PickerScreen : Node2D
{
    private const int RowH = 12;
    private readonly List<(Rect2I Rect, string Side, string Id)> _rows = new();
    private Rect2I _seedButton;
    private Rect2I _fightButton;

    public override void _Ready() => QueueRedraw();

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        PixelFont font = r.Font;
        DrawRect(new Rect2(0, 0, r.Layout.CanvasW, r.Layout.CanvasH), Tones.Fill("structure"));
        font.Draw(this, 8, 8, "COMPANY WARS · M1 · pick two scripted rivals", font.Small, Tones.Text("interface").Inverted());
        _rows.Clear();
        int y0 = 32;
        foreach ((string side, int x) in new[] { ("A", 8), ("B", 328) })
        {
            font.Draw(this, x, y0 - 12, side == "A" ? "SIDE A (you)" : "SIDE B (rival)", font.Small, Tones.Hatch("interface"));
            int y = y0;
            foreach (ScriptedRival rival in r.Content.ScriptedRivals)
            {
                var rect = new Rect2I(x, y, 300, RowH);
                bool selected = (side == "A" ? r.RivalA : r.RivalB) == rival.Id;
                DrawRect(new Rect2(rect.Position, rect.Size), selected ? Tones.Fill("people") : Tones.Fill("interface"));
                DrawRect(new Rect2(rect.Position, rect.Size), Tones.Border("interface"), false);
                font.Draw(this, x + 4, y + 2, $"R{rival.Round,2}  {rival.Name}  [{rival.Archetype}]", font.Small, Tones.Text("interface"));
                _rows.Add((rect, side, rival.Id));
                y += RowH + 2;
            }
        }
        _seedButton = new Rect2I(8, 200, 120, 16);
        DrawRect(new Rect2(_seedButton.Position, _seedButton.Size), Tones.Fill("support"));
        font.Draw(this, 12, 204, $"SEED {r.Seed}  ▸ next", font.Small, Tones.Text("support"));
        _fightButton = new Rect2I(8, 224, 120, 20);
        DrawRect(new Rect2(_fightButton.Position, _fightButton.Size), Tones.Fill("operations"));
        font.Draw(this, 12, 226, "FIGHT", font.Large, Tones.Text("operations"));
        font.Draw(this, 8, 340, "keys in battle: 1 2 4 speed · space pause · S skip · esc autopsy · hover a floor for the inset", font.Small, Tones.Hatch("interface"));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        ScreenRouter r = ScreenRouter.Instance;
        var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
        foreach ((Rect2I rect, string side, string id) in _rows)
        {
            if (rect.HasPoint(p))
            {
                if (side == "A") r.RivalA = id; else r.RivalB = id;
                QueueRedraw();
                return;
            }
        }
        if (_seedButton.HasPoint(p)) { r.Seed++; QueueRedraw(); return; }
        if (_fightButton.HasPoint(p)) r.Go("res://scenes/Battle.tscn");
    }
}
