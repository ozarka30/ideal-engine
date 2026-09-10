using System;
using System.Collections.Generic;
using CompanyWars.Playback;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>The autopsy (GAME_DESIGN.md §19.3): banner, share timeline with a draggable playhead, per-floor bars, three findings, filter chips, the full ledger, CONTINUE.</summary>
public partial class AutopsyScreen : Node2D
{
    private SceneLayout _at = null!;
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
    private readonly Hits _hits = new();
    private bool _scrollDragging;
    private int _scrollDragStartY;
    private int _scrollDragStart;
    private List<LedgerEntry> _filtered = new();
    private string[] _findings = Array.Empty<string>();
    private FloorTotals[] _bars = Array.Empty<FloorTotals>();
    private UnitTotals[] _staff = Array.Empty<UnitTotals>();
    private bool _staffView;
    private long[] _timeline = Array.Empty<long>();

    public override void _Ready()
    {
        _at = new SceneLayout(this);
        _r = ScreenRouter.Instance;
        _view = _r.LastView ?? _r.Fight();
        _playhead = _view.Result.EndTick;
        _findings = Autopsy.Findings(_view, _r.NameA, _r.NameB);
        _bars = Autopsy.FloorBars(_view);
        _staff = Autopsy.UnitBars(_view, "A", 5);
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
        Rect2I banner = _at.Rect("banner");
        DrawRect(new Rect2(banner.Position, banner.Size), Tones.Fill("interface"));
        font.Draw(this, banner.Position.X + 8, banner.Position.Y + 4, Autopsy.ResultBanner(_view, _r.FightRound, "A"), font.Large, Tones.Text("interface"));
        font.Draw(this, banner.Position.X, banner.Position.Y + 8, $"{_r.NameA} vs {_r.NameB} · at {Autopsy.Seconds(_playhead)} share {_view.Share(_playhead) / 100}.{_view.Share(_playhead) % 100 / 10}%", font.Small, Tones.Hatch("interface"), HorizontalAlignment.Right, banner.Size.X - 8);

        // Timeline
        Rect2I tl = _at.Rect("timeline");
        DrawRect(new Rect2(tl.Position, tl.Size), Tones.Fill("interface"));
        int colW = tl.Size.X / Columns;
        long span = _view.Rules.QuarterTicks / Columns;
        for (int c = 0; c < Columns; c++)
        {
            long t = c * span;
            if (t > _view.Result.EndTick) break;
            int h = (int)(tl.Size.Y * _timeline[c] / _view.Rules.ShareTotal);
            DrawRect(new Rect2(tl.Position.X + c * colW, tl.Position.Y + tl.Size.Y - h, colW - 1, h), Tones.Fill(Ui.SideTone("A")));
        }
        foreach (long start in _view.Rules.MonthStart)
        {
            if (start == 0) continue;
            int x = tl.Position.X + (int)(start / span) * colW;
            DrawLine(new Vector2(x, tl.Position.Y), new Vector2(x, tl.Position.Y + tl.Size.Y), Tones.Border("interface"));
        }
        int px = tl.Position.X + (int)(_playhead / span) * colW;
        DrawLine(new Vector2(px, tl.Position.Y), new Vector2(px, tl.Position.Y + tl.Size.Y), Tones.Fill("invalid"), 1);

        // Floors, or the side's staff by output (D-68): the chart players sell by.
        Rect2I fl = _at.Rect("floors");
        DrawRect(new Rect2(fl.Position, fl.Size), Tones.Fill("interface"));
        _hits.Clear();
        int tabH = 16;
        var floorsTab = new Rect2I(fl.Position.X, fl.Position.Y, fl.Size.X / 2, tabH);
        var staffTab = new Rect2I(fl.Position.X + fl.Size.X / 2, fl.Position.Y, fl.Size.X - fl.Size.X / 2, tabH);
        Ui.Button(this, floorsTab, "FLOORS", _staffView ? "structure" : "operations");
        Ui.Button(this, staffTab, "STAFF", _staffView ? "operations" : "structure");
        _hits.Add(floorsTab, () => { _staffView = false; QueueRedraw(); }, "Output by floor, both sides");
        _hits.Add(staffTab, () => { _staffView = true; QueueRedraw(); }, "Your five employees with the most output");
        int rowH = (fl.Size.Y - tabH) / 5;
        if (_staffView)
        {
            long smax = 1;
            foreach (UnitTotals u in _staff) smax = Math.Max(smax, u.Total);
            for (int i = 0; i < 5; i++)
            {
                int y = fl.Position.Y + tabH + i * rowH;
                if (i >= _staff.Length) break;
                UnitTotals u = _staff[i];
                int barX = fl.Position.X + 2, barW = fl.Size.X - 4;
                font.Draw(this, barX, y + 1, $"{LiveLedger.FloorName(u.FloorIndex)} {Ui.Abbrev(u.Name, 18)}", font.Small, Tones.Text("interface"));
                DrawRect(new Rect2(barX, y + 11, (int)(barW * u.Total / smax), 6), Tones.Fill(Ui.SideTone("A")));
                font.Draw(this, barX, y + 1, u.Total.ToString(), font.Small, Tones.Text("interface"), HorizontalAlignment.Right, barW);
            }
            if (_staff.Length == 0) font.Draw(this, fl.Position.X + 2, fl.Position.Y + tabH + 2, "nobody dealt anything", font.Small, Tones.Hatch("interface"));
        }
        long max = 1;
        foreach (FloorTotals b in _bars) max = Math.Max(max, Math.Max(b.A, b.B));
        for (int i = 4; i >= 0 && !_staffView; i--)
        {
            FloorTotals b = _bars[i];
            int y = fl.Position.Y + tabH + (4 - i) * rowH;
            font.Draw(this, fl.Position.X + 2, y + 2, LiveLedger.FloorName(b.FloorIndex), font.Small, Tones.Text("interface"));
            int barX = fl.Position.X + 24;
            int barW = fl.Size.X - 28;
            int wA = (int)(barW * b.A / max), wB = (int)(barW * b.B / max);
            DrawRect(new Rect2(barX, y + 2, wA, 8), Tones.Fill(Ui.SideTone("A")));
            DrawRect(new Rect2(barX, y + 12, wB, 8), Tones.Fill(Ui.SideTone("B")));
            font.Draw(this, barX, y + 2, b.A.ToString(), font.Small, Tones.Text("interface"), HorizontalAlignment.Right, barW);
            font.Draw(this, barX, y + 12, b.B.ToString(), font.Small, Tones.Text("interface"), HorizontalAlignment.Right, barW);
        }

        // Findings
        Rect2I fd = _at.Rect("findings");
        DrawRect(new Rect2(fd.Position, fd.Size), Tones.Fill("interface"));
        int fy = fd.Position.Y + 2;
        for (int i = 0; i < _findings.Length; i++)
        {
            foreach (string line in Ui.Wrap(font, font.Small, _findings[i], fd.Size.X - 4, 3))
            {
                font.Draw(this, fd.Position.X + 2, fy, line, font.Small, Tones.Text("interface"));
                fy += LineH;
            }
            fy += LineH / 2;
        }

        // Filters
        Rect2I ft = _at.Rect("filters");
        DrawRect(new Rect2(ft.Position, ft.Size), Tones.Fill("interface"));
        int cx = ft.Position.X;
        void Chip(string label, bool active, Action toggle)
        {
            int w = Math.Max(16, font.Width(label, font.Small) + 6);
            var rect = new Rect2I(cx, ft.Position.Y, w, ft.Size.Y);
            Ui.Button(this, rect, label, active ? "operations" : "structure");
            _hits.Add(rect, () => { toggle(); Refilter(); QueueRedraw(); }, $"Filter: {label}");
            cx += w + 2;
        }
        Chip("ALL", _kinds.Count == 0 && _sides.Count == 0 && _floors.Count == 0, () => { _kinds.Clear(); _sides.Clear(); _floors.Clear(); });
        foreach (string k in Autopsy.KindFilters) Chip(k.ToUpperInvariant(), _kinds.Contains(k), () => Toggle(_kinds, k));
        cx += 6;
        foreach (string s in new[] { "A", "B" }) Chip(s, _sides.Contains(s), () => Toggle(_sides, s));
        cx += 6;
        foreach ((string label, long f) in new[] { ("G", 0L), ("1", 1L), ("2", 2L), ("3", 3L), ("B1", -1L) }) Chip(label, _floors.Contains(f), () => Toggle(_floors, f));

        // Ledger
        Rect2I lg = _at.Rect("ledger");
        DrawRect(new Rect2(lg.Position, lg.Size), Tones.Fill("interface"));
        int rows = lg.Size.Y / LineH;
        int selected = SelectedRow();
        if (!_scrollDragging && selected >= 0 && (selected < _scroll || selected >= _scroll + rows)) _scroll = Math.Max(0, selected - rows / 2);
        for (int i = 0; i < rows && _scroll + i < _filtered.Count; i++)
        {
            LedgerEntry e = _filtered[_scroll + i];
            int y = lg.Position.Y + i * LineH;
            if (_scroll + i == selected) DrawRect(new Rect2(lg.Position.X, y, lg.Size.X, LineH), Tones.Fill("operations"));
            font.Draw(this, lg.Position.X + 2, y, Row(e), font.Small, Tones.Fill(Tones.ForKind(e.Kind)).Lightened(0.35f));
        }
        // Scroll thumb on the right edge: where these rows sit within the filtered ledger.
        if (_filtered.Count > rows)
        {
            int trackH = lg.Size.Y;
            int thumbH = Math.Max(4, trackH * rows / _filtered.Count);
            int thumbY = lg.Position.Y + (trackH - thumbH) * _scroll / Math.Max(1, _filtered.Count - rows);
            DrawRect(new Rect2(lg.Position.X + lg.Size.X - 2, thumbY, 2, thumbH), Tones.Hatch("interface"));
        }
        // Footer in the free strip beside CONTINUE, never over a row.
        font.Draw(this, 216, 340, $"{_filtered.Count} entries · drag or wheel to scroll · drag the timeline", font.Small, Tones.Hatch("interface"), HorizontalAlignment.Left, 332);

        // Continue
        Rect2I ct = _at.Rect("continue");
        Ui.Button(this, ct, "CONTINUE", "operations");
        _hits.Add(ct, Continue, "Back to the build phase");
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

    /// <summary>Rows speak the ledger's language, in seconds, with only the numbers that moved (GAME_DESIGN.md §20).</summary>
    private string Row(LedgerEntry e)
    {
        string text = LiveLedger.Describe(_view, e);
        var parts = new List<string>();
        if (e.Kind is "status" or "retrigger" && e.TargetUnits.Length > 0 && _view.Unit(e.TargetUnits[0]) is UnitInfo t) text += $" → {t.Name}";
        if (e.GoodwillDelta != 0 && e.Kind != "regen" && e.Kind != "restore") parts.Add($"gw {e.GoodwillDelta:+#;-#;0}");
        if (e.CapDelta != 0) parts.Add($"cap {e.CapDelta:+#;-#;0}");
        if (e.ShareDelta != 0 && e.Kind is not ("push" or "anomaly")) parts.Add($"share {e.ShareDelta:+#;-#;0}");
        if (e.Depth > 0) parts.Add("retriggered");
        string side = e.SourceSide == "*" ? " " : e.SourceSide;
        return $"{Autopsy.Seconds(e.Tick),6} {side} {text}{(parts.Count > 0 ? " · " + string.Join(" ", parts) : string.Empty)}";
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        ManifestLayout L = _r.Layout;
        if (@event is InputEventMouseButton mb)
        {
            var p = new Vector2I((int)mb.Position.X, (int)mb.Position.Y);
            Rect2I tl = _at.Rect("timeline");
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed && tl.HasPoint(p)) { _dragging = true; Seek(p.X); }
                if (mb.Pressed && _at.Rect("ledger").HasPoint(p)) { _scrollDragging = true; _scrollDragStartY = p.Y; _scrollDragStart = _scroll; }
                if (!mb.Pressed) { _dragging = false; _scrollDragging = false; }
                if (mb.Pressed)
                {
                    Hits.Hit? hit = _hits.At(p);
                    if (hit != null) { hit.Value.Click(); return; }
                }
            }
            if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelDown) { _scroll = Math.Min(_scroll + 3, Math.Max(0, _filtered.Count - 1)); _playhead = _filtered.Count > 0 ? _filtered[_scroll].Tick : _playhead; QueueRedraw(); }
            if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelUp) { _scroll = Math.Max(0, _scroll - 3); _playhead = _filtered.Count > 0 ? _filtered[_scroll].Tick : _playhead; QueueRedraw(); }
        }
        if (@event is InputEventMouseMotion mm && _dragging) Seek((int)mm.Position.X);
        if (@event is InputEventMouseMotion mm2 && _scrollDragging)
        {
            _scroll = Math.Clamp(_scrollDragStart - ((int)mm2.Position.Y - _scrollDragStartY) / LineH, 0, Math.Max(0, _filtered.Count - 1));
            QueueRedraw();
        }
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape or Key.Enter }) Continue();
    }

    private void Continue()
    {
        if (_r.CurrentFight?.InRun == true) _r.AfterFight();
        else _r.Go("res://scenes/Picker.tscn");
    }

    private void Seek(int x)
    {
        Rect2I tl = _at.Rect("timeline");
        int colW = tl.Size.X / Columns;
        long span = _view.Rules.QuarterTicks / Columns;
        int col = Math.Clamp((x - tl.Position.X) / colW, 0, Columns - 1);
        _playhead = Math.Min(col * span, _view.Result.EndTick);
        QueueRedraw();
    }
}
