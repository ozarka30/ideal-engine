using System;
using System.Collections.Generic;
using System.Linq;
using CompanyWars.Build;
using CompanyWars.Content;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// Founder select (GAME_DESIGN.md §19.7): a roster column of eight thumbnails and a detail panel for the one
/// selected (D-72). The title is type on the backdrop, not a plate (D-75); the firm name is a readout, not a
/// control, and the screen has exactly one button. No text input (D-55).
/// </summary>
public partial class FounderScreen : Node2D
{
    private string _selected = string.Empty;
    private readonly List<(Rect2I Rect, string Id)> _tiles = new();
    private SceneLayout _at = null!;

    private int _frames;
    private bool _captured;

    public override void _Ready()
    {
        _at = new SceneLayout(this);
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

        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Ground());
        Vector2I title = _at.At("title");
        font.Draw(this, title.X, title.Y, "CHOOSE A FOUNDER", font.Title, Tones.Muted("support"));

        DrawEntry(L, "ui.founder.grid", _at.Rect("grid"));
        Vector2I tile = L.Size("ui.founder.tile");
        Vector2I tiles = _at.Origin("tiles");
        _tiles.Clear();
        FounderDef? selected = null;
        int i = 0;
        foreach (FounderDef f in r.Content.Founders)
        {
            var rect = new Rect2I(tiles.X + i % 2 * 72, tiles.Y + i / 2 * 72, tile.X, tile.Y);
            bool sel = f.Id == _selected;
            if (sel) selected = f;
            DrawTextureRect(r.Textures.For(L.Entry("ui.founder.tile")), new Rect2(rect.Position, rect.Size), false);
            Vector2I thumb = L.Size(f.Id + ".thumb");
            DrawTextureRect(r.Textures.For(L.Entry(f.Id + ".thumb")), new Rect2(rect.Position.X + 8, rect.Position.Y + 8, thumb.X, thumb.Y), false);
            if (sel) DrawRect(new Rect2(rect.Position, rect.Size), Tones.Fill("support"), false, 2);
            _tiles.Add((rect, f.Id));
            i++;
        }

        DrawEntry(L, "ui.founder.detail", _at.Rect("detail"));
        Color ink = Tones.Text("interface");
        Color muted = Tones.Border("interface");
        if (selected != null)
        {
            Vector2I portrait = L.Size(selected.Portrait);
            DrawTextureRect(r.Textures.For(L.Entry(selected.Portrait)), new Rect2(_at.At("portrait"), portrait), false);
            Text(font, "name", selected.Name, font.Large, ink);
            Text(font, "title_line", selected.Title, font.Small, muted);

            // The badge is what represents this founder in a fight; the founder itself does nothing there (D-46).
            Vector2I badge = L.Size(selected.Badge);
            DrawTextureRect(r.Textures.For(L.Entry(selected.Badge)), new Rect2(_at.At("badge"), badge), false);
            Text(font, "badge_caption", "your badge in the fight", font.Small, muted);

            Text(font, "firm_caption", "FIRM NAME", font.Small, muted);
            Text(font, "firm_name", CompanyWars.Build.Run.DefaultFirmName(selected), font.Large, ink);

            Rect2I bio = _at.Rect("bio");
            int by = bio.Position.Y;
            foreach (string line in Ui.Wrap(font, font.Small, selected.Bio, bio.Size.X, 3))
            {
                font.Draw(this, bio.Position.X, by, line, font.Small, ink);
                by += 12;
            }

            Rect2I rule = _at.Rect("rule");
            DrawLine(rule.Position, new Vector2(rule.Position.X + rule.Size.X, rule.Position.Y), muted);
            Text(font, "stakes_heading", "YOU START WITH", font.Large, muted);

            // The stakes (D-68), drawn from content rather than written down: the roster as sprites, then in words.
            EconomyFile eco = r.Content.Economy;
            Vector2I roster = _at.At("roster");
            int rx = roster.X;
            foreach (string id in eco.StartingRoster)
            {
                EmployeeDef d = r.Content.Employees.First(e => e.Id == id);
                Vector2I s = L.Size(d.Sprite);
                DrawTextureRect(r.Textures.For(L.Entry(d.Sprite)), new Rect2(rx, roster.Y, s.X, s.Y), false);
                rx += 40;
            }
            FloorDef start = r.Content.Floors.First(f => f.Id == eco.StartingRosterFloor);
            string names = Roster(r.Content, eco.StartingRoster);
            Text(font, "roster_line", $"{names} on {Legality.FloorName(start.Index)}", font.Small, ink);
            Text(font, "budget_line", $"¥{eco.StartingBudget} to spend", font.Small, ink);
            Text(font, "passive_line", selected.Effects.Length == 0
                ? "no founder passive in this build"
                : string.Join(" ", Explain.Passives(r.Content, selected.Effects)), font.Small, muted);
        }

        Ui.Button(this, _at.Rect("confirm"), "FOUND THE FIRM", "operations", _selected.Length > 0, "ui.founder.confirm");
    }

    /// <summary>Chrome ships at an integer multiple of its slot and is drawn down into it (D-76).</summary>
    private void DrawEntry(ManifestLayout L, string id, Rect2I rect) =>
        DrawTextureRect(ScreenRouter.Instance.Textures.For(L.Entry(id)), new Rect2(rect.Position, rect.Size), false);

    /// <summary>Text at a slot's top-left; the scene decides where that is (D-77).</summary>
    private void Text(PixelFont font, string slot, string text, int size, Color color)
    {
        Vector2I p = _at.At(slot);
        font.Draw(this, p.X, p.Y, text, size, color);
    }

    /// <summary>"two Junior Developers", "a Junior Developer and a Recruiter" — the roster said out loud.</summary>
    private static string Roster(ContentDb db, string[] ids)
    {
        string[] names = ids.Select(id => db.Employees.First(e => e.Id == id).Name).ToArray();
        if (names.Length > 1 && names.Distinct().Count() == 1) return $"{Count(names.Length)} {names[0]}s";
        return string.Join(" and ", names);
    }

    private static string Count(int n) => n switch { 2 => "two", 3 => "three", 4 => "four", 5 => "five", _ => n.ToString() };

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter } && _selected.Length > 0) { ScreenRouter.Instance.StartRun(_selected); return; }
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
        foreach ((Rect2I rect, string id) in _tiles)
        {
            if (rect.HasPoint(p)) { _selected = id; QueueRedraw(); return; }
        }
        if (_at.Rect("confirm").HasPoint(p) && _selected.Length > 0) ScreenRouter.Instance.StartRun(_selected);
    }
}
