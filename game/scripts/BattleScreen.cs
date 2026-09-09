using System;
using System.Collections.Generic;
using CompanyWars.Content;
using CompanyWars.Manifest;
using CompanyWars.Playback;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// The battle screen (GAME_DESIGN.md §19.2, D-35, D-08): two greybox towers, window bursts and floating numbers,
/// the Market Share bar, both Goodwill bars with eroding frames, the month banner, founder badges, the live
/// ledgers, the floor inset on hover, playback controls and the result banner. Everything is a view over the
/// finished MatchResult at the playback tick; nothing here computes what the sim did not.
/// </summary>
public partial class BattleScreen : Node2D
{
    private const long ScreenshotTick = 400;

    private MatchView _view = null!;
    private PlaybackClock _clock = null!;
    private ScreenRouter _r = null!;
    private int _frames;
    private bool _captured;
    private int _hoverFloor = int.MinValue;
    private string _hoverSide = "A";

    public override void _Ready()
    {
        _r = ScreenRouter.Instance;
        _view = _r.Fight();
        _clock = new PlaybackClock(_view.Rules.TicksPerSecond, _view.Result.EndTick);
        if (_r.ScreenshotMode) _clock.Seek(ScreenshotTick);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_r.ScreenshotMode)
        {
            _frames++;
            if (_frames == 3 && !_captured) { _captured = true; _r.Capture(); }
            return;
        }
        long micros = (long)Math.Round(delta * 1_000_000);
        _clock.Advance(micros);
        UpdateHover();
        QueueRedraw();
    }

    private void UpdateHover()
    {
        Vector2 m = GetViewport().GetMousePosition();
        var p = new Vector2I((int)m.X, (int)m.Y);
        _hoverFloor = int.MinValue;
        foreach (string side in new[] { "A", "B" })
        {
            for (int f = -1; f <= 3; f++)
            {
                if (SegmentRect(side, f).HasPoint(p)) { _hoverFloor = f; _hoverSide = side; return; }
            }
        }
    }

    // ---------------------------------------------------------------- geometry from the manifest

    private Rect2I SegmentRect(string side, int floor)
    {
        Rect2I baseRect = _r.Layout.Side("fx.tower.floor_segment", side);
        int cx = baseRect.Position.X + baseRect.Size.X / 2;
        int baseY = baseRect.Position.Y + baseRect.Size.Y; // the anchor: bottom-centre of G
        if (floor < 0) return _r.Layout.At("fx.tower.basement", cx, baseY);
        int h = baseRect.Size.Y;
        return _r.Layout.At("fx.tower.floor_segment", cx, baseY - h * floor);
    }

    private static bool HasFloor(TowerSnapshot s, int index)
    {
        foreach (SnapshotFloor f in s.Floors)
        {
            if (f.Index == index) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- drawing

    public override void _Draw()
    {
        long tick = _clock.Tick;
        ManifestLayout L = _r.Layout;
        PixelFont font = _r.Font;

        DrawTextureRect(_r.Textures.For(L.Entry("bg.battle.street")), new Rect2(L.Rect("bg.battle.street").Position, L.Rect("bg.battle.street").Size), false);

        DrawTower("A", _view.SnapshotA, tick);
        DrawTower("B", _view.SnapshotB, tick);
        DrawBursts(tick);
        DrawShareBar(tick);
        DrawGoodwill("A", _view.FrameA(tick), _view.CapAtStartA, tick);
        DrawGoodwill("B", _view.FrameB(tick), _view.CapAtStartB, tick);
        DrawBanner(tick);
        DrawFounder("A", _view.SnapshotA.Globals.FounderId, _r.NameA);
        DrawFounder("B", _view.SnapshotB.Globals.FounderId, _r.NameB);
        DrawLedger("A", tick);
        DrawLedger("B", tick);
        DrawControls();
        if (_hoverFloor != int.MinValue) DrawInset(_hoverSide, _hoverFloor, tick);
        if (_clock.Finished) DrawResult();
        _ = font;
    }

    private void DrawTower(string side, TowerSnapshot snap, long tick)
    {
        ManifestLayout L = _r.Layout;
        for (int f = 0; f <= 3; f++)
        {
            string id = HasFloor(snap, f) ? "fx.tower.floor_segment" : "fx.tower.floor_segment_empty";
            Rect2I rect = SegmentRect(side, f);
            DrawTextureRect(_r.Textures.For(L.Entry(id)), new Rect2(rect.Position, rect.Size), false);
            _r.Font.Draw(this, rect.Position.X + 2, rect.Position.Y + 2, LiveLedger.FloorName(f), _r.Font.Small, Tones.Text("structure"));
        }
        Rect2I top = SegmentRect(side, 3);
        Rect2I roof = L.At("fx.tower.roof", top.Position.X + top.Size.X / 2, top.Position.Y);
        DrawTextureRect(_r.Textures.For(L.Entry("fx.tower.roof")), new Rect2(roof.Position, roof.Size), false);
        if (snap.Globals.LeasedB1)
        {
            Rect2I b1 = SegmentRect(side, -1);
            DrawTextureRect(_r.Textures.For(L.Entry("fx.tower.basement")), new Rect2(b1.Position, b1.Size), false);
        }
        _ = tick;
    }

    private void DrawBursts(long tick)
    {
        ManifestLayout L = _r.Layout;
        long life = _view.Rules.TicksPerSecond; // 20 ticks
        foreach (LedgerEntry e in _view.EntriesBetween(tick - life + 1, tick))
        {
            if (e.SourceUnit < 0 || _view.Unit(e.SourceUnit) is not UnitInfo u) continue;
            if (e.Kind is not ("push" or "anomaly" or "restore" or "status" or "retrigger" or "morale")) continue;
            if (Array.IndexOf(e.Tags, "self_cost") >= 0) continue;
            long age = tick - e.Tick;
            Rect2I seg = SegmentRect(e.SourceSide, (int)u.Unit.FloorIndex);
            int cx = seg.Position.X + seg.Size.X / 2;
            int cy = seg.Position.Y + seg.Size.Y / 2;
            Rect2I burst = L.At("fx.window_burst", cx, cy);
            Color tone = Tones.Fill(Tones.ForKind(e.Kind));
            tone.A = 1f - (float)age / life;
            DrawTextureRect(_r.Textures.For(L.Entry("fx.window_burst")), new Rect2(burst.Position, burst.Size), false, tone);
            if (e.Raw > 0 && e.Kind is "push" or "anomaly" or "restore")
            {
                int rise = (int)(16 * age / life);
                Rect2I num = L.At("fx.floating_number", cx, cy - rise);
                _r.Font.Draw(this, num.Position.X, num.Position.Y, (e.Kind == "restore" ? "+" : "-") + e.Raw, _r.Font.Small, Tones.Text("interface"), HorizontalAlignment.Center, num.Size.X);
            }
        }
    }

    private void DrawShareBar(long tick)
    {
        Rect2I r = _r.Layout.Rect("ui.battle.bar");
        long share = _view.Share(tick);
        long total = _view.Rules.ShareTotal;
        DrawRect(new Rect2(r.Position, r.Size), Tones.Fill("interface"));
        int aWidth = (int)(r.Size.X * share / total);
        DrawRect(new Rect2(r.Position.X, r.Position.Y, aWidth, r.Size.Y), Tones.Fill("people"));
        DrawRect(new Rect2(r.Position.X + aWidth, r.Position.Y, r.Size.X - aWidth, r.Size.Y), Tones.Fill("anomalous"));
        for (int i = 1; i < 10; i++)
        {
            int x = r.Position.X + r.Size.X * i / 10;
            DrawLine(new Vector2(x, r.Position.Y), new Vector2(x, r.Position.Y + r.Size.Y), Tones.Border("interface"));
        }
        DrawRect(new Rect2(r.Position, r.Size), Tones.Border("interface"), false);
        _r.Font.Draw(this, r.Position.X - 36, r.Position.Y + 2, Percent(share, total), _r.Font.Small, Tones.Text("interface").Inverted(), HorizontalAlignment.Right, 34);
        _r.Font.Draw(this, r.Position.X + r.Size.X + 2, r.Position.Y + 2, Percent(total - share, total), _r.Font.Small, Tones.Text("interface").Inverted());
    }

    private static string Percent(long v, long total) => $"{v * 1000 / total / 10}.{v * 1000 / total % 10}%";

    private void DrawGoodwill(string side, FirmFrame f, long capAtStart, long tick)
    {
        Rect2I r = _r.Layout.Side("ui.battle.goodwill_bar", side);
        int frameW = (int)(r.Size.X * f.Cap / Math.Max(1, capAtStart));
        int fillW = (int)(r.Size.X * f.Goodwill / Math.Max(1, capAtStart));
        Color fill = Tones.Fill("operations");
        Color frame = Tones.Border("interface");
        Color text = Tones.Text("interface").Inverted();
        if (f.Suppressed) { fill.A = 0.5f; }
        long breakTick = _view.BreakTick(side);
        bool flash = breakTick >= 0 && tick - breakTick < 20 && (tick - breakTick) / 5 % 2 == 0;
        if (flash) fill = Tones.Fill("invalid");
        DrawRect(new Rect2(r.Position, r.Size), Tones.Fill("structure"));
        if (side == "A")
        {
            DrawRect(new Rect2(r.Position.X, r.Position.Y, fillW, r.Size.Y), fill);
            DrawRect(new Rect2(r.Position.X, r.Position.Y, frameW, r.Size.Y), frame, false);
            _r.Font.Draw(this, r.Position.X + 4, r.Position.Y, f.Goodwill.ToString("N0"), _r.Font.Large, text);
        }
        else
        {
            DrawRect(new Rect2(r.Position.X + r.Size.X - fillW, r.Position.Y, fillW, r.Size.Y), fill);
            DrawRect(new Rect2(r.Position.X + r.Size.X - frameW, r.Position.Y, frameW, r.Size.Y), frame, false);
            _r.Font.Draw(this, r.Position.X, r.Position.Y, f.Goodwill.ToString("N0"), _r.Font.Large, text, HorizontalAlignment.Right, r.Size.X - 4);
        }
    }

    private void DrawBanner(long tick)
    {
        Rect2I r = _r.Layout.Rect("ui.battle.banner");
        long lead = _view.Rules.TicksPerSecond; // telegraphed one second early
        for (int m = 0; m < _view.Rules.MonthStart.Length; m++)
        {
            long start = _view.Rules.MonthStart[m];
            if (tick < start - lead || tick > start + 2 * lead) continue;
            string text = m switch { 0 => "— QUARTER OPEN —", 1 => "— MONTH 2 —", 2 => "— CRUNCH —", _ => "— THE BELL —" };
            Color c = Tones.Text("interface");
            Color bg = Tones.Fill("interface");
            if (tick < start) { c.A = 0.5f; bg.A = 0.5f; }
            DrawRect(new Rect2(r.Position, r.Size), bg);
            _r.Font.Draw(this, r.Position.X, r.Position.Y + 2, text, _r.Font.Small, c, HorizontalAlignment.Center, r.Size.X);
        }
    }

    private void DrawFounder(string side, string founderId, string firmName)
    {
        ManifestLayout L = _r.Layout;
        Rect2I frame = L.Side("ui.battle.founder", side);
        DrawTextureRect(_r.Textures.For(L.Entry("ui.battle.founder")), new Rect2(frame.Position, frame.Size), false);
        string badgeId = founderId + ".badge";
        Vector2I badge = L.Size(badgeId);
        var inner = new Rect2I(frame.Position.X + (frame.Size.X - badge.X) / 2, frame.Position.Y + (frame.Size.Y - badge.Y) / 2, badge.X, badge.Y);
        DrawTextureRect(_r.Textures.For(L.Entry(badgeId)), new Rect2(inner.Position, inner.Size), false);
        int nameY = frame.Position.Y + frame.Size.Y + 2;
        if (side == "A") _r.Font.Draw(this, frame.Position.X, nameY, firmName, _r.Font.Small, Tones.Text("interface").Inverted());
        else _r.Font.Draw(this, frame.Position.X + frame.Size.X - 120, nameY, firmName, _r.Font.Small, Tones.Text("interface").Inverted(), HorizontalAlignment.Right, 120);
    }

    private void DrawLedger(string side, long tick)
    {
        Rect2I r = _r.Layout.Side("ui.battle.ledger", side);
        int lineH = _r.Layout.Size("font.ui.8").Y;
        DrawRect(new Rect2(r.Position, r.Size), Tones.Fill("interface"));
        DrawRect(new Rect2(r.Position, r.Size), Tones.Border("interface"), false);
        FirmFrame f = side == "A" ? _view.FrameA(tick) : _view.FrameB(tick);
        string header = $"{side} · {(side == "A" ? _r.NameA : _r.NameB)} · GW {f.Goodwill}/{f.Cap}{(f.Broken ? " — GOODWILL BROKEN —" : string.Empty)}";
        _r.Font.Draw(this, r.Position.X + 2, r.Position.Y, header, _r.Font.Small, Tones.Hatch("interface"));
        int y = r.Position.Y + lineH;
        foreach (LedgerLine line in LiveLedger.Visible(_view, side, tick))
        {
            Color c = line.Rollup ? Tones.Hatch("interface") : Tones.Fill(Tones.ForKind(line.Kind)).Lightened(0.35f);
            _r.Font.Draw(this, r.Position.X + 2, y, $"{line.Tick / 20,3}s {line.Text}", _r.Font.Small, c);
            y += lineH;
        }
    }

    private void DrawControls()
    {
        Rect2I r = _r.Layout.Rect("ui.battle.controls");
        DrawRect(new Rect2(r.Position, r.Size), Tones.Fill("interface"));
        string[] labels = { "1×", "2×", "4×", "▸▸" };
        int cell = r.Size.X / labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            bool active = (i == 0 && _clock.Speed == 1) || (i == 1 && _clock.Speed == 2) || (i == 2 && _clock.Speed == 4);
            var cr = new Rect2(r.Position.X + i * cell, r.Position.Y, cell, r.Size.Y);
            if (active) DrawRect(cr, Tones.Fill("operations"));
            _r.Font.Draw(this, (int)cr.Position.X, r.Position.Y + 2, labels[i], _r.Font.Small, Tones.Text("interface"), HorizontalAlignment.Center, cell);
        }
    }

    private void DrawInset(string side, int floor, long tick)
    {
        ManifestLayout L = _r.Layout;
        TowerSnapshot snap = side == "A" ? _view.SnapshotA : _view.SnapshotB;
        SnapshotFloor? sf = null;
        foreach (SnapshotFloor f in snap.Floors)
        {
            if (f.Index == floor) sf = f;
        }
        if (sf == null) return;
        Rect2I seg = SegmentRect(side, floor);
        Vector2I size = L.Size("ui.battle.floor_inset");
        int x = Math.Clamp(seg.Position.X + seg.Size.X, 0, L.CanvasW - size.X);
        int y = Math.Clamp(seg.Position.Y - size.Y / 2, 0, L.CanvasH - size.Y);
        var inset = new Rect2I(x, y, size.X, size.Y);
        DrawTextureRect(_r.Textures.For(L.Entry("ui.battle.floor_inset")), new Rect2(inset.Position, inset.Size), false);
        int ox = x + 4, oy = y + 4, t = L.Tile;
        foreach (SnapshotRoom room in sf.Rooms)
        {
            var rr = new Rect2(ox + room.Rect[0] * t, oy + room.Rect[1] * t, room.Rect[2] * t, room.Rect[3] * t);
            DrawTextureRect(_r.Textures.For(L.Entry(_r.Content.Rooms.First(d => d.Id == room.DefId).Tile)), rr, true);
        }
        for (int gx = 0; gx <= sf.Grid.W; gx++) DrawLine(new Vector2(ox + gx * t, oy), new Vector2(ox + gx * t, oy + sf.Grid.H * t), Tones.Border("structure"));
        for (int gy = 0; gy <= sf.Grid.H; gy++) DrawLine(new Vector2(ox, oy + gy * t), new Vector2(ox + sf.Grid.W * t, oy + gy * t), Tones.Border("structure"));
        long lastFirer = -1, lastTick = -1;
        foreach (LedgerEntry e in _view.EntriesBetween(0, tick))
        {
            if (e.SourceUnit >= 0 && e.SourceSide == side && e.Kind != "status" && e.Tick >= lastTick) { lastFirer = e.SourceUnit; lastTick = e.Tick; }
        }
        foreach (SnapshotOccupant o in sf.Occupants)
        {
            string spriteId = o.Kind == "employee" ? _r.Content.Employees.First(d => d.Id == o.DefId).Sprite : _r.Content.Furniture.First(d => d.Id == o.DefId).Sprite;
            ManifestEntry e = L.Entry(spriteId);
            Rect2I rect = L.At(spriteId, ox + (int)o.Tile[0] * t + t / 2, oy + (int)(o.Tile[1] + 1) * t);
            DrawTextureRect(_r.Textures.For(e), new Rect2(rect.Position, rect.Size), false);
            if (lastFirer >= 0 && _view.Unit(lastFirer)?.Unit.InstanceId == o.InstanceId) DrawRect(new Rect2(rect.Position, rect.Size), Tones.Fill("invalid"), false, 1);
        }
    }

    private void DrawResult()
    {
        Rect2I r = _r.Layout.Rect("ui.battle.result");
        DrawRect(new Rect2(r.Position, r.Size), Tones.Fill("interface"));
        string text = Autopsy.ResultBanner(_view, _r.FightRound, "A") + $" · {Autopsy.Seconds(_view.Result.EndTick)} · click for the autopsy";
        _r.Font.Draw(this, r.Position.X, r.Position.Y + 4, text, _r.Font.Large, Tones.Text("interface"), HorizontalAlignment.Center, r.Size.X);
    }

    // ---------------------------------------------------------------- input

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Key1: _clock.SetSpeed(1); break;
                case Key.Key2: _clock.SetSpeed(2); break;
                case Key.Key4: _clock.SetSpeed(4); break;
                case Key.Space: _clock.Paused = !_clock.Paused; break;
                case Key.S: _clock.Skip(); break;
                case Key.Escape: case Key.Enter: _r.Go("res://scenes/Autopsy.tscn"); break;
                default: break;
            }
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            var p = new Vector2I((int)click.Position.X, (int)click.Position.Y);
            Rect2I c = _r.Layout.Rect("ui.battle.controls");
            if (c.HasPoint(p))
            {
                int cell = (p.X - c.Position.X) / (c.Size.X / 4);
                switch (cell)
                {
                    case 0: _clock.SetSpeed(1); break;
                    case 1: _clock.SetSpeed(2); break;
                    case 2: _clock.SetSpeed(4); break;
                    default: _clock.Skip(); break;
                }
            }
            else if (_clock.Finished)
            {
                _r.Go("res://scenes/Autopsy.tscn");
            }
        }
    }
}
