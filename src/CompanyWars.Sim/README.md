# CompanyWars.Sim

The combat simulation of `docs/SIMULATION_SPEC.md`, as one pure function:

```csharp
MatchResult Simulator.Simulate(uint seed, TowerSnapshot a, TowerSnapshot b, RuleSet rules, ContentTable content)
```

It references nothing but the .NET base library. `BannedSymbols.txt` and the reflection and
source-scan tests in `tests/CompanyWars.Sim.Tests` enforce the determinism contract (§18):
no clock, no `System.Random`, no I/O, no floating-point type anywhere in the assembly.
`long` and permille only; the RNG and the hashes are `uint` in `unchecked` arithmetic.

## Where each section of the spec lives

| Spec | File |
| --- | --- |
| §2 numeric model | `Arith.cs` |
| §3–§4 constants, inputs | `RuleSet.cs`, `Snapshot.cs`, `Content.cs` |
| §5 setup, §6.4 stats and flags | `Match.Setup.cs` |
| §6 targeting | `Match.Targeting.cs` |
| §7–§15 tick loop, resolution, statuses, retriggers, banners, end | `Match.Run.cs` |
| §16 ledger, result, hashes | `Ledger.cs`, `Canon.cs`, `Fnv1a.cs` |
| §17 random numbers | `Mulberry32.cs` |
| §19 conformance | `fixtures/sim/*.json`, recorded by `CompanyWars.Tools fixtures record` |

## Readings the spec leaves open, as implemented

These are decisions the text does not settle. Each is deterministic and pinned by a fixture;
changing one is a rule change (§18.8) and regenerates the fixtures under the fixture guard.

1. **Phase order within a tick is A, B, D, C, E, F** (D-62; F does nothing since revision 2): readiness is judged on the
   progress accumulated through the previous tick, then cooldowns advance, so a fresh
   80-tick cooldown fires on tick 80. §7 once listed the advance first; the human ruled
   for §20's reading and §7 now says so.
2. **Status entries carry the status id in `tags`** (`status.burnout`, `status.overtime`, …)
   because §16.1 has no status field and two applications to one unit at one tick would
   otherwise be indistinguishable.
3. **`cleanse`** is in the content vocabulary but not in §9. It removes up to `stacks` stacks
   (Burnout: count; Overtime and Bureaucracy: the freshest expiries first; Frozen: clears)
   and emits a `status` entry per unit *changed*, with negative `stacks` and the tag
   `cleanse`, as §11.3's Water Cooler prescribes.
4. **The Tenure step applies to room-granted `sales`, `poach`, `curse` and `pr` permille**
   (§5.4 `roomAura`), not to `cooldown`. `CONTENT_SCHEMA.md` §3.5 and `GAME_DESIGN.md` §7.1
   say "every multiplier the room grants", which would make a Tier III Open Plan's cooldown
   ×0.9 into ×1.2. The spec wins for what the sim computes; the two documents need a line.
5. **A retrigger never targets its own caster.** `sameFloor` and `adjacent` own-targets
   include the caster for stats and cleanses, but a VP of Operations retriggering itself
   would only whiff on depth.
6. **`highest_base_value` counts a per-tag or percent-of-cap value as 0** (§6.2 says so);
   a retrigger or status ability is also 0.
7. **Only shop-bought extraplanar employees must carry a rider** (§5.1 says every
   extraplanar employee; `CONTENT_SCHEMA.md` §12 says shop-bought, and ritual results have
   no rider).
8. **Room banner effects apply per occupant** as §14's iteration order implies (each unit,
   then its enclosing room's banner effect on that unit), so a Server Room burns each
   occupant exactly once.
9. **Static permille grants chain in a fixed order** — rooms, furniture, employees, riders,
   founder, modifiers — flooring after each, so a modifier's `sales` ×1.1 lands on top of the
   room aura. Flags and sums are order-free; overrides (Old Money's Tenure tier) run first.
10. **`abilityId`** is `ability.` plus the ability's name in lowercase with non-alphanumerics
    collapsed to `_` (`Cease & Desist` → `ability.cease_desist`); furniture, rooms, riders
    and modifiers use their own ids; the regen, burnout and banner events use those words.
11. **`random_selector`** needs a definition with a random selector and no shipped employee
    has one (the Paralegal uses `most_populated_floor`); the fixture carries a fixture-local
    overlay definition, declared in its file.
