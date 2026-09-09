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
    private string _hint = string.Empty;
    private string _selectedOccupant = string.Empty;
    private string _selectedRoom = string.Empty;
    private int _frames;
    private bool _captured;
    private readonly List<(Rect2I Rect, Action Click, string Hint)> _hits = new();

    private BuildState State => _r.Building!;
    private RunState Run => State.Current;

    public override void _Ready()
    {
        _r = ScreenRouter.Instance;
        _db = _r.Content;
        L = _r.Layout;
        _font = _r.Font;
        if (_r.Building == null) { _r.Go("res://scenes/Menu.tscn"); return; }
        _selectedFloor = _db.Floors.First(f => f.Id == _db.Economy.StartingRosterFloor).Index;
        _hint = _r.Run!.Round == 1 ? _db.Tutorial.Hints.FirstOrDefault(h => h.Round == 1)?.Text ?? string.Empty : string.Empty;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
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
        }
        QueueRedraw();
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

    private int FloorSlotY(int slot) => L.Rect("ui.build.tower").Position.Y + slot * 104; // above, selected, below

    private Rect2I TileRect(int slot, long col, long row)
    {
        int x = L.Rect("ui.build.tower").Position.X + L.Size("ui.build.shaft").X + (int)col * L.Tile;
        int y = FloorSlotY(slot) + (int)row * L.Tile;
        return new Rect2I(x, y, L.Tile, L.Tile);
    }

    private (int Slot, long Index)[] VisibleFloors()
    {
        return new[] { (0, _selectedFloor + 1), (1, _selectedFloor), (2, _selectedFloor - 1) };
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
        Rect2I bar = L.Rect("ui.build.topbar");
        DrawRect(new Rect2(bar.Position, bar.Size), Tones.Fill("interface"));
        Mode mode = _db.Modes.First(m => m.Id == Run.Mode);
        _font.Draw(this, 8, 8, $"Q{Run.Round} · FIGHT {Run.Round}/{mode.Rounds}", _font.Small, Tones.Text("interface"));
        _font.Draw(this, 200, 8, $"¥ {Run.Budget}", _font.Small, Tones.Text("interface"));
        _font.Draw(this, 280, 8, $"+¥{State.Income}{(State.Passives > 0 ? $"+{State.Passives}" : string.Empty)} −¥{State.Upkeep}/qtr{(State.UnpaidUpkeep > 0 ? $" ({State.UnpaidUpkeep} unpaid → Goodwill)" : string.Empty)}", _font.Small, Tones.Text("interface"));
        for (int i = 0; i < mode.Strikes; i++)
        {
            var s = new Rect2(400 + i * 10, 8, 8, 8);
            DrawRect(s, i < Run.Strikes ? Tones.Fill("people") : Tones.Fill("structure"));
            DrawRect(s, Tones.Border("interface"), false);
        }
        Rect2I ready = L.Rect("ui.build.ready");
        DrawRect(new Rect2(ready.Position, ready.Size), Tones.Fill("operations"));
        _font.Draw(this, ready.Position.X, ready.Position.Y + 4, "READY", _font.Small, Tones.Text("operations"), HorizontalAlignment.Center, ready.Size.X);
        _hits.Add((ready, () => _r.ReadyUp(), "Commit the tower and fight. No confirmation; undo is Z."));
        // Touch and controller adapters (D-47): the keys Z and Esc as buttons, sized from the READY button.
        int half = ready.Size.X / 2;
        var undo = new Rect2I(ready.Position.X - half * 2 - 8, ready.Position.Y, half, ready.Size.Y);
        var drop = new Rect2I(ready.Position.X - half - 4, ready.Position.Y, half, ready.Size.Y);
        DrawRect(new Rect2(undo.Position, undo.Size), State.Log.Length > 0 ? Tones.Fill("support") : Tones.Fill("structure"));
        _font.Draw(this, undo.Position.X, undo.Position.Y + 4, "UNDO", _font.Small, Tones.Text("support"), HorizontalAlignment.Center, undo.Size.X);
        _hits.Add((undo, Undo, "Undo the last action (Z)"));
        DrawRect(new Rect2(drop.Position, drop.Size), _carry != CarryKind.None ? Tones.Fill("support") : Tones.Fill("structure"));
        _font.Draw(this, drop.Position.X, drop.Position.Y + 4, "DROP", _font.Small, Tones.Text("support"), HorizontalAlignment.Center, drop.Size.X);
        _hits.Add((drop, DropCarry, "Put down what you are carrying (Esc, right-click)"));
    }

    private void DrawTower()
    {
        Rect2I tower = L.Rect("ui.build.tower");
        Rect2I shaft = L.Rect("ui.build.shaft");
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.tower")), new Rect2(tower.Position, tower.Size), false);
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.shaft")), new Rect2(shaft.Position, shaft.Size), false);
        // Floor up and down as buttons at the shaft's ends, for touch (the wheel and the arrow keys do the same).
        var up = new Rect2I(shaft.Position.X, shaft.Position.Y + shaft.Size.Y - 2 * shaft.Size.X, shaft.Size.X, shaft.Size.X);
        var down = new Rect2I(shaft.Position.X, shaft.Position.Y + shaft.Size.Y - shaft.Size.X, shaft.Size.X, shaft.Size.X);
        DrawRect(new Rect2(up.Position, up.Size), _selectedFloor < 3 ? Tones.Fill("support") : Tones.Fill("structure"));
        DrawRect(new Rect2(down.Position, down.Size), _selectedFloor > -1 ? Tones.Fill("support") : Tones.Fill("structure"));
        _font.Draw(this, up.Position.X, up.Position.Y + 4, "▲", _font.Small, Tones.Text("support"), HorizontalAlignment.Center, up.Size.X);
        _font.Draw(this, down.Position.X, down.Position.Y + 4, "▼", _font.Small, Tones.Text("support"), HorizontalAlignment.Center, down.Size.X);
        _hits.Add((up, () => { if (_selectedFloor < 3) _selectedFloor++; QueueRedraw(); }, "Floor up (wheel, arrow up)"));
        _hits.Add((down, () => { if (_selectedFloor > -1) _selectedFloor--; QueueRedraw(); }, "Floor down (wheel, arrow down)"));
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
            if (!selected) _hits.Add((hit, () => { _selectedFloor = captured; QueueRedraw(); }, $"Select {Legality.FloorName(index)}"));
            if (!Legality.HasFloor(Run.Tower, index))
            {
                DrawTextureRect(_r.Textures.For(L.Entry("ui.build.floor_void")), new Rect2(frame.Position, frame.Size), true, dim);
                FloorDef fd = _db.FloorByIndex(index);
                _font.Draw(this, frame.Position.X + 4, y + 4, $"{fd.Name} · lease ¥{fd.Lease}", _font.Small, Tones.Hatch("structure"));
                continue;
            }
            SnapshotFloor floor = Legality.Floor(Run.Tower, index);
            DrawTextureRect(_r.Textures.For(L.Entry("ui.build.floor_frame")), new Rect2(frame.Position, frame.Size), false, dim);
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
                DrawTextureRect(_r.Textures.For(L.Entry(rdef.Tile)), rr, true, dim);
                if (room.RoomId == _selectedRoom && selected) DrawRect(rr, Tones.Fill("operations"), false, 1);
                Vector2I sign = L.Size("ui.room_sign");
                DrawTextureRect(_r.Textures.For(L.Entry("ui.room_sign")), new Rect2(tl.Position.X, tl.Position.Y, sign.X, sign.Y), false, dim);
                _font.Draw(this, tl.Position.X + 1, tl.Position.Y, rdef.Name.Length > 7 ? rdef.Name[..7] : rdef.Name, _font.Small, Tones.Text("interface"));
                long tier = Overlays.Tier(_db, room);
                Vector2I pip = L.Size("ui.tenure_pip");
                for (int i = 0; i < tier; i++) DrawRect(new Rect2(tl.Position.X + sign.X - (i + 1) * (pip.X + 1), tl.Position.Y + 2, pip.X, pip.Y), Tones.Fill("support"));
                string roomId = room.RoomId;
                if (selected) _hits.Add((new Rect2I((int)rr.Position.X, (int)rr.Position.Y, (int)rr.Size.X, (int)rr.Size.Y), () => ClickTile(index, room.Rect[0], room.Rect[1]), $"{rdef.Name} · {rdef.Flavor}"));
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
                        _hits.Add((t, () => ClickTile(index, c, rw), HintForTile(floor, c, rw)));
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
        Rect2I shop = L.Rect("ui.build.shop");
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.shop")), new Rect2(shop.Position, shop.Size), false);
        Vector2I tab = L.Size("ui.build.tab");
        string[] tabs = { Shop.StaffTab, Shop.RoomsTab, Shop.FurnitureTab };
        for (int i = 0; i < tabs.Length; i++)
        {
            var rect = new Rect2I(shop.Position.X + i * tab.X, shop.Position.Y, tab.X, tab.Y);
            bool active = tabs[i] == _tab;
            DrawRect(new Rect2(rect.Position, rect.Size), active ? Tones.Fill("operations") : Tones.Fill("interface"));
            _font.Draw(this, rect.Position.X, rect.Position.Y + 4, tabs[i].ToUpperInvariant(), _font.Small, Tones.Text("interface"), HorizontalAlignment.Center, rect.Size.X);
            string t = tabs[i];
            _hits.Add((rect, () => { _tab = t; DropCarry(); }, $"Show the {t} tab"));
        }
        string[] cards = _tab == Shop.StaffTab ? Run.Shop.StaffCards : _tab == Shop.RoomsTab ? Run.Shop.RoomCards : Run.Shop.FurnitureCards;
        Vector2I card = L.Size("ui.card.applicant");
        for (int i = 0; i < cards.Length; i++)
        {
            var rect = new Rect2I(shop.Position.X + i * (card.X + 4), shop.Position.Y + 24, card.X, card.Y);
            DrawCard(rect, cards[i], i);
        }
        var reroll = new Rect2I(shop.Position.X, shop.Position.Y + 110, 120, 12);
        long rerollCost = Economy.RerollCost(_db, Run.Tower);
        DrawRect(new Rect2(reroll.Position, reroll.Size), Tones.Fill("support"));
        _font.Draw(this, reroll.Position.X + 2, reroll.Position.Y + 2, $"REROLL {_tab.ToUpperInvariant()} · ¥{rerollCost}", _font.Small, Tones.Text("support"));
        _hits.Add((reroll, () => Do(new Reroll(_tab)), "Replace this tab's cards from its bag; nothing repeats until the bag empties"));
        _font.Draw(this, shop.Position.X, shop.Position.Y + 128, "Otherworld Temp Agency · the portal is closed", _font.Small, Tones.Hatch("anomalous"));

        _font.Draw(this, shop.Position.X, shop.Position.Y + 208, "LEASE", _font.Small, Tones.Hatch("interface"));
        Vector2I lb = L.Size("ui.build.lease_button");
        int k = 0;
        foreach (string floorId in _db.Shop.Leases)
        {
            FloorDef fd = _db.Floors.First(f => f.Id == floorId);
            var rect = new Rect2I(shop.Position.X + k * 80, shop.Position.Y + 228, lb.X, lb.Y);
            bool owned = Legality.HasFloor(Run.Tower, fd.Index);
            DrawRect(new Rect2(rect.Position, rect.Size), owned ? Tones.Fill("structure") : Tones.Fill("interface"));
            DrawRect(new Rect2(rect.Position, rect.Size), Tones.Border("interface"), false);
            _font.Draw(this, rect.Position.X + 2, rect.Position.Y + 2, Legality.FloorName(fd.Index) + (owned ? " leased" : $" ¥{fd.Lease}"), _font.Small, Tones.Text("interface"));
            _font.Draw(this, rect.Position.X + 2, rect.Position.Y + 12, owned ? $"−¥{fd.UpkeepBudget}/qtr" : $"then −¥{fd.UpkeepBudget}/qtr", _font.Small, Tones.Hatch("interface"));
            if (!owned) _hits.Add((rect, () => Do(new Lease(floorId)), $"Lease {fd.Name} for ¥{fd.Lease}; upkeep ¥{fd.UpkeepBudget} per round{(fd.RequiresPortal ? "; needs the portal" : string.Empty)}"));
            k++;
        }
    }

    private void DrawCard(Rect2I rect, string defId, int index)
    {
        string kindEntry = _tab == Shop.StaffTab ? "ui.card.applicant" : _tab == Shop.RoomsTab ? "ui.card.room" : "ui.card.furniture";
        bool carried = _carryIndex == index && (_carry is CarryKind.StaffCard or CarryKind.RoomCard or CarryKind.FurnitureCard);
        DrawTextureRect(_r.Textures.For(L.Entry(kindEntry)), new Rect2(rect.Position, rect.Size), false, carried ? new Color(1, 1, 1, 0.5f) : Colors.White);
        string name, line1, line2, cost, hint;
        if (_tab == Shop.StaffTab)
        {
            EmployeeDef d = _db.Employees.First(e => e.Id == defId);
            Effect ab = d.Effects.First(e => e.On == "ability");
            name = d.Name;
            line1 = $"{d.Dept[..Math.Min(3, d.Dept.Length)]} T{d.Tier} {d.CooldownTicks / 20}.{d.CooldownTicks % 20 / 2}s";
            line2 = $"{ab.Do} {(ab.Value?.Constant.HasValue == true ? ab.Value.Constant.Value.ToString() : ab.Status?[7..] ?? string.Empty)}";
            cost = $"¥{Economy.EmployeePrice(_db, d)}";
            hint = $"{d.Name}: {d.Flavor}";
            DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(rect.Position.X + 10, rect.Position.Y + 4, 32, 32), false);
        }
        else if (_tab == Shop.RoomsTab)
        {
            RoomDef d = _db.Rooms.First(r => r.Id == defId);
            name = d.Name;
            line1 = $"{d.Footprint.W}x{d.Footprint.H} {string.Join(" ", d.Floors.Select(f => Legality.FloorName(_db.Floors.First(x => x.Id == f).Index)))}";
            Effect? aura = d.Effects.FirstOrDefault(e => e.On == "static" && e.Do == "stat" && e.Permille != null);
            line2 = aura != null ? $"{aura.Stat} ×{aura.Permille / 1000}.{aura.Permille % 1000 / 100}" : string.Empty;
            cost = $"¥{d.Cost}";
            hint = $"{d.Name}: {d.Flavor}";
            DrawTextureRect(_r.Textures.For(L.Entry(d.Tile)), new Rect2(rect.Position.X + 10, rect.Position.Y + 4, 32, 32), true);
        }
        else
        {
            FurnitureDef d = _db.Furniture.First(f => f.Id == defId);
            name = d.Name;
            line1 = $"{d.Footprint.W}x{d.Footprint.H} {d.Rarity}";
            Effect e0 = d.Effects[0];
            line2 = e0.Do == "stat" ? $"{e0.Stat} {(e0.Amount.HasValue ? "+" + e0.Amount : "×" + e0.Permille / 1000 + "." + e0.Permille % 1000 / 100)}" : e0.Do;
            cost = $"¥{d.Cost}";
            hint = $"{d.Name}: {d.Flavor}";
            DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(rect.Position.X + 10, rect.Position.Y + 4, 32, 32), false);
        }
        _font.Draw(this, rect.Position.X + 2, rect.Position.Y + 40, name.Length > 10 ? name[..10] : name, _font.Small, Tones.Text("interface"));
        _font.Draw(this, rect.Position.X + 2, rect.Position.Y + 50, line1, _font.Small, Tones.Text("interface"));
        _font.Draw(this, rect.Position.X + 2, rect.Position.Y + 60, line2, _font.Small, Tones.Text("interface"));
        _font.Draw(this, rect.Position.X + 2, rect.Position.Y + 70, cost, _font.Small, Tones.Text("interface"));
        int i = index;
        _hits.Add((rect, () => PickCard(i), hint + " · click, then click a tile"));
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
            _selectedOccupant = o.InstanceId;
            _selectedRoom = string.Empty;
            _carry = CarryKind.Occupant;
            _carryId = o.InstanceId;
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
        Rect2I panel = L.Rect("ui.build.inspector");
        DrawTextureRect(_r.Textures.For(L.Entry("ui.build.inspector")), new Rect2(panel.Position, panel.Size), false);
        int x = panel.Position.X + 8, y = panel.Position.Y + 8;
        SnapshotOccupant? occ = null;
        SnapshotFloor? occFloor = null;
        foreach (SnapshotFloor f in Run.Tower.Floors)
        {
            occ = f.Occupants.FirstOrDefault(o => o.InstanceId == _selectedOccupant);
            if (occ != null) { occFloor = f; break; }
        }
        SnapshotRoom? room = Run.Tower.Floors.SelectMany(f => f.Rooms).FirstOrDefault(r => r.RoomId == _selectedRoom);
        Vector2I action = L.Size("ui.build.action_button");
        int actionY = panel.Position.Y + panel.Size.Y - action.Y - 4;
        if (occ != null && occFloor != null)
        {
            if (occ.Kind == "employee")
            {
                EmployeeDef d = _db.Employees.First(e => e.Id == occ.DefId);
                DrawTextureRect(_r.Textures.For(L.Entry("ui.portrait")), new Rect2(x, y, 64, 64), false);
                DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(x + 16, y + 16, 32, 32), false);
                _font.Draw(this, x + 72, y, d.Name, _font.Small, Tones.Text("interface"));
                _font.Draw(this, x + 72, y + 10, $"{d.Dept} · tier {d.Tier}", _font.Small, Tones.Hatch("interface"));
                int ry = panel.Position.Y + 80;
                foreach (Effect e in d.Effects)
                {
                    if (e.On == "economy") { _font.Draw(this, x, ry, $"income +¥{e.Amount}/round", _font.Small, Tones.Text("interface")); ry += 10; continue; }
                    string line = e.On == "ability" ? $"{e.Name}: {e.Do} {Describe(e)} every {d.CooldownTicks / 20}.{d.CooldownTicks % 20 / 2}s" : $"{e.On}: {e.Do} {Describe(e)}";
                    _font.Draw(this, x, ry, line.Length > 44 ? line[..44] : line, _font.Small, Tones.Text("interface"));
                    ry += 10;
                }
                long aura = Overlays.AuraPermille(_db, Run.Tower, occFloor, occ);
                _font.Draw(this, x, ry + 4, $"aura {Overlays.AuraBadge(aura)} · floor ×{_db.FloorByIndex(occFloor.Index).OutputPermille / 1000}.{_db.FloorByIndex(occFloor.Index).OutputPermille % 1000 / 100}", _font.Small, Tones.Hatch("interface"));
                long fee = Economy.Severance(_db, Run.Tower, d);
                var btn = new Rect2I(x, actionY, panel.Size.X - 16, action.Y);
                DrawRect(new Rect2(btn.Position, btn.Size), Tones.Fill("invalid"));
                _font.Draw(this, btn.Position.X, btn.Position.Y + 6, $"LAY OFF · ¥{fee}", _font.Small, Tones.Text("invalid"), HorizontalAlignment.Center, btn.Size.X);
                string id = occ.InstanceId;
                _hits.Add((btn, () => { Do(new LayOff(id)); DropCarry(); _selectedOccupant = string.Empty; }, $"Lay off for ¥{fee} severance. Refunds nothing."));
            }
            else
            {
                FurnitureDef d = _db.Furniture.First(e => e.Id == occ.DefId);
                DrawTextureRect(_r.Textures.For(L.Entry(d.Sprite)), new Rect2(x, y, 32, 32), false);
                _font.Draw(this, x + 72, y, d.Name, _font.Small, Tones.Text("interface"));
                _font.Draw(this, x, panel.Position.Y + 80, d.Flavor ?? string.Empty, _font.Small, Tones.Text("interface"), HorizontalAlignment.Left, panel.Size.X - 16);
                var btn = new Rect2I(x, actionY, panel.Size.X - 16, action.Y);
                DrawRect(new Rect2(btn.Position, btn.Size), Tones.Fill("invalid"));
                _font.Draw(this, btn.Position.X, btn.Position.Y + 6, "SELL · ¥0", _font.Small, Tones.Text("invalid"), HorizontalAlignment.Center, btn.Size.X);
                string id = occ.InstanceId;
                _hits.Add((btn, () => { Do(new Sell(id)); DropCarry(); _selectedOccupant = string.Empty; }, "Sell furniture. Refunds nothing, costs nothing."));
            }
        }
        else if (room != null)
        {
            RoomDef d = _db.Rooms.First(r => r.Id == room.DefId);
            DrawTextureRect(_r.Textures.For(L.Entry(d.Tile)), new Rect2(x, y, 64, 64), true);
            _font.Draw(this, x + 72, y, d.Name, _font.Small, Tones.Text("interface"));
            _font.Draw(this, x + 72, y + 10, $"Tenure {room.TenureRounds} · Tier {Overlays.Tier(_db, room)}", _font.Small, Tones.Hatch("interface"));
            int ry = panel.Position.Y + 80;
            foreach (Effect e in d.Effects)
            {
                string line = $"{e.On}: {e.Do} {Describe(e)}{(e.FromTier.HasValue ? $" from T{e.FromTier}" : string.Empty)}{(e.UntilTier.HasValue ? $" until T{e.UntilTier}" : string.Empty)}";
                _font.Draw(this, x, ry, line.Length > 44 ? line[..44] : line, _font.Small, Tones.Text("interface"));
                ry += 10;
            }
            long fee = Economy.RenovationFee(_db, Run.Round);
            if (!d.Fixed)
            {
                var rel = new Rect2I(x, actionY, action.X, action.Y);
                var dem = new Rect2I(x + action.X + 4, actionY, action.X, action.Y);
                DrawRect(new Rect2(rel.Position, rel.Size), Tones.Fill("support"));
                _font.Draw(this, rel.Position.X, rel.Position.Y + 6, $"RELOCATE ¥{fee}", _font.Small, Tones.Text("support"), HorizontalAlignment.Center, rel.Size.X);
                DrawRect(new Rect2(dem.Position, dem.Size), Tones.Fill("invalid"));
                _font.Draw(this, dem.Position.X, dem.Position.Y + 6, $"DEMOLISH ¥{fee}", _font.Small, Tones.Text("invalid"), HorizontalAlignment.Center, dem.Size.X);
                string id = room.RoomId;
                _hits.Add((rel, () => { _carry = CarryKind.Room; _carryId = id; _hint = "Click the new top-left tile on any leased floor"; QueueRedraw(); }, $"Move the room for ¥{fee}; it keeps its Tenure less {_db.Economy.RelocationTenurePenaltyRounds} rounds"));
                _hits.Add((dem, () => { Do(new Demolish(id)); _selectedRoom = string.Empty; }, $"Demolish for ¥{fee}. Tenure is forfeited; occupants stay."));
            }
        }
        else
        {
            FounderDef founder = _db.Founders.First(f => f.Id == Run.Tower.Globals.FounderId);
            DrawTextureRect(_r.Textures.For(L.Entry(founder.Portrait)), new Rect2(x, y, 64, 64), false);
            _font.Draw(this, x + 72, y, Run.FirmName, _font.Small, Tones.Text("interface"));
            _font.Draw(this, x + 72, y + 10, founder.Name, _font.Small, Tones.Hatch("interface"));
            _font.Draw(this, x + 72, y + 20, founder.Title, _font.Small, Tones.Hatch("interface"));
            int ry = panel.Position.Y + 80;
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
            _font.Draw(this, x, ry + 10, "Z undo · Enter READY · wheel floors", _font.Small, Tones.Hatch("interface"));
            _font.Draw(this, x, ry + 20, "click a card then a tile to place", _font.Small, Tones.Hatch("interface"));
            _font.Draw(this, x, ry + 30, "click a person to pick up, a tile to move", _font.Small, Tones.Hatch("interface"));
        }
    }

    private static string Describe(Effect e)
    {
        if (e.Value != null) return e.Value.Constant?.ToString() ?? (e.Value.Base.HasValue ? $"{e.Value.Base}+{e.Value.Each}/{e.Value.PerTag}" : $"{e.Value.PermilleOfTargetCap}‰ of cap");
        if (e.Stat != null) return $"{e.Stat} {(e.Amount.HasValue ? (e.Amount >= 0 ? "+" : string.Empty) + e.Amount : "×" + e.Permille / 1000 + "." + e.Permille % 1000 / 100)}";
        if (e.Flag != null) return e.Flag;
        if (e.Status != null) return $"{e.Status[7..]} +{e.Stacks}";
        return string.Empty;
    }

    private void DrawHint()
    {
        Rect2I hint = L.Rect("ui.build.hint");
        DrawRect(new Rect2(hint.Position, hint.Size), Tones.Fill("interface"));
        string text = _hint;
        if (text.Length == 0)
        {
            Vector2 m = GetViewport().GetMousePosition();
            var p = new Vector2I((int)m.X, (int)m.Y);
            foreach ((Rect2I rect, Action _, string h) in _hits)
            {
                if (rect.HasPoint(p)) { text = h; break; }
            }
        }
        if (_carry != CarryKind.None && _hint.Length == 0) text = "Carrying · click a tile to place, right-click or Esc to drop · " + text;
        _font.Draw(this, hint.Position.X + 4, hint.Position.Y + 4, text.Length > 110 ? text[..110] : text, _font.Small, Tones.Text("interface"));
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
                // Hit zones are registered during the draw; the smallest rect under the cursor wins (a tile before its room).
                (Rect2I Rect, Action Click, string Hint)? best = null;
                foreach ((Rect2I rect, Action click, string hint) in _hits)
                {
                    if (!rect.HasPoint(p)) continue;
                    if (best == null || rect.Size.X * rect.Size.Y < best.Value.Rect.Size.X * best.Value.Rect.Size.Y) best = (rect, click, hint);
                }
                best?.Click();
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
