using System.Text.Json;
using CompanyWars.Sim;

namespace CompanyWars.Content;

// C# records for every content file (CONTENT_SCHEMA.md §1, §10, §11). Sim-facing definitions live in
// CompanyWars.Sim (Content.cs); everything here is the build-side remainder and the file wrappers.
// A test round-trips every content file through these and fails on any lost or invented field.

public sealed record ContentIndex(string ContentVersion, string Schema, IndexFile[] Files);

public sealed record IndexFile(string Path, string Def);

public sealed record RulesFile(
    string Id,
    long SchemaVersion,
    RulesTime Time,
    RulesGoodwill Goodwill,
    RulesMorale Morale,
    RulesAnomaly Anomaly,
    RulesShare Share,
    RulesFloors Floors,
    RulesTenure Tenure,
    RulesPortal Portal,
    RulesRetrigger Retrigger,
    RulesUpkeep Upkeep);

public sealed record RulesTime(long TicksPerSecond, long QuarterTicks, long[] MonthStart, long[] PushMult, long[] RegenMult);

public sealed record RulesBaseCap(long Constant, long PerRound);

public sealed record RulesGoodwill(RulesBaseCap BaseCap, long RegenBasePermille, long RegenInterval, long RegenSuppressWindow, long SuppressThresholdPermille);

public sealed record RulesMorale(long Interval, long PerStack, long RatePermille);

public sealed record RulesAnomaly(long SelfCostPermille);

public sealed record RulesShare(long Total, long Start, long[] SpPerPushPermille);

public sealed record RulesFloors(long CorridorMult);

public sealed record RulesTenure(long[] TierRounds, long StepPermille, long StaffedPermille);

public sealed record RulesPortal(long ReceptionCapPerOccupant, long EmployeeCapTax, long B1LeaseCapTax);

public sealed record RulesRetrigger(long DepthMax);

public sealed record RulesUpkeep(long GoodwillPerUnpaidBudget);

public sealed record EconomyFile(
    string Id,
    long StartingBudget,
    EconomyIncome Income,
    WinBonus WinBonus,
    Dictionary<string, long> EmployeeCost,
    Dictionary<string, long> Severance,
    Dictionary<string, long> RoomCostByTiles,
    FurnitureCost FurnitureCost,
    long RerollCost,
    string RenovationFee,
    string RelocationFee,
    long RelocationTenurePenaltyRounds,
    long FurnitureSellRefund,
    string[] StartingRoster,
    string StartingRosterFloor);

public sealed record EconomyIncome(long Constant, long PerTwoRounds, long[] Table);

public sealed record WinBonus(long Fight, long Audit, long Boss);

public sealed record FurnitureCost(long Common, long Uncommon);

public sealed record FloorFile(FloorDef[] Floors);

public sealed record StatusFile(StatusDef[] Statuses);

public sealed record EmployeeFile(EmployeeDef[] Employees);

public sealed record RoomFile(RoomDef[] Rooms);

public sealed record FurnitureFile(FurnitureDef[] Furniture);

public sealed record RiderFile(RiderDef[] Riders);

public sealed record ModifierFile(ModifierDef[] Modifiers);

public sealed record FounderFile(FounderDef[] Founders);

public sealed record RecipeFile(Recipe[] Recipes);

public sealed record Recipe(string Id, string Name, string Class, RecipeInput[] Inputs, RecipeContext? Context, RecipeResult Result, string CodexHint);

public sealed record RecipeInput(RecipeMatch Match, long Count, bool Consumed, string Adjacency);

public sealed record RecipeMatch(string? DefId, string? Kind, long? Tier, string? Dept, string? Tag);

public sealed record RecipeContext(string Room);

public sealed record RecipeResult(string Kind, string? DefId, long? Tier, long? Rounds);

public sealed record ShopFile(
    string Id,
    long CardsPerTab,
    long RerollCost,
    DrawModel DrawModel,
    RecruiterNode RecruiterNode,
    Otherworld Otherworld,
    ShopTier[] Tiers,
    bool RoomsOnlyForOwnedFloors,
    string[] Leases);

public sealed record DrawModel(string Kind, string? Note);

public sealed record RecruiterNode(long CardsPerTab, bool FirstRerollFree);

public sealed record Otherworld(long Cards, long RerollCost, bool RequiresPortal);

public sealed record ShopTier(long[] Rounds, Dictionary<string, long> Staff, long[] RoomTiles, string[] FurnitureRarity);

public sealed record ModeFile(Mode[] Modes);

public sealed record Mode(
    string Id,
    string Name,
    string? Map,
    bool Interludes,
    RivalSource RivalSource,
    bool RivalGimmicks,
    long? GimmicksFromRound,
    PortalUnlock PortalUnlock,
    WinBonus WinBonus,
    long Strikes,
    long Rounds,
    bool MetaUnlocks,
    bool Rating,
    bool SnapshotCapture,
    bool DrawIsLossWithoutStrike,
    bool FirstRunTutorial);

public sealed record RivalSource(bool Scripted, bool Templates, bool Ghosts);

public sealed record PortalUnlock(string Condition, long? Act, long? Round);

public sealed record CampaignMap(string Id, Dictionary<string, NodeKind> NodeKinds, MapAct[] Acts, MapLayout Layout);

public sealed record NodeKind(bool Fight, string Icon, long? RivalRoundOffset, bool? AlwaysGimmick, long? Choices);

public sealed record MapAct(long Act, string Name, string Columns, long[] Rounds, string Boss, string[]? ScriptedFightsFirstRun, string? ConsultantAlwaysOffers);

public sealed record MapLayout(long[] RowsPerColumn, long BossRows, long[] AuditsPerAct, bool AuditNeverInFirstColumn);

public sealed record TutorialFile(string Id, TutorialHint[] Hints);

public sealed record TutorialHint(long Round, string Text);

public sealed record BalanceFile(
    string Id,
    BalanceSeeds Seeds,
    long[] SmokeRounds,
    Dictionary<string, JsonElement> Populations,
    Dictionary<string, JsonElement> Bands,
    Counter[] Counters,
    BossCounter[] BossCounters,
    Invariant[] Invariants);

public sealed record BalanceSeeds(long Smoke, long Nightly, long Search);

public sealed record Counter(string Winner, string Loser, string Why);

public sealed record BossCounter(string Boss, string Favoured, long FavouredMin, string? Punished, long? PunishedMax);

public sealed record Invariant(string Id, string Name, string Statement, string Population, string Measure, string Comparator, JsonElement Threshold, string Cadence, string Severity, string Source);

public sealed record TemplateFile(LeaseSchedule LeaseSchedule, Template[] Templates);

public sealed record LeaseSchedule(string Id, Dictionary<string, string[]> ByRound);

public sealed record Template(
    string Id,
    string Archetype,
    string[] NamePool,
    string[] FounderPool,
    long BudgetPermille,
    ShopListEntry[] Rooms,
    ShopListEntry[] Staff,
    string[] GimmickPool,
    TemplateLayout Layout);

public sealed record ShopListEntry(string DefId, long Weight, long MinRound);

public sealed record TemplateLayout(string[] FloorPreference, string[] FillOrder);

public sealed record ScriptedRival(string Id, string Name, string Kind, string Archetype, long Round, string? Gimmick, bool ExemptFromBudget, string Note, TowerSnapshot Snapshot);
