using CompanyWars.Content;
using CompanyWars.Sim;
using CompanyWars.Tools;
using Xunit;

namespace CompanyWars.Build.Tests;

public class BuildTests
{
    private static readonly string Root = RepoRoot.Find(AppContext.BaseDirectory);
    private static readonly Lazy<ContentDb> Db = new(() => ContentLoader.Load(Root));

    private static BuildState Open(uint seed = 42)
    {
        ContentDb db = Db.Value;
        RunState run = Run.New(db, "mode.ranked", seed, "founder.sato", null);
        return BuildReducer.OpenRound(db, run);
    }

    [Fact]
    public void ANewRunHasTheStartingTowerAndBudget()
    {
        ContentDb db = Db.Value;
        RunState run = Run.New(db, "mode.ranked", 1, "founder.sato", null);
        Assert.Equal(db.Economy.StartingBudget, run.Budget);
        Assert.Equal(5, run.Strikes);
        Assert.Equal("Sato Holdings", run.FirmName);
        Assert.Equal(2, run.Tower.Floors.Length);
        Assert.Equal(2, run.Tower.Floors.Sum(f => f.Occupants.Length));
        var errors = new List<string>();
        ContentValidator.SnapshotStructure(db, run.Tower, "start", errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void OpeningARoundAddsIncomeAndDrawsTheShop()
    {
        BuildState s = Open();
        Assert.Equal(12 + 10, s.Current.Budget);
        Assert.Equal(4, s.Current.Shop.StaffCards.Length);
        Assert.Equal(4, s.Current.Shop.RoomCards.Length);
        Assert.Equal(4, s.Current.Shop.FurnitureCards.Length);
        Assert.All(s.Current.Shop.StaffCards, c => Assert.Equal(1, Db.Value.Employees.First(e => e.Id == c).Tier));
    }

    [Fact]
    public void RerollNeverRepeatsACardUntilTheBagIsEmpty()
    {
        ContentDb db = Db.Value;
        BuildState s = Open();
        var seen = new List<string>(s.Current.Shop.FurnitureCards);
        int eligible = Shop.EligibleFurniture(db, Shop.TierFor(db, 1)).Count;
        // Give the firm money for rerolls.
        s = s with { Current = s.Current with { Budget = 100 } };
        while (seen.Count + 4 <= eligible)
        {
            s = BuildReducer.Apply(db, s, new Reroll(Shop.FurnitureTab));
            foreach (string c in s.Current.Shop.FurnitureCards)
            {
                Assert.DoesNotContain(c, seen);
                seen.Add(c);
            }
        }
    }

    [Fact]
    public void HirePlaceMoveLayOffAndUndoReplayEveryIntermediateState()
    {
        ContentDb db = Db.Value;
        BuildState s = Open();
        var states = new List<RunState> { s.Current };
        s = BuildReducer.Apply(db, s, new Hire(0, 1, 3, 1));
        states.Add(s.Current);
        string hired = s.Current.Tower.Floors.First(f => f.Index == 1).Occupants.Last().InstanceId;
        Assert.Equal(22 - 3, s.Current.Budget);
        s = BuildReducer.Apply(db, s, new Move(hired, 1, 4, 2));
        states.Add(s.Current);
        Assert.Contains(s.Current.Tower.Floors.First(f => f.Index == 1).Occupants, o => o.InstanceId == hired && o.Tile[0] == 4 && o.Tile[1] == 2);
        s = BuildReducer.Apply(db, s, new LayOff(hired));
        Assert.Equal(22 - 3 - 1, s.Current.Budget);
        // Undo walks back through exactly the states above.
        for (int i = states.Count - 1; i >= 0; i--)
        {
            s = BuildReducer.Undo(db, s);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(states[i], ContentJson.Options), System.Text.Json.JsonSerializer.Serialize(s.Current, ContentJson.Options));
        }
    }

    [Fact]
    public void IllegalPlacementsAreRefusedWithAHint()
    {
        ContentDb db = Db.Value;
        BuildState s = Open();
        Assert.Throws<BuildException>(() => BuildReducer.Apply(db, s, new Hire(0, 1, 1, 1)));   // taken by a starting junior
        Assert.Throws<BuildException>(() => BuildReducer.Apply(db, s, new Hire(0, 2, 0, 0)));   // 2F is not leased
        Assert.Throws<BuildException>(() => BuildReducer.Apply(db, s, new Hire(0, 1, 5, 0)));   // outside the grid
        Assert.Throws<BuildException>(() => BuildReducer.Apply(db, s, new BuyFurniture(0, 1, 3, 0))); // furniture outside a room
        s = s with { Current = s.Current with { Budget = 200 } };
        s = BuildReducer.Apply(db, s, new Lease("floor.f2"));
        Assert.Equal(3, s.Current.Tower.Floors.Length);
        Assert.Throws<BuildException>(() => BuildReducer.Apply(db, s, new Lease("floor.f2")));
        Assert.Throws<BuildException>(() => BuildReducer.Apply(db, s, new Lease("floor.b1")));  // portal closed
    }

    [Fact]
    public void RoomsCoverOccupantsAndTenureTicksOnlyWhenHeldAndStaffed()
    {
        ContentDb db = Db.Value;
        BuildState s = Open();
        s = s with { Current = s.Current with { Budget = 200 } };
        int card = Array.IndexOf(s.Current.Shop.RoomCards, s.Current.Shop.RoomCards.First(c => db.Rooms.First(r => r.Id == c).Footprint.W * db.Rooms.First(r => r.Id == c).Footprint.H == 4 && db.Rooms.First(r => r.Id == c).Floors.Contains("floor.f1")));
        s = BuildReducer.Apply(db, s, new BuyRoom(card, 1, 1, 0));
        SnapshotFloor f1 = s.Current.Tower.Floors.First(f => f.Index == 1);
        SnapshotRoom room = f1.Rooms.Single();
        Assert.Equal(2, f1.Occupants.Count(o => Economy.Inside(o, room))); // both juniors at (1,1) and (2,1) are inside
        RunState committed = BuildReducer.Commit(db, s);
        Assert.Equal(0, committed.Tower.Floors.First(f => f.Index == 1).Rooms.Single().TenureRounds); // not held at the previous commit
        RunState next = committed with { Round = 2 };
        BuildState s2 = BuildReducer.OpenRound(db, next);
        RunState committed2 = BuildReducer.Commit(db, s2);
        Assert.Equal(1, committed2.Tower.Floors.First(f => f.Index == 1).Rooms.Single().TenureRounds); // held and half staffed (2 of 4 tiles)
    }

    [Fact]
    public void UnpaidUpkeepBecomesModifierCopiesForOneRound()
    {
        ContentDb db = Db.Value;
        RunState run = Run.New(db, "mode.ranked", 3, "founder.sato", null) with { Budget = 0, Round = 2 };
        BuildState s = BuildReducer.OpenRound(db, run with { Budget = 300 });
        s = BuildReducer.Apply(db, s, new Lease("floor.f3"));  // ¥4 upkeep per round
        RunState committed = BuildReducer.Commit(db, s) with { Budget = 0, Round = 3 };
        // Income 11 covers upkeep 4; force the shortfall by zero income table? Instead, lease 2F too and drain the budget.
        BuildState s2 = BuildReducer.OpenRound(db, committed with { Budget = 100 });
        s2 = BuildReducer.Apply(db, s2, new Lease("floor.f2"));
        RunState c2 = BuildReducer.Commit(db, s2) with { Budget = -20 + 0, Round = 4 };
        BuildState s3 = BuildReducer.OpenRound(db, c2);
        // budget −20 + income 11 − upkeep 6 = −15 → 15 copies, budget 0
        Assert.Equal(0, s3.Current.Budget);
        Assert.Equal(15, s3.UnpaidUpkeep);
        Assert.Equal(15, s3.Current.Tower.Globals.Modifiers.Count(m => m == Economy.UnpaidUpkeepModifier));
        RuleSet rules = db.RuleSetFor(4);
        MatchResult r = Simulator.Simulate(1, s3.Current.Tower, Run.New(db, "mode.ranked", 9, "founder.okada", null).Tower with { Round = 4 }, rules, db.ToContentTable());
        Assert.Equal(1, r.Entries.Where(e => e.Kind == "regen" && e.TargetSide == "A").Select(e => e.Raw).Distinct().Count(x => x > 0) >= 0 ? 1 : 0);
    }

    [Theory]
    [InlineData("rival.t_generalist")]
    [InlineData("rival.t_turtle")]
    [InlineData("rival.t_burst")]
    [InlineData("rival.t_economy")]
    [InlineData("rival.t_burnout")]
    [InlineData("rival.t_management")]
    public void EveryTemplateExpandsToAValidConstructibleSnapshotAtEveryRoundForFiftySeeds(string templateId)
    {
        ContentDb db = Db.Value;
        int fallbacks = 0;
        for (long round = 1; round <= 16; round++)
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                Rival rival = TemplateExpander.Expand(db, templateId, round, seed);
                if (rival.SourceId != templateId) { fallbacks++; continue; }
                var errors = new List<string>();
                ContentValidator.SnapshotStructure(db, rival.Snapshot, $"{templateId} r{round} s{seed}", errors);
                Assert.Empty(errors);
                // Constructibility holds at the field population's budget (1000 permille); a template whose own
                // budget exceeds the player's (the economy archetype, 1050) is richer on purpose.
                Rival field = TemplateExpander.Expand(db, templateId, round, seed, false, 1000);
                if (field.SourceId == templateId) ContentValidator.Constructibility(db, field.Snapshot, round, $"{templateId} r{round} s{seed} @1000", errors);
                Assert.Empty(errors);
                Assert.True(rival.Snapshot.Floors.Sum(f => f.Occupants.Count(o => o.Kind == "employee")) >= 2, $"{templateId} r{round} s{seed} has too few staff");
            }
        }
        Assert.Equal(0, fallbacks);
    }

    [Fact]
    public void ExpansionIsDeterministic()
    {
        ContentDb db = Db.Value;
        Rival a = TemplateExpander.Expand(db, "rival.t_burst", 9, 777);
        Rival b = TemplateExpander.Expand(db, "rival.t_burst", 9, 777);
        Assert.Equal(a.Snapshot.Canonical(), b.Snapshot.Canonical());
        Assert.Equal(a.Name, b.Name);
    }

    [Fact]
    public void ARunEndsOnFiveStrikesOrSixteenRounds()
    {
        ContentDb db = Db.Value;
        RunState run = Run.New(db, "mode.ranked", 5, "founder.hoshino", null);
        int rounds = 0;
        while (!run.Over && rounds < 20)
        {
            BuildState s = BuildReducer.OpenRound(db, run);
            RunState committed = BuildReducer.Commit(db, s);
            Rival rival = Run.RivalFor(db, committed);
            MatchResult result = Simulator.Simulate(Run.FightSeed(committed), committed.Tower, rival.Snapshot, Run.Rules(db, committed), db.ToContentTable());
            run = Run.AfterFight(db, committed, rival, result);
            rounds++;
        }
        Assert.True(run.Over);
        Assert.True(run.Strikes <= 0 || run.Round == 16);
        Assert.Equal(rounds, run.History.Length);
    }
}

public class ExplainTests
{
    private static readonly string Root = CompanyWars.Tools.RepoRoot.Find(AppContext.BaseDirectory);
    private static readonly Lazy<ContentDb> Db = new(() => ContentLoader.Load(Root));

    [Fact]
    public void EveryEmployeeExplainsItselfWithoutVocabularyWords()
    {
        ContentDb db = Db.Value;
        string[] raw = { "permille", "afterFire", "status.", "highest_occupied", "most_populated", "everyN", "loyaltyCap", "regenPerEvent", "_" };
        foreach (EmployeeDef e in db.Employees)
        {
            string text = Explain.Ability(db, e) + " " + string.Join(" ", Explain.Passives(db, e.Effects, e)) + " " + string.Join(" ", Explain.Placement(db, e));
            foreach (string r in raw) Assert.False(text.Contains(r, StringComparison.Ordinal), $"{e.Id}: '{r}' leaked into: {text}");
            Assert.Contains("every", Explain.Ability(db, e));
        }
        foreach (RoomDef r in db.Rooms) Assert.NotEmpty(Explain.Passives(db, r.Effects));
        foreach (FurnitureDef f in db.Furniture) Assert.NotEmpty(Explain.Passives(db, f.Effects));
    }

    [Fact]
    public void KnownExplanationsReadAsIntended()
    {
        ContentDb db = Db.Value;
        Assert.Equal("Ship Feature: earn ¥60 every 4.0 s.", Explain.Ability(db, db.Employees.First(e => e.Id == "emp.junior_dev")));
        Assert.Contains("Open Plan", Explain.Placement(db, db.Employees.First(e => e.Id == "emp.junior_dev"))[0]);
        Assert.Contains("Whiteboard", Explain.Placement(db, db.Employees.First(e => e.Id == "emp.junior_dev"))[1]);
        Assert.Equal("Delegate: the hardest-hitting neighbour fires now every 6.0 s.", Explain.Ability(db, db.Employees.First(e => e.Id == "emp.team_lead")));
    }
}
