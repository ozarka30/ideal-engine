using System;
using System.Collections.Generic;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>Founder select (GAME_DESIGN.md §19.7, D-46): eight cards, a bio, a default firm name, FOUND THE FIRM. No text input (D-55).</summary>
public partial class FounderScreen : Node2D
{
    private string _selected = string.Empty;
    private readonly List<(Rect2I Rect, string Id)> _cards = new();

    private int _frames;
    private bool _captured;

    public override void _Ready()
    {
        _selected = ScreenRouter.Instance.LastFounderId;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!ScreenRouter.Instance.ScreenshotMode) return;
        _frames++;
        if (_frames == 3 && !_captured) { _captured = true; ScreenRouter.Instance.Capture(); }
    }

    public override void _Draw()
    {
        ScreenRouter r = ScreenRouter.Instance;
        ManifestLayout L = r.Layout;
        PixelFont font = r.Font;
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        Rect2I header = L.Rect("ui.founder.header");
        DrawRect(new Rect2(header.Position, header.Size), Tones.Fill("interface"));
        font.Draw(this, header.Position.X + 8, header.Position.Y + 4, "CHOOSE A FOUNDER", font.Large, Tones.Text("interface"));

        Rect2I grid = L.Rect("ui.founder.grid");
        Vector2I card = L.Size("ui.founder.card");
        int cellW = grid.Size.X / 4, cellH = grid.Size.Y / 2;
        _cards.Clear();
        int i = 0;
        FounderDef? selected = null;
        foreach (FounderDef f in r.Content.Founders)
        {
            int col = i % 4, row = i / 4;
            int x = grid.Position.X + col * cellW + (cellW - card.X) / 2;
            int y = grid.Position.Y + row * cellH + (cellH - card.Y) / 2;
            var rect = new Rect2I(x, y, card.X, card.Y);
            bool sel = f.Id == _selected;
            if (sel) selected = f;
            DrawTextureRect(r.Textures.For(L.Entry("ui.founder.card")), new Rect2(rect.Position, rect.Size), false);
            if (sel) DrawRect(new Rect2(rect.Position, rect.Size), Tones.Fill("operations"), false, 2);
            Vector2I portrait = L.Size(f.Portrait);
            DrawTextureRect(r.Textures.For(L.Entry(f.Portrait)), new Rect2(x + 4, y + 4, portrait.X, portrait.Y), false);
            font.Draw(this, x + 4, y + 72, f.Name, font.Small, Tones.Text("people"), HorizontalAlignment.Left, 64);
            font.Draw(this, x + 4, y + 82, f.Title, font.Small, Tones.Hatch("people"), HorizontalAlignment.Left, 64);
            _cards.Add((rect, f.Id));
            i++;
        }

        Rect2I bio = L.Rect("ui.founder.bio");
        DrawRect(new Rect2(bio.Position, bio.Size), Tones.Fill("interface"));
        if (selected != null)
        {
            int by = bio.Position.Y + 4;
            foreach (string line in Ui.Wrap(selected.Bio, 110, 4))
            {
                font.Draw(this, bio.Position.X + 4, by, line, font.Small, Tones.Text("interface"));
                by += 8;
            }
        }

        Rect2I name = L.Rect("ui.founder.firm_name");
        DrawRect(new Rect2(name.Position, name.Size), Tones.Fill("interface"));
        font.Draw(this, name.Position.X + 4, name.Position.Y + 4, "FIRM · ", font.Small, Tones.Hatch("interface"));
        font.Draw(this, name.Position.X + 4 + font.Width("FIRM · ", font.Small), name.Position.Y + 4, selected != null ? CompanyWars.Build.Run.DefaultFirmName(selected) : string.Empty, font.Small, Tones.Text("interface"));

        Rect2I confirm = L.Rect("ui.founder.confirm");
        Ui.Button(this, confirm, "FOUND THE FIRM", "operations", _selected.Length > 0);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter } && _selected.Length > 0) { ScreenRouter.Instance.StartRun(_selected); return; }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
        foreach ((Rect2I rect, string id) in _cards)
        {
            if (rect.HasPoint(p)) { _selected = id; QueueRedraw(); return; }
        }
        if (ScreenRouter.Instance.Layout.Rect("ui.founder.confirm").HasPoint(p) && _selected.Length > 0) ScreenRouter.Instance.StartRun(_selected);
    }
}
