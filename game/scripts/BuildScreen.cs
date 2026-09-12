using System;
using System.Collections.Generic;
using System.Linq;
using CompanyWars.Build;
using CompanyWars.Content;
using CompanyWars.Manifest;
using CompanyWars.Sim;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// The build screen (GAME_DESIGN.md §19.1): top bar, the three-floor viewport with aura badges, link lines and room
/// signs, the shop with its tabs and lease buttons, the inspector or firm panel, the hint line. Input produces
/// actions; actions go to the reducer; the reducer produces state; the screen redraws (ARCHITECTURE.md §4.5).
/// </summary>
public partial class BuildScreen : Node2D
{
    private SceneLayout _at = null!;
    private enum CarryKind { None, StaffCard, RoomCard, FurnitureCard, Occupant, Room }

    private ScreenRouter _r = null!;
    private ContentDb _db = null!;
    private ManifestLayout L = null!;
    private PixelFont _font = null!;
    private string _tab = Shop.StaffTab;
    private long _selectedFloor = 1;
    private CarryKind _carry = CarryKind.None;
    private int _carryIndex = -1;
    private string _carryId = string.Empty;
    private string _hint = string.Empty;           // the reducer's refusal, cleared by the next action
    private string _lastTapHint = string.Empty;    // touch has no hover: the last tapped element's summary stays
    private string _selectedOccupant = string.Empty;
    private string _selectedRoom = string.Empty;
    private int _frames;
    private bool _captured;
    private int _flashFrames;
    private (long Floor, long Col, long Row) _flashTile;
    private readonly Hits _hits = new();

    /// <summary>
    /// Spawned room plans (D-79). A room is its scene, instantiated, not a baked texture: a half-scaled
    /// TileMapLayer inside it draws its 32px tiles across 32 device pixels at a 2x window, where a bake
    /// would have thrown half of them away first. The container sits behind the screen's own drawing,
    /// which is where sortBias -10 puts a room floor anyway.
    /// </summary>
    private readonly Dictionary<string, SubViewport?> _views = new();

    private BuildState State => _r.Building!;
    private RunState Run => State.Current;

    public override void _Ready()
    {
        _at = new SceneLayout(this);
        _r = ScreenRouter.Instance;
        _db = _r.Content;
        L = _r.Layout;
        _font = _r.Font;
        if (_r.Building == null) { _r.Go("res://scenes/Menu.tscn"); return; }
        _selectedFloor = _db.Floors.First(f => f.Id == _db.Economy.StartingRosterFloor).Index;
        _hint = _r.Run!.Round == 1 ? _db.Tutorial.Hints.FirstOrDefault(h => h.Round == 1)?.Text ?? string.Empty : string.Empty;
        if (_r.CurrentShot == "build_inspect")
        {
            // The fixture with the inspector open on a hire: the explainer is what a first-time player reads.
            _selectedOccupant = Run.Tower.Floors.SelectMany(f => f.Occupants).First(o => o.Kind == "employee").InstanceId;
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_flashFrames > 0) { _flashFrames--; QueueRedraw(); }
        if (!_r.ScreenshotMode) return;
        _frames++;
        if (_frames == 3 && !_captured) { _captured = true; _r.Capture(); }
    }

    // ---------------------------------------------------------------- actions

    private void Do(BuildAction action)
    {
        try
        {
            _r.Building = BuildReducer.Apply(_db, State, action);
            _hint = string.Empty;
        }
        catch (BuildException ex)
        {
            _hint = ex.Message;
            if (action is Hire h) Flash(h.Floor, h.Col, h.Row);
            else if (action is BuyRoom br) Flash(br.Floor, br.Col, br.Row);
            else if (action is BuyFurniture bf) Flash(bf.Floor, bf.Col, bf.Row);
            else if (action is Move mv) Flash(mv.Floor, mv.Col, mv.Row);
            else if (action is Relocate rl) Flash(rl.Floor, rl.Col, rl.Row);
        }
        QueueRedraw();
    }

    /// <summary>Illegal placement: the tile flashes the invalid tone (GAME_DESIGN.md §20). On touch this is the whole feedback.</summary>
    private void Flash(long floor, long col, long row)
    {
        _flashTile = (floor, col, row);
        _flashFrames = 30;
    }

    /// <summary>Would this placement be accepted? Asked of the reducer itself, so the preview and the rule never disagree.</summary>
    private bool WouldAccept(BuildAction action)
    {
        try
        {
            BuildReducer.Apply(_db, State, action);
            return true;
        }
        catch (BuildException)
        {
            return false;
        }
    }

    private BuildAction? CarryAction(long floor, long col, long row) => _carry switch
    {
        CarryKind.StaffCard => new Hire(_carryIndex, floor, col, row),
        CarryKind.RoomCard => new BuyRoom(_carryIndex, floor, col, row),
        CarryKind.FurnitureCard => new BuyFurniture(_carryIndex, floor, col, row),
        CarryKind.Occupant => new Move(_carryId, floor, col, row),
        CarryKind.Room => new Relocate(_carryId, floor, col, row),
        _ => null,
    };

    private (long W, long H) CarryFootprint()
    {
        switch (_carry)
        {
            case CarryKind.RoomCard: { RoomDef d = _db.Rooms.First(r => r.Id == Run.Shop.RoomCards[_carryIndex]); return (d.Footprint.W, d.Footprint.H); }
            case CarryKind.FurnitureCard: { FurnitureDef d = _db.Furniture.First(f => f.Id == Run.Shop.FurnitureCards[_carryIndex]); return (d.Footprint.W, d.Footprint.H); }
            case CarryKind.Room: { SnapshotRoom room = Run.Tower.Floors.SelectMany(f => f.Rooms).First(r => r.RoomId == _carryId); return (room.Rect[2], room.Rect[3]); }
            case CarryKind.Occupant: { (SnapshotFloor _, SnapshotOccupant o) = BuildReducer.Find(Run, _carryId); return Legality.Footprint(_db, o); }
            default: return (1, 1);
        }
    }

    private string CarryLabel()
    {
        switch (_carry)
        {
            case CarryKind.StaffCard: return _db.Employees.First(e => e.Id == Run.Shop.StaffCards[_carryIndex]).Name;
            case CarryKind.RoomCard: return _db.Rooms.First(r => r.Id == Run.Shop.RoomCards[_carryIndex]).Name;
            case CarryKind.FurnitureCard: return _db.Furniture.First(f => f.Id == Run.Shop.FurnitureCards[_carryIndex]).Name;
            case CarryKind.Occupant: { (SnapshotFloor _, SnapshotOccupant o) = BuildReducer.Find(Run, _carryId); return o.Kind == "employee" ? _db.Employees.First(e => e.Id == o.DefId).Name : _db.Furniture.First(f => f.Id == o.DefId).Name; }
            case CarryKind.Room: return _db.Rooms.First(r => r.Id == Run.Tower.Floors.SelectMany(f => f.Rooms).First(x => x.RoomId == _carryId).DefId).Name;
            default: return string.Empty;
        }
    }

    private void Undo()
    {
        _r.Building = BuildReducer.Undo(_db, State);
        _carry = CarryKind.None;
        QueueRedraw();
    }

    private void DropCarry()
    {
        _carry = CarryKind.None;
        _carryIndex = -1;
        _carryId = string.Empty;
        QueueRedraw();
    }

    // ---------------------------------------------------------------- geometry

    private static readonly string[] FloorSlots = { "floor_above", "floor_selected", "floor_below" };

    /// <summary>Where each of the three visible floors sits; the scene holds it, not a 104px stride (D-78).</summary>
    private int FloorSlotY(int slot) => _at.Rect(FloorSlots[slot]).Position.Y;

    /// <summary>
    /// A tile on the build grid. The row comes from the scene and the column from the shaft's right edge,
    /// so dragging either moves the grid with it. The 32px pitch is not a knob (BALANCE_PLAN.md §9).
    /// </summary>
    private Rect2I TileRect(int slot, long col, long row)
    {
        Rect2I shaft = _at.Rect("shaft");
        int x = shaft.Position.X + shaft.Size.X + (int)col * L.Tile;
        int y = FloorSlotY(slot) + (int)row * L.Tile;
        return new Rect2I(x, y, L.Tile, L.Tile);
    }

    private (int Slot, long Index)[] VisibleFloors()
    {
        return new[] { (0, _selectedFloor + 1), (1, _selectedFloor), (2, _selectedFloor - 1) };
    }


    // ---------------------------------------------------------------- spawned rooms (D-79)

    /// <summary>
    /// A room is its scene, live, not a baked PNG. The scene renders into a SubViewport at 2x its plan
    /// size and the screen draws that texture where the old bake went, which keeps the immediate-mode
    /// draw order intact -- a room has to sit above the floor void and below the people standing on it,
    /// and child nodes cannot be interleaved into a _Draw.
    ///
    /// 2x is what buys the resolution: a half-scaled TileMapLayer inside the scene puts its 32px tiles
    /// across 32 viewport pixels, where baking to the plan's own size would have thrown half of them
    /// away. At a 2x window the texture lands 1:1 on the display.
    /// </summary>
    private const int RoomOversample = 2;

    private SubViewport? RoomView(RoomDef rdef) => SceneView($"res://scenes/rooms/{rdef.Id.Split('.')[1]}.tscn", L.Size(rdef.Tile));

    /// <summary>A room's or a floor's scene in its own 2x viewport, made once per path; null when there is no scene.</summary>
    private SubViewport? SceneView(string path, Vector2I plan)
    {
        if (_views.TryGetValue(path, out SubViewport? found)) return found;
        if (!ResourceLoader.Exists(path)) { _views[path] = null; return null; }

        var vp = new SubViewport
        {
            Size = plan * RoomOversample,
            TransparentBg = true,
            Disable3D = true,
            // Always, not Once: the docs are explicit that UPDATE_ONCE renders a single frame and then
            // switches itself to UPDATE_DISABLED, so a viewport that ticks before its child scene is ready
            // would stay blank for good. The content is a few hundred pixels; redrawing it is free.
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        var scene = GD.Load<PackedScene>(path).Instantiate<Node2D>();
        scene.Scale = new Vector2(RoomOversample, RoomOversample);
        // A floor scene's painting guides are for the editor (tools/dev/floor_guides.py); the game never draws them.
        if (scene.GetNodeOrNull("Guides") is CanvasItem guides) guides.Visible = false;
        vp.AddChild(scene);
        AddChild(vp);
        _views[path] = vp;
        return vp;
    }

    /// <summary>The plan's origin sits `overhang.top` above the footprint, where its back-row fittings draw.</summary>
    private void DrawRoom(RoomDef rdef, Rect2I tile, Color dim)
    {
        SubViewport? vp = RoomView(rdef);
        if (vp == null) return;
        Vector2I plan = L.Size(rdef.Tile);
        Overhang o = L.Entry(rdef.Tile).Overhang ?? new Overhang(0, 0, 0, 0);
        DrawTextureRect(vp.GetTexture(), new Rect2(tile.Position.X, tile.Position.Y - o.Top, plan.X, plan.Y), false, dim);
    }

    /// <summary>A floor's look is its scene (D-84), rendered like a room's; the frame panel stands in for a floor without one.</summary>
    private void DrawFloor(FloorDef fd, Rect2I frame, Color dim)
    {
        // ponytail: every founder is the basic business; a founder's own folder, falling back to basic, once founders carry a business type.
        SubViewport? vp = SceneView($"res://scenes/floors/basic/{fd.Id.Split('.')[1]}.tscn", frame.Size);
        Texture2D look = vp != null ? vp.GetTexture() : _r.Textures.For(L.Entry("ui.build.floor_frame"));
        DrawTextureRect(look, new Rect2(frame.Position, frame.Size), false, dim);
    }

    /// <summary>Text at a slot the scene positions (D-77). Anything visible is editable in Godot.</summary>
    private void Text(string slot, string text) => Text(slot, text, Tones.Text("interface"));

    private void Text(string slot, string text, Color color)
    {
        Vector2I p = _at.At(slot);
        _font.Draw(this, p.X, p.Y, text, _font.Small, color);
    }

    // ---------------------------------------------------------------- drawing

    public override void _Draw()
    {
        if (_r.Building == null) return;
        _hits.Clear();
        DrawRect(new Rect2(0, 0, L.CanvasW, L.CanvasH), Tones.Fill("structure"));
        DrawTopBar();
        DrawTower();
        DrawShop();
        DrawInspector();
        DrawHint();
    }

    private void DrawTopBar()
    {
        Rect2I bar = _at.Rect("topbar");
        DrawRect(new Rect2(bar.Position, bar.Size), Tones.Fill("interface"));
        Mode mode = _db.Modes.First(m => m.Id == Run.Mode);
        Text("topbar_round", $"Q{Run.Round} · FIGHT {Run.Round}/{mode.Rounds}");
        Text("topbar_budget", $"¥ {Run.Budget}");
        Text("topbar_income", $"+¥{State.Income}{(State.Passives > 0 ? $"+{State.Passives}" : string.Empty)} −¥{State.Upkeep}/qtr{(State.UnpaidUpkeep > 0 ? $" ({State.UnpaidUpkeep} unpaid → Goodwill)" : string.Empty)}");
        for (int i = 0; i < mode.Strikes; i++)
        {
            var s = new Rect2(_at.X("strikes") + i * 10, _at.Y("strikes"), 8, 8);
            DrawRect(s, i < Run.Strikes ? Tones.Fill("people") : Tones.Fill("structure"));
            DrawRect(s, Tones.Border("interface"), false);
        }
        Rect2I ready = _at.Rect("ready");
        Ui.Button(this, ready, "READY", "operations");
        _hits.Add(ready, () => _r.ReadyUp(), "Commit the tower and fight. No confirmation; UNDO is always one press away.");
        // Touch and controller adapters (D-47, D-64): the keys Z and Esc as buttons, sized from the READY button.
        int half = ready.Size.X / 2;
        var undo = new Rect2I(ready.Position.X - half * 2 - 8, ready.Position.Y, half, ready.Size.Y);
        var drop = new Rect2I(ready.Position.X - half - 4, ready.Position.Y, half, ready.Size.Y);
        Ui.Button(this, undo, "UNDO", "support", State.Log.Length > 0);
        _hits.Add(undo, Undo, "Undo the last action (Z)");
        Ui.Button(this, drop, _carry == CarryKind.None ? "DROP" : "DROP " + Ui.Abbrev(CarryLabel(), 6), "support", _carry != CarryKind.None);
        _hits.Add(drop, DropCarry, "Put down what you are carrying (Esc, right-click)");
    }

    private void DrawTower()
    {
        Rect2I tower = _at.Rect("tower");
        Rect2I shaft = _at.Rect("shaft");
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.tower")), new Rect2(tower.Position, tower.Size), false);
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.shaft")), new Rect2(shaft.Position, shaft.Size), false);
        // Floor up and down as buttons at the shaft's ends, for touch (the wheel and the arrow keys do the same).
        var up = new Rect2I(shaft.Position.X, shaft.Position.Y + shaft.Size.Y - 2 * shaft.Size.X, shaft.Size.X, shaft.Size.X);
        var down = new Rect2I(shaft.Position.X, shaft.Position.Y + shaft.Size.Y - shaft.Size.X, shaft.Size.X, shaft.Size.X);
        Ui.Button(this, up, "▲", "support", _selectedFloor < 3);
        Ui.Button(this, down, "▼", "support", _selectedFloor > -1);
        _hits.Add(up, () => { if (_selectedFloor < 3) _selectedFloor++; QueueRedraw(); }, "Floor up (wheel, arrow up)");
        _hits.Add(down, () => { if (_selectedFloor > -1) _selectedFloor--; QueueRedraw(); }, "Floor down (wheel, arrow down)");
        foreach ((int slot, long index) in VisibleFloors())
        {
            int y = FloorSlotY(slot);
            var frame = new Rect2I(shaft.Position.X + shaft.Size.X, y, L.Size("ui.build.floor_frame").X, L.Size("ui.build.floor_frame").Y);
            bool selected = slot == 1;
            Color dim = selected ? Colors.White : new Color(1, 1, 1, 0.5f);
            if (index < -1 || index > 3) continue;
            _font.Draw(this, shaft.Position.X + 2, y + 2, Legality.FloorName(index), _font.Small, Tones.Text("structure"));
            var hit = new Rect2I(frame.Position, frame.Size);
            long captured = index;
            if (!Legality.HasFloor(Run.Tower, index))
            {
                // An unleased floor is its own lease button (D-83): the empty floor greyed under the widget's screen,
                // and a tag with the price over the upkeep it adds. Leasing it lifts the screen.
                FloorDef fd = _db.FloorByIndex(index);
                Rect2 tag(string slot) => new Rect2(frame.Position + LeaseTag.At(slot), LeaseTag.Rect(slot).Size);
                bool shut = fd.RequiresPortal && !Run.PortalOpen;
                DrawFloor(fd, frame, dim);
                DrawRect(new Rect2(frame.Position, frame.Size), LeaseTag.Tint("screen"));
                DrawTextureRect(_r.Textures.For(L.Entry("ui.build.lease_button")), tag("plate"), false);
                Rect2 name = tag("name"), price = tag("price_text"), upkeep = tag("upkeep");
                _font.Draw(this, (int)name.Position.X, (int)name.Position.Y, fd.Name, (int)name.Size.Y, Tones.Text("structure"));
                _font.Draw(this, (int)price.Position.X, (int)price.Position.Y, shut ? "LOCKED" : $"LEASE ¥{fd.Lease}", (int)price.Size.Y,
                    !shut && Run.Budget >= fd.Lease ? Tones.Text("structure") : Tones.Hatch("structure"), HorizontalAlignment.Center, (int)price.Size.X);
                _font.Draw(this, (int)upkeep.Position.X, (int)upkeep.Position.Y, shut ? "needs the portal" : $"−¥{fd.UpkeepBudget} upkeep/qtr", (int)upkeep.Size.Y,
                    Tones.Hatch("structure"), HorizontalAlignment.Center, (int)upkeep.Size.X);
                string floorId = fd.Id;
                _hits.Add(hit, () => { _selectedFloor = captured; Do(new Lease(floorId)); }, $"Lease {fd.Name} for ¥{fd.Lease}; then −¥{fd.UpkeepBudget} upkeep per round{(fd.RequiresPortal ? "; needs the portal" : string.Empty)}");
                continue;
            }
            if (!selected) _hits.Add(hit, () => { _selectedFloor = captured; QueueRedraw(); }, $"Select {Legality.FloorName(index)}");
            SnapshotFloor floor = Legality.Floor(Run.Tower, index);
            DrawFloor(_db.FloorByIndex(index), frame, dim);
            // void beyond the grid
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if (col < floor.Grid.W && row < floor.Grid.H) continue;
                    Rect2I t = TileRect(slot, col, row);
                    DrawTextureRect(_r.Textures.For(L.Entry("ui.build.floor_void")), new Rect2(t.Position, t.Size), false, dim);
                }
            }
            // rooms: tiles and signs
            foreach (SnapshotRoom room in floor.Rooms)
            {
                RoomDef rdef = _db.Rooms.First(r => r.Id == room.DefId);
                Rect2I tl = TileRect(slot, room.Rect[0], room.Rect[1]);
                var rr = new Rect2(tl.Position.X, tl.Position.Y, room.Rect[2] * L.Tile, room.Rect[3] * L.Tile);
                DrawRoom(rdef, tl, dim);
                if (room.RoomId == _selectedRoom && selected) DrawRect(rr, Tones.Fill("operations"), false, 1);
                Vector2I sign = L.Size("ui.room_sign");
                DrawTextureRect(_r.Textures.For(L.Entry("ui.room_sign")), new Rect2(tl.Position.X, tl.Position.Y, sign.X, sign.Y), false, dim);
                _font.Draw(this, tl.Position.X + 1, tl.Position.Y, Ui.Abbrev(rdef.Name, 6), _font.Small, Tones.Text("interface"));
                long tier = Overlays.Tier(_db, room);
                Vector2I pip = L.Size("ui.tenure_pip");
                for (int i = 0; i < tier; i++) DrawRect(new Rect2(tl.Position.X + sign.X - (i + 1) * (pip.X + 1), tl.Position.Y + 2, pip.X, pip.Y), Tones.Fill("support"));
                string roomId = room.RoomId;
                if (selected) _hits.Add(new Rect2I((int)rr.Position.X, (int)rr.Position.Y, (int)rr.Size.X, (int)rr.Size.Y), () => ClickTile(index, room.Rect[0], room.Rect[1]), $"{rdef.Name} · Tenure {room.TenureRounds} (Tier {Overlays.Tier(_db, room)}) · {rdef.Flavor}");
            }
            // grid, landing highlight, tiles
            for (long row = 0; row < floor.Grid.H; row++)
            {
                for (long col = 0; col < floor.Grid.W; col++)
                {
                    Rect2I t = TileRect(slot, col, row);
                    DrawRect(new Rect2(t.Position, t.Size), Tones.Border("structure") * dim, false);
                    if (col == 0) DrawLine(new Vector2(t.Position.X, t.Position.Y), new Vector2(t.Position.X, t.Position.Y + t.Size.Y), Tones.Fill("operations") * dim);
                    if (selected)
                    {
                        long c = col, rw = row;
                        _hits.Add(t, () => ClickTile(index, c, rw), HintForTile(floor, c, rw));
                    }
                }
            }
            // furniture links, occupants, badges
            foreach (SnapshotOccupant o in floor.Occupants.Where(o => o.Kind == "furniture"))
            {
                foreach (SnapshotOccupant e in Overlays.Linked(_db, floor, o))
                {
                    Rect2I a = TileRect(slot, o.Tile[0], o.Tile[1]);
                    Rect2I b = TileRect(slot, e.Tile[0], e.Tile[1]);
                    DrawLine(new Vector2(a.Position.X + L.Tile / 2, a.Position.Y + L.Tile / 2), new Vector2(b.Position.X + L.Tile / 2, b.Position.Y + L.Tile / 2), Tones.Fill("support") * dim);
                }
            }
            foreach (SnapshotOccupant o in floor.Occupants)
            {
                string spriteId = o.Kind == "employee" ? _db.Employees.First(d => d.Id == o.DefId).Sprite : _db.Furniture.First(d => d.Id == o.DefId).Sprite;
                Rect2I t = TileRect(slot, o.Tile[0], o.Tile[1]);
                (long fw, long fh) = Legality.Footprint(_db, o);
                Rect2I rect = L.At(spriteId, t.Position.X + (int)(fw * L.Tile) / 2, t.Position.Y + (int)(fh * L.Tile));
                Color mod = dim;
                if (_carry == CarryKind.Occupant && _carryId == o.InstanceId) mod = new Color(1, 1, 1, 0.4f);
                DrawTextureRect(_r.Textures.For(L.Entry(spriteId)), new Rect2(rect.Position, rect.Size), false, mod);
                if (o.InstanceId == _selectedOccupant && selected) DrawRect(new Rect2(rect.Position, rect.Size), Tones.Fill("operations"), false, 1);
                if (o.Kind == "employee")
                {
                    // The badge shows a room aura (GAME_DESIGN §20); the corridor penalty is the floor's business, not a badge.
                    long aura = Overlays.AuraPermille(_db, Run.Tower, floor, o);
                    if (aura != 1000 && Legality.RoomAt(floor, o.Tile[0], o.Tile[1]) != null)
                    {
                        Vector2I badge = L.Size("ui.aura_badge");
                        var br = new Rect2(t.Position.X + L.Tile - badge.X, t.Position.Y, badge.X, badge.Y);
                        DrawRect(br, Tones.Fill(aura > 1000 ? "operations" : "support") * dim);
                        _font.Draw(this, (int)br.Position.X, (int)br.Position.Y, Overlays.AuraBadge(aura), _font.Small, Tones.Text("interface") * dim);
                    }
                }
            }
            // Placement preview while carrying: the footprint at the hovered tile, in operations if the reducer would
            // accept it and invalid if it would not; and the refusal flash after a failed tap.
            if (selected && _carry != CarryKind.None)
            {
                Vector2 m = GetViewport().GetMousePosition();
                var mp = new Vector2I((int)m.X, (int)m.Y);
                for (long row = 0; row < floor.Grid.H; row++)
                {
                    for (long col = 0; col < floor.Grid.W; col++)
                    {
                        if (!TileRect(slot, col, row).HasPoint(mp)) continue;
                        BuildAction? a = CarryAction(index, col, row);
                        if (a == null) continue;
                        (long fw, long fh) = CarryFootprint();
                        Rect2I t0 = TileRect(slot, col, row);
                        var pr = new Rect2(t0.Position.X, t0.Position.Y, fw * L.Tile, fh * L.Tile);
                        DrawRect(pr, Tones.Fill(WouldAccept(a) ? "operations" : "invalid"), false, 2);
                    }
                }
            }
            if (selected && _flashFrames > 0 && _flashTile.Floor == index)
            {
                Rect2I ft = TileRect(slot, _flashTile.Col, _flashTile.Row);
                Color inv = Tones.Fill("invalid");
                inv.A = (_flashFrames / 5) % 2 == 0 ? 0.7f : 0.3f;
                DrawTextureRect(_r.Textures.For(L.Entry("ui.invalid_tile")), new Rect2(ft.Position, ft.Size), false, inv);
            }
        }
    }

    private string HintForTile(SnapshotFloor floor, long col, long row)
    {
        SnapshotOccupant? o = Legality.OccupantAt(_db, floor, col, row);
        if (o != null)
        {
            if (o.Kind == "employee")
            {
                EmployeeDef d = _db.Employees.First(e => e.Id == o.DefId);
                Effect ab = d.Effects.First(e => e.On == "ability");
                return $"{d.Name} · {d.Dept} T{d.Tier} · {ab.Name ?? ab.Do} every {d.CooldownTicks / 20}.{d.CooldownTicks % 20 / 2}s";
            }
            FurnitureDef f = _db.Furniture.First(e => e.Id == o.DefId);
            return $"{f.Name} · {f.Flavor}";
        }
        SnapshotRoom? room = Legality.RoomAt(floor, col, row);
        if (room != null)
        {
            RoomDef rd = _db.Rooms.First(r => r.Id == room.DefId);
            return $"{rd.Name} · Tenure {room.TenureRounds} (Tier {Overlays.Tier(_db, room)}) · {rd.Flavor}";
        }
        return col == 0 ? "Landing column: adjacent to the landing tiles above and below" : "Corridor: output ×0.9";
    }

    private void DrawShop()
    {
        Rect2I shop = _at.Rect("shop");
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.shop")), new Rect2(shop.Position, shop.Size), false);
        Vector2I tab = L.Size("ui.build.tab");
        string[] tabs = { Shop.StaffTab, Shop.RoomsTab, Shop.FurnitureTab };
        for (int i = 0; i < tabs.Length; i++)
        {
            var rect = new Rect2I(_at.X("tabs") + i * tab.X, _at.Y("tabs"), tab.X, tab.Y);
            bool active = tabs[i] == _tab;
            Ui.Button(this, rect, tabs[i].ToUpperInvariant(), active ? "operations" : "interface");
            string t = tabs[i];
            _hits.Add(rect, () => { _tab = t; DropCarry(); }, $"Show the {t} tab");
        }
        string[] cards = _tab == Shop.StaffTab ? Run.Shop.StaffCards : _tab == Shop.RoomsTab ? Run.Shop.RoomCards : Run.Shop.FurnitureCards;
        Vector2I card = L.Size("ui.card.applicant");
        for (int i = 0; i < cards.Length; i++)
        {
            var rect = new Rect2I(_at.X("cards") + i * (card.X + 4), _at.Y("cards"), card.X, card.Y);
            DrawCard(rect, cards[i], i);
        }
        Rect2I reroll = _at.Rect("reroll");
        long rerollCost = Economy.RerollCost(_db, Run.Tower);
        Ui.Button(this, reroll, $"REROLL {_tab.ToUpperInvariant()} · ¥{rerollCost}", "support", Run.Budget >= rerollCost);
        _hits.Add(reroll, () => Do(new Reroll(_tab)), "Replace this tab's cards from its bag; nothing repeats until the bag empties");
        Text("otherworld", "Otherworld Temp Agency · the portal is closed", Tones.Hatch("anomalous"));

        // The commit where the thumb already is on touch (D-68); the top-right READY stays for keyboard and mouse.
        // Floors are leased from the tower itself (D-83), so the shop no longer has a lease row above it.
        Rect2I readyShop = _at.Rect("ready_shop");
        Ui.Button(this, readyShop, "READY", "operations");
        _hits.Add(readyShop, () => _r.ReadyUp(), "Commit the tower and fight. No confirmation; UNDO is always one press away.");
    }

    private static SceneLayout Card => SceneLayout.Widget("res://scenes/widgets/Card.tscn");

    private static SceneLayout LeaseTag => SceneLayout.Widget("res://scenes/widgets/LeaseTag.tscn");

    private void DrawCard(Rect2I rect, string defId, int index)
    {
        Vector2I at(string slot) => rect.Position + Card.At(slot);
        Rect2 box(string slot) => new Rect2(at(slot), Card.Rect(slot).Size);
        string kindEntry = _tab == Shop.StaffTab ? "ui.card.applicant" : _tab == Shop.RoomsTab ? "ui.card.room" : "ui.card.furniture";
        bool carried = _carryIndex == index && (_carry is CarryKind.StaffCard or CarryKind.RoomCard or CarryKind.FurnitureCard);
        // The panel art has a soft margin, so the scene sizes it past the hit rect until its body fills the card.
        DrawTextureRect(_r.Textures.For(L.Entry(kindEntry)), box("bg"), false);
        // The sprite stands on a lighter stage, lit when the card is picked; the selector frames it at the end.
        DrawRect(box("stage"), carried ? Tones.Text("structure") : Tones.Hatch(L.Entry(kindEntry).Category));
        string name, cost, hint;
        if (_tab == Shop.StaffTab)
        {
            EmployeeDef d = _db.Employees.First(e => e.Id == defId);
            name = d.Name;
            cost = $"¥{Economy.EmployeePrice(_db, d)}";
            hint = $"{d.Name} · {Explain.Ability(_db, d)}";
            DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(at("sprite"), L.Size(d.Sprite)), false);
        }
        else if (_tab == Shop.RoomsTab)
        {
            RoomDef d = _db.Rooms.First(r => r.Id == defId);
            name = d.Name;
            cost = $"¥{d.Cost}";
            hint = $"{d.Name} · {Explain.Passives(_db, d.Effects).FirstOrDefault() ?? d.Flavor}";
            // The whole room, fitted to the stage and centred: a plan is larger than a card, so it is drawn down, never up.
            Rect2 stage = box("stage");
            Vector2 plan = L.Size(d.Tile);
            Vector2 fitted = plan * Math.Min(1f, Math.Min(stage.Size.X / plan.X, stage.Size.Y / plan.Y));
            DrawTextureRect(_r.Textures.For(L.Entry(d.Tile)), new Rect2(stage.Position + (stage.Size - fitted) / 2, fitted), false);
        }
        else
        {
            FurnitureDef d = _db.Furniture.First(f => f.Id == defId);
            name = d.Name;
            cost = $"¥{d.Cost}";
            hint = $"{d.Name} · {Explain.Passives(_db, d.Effects).FirstOrDefault() ?? d.Flavor}";
            DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(at("sprite"), L.Size(d.Sprite)), false);
        }
        // A card is its face, name and price (D-82); what it does is the inspector's once the card is picked.
        // The price sits on a rounded tag along the bottom edge. The figure has its own slot and is drawn in the
        // header face at that slot's height, so Card.tscn sizes the text independently of the tag.
        DrawTextureRect(_r.Textures.For(L.Entry("ui.card.price")), box("price"), false);
        Rect2 figure = box("price_text");
        _font.Draw(this, (int)figure.Position.X, (int)figure.Position.Y, cost, (int)figure.Size.Y, Tones.Text("structure"), HorizontalAlignment.Center, (int)figure.Size.X);
        // The name wraps to as many 8 px lines as its slot is tall, centred in the slot, rather than being cut.
        Rect2 label = box("name");
        List<string> lines = Ui.Wrap(_font, _font.Small, name, (int)label.Size.X, Math.Max(1, (int)label.Size.Y / _font.Small)).ToList();
        int ly = (int)label.Position.Y + ((int)label.Size.Y - lines.Count * _font.Small) / 2;
        foreach (string line in lines)
        {
            _font.Draw(this, (int)label.Position.X, ly, line, _font.Small, Tones.Text("interface"), HorizontalAlignment.Center, (int)label.Size.X);
            ly += _font.Small;
        }
        if (carried) DrawTextureRect(_r.Textures.For(L.Entry("ui.card.selector")), box("selector"), false);
        int i = index;
        _hits.Add(rect, () => PickCard(i), hint + " · tap the card to read more, then a tile to place");
    }

    private void PickCard(int index)
    {
        _carry = _tab == Shop.StaffTab ? CarryKind.StaffCard : _tab == Shop.RoomsTab ? CarryKind.RoomCard : CarryKind.FurnitureCard;
        _carryIndex = index;
        _selectedOccupant = string.Empty;
        _selectedRoom = string.Empty;
        QueueRedraw();
    }

    private void ClickTile(long floorIndex, long col, long row)
    {
        switch (_carry)
        {
            case CarryKind.StaffCard: Do(new Hire(_carryIndex, floorIndex, col, row)); if (_hint.Length == 0) DropCarry(); return;
            case CarryKind.RoomCard: Do(new BuyRoom(_carryIndex, floorIndex, col, row)); if (_hint.Length == 0) DropCarry(); return;
            case CarryKind.FurnitureCard: Do(new BuyFurniture(_carryIndex, floorIndex, col, row)); if (_hint.Length == 0) DropCarry(); return;
            case CarryKind.Occupant: Do(new Move(_carryId, floorIndex, col, row)); if (_hint.Length == 0) DropCarry(); return;
            case CarryKind.Room: Do(new Relocate(_carryId, floorIndex, col, row)); if (_hint.Length == 0) DropCarry(); return;
            default: break;
        }
        SnapshotFloor floor = Legality.Floor(Run.Tower, floorIndex);
        SnapshotOccupant? o = Legality.OccupantAt(_db, floor, col, row);
        if (o != null)
        {
            // First tap selects (the inspector opens); a second tap on the selected occupant picks it up.
            if (_selectedOccupant == o.InstanceId)
            {
                _carry = CarryKind.Occupant;
                _carryId = o.InstanceId;
                _hint = string.Empty;
            }
            else
            {
                _selectedOccupant = o.InstanceId;
                _selectedRoom = string.Empty;
            }
            QueueRedraw();
            return;
        }
        SnapshotRoom? room = Legality.RoomAt(floor, col, row);
        _selectedRoom = room?.RoomId ?? string.Empty;
        _selectedOccupant = string.Empty;
        QueueRedraw();
    }

    private void DrawInspector()
    {
        Rect2I panel = _at.Rect("inspector");
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.inspector")), new Rect2(panel.Position, panel.Size), false);
        // The inspector's insides are slots now (D-81); D-71 made the preview 96x96 and this
        // screen was still drawing every one of them at 64.
        Rect2I preview = _at.Rect("insp_preview");
        int x = preview.Position.X, y = preview.Position.Y;
        SnapshotOccupant? occ = null;
        SnapshotFloor? occFloor = null;
        foreach (SnapshotFloor f in Run.Tower.Floors)
        {
            occ = f.Occupants.FirstOrDefault(o => o.InstanceId == _selectedOccupant);
            if (occ != null) { occFloor = f; break; }
        }
        SnapshotRoom? room = Run.Tower.Floors.SelectMany(f => f.Rooms).FirstOrDefault(r => r.RoomId == _selectedRoom);
        Vector2I action = L.Size("ui.build.action_button");
        int actionY = _at.Y("insp_actions");
        int limitY = actionY - 10;
        if (_carry is CarryKind.StaffCard or CarryKind.RoomCard or CarryKind.FurnitureCard)
        {
            DrawCardInspector(panel, x, y, limitY);
        }
        else if (occ != null && occFloor != null)
        {
            if (occ.Kind == "employee")
            {
                EmployeeDef d = _db.Employees.First(e => e.Id == occ.DefId);
                DrawTextureRect(_r.Textures.For(L.Entry("ui.portrait")), new Rect2(preview.Position, preview.Size), false);
                DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(_at.Rect("insp_sprite").Position, L.Size(d.Sprite)), false);
                _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), d.Name, _font.Small, Tones.Text("interface"));
                _font.Draw(this, _at.X("insp_sub"), _at.Y("insp_sub"), $"{d.Dept} · tier {d.Tier}", _font.Small, Tones.Muted("interface"));
                int ry = _at.Y("insp_context");
                long aura = Overlays.AuraPermille(_db, Run.Tower, occFloor, occ);
                _font.Draw(this, x, ry, $"here: room {Overlays.AuraBadge(aura)} · floor ×{_db.FloorByIndex(occFloor.Index).OutputPermille / 1000}.{_db.FloorByIndex(occFloor.Index).OutputPermille % 1000 / 100}", _font.Small, Tones.Muted("interface"));
                ry += 14;
                DrawEmployeeExplanation(d, x, ref ry, limitY);
                long fee = Economy.Severance(_db, Run.Tower, d);
                var move = new Rect2I(x, actionY, action.X, action.Y);
                var btn = new Rect2I(x + action.X + 4, actionY, action.X, action.Y);
                string id = occ.InstanceId;
                bool carrying = _carry == CarryKind.Occupant && _carryId == id;
                Ui.Button(this, move, carrying ? "CARRYING" : "MOVE", "support", !carrying);
                _hits.Add(move, () => { _carry = CarryKind.Occupant; _carryId = id; QueueRedraw(); }, "Pick up, then tap a tile (or tap the person again)");
                Ui.Button(this, btn, $"LAY OFF ¥{fee}", "invalid");
                _hits.Add(btn, () => { Do(new LayOff(id)); DropCarry(); _selectedOccupant = string.Empty; }, $"Lay off for ¥{fee} severance. Refunds nothing.");
            }
            else
            {
                FurnitureDef d = _db.Furniture.First(e => e.Id == occ.DefId);
                DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(x, y, 32, 32), false);
                _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), d.Name, _font.Small, Tones.Text("interface"));
                int ry = _at.Y("insp_context");
                DrawLines(Explain.Passives(_db, d.Effects), x, ref ry, limitY, Tones.Text("interface"));
                ry += 4;
                DrawLines(new[] { d.Flavor ?? string.Empty }, x, ref ry, limitY, Tones.Hatch("interface"));
                var move = new Rect2I(x, actionY, action.X, action.Y);
                var btn = new Rect2I(x + action.X + 4, actionY, action.X, action.Y);
                string id = occ.InstanceId;
                Ui.Button(this, move, "MOVE", "support");
                _hits.Add(move, () => { _carry = CarryKind.Occupant; _carryId = id; QueueRedraw(); }, "Pick up, then tap a tile");
                Ui.Button(this, btn, $"SELL ¥{_db.Economy.FurnitureSellRefund}", "invalid");
                _hits.Add(btn, () => { Do(new Sell(id)); DropCarry(); _selectedOccupant = string.Empty; }, "Sell furniture. Refunds nothing, costs nothing.");
            }
        }
        else if (room != null)
        {
            RoomDef d = _db.Rooms.First(r => r.Id == room.DefId);
            DrawTextureRect(_r.Textures.For(L.Entry(d.Tile)), new Rect2(preview.Position, L.Size(d.Tile)), false);
            _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), d.Name, _font.Small, Tones.Text("interface"));
            _font.Draw(this, _at.X("insp_sub"), _at.Y("insp_sub"), $"Tenure {room.TenureRounds} · Tier {Overlays.Tier(_db, room)}", _font.Small, Tones.Muted("interface"));
            int ry = _at.Y("insp_context");
            DrawLines(Explain.Passives(_db, d.Effects), x, ref ry, Math.Min(limitY, _at.Y("insp_compare") - 8), Tones.Text("interface"));
            long fee = Economy.RenovationFee(_db, Run.Round);
            if (!d.Fixed)
            {
                DrawCompare(room, d, _at.X("insp_compare"), _at.Y("insp_compare"), _at.Rect("insp_compare").Size.X, fee);
                var rel = new Rect2I(x, actionY, action.X, action.Y);
                var dem = new Rect2I(x + action.X + 4, actionY, action.X, action.Y);
                Ui.Button(this, rel, $"RELOCATE ¥{fee}", "support", Run.Budget >= fee);
                Ui.Button(this, dem, $"DEMOLISH ¥{fee}", "invalid", Run.Budget >= fee);
                string id = room.RoomId;
                _hits.Add(rel, () => { _carry = CarryKind.Room; _carryId = id; _hint = string.Empty; _lastTapHint = "Tap the new top-left tile on any leased floor"; QueueRedraw(); }, $"Move the room for ¥{fee}; it keeps its Tenure less {_db.Economy.RelocationTenurePenaltyRounds} rounds");
                _hits.Add(dem, () => { Do(new Demolish(id)); _selectedRoom = string.Empty; }, $"Demolish for ¥{fee}. Tenure is forfeited; occupants stay.");
            }
        }
        else
        {
            FounderDef founder = _db.Founders.First(f => f.Id == Run.Tower.Globals.FounderId);
            DrawTextureRect(_r.Textures.For(L.Entry(founder.Portrait)), new Rect2(preview.Position, L.Size(founder.Portrait)), false);
            _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), Run.FirmName, _font.Small, Tones.Text("interface"));
            _font.Draw(this, _at.X("insp_sub"), _at.Y("insp_sub"), founder.Name, _font.Small, Tones.Muted("interface"));
            _font.Draw(this, _at.X("insp_sub2"), _at.Y("insp_sub2"), founder.Title, _font.Small, Tones.Muted("interface"));
            int ry = _at.Y("insp_context");
            long staff = Run.Tower.Floors.Sum(f => f.Occupants.Count(o => o.Kind == "employee"));
            foreach (string line in new[]
            {
                $"round {Run.Round} · strikes {Run.Strikes}",
                $"fights won {Run.FightsWon} of {Run.History.Length}",
                $"Goodwill cap base {_db.RuleSetFor(Run.Round).GoodwillBase(Run.Round)}",
                $"floors leased {Run.Tower.Floors.Length} · staff {staff}",
                $"budget ¥{Run.Budget} · income ¥{State.Income}+{State.Passives}",
                $"upkeep ¥{State.Upkeep}/qtr",
            })
            {
                _font.Draw(this, x, ry, line, _font.Small, Tones.Text("interface"));
                ry += 10;
            }
            ry += 6;
            string[] primer = Explain.Primer();
            int firmLimit = panel.Position.Y + panel.Size.Y - 22; // no action buttons on the firm panel: the primer may use their row
            _font.Draw(this, x, ry, primer[0], _font.Small, Tones.Muted("interface"));
            ry += 10;
            DrawLines(primer.Skip(1), x, ref ry, firmLimit, Tones.Text("interface"));
            _font.Draw(this, x, Math.Min(ry + 4, firmLimit + 10), "Tap a card or a person to read what it does", _font.Small, Tones.Muted("interface"));
        }
    }

    /// <summary>The card being carried, explained before it is placed: what it does, and where it wants to stand.</summary>
    private void DrawCardInspector(Rect2I panel, int x, int y, int limitY)
    {
        int ry = _at.Y("insp_context");
        if (_carry == CarryKind.StaffCard)
        {
            EmployeeDef d = _db.Employees.First(e => e.Id == Run.Shop.StaffCards[_carryIndex]);
            DrawTextureRect(_r.Textures.For(L.Entry("ui.portrait")), new Rect2(_at.Rect("insp_preview").Position, _at.Rect("insp_preview").Size), false);
            DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(_at.Rect("insp_sprite").Position, L.Size(d.Sprite)), false);
            _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), d.Name, _font.Small, Tones.Text("interface"));
            _font.Draw(this, _at.X("insp_sub"), _at.Y("insp_sub"), $"{d.Dept} · tier {d.Tier} · ¥{Economy.EmployeePrice(_db, d)}", _font.Small, Tones.Muted("interface"));
            _font.Draw(this, _at.X("insp_sub2"), _at.Y("insp_sub2"), "tap a tile to hire", _font.Small, Tones.Muted("interface"));
            DrawEmployeeExplanation(d, x, ref ry, limitY);
        }
        else if (_carry == CarryKind.RoomCard)
        {
            RoomDef d = _db.Rooms.First(r => r.Id == Run.Shop.RoomCards[_carryIndex]);
            DrawTextureRect(_r.Textures.For(L.Entry(d.Tile)), new Rect2(_at.Rect("insp_preview").Position, L.Size(d.Tile)), false);
            _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), d.Name, _font.Small, Tones.Text("interface"));
            _font.Draw(this, _at.X("insp_sub"), _at.Y("insp_sub"), $"{d.Footprint.W}x{d.Footprint.H} · ¥{d.Cost}", _font.Small, Tones.Muted("interface"));
            _font.Draw(this, _at.X("insp_sub2"), _at.Y("insp_sub2"), "tap its top-left tile", _font.Small, Tones.Muted("interface"));
            _font.Draw(this, x, ry, "WHAT IT DOES", _font.Small, Tones.Muted("interface"));
            ry += 10;
            DrawLines(Explain.Passives(_db, d.Effects), x, ref ry, limitY, Tones.Text("interface"));
            ry += 4;
            _font.Draw(this, x, ry, "WHERE TO PUT IT", _font.Small, Tones.Muted("interface"));
            ry += 10;
            DrawLines(new[]
            {
                $"Fits on {string.Join(", ", d.Floors.Select(f => _db.Floors.First(x => x.Id == f).Name))}.",
                "A room is a zone: people inside it get its bonus. It grows a Tenure tier every few rounds it stays put.",
            }, x, ref ry, limitY, Tones.Text("interface"));
        }
        else
        {
            FurnitureDef d = _db.Furniture.First(f => f.Id == Run.Shop.FurnitureCards[_carryIndex]);
            DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(_at.Rect("insp_sprite").Position, L.Size(d.Sprite)), false);
            _font.Draw(this, _at.X("insp_name"), _at.Y("insp_name"), d.Name, _font.Small, Tones.Text("interface"));
            _font.Draw(this, _at.X("insp_sub"), _at.Y("insp_sub"), $"{d.Rarity} · ¥{d.Cost}", _font.Small, Tones.Muted("interface"));
            _font.Draw(this, _at.X("insp_sub2"), _at.Y("insp_sub2"), "tap an empty tile", _font.Small, Tones.Muted("interface"));
            _font.Draw(this, x, ry, "WHAT IT DOES", _font.Small, Tones.Muted("interface"));
            ry += 10;
            DrawLines(Explain.Passives(_db, d.Effects), x, ref ry, limitY, Tones.Text("interface"));
            ry += 4;
            _font.Draw(this, x, ry, "WHERE TO PUT IT", _font.Small, Tones.Muted("interface"));
            ry += 10;
            DrawLines(new[] { "Furniture takes a tile and helps the four tiles around it. Put it beside the people it names." }, x, ref ry, limitY, Tones.Text("interface"));
        }
    }

    /// <summary>What an employee does and where it should stand, from Explain; the same block for a card and a hire.</summary>
    private void DrawEmployeeExplanation(EmployeeDef d, int x, ref int ry, int limitY)
    {
        Effect ab = d.Effects.First(e => e.On == "ability");
        _font.Draw(this, x, ry, "WHAT IT DOES", _font.Small, Tones.Muted("interface"));
        ry += 10;
        DrawLines(new[] { Explain.Ability(_db, d) }, x, ref ry, limitY, Tones.Text("interface"));
        DrawLines(Explain.Passives(_db, d.Effects, d), x, ref ry, limitY, Tones.Text("interface"));
        DrawLines(new[] { Explain.Kind(ab.Do) }, x, ref ry, limitY, Tones.Hatch("interface"));
        if (ab.Do == "status" && ab.Status != null) DrawLines(new[] { Explain.Status(ab.Status) }, x, ref ry, limitY, Tones.Hatch("interface"));
        ry += 4;
        _font.Draw(this, x, ry, "WHERE TO PUT IT", _font.Small, Tones.Muted("interface"));
        ry += 10;
        DrawLines(Explain.Placement(_db, d), x, ref ry, limitY, Tones.Text("interface"));
    }

    /// <summary>Wraps each line to the inspector's width and stops at limitY; the panel never overflows its buttons.</summary>
    private void DrawLines(IEnumerable<string> lines, int x, ref int ry, int limitY, Color tone)
    {
        int width = _at.Rect("inspector").Size.X - 16;
        foreach (string text in lines)
        {
            foreach (string line in Ui.Wrap(_font, _font.Small, text, width, 4))
            {
                if (ry > limitY) return;
                _font.Draw(this, x, ry, line, _font.Small, tone);
                ry += 10;
            }
        }
    }

    /// <summary>The relocation trade-off (GAME_DESIGN.md §19.1, §20): here, elsewhere now and by a later round, and the fee.</summary>
    private void DrawCompare(SnapshotRoom room, RoomDef d, int x, int y, int w, long fee)
    {
        Vector2I size = L.Size("ui.build.room_compare");
        DrawRect(new Rect2(x, y, w, size.Y), Tones.Fill("structure"));
        Effect? aura = d.Effects.FirstOrDefault(e => e.On == "static" && e.Do == "stat" && e.Permille != null && e.Stat is "push" or "anomaly" or "restore");
        long basePermille = aura?.Permille ?? 1000;
        long step = _db.Rules.Tenure.StepPermille;
        long tierNow = Overlays.Tier(_db, room);
        long here = (basePermille + step * tierNow) * _db.FloorByIndex(FloorOf(room)).OutputPermille / 1000;
        string[] tiers = { "—", "I", "II", "III" };
        _font.Draw(this, x + 2, y, $"here {Overlays.AuraBadge(here)} · Tier {tiers[Math.Clamp((int)tierNow, 0, 3)]}", _font.Small, Tones.Text("interface"));
        long tenureAfter = Math.Max(0, room.TenureRounds - _db.Economy.RelocationTenurePenaltyRounds);
        long tierAfter = _db.Rules.Tenure.TierRounds.Count(t => t <= tenureAfter);
        FloorDef? best = null;
        foreach (SnapshotFloor sf in Run.Tower.Floors)
        {
            FloorDef fd = _db.FloorByIndex(sf.Index);
            if (sf.Index == FloorOf(room) || Array.IndexOf(d.Floors, fd.Id) < 0) continue;
            if (best == null || fd.OutputPermille > best.OutputPermille) best = fd;
        }
        if (best != null)
        {
            long now = (basePermille + step * tierAfter) * best.OutputPermille / 1000;
            long later = (basePermille + step * Math.Min(3, tierAfter + 2)) * best.OutputPermille / 1000;
            _font.Draw(this, x + 2, y + 8, $"on {Legality.FloorName(best.Index)} {Overlays.AuraBadge(now)} now, {Overlays.AuraBadge(later)} two tiers on", _font.Small, Tones.Text("interface"));
        }
        else
        {
            _font.Draw(this, x + 2, y + 8, "no other leased floor takes this room", _font.Small, Tones.Muted("interface"));
        }
        _font.Draw(this, x + 2, y + 16, $"relocate: −{_db.Economy.RelocationTenurePenaltyRounds} Tenure rounds, ¥{fee}", _font.Small, Tones.Text("interface"));
    }

    private long FloorOf(SnapshotRoom room) => Run.Tower.Floors.First(f => f.Rooms.Contains(room)).Index;

    private void DrawHint()
    {
        Rect2I hint = _at.Rect("hint");
        DrawRect(new Rect2(hint.Position, hint.Size), Tones.Fill("interface"));
        string text = _hint;
        Color tone = _hint.Length > 0 ? Tones.Fill("invalid").Lightened(0.4f) : Tones.Text("interface");
        if (text.Length == 0)
        {
            Vector2 m = GetViewport().GetMousePosition();
            text = _hits.HintAt(new Vector2I((int)m.X, (int)m.Y));
            if (text.Length == 0) text = _lastTapHint;
        }
        _font.Draw(this, hint.Position.X + 4, hint.Position.Y + 4, text.Length > 118 ? text[..118] : text, _font.Small, tone);
    }

    // ---------------------------------------------------------------- input

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_r.Building == null) return;
        if (@event is InputEventMouseMotion) { QueueRedraw(); return; }
        if (@event is InputEventMouseButton { Pressed: true } mb)
        {
            var p = new Vector2I((int)mb.Position.X, (int)mb.Position.Y);
            if (mb.ButtonIndex == MouseButton.Left)
            {
                // Hit zones are registered during the draw; the smallest rect under the point wins (a tile before its room).
                Hits.Hit? best = _hits.At(p);
                if (best != null)
                {
                    _lastTapHint = best.Value.Hint;
                    best.Value.Click();
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                DropCarry();
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && _selectedFloor < 3) { _selectedFloor++; QueueRedraw(); }
            else if (mb.ButtonIndex == MouseButton.WheelDown && _selectedFloor > -1) { _selectedFloor--; QueueRedraw(); }
        }
        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Z: Undo(); break;
                case Key.Enter: case Key.KpEnter: _r.ReadyUp(); break;
                case Key.Escape: DropCarry(); break;
                case Key.Up: if (_selectedFloor < 3) _selectedFloor++; QueueRedraw(); break;
                case Key.Down: if (_selectedFloor > -1) _selectedFloor--; QueueRedraw(); break;
                default: break;
            }
        }
    }
}
