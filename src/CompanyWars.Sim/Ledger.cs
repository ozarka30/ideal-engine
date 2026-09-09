namespace CompanyWars.Sim;

/// <summary>One event in the match (SIMULATION_SPEC.md §16.1). Field order is normative for serialisation.</summary>
public sealed record LedgerEntry(
    long Seq,
    long Tick,
    string Kind,
    string SourceSide,
    long SourceUnit,
    string SourceInstanceId,
    long SourceFloor,
    string AbilityId,
    string TargetSide,
    long[] TargetUnits,
    long Raw,
    long GoodwillDelta,
    long CapDelta,
    long ShareDelta,
    long Overflow,
    long Stacks,
    long Depth,
    long Month,
    string[] Tags);

public sealed record SideValues(long A, long B);

/// <summary>The replay (SIMULATION_SPEC.md §16.2). There is no second format.</summary>
public sealed record MatchResult(
    long SchemaVersion,
    uint Seed,
    long Round,
    string RulesHash,
    string SnapshotHashA,
    string SnapshotHashB,
    string Winner,
    long EndTick,
    long FinalShare,
    SideValues FinalGoodwill,
    SideValues FinalCap,
    SideValues TotalPush,
    LedgerEntry[] Entries,
    string StateHash);

/// <summary>Thrown by <see cref="Simulator.Simulate"/> when setup validation (§5.1) rejects the inputs. A rejected match has no result.</summary>
public sealed class SimulationInputException : System.Exception
{
    public SimulationInputException(string message) : base(message)
    {
    }
}
