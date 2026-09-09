using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>
/// The build phase as a pure reducer (D-17, D-43): <c>Apply(state, action)</c> over the closed action set. The undo
/// stack is the action log; undo replays all but the last action from the round's start state. <c>Commit</c> is
/// Ready: Tenure ticks, the snapshot is the tower.
/// </summary>
public static class BuildReducer
{
    /// <summary>Opens a round: income and passives in, upkeep out, unpaid upkeep carried as modifier copies, a fresh shop.</summary>
    public static BuildState OpenRound(ContentDb db, RunState run)
    {
        long income = Economy.RoundIncome(db, run.Round);
        long passives = Economy.Passives(db, run.Tower);
        long upkeep = Economy.Upkeep(db, run.Tower);
        long budget = run.Budget + income + passives - upkeep;
        long unpaid = 0;
        if (budget < 0)
        {
            unpaid = -budget;
            budget = 0;
        }
        var modifiers = new List<string>(run.Modifiers);
        for (long i = 0; i < unpaid; i++) modifiers.Add(Economy.UnpaidUpkeepModifier);
        TowerSnapshot tower = run.Tower with { Round = run.Round, Globals = run.Tower.Globals with { Modifiers = modifiers.ToArray() } };
        uint rng = run.Rng;
        ShopState shop = Shop.Open(db, tower, run.Round, rng);
        RunState start = run with { Budget = budget, Tower = tower, Shop = shop, Rng = shop.Rng };
        return new BuildState(start, start, Array.Empty<BuildAction>(), unpaid, income, upkeep, passives);
    }

    public static BuildState Apply(ContentDb db, BuildState state, BuildAction action)
    {
        RunState next = ApplyOne(db, state.Current, action);
        var log = new BuildAction[state.Log.Length + 1];
        Array.Copy(state.Log, log, state.Log.Length);
        log[^1] = action;
        return state with { Current = next, Log = log };
    }

    /// <summary>Steps back one action by replaying the rest from the round's start.</summary>
    public static BuildState Undo(ContentDb db, BuildState state)
    {
        if (state.Log.Length == 0) return state;
        RunState cur = state.RoundStart;
        var log = state.Log[..^1];
        foreach (BuildAction a in log) cur = ApplyOne(db, cur, a);
        return state with { Current = cur, Log = log };
    }

    /// <summary>Ready (GAME_DESIGN.md §5.6): the log clears, Tenure ticks for rooms held since the previous commit and at least half staffed, the snapshot is taken.</summary>
    public static RunState Commit(ContentDb db, BuildState state)
    {
        RunState run = state.Current;
        long staffedPermille = db.Rules.Tenure.StaffedPermille;
        var held = new HashSet<string>(run.HeldRoomIds, StringComparer.Ordinal);
        var floors = new List<SnapshotFloor>();
        var nowHeld = new List<string>();
        foreach (SnapshotFloor f in run.Tower.Floors)
        {
            var rooms = new List<SnapshotRoom>();
            foreach (SnapshotRoom r in f.Rooms)
            {
                long tiles = r.Rect[2] * r.Rect[3];
                long staffed = f.Occupants.Count(o => o.Kind == "employee" && Economy.Inside(o, r));
                bool tick = held.Contains(r.RoomId) && staffed * 1000 >= tiles * staffedPermille;
                rooms.Add(tick ? r with { TenureRounds = r.TenureRounds + 1 } : r);
                nowHeld.Add(r.RoomId);
            }
            floors.Add(f with { Rooms = rooms.ToArray() });
        }
        TowerSnapshot tower = run.Tower with { Floors = floors.ToArray() };
        return run with { Tower = tower, HeldRoomIds = nowHeld.ToArray() };
    }

    private static RunState ApplyOne(ContentDb db, RunState run, BuildAction action)
    {
        switch (action)
        {
            case Hire a: return HireCard(db, run, a);
            case BuyRoom a: return BuyRoomCard(db, run, a);
            case BuyFurniture a: return BuyFurnitureCard(db, run, a);
            case Move a: return MoveOccupant(db, run, a);
            case LayOff a: return LayOffEmployee(db, run, a);
            case Sell a: return SellFurniture(db, run, a);
            case Demolish a: return DemolishRoom(db, run, a);
            case Relocate a: return RelocateRoom(db, run, a);
            case Lease a: return LeaseFloor(db, run, a);
            case Reroll a: return RerollTab(db, run, a);
            default: throw new BuildException("unknown action");
        }
    }

    private static RunState Pay(RunState run, long cost, string what)
    {
        if (cost > run.Budget) throw new BuildException($"{what} costs ¥{cost}; you have ¥{run.Budget}");
        return run with { Budget = run.Budget - cost };
    }

    private static string NewInstanceId(RunState run, string prefix)
    {
        int n = 0;
        foreach (SnapshotFloor f in run.Tower.Floors) n += f.Occupants.Length;
        string id;
        do
        {
            id = $"{prefix}_{run.Round:D2}_{n:D3}";
            n++;
        }
        while (run.Tower.Floors.Any(f => f.Occupants.Any(o => o.InstanceId == id)));
        return id;
    }

    private static TowerSnapshot WithFloor(TowerSnapshot tower, SnapshotFloor floor)
    {
        var floors = tower.Floors.Select(f => f.Index == floor.Index ? floor : f).ToArray();
        return tower with { Floors = floors };
    }

    private static RunState HireCard(ContentDb db, RunState run, Hire a)
    {
        if (a.CardIndex < 0 || a.CardIndex >= run.Shop.StaffCards.Length) throw new BuildException("no such card");
        string defId = run.Shop.StaffCards[a.CardIndex];
        EmployeeDef def = db.Employees.First(e => e.Id == defId);
        Legality.CheckEmployeePlacement(db, run.Tower, def, a.Floor, a.Col, a.Row, null, false);
        run = Pay(run, Economy.EmployeePrice(db, def), def.Name);
        SnapshotFloor floor = Legality.Floor(run.Tower, a.Floor);
        var occ = new SnapshotOccupant(new[] { a.Col, a.Row }, "employee", defId, NewInstanceId(run, "e"), Array.Empty<string>());
        floor = floor with { Occupants = floor.Occupants.Append(occ).ToArray() };
        string[] cards = run.Shop.StaffCards.Where((_, i) => i != a.CardIndex).ToArray();
        return run with { Tower = WithFloor(run.Tower, floor), Shop = run.Shop with { StaffCards = cards } };
    }

    private static RunState BuyRoomCard(ContentDb db, RunState run, BuyRoom a)
    {
        if (a.CardIndex < 0 || a.CardIndex >= run.Shop.RoomCards.Length) throw new BuildException("no such card");
        RoomDef def = db.Rooms.First(r => r.Id == run.Shop.RoomCards[a.CardIndex]);
        Legality.CheckRoomPlacement(db, run.Tower, def, a.Floor, a.Col, a.Row, null);
        run = Pay(run, def.Cost, def.Name);
        SnapshotFloor floor = Legality.Floor(run.Tower, a.Floor);
        string roomId = $"{Legality.FloorName(a.Floor).ToLowerInvariant()}_{def.Id[(def.Id.IndexOf('.') + 1)..]}_{run.Round:D2}_{floor.Rooms.Length}";
        var room = new SnapshotRoom(roomId, def.Id, new[] { a.Col, a.Row, def.Footprint.W, def.Footprint.H }, 0);
        floor = floor with { Rooms = floor.Rooms.Append(room).ToArray() };
        string[] cards = run.Shop.RoomCards.Where((_, i) => i != a.CardIndex).ToArray();
        return run with { Tower = WithFloor(run.Tower, floor), Shop = run.Shop with { RoomCards = cards } };
    }

    private static RunState BuyFurnitureCard(ContentDb db, RunState run, BuyFurniture a)
    {
        if (a.CardIndex < 0 || a.CardIndex >= run.Shop.FurnitureCards.Length) throw new BuildException("no such card");
        FurnitureDef def = db.Furniture.First(f => f.Id == run.Shop.FurnitureCards[a.CardIndex]);
        Legality.CheckFurniturePlacement(db, run.Tower, def, a.Floor, a.Col, a.Row, null);
        run = Pay(run, def.Cost, def.Name);
        SnapshotFloor floor = Legality.Floor(run.Tower, a.Floor);
        var occ = new SnapshotOccupant(new[] { a.Col, a.Row }, "furniture", def.Id, NewInstanceId(run, "f"), Array.Empty<string>());
        floor = floor with { Occupants = floor.Occupants.Append(occ).ToArray() };
        string[] cards = run.Shop.FurnitureCards.Where((_, i) => i != a.CardIndex).ToArray();
        return run with { Tower = WithFloor(run.Tower, floor), Shop = run.Shop with { FurnitureCards = cards } };
    }

    public static (SnapshotFloor Floor, SnapshotOccupant Occupant) Find(RunState run, string instanceId)
    {
        foreach (SnapshotFloor f in run.Tower.Floors)
        {
            foreach (SnapshotOccupant o in f.Occupants)
            {
                if (o.InstanceId == instanceId) return (f, o);
            }
        }
        throw new BuildException("nothing there");
    }

    private static RunState MoveOccupant(ContentDb db, RunState run, Move a)
    {
        (SnapshotFloor origin, SnapshotOccupant o) = Find(run, a.InstanceId);
        if (o.Kind == "employee")
        {
            EmployeeDef def = db.Employees.First(e => e.Id == o.DefId);
            Legality.CheckEmployeePlacement(db, run.Tower, def, a.Floor, a.Col, a.Row, o.InstanceId, Economy.HasFlag(db, run.Tower, o.InstanceId, "landingOnly"));
        }
        else
        {
            FurnitureDef def = db.Furniture.First(f => f.Id == o.DefId);
            Legality.CheckFurniturePlacement(db, run.Tower, def, a.Floor, a.Col, a.Row, o.InstanceId);
        }
        TowerSnapshot tower = WithFloor(run.Tower, origin with { Occupants = origin.Occupants.Where(x => x.InstanceId != o.InstanceId).ToArray() });
        SnapshotFloor to = Legality.Floor(tower, a.Floor);
        var moved = o with { Tile = new[] { a.Col, a.Row } };
        tower = WithFloor(tower, to with { Occupants = to.Occupants.Append(moved).ToArray() });
        return run with { Tower = tower };
    }

    private static RunState LayOffEmployee(ContentDb db, RunState run, LayOff a)
    {
        (SnapshotFloor origin, SnapshotOccupant o) = Find(run, a.InstanceId);
        if (o.Kind != "employee") throw new BuildException("that is not an employee");
        if (Economy.HasFlag(db, run.Tower, o.InstanceId, "cannotBeLaidOff")) throw new BuildException("this employee cannot be laid off");
        EmployeeDef def = db.Employees.First(e => e.Id == o.DefId);
        run = Pay(run, Economy.Severance(db, run.Tower, def), "severance");
        TowerSnapshot tower = WithFloor(run.Tower, origin with { Occupants = origin.Occupants.Where(x => x.InstanceId != o.InstanceId).ToArray() });
        tower = tower with { Globals = tower.Globals with { Riders = tower.Globals.Riders.Where(r => r.InstanceId != o.InstanceId).ToArray() } };
        return run with { Tower = tower };
    }

    private static RunState SellFurniture(ContentDb db, RunState run, Sell a)
    {
        (SnapshotFloor origin, SnapshotOccupant o) = Find(run, a.InstanceId);
        if (o.Kind != "furniture") throw new BuildException("that is not furniture");
        run = run with { Budget = run.Budget + db.Economy.FurnitureSellRefund };
        return run with { Tower = WithFloor(run.Tower, origin with { Occupants = origin.Occupants.Where(x => x.InstanceId != o.InstanceId).ToArray() }) };
    }

    private static (SnapshotFloor Floor, SnapshotRoom Room) FindRoom(RunState run, string roomId)
    {
        foreach (SnapshotFloor f in run.Tower.Floors)
        {
            foreach (SnapshotRoom r in f.Rooms)
            {
                if (r.RoomId == roomId) return (f, r);
            }
        }
        throw new BuildException("no such room");
    }

    private static RunState DemolishRoom(ContentDb db, RunState run, Demolish a)
    {
        (SnapshotFloor floor, SnapshotRoom room) = FindRoom(run, a.RoomId);
        RoomDef def = db.Rooms.First(r => r.Id == room.DefId);
        if (def.Fixed) throw new BuildException($"{def.Name} cannot be demolished");
        run = Pay(run, Economy.RenovationFee(db, run.Round), "the renovation fee");
        // Furniture inside the room has nowhere legal to stand; it is sold with the room.
        var occupants = floor.Occupants.Where(o => o.Kind == "employee" || !Economy.Inside(o, room)).ToArray();
        floor = floor with { Rooms = floor.Rooms.Where(r => r.RoomId != room.RoomId).ToArray(), Occupants = occupants };
        return run with { Tower = WithFloor(run.Tower, floor) };
    }

    private static RunState RelocateRoom(ContentDb db, RunState run, Relocate a)
    {
        (SnapshotFloor origin, SnapshotRoom room) = FindRoom(run, a.RoomId);
        RoomDef def = db.Rooms.First(r => r.Id == room.DefId);
        if (def.Fixed) throw new BuildException($"{def.Name} cannot be relocated");
        long dc = a.Col - room.Rect[0], dr = a.Row - room.Rect[1];
        var moving = origin.Occupants.Where(o => Economy.Inside(o, room)).ToList();
        // Legality on a tower without the room and its contents, then with them placed at the target.
        TowerSnapshot without = WithFloor(run.Tower, origin with
        {
            Rooms = origin.Rooms.Where(r => r.RoomId != room.RoomId).ToArray(),
            Occupants = origin.Occupants.Where(o => !moving.Contains(o)).ToArray(),
        });
        Legality.CheckRoomPlacement(db, without, def, a.Floor, a.Col, a.Row, null);
        SnapshotFloor target = Legality.Floor(without, a.Floor);
        foreach (SnapshotOccupant o in moving)
        {
            long c = o.Tile[0] + dc, r = o.Tile[1] + dr;
            if (Legality.OccupantAt(db, target, c, r) != null) throw new BuildException("something stands where the room would go");
            if (o.Kind == "employee")
            {
                EmployeeDef edef = db.Employees.First(e => e.Id == o.DefId);
                FloorDef fdef = db.FloorByIndex(a.Floor);
                if (edef.Placement != null && Array.IndexOf(edef.Placement.Floors, fdef.Id) < 0) throw new BuildException($"{edef.Name} may not stand on {Legality.FloorName(a.Floor)}");
                if (a.Floor == -1 && !edef.Extraplanar) throw new BuildException("only extraplanar staff may stand on B1");
            }
        }
        run = Pay(run, Economy.RelocationFee(db, run.Round), "the relocation fee");
        var movedRoom = room with { Rect = new[] { a.Col, a.Row, room.Rect[2], room.Rect[3] }, TenureRounds = Math.Max(0, room.TenureRounds - db.Economy.RelocationTenurePenaltyRounds) };
        var movedOcc = moving.Select(o => o with { Tile = new[] { o.Tile[0] + dc, o.Tile[1] + dr } });
        target = Legality.Floor(without, a.Floor);
        target = target with { Rooms = target.Rooms.Append(movedRoom).ToArray(), Occupants = target.Occupants.Concat(movedOcc).ToArray() };
        return run with { Tower = WithFloor(without, target) };
    }

    private static RunState LeaseFloor(ContentDb db, RunState run, Lease a)
    {
        FloorDef fdef = db.Floors.FirstOrDefault(f => f.Id == a.FloorId) ?? throw new BuildException("no such floor");
        if (Legality.HasFloor(run.Tower, fdef.Index)) throw new BuildException($"{fdef.Name} is already leased");
        if (fdef.RequiresPortal && !run.PortalOpen) throw new BuildException("the portal is not open");
        run = Pay(run, fdef.Lease, fdef.Name);
        var floor = new SnapshotFloor(fdef.Index, new Footprint(fdef.Grid.W, fdef.Grid.H), FixedRooms(fdef), Array.Empty<SnapshotOccupant>());
        var floors = run.Tower.Floors.Append(floor).OrderBy(f => f.Index).ToArray();
        TowerSnapshot tower = run.Tower with { Floors = floors };
        if (fdef.Index == -1) tower = tower with { Globals = tower.Globals with { LeasedB1 = true } };
        return run with { Tower = tower };
    }

    public static SnapshotRoom[] FixedRooms(FloorDef fdef)
    {
        if (fdef.FixedRooms == null) return Array.Empty<SnapshotRoom>();
        return fdef.FixedRooms.Select((r, i) => new SnapshotRoom($"{fdef.Id[(fdef.Id.IndexOf('.') + 1)..]}_{r.DefId[(r.DefId.IndexOf('.') + 1)..]}", r.DefId, r.Rect, 0)).ToArray();
    }

    private static RunState RerollTab(ContentDb db, RunState run, Reroll a)
    {
        run = Pay(run, Economy.RerollCost(db, run.Tower), "a reroll");
        ShopState shop = Shop.Draw(db, run.Tower, run.Round, run.Shop, a.Tab);
        return run with { Shop = shop, Rng = shop.Rng };
    }
}
