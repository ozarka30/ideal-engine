using System.Collections.Generic;

namespace CompanyWars.Sim;

/// <summary>A unit's place in the canonical order (SIMULATION_SPEC.md §5.2), for consumers that read a ledger's <c>unitIndex</c>.</summary>
public sealed record OrderedUnit(long UnitIndex, string Side, long IndexWithinSide, long FloorIndex, long Col, long Row, string DefId, string InstanceId);

/// <summary>The canonical unit ordering, exposed so views name units the way the sim numbered them. Pure; no validation.</summary>
public static class UnitOrdering
{
    public static IReadOnlyList<OrderedUnit> Canonical(TowerSnapshot a, TowerSnapshot b)
    {
        var result = new List<OrderedUnit>();
        long index = 0;
        foreach ((TowerSnapshot snap, string side) in new[] { (a, "A"), (b, "B") })
        {
            long within = 0;
            var floors = new List<SnapshotFloor>(snap.Floors);
            floors.Sort((x, y) => x.Index.CompareTo(y.Index));
            foreach (SnapshotFloor floor in floors)
            {
                var units = new List<SnapshotOccupant>();
                foreach (SnapshotOccupant o in floor.Occupants)
                {
                    if (o.Kind == "employee") units.Add(o);
                }
                units.Sort((x, y) => x.Tile[1] != y.Tile[1] ? x.Tile[1].CompareTo(y.Tile[1]) : x.Tile[0].CompareTo(y.Tile[0]));
                foreach (SnapshotOccupant o in units)
                {
                    result.Add(new OrderedUnit(index++, side, within++, floor.Index, o.Tile[0], o.Tile[1], o.DefId, o.InstanceId));
                }
            }
        }
        return result;
    }
}
