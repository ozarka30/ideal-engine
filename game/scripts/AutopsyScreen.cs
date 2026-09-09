using System;
using System.Collections.Generic;
using CompanyWars.Playback;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>The autopsy (GAME_DESIGN.md §19.3): banner, share timeline with a draggable playhead, per-floor bars, three findings, filter chips, the full ledger, CONTINUE.</summary>
public partial class AutopsyScreen : Node2D
{
    private const int Columns = 60;

    private ScreenRouter _r = null!;
    private MatchView _view = null!;
    private long _playhead;
    private int _scroll;
    private bool _dragging;
    private int _frames;
    private bool _captured;
    private readonly HashSet<string> _kinds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _sides = new(StringComparer.Ordinal);
    private readonly HashSet<long> _floors = new();
    private readonly List<(Rect2I Rect, Action Toggle)> _chips = new();
    private List<LedgerEntry> _filtered = new();
    private string[] _findings = Array.Empty<string>();
    private FloorTotals[] _bars = Array.Empty<FloorTotals>();
    private long[] _timeline = Array.Empty<long>();

    public override void _Ready()
    {
        _r = ScreenRouter.Instance;
        _view = _r.LastView ?? _r.Fight();
        _playhead = _view.Result.EndTick;
        _findings = Autopsy.Findings(_view, _r.RivalName(_r.RivalA), _r.RivalName(_r.RivalB));
        _bars = Autopsy.FloorBars(_view);
        _timeline = Autopsy.Timeline(_view, Columns);
        Refilter();
        if (_r.ScreenshotMode) _playhead = 400;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_r.ScreenshotMode) return;
        _frames++;
        if (_frames == 3 && !_captured) { _captured = true; _r.Capture(); }
    }

    private void Refilter()
    {
        _filtered = Autopsy.Filter(_view, _kinds, _sides, _floors);
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _filtered.Count - 1));
    }

    private int LineH => _r.Layout.Size("font.ui.8").Y;

    public override void _Draw()
    {
        ManifestLayout L = _r.Layout;
        PixelFont font = _r.Font;
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));

        // Banner
        Rect2I banner = L.Rect("ui.autopsy.banner");
        DrawRect(new Rect2(banner.Position, banner.Size), Tones.Fill("interface"));
        long round = Math.Max(_r.Content.Rival(_r.RivalA).Round, _r.Content.Rival(_r.RivalB).Round);
        font.Draw(this, banner.Position.X + 8, banner.Position.Y + 4, Autopsy.ResultBanner(_view, round, "A"), font.Large, Tones.Text("interface"));
        font.Draw(this, banner.Position.X, banner.Position.Y + 8, $"{_r.RivalName(_r.RivalA)} vs {_r.RivalName(_r.RivalB)} · seed {_r.Seed} · {_view.Result.StateHash}", font.Small, Tones.Hatch("interface"), HorizontalAlignment.Right, banner.Size.X - 8);

        // Timeline
        Rect2I tl = L.Rect("ui.autopsy.timeline");
        DrawRect(new Rect2(tl.Position, tl.Size), Tones.Fill("interface"));
        int colW = tl.Size.X / Columns;
        long span = _view.Rules.QuarterTicks / Columns;
        for (int c = 0; c < Columns; c++)
        {
            long t = c * span;
            if (t > _view.Result.EndTick) break;
            int h = (int)(tl.Size.Y * _timeline[c] / _view.Rules.ShareTotal);
            DrawRect(new Rect2(tl.Position.X + c * colW, tl.Position.Y + tl.Size.Y - h, colW - 1, h), Tones.Fill("people"));
        }
        foreach (long start in _view.Rules.MonthStart)
        {
            if (start == 0) continue;
            int x = tl.Position.X + (int)(start / span) * colW;
            DrawLine(new Vector2(x, tl.Position.Y), new Vector2(x, tl.Position.Y + tl.Size.Y), Tones.Border("interface"));
        }
        int px = tl.Position.X + (int)(_playhead / span) * colW;
        DrawLine(new Vector2(px, tl.Position.Y), new Vector2(px, tl.Position.Y + tl.Size.Y), Tones.Fill("invalid"), 1);
        font.Draw(this, tl.Position.X + 2, tl.Position.Y, $"share · {Autopsy.Seconds(_playhead)} · {_view.Share(_playhead) / 100}.{_view.Share(_playhead) % 100 / 10}%", font.Small, Tones.Text("interface"));

        // Floors
        Rect2I fl = L.Rect("ui.autopsy.floors");
        DrawRect(new Rect2(fl.Position, fl.Size), Tones.Fill("interface"));
        long max = 1;
        foreach (FloorTotals b in _bars) max = Math.Max(max, Math.Max(b.A, b.B));
        int rowH = fl.Size.Y / 5;
        for (int i = 4; i >= 0; i--)
        {
            FloorTotals b = _bars[i];
            int y = fl.Position.Y + (4 - i) * rowH;
            font.Draw(this, fl.Position.X + 2, y + 2, LiveLedger.FloorName(b.FloorIndex), font.Small, Tones.Text("interface"));
            int barX = fl.Position.X + 24;
            int barW = fl.Size.X - 28;
            DrawRect(new Rect2(barX, y + 2, (int)(barW * b.A / max), 8), Tones.Fill("people"));
            DrawRect(new Rect2(barX, y + 12, (int)(barW * b.B / max), 8), Tones.Fill("anomalous"));
            font.Draw(this, barX + 2, y + 2, b.A.ToString(), font.Small, Tones.Text("people"));
            font.Draw(this, barX + 2, y + 12, b.B.ToString(), font.Small, Tones.Text("anomalous"));
        }

        // Findings
        Rect2I fd = L.Rect("ui.autopsy.findings");
        DrawRect(new Rect2(fd.Position, fd.Size), Tones.Fill("interface"));
        int fy = fd.Position.Y + 2;
        for (int i = 0; i < _findings.Length; i++)
        {
            foreach (string line in Wrap(_findings[i], 44, 3))
            {
                font.Draw(this, fd.Position.X + 2, fy, line, font.Small, Tones.Text("interface"));
                fy += LineH;
            }
            fy += LineH / 2;
        }

        // Filters
        Rect2I ft = L.Rect("ui.autopsy.filters");
        DrawRect(new Rect2(ft.Position, ft.Size), Tones.Fill("interface"));
        _chips.Clear();
        int cx = ft.Position.X + 2;
        void Chip(string label, bool active, Action toggle)
        {
            int w = font.Width(label, font.Small) + 6;
            var rect = new Rect2I(cx, ft.Position.Y + 2, w, ft.Size.Y - 4);
            DrawRect(new Rect2(rect.Position, rect.Size), active ? Tones.Fill("operations") : Tones.Fill("structure"));
            font.Draw(this, cx + 3, ft.Position.Y + 3, label, font.Small, Tones.Text("interface"));
            _chips.Add((rect, toggle));
            cx += w + 3;
        }
        Chip("ALL", _kinds.Count == 0 && _sides.Count == 0 && _floors.Count == 0, () => { _kinds.Clear(); _sides.Clear(); _floors.Clear(); });
        foreach (string k in Autopsy.KindFilters) Chip(k.ToUpperInvariant(), _kinds.Contains(k), () => Toggle(_kinds, k));
        cx += 6;
        foreach (string s in new[] { "A", "B" }) Chip(s, _sides.Contains(s), () => Toggle(_sides, s));
        cx += 6;
        foreach ((string label, long f) in new[] { ("G", 0L), ("1", 1L), ("2", 2L), ("3", 3L), ("B1", -1L) }) Chip(label, _floors.Contains(f), () => Toggle(_floors, f));

        // Ledger
        Rect2I lg = L.Rect("ui.autopsy.ledger");
        DrawRect(new Rect2(lg.Position, lg.Size), Tones.Fill("interface"));
        int rows = lg.Size.Y / LineH;
        int selected = SelectedRow();
        if (selected >= 0 && (selected < _scroll || selected >= _scroll + rows)) _scroll = Math.Max(0, selected - rows / 2);
        for (int i = 0; i < rows && _scroll + i < _filtered.Count; i++)
        {
            LedgerEntry e = _filtered[_scroll + i];
            int y = lg.Position.Y + i * LineH;
            if (_scroll + i == selected) DrawRect(new Rect2(lg.Position.X, y, lg.Size.X, LineH), Tones.Fill("operations"));
            font.Draw(this, lg.Position.X + 2, y, Row(e), font.Small, Tones.Fill(Tones.ForKind(e.Kind)).Lightened(0.35f));
        }
        font.Draw(this, lg.Position.X, lg.Position.Y + lg.Size.Y - LineH, $"{_filtered.Count} entries · wheel to scroll · drag the timeline", font.Small, Tones.Hatch("interface"), HorizontalAlignment.Right, lg.Size.X - 2);

        // Continue
        Rect2I ct = L.Rect("ui.autopsy.continue");
        DrawRect(new Rect2(ct.Position, ct.Size), Tones.Fill("operations"));
        font.Draw(this, ct.Position.X, ct.Position.Y + 4, "CONTINUE", font.Small, Tones.Text("operations"), HorizontalAlignment.Center, ct.Size.X);
    }

    private static void Toggle<T>(HashSet<T> set, T item)
    {
        if (!set.Remove(item)) set.Add(item);
    }

    private int SelectedRow()
    {
        for (int i = 0; i < _filtered.Count; i++)
        {
            if (_filtered[i].Tick >= _playhead) return i;
        }
        return _filtered.Count - 1;
    }

    private string Row(LedgerEntry e)
    {
        string src = e.SourceUnit >= 0 && _view.Unit(e.SourceUnit) is UnitInfo u ? $"{LiveLedger.FloorName(u.Unit.FloorIndex)} {u.Name}" : e.AbilityId;
        string tgt = e.TargetUnits.Length > 0 && _view.Unit(e.TargetUnits[0]) is UnitInfo t ? t.Name : e.TargetSide;
        string tags = e.Tags.Length > 0 ? " [" + string.Join(",", e.Tags) + "]" : string.Empty;
        return $"{e.Tick,4} {e.SourceSide} {e.Kind,-9} {src} → {tgt} raw {e.Raw} gw {e.GoodwillDelta} cap {e.CapDelta} sh {e.ShareDelta}{(e.Stacks != 0 ? $" st {e.Stacks}" : string.Empty)}{(e.Depth > 0 ? " d1" : string.Empty)}{tags}";
    }

    private static IEnumerable<string> Wrap(string text, int width, int maxLines)
    {
        var lines = new List<string>();
        string current = string.Empty;
        foreach (string word in text.Split(' '))
        {
            if (current.Length + word.Length + 1 > width && current.Length > 0)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = current.Length == 0 ? word : current + " " + word;
            }
        }
        if (current.Length > 0) lines.Add(current);
        return lines.GetRange(0, Math.Min(maxLines, lines.Count));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        ManifestLayout L = _r.Layout;
        if (@event is InputEventMouseButton mb)
        {
            var p = new Vector2I((int)mb.Position.X, (int)mb.Position.Y);
            Rect2I tl = L.Rect("ui.autopsy.timeline");
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed && tl.HasPoint(p)) { _dragging = true; Seek(p.X); }
                if (!mb.Pressed) _dragging = false;
                if (mb.Pressed && L.Rect("ui.autopsy.continue").HasPoint(p)) { _r.Go("res://scenes/Picker.tscn"); return; }
                if (mb.Pressed)
                {
                    foreach ((Rect2I rect, Action toggle) in _chips)
                    {
                        if (rect.HasPoint(p)) { toggle(); Refilter(); QueueRedraw(); return; }
                    }
                }
            }
            if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelDown) { _scroll = Math.Min(_scroll + 3, Math.Max(0, _filtered.Count - 1)); _playhead = _filtered.Count > 0 ? _filtered[_scroll].Tick : _playhead; QueueRedraw(); }
            if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelUp) { _scroll = Math.Max(0, _scroll - 3); _playhead = _filtered.Count > 0 ? _filtered[_scroll].Tick : _playhead; QueueRedraw(); }
        }
        if (@event is InputEventMouseMotion mm && _dragging) Seek((int)mm.Position.X);
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape or Key.Enter }) _r.Go("res://scenes/Picker.tscn");
    }

    private void Seek(int x)
    {
        Rect2I tl = _r.Layout.Rect("ui.autopsy.timeline");
        int colW = tl.Size.X / Columns;
        long span = _view.Rules.QuarterTicks / Columns;
        int col = Math.Clamp((x - tl.Position.X) / colW, 0, Columns - 1);
        _playhead = Math.Min(col * span, _view.Result.EndTick);
        QueueRedraw();
    }
}
