using CompanyWars.Sim;

namespace CompanyWars.Game;

/// <summary>What the battle screen fights: two snapshots, two names, a seed and a round; and whether a run is waiting for the result.</summary>
public sealed record FightSpec(TowerSnapshot A, TowerSnapshot B, string NameA, string NameB, uint Seed, long Round, bool InRun);
