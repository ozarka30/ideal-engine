using System;
using System.Collections.Generic;

namespace CompanyWars.Sim;

internal sealed partial class Match
{
    // ---------------------------------------------------------------- §6.1 floor selectors

    private List<int> OccupiedFloors(Firm defender, bool includeB1)
    {
        var list = new List<int>();
        foreach (FloorState f in defender.Floors.Values)
        {
            if (f.Units.Count == 0) continue;
            if (f.Index < 0 && !includeB1) continue;
            list.Add(f.Index);
        }
        return list;
    }

    private List<int> SelectFloors(string selector, Firm attacker, Firm defender, Unit? caster)
    {
        var result = new List<int>();
        List<int> occupied = OccupiedFloors(defender, includeB1: false);
        switch (selector)
        {
            case "highest_occupied_floor":
                if (occupied.Count > 0) result.Add(occupied[occupied.Count - 1]);
                if (attacker.FloorSelectorMirror && occupied.Count > 0 && !result.Contains(occupied[0])) result.Insert(0, occupied[0]);
                break;
            case "lowest_occupied_floor":
                if (occupied.Count > 0) result.Add(occupied[0]);
                break;
            case "most_populated_floor":
                if (defender.EveryFloorMostPopulated)
                {
                    result.AddRange(occupied);
                }
                else
                {
                    int best = int.MinValue;
                    int bestCount = 0;
                    foreach (int fl in occupied)
                    {
                        int c = defender.UnitsOnFloor(fl).Count;
                        if (c > bestCount) { bestCount = c; best = fl; }
                    }
                    if (best != int.MinValue) result.Add(best);
                }
                break;
            case "least_populated_floor":
                {
                    int best = int.MinValue;
                    int bestCount = int.MaxValue;
                    foreach (int fl in occupied)
                    {
                        int c = defender.UnitsOnFloor(fl).Count;
                        if (c < bestCount) { bestCount = c; best = fl; }
                    }
                    if (best != int.MinValue) result.Add(best);
                    break;
                }
            case "same_floor_index":
                if (caster != null && occupied.Contains(caster.FloorIndex)) result.Add(caster.FloorIndex);
                break;
            case "random_floor":
                if (occupied.Count > 0) result.Add(occupied[(int)_rng.Draw((uint)occupied.Count)]);
                break;
            case "all_floors":
                result.AddRange(OccupiedFloors(defender, includeB1: true));
                break;
            default:
                throw new SimulationInputException($"unknown floor selector {selector}");
        }
        return result;
    }

    // ---------------------------------------------------------------- §6.2 employee selectors and §6.3 picks

    private List<Unit> SelectUnits(string selector, List<Unit> candidates, int month)
    {
        var result = new List<Unit>();
        if (candidates.Count == 0) return result;
        switch (selector)
        {
            case "lowest_cooldown_remaining":
                {
                    Unit? best = null;
                    long bestRem = long.MaxValue;
                    foreach (Unit u in candidates)
                    {
                        long rem = u.CdTotal(month) - u.CdProgress;
                        if (rem < bestRem) { bestRem = rem; best = u; }
                    }
                    result.Add(best!);
                    break;
                }
            case "highest_base_value":
                {
                    Unit? best = null;
                    long bestVal = long.MinValue;
                    foreach (Unit u in candidates)
                    {
                        long v = u.AbilityBaseConstant;
                        if (v > bestVal) { bestVal = v; best = u; }
                    }
                    result.Add(best!);
                    break;
                }
            case "random":
                result.Add(candidates[(int)_rng.Draw((uint)candidates.Count)]);
                break;
            case "all":
                result.AddRange(candidates);
                break;
            default:
                throw new SimulationInputException($"unknown unit selector {selector}");
        }
        return result;
    }

    /// <summary>Enemy targets (§6.1–§6.2). Empty means the effect whiffs.</summary>
    private List<Unit> SelectEnemyTargets(Firm attacker, Unit? caster, TargetSpec target, long tick)
    {
        Firm defender = Opponent(attacker);
        string floorSel = caster?.FloorSelectorOverride ?? target.Floor ?? "highest_occupied_floor";
        string unitSel = target.Unit ?? "all";
        int month = _rules.Month(tick);
        var result = new List<Unit>();
        foreach (int fl in SelectFloors(floorSel, attacker, defender, caster))
        {
            var cands = new List<Unit>();
            foreach (Unit u in defender.UnitsOnFloor(fl))
            {
                if (!u.Untargetable) cands.Add(u);
            }
            result.AddRange(SelectUnits(unitSel, cands, month));
        }
        return result;
    }

    /// <summary>Own-side targets (§6.3). <paramref name="caster"/> is the unit whose fire this is, if any.</summary>
    private List<Unit> SelectOwnTargets(Firm firm, Unit? caster, RoomState? room, FurnitureState? furniture, TargetSpec target, long tick, bool excludeCaster)
    {
        IEnumerable<Unit> pool;
        switch (target.Scope)
        {
            case "self":
                pool = caster != null ? new[] { caster } : Array.Empty<Unit>();
                break;
            case "adjacent":
                if (caster != null) pool = caster.WholeFloorAdjacency ? firm.UnitsOnFloor(caster.FloorIndex) : caster.Adjacent;
                else if (furniture != null) pool = furniture.Adjacent;
                else if (room != null) pool = AdjacentToAny(firm, room.Occupants);
                else pool = Array.Empty<Unit>();
                break;
            case "sameFloor":
                {
                    int? fl = caster?.FloorIndex ?? furniture?.FloorIndex ?? room?.FloorIndex;
                    pool = fl.HasValue ? firm.UnitsOnFloor(fl.Value) : Array.Empty<Unit>();
                    break;
                }
            case "occupants":
                if (room != null) pool = room.Occupants;
                else if (caster?.Room != null) pool = caster.Room.Occupants;
                else if (furniture?.Room != null) pool = furniture.Room.Occupants;
                else pool = Array.Empty<Unit>();
                break;
            case "all":
                pool = firm.Units;
                break;
            default:
                throw new SimulationInputException($"unknown own scope {target.Scope}");
        }
        var cands = new List<Unit>();
        foreach (Unit u in pool)
        {
            if (excludeCaster && u == caster) continue;
            if (u.MatchesFilter(target.Dept, null, target.Tag)) cands.Add(u);
        }
        cands.Sort((x, y) => x.UnitIndex.CompareTo(y.UnitIndex));
        if (target.Pick != null) return SelectUnits(target.Pick, cands, _rules.Month(tick));
        return cands;
    }
}
