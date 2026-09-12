using System.Text;
using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>
/// Plain-language explanations generated from content (GAME_DESIGN.md §20: every mechanic legible on the screen
/// where it matters). Nothing here is authored per entity: a new employee explains itself from its effects.
/// </summary>
public static class Explain
{
    /// <summary>What a damage or support kind does to the fight, in one sentence.</summary>
    public static string Kind(string kind) => kind switch
    {
        "sales" => "Sales add money to your Revenue in proportion to your Client Loyalty: wavering clients buy less.",
        "poach" => "Poach drains the rival's Client Loyalty; once it is empty, every Poach moves money from their Revenue to yours.",
        "scandal" => "A Scandal shrinks a firm's Loyalty cap for the rest of the quarter and hands a quarter of its size in money to the other firm.",
        "curse" => "A Curse moves money from the rival to you straight through their Loyalty, but a quarter of it rebounds on your own Loyalty.",
        "pr" => "PR rebuilds your own Client Loyalty.",
        "status" => "A status changes how an employee works for a while.",
        "cleanse" => "Cleanse removes status stacks from your own people.",
        "retrigger" => "A retrigger makes another employee fire now, without resetting their cooldown.",
        _ => string.Empty,
    };

    public static string Status(string statusId) => statusId switch
    {
        "status.burnout" => "Burnout: −5% output per stack, and their firm causes itself a Scandal every second. Never wears off in a fight.",
        "status.overtime" => "Overtime: +50% cooldown speed per stack for 3 s, then one Burnout when it wears off.",
        "status.bureaucracy" => "Bureaucracy: −20% cooldown speed per stack for 5 s.",
        "status.frozen" => "Frozen: the cooldown stops entirely for the duration.",
        _ => statusId,
    };

    public static string Seconds(long ticks) => $"{ticks / 20}.{ticks % 20 / 2} s";

    /// <summary>The ability in one sentence: "Ship Feature: 60 Push every 4.0 s."</summary>
    public static string Ability(ContentDb db, EmployeeDef e)
    {
        Effect ab = e.Effects.First(x => x.On == "ability");
        string name = ab.Name ?? "Ability";
        return $"{name}: {Action(db, ab, e)} every {Seconds(e.CooldownTicks)}.";
    }

    /// <summary>One effect as a phrase, for any owner.</summary>
    public static string Action(ContentDb db, Effect e, EmployeeDef? owner = null)
    {
        string target = Target(e.Target);
        switch (e.Do)
        {
            case "sales": return $"earn {Money(e.Value, owner)}";
            case "poach": return $"poach {Money(e.Value, owner)} from the rival";
            case "curse": return $"curse {Money(e.Value, owner)} from the rival";
            case "scandal": return $"a {Value(e.Value, owner)} Scandal on {(e.Target?.Side == "own" ? "your firm" : "the rival")}";
            case "pr": return $"rebuild {Value(e.Value, owner)} Loyalty";
            case "status": return $"{e.Stacks} {StatusName(e.Status)}{(e.DurationTicks.HasValue ? $" for {Seconds(e.DurationTicks.Value)}" : string.Empty)} to {target}";
            case "cleanse": return $"remove {e.Stacks} {StatusName(e.Status)} from {target}";
            case "retrigger": return $"{target} fire{(e.Target?.Pick != null || e.Target?.Scope == "self" ? "s" : string.Empty)} now{(e.Then != null ? $", then {e.Then.Stacks} {StatusName(e.Then.Status)} each" : string.Empty)}";
            case "stat": return Stat(db, e);
            case "flag": return Flag(e.Flag);
            case "override": return e.Override == "floorSelector" ? $"targets the {Selector(e.To?.Name)} instead" : $"every room counts as Tier {e.To?.Tier}";
            default: return e.Do;
        }
    }

    /// <summary>Every effect besides the ability, each as a short line.</summary>
    public static List<string> Passives(ContentDb db, Effect[] effects, EmployeeDef? owner = null)
    {
        var lines = new List<string>();
        foreach (Effect e in effects)
        {
            switch (e.On)
            {
                case "ability": break;
                case "afterFire": lines.Add($"{(e.EveryN is > 1 ? $"Every {Ordinal(e.EveryN.Value)} fire" : "After each fire")}: {Action(db, e, owner)}."); break;
                case "static": lines.Add(Capitalise(Action(db, e, owner)) + (e.FromTier.HasValue ? $" (Tier {Roman(e.FromTier.Value)} and up)" : e.UntilTier.HasValue ? $" (until Tier {Roman(e.UntilTier.Value)})" : string.Empty) + "."); break;
                case "periodic": lines.Add($"Every {Seconds(e.Every ?? 0)}: {Action(db, e, owner)}."); break;
                case "banner": lines.Add($"At {Month(e.Month ?? 0)}: {Action(db, e, owner)}{(e.UntilTier.HasValue ? $" (until Tier {Roman(e.UntilTier.Value)})" : string.Empty)}."); break;
                case "economy": lines.Add(Economy(e) + "."); break;
                default: break;
            }
        }
        return lines;
    }

    /// <summary>Where this employee wants to stand, from the rooms and furniture that boost its department.</summary>
    public static List<string> Placement(ContentDb db, EmployeeDef e)
    {
        var lines = new List<string>();
        var rooms = new List<string>();
        foreach (RoomDef r in db.Rooms)
        {
            if (r.Fixed) continue;
            foreach (Effect x in r.Effects)
            {
                if (x.On != "static" || x.Do != "stat" || x.Permille == null || x.Permille <= 1000) continue;
                if (x.Stat is not ("sales" or "poach" or "curse" or "pr" or "passiveMult")) continue;
                if (!Matches(x.Subject, e)) continue;
                rooms.Add($"{r.Name} ×{x.Permille / 1000}.{x.Permille % 1000 / 100}");
                break;
            }
        }
        var furniture = new List<string>();
        foreach (FurnitureDef f in db.Furniture)
        {
            foreach (Effect x in f.Effects)
            {
                if (x.On == "economy") continue;
                string[]? dept = x.Subject?.Dept ?? x.Target?.Dept;
                if (dept != null && !Matches(new SubjectSpec("adjacent", dept, null, null), e)) continue;
                if (x.Subject?.Scope != "adjacent" && x.Target?.Scope != "adjacent") continue;
                furniture.Add(f.Name);
                break;
            }
        }
        lines.Add(rooms.Count > 0 ? $"Boosted inside: {string.Join(", ", rooms)}." : "No room boosts this department; any room beats the corridor (×0.9).");
        if (furniture.Count > 0) lines.Add($"Likes standing next to: {string.Join(", ", furniture.Take(4))}.");
        Effect ab = e.Effects.First(x => x.On == "ability");
        if (ab.Target?.Side == "own" && ab.Target.Scope is "adjacent" or "sameFloor") lines.Add($"Its ability reaches {(ab.Target.Scope == "adjacent" ? "the four tiles around it, and the landing tile above and below on column 0" : "everyone on its floor")}: put it beside who it should help.");
        foreach (Effect x in e.Effects)
        {
            if (x.On == "afterFire" && x.Target?.Side == "own" && x.Target.Scope == "adjacent") { lines.Add("Its follow-up hits the tiles around it: neighbours matter."); break; }
        }
        if (e.Placement != null) lines.Add($"May only stand on {string.Join(", ", e.Placement.Floors.Select(f => db.Floors.First(x => x.Id == f).Name))}.");
        lines.Add("Higher floors multiply output: G ×0.9, 1F ×1.0, 2F ×1.15, 3F ×1.45.");
        return lines;
    }

    /// <summary>How a fight works, for the firm panel: a heading and the sentences a first-time player needs before pressing READY. The screen wraps them.</summary>
    public static string[] Primer() => new[]
    {
        "HOW A QUARTER WORKS",
        "Each employee works when its cooldown fills.",
        "Sales add money to your Revenue; Poach and Curse take it from the rival's.",
        "Client Loyalty shields your Revenue from Poaching, and regrows every 2 s unless you were just Poached.",
        "The Bell rings at 60 s: the firm with more Revenue wins.",
        "Rooms multiply everyone inside; the corridor is ×0.9.",
        "Furniture and abilities reach the tiles beside them.",
    };


    // ---------------------------------------------------------------- pieces

    private static string Value(ValueSpec? v, EmployeeDef? owner)
    {
        if (v == null) return string.Empty;
        if (v.Constant.HasValue) return v.Constant.Value.ToString();
        if (v.Base.HasValue) return $"{v.Base} + {v.Each} per {v.PerTag} employee";
        return $"{(v.PermilleOfTargetCap ?? 0) / 10}% of the rival's Loyalty cap";
    }

    /// <summary>A value that is money: "¥60" for a constant; the other forms read as <see cref="Value"/>.</summary>
    private static string Money(ValueSpec? v, EmployeeDef? owner) => v?.Constant is long c ? $"¥{c}" : Value(v, owner);

    private static string Target(TargetSpec? t)
    {
        if (t == null) return "the rival";
        if (t.Side == "enemy")
        {
            if (t.Scope == "firm") return "the rival";
            string floor = Selector(t.Floor);
            string unit = t.Unit switch
            {
                "all" => "every enemy",
                "random" => "a random enemy",
                "lowest_cooldown_remaining" => "the enemy about to fire",
                "highest_base_value" => "the enemy's hardest hitter",
                _ => "an enemy",
            };
            return $"{unit} on {floor}";
        }
        if (t.Scope == "firm") return "your firm";
        string who = t.Scope switch
        {
            "self" => "itself",
            "adjacent" => "each neighbour",
            "sameFloor" => "everyone on its floor",
            "occupants" => "everyone in the room",
            "all" => "everyone in the tower",
            _ => t.Scope ?? "own",
        };
        if (t.Dept != null) who = $"each {string.Join("/", t.Dept)} {(t.Scope == "adjacent" ? "neighbour" : "employee")}";
        if (t.Pick == "highest_base_value") who = $"the hardest-hitting {(t.Dept != null ? string.Join("/", t.Dept) + " " : string.Empty)}neighbour";
        return who;
    }

    private static string Selector(string? floor) => floor switch
    {
        "highest_occupied_floor" => "their highest floor",
        "lowest_occupied_floor" => "their lowest floor",
        "most_populated_floor" => "their busiest floor",
        "least_populated_floor" => "their emptiest floor",
        "same_floor_index" => "the same floor as this one",
        "random_floor" => "a random floor",
        "all_floors" => "every floor",
        _ => "their floor",
    };

    private static string StatusName(string? id) => id switch
    {
        "status.burnout" => "Burnout",
        "status.overtime" => "Overtime",
        "status.bureaucracy" => "Bureaucracy",
        "status.frozen" => "Frozen",
        _ => id ?? "status",
    };

    private static string Stat(ContentDb db, Effect e)
    {
        string who = e.Subject?.Scope switch
        {
            "self" => string.Empty,
            "adjacent" => $"{Depts(e.Subject?.Dept, e.Subject?.NotDept)}neighbours",
            "sameFloor" => $"{Depts(e.Subject?.Dept, e.Subject?.NotDept)}on its floor",
            "occupants" => $"{Depts(e.Subject?.Dept, e.Subject?.NotDept)}inside",
            "all" => "everyone",
            "firm" => "the firm",
            _ => string.Empty,
        };
        string mult = e.Permille.HasValue ? $"×{e.Permille / 1000}.{e.Permille % 1000 / 100}" : $"{(e.Amount >= 0 ? "+" : string.Empty)}{e.Amount}";
        string what = StatWord(e.Stat, e.Status, e.Floor, db);
        if (e.Stat == "burnoutMaxOverride") mult = $"is {e.Amount}";
        if (e.Stat == "income") return $"+¥{e.Amount} income per round";
        return who.Length == 0 ? $"{what} {mult}" : $"{what} {mult} for {who}";
    }

    /// <summary>A stat's plain name: "sales" → "Sales", "loyaltyCap" → "Loyalty cap". Cards use it at twelve columns.</summary>
    public static string StatWord(string? stat, string? status = null, string? floor = null, ContentDb? db = null) => stat switch
    {
        "sales" => "Sales",
        "poach" => "Poach",
        "curse" => "Curse",
        "pr" => "PR",
        "flatSales" => "Sales",
        "cooldown" => "cooldown time",
        "loyaltyCap" => "Loyalty cap",
        "loyaltyCapMult" => "Loyalty cap",
        "regenPerEvent" => "Loyalty regrowth",
        "passiveMult" => "cap and regen passives",
        "statusStacksBonus" => $"{StatusName(status)} applied",
        "burnoutMaxOverride" => "Burnout limit",
        "burnoutMaxDelta" => "Burnout limit",
        "curseSelfCost" => "Curse self-cost",
        "retriggerBonus" => "retrigger strength",
        "floorOutput" => $"output on {(floor == "*" ? "every floor" : db?.Floors.FirstOrDefault(f => f.Id == floor)?.Name ?? floor)}",
        "income" => "income per round",
        "upkeep" => "upkeep per round",
        "rerollCost" => "reroll cost",
        "severance" => "severance",
        "severanceMult" => "severance",
        _ => stat ?? string.Empty,
    };

    private static string Depts(string[]? dept, string[]? notDept)
    {
        if (dept != null) return string.Join("/", dept) + " ";
        if (notDept != null) return "non-" + string.Join("/", notDept) + " ";
        return string.Empty;
    }

    private static string Flag(string? flag) => flag switch
    {
        "untargetable" => "cannot be targeted by enemy abilities",
        "bureaucracyImmune" => "immune to Bureaucracy",
        "frozenImmune" => "immune to Frozen",
        "burnoutImmune" => "immune to Burnout",
        "overtimePermanent" => "always on Overtime, without the hangover",
        "cannotBeRetriggered" => "cannot be retriggered",
        "wholeFloorAdjacency" => "management retriggers reach the whole floor",
        "capProtected" => "its Loyalty cap contribution cannot be eroded",
        "regenNeverSuppressed" => "Loyalty regrows even after a Poach",
        "receptionDisabled" => "Reception grants no cap",
        "everyFloorMostPopulated" => "every floor counts as the busiest",
        "floorSelectorMirror" => "highest-floor abilities also hit the lowest floor",
        "cannotBeLaidOff" => "cannot be laid off",
        "landingOnly" => "must stand on the landing column",
        _ => flag ?? string.Empty,
    };

    private static string Economy(Effect e) => e.Do switch
    {
        "stat" when e.Stat == "income" => $"+¥{e.Amount} income per round",
        "stat" => Stat(new ContentDbShim(), e),
        "flag" => Capitalise(Flag(e.Flag)),
        _ => e.Do,
    };

    private static string Month(long m) => m switch { 0 => "the Quarter Open", 1 => "Month 2", 2 => "Crunch", _ => "the Bell" };

    private static string Roman(long t) => t switch { 1 => "I", 2 => "II", 3 => "III", _ => t.ToString() };

    private static string Ordinal(long n) => n switch { 2 => "2nd", 3 => "3rd", _ => $"{n}th" };

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static bool Matches(SubjectSpec? subject, EmployeeDef e)
    {
        if (subject?.Dept != null && Array.IndexOf(subject.Dept, e.Dept) < 0 && (e.CountsAsDept == null || Array.IndexOf(subject.Dept, e.CountsAsDept) < 0)) return false;
        if (subject?.NotDept != null && Array.IndexOf(subject.NotDept, e.Dept) >= 0) return false;
        return true;
    }

    /// <summary>Stat() needs a db only for floor names; economy stats never name a floor.</summary>
    private sealed class ContentDbShim
    {
        public static implicit operator ContentDb(ContentDbShim _) => null!;
    }
}
