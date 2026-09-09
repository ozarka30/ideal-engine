using System.Text;

namespace CompanyWars.Sim;

/// <summary>A tower snapshot, exactly the fields the sim reads (SIMULATION_SPEC.md §4.2, D-18, D-25).</summary>
public sealed record TowerSnapshot(
    long SchemaVersion,
    string ContentVersion,
    long Round,
    SnapshotFloor[] Floors,
    SnapshotGlobals Globals)
{
    /// <summary>Canonical serialisation for <c>snapshotHashA/B</c> (§16.4).</summary>
    public string Canonical()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        Canon.Field(sb, "schemaVersion", SchemaVersion); sb.Append(',');
        Canon.Field(sb, "contentVersion", ContentVersion); sb.Append(',');
        Canon.Field(sb, "round", Round); sb.Append(',');
        Canon.Key(sb, "floors"); sb.Append('[');
        for (int f = 0; f < Floors.Length; f++)
        {
            if (f > 0) sb.Append(',');
            SnapshotFloor fl = Floors[f];
            sb.Append('{');
            Canon.Field(sb, "index", fl.Index); sb.Append(',');
            sb.Append("\"grid\":{");
            Canon.Field(sb, "w", fl.Grid.W); sb.Append(',');
            Canon.Field(sb, "h", fl.Grid.H); sb.Append("},");
            Canon.Key(sb, "rooms"); sb.Append('[');
            for (int r = 0; r < fl.Rooms.Length; r++)
            {
                if (r > 0) sb.Append(',');
                SnapshotRoom rm = fl.Rooms[r];
                sb.Append('{');
                Canon.Field(sb, "roomId", rm.RoomId); sb.Append(',');
                Canon.Field(sb, "defId", rm.DefId); sb.Append(',');
                Canon.Field(sb, "rect", rm.Rect); sb.Append(',');
                Canon.Field(sb, "tenureRounds", rm.TenureRounds);
                sb.Append('}');
            }
            sb.Append("],");
            Canon.Key(sb, "occupants"); sb.Append('[');
            for (int o = 0; o < fl.Occupants.Length; o++)
            {
                if (o > 0) sb.Append(',');
                SnapshotOccupant oc = fl.Occupants[o];
                sb.Append('{');
                Canon.Field(sb, "tile", oc.Tile); sb.Append(',');
                Canon.Field(sb, "kind", oc.Kind); sb.Append(',');
                Canon.Field(sb, "defId", oc.DefId); sb.Append(',');
                Canon.Field(sb, "instanceId", oc.InstanceId); sb.Append(',');
                Canon.Field(sb, "attachments", oc.Attachments);
                sb.Append('}');
            }
            sb.Append("]}");
        }
        sb.Append("],");
        sb.Append("\"globals\":{");
        Canon.Field(sb, "founderId", Globals.FounderId); sb.Append(',');
        Canon.Field(sb, "modifiers", Globals.Modifiers); sb.Append(',');
        Canon.Key(sb, "riders"); sb.Append('[');
        for (int r = 0; r < Globals.Riders.Length; r++)
        {
            if (r > 0) sb.Append(',');
            sb.Append('{');
            Canon.Field(sb, "instanceId", Globals.Riders[r].InstanceId); sb.Append(',');
            Canon.Field(sb, "riderId", Globals.Riders[r].RiderId);
            sb.Append('}');
        }
        sb.Append("],");
        Canon.Field(sb, "leasedB1", Globals.LeasedB1);
        sb.Append("}}");
        return sb.ToString();
    }
}

public sealed record Footprint(long W, long H);

public sealed record SnapshotFloor(long Index, Footprint Grid, SnapshotRoom[] Rooms, SnapshotOccupant[] Occupants);

/// <summary><c>Rect</c> is <c>[col, row, w, h]</c>.</summary>
public sealed record SnapshotRoom(string RoomId, string DefId, long[] Rect, long TenureRounds);

/// <summary><c>Tile</c> is <c>[col, row]</c>; <c>Kind</c> is <c>employee</c> or <c>furniture</c>; <c>Attachments</c> is always empty in v1 (D-13).</summary>
public sealed record SnapshotOccupant(long[] Tile, string Kind, string DefId, string InstanceId, string[] Attachments);

public sealed record SnapshotGlobals(string FounderId, string[] Modifiers, RiderRef[] Riders, bool LeasedB1);

public sealed record RiderRef(string InstanceId, string RiderId);
