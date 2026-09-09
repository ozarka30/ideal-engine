using System;
using System.Collections.Generic;

namespace CompanyWars.Sim;

internal sealed partial class Match
{
    private static readonly string[] NoTags = Array.Empty<string>();
    private static readonly long[] NoUnits = Array.Empty<long>();

    // ---------------------------------------------------------------- §16 emission

    private void Emit(long tick, string kind, Source src, string targetSide, long[] targetUnits, long raw, long goodwillDelta, long capDelta, long shareDelta, long overflow, long stacks, int depth, string[] tags)
    {
        string[] sorted = tags;
        if (tags.Length > 1)
        {
            sorted = (string[])tags.Clone();
            Array.Sort(sorted, string.CompareOrdinal);
        }
        _entries.Add(new LedgerEntry(
            Seq: _entries.Count,
            Tick: tick,
            Kind: kind,
            SourceSide: src.Floor == -2 && src.Unit == null && src.AbilityId == "banner" ? "*" : src.Side.Name(),
            SourceUnit: src.Unit?.UnitIndex ?? -1,
            SourceInstanceId: src.Unit?.InstanceId ?? string.Empty,
            SourceFloor: src.Floor,
            AbilityId: src.AbilityId,
            TargetSide: targetSide,
            TargetUnits: targetUnits,
            Raw: raw,
            GoodwillDelta: goodwillDelta,
            CapDelta: capDelta,
            ShareDelta: shareDelta,
            Overflow: overflow,
            Stacks: stacks,
            Depth: depth,
            Month: _rules.Month(tick),
            Tags: sorted));
    }

    private void EmitWhiff(long tick, Source src, int depth, string[] tags)
    {
        Emit(tick, "whiff", src, "*", NoUnits, 0, 0, 0, 0, 0, 0, depth, tags);
    }

    // ---------------------------------------------------------------- §7 the tick loop

    public MatchResult Run(uint seed, TowerSnapshot a, TowerSnapshot b)
    {
        for (long tick = 0; tick < _rules.QuarterTicks; tick++)
        {
            Banners(tick);          // A
            Expiry(tick);           // B
            // Readiness is judged on the progress accumulated through the previous tick, then the
            // cooldowns advance: this is the order the worked trace in SIMULATION_SPEC.md §20 encodes
            // (first fires on tick 80, totalPush 1131). See the errata note in src/CompanyWars.Sim/README.md.
            ReadyAndResolve(tick);  // D
            Advance(tick);          // C
            Periodic(tick);         // E
            if (EndCheck(tick)) break; // F
        }
        if (!_ended) Bell();

        string stateString = string.Join("|", new[]
        {
            _winner, _endTick.ToString(), _share.ToString(),
            A.Goodwill.ToString(), B.Goodwill.ToString(),
            A.Cap.ToString(), B.Cap.ToString(),
            A.TotalPush.ToString(), B.TotalPush.ToString(),
            _entries.Count.ToString(), _rng.State.ToString(),
        });

        return new MatchResult(
            SchemaVersion: _rules.SchemaVersion,
            Seed: seed,
            Round: _rules.Round,
            RulesHash: Fnv1a.Format(Fnv1a.Hash(_rules.Canonical())),
            SnapshotHashA: Fnv1a.Format(Fnv1a.Hash(a.Canonical())),
            SnapshotHashB: Fnv1a.Format(Fnv1a.Hash(b.Canonical())),
            Winner: _winner,
            EndTick: _endTick,
            FinalShare: _share,
            FinalGoodwill: new SideValues(A.Goodwill, B.Goodwill),
            FinalCap: new SideValues(A.Cap, B.Cap),
            TotalPush: new SideValues(A.TotalPush, B.TotalPush),
            Entries: _entries.ToArray(),
            StateHash: Fnv1a.Format(Fnv1a.Hash(stateString)));
    }

    // ---------------------------------------------------------------- §14 banners

    private void Banners(long tick)
    {
        int month = -1;
        for (int i = 0; i < _rules.MonthStart.Length; i++)
        {
            if (_rules.MonthStart[i] == tick) month = i;
        }
        if (month < 0) return;
        Emit(tick, "banner", Source.OfFirm(Side.A, "banner"), "*", NoUnits, month, 0, 0, 0, 0, 0, 0, NoTags);

        foreach (Firm firm in _firms)
        {
            // 1. each unit's enclosing room, applied to that unit
            foreach (Unit u in firm.Units)
            {
                RoomState? room = u.Room;
                if (room == null) continue;
                foreach (Effect e in room.Def.Effects)
                {
                    if (e.On != "banner" || e.Month != month || !room.EffectActive(e)) continue;
                    ApplyEffect(firm, Source.OfRoom(room), null, room, null, e, tick, 0, false, 1000, new List<Unit> { u });
                }
            }
            // 2. furniture
            foreach (FurnitureState f in firm.Furniture)
            {
                foreach (Effect e in f.Def.Effects)
                {
                    if (e.On != "banner" || e.Month != month) continue;
                    ApplyEffect(firm, Source.OfFurniture(f), null, null, f, e, tick, 0, false, 1000, null);
                }
            }
            // 3. riders, in globals.riders order
            foreach ((Unit ru, RiderDef rd) in firm.Riders)
            {
                foreach (Effect e in rd.Effects)
                {
                    if (e.On != "banner" || e.Month != month) continue;
                    ApplyEffect(firm, Source.OfUnit(ru, rd.Id), ru, null, null, e, tick, 0, false, 1000, null, firmLevelValue: true);
                }
            }
            // 4. founder, then modifiers
            if (firm.Founder != null)
            {
                foreach (Effect e in firm.Founder.Effects)
                {
                    if (e.On != "banner" || e.Month != month) continue;
                    ApplyEffect(firm, Source.OfFirm(firm.Side, firm.Founder.Id), null, null, null, e, tick, 0, false, 1000, null, firmLevelValue: true);
                }
            }
            foreach (ModifierDef m in firm.Modifiers)
            {
                foreach (Effect e in m.Effects)
                {
                    if (e.On != "banner" || e.Month != month) continue;
                    ApplyEffect(firm, Source.OfFirm(firm.Side, m.Id), null, null, null, e, tick, 0, false, 1000, null, firmLevelValue: true);
                }
            }
        }
    }

    // ---------------------------------------------------------------- §12.4 expiry

    private void Expiry(long tick)
    {
        foreach (Unit u in _units)
        {
            while (u.Overtime.Count > 0 && u.Overtime[0] <= tick)
            {
                u.Overtime.RemoveAt(0);
                if (!u.OvertimePermanent && u.BurnoutMax != 0)
                {
                    ApplyStatus(null, u, Vocabulary.StatusBurnout, 1, null, tick, Source.OfUnit(u, Vocabulary.StatusOvertime), 0, new[] { "overtime_expired" });
                }
            }
            u.Bureaucracy.RemoveAll(x => x <= tick);
        }
    }

    // ---------------------------------------------------------------- §8.1 advance

    private long OvertimeStacks(Unit u) => u.OvertimePermanent ? 2 : u.Overtime.Count;

    private void Advance(long tick)
    {
        foreach (Unit u in _units)
        {
            long rate;
            if (u.FrozenUntil > tick)
            {
                rate = 0;
            }
            else
            {
                rate = 1000 + _rules.OvertimeRatePermille * OvertimeStacks(u) - _rules.BureaucracyRatePermille * u.Bureaucracy.Count;
                rate = Arith.Max(0, rate);
            }
            u.CdProgress += rate;
        }
    }

    // ---------------------------------------------------------------- §8.2 ready list and initiative

    private void ReadyAndResolve(long tick)
    {
        int month = _rules.Month(tick);
        var ready = new List<(long CdTotal, long SidePriority, long IndexWithinSide, Unit Unit)>();
        foreach (Unit u in _units)
        {
            long total = u.CdTotal(month);
            if (u.CdProgress >= total)
            {
                long pri = u.Side == Side.A ? tick % 2 : 1 - (tick % 2);
                ready.Add((total, pri, u.IndexWithinSide, u));
            }
        }
        ready.Sort((x, y) =>
        {
            int c = x.CdTotal.CompareTo(y.CdTotal);
            if (c != 0) return c;
            c = x.SidePriority.CompareTo(y.SidePriority);
            if (c != 0) return c;
            return x.IndexWithinSide.CompareTo(y.IndexWithinSide);
        });
        foreach ((_, _, _, Unit u) in ready)
        {
            Resolve(u, tick, 0, false, 1000);
            u.CdProgress = 0;
        }
    }

    // ---------------------------------------------------------------- §9 resolution

    private void Resolve(Unit u, long tick, int depth, bool viaRetrigger, long retriggerBonus)
    {
        Firm firm = _firms[(int)u.Side];
        u.FireCount++;
        ApplyEffect(firm, Source.OfUnit(u, u.AbilityId), u, null, null, u.Ability, tick, depth, viaRetrigger, retriggerBonus, null);
        foreach (ExtraEffect x in u.Extras)
        {
            if (x.EveryN > 0 && u.FireCount % x.EveryN == 0)
            {
                ApplyEffect(firm, Source.OfUnit(u, x.SourceId), u, null, null, x.Effect, tick, depth, viaRetrigger, retriggerBonus, null);
            }
        }
    }

    private long BaseValue(Firm firm, Unit? caster, ValueSpec? v)
    {
        if (v == null) return 0;
        if (v.Constant.HasValue) return v.Constant.Value;
        if (v.PermilleOfTargetCap.HasValue) return Arith.Permille(Opponent(firm).Cap, v.PermilleOfTargetCap.Value);
        if (v.Base.HasValue)
        {
            long count = 0;
            foreach (Unit o in firm.Units)
            {
                if (v.PerTag != null && (Array.IndexOf(o.Def.Tags, v.PerTag) >= 0 || o.HasDept(v.PerTag))) count++;
            }
            return v.Base.Value + (v.Each ?? 0) * count;
        }
        return 0;
    }

    /// <summary>§9.2: seven steps, flooring after each.</summary>
    private long Pipeline(Firm firm, Unit u, int kind, ValueSpec? value, long tick, long retriggerBonus, bool viaRetrigger)
    {
        long v = BaseValue(firm, u, value);
        if (kind == Kind.Push) v += u.FlatPush;
        v = Arith.Permille(v, kind >= 0 && kind < 3 ? u.Aura[kind] : 1000);
        v = Arith.Permille(v, u.FloorMult);
        v = Arith.Permille(v, 1000 - _rules.BurnoutPushPenaltyPermille * u.Burnout);
        v = Arith.Permille(v, viaRetrigger ? retriggerBonus : 1000);
        v = Arith.Permille(v, kind == Kind.Restore ? 1000 : _rules.PushMult[_rules.Month(tick)]);
        return v;
    }

    private void ApplyEffect(Firm firm, Source src, Unit? caster, RoomState? room, FurnitureState? furniture, Effect e, long tick, int depth, bool viaRetrigger, long retriggerBonus, List<Unit>? forcedTargets, bool firmLevelValue = false)
    {
        Firm enemy = Opponent(firm);
        int month = _rules.Month(tick);
        switch (e.Do)
        {
            case "push":
                {
                    long v = caster != null && !firmLevelValue
                        ? Pipeline(firm, caster, Kind.Push, e.Value, tick, retriggerBonus, viaRetrigger)
                        : Arith.Permille(BaseValue(firm, caster, e.Value), _rules.PushMult[month]);
                    (long absorbed, long overflow, long sp) = ApplyPush(firm, enemy, v, tick);
                    firm.TotalPush += v;
                    Emit(tick, "push", src, enemy.Side.Name(), NoUnits, v, -absorbed, 0, sp, overflow, 0, depth, NoTags);
                    break;
                }
            case "anomaly":
                {
                    long v = caster != null && !firmLevelValue
                        ? Pipeline(firm, caster, Kind.Anomaly, e.Value, tick, retriggerBonus, viaRetrigger)
                        : Arith.Permille(BaseValue(firm, caster, e.Value), _rules.PushMult[month]);
                    long sp = ConvertToShare(firm, v);
                    Emit(tick, "anomaly", src, enemy.Side.Name(), NoUnits, v, 0, 0, sp, 0, 0, depth, NoTags);
                    long selfCostPermille = caster?.SelfCostPermille ?? _rules.AnomalySelfCostPermille;
                    long self = Arith.Permille(v, selfCostPermille);
                    if (self > 0)
                    {
                        (long absorbed, long overflow, long sp2) = ApplyPush(firm, firm, self, tick);
                        Emit(tick, "anomaly", src, firm.Side.Name(), NoUnits, self, -absorbed, 0, sp2, overflow, 0, depth, new[] { "self_cost" });
                    }
                    firm.TotalPush += v;
                    break;
                }
            case "morale":
                {
                    long v = caster != null && !firmLevelValue
                        ? Pipeline(firm, caster, -1, e.Value, tick, retriggerBonus, viaRetrigger)
                        : Arith.Permille(BaseValue(firm, caster, e.Value), _rules.PushMult[month]);
                    Firm defender = e.Target?.Side == "own" ? firm : enemy;
                    (long sp, long capDelta, long clamped) = ApplyMorale(defender, v, Opponent(defender));
                    Emit(tick, "morale", src, defender.Side.Name(), NoUnits, v, -clamped, capDelta, sp, 0, 0, depth, NoTags);
                    break;
                }
            case "restore":
                {
                    long v = caster != null && !firmLevelValue
                        ? Pipeline(firm, caster, Kind.Restore, e.Value, tick, retriggerBonus, viaRetrigger)
                        : BaseValue(firm, caster, e.Value);
                    long applied = Arith.Min(v, firm.Cap - firm.Goodwill);
                    firm.Goodwill += applied;
                    Emit(tick, "restore", src, firm.Side.Name(), NoUnits, v, applied, 0, 0, 0, 0, depth, NoTags);
                    break;
                }
            case "status":
                {
                    List<Unit> targets = forcedTargets ?? Targets(firm, caster, room, furniture, e, tick, false);
                    if (targets.Count == 0) { EmitWhiff(tick, src, depth, NoTags); break; }
                    foreach (Unit t in targets)
                    {
                        ApplyStatus(caster, t, e.Status ?? string.Empty, e.Stacks ?? 1, e.DurationTicks, tick, src, depth, NoTags);
                    }
                    break;
                }
            case "cleanse":
                {
                    List<Unit> targets = forcedTargets ?? Targets(firm, caster, room, furniture, e, tick, false);
                    if (targets.Count == 0) { EmitWhiff(tick, src, depth, NoTags); break; }
                    foreach (Unit t in targets)
                    {
                        long removed = Cleanse(t, e.Status ?? string.Empty, e.Stacks ?? 1, tick);
                        if (removed > 0)
                        {
                            Emit(tick, "status", src, t.Side.Name(), new[] { (long)t.UnitIndex }, 0, 0, 0, 0, 0, -removed, depth, new[] { "cleanse", e.Status ?? string.Empty });
                        }
                    }
                    break;
                }
            case "retrigger":
                {
                    List<Unit> targets = forcedTargets ?? Targets(firm, caster, room, furniture, e, tick, true);
                    if (targets.Count == 0) { EmitWhiff(tick, src, depth, NoTags); break; }
                    long bonus = caster?.RetriggerBonus ?? 1000;
                    foreach (Unit t in targets)
                    {
                        Emit(tick, "retrigger", src, t.Side.Name(), new[] { (long)t.UnitIndex }, 0, 0, 0, 0, 0, 0, depth, NoTags);
                        RetriggerUnit(t, tick, depth, bonus, src);
                        if (e.Then != null)
                        {
                            ApplyStatus(caster, t, e.Then.Status, e.Then.Stacks, null, tick, src, depth, NoTags);
                        }
                    }
                    break;
                }
            default:
                break; // static, stat, flag, override, build-phase actions: not resolved here
        }
    }

    private List<Unit> Targets(Firm firm, Unit? caster, RoomState? room, FurnitureState? furniture, Effect e, long tick, bool excludeCaster)
    {
        TargetSpec? t = e.Target;
        if (t == null) return new List<Unit>();
        if (t.Side == "enemy") return SelectEnemyTargets(firm, caster, t, tick);
        return SelectOwnTargets(firm, caster, room, furniture, t, tick, excludeCaster);
    }

    // ---------------------------------------------------------------- §10 Goodwill, overflow and Market Share

    private (long Absorbed, long Overflow, long Sp) ApplyPush(Firm attacker, Firm defender, long v, long tick)
    {
        if (v >= defender.SuppressThreshold) defender.LastSuppressTick = tick;
        long absorbed = Arith.Min(v, defender.Goodwill);
        defender.Goodwill -= absorbed;
        long overflow = v - absorbed;
        long sp = 0;
        if (overflow > 0) sp = ConvertToShare(Opponent(defender), overflow);
        return (absorbed, overflow, sp);
    }

    private (long Sp, long CapDelta, long Clamped) ApplyMorale(Firm defender, long raw, Firm creditTo)
    {
        long before = defender.Cap;
        long floorCap = Arith.Max(1, defender.ProtectedCap);
        defender.Cap = Arith.Max(floorCap, defender.Cap - raw);
        long clamped = 0;
        if (defender.Goodwill > defender.Cap)
        {
            clamped = defender.Goodwill - defender.Cap;
            defender.Goodwill = defender.Cap;
        }
        long sp = ConvertToShare(creditTo, Arith.Permille(raw, _rules.MoraleRatePermille));
        return (sp, defender.Cap - before, clamped);
    }

    private long ConvertToShare(Firm creditTo, long push)
    {
        creditTo.SpCarry += push * _rules.SpPerPushPermille[_rules.Round - 1];
        long sp = Arith.FloorDiv(creditTo.SpCarry, 1000);
        creditTo.SpCarry -= sp * 1000;
        long before = _share;
        if (creditTo.Side == Side.A) _share = Arith.Min(_rules.ShareTotal, _share + sp);
        else _share = Arith.Max(0, _share - sp);
        return _share - before;
    }

    // ---------------------------------------------------------------- §11 periodic events

    private void Periodic(long tick)
    {
        if (tick <= 0) return;
        int month = _rules.Month(tick);
        if (tick % _rules.RegenInterval == 0)
        {
            foreach (Firm firm in _firms)
            {
                bool suppressed = !firm.RegenNeverSuppressed && (tick - firm.LastSuppressTick) <= _rules.RegenSuppressWindow;
                long amount = suppressed ? 0 : Arith.Permille(firm.RegenPerEvent, _rules.RegenMult[month]);
                long applied = Arith.Min(amount, firm.Cap - firm.Goodwill);
                firm.Goodwill += applied;
                Emit(tick, "regen", Source.OfFirm(firm.Side, "regen"), firm.Side.Name(), NoUnits, amount, applied, 0, 0, 0, 0, 0, suppressed ? new[] { "suppressed" } : NoTags);
            }
        }
        if (tick % _rules.MoraleInterval == 0)
        {
            foreach (Firm firm in _firms)
            {
                long stacks = 0;
                foreach (Unit u in firm.Units) stacks += u.Burnout;
                if (stacks == 0) continue;
                long raw = Arith.FloorDiv(_rules.MoralePerStack * stacks * _rules.PushMult[month], 1000);
                (long sp, long capDelta, long clamped) = ApplyMorale(firm, raw, Opponent(firm));
                Emit(tick, "morale", Source.OfFirm(firm.Side, "burnout"), firm.Side.Name(), NoUnits, raw, -clamped, capDelta, sp, 0, 0, 0, new[] { "burnout" });
            }
        }
        foreach (FurnitureState f in _furniture)
        {
            foreach (Effect e in f.Def.Effects)
            {
                if (e.On != "periodic" || e.Every == null || e.Every <= 0 || tick % e.Every.Value != 0) continue;
                ApplyEffect(_firms[(int)f.Side], Source.OfFurniture(f), null, null, f, e, tick, 0, false, 1000, null);
            }
        }
        foreach (Firm firm in _firms)
        {
            foreach (RoomState r in firm.Rooms)
            {
                foreach (Effect e in r.Def.Effects)
                {
                    if (e.On != "periodic" || e.Every == null || e.Every <= 0 || tick % e.Every.Value != 0 || !r.EffectActive(e)) continue;
                    ApplyEffect(firm, Source.OfRoom(r), null, r, null, e, tick, 0, false, 1000, null);
                }
            }
        }
    }

    // ---------------------------------------------------------------- §12.3 status application

    private void ApplyStatus(Unit? applier, Unit target, string status, long n, long? durationTicks, long tick, Source src, int depth, string[] tags)
    {
        long bonus = 0;
        if (applier != null) applier.StacksBonus.TryGetValue(status, out bonus);
        long requested = n + bonus;
        long applied = 0;
        string[] outTags = tags;
        switch (status)
        {
            case "status.burnout":
                if (target.BurnoutMax == 0)
                {
                    outTags = With(tags, "immune");
                }
                else
                {
                    long before = target.Burnout;
                    target.Burnout = Arith.Min(target.BurnoutMax, target.Burnout + requested);
                    applied = target.Burnout - before;
                }
                break;
            case "status.overtime":
                if (target.OvertimePermanent)
                {
                    outTags = With(tags, "permanent");
                }
                else
                {
                    applied = AddTimed(target.Overtime, requested, _rules.OvertimeMax, tick + _rules.OvertimeDuration);
                }
                break;
            case "status.bureaucracy":
                if (target.BureaucracyImmune)
                {
                    outTags = With(tags, "immune");
                }
                else
                {
                    applied = AddTimed(target.Bureaucracy, requested, _rules.BureaucracyMax, tick + _rules.BureaucracyDuration);
                }
                break;
            case "status.frozen":
                if (target.FrozenImmune)
                {
                    outTags = With(tags, "immune");
                }
                else
                {
                    target.FrozenUntil = Arith.Max(target.FrozenUntil, tick + (durationTicks ?? 0));
                    applied = requested;
                }
                break;
            default:
                throw new SimulationInputException($"unknown status {status}");
        }
        // The entry format (§16.1) has no status field; the status id rides in tags so the autopsy can tell
        // a Burnout application from an Overtime one on the same unit at the same tick.
        Emit(tick, "status", src, target.Side.Name(), new[] { (long)target.UnitIndex }, 0, 0, 0, 0, 0, applied, depth, With(outTags, status));
    }

    private static long AddTimed(List<long> stacks, long n, long max, long expiry)
    {
        long applied = 0;
        for (long i = 0; i < n; i++)
        {
            if (stacks.Count < max)
            {
                stacks.Add(expiry);
            }
            else if (stacks.Count > 0)
            {
                stacks[0] = expiry; // the list is kept ascending, so [0] is the smallest expiry
            }
            else
            {
                break;
            }
            stacks.Sort();
            applied++;
        }
        return applied;
    }

    private static string[] With(string[] tags, string tag)
    {
        var list = new List<string>(tags) { tag };
        return list.ToArray();
    }

    private long Cleanse(Unit target, string status, long n, long tick)
    {
        switch (status)
        {
            case "status.burnout":
                {
                    long removed = Arith.Min(n, target.Burnout);
                    target.Burnout -= removed;
                    return removed;
                }
            case "status.overtime":
                if (target.OvertimePermanent) return 0;
                return RemoveTimed(target.Overtime, n);
            case "status.bureaucracy":
                return RemoveTimed(target.Bureaucracy, n);
            case "status.frozen":
                if (target.FrozenUntil > tick) { target.FrozenUntil = -1; return 1; }
                return 0;
            default:
                throw new SimulationInputException($"unknown status {status}");
        }
    }

    /// <summary>Removes up to <paramref name="n"/> stacks, freshest (largest expiry) first.</summary>
    private static long RemoveTimed(List<long> stacks, long n)
    {
        long removed = 0;
        while (removed < n && stacks.Count > 0)
        {
            stacks.RemoveAt(stacks.Count - 1);
            removed++;
        }
        return removed;
    }

    // ---------------------------------------------------------------- §13 retriggers

    private void RetriggerUnit(Unit target, long tick, int depth, long retriggerBonus, Source retriggerer)
    {
        if (depth >= _rules.RetriggerDepthMax)
        {
            EmitWhiff(tick, retriggerer, depth, new[] { "retrigger_depth" });
            return;
        }
        if (target.CannotBeRetriggered)
        {
            EmitWhiff(tick, retriggerer, depth, new[] { "retrigger_refused" });
            return;
        }
        Resolve(target, tick, depth + 1, true, retriggerBonus);
    }

    // ---------------------------------------------------------------- §15 end of match

    private bool EndCheck(long tick)
    {
        if (_share >= _rules.ShareTotal) { _winner = "A"; _ended = true; }
        else if (_share <= 0) { _winner = "B"; _ended = true; }
        if (_ended) _endTick = tick;
        return _ended;
    }

    private void Bell()
    {
        if (_share > _rules.ShareStart) _winner = "A";
        else if (_share < _rules.ShareStart) _winner = "B";
        else if (A.Goodwill != B.Goodwill) _winner = A.Goodwill > B.Goodwill ? "A" : "B";
        else if (A.TotalPush != B.TotalPush) _winner = A.TotalPush > B.TotalPush ? "A" : "B";
        else _winner = "draw";
        _endTick = _rules.QuarterTicks - 1;
    }
}
