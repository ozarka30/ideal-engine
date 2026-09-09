using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>Placement rules (GAME_DESIGN.md §5.2, CONTENT_SCHEMA.md §12). Each check throws the hint the screen shows.</summary>
public static class Legality
{
    public static SnapshotFloor Floor(TowerSnapshot tower, long index)
    {
        foreach (SnapshotFloor f in tower.Floors)
        {
            if (f.Index == index) return f;
        }
        throw new BuildException(FloorName(index) + " is not leased");
    }

    public static bool HasFloor(TowerSnapshot tower, long index) => tower.Floors.Any(f => f.Index == index);

    public static string FloorName(long index) => index switch { -1 => "B1", 0 => "G", 1 => "1F", 2 => "2F", 3 => "3F", _ => "?" };

    public static SnapshotRoom? RoomAt(SnapshotFloor floor, long col, long row)
    {
        foreach (SnapshotRoom r in floor.Rooms)
        {
            if (col >= r.Rect[0] && col < r.Rect[0] + r.Rect[2] && row >= r.Rect[1] && row < r.Rect[1] + r.Rect[3]) return r;
        }
        return null;
    }

    public static SnapshotOccupant? OccupantAt(ContentDb db, SnapshotFloor floor, long col, long row, string? ignoreInstance = null)
    {
        foreach (SnapshotOccupant o in floor.Occupants)
        {
            if (o.InstanceId == ignoreInstance) continue;
            (long w, long h) = Footprint(db, o);
            if (col >= o.Tile[0] && col < o.Tile[0] + w && row >= o.Tile[1] && row < o.Tile[1] + h) return o;
        }
        return null;
    }

    public static (long W, long H) Footprint(ContentDb db, SnapshotOccupant o)
    {
        if (o.Kind == "furniture")
        {
            FurnitureDef f = db.Furniture.First(x => x.Id == o.DefId);
            return (f.Footprint.W, f.Footprint.H);
        }
        return (1, 1);
    }

    public static void CheckEmployeePlacement(ContentDb db, TowerSnapshot tower, EmployeeDef def, long floorIndex, long col, long row, string? ignoreInstance, bool landingOnly)
    {
        SnapshotFloor floor = Floor(tower, floorIndex);
        FloorDef fdef = db.FloorByIndex(floorIndex);
        if (col < 0 || row < 0 || col >= floor.Grid.W || row >= floor.Grid.H) throw new BuildException("that tile is outside the floor");
        if (OccupantAt(db, floor, col, row, ignoreInstance) != null) throw new BuildException("that tile is taken");
        if (def.Placement != null && Array.IndexOf(def.Placement.Floors, fdef.Id) < 0) throw new BuildException($"{def.Name} may only stand on {string.Join(", ", def.Placement.Floors.Select(f => FloorName(db.Floors.First(x => x.Id == f).Index)))}");
        if (floorIndex == -1 && !def.Extraplanar) throw new BuildException("only extraplanar staff may stand on B1");
        if (landingOnly && col != 0) throw new BuildException($"{def.Name} may only stand on the landing column");
        SnapshotRoom? room = RoomAt(floor, col, row);
        if (room != null)
        {
            RoomDef rdef = db.Rooms.First(r => r.Id == room.DefId);
            if (rdef.MaxOccupants.HasValue)
            {
                long occupants = floor.Occupants.Count(o => o.Kind == "employee" && o.InstanceId != ignoreInstance && Economy.Inside(o, room));
                if (occupants >= rdef.MaxOccupants.Value) throw new BuildException($"{rdef.Name} holds at most {rdef.MaxOccupants.Value}");
            }
        }
    }

    public static void CheckFurniturePlacement(ContentDb db, TowerSnapshot tower, FurnitureDef def, long floorIndex, long col, long row, string? ignoreInstance)
    {
        SnapshotFloor floor = Floor(tower, floorIndex);
        FloorDef fdef = db.FloorByIndex(floorIndex);
        if (def.Floors != null && Array.IndexOf(def.Floors, fdef.Id) < 0) throw new BuildException($"{def.Name} is not legal on {FloorName(floorIndex)}");
        SnapshotRoom? room = null;
        for (long dy = 0; dy < def.Footprint.H; dy++)
        {
            for (long dx = 0; dx < def.Footprint.W; dx++)
            {
                long c = col + dx, r = row + dy;
                if (c < 0 || r < 0 || c >= floor.Grid.W || r >= floor.Grid.H) throw new BuildException("that tile is outside the floor");
                if (OccupantAt(db, floor, c, r, ignoreInstance) != null) throw new BuildException("that tile is taken");
                SnapshotRoom? here = RoomAt(floor, c, r);
                if (here == null) throw new BuildException("furniture must be inside a room");
                if (room != null && here != room) throw new BuildException("furniture must lie inside one room");
                room = here;
            }
        }
    }

    public static void CheckRoomPlacement(ContentDb db, TowerSnapshot tower, RoomDef def, long floorIndex, long col, long row, string? ignoreRoomId)
    {
        SnapshotFloor floor = Floor(tower, floorIndex);
        FloorDef fdef = db.FloorByIndex(floorIndex);
        if (Array.IndexOf(def.Floors, fdef.Id) < 0 || Array.IndexOf(fdef.RoomKinds, def.Kind) < 0) throw new BuildException($"{def.Name} is not legal on {FloorName(floorIndex)}");
        long w = def.Footprint.W, h = def.Footprint.H;
        if (col < 0 || row < 0 || col + w > floor.Grid.W || row + h > floor.Grid.H) throw new BuildException("the room would leave the floor");
        if (!def.LandingLegal && col == 0) throw new BuildException($"{def.Name} may not include the landing column");
        foreach (SnapshotRoom o in floor.Rooms)
        {
            if (o.RoomId == ignoreRoomId) continue;
            if (col < o.Rect[0] + o.Rect[2] && o.Rect[0] < col + w && row < o.Rect[1] + o.Rect[3] && o.Rect[1] < row + h) throw new BuildException("rooms may not overlap");
        }
        // Furniture on the covered tiles must end up wholly inside this room, never straddling its edge.
        foreach (SnapshotOccupant o in floor.Occupants)
        {
            if (o.Kind != "furniture") continue;
            (long fw, long fh) = Footprint(db, o);
            bool overlaps = o.Tile[0] < col + w && col < o.Tile[0] + fw && o.Tile[1] < row + h && row < o.Tile[1] + fh;
            bool inside = o.Tile[0] >= col && o.Tile[0] + fw <= col + w && o.Tile[1] >= row && o.Tile[1] + fh <= row + h;
            if (overlaps && !inside) throw new BuildException("the room would cut through furniture");
        }
    }
}
