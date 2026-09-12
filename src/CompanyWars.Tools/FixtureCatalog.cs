using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Tools;

/// <summary>The minimum conformance fixture set of SIMULATION_SPEC.md §19, as snapshot inputs.</summary>
public static class FixtureCatalog
{
    private static readonly string[] None = Array.Empty<string>();

    public static IEnumerable<FixtureInput> All(ContentDb db)
    {
        string v = db.ContentVersion;
        yield return new FixtureInput("mirror_junior", "One Junior Developer each, corridor. Ends in a draw at the Bell (§20).", 1, 1,
            Tower(v, 1, "founder.sato", Ground(), Corridor(1, Emp("emp.junior_dev", 2, 1, "e_a1"))),
            Tower(v, 1, "founder.sato", Ground(), Corridor(1, Emp("emp.junior_dev", 2, 1, "e_b1"))), null);

        yield return new FixtureInput("overflow", "A's single Architect at round 16 against an empty tower. Large Sales; needs a Poach input to overflow again (revision 2).", 2, 16,
            Tower(v, 16, "founder.nakagawa", Ground(), Corridor(1, Emp("emp.architect", 2, 1, "e_a1"))),
            Tower(v, 16, "founder.sato", Ground(), Corridor(1)), null);

        yield return new FixtureInput("regen_suppress", "QA Tester against a Recruiter. Needs a Poach below the suppress threshold to test regen again (revision 2).", 3, 1,
            Tower(v, 1, "founder.nakagawa", Ground(), Corridor(1, Emp("emp.qa_tester", 2, 1, "e_a1"))),
            Tower(v, 1, "founder.ueda", Ground(), Corridor(1, Emp("emp.recruiter", 2, 1, "e_b1"))), null);

        yield return new FixtureInput("burnout_pierce", "Consultant against a Legal turtle. Scandal erosion; Bell ordering.", 4, 6,
            Tower(v, 6, "founder.moriyama", Ground(), Corridor(1, Emp("emp.consultant", 2, 1, "e_a1"), Emp("emp.junior_dev", 3, 1, "e_a2"))),
            Tower(v, 6, "founder.okada",
                Ground(Emp("emp.paralegal", 1, 0, "e_b0")),
                Floor(1, 5, 3, new[] { Room("f1_legal", "room.legal_dept", 0, 0, 2, 2, 6) },
                    Emp("emp.counsel", 0, 0, "e_b1"), Emp("emp.paralegal", 1, 0, "e_b2"), Emp("emp.patent_attorney", 0, 1, "e_b3"), Furn("furn.filing_cabinet", 1, 1, "f_b4"))), null);

        yield return new FixtureInput("retrigger_depth", "Team Lead adjacent to a Director adjacent to three units. Depth whiffs.", 5, 4,
            Tower(v, 4, "founder.moriyama", Ground(), Corridor(1,
                Emp("emp.junior_dev", 2, 0, "e_a1"), Emp("emp.team_lead", 1, 1, "e_a2"), Emp("emp.director", 2, 1, "e_a3"), Emp("emp.junior_dev", 3, 1, "e_a4"), Emp("emp.junior_dev", 2, 2, "e_a5"))),
            Tower(v, 4, "founder.sato", Ground(), Corridor(1, Emp("emp.junior_dev", 2, 1, "e_b1"))), null);

        yield return new FixtureInput("tie_parity", "Two identical towers with identical 3.0 s cooldowns. Initiative by tick parity.", 6, 1,
            Tower(v, 1, "founder.hoshino", Ground(), Corridor(1, Emp("emp.sales_rep", 2, 1, "e_a1"))),
            Tower(v, 1, "founder.hoshino", Ground(), Corridor(1, Emp("emp.sales_rep", 2, 1, "e_b1"))), null);

        EmployeeDef paralegal = db.Employees.First(e => e.Id == "emp.paralegal");
        var randomParalegal = paralegal with
        {
            Id = "emp.test_random_paralegal",
            Name = "Paralegal (random selectors, fixture only)",
            Effects = paralegal.Effects.Select(e => e.On == "ability" ? e with { Target = new TargetSpec("enemy", "random_floor", "random", null, null, null, null) } : e).ToArray(),
        };
        yield return new FixtureInput("random_selector", "A Paralegal with random_floor / random against a three-floor tower. RNG draw order. The definition is a fixture-local overlay: no shipped employee uses a random selector.", 12345, 3,
            Tower(v, 3, "founder.okada", Ground(), Corridor(1, Emp("emp.test_random_paralegal", 2, 1, "e_a1"))),
            Tower(v, 3, "founder.sato",
                Ground(Emp("emp.junior_dev", 0, 0, "e_b0")),
                Corridor(1, Emp("emp.junior_dev", 2, 1, "e_b1"), Emp("emp.qa_tester", 3, 1, "e_b2")),
                Corridor(2, Emp("emp.junior_dev", 2, 1, "e_b3"))),
            new[] { randomParalegal });

        yield return new FixtureInput("anomaly_selfcost", "Salaryman Ghost in a Summoning Circle with Ofuda (A) and without (B). Curse and its self-cost.", 8, 6,
            TowerFull(v, 6, "founder.kitamura",
                new[] { Basement(new[] { Room("b1_circle", "room.summoning_circle", 0, 0, 2, 2, 0) }, Emp("emp.x_salaryman_ghost", 0, 0, "e_a1"), Furn("furn.ofuda", 1, 0, "f_a2")), Ground(), Corridor(1) },
                None, new[] { new RiderRef("e_a1", "rider.tenured") }, leasedB1: true),
            TowerFull(v, 6, "founder.kitamura",
                new[] { Basement(new[] { Room("b1_circle", "room.summoning_circle", 0, 0, 2, 2, 0) }, Emp("emp.x_salaryman_ghost", 0, 0, "e_b1")), Ground(), Corridor(1) },
                None, new[] { new RiderRef("e_b1", "rider.tenured") }, leasedB1: true), null);

        yield return new FixtureInput("banner_order", "Yakult Cart, Overtime Culture and Bad Influence on the same tower at tick 0.", 9, 5,
            TowerFull(v, 5, "founder.ueda",
                new[]
                {
                    Ground(),
                    Floor(1, 5, 3, new[] { Room("f1_open", "room.open_plan", 0, 0, 2, 3, 0) },
                        Emp("emp.junior_dev", 1, 0, "e_a1"), Emp("emp.junior_dev", 0, 1, "e_a2"), Furn("furn.yakult_cart", 1, 1, "f_a3"), Emp("emp.x_kappa_intern", 0, 2, "e_a4"), Emp("emp.junior_dev", 1, 2, "e_a5")),
                },
                new[] { "mod.overtime_culture" }, new[] { new RiderRef("e_a4", "rider.bad_influence") }, leasedB1: false),
            Tower(v, 5, "founder.sato", Ground(), Corridor(1, Emp("emp.junior_dev", 2, 1, "e_b1"))), null);

        ScriptedRival boss = db.Rival("rival.boss_compliance_office");
        yield return new FixtureInput("boss_act2", "The Compliance Office against the intended Act 2 counter-build: out-earn it, with Scandal and Curse.", 10, boss.Round,
            TowerFull(v, boss.Round, "founder.hoshino",
                new[]
                {
                    Basement(new[] { Room("b1_circle", "room.summoning_circle", 0, 0, 2, 2, 3) }, Emp("emp.x_salaryman_ghost", 0, 0, "e_a10"), Emp("emp.x_efficiency_wraith", 1, 0, "e_a11")),
                    Ground(Emp("emp.sales_rep", 1, 0, "e_a1"), Emp("emp.paralegal", 2, 0, "e_a2")),
                    Floor(1, 5, 3, new[] { Room("f1_meeting", "room.meeting_room", 1, 0, 2, 2, 6) },
                        Emp("emp.consultant", 1, 0, "e_a3"), Emp("emp.consultant", 2, 0, "e_a4"), Emp("emp.senior_dev", 3, 0, "e_a5"), Emp("emp.headhunter", 1, 1, "e_a6"), Emp("emp.team_lead", 2, 1, "e_a7")),
                    Floor(2, 5, 3, new[] { Room("f2_sales", "room.sales_floor", 1, 0, 2, 2, 3) },
                        Emp("emp.sales_rep", 1, 0, "e_a8"), Emp("emp.account_manager", 2, 0, "e_a9"), Emp("emp.headhunter", 1, 1, "e_a12")),
                },
                None, new[] { new RiderRef("e_a10", "rider.tenured"), new RiderRef("e_a11", "rider.union_dispute") }, leasedB1: true),
            boss.Snapshot, null);
    }

    private static SnapshotFloor Ground(params SnapshotOccupant[] occupants) => new(0, new Footprint(5, 3), new[] { Room("g_reception", "room.reception", 1, 0, 2, 2, 0) }, occupants);

    private static SnapshotFloor Corridor(long index, params SnapshotOccupant[] occupants) => new(index, new Footprint(5, 3), Array.Empty<SnapshotRoom>(), occupants);

    private static SnapshotFloor Floor(long index, long w, long h, SnapshotRoom[] rooms, params SnapshotOccupant[] occupants) => new(index, new Footprint(w, h), rooms, occupants);

    private static SnapshotFloor Basement(SnapshotRoom[] rooms, params SnapshotOccupant[] occupants) => new(-1, new Footprint(3, 3), rooms, occupants);

    private static SnapshotRoom Room(string id, string defId, long col, long row, long w, long h, long tenure) => new(id, defId, new[] { col, row, w, h }, tenure);

    private static SnapshotOccupant Emp(string defId, long col, long row, string id) => new(new[] { col, row }, "employee", defId, id, Array.Empty<string>());

    private static SnapshotOccupant Furn(string defId, long col, long row, string id) => new(new[] { col, row }, "furniture", defId, id, Array.Empty<string>());

    private static TowerSnapshot Tower(string version, long round, string founder, params SnapshotFloor[] floors) => TowerFull(version, round, founder, floors, None, Array.Empty<RiderRef>(), false);

    private static TowerSnapshot TowerFull(string version, long round, string founder, SnapshotFloor[] floors, string[] modifiers, RiderRef[] riders, bool leasedB1)
        => new(1, version, round, floors, new SnapshotGlobals(founder, modifiers, riders, leasedB1));
}
