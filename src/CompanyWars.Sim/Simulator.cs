namespace CompanyWars.Sim;

/// <summary>The simulation as one pure function (SIMULATION_SPEC.md §1).</summary>
public static class Simulator
{
    /// <summary>
    /// Runs one match. Throws <see cref="SimulationInputException"/> when §5.1 validation rejects the inputs.
    /// <paramref name="content"/> is the resolved content table the snapshots' definitions resolve through.
    /// </summary>
    public static MatchResult Simulate(uint seed, TowerSnapshot a, TowerSnapshot b, RuleSet rules, ContentTable content)
    {
        var match = new Match(seed, a, b, rules, content);
        return match.Run(seed, a, b);
    }
}
