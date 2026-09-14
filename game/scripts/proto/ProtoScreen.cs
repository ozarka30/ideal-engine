using System;
using System.Collections.Generic;
using System.Linq;
using CompanyWars.Content;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game.Proto;

/// <summary>
/// The business-first prototype (docs/BUSINESS_TYPES.md, BUSINESS_FIRST.md): rooms are what you place, people
/// hire themselves a desk and walk, the quarter is two buildings running side by side for a shared market.
/// Both towers are drawn at half scale so an 8 x 4 floor fits; this is the "does a cutaway fit 640 x 360" mock.
/// ponytail: one screen for build and quarter, drawn immediate-mode; throwaway.
/// </summary>
public partial class ProtoScreen : Node2D
{
    private const int Tile = 16;                     // on screen; room art is 32 px per tile, drawn at half
    private const int FloorW = Tile * ProtoSim.GridW, FloorH = Tile * ProtoSim.GridH;
    private const int RoomOversample = 2;

    private ScreenRouter _r = null!;
    private ContentDb _db = null!;
    private ManifestLayout L = null!;
    private PixelFont _font = null!;
    private SceneLayout _at = null!;
    private readonly Hits _hits = new();
    private readonly Dictionary<string, SubViewport?> _views = new();

    private Firm _me = null!;
    private Firm _rival = null!;
    private Quarter? _q;
    private int _round = 1, _strikes;
    private bool _runOver, _sheet;
    private RoomKind? _carry;
    private bool _carryRot;
    private Room? _selected;
    private string _hint = string.Empty, _lastTap = string.Empty;
    private int _speed = 1;
    private double _acc;
    private double _time;
    private readonly List<string> _roundNotes = new();

    public override void _Ready()
    {
        _r = ScreenRouter.Instance;
        _db = _r.Content;
        L = _r.Layout;
        _font = _r.Font;
        _at = new SceneLayout(this);
        _me = ProtoSim.NewPlayer("Your Firm");
        _rival = ProtoSim.Rival(_round);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        if (_q == null || _q.Finished) { if (_q == null) QueueRedraw(); return; }
        if (_speed == 0) { while (!_q.Finished) _q.Step(); QueueRedraw(); return; }
        _acc += delta * Quarter.TicksPerSecond * _speed;
        while (_acc >= 1 && !_q.Finished) { _q.Step(); _acc -= 1; }
        QueueRedraw();
    }

    // ---------------------------------------------------------------- flow

    private void StartQuarter()
    {
        if (_q != null || _runOver) return;
        _carry = null; _selected = null; _hint = string.Empty; _sheet = false;
        _q = new Quarter(_me, _rival);
        _speed = 1;
        QueueRedraw();
    }

    private void Continue()
    {
        if (_q == null || !_q.Finished) return;
        bool won = _q.Winner == "A";
        if (!won) _strikes++;
        _roundNotes.Clear();
        _roundNotes.Add($"Q{_round}: {(won ? "WON" : _q.Winner == "draw" ? "DRAW" : "LOST")} ¥{_me.Revenue:N0} to ¥{_rival.Revenue:N0}");
        _roundNotes.AddRange(_me.EndRound(won, _round));
        _q = null;
        _round++;
        if (_strikes >= ProtoSim.Strikes || _round > ProtoSim.Rounds) { _runOver = true; QueueRedraw(); return; }
        _rival = ProtoSim.Rival(_round);
        QueueRedraw();
    }

    // ---------------------------------------------------------------- scenes (copied from BuildScreen, D-79)

    private SubViewport? SceneView(string path, Vector2I plan)
    {
        if (_views.TryGetValue(path, out SubViewport? found)) return found;
        if (!ResourceLoader.Exists(path)) { _views[path] = null; return null; }
        var vp = new SubViewport
        {
            Size = plan * RoomOversample,
            TransparentBg = true,
            Disable3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        var scene = GD.Load<PackedScene>(path).Instantiate<Node2D>();
        scene.Scale = new Vector2(RoomOversample, RoomOversample);
        if (scene.GetNodeOrNull("Guides") is CanvasItem guides) guides.Visible = false;
        vp.AddChild(scene);
        AddChild(vp);
        _views[path] = vp;
        return vp;
    }

    private Rect2I RoomRect(bool mine, Room room)
    {
        Vector2I o = FloorOrigin(mine, room.Floor);
        return new Rect2I(o.X + room.Col * Tile, o.Y + room.Row * Tile, room.W * Tile, room.H * Tile);
    }

    private void DrawRoom(bool mine, Room room, Color dim)
    {
        Rect2I rr = RoomRect(mine, room);
        RoomDef rdef = _db.Rooms.First(r => r.Id == room.Kind.Look);
        SubViewport? vp = SceneView($"res://scenes/rooms/{rdef.Id.Split('.')[1]}.tscn", L.Size(rdef.Tile));
        if (vp == null) { DrawRect(new Rect2(rr.Position, rr.Size), Tones.Fill("structure") * dim); }
        else if (!room.Rot)
        {
            Vector2I plan = L.Size(rdef.Tile);
            CompanyWars.Manifest.Overhang o = L.Entry(rdef.Tile).Overhang ?? new CompanyWars.Manifest.Overhang(0, 0, 0, 0);
            DrawTextureRect(vp.GetTexture(), new Rect2(rr.Position.X, rr.Position.Y - o.Top / 2, plan.X / 2, plan.Y / 2), false, dim);
        }
        else
        {
            // Turned a quarter clockwise about the room's top-right corner: the footprint alone, without its overhang.
            Vector2I plan = L.Size(rdef.Tile);
            CompanyWars.Manifest.Overhang o = L.Entry(rdef.Tile).Overhang ?? new CompanyWars.Manifest.Overhang(0, 0, 0, 0);
            var src = new Rect2(0, o.Top * RoomOversample, plan.X * RoomOversample, (plan.Y - o.Top) * RoomOversample);
            DrawSetTransform(new Vector2(rr.Position.X + rr.Size.X, rr.Position.Y), Mathf.Pi / 2, Vector2.One);
            DrawTextureRectRegion(vp.GetTexture(), new Rect2(0, 0, rr.Size.Y, rr.Size.X), src, dim);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
        Color label = Tones.Text("interface").Inverted() * dim;
        string name = Ui.Abbrev(room.Kind.Name, room.W * 3);
        _font.Draw(this, rr.Position.X + 1, rr.Position.Y - 1, name, _font.Small, label);
        if (room.HasDesks) _font.Draw(this, rr.Position.X + 1, rr.Position.Y + rr.Size.Y - 9, $"{room.Seated.Count}/{room.Kind.DeskCount(room.Fit)}", _font.Small, label);
        for (int i = 0; i < room.Fit; i++) DrawRect(new Rect2(rr.Position.X + rr.Size.X - (i + 1) * 4, rr.Position.Y + 1, 3, 3), Tones.Fill("support") * dim);
        if (room == _selected) DrawRect(new Rect2(rr.Position, rr.Size), Tones.Fill("operations"), false, 1);
    }

    // ---------------------------------------------------------------- drawing

    private Rect2I TowerRect(bool mine) => _at.Rect(mine ? "tower_a" : "tower_b");

    /// <summary>2F on top, G at the bottom, the way a building stands.</summary>
    private Vector2I FloorOrigin(bool mine, int floor)
    {
        Rect2I t = TowerRect(mine);
        return new Vector2I(t.Position.X, t.Position.Y + (2 - floor) * FloorH);
    }

    public override void _Draw()
    {
        _hits.Clear();
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        DrawTower(true);
        DrawTower(false);
        DrawTop();
        if (_sheet) DrawSheet();
        else if (_q == null) DrawShop();
        else DrawQuarterPanel();
        if (_runOver) DrawRunOver();
        DrawHint();
    }

    private void DrawTop()
    {
        Rect2I r = _at.Rect("lead_bar");
        long a = _q != null ? Quarter.Live(_q.A) : 0, b = _q != null ? Quarter.Live(_q.B) : 0;
        long share = a + b == 0 ? 500 : a * 1000 / (a + b);
        DrawRect(new Rect2(r.Position, r.Size), Tones.Fill("interface"));
        int aw = (int)(r.Size.X * share / 1000);
        DrawRect(new Rect2(r.Position.X, r.Position.Y, aw, r.Size.Y), Tones.Fill(Ui.SideTone("A")));
        DrawRect(new Rect2(r.Position.X + aw, r.Position.Y, r.Size.X - aw, r.Size.Y), Tones.Fill(Ui.SideTone("B")));
        DrawRect(new Rect2(r.Position, r.Size), Tones.Border("interface"), false);
        Color t = Tones.Text("interface").Inverted();
        _font.Draw(this, r.Position.X - 40, r.Position.Y + 2, $"¥{a / 1000:N0}", _font.Small, t, HorizontalAlignment.Right, 38);
        _font.Draw(this, r.Position.X + r.Size.X + 2, r.Position.Y + 2, $"¥{b / 1000:N0}", _font.Small, t);
        _font.Draw(this, 8, 6, $"{_me.Name} · Q{_round}/{ProtoSim.Rounds}", _font.Small, t);
        _font.Draw(this, 8, 16, $"strikes {_strikes}/{ProtoSim.Strikes} · Loyalty {_me.Loyalty}", _font.Small, t);
        _font.Draw(this, 472, 6, $"{_rival.Name}", _font.Small, t, HorizontalAlignment.Right, 160);
        _font.Draw(this, 472, 16, $"{OvertimeName(_rival.Overtime)}{(_rival.Party ? " · parties" : string.Empty)} · Loyalty {_rival.Loyalty}", _font.Small, t, HorizontalAlignment.Right, 160);
        // Below the towers: the staff line, which the tower has no room for at half scale.
        Rect2I ta = TowerRect(true), tb = TowerRect(false);
        int y = ta.Position.Y + ta.Size.Y + 4;
        foreach ((Firm f, Rect2I tr) in new[] { (_me, ta), (_rival, tb) })
        {
            int work = f.People.Count(p => p.Act == Act.Working), rest = f.People.Count(p => p.Act == Act.Resting), walk = f.People.Count(p => p.Act == Act.Walking);
            int burnt = f.People.Count(p => p.Act == Act.BurntOut), idle = f.People.Count(p => p.Act == Act.Idle);
            _font.Draw(this, tr.Position.X, y, $"{f.People.Count} staff · {work} working · {rest} resting", _font.Small, t);
            _font.Draw(this, tr.Position.X, y + 10, $"{walk} walking · {burnt} burnt out · {idle} idle", _font.Small, t);
        }
    }

    private static string OvertimeName(int ot) => ot switch { 0 => "overtime off", 1 => "overtime on", _ => "CRUNCH" };

    private void DrawTower(bool mine)
    {
        Firm f = mine ? _me : _rival;
        if (_q == null) f.Compute(discover: false);
        for (int floor = 0; floor <= 2; floor++)
        {
            Vector2I o = FloorOrigin(mine, floor);
            var frame = new Rect2I(o.X, o.Y, FloorW, FloorH);
            bool leased = f.Leased[floor];
            Color dim = leased ? Colors.White : new Color(1, 1, 1, 0.35f);
            DrawRect(new Rect2(frame.Position, frame.Size), leased ? Tones.Fill("interface").Darkened(0.35f) : Tones.Fill("structure").Darkened(0.3f));
            DrawRect(new Rect2(frame.Position, frame.Size), Tones.Border("structure"), false);
            _font.Draw(this, o.X + FloorW - 14, o.Y + FloorH - 9, ProtoSim.FloorName(floor), _font.Small, Tones.Hatch("structure"));
            if (!leased)
            {
                int cost = ProtoSim.LeaseCost[floor];
                if (mine && _q == null)
                {
                    _font.Draw(this, o.X, o.Y + 24, $"LEASE {ProtoSim.FloorName(floor)} · ¥{cost}", _font.Large, _me.Budget >= cost ? Tones.Text("structure").Inverted() : Tones.Hatch("structure"), HorizontalAlignment.Center, FloorW);
                    int fl = floor;
                    _hits.Add(frame, () => { if (_me.Budget < cost) { _hint = $"¥{cost} needed"; return; } _me.Budget -= cost; _me.Leased[fl] = true; QueueRedraw(); }, $"Lease {ProtoSim.FloorName(floor)} for ¥{cost}. Rooms go on it; wages are per person, not per floor.");
                }
                continue;
            }
            for (int row = 0; row < ProtoSim.GridH; row++)
            {
                for (int col = 0; col < ProtoSim.GridW; col++)
                {
                    var t = new Rect2I(o.X + col * Tile, o.Y + row * Tile, Tile, Tile);
                    DrawRect(new Rect2(t.Position, t.Size), Tones.Border("structure") * new Color(1, 1, 1, 0.35f), false);
                    if (mine && _q == null && f.RoomAt(floor, col, row) == null)
                    {
                        int fl = floor, c = col, rw = row;
                        _hits.Add(t, () => { if (_carry != null) PlaceAt(fl, c, rw); else { _selected = null; QueueRedraw(); } }, _carry != null ? $"Place {_carry.Name} with its top-left here" : "Empty tile. Pick a room on the right, then tap a tile.");
                    }
                }
            }
            foreach (Room room in f.Rooms.Where(x => x.Floor == floor))
            {
                DrawRoom(mine, room, dim);
                if (mine && _q == null)
                {
                    Room captured = room;
                    _hits.Add(RoomRect(mine, room), () => { if (_carry == null) { _selected = captured; QueueRedraw(); } else PlaceAt(captured.Floor, captured.Col, captured.Row); }, RoomHint(f, room));
                }
            }
            if (mine && _q == null && _carry != null)
            {
                Vector2 m = GetViewport().GetMousePosition();
                if (frame.HasPoint(new Vector2I((int)m.X, (int)m.Y)))
                {
                    int c = ((int)m.X - o.X) / Tile, rw = ((int)m.Y - o.Y) / Tile;
                    int cw = _carryRot ? _carry.H : _carry.W, ch = _carryRot ? _carry.W : _carry.H;
                    bool ok = Array.IndexOf(_carry.Floors, floor) >= 0 && f.Fits(cw, ch, floor, c, rw) == null;
                    DrawRect(new Rect2(o.X + c * Tile, o.Y + rw * Tile, cw * Tile, ch * Tile), Tones.Fill(ok ? "operations" : "invalid"), false, 2);
                }
            }
        }
        DrawSynergyLinks(mine, f);
        DrawPeople(mine, f);
    }

    private string RoomHint(Firm f, Room room)
    {
        RoomStats st = f.StatsOf(room);
        string active = st.Active.Count > 0 ? " · " + string.Join(", ", st.Active.Select(a => a.Name)) : string.Empty;
        int hum = f.Undiscovered(room);
        return $"{room.Kind.Name}{active}{(hum > 0 ? $" · ? {hum} undiscovered" : string.Empty)} · {room.Kind.Blurb}";
    }

    /// <summary>The notifier: a line between the rooms that form each active synergy, a glow on the room, and during the quarter its name floating over it in turn.</summary>
    private void DrawSynergyLinks(bool mine, Firm f)
    {
        float pulse = 0.55f + 0.45f * (float)Math.Sin(_time * 4);
        foreach (Room room in f.Rooms)
        {
            if (!f.Leased[room.Floor]) continue;
            RoomStats st = f.StatsOf(room);
            if (st.Active.Count == 0) continue;
            Rect2I rr = RoomRect(mine, room);
            var centre = new Vector2(rr.Position.X + rr.Size.X / 2f, rr.Position.Y + rr.Size.Y / 2f);
            bool bad = st.Active.Any(a => a.Id is "gossip") && st.Active.Count == 1;
            Color glow = Tones.Fill(bad ? "invalid" : "operations");
            glow.A = _q != null ? 0.35f + 0.5f * pulse : 0.8f;
            DrawRect(new Rect2(rr.Position, rr.Size), glow, false, 2);
            foreach (Synergy syn in st.Active)
            {
                foreach (Room partner in syn.Partners(f, room))
                {
                    if (!f.Leased[partner.Floor]) continue;
                    Rect2I pr = RoomRect(mine, partner);
                    var pc = new Vector2(pr.Position.X + pr.Size.X / 2f, pr.Position.Y + pr.Size.Y / 2f);
                    Color line = Tones.Hatch("operations");
                    line.A = _q != null ? 0.4f + 0.6f * pulse : 0.9f;
                    DrawLine(centre, pc, line, 2);
                    DrawCircle(pc, 2, line);
                }
            }
            if (_q != null && !_q.Finished)
            {
                // One name at a time, two seconds each, so a room with three combos shows all three in turn.
                int slot = (int)(_time / 2) % st.Active.Count;
                float phase = (float)(_time % 2) / 2f;
                Color c = Tones.Text("interface").Inverted();
                c.A = phase < 0.15f ? phase / 0.15f : phase > 0.8f ? (1 - phase) / 0.2f : 1f;
                int rise = (int)(6 * phase);
                _font.Draw(this, rr.Position.X - 16, rr.Position.Y - 10 - rise, st.Active[slot].Name, _font.Small, c, HorizontalAlignment.Center, rr.Size.X + 32);
            }
        }
    }

    private void DrawPeople(bool mine, Firm f)
    {
        foreach (Person p in f.People.OrderBy(p => p.Row))
        {
            (int floor, float col, float row) = Where(p);
            if (!f.Leased[floor]) continue;
            Vector2I o = FloorOrigin(mine, floor);
            int cx = o.X + (int)(col * Tile) + Tile / 2;
            int by = o.Y + (int)(row * Tile) + Tile - 1;
            string sprite = p.Role switch { Role.Sales => "emp.sales_rep", Role.Manager => "emp.middle_manager", _ => "emp.junior_dev" };
            var rect = new Rect2I(cx - Tile / 2, by - Tile, Tile, Tile);   // the 32 px sprite at half
            Color mod = Colors.White;
            if (p.Act == Act.BurntOut) mod = Tones.Fill("invalid").Lightened(0.3f);
            else if (p.Act == Act.Resting) mod = Tones.Fill("operations").Lightened(0.5f);
            else if (p.Act == Act.Idle) mod = new Color(1, 1, 1, 0.55f);
            DrawTextureRect(_r.Textures.For(L.Entry(sprite)), new Rect2(rect.Position, rect.Size), false, mod);
            int w = 8 * p.Stamina / 1_000_000;
            DrawRect(new Rect2(cx - 4, by, 8, 1), Tones.Fill("structure"));
            DrawRect(new Rect2(cx - 4, by, w, 1), p.Stamina < 300_000 ? Tones.Fill("invalid") : Tones.Fill("operations"));
            if (p.Act == Act.BurntOut) _font.Draw(this, cx - 6, rect.Position.Y - 7, "zz", _font.Small, Tones.Text("interface").Inverted());
            if (_q == null && mine) _hits.Add(rect, () => { }, $"{p.Name} · {p.Role} · skill {p.Skill} · stamina {p.Stamina / 10_000}% · morale {p.Morale / 10_000}% · {p.Tenure} quarters");
        }
    }

    /// <summary>Walks are drawn between tiles; a walk between floors goes to the landing, changes floor, then on.</summary>
    private static (int Floor, float Col, float Row) Where(Person p)
    {
        if (p.Act != Act.Walking || p.WalkTotal == 0) return (p.Floor, p.Col, p.Row);
        float t = 1f - (float)p.WalkLeft / p.WalkTotal;
        if (p.Floor == p.ToFloor) return (p.Floor, p.Col + (p.ToCol - p.Col) * t, p.Row + (p.ToRow - p.Row) * t);
        if (t < 0.5f) { float u = t * 2; return (p.Floor, p.Col * (1 - u), p.Row); }
        float v = (t - 0.5f) * 2;
        return (p.ToFloor, p.ToCol * v, p.Row + (p.ToRow - p.Row) * v);
    }

    private void PlaceAt(int floor, int col, int row)
    {
        if (_carry == null) return;
        string? why = _me.Place(_carry, floor, col, row, _carryRot);
        if (why != null) { _hint = why; QueueRedraw(); return; }
        _hint = string.Empty;
        _carry = null;
        _carryRot = false;
        QueueRedraw();
    }

    /// <summary>R: turn what is being carried, or the selected room in place.</summary>
    private void Rotate()
    {
        if (_q != null) return;
        if (_carry != null) { _carryRot = !_carryRot; _hint = string.Empty; }
        else if (_selected != null) { _hint = _me.Rotate(_selected) ?? string.Empty; }
        QueueRedraw();
    }

    private void DrawShop()
    {
        Rect2I p = _at.Rect("panel");
        DrawRect(new Rect2(p.Position, p.Size), Tones.Fill("interface"));
        DrawRect(new Rect2(p.Position, p.Size), Tones.Border("interface"), false);
        int x = p.Position.X + 6, y = p.Position.Y + 4;
        Color text = Tones.Text("interface"), muted = Tones.Muted("interface");
        _font.Draw(this, x, y, $"¥{_me.Budget} · staff {_me.People.Count} · desks {_me.DesksUsed}/{_me.Desks(false) + _me.Desks(true)} · wages ¥{_me.Wages}/qtr", _font.Small, text);
        y += 10;
        foreach (string n in _roundNotes) { _font.Draw(this, x, y, n, _font.Small, muted); y += 10; }
        y += 2;
        _font.Draw(this, x, y, "ROOMS · tap one, then a tile on your building", _font.Small, muted); y += 10;
        int bw = (p.Size.X - 16) / 2;
        for (int i = 0; i < ProtoSim.Catalogue.Length; i++)
        {
            RoomKind k = ProtoSim.Catalogue[i];
            var br = new Rect2I(x + (i % 2) * (bw + 4), y + (i / 2) * 16, bw, 14);
            bool turned = _carry == k && _carryRot;
            Ui.Button(this, br, $"{k.Name} {(turned ? k.H : k.W)}×{(turned ? k.W : k.H)} ¥{k.Cost}", _carry == k ? "operations" : "support", _me.Budget >= k.Cost);
            string printed = string.Join(" ", ProtoSim.Synergies.Where(s => !s.Hidden && s.Blurb.Contains(k.Name)).Select(s => s.Blurb));
            _hits.Add(br, () => { _carry = _carry == k ? null : k; _carryRot = false; _selected = null; QueueRedraw(); }, $"{k.Name} ({k.W}×{k.H}) · {k.Blurb} {printed} R turns it.");
        }
        y += 16 * ((ProtoSim.Catalogue.Length + 1) / 2) + 2;
        if (_carry != null || (_selected != null && _selected.Kind != ProtoSim.ReceptionKind))
        {
            var rot = new Rect2I(x, y, bw, 14);
            Ui.Button(this, rot, _carry != null ? $"ROTATE {_carry.Name} (R)" : $"ROTATE {_selected!.Kind.Name} (R)", "support");
            _hits.Add(rot, Rotate, _carry != null ? "Turn the room a quarter before placing it." : "Turn the room a quarter in place, if it still fits. Free during the build.");
            y += 18;
        }
        if (_selected != null)
        {
            RoomStats st = _me.StatsOf(_selected);
            var parts = new List<string>();
            if (_selected.HasDesks) parts.Add($"bills ×{st.Bill / 1000}.{st.Bill % 1000 / 10:D2} · tires ×{st.Drain / 1000}.{st.Drain % 1000 / 10:D2}");
            if (_selected.Kind.RestCap > 0) parts.Add($"rests {_selected.Kind.RestCap + st.RestCapBonus} · recovers ×{st.Recover / 1000}.{st.Recover % 1000 / 10:D2}");
            parts.AddRange(st.Active.Select(a => a.Name));
            int hum = _me.Undiscovered(_selected);
            if (hum > 0) parts.Add($"? {hum} undiscovered");
            foreach (string line in Ui.Wrap(_font, _font.Small, $"{_selected.Kind.Name}: {string.Join(" · ", parts)}", p.Size.X - 12, 2)) { _font.Draw(this, x, y, line, _font.Small, text); y += 10; }
        }
        if (_selected != null && _selected.Kind != ProtoSim.ReceptionKind)
        {
            Room s = _selected;
            bool canFit = s.Kind.MaxFit > s.Fit;
            var fit = new Rect2I(x, y, bw, 14);
            var dem = new Rect2I(x + bw + 4, y, bw, 14);
            Ui.Button(this, fit, canFit ? $"FIT OUT {s.Kind.Name} ¥{ProtoSim.FitCost} ({s.Fit}/{s.Kind.MaxFit})" : $"{s.Kind.Name} fully fitted", "operations", canFit && _me.Budget >= ProtoSim.FitCost);
            _hits.Add(fit, () => { if (!canFit || _me.Budget < ProtoSim.FitCost) return; _me.Budget -= ProtoSim.FitCost; s.Fit++; QueueRedraw(); }, s.Kind.Desks > 0 ? $"More desks in the same room: +{s.Kind.DesksPerFit}. Crowding makes everyone in it tire 25% faster per level." : "This room has no fit-out.");
            Ui.Button(this, dem, "DEMOLISH (no refund)", "invalid");
            _hits.Add(dem, () => { _me.Rooms.Remove(s); foreach (Person q in _me.People) if (q.Desk == s) q.Desk = null; _selected = null; QueueRedraw(); }, "Remove the room. Nothing back.");
            y += 18;
        }
        _font.Draw(this, x, y, "PEOPLE · they find their own desk; no desk, no work", _font.Small, muted); y += 10;
        var hires = new (string Label, Role Role)[] { ("WORKER", Role.Worker), ("SALES", Role.Sales), ("MANAGER", Role.Manager) };
        int hw = (p.Size.X - 20) / 3;
        for (int i = 0; i < hires.Length; i++)
        {
            (string label, Role role) = hires[i];
            int cost = ProtoSim.HireCost(role);
            var br = new Rect2I(x + i * (hw + 4), y, hw, 14);
            Ui.Button(this, br, $"HIRE {label} ¥{cost}", "support", _me.Budget >= cost);
            _hits.Add(br, () => { if (_me.Budget < cost) return; _me.Budget -= cost; _me.People.Add(ProtoSim.NewPerson(role)); QueueRedraw(); },
                role switch { Role.Worker => "Bills about ¥100 a month at a desk, more in Crunch. Wage ¥2 a quarter.", Role.Sales => "Bills a little less, but what sales bill counts double toward the market. Needs a Sales Floor desk.", _ => "Bills half; everyone at their desks' room works 20% harder and tires 20% faster." });
        }
        y += 18;
        int burnt = _me.People.Count(q => q.Stamina < 450_000);
        var lay = new Rect2I(x, y, bw, 14);
        var sheet = new Rect2I(x + bw + 4, y, bw, 14);
        Ui.Button(this, lay, $"LAY OFF THE TIRED ({burnt}) ¥{3 * burnt}", "invalid", burnt > 0 && _me.Budget >= 3 * burnt);
        _hits.Add(lay, () => { if (burnt == 0 || _me.Budget < 3 * burnt) return; _me.Budget -= 3 * burnt; _me.People.RemoveAll(q => q.Stamina < 450_000); QueueRedraw(); }, "Severance ¥3 each for everyone under 45% stamina. Fresh hires arrive rested.");
        Ui.Button(this, sheet, $"SYNERGY SHEET (S) · {_me.Discovered.Count + ProtoSim.Synergies.Count(s => !s.Hidden)}/{ProtoSim.Synergies.Length}", "support");
        _hits.Add(sheet, () => { _sheet = true; QueueRedraw(); }, "Every combo: the printed ones in full, the hidden ones as a hint until you find them.");
        y += 20;
        _font.Draw(this, x, y, "POLICY · the only orders you give", _font.Small, muted); y += 10;
        var ot = new Rect2I(x, y, bw, 14);
        var party = new Rect2I(x + bw + 4, y, bw, 14);
        Ui.Button(this, ot, OvertimeName(_me.Overtime).ToUpperInvariant(), _me.Overtime == 2 ? "invalid" : _me.Overtime == 1 ? "operations" : "support");
        _hits.Add(ot, () => { _me.Overtime = (_me.Overtime + 1) % 3; QueueRedraw(); }, "Off: rest at 30% stamina. On: bill ×1.25, tire ×1.5, rest at 15%, morale drains. Crunch: bill ×1.5, tire ×2.2, nobody rests, morale sinks fast.");
        Ui.Button(this, party, _me.Party ? "DRINKING PARTY: YES ¥10" : "DRINKING PARTY: NO", _me.Party ? "operations" : "support");
        _hits.Add(party, () => { _me.Party = !_me.Party; QueueRedraw(); }, "¥10 at the bell: +20% morale for everyone, −15% stamina the next morning.");
        y += 20;
        var ready = new Rect2I(x, y, p.Size.X - 12, 20);
        Ui.Button(this, ready, "READY · RUN THE QUARTER", "operations");
        _hits.Add(ready, StartQuarter, "Sixty seconds. Both buildings run; the one that bills more and wins more of the market takes the quarter.");
        y += 24;
        _font.Draw(this, x, y, $"Rival this quarter: {_rival.Name}, {_rival.People.Count} staff, {OvertimeName(_rival.Overtime)}.", _font.Small, muted); y += 10;
        foreach (string line in Ui.Wrap(_font, _font.Small, "Rooms that touch, and rooms stacked on the floor above or below, do things together. Lit rooms and lines show the combos you know; tap a room for what is humming under it.", p.Size.X - 12, 3)) { _font.Draw(this, x, y, line, _font.Small, muted); y += 10; }
    }

    /// <summary>The sheet: every synergy, printed ones in full, hidden ones as a hint until found.</summary>
    private void DrawSheet()
    {
        Rect2I p = _at.Rect("panel");
        DrawRect(new Rect2(p.Position, p.Size), Tones.Fill("interface"));
        DrawRect(new Rect2(p.Position, p.Size), Tones.Border("interface"), false);
        int x = p.Position.X + 6, y = p.Position.Y + 4;
        Color text = Tones.Text("interface"), muted = Tones.Muted("interface");
        _font.Draw(this, x, y, "SYNERGY SHEET", _font.Large, text); y += 18;
        foreach (Synergy s in ProtoSim.Synergies)
        {
            bool known = !s.Hidden || _me.Discovered.Contains(s.Id);
            string head = known ? s.Name + (s.Hidden ? " · found" : string.Empty) : "? ? ?";
            _font.Draw(this, x, y, head, _font.Small, known ? text : muted); y += 10;
            foreach (string line in Ui.Wrap(_font, _font.Small, known ? s.Blurb : s.Hint, p.Size.X - 20, 2)) { _font.Draw(this, x + 8, y, line, _font.Small, known ? text : muted); y += 10; }
            y += 2;
        }
        var back = new Rect2I(x, p.Position.Y + p.Size.Y - 20, p.Size.X - 12, 16);
        Ui.Button(this, back, "BACK (S)", "operations");
        _hits.Add(back, () => { _sheet = false; QueueRedraw(); }, "Back to the shop");
    }

    private void DrawQuarterPanel()
    {
        Quarter q = _q!;
        Rect2I p = _at.Rect("panel");
        DrawRect(new Rect2(p.Position, p.Size), Tones.Fill("interface"));
        DrawRect(new Rect2(p.Position, p.Size), Tones.Border("interface"), false);
        int x = p.Position.X + 4, y = p.Position.Y + 4;
        int month = Quarter.Month(Math.Min(q.Tick, Quarter.Ticks - 1));
        string phase = q.Finished ? "THE BELL" : month switch { 0 => "MONTH 1", 1 => "MONTH 2", _ => "CRUNCH ×2.5" };
        _font.Draw(this, x, y, $"{q.Tick / Quarter.TicksPerSecond,2}s · {phase} · market ¥{Quarter.PoolBase * Quarter.MonthMult[month] / 1_000_000} this month", _font.Small, Tones.Muted("interface"));
        y += 12;
        int lines = (p.Size.Y - 60) / 10;
        var visible = q.Ledger.Skip(Math.Max(0, q.Ledger.Count - lines)).ToList();
        foreach ((int tick, string side, string text, string kind) in visible)
        {
            Color c = Tones.Border(Tones.ForKind(kind));
            string line = $"{tick / Quarter.TicksPerSecond,3}s {side} {text}";
            _font.Draw(this, x, y, line.Length > 62 ? line[..62] : line, _font.Small, c);
            y += 10;
        }
        int cy = p.Position.Y + p.Size.Y - 18;
        if (q.Finished)
        {
            string res = q.Winner == "A" ? "WON" : q.Winner == "B" ? "LOST" : "DRAW";
            _font.Draw(this, x, cy - 14, $"Q{_round} · {res} · ¥{q.A.Revenue:N0} to ¥{q.B.Revenue:N0} · burnt out {q.A.People.Count(o => o.Slumped)} vs {q.B.People.Count(o => o.Slumped)}", _font.Small, Tones.Text("interface"));
            var cont = new Rect2I(x, cy, p.Size.X - 8, 16);
            Ui.Button(this, cont, "CONTINUE (Enter)", "operations");
            _hits.Add(cont, Continue, "Bank it and build for the next quarter.");
            return;
        }
        string[] labels = { "1×", "2×", "4×", "▸▸" };
        int cell = (p.Size.X - 8) / labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            int speed = i switch { 0 => 1, 1 => 2, 2 => 4, _ => 0 };
            var cr = new Rect2I(x + i * cell, cy, cell, 16);
            Ui.Button(this, cr, labels[i], _speed == speed ? "operations" : "interface");
            _hits.Add(cr, () => { _speed = speed; }, speed == 0 ? "Skip to the bell" : $"Play at {speed}×");
        }
    }

    private void DrawRunOver()
    {
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), new Color(0, 0, 0, 0.6f));
        Color t = Tones.Text("interface").Inverted();
        int y = 120;
        _font.Draw(this, 0, y, _strikes >= ProtoSim.Strikes ? "STRUCK OUT" : "TWELVE QUARTERS", _font.Title, t, HorizontalAlignment.Center, L.CanvasW); y += 40;
        _font.Draw(this, 0, y, $"won {_me.Wins} of {_round - 1} · burnt out {_me.Burnouts} · quit {_me.Quits} · headhunted {_me.Poached} · combos {_me.Discovered.Count}/{ProtoSim.Synergies.Count(s => s.Hidden)}", _font.Large, t, HorizontalAlignment.Center, L.CanvasW); y += 24;
        _font.Draw(this, 0, y, "Enter for the menu", _font.Small, t, HorizontalAlignment.Center, L.CanvasW);
    }

    private void DrawHint()
    {
        Rect2I h = _at.Rect("hint");
        DrawRect(new Rect2(h.Position, h.Size), Tones.Fill("interface"));
        string text = _hint;
        Color tone = _hint.Length > 0 ? Tones.Fill("invalid").Lightened(0.4f) : Tones.Text("interface");
        if (text.Length == 0)
        {
            Vector2 m = GetViewport().GetMousePosition();
            text = _hits.HintAt(new Vector2I((int)m.X, (int)m.Y));
            if (text.Length == 0) text = _lastTap;
        }
        _font.Draw(this, h.Position.X + 4, h.Position.Y + 4, text.Length > 118 ? text[..118] : text, _font.Small, tone);
    }

    // ---------------------------------------------------------------- input

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventMouseButton { Pressed: true } mb)
        {
            var p = new Vector2I((int)mb.Position.X, (int)mb.Position.Y);
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (_runOver) { _r.Go("res://scenes/Menu.tscn"); return; }
                Hits.Hit? best = _hits.At(p);
                if (best != null) { _lastTap = best.Value.Hint; _hint = string.Empty; best.Value.Click(); QueueRedraw(); }
            }
            else if (mb.ButtonIndex == MouseButton.Right) { _carry = null; _carryRot = false; _selected = null; QueueRedraw(); }
        }
        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Enter: case Key.KpEnter:
                    if (_runOver) _r.Go("res://scenes/Menu.tscn");
                    else if (_q == null) StartQuarter();
                    else if (_q.Finished) Continue();
                    break;
                case Key.S: _sheet = !_sheet; QueueRedraw(); break;
                case Key.R: Rotate(); break;
                case Key.Escape: if (_sheet) { _sheet = false; QueueRedraw(); } else if (_q == null) { _carry = null; _selected = null; QueueRedraw(); } else _r.Go("res://scenes/Menu.tscn"); break;
                case Key.Key1: _speed = 1; break;
                case Key.Key2: _speed = 2; break;
                case Key.Key4: _speed = 4; break;
                case Key.Space: if (_q != null && !_q.Finished) _speed = 0; break;
                default: break;
            }
        }
    }
}
