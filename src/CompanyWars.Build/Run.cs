using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>A run: sixteen rounds against templated rivals, five strikes (the ranked-shaped loop, D-49). Everything random derives from RunSeed (ARCHITECTURE.md §5.4).</summary>
public static class Run
{
    /// <summary>2 since the revenue race (D-85): a fight's record keeps both firms' Revenue, not the final share.</summary>
    public const long SchemaVersion = 2;

    public static RunState New(ContentDb db, string modeId, uint runSeed, string founderId, string? firmName)
    {
        Mode mode = db.Modes.First(m => m.Id == modeId);
        FounderDef founder = db.Founders.First(f => f.Id == founderId);
        var floors = new List<SnapshotFloor>();
        foreach (FloorDef f in db.Floors.Where(f => f.Starting).OrderBy(f => f.Index))
        {
            floors.Add(new SnapshotFloor(f.Index, new Footprint(f.Grid.W, f.Grid.H), BuildReducer.FixedRooms(f), Array.Empty<SnapshotOccupant>()));
        }
        var tower = new TowerSnapshot(1, db.ContentVersion, 1, floors.ToArray(), new SnapshotGlobals(founderId, Array.Empty<string>(), Array.Empty<RiderRef>(), false));
        // The starting roster stands on the starting floor, corridor, left to right from column 1.
        FloorDef start = db.Floors.First(f => f.Id == db.Economy.StartingRosterFloor);
        SnapshotFloor sf = tower.Floors.First(f => f.Index == start.Index);
        var occ = new List<SnapshotOccupant>();
        long col = 1;
        int n = 0;
        foreach (string defId in db.Economy.StartingRoster)
        {
            occ.Add(new SnapshotOccupant(new[] { col++, 1L }, "employee", defId, $"e_00_{n++:D3}", Array.Empty<string>()));
        }
        tower = tower with { Floors = tower.Floors.Select(f => f.Index == sf.Index ? f with { Occupants = occ.ToArray() } : f).ToArray() };
        return new RunState(
            SchemaVersion, db.ContentVersion, modeId, runSeed, 1, mode.Strikes, db.Economy.StartingBudget,
            firmName ?? DefaultFirmName(founder), tower,
            new ShopState(new Dictionary<string, BagState>(StringComparer.Ordinal), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), runSeed),
            Array.Empty<string>(), false, Array.Empty<FightRecord>(), Hash(runSeed, 0, 0), 0,
            tower.Floors.SelectMany(f => f.Rooms.Select(r => r.RoomId)).ToArray(), false);
    }

    public static string DefaultFirmName(FounderDef founder)
    {
        string surname = founder.Name.Split(' ')[0];
        return surname + " Holdings";
    }

    /// <summary>Fight seeds are Hash(RunSeed, Round); template expansions take Hash(RunSeed, Round, NodeIndex).</summary>
    public static uint Hash(uint runSeed, long round, long nodeIndex) => Fnv1a.Hash($"{runSeed}|{round}|{nodeIndex}");

    public static uint FightSeed(RunState run) => Hash(run.RunSeed, run.Round, 1);

    /// <summary>The round's rival: a template drawn from the six by the round's seed, expanded at the round.</summary>
    public static Rival RivalFor(ContentDb db, RunState run)
    {
        Mode mode = db.Modes.First(m => m.Id == run.Mode);
        uint seed = Hash(run.RunSeed, run.Round, 0);
        var rng = new Mulberry32(seed);
        Template[] templates = db.Templates.Templates;
        Template t = templates[(int)rng.Draw((uint)templates.Length)];
        bool gimmick = mode.RivalGimmicks && mode.GimmicksFromRound.HasValue && run.Round >= mode.GimmicksFromRound.Value;
        return TemplateExpander.Expand(db, t.Id, run.Round, rng.Next(), gimmick);
    }

    public static RuleSet Rules(ContentDb db, RunState run) => db.RuleSetFor(run.Round);

    /// <summary>After the fight: strikes, the win bonus, the record, the next round or the end of the run.</summary>
    public static RunState AfterFight(ContentDb db, RunState run, Rival rival, MatchResult result)
    {
        Mode mode = db.Modes.First(m => m.Id == run.Mode);
        long strikes = run.Strikes;
        long budget = run.Budget;
        long won = run.FightsWon;
        if (result.Winner == "A")
        {
            budget += mode.WinBonus.Fight;
            won++;
        }
        else if (result.Winner == "B" || mode.DrawIsLossWithoutStrike == false)
        {
            strikes--;
        }
        var record = new FightRecord(run.Round, rival.Name, rival.Archetype, FightSeed(run), result.Winner, result.EndTick, result.FinalRevenue, result.StateHash);
        var history = run.History.Append(record).ToArray();
        bool over = strikes <= 0 || run.Round >= mode.Rounds;
        // Unpaid-upkeep copies last one round; the persisted modifiers are the run's own.
        TowerSnapshot tower = run.Tower with { Globals = run.Tower.Globals with { Modifiers = run.Modifiers } };
        return run with
        {
            Strikes = strikes,
            Budget = budget,
            FightsWon = won,
            History = history,
            Round = over ? run.Round : run.Round + 1,
            Tower = tower,
            Over = over,
        };
    }

    public static string Summary(ContentDb db, RunState run)
    {
        Mode mode = db.Modes.First(m => m.Id == run.Mode);
        string outcome = run.Strikes <= 0 ? "STRUCK OUT" : run.Round >= mode.Rounds && run.Over ? "SURVIVED THE YEAR" : "IN PROGRESS";
        return $"{run.FirmName} · {outcome} · {run.FightsWon} won of {run.History.Length} · {run.Strikes} strikes left";
    }
}
