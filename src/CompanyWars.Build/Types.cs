using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>A rival for one round: a name, an archetype and the snapshot the sim eats.</summary>
public sealed record Rival(string Name, string Archetype, string SourceId, TowerSnapshot Snapshot);

/// <summary>One fight's record in the run history (ARCHITECTURE.md §4.1).</summary>
public sealed record FightRecord(long Round, string RivalName, string RivalArchetype, uint Seed, string Winner, long EndTick, long FinalShare, string StateHash);

/// <summary>One bag: the eligible card ids for a tab, shuffled; draws come off the front; refilled only when empty (D-54).</summary>
public sealed record BagState(string[] Remaining);

/// <summary>The shop between rerolls: the bags and the cards on show.</summary>
public sealed record ShopState(Dictionary<string, BagState> Bags, string[] StaffCards, string[] RoomCards, string[] FurnitureCards, uint Rng);

/// <summary>Everything about the current run that must survive a restart (ARCHITECTURE.md §4.1). The tower is the snapshot the sim reads.</summary>
public sealed record RunState(
    long SchemaVersion,
    string ContentVersion,
    string Mode,
    uint RunSeed,
    long Round,
    long Strikes,
    long Budget,
    string FirmName,
    TowerSnapshot Tower,
    ShopState Shop,
    string[] Modifiers,
    bool PortalOpen,
    FightRecord[] History,
    uint Rng,
    long FightsWon,
    string[] HeldRoomIds,
    bool Over);

/// <summary>The closed action set of the build phase (D-43). Undo replays all but the last action from the round's start state.</summary>
public abstract record BuildAction;

public sealed record Hire(int CardIndex, long Floor, long Col, long Row) : BuildAction;

public sealed record BuyRoom(int CardIndex, long Floor, long Col, long Row) : BuildAction;

public sealed record BuyFurniture(int CardIndex, long Floor, long Col, long Row) : BuildAction;

public sealed record Move(string InstanceId, long Floor, long Col, long Row) : BuildAction;

public sealed record LayOff(string InstanceId) : BuildAction;

public sealed record Sell(string InstanceId) : BuildAction;

public sealed record Demolish(string RoomId) : BuildAction;

public sealed record Relocate(string RoomId, long Floor, long Col, long Row) : BuildAction;

public sealed record Lease(string FloorId) : BuildAction;

public sealed record Reroll(string Tab) : BuildAction;

/// <summary>Thrown when an action is illegal; the message is the hint line the screen shows.</summary>
public sealed class BuildException : Exception
{
    public BuildException(string message) : base(message)
    {
    }
}

/// <summary>The working state of a build phase: the start-of-round state, the action log, and the state the log produces.</summary>
public sealed record BuildState(RunState RoundStart, RunState Current, BuildAction[] Log, long UnpaidUpkeep, long Income, long Upkeep, long Passives);
