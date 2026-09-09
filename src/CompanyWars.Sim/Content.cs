using System.Collections.Generic;

namespace CompanyWars.Sim;

// Resolved content definitions (CONTENT_SCHEMA.md §3–§9). These records mirror the schema
// field for field so that CompanyWars.Content can deserialise content files straight into
// them; the sim receives a ContentTable and never parses a file (SIMULATION_SPEC.md §18.5).

/// <summary>One effect from the closed vocabulary (CONTENT_SCHEMA.md §3). Which fields are set depends on <c>On</c> and <c>Do</c>.</summary>
public sealed record Effect(
    string On,
    string Do,
    string? Name,
    ValueSpec? Value,
    TargetSpec? Target,
    SubjectSpec? Subject,
    string? Status,
    long? Stacks,
    long? DurationTicks,
    string? Stat,
    long? Amount,
    long? Permille,
    string? Floor,
    string? Flag,
    string? Override,
    OverrideTo? To,
    long? Every,
    long? EveryN,
    long? Month,
    long? FromTier,
    long? UntilTier,
    ThenSpec? Then,
    long? Count,
    long? Rounds);

/// <summary>A value (§3.4): a constant, a per-tag form, or a permille of the target's cap. Exactly one form is set.</summary>
public sealed record ValueSpec(long? Constant, long? Base, string? PerTag, long? Each, long? PermilleOfTargetCap);

/// <summary>A target (§3.3). Enemy targets carry <c>Floor</c> and <c>Unit</c>; own targets carry <c>Scope</c> and optional filters and <c>Pick</c>; firm targets carry <c>Scope</c> = firm.</summary>
public sealed record TargetSpec(string Side, string? Floor, string? Unit, string? Scope, string[]? Dept, string? Tag, string? Pick);

public sealed record SubjectSpec(string Scope, string[]? Dept, string[]? NotDept, string? Tag);

public sealed record ThenSpec(string Status, long Stacks);

/// <summary>The <c>to</c> of an override: a selector name or a tier number.</summary>
public sealed record OverrideTo(string? Name, long? Tier);

public sealed record Placement(string[] Floors);

public sealed record EmployeeDef(
    string Id,
    string Name,
    string Dept,
    long Tier,
    long Cost,
    bool Extraplanar,
    bool InShop,
    string[] Tags,
    long CooldownTicks,
    Dictionary<string, long>? CooldownTicksByMonth,
    long InitialProgressPermille,
    Placement? Placement,
    string? CountsAsDept,
    Effect[] Effects,
    string Sprite,
    string? Flavor);

public sealed record RoomDef(
    string Id,
    string Name,
    string Kind,
    Footprint Footprint,
    string[] Floors,
    bool LandingLegal,
    long Cost,
    bool Fixed,
    long? MaxOccupants,
    Effect[] Effects,
    string Tile,
    string? Flavor);

public sealed record FurnitureDef(
    string Id,
    string Name,
    Footprint Footprint,
    bool WallMounted,
    long Cost,
    string Rarity,
    string[]? Floors,
    Effect[] Effects,
    string Sprite,
    string? Flavor);

public sealed record RiderDef(string Id, string Name, string Text, Effect[] Effects);

public sealed record BoardMeeting(string Cost, string Benefit);

public sealed record ModifierDef(string Id, string Name, string Text, bool RivalOnly, BoardMeeting? BoardMeeting, string? Teaches, Effect[] Effects);

public sealed record FounderDef(string Id, string Name, string Title, string Bio, string Portrait, string Badge, bool InShop, Effect[] Effects);

public sealed record StatusDef(
    string Id,
    string Name,
    long MaxStacks,
    long? DurationTicks,
    bool Expires,
    long CooldownRatePermillePerStack,
    long PushPenaltyPermillePerStack,
    long MoralePerStackPerEvent,
    ThenSpec[] OnExpire,
    string Tone,
    string Text);

public sealed record FixedRoom(string DefId, long[] Rect);

public sealed record FloorDef(
    string Id,
    long Index,
    string Name,
    string Kind,
    Footprint Grid,
    long OutputPermille,
    string[] RoomKinds,
    long UpkeepBudget,
    long CapTax,
    long Lease,
    bool Starting,
    bool RequiresPortal,
    bool Targetable,
    FixedRoom[]? FixedRooms);

/// <summary>The resolved content the sim is handed: a dictionary from id to definition per type.</summary>
public sealed class ContentTable
{
    public ContentTable(
        string contentVersion,
        IReadOnlyDictionary<string, EmployeeDef> employees,
        IReadOnlyDictionary<string, RoomDef> rooms,
        IReadOnlyDictionary<string, FurnitureDef> furniture,
        IReadOnlyDictionary<string, RiderDef> riders,
        IReadOnlyDictionary<string, ModifierDef> modifiers,
        IReadOnlyDictionary<string, FounderDef> founders,
        IReadOnlyDictionary<string, StatusDef> statuses,
        IReadOnlyDictionary<string, FloorDef> floors)
    {
        ContentVersion = contentVersion;
        Employees = employees;
        Rooms = rooms;
        Furniture = furniture;
        Riders = riders;
        Modifiers = modifiers;
        Founders = founders;
        Statuses = statuses;
        Floors = floors;
    }

    public string ContentVersion { get; }
    public IReadOnlyDictionary<string, EmployeeDef> Employees { get; }
    public IReadOnlyDictionary<string, RoomDef> Rooms { get; }
    public IReadOnlyDictionary<string, FurnitureDef> Furniture { get; }
    public IReadOnlyDictionary<string, RiderDef> Riders { get; }
    public IReadOnlyDictionary<string, ModifierDef> Modifiers { get; }
    public IReadOnlyDictionary<string, FounderDef> Founders { get; }
    public IReadOnlyDictionary<string, StatusDef> Statuses { get; }
    public IReadOnlyDictionary<string, FloorDef> Floors { get; }
}

/// <summary>The words of the vocabulary the sim recognises. A word not here is not a word (SIMULATION_SPEC.md §6.4).</summary>
public static class Vocabulary
{
    public const string StatusBurnout = "status.burnout";
    public const string StatusOvertime = "status.overtime";
    public const string StatusBureaucracy = "status.bureaucracy";
    public const string StatusFrozen = "status.frozen";

    public static readonly string[] Triggers = { "ability", "afterFire", "static", "periodic", "banner", "economy", "onHire", "onAccept" };
    public static readonly string[] Actions = { "push", "anomaly", "morale", "restore", "status", "cleanse", "retrigger", "stat", "flag", "override", "consumeAdjacentFurniture", "roomTenure" };
    public static readonly string[] Stats = { "push", "anomaly", "restore", "flatPush", "cooldown", "goodwillCap", "goodwillCapMult", "regenPerEvent", "passiveMult", "statusStacksBonus", "burnoutMaxOverride", "burnoutMaxDelta", "anomalySelfCost", "retriggerBonus", "floorOutput", "income", "upkeep", "rerollCost", "severance", "severanceMult" };
    public static readonly string[] Flags = { "untargetable", "bureaucracyImmune", "frozenImmune", "burnoutImmune", "overtimePermanent", "cannotBeRetriggered", "wholeFloorAdjacency", "capProtected", "regenNeverSuppressed", "receptionDisabled", "everyFloorMostPopulated", "floorSelectorMirror", "cannotBeLaidOff", "landingOnly" };
    public static readonly string[] Overrides = { "floorSelector", "tenureTier" };
    public static readonly string[] FloorSelectors = { "highest_occupied_floor", "lowest_occupied_floor", "most_populated_floor", "least_populated_floor", "same_floor_index", "random_floor", "all_floors" };
    public static readonly string[] UnitSelectors = { "lowest_cooldown_remaining", "highest_base_value", "random", "all" };
    public static readonly string[] OwnScopes = { "self", "adjacent", "sameFloor", "occupants", "all" };
    public static readonly string[] SubjectScopes = { "self", "adjacent", "sameFloor", "occupants", "all", "firm", "allRooms" };
}
