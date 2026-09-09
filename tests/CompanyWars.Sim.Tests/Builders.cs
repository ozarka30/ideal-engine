namespace CompanyWars.Sim.Tests;

/// <summary>Small snapshot builders for tests.</summary>
public static class Builders
{
    public static SnapshotFloor Ground(params SnapshotOccupant[] occupants) => new(0, new Footprint(5, 3), new[] { new SnapshotRoom("g_reception", "room.reception", new long[] { 1, 0, 2, 2 }, 0) }, occupants);

    public static SnapshotFloor Floor(long index, long w, long h, SnapshotRoom[] rooms, params SnapshotOccupant[] occupants) => new(index, new Footprint(w, h), rooms, occupants);

    public static SnapshotOccupant Emp(string defId, long col, long row, string instanceId) => new(new[] { col, row }, "employee", defId, instanceId, Array.Empty<string>());

    public static SnapshotOccupant Furn(string defId, long col, long row, string instanceId) => new(new[] { col, row }, "furniture", defId, instanceId, Array.Empty<string>());

    public static TowerSnapshot Tower(long round, string founder, SnapshotFloor[] floors, string[]? modifiers = null, RiderRef[]? riders = null, bool leasedB1 = false)
        => new(1, TestContent.Db.ContentVersion, round, floors, new SnapshotGlobals(founder, modifiers ?? Array.Empty<string>(), riders ?? Array.Empty<RiderRef>(), leasedB1));

    /// <summary>The §20 tower: one Junior Developer on 1F at column 2, row 1, in the corridor.</summary>
    public static TowerSnapshot MirrorJunior(string instanceId) => Tower(1, "founder.sato", new[]
    {
        Ground(),
        Floor(1, 5, 3, Array.Empty<SnapshotRoom>(), Emp("emp.junior_dev", 2, 1, instanceId)),
    });
}
