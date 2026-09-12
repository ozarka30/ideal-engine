# Company Wars — Simulation Specification

Status: **Phase 2 draft.** This is the combat simulation as an implementable
specification. It is written so that two independent implementations, given the same
seed and the same two tower snapshots, produce byte-identical ledgers and the same
state hash. Where `GAME_DESIGN.md` and this document disagree, this document wins for
anything the sim computes.

Every number in §3 is an initial value owned by `BALANCE_PLAN.md`. Every *rule* is
owned here. Changing a number does not change this document's version; changing a
rule does.

**Revision 2 — the revenue race (D-85).** The fight is won by the firm with more Revenue
at the Bell. Market Share, Share Points and the early finish are gone; Goodwill is
renamed Client Loyalty; the effect kinds are `sales`, `poach`, `scandal`, `curse` and
`pr`. The `MatchResult` `schemaVersion` is 2, and fixtures recorded under revision 1 are
invalid until re-recorded (§18.8). The `TowerSnapshot` format is unchanged.

**Revision 3 — Sales scale with Loyalty (D-87).** A `sales` effect earns
`floor(v × loyalty / capAtStart)` (§9.3): wavering clients buy less. The entry's `raw` is
`v` and its `revenueDelta` what was earned (§16.1). Fixtures recorded under revision 2 are
invalid until re-recorded; the §20 trace is unchanged, because with no Poach Loyalty
stays at its cap.

---

## Contents

1. [Scope and guarantees](#1-scope-and-guarantees)
2. [Numeric model](#2-numeric-model)
3. [Constants](#3-constants)
4. [Inputs](#4-inputs)
5. [Match setup](#5-match-setup)
6. [Targeting](#6-targeting)
7. [The tick loop](#7-the-tick-loop)
8. [Cooldowns and initiative](#8-cooldowns-and-initiative)
9. [Resolution](#9-resolution)
10. [Loyalty, overflow and Revenue](#10-loyalty-overflow-and-revenue)
11. [Periodic events](#11-periodic-events)
12. [Status effects](#12-status-effects)
13. [Retriggers](#13-retriggers)
14. [Banners and month transitions](#14-banners-and-month-transitions)
15. [End of match](#15-end-of-match)
16. [Ledger entries and the replay](#16-ledger-entries-and-the-replay)
17. [Random numbers](#17-random-numbers)
18. [Determinism contract](#18-determinism-contract)
19. [Conformance](#19-conformance)
20. [Worked trace](#20-worked-trace)

---

## 1. Scope and guarantees

The simulation is one pure function:

```
simulate(seed: u32, a: TowerSnapshot, b: TowerSnapshot, rules: RuleSet): MatchResult
```

It has no clock, no I/O, no rendering dependency, and no source of randomness other
than the seeded generator in §17. It reads its two snapshots and its rules, and it
returns a result containing the complete ordered list of everything that happened. The
renderer, the live ledger, the autopsy, the replay viewer and the balance harness are
all consumers of that list; none of them computes anything the sim did not.

Guarantees:

- **Determinism.** Same inputs, same output, on any platform, in any implementation
  conforming to this document.
- **Completeness.** Every change to Loyalty, Loyalty cap, Revenue, or an employee's
  status is represented by exactly one ledger entry, in the order it happened. A
  transfer of Revenue between firms is one entry.
- **Integrality.** No value in the sim is ever a non-integer.

Not in scope: the build phase, crafting, the economy, snapshot legality validation.
Those are deterministic too, but they are not this function.

---

## 2. Numeric model

All quantities are integers. The reference implementation is C# (D-60): values are
`long`, the RNG and the hash are `uint` in `unchecked` arithmetic, and no
floating-point type appears anywhere in the sim assembly. Any other implementation
must keep every intermediate within the exact-integer range of its numeric type; the
multiplier chain in §9.2 is ordered so that no intermediate exceeds ~10^12, which fits
a 64-bit integer and the 2^53 exact range of a double alike.

**Permille.** Every multiplier is an integer in thousandths. A multiplier of 1.2 is
written `1200`. Applying a multiplier `m` to a value `v`:

```
v = floor(v * m / 1000)
```

where `floor` is the mathematical floor. Since every `v` and `m` in this document is
non-negative, `floor` equals truncation. Implementations must not use floating-point
division followed by rounding; they must compute the exact integer quotient.

**Order.** Multipliers are applied one at a time, in the order the relevant section
lists them, flooring after each. Never multiply the multipliers together first.

**Percent-of.** "X permille of Y" is `floor(Y * X / 1000)`.

**Clamping.** `clamp(v, lo, hi) = max(lo, min(hi, v))`.

---

## 3. Constants

All constants live in the `RuleSet` (§4.3). The values here are the initial ones.

### 3.1 Time

| Name | Value | Meaning |
| --- | --- | --- |
| `TICKS_PER_SECOND` | 20 | |
| `QUARTER_TICKS` | 1200 | Ticks 0..1199 inclusive |
| `MONTH_START` | `[0, 400, 800, 1160]` | First tick of Month 1, Month 2, Crunch, Bell |
| `RUSH_MULT` | `[1000, 1400, 2000, 3000]` | Per month, applied to Sales, Poach, Scandal and Curse — the quarter-end rush |
| `REGEN_MULT` | `[1000, 600, 200, 0]` | Per month, applied to regen |

`month(tick)` is the largest index `i` such that `MONTH_START[i] <= tick`.

### 3.2 Client Loyalty and Revenue

| Name | Value | Meaning |
| --- | --- | --- |
| `LOYALTY_BASE(round)` | `600 + 100 * round` | Base Loyalty cap for both firms |
| `REGEN_BASE_PERMILLE` | 30 | Clients drifting back per regen event, as permille of cap |
| `REGEN_INTERVAL` | 40 | Ticks between regen events |
| `REGEN_SUPPRESS_WINDOW` | 20 | A suppressing Poach within this many ticks blocks regen |
| `SUPPRESS_THRESHOLD_PERMILLE` | 40 | A Poach must be at least this permille of the target's cap to suppress |
| `SCANDAL_INTERVAL` | 20 | Ticks between Burnout Scandal events |
| `SCANDAL_PER_STACK` | 8 | Raw Scandal per Burnout stack per event |
| `SCANDAL_TRANSFER_PERMILLE` | 250 | A Scandal moves this permille of its raw amount from the target's Revenue |
| `CURSE_SELF_COST_PERMILLE` | 250 | The curser's own Loyalty takes this permille of raw Curse, as a Poach |

Revenue needs no constant. It is counted in the value pipeline's own units, and both
firms fight in the same round, so the scale is always shared. Revision 1's Share Points
and their per-round conversion table are gone (D-85 supersedes D-30).

### 3.3 Floors and rooms

| Name | Value |
| --- | --- |
| `FLOOR_MULT` | `{ G: 900, F1: 1000, F2: 1150, F3: 1450, B1: 1000 }` |
| `CORRIDOR_MULT` | 900 |
| `FLOOR_INDEX` | `{ B1: -1, G: 0, F1: 1, F2: 2, F3: 3 }` |
| `TENURE_TIER_ROUNDS` | `[3, 6, 10]` |
| `TENURE_STEP_PERMILLE` | 100 |
| `RECEPTION_CAP_PER_OCCUPANT` | 100 |
| `PORTAL_EMPLOYEE_CAP_TAX` | 100 |
| `B1_LEASE_CAP_TAX` | 150 |

### 3.4 Status effects

| Name | Value |
| --- | --- |
| `BURNOUT_MAX` | 5 |
| `BURNOUT_OUTPUT_PENALTY_PERMILLE` | 50 per stack |
| `OVERTIME_MAX` | 2 |
| `OVERTIME_DURATION` | 60 |
| `OVERTIME_RATE_PERMILLE` | 500 per stack |
| `BUREAUCRACY_MAX` | 3 |
| `BUREAUCRACY_DURATION` | 100 |
| `BUREAUCRACY_RATE_PERMILLE` | 200 per stack, subtracted |
| `RETRIGGER_DEPTH_MAX` | 1 |

---

## 4. Inputs

### 4.1 Seed

A 32-bit unsigned integer. It seeds the generator in §17 once, at match setup.

### 4.2 TowerSnapshot

The format from D-18, extended by D-25 with per-room Tenure. The sim reads exactly the
fields below and nothing else; a snapshot may carry more (the save file does), but a
conforming sim must produce identical output whether or not extra fields are present.

```jsonc
{
  "schemaVersion": 1,
  "contentVersion": "0.4.2",
  "round": 8,                          // informational; the match round is rules.round
  "floors": [
    {
      "index": 1,                      // FLOOR_INDEX value: -1, 0, 1, 2, 3
      "grid": { "w": 5, "h": 3 },
      "rooms": [
        {
          "roomId": "r1",              // unique within the snapshot
          "defId": "room.server_room",
          "rect": [1, 0, 2, 2],        // [col, row, w, h]
          "tenureRounds": 4            // rounds held; tier is derived
        }
      ],
      "occupants": [
        {
          "tile": [1, 0],              // [col, row]
          "kind": "employee",          // employee | furniture
          "defId": "emp.senior_dev",
          "instanceId": "e_7f3a",      // unique within the snapshot
          "attachments": []            // always empty in v1
        }
      ]
    }
  ],
  "globals": {
    "founderId": "founder.sato",     // the firm's founder; its effects apply like a modifier (empty in v1)
    "modifiers": ["mod.overtime_culture"],
    "riders": [ { "instanceId": "e_9c01", "riderId": "rider.tenured" } ],
    "leasedB1": true
  }
}
```

Definitions (`emp.*`, `room.*`, `furn.*`, `rider.*`, `mod.*`) resolve through the
content database at `contentVersion`. The sim is handed a resolved content table; it
never loads files.

`globals.founderId` names the firm's founder. A founder is a modifier-shaped entity
(`CONTENT_SCHEMA.md` §9) whose `effects` the sim applies exactly as it applies a
modifier's, in every place §5.5 and §14 say "modifier", ordered *before* the
modifiers list. In v1 every founder's effects list is empty, so the field changes no
outcome; it is in the snapshot so that a founder gaining a mechanic later is a content
change, not a format migration.

`globals.modifiers` is the same field on both sides. A campaign rival's gimmick
(`GAME_DESIGN.md` §15.5) and a player's Board Meeting modifier are both entries in it;
the sim applies whatever it finds and does not know which side is the player. The
content database, not the sim, marks a modifier as rival-only.

### 4.3 RuleSet

Every constant in §3, plus:

| Field | Meaning |
| --- | --- |
| `round` | The match round, 1–16. Drives `LOYALTY_BASE` for **both** firms |
| `contentVersion` | Must equal both snapshots' `contentVersion`, or `simulate` throws before setup |

The `RuleSet` is hashed into the replay header. Two matches with different rules are
different matches, even with the same seed and snapshots.

---

## 5. Match setup

Setup runs once, before tick 0, in this order. Any step that reads content reads the
resolved definitions.

### 5.1 Validation

Reject (throw) if: `contentVersion` mismatch; a floor index appears twice; a room
rectangle leaves its grid or overlaps another; two occupants share a tile; furniture is
not inside a room; an occupant references an unknown definition; an extraplanar
employee has no rider in `globals.riders`; B1 has occupants but `leasedB1` is false.
Validation is a pre-condition, not a rule of play — a rejected match has no result.

### 5.2 Canonical unit ordering

Every employee becomes a **unit**. Units are ordered:

1. Side A before side B.
2. Within a side: floor index ascending (so B1 first, then G, 1F, 2F, 3F).
3. Within a floor: row ascending, then column ascending.

Each unit receives `unitIndex` 0..n−1 in that order across both sides, and
`indexWithinSide` restarting at 0 for side B. Furniture is ordered the same way and
receives `furnIndex`. **Every iteration in this document over units or furniture is in
this order unless it says otherwise.**

### 5.3 Room membership and adjacency

A unit or furniture is **inside** a room if its tile lies within the room's rect. It is
in the **corridor** otherwise.

Two tiles are **adjacent** if they are on the same floor and differ by exactly one in
row or column (not both), **or** if both are in column 0 (the landing column) on floors
whose indices differ by exactly one. B1 (index −1) and G (index 0) are vertically
adjacent through the landing column like any other pair.

Room auras with a `wholeFloor` reach (Boardroom for Management retriggers) treat every
unit on the same floor as adjacent for that effect only.

### 5.4 Derived unit stats

For each unit, from its definition and surroundings:

| Stat | Derivation |
| --- | --- |
| `cdBase` | `definition.cooldownTicks * 1000` |
| `cdMultPermille` | Product, applied in furniture order, of every adjacent furniture and enclosing-room cooldown multiplier that applies to this unit's department (e.g. 90s PC 900, Open Plan Tier III 900). Default 1000 |
| `cdTotal(month)` | `floor(cdBaseFor(month) * cdMultPermille / 1000)`, where `cdBaseFor` honours per-month overrides such as the Senior Developer's Crunch cooldown |
| `cdProgress` | `floor(cdTotal(0) * definition.initialProgressPermille / 1000)`, default 0 |
| `roomAura(kind)` | The enclosing room's permille for this unit's department and ability kind, plus `TENURE_STEP_PERMILLE * tier` where `tier` is the count of `TENURE_TIER_ROUNDS` entries ≤ `tenureRounds`. Corridor: `CORRIDOR_MULT`. Room with no matching aura: 1000 |
| `flatBonus` | Sum of adjacent furniture flat bonuses matching this unit's department and ability kind (Whiteboard +15 for Engineering Sales) |
| `floorMult` | `FLOOR_MULT[floor]` |
| `retriggerBonus` | 1250 if adjacent to an Executive Desk and Management, else 1000 |
| `burnoutMax` | `BURNOUT_MAX`, or 3 if the unit's firm has a Head of People, or 0 if the unit is immune (Break Room occupant) |
| `bureaucracyImmune`, `frozenImmune`, `untargetable` | From room (Security Desk, its Tier III) and definition flags |
| `overtimePermanent` | Salaryman Ghost: holds 2 Overtime stacks that never expire and never cause Burnout |
| `fireCount` | 0 |
| statuses | Empty: `burnout = 0`, `overtime = []`, `bureaucracy = []`, `frozenUntil = -1` |

Tenure tier for a room: `tier = count of t in TENURE_TIER_ROUNDS where t <= tenureRounds`,
so 0–2 → 0, 3–5 → 1, 6–9 → 2, 10+ → 3.

### 5.5 Derived firm stats

For each firm, from `rules.round`, its snapshot and its units:

```
cap  = LOYALTY_BASE(round)
     + Σ unit passive loyaltyCap (× 1500 if the unit is inside a Legal Department, permille)
     + Σ adjacent Filing Cabinet bonuses to Legal units
     + RECEPTION_CAP_PER_OCCUPANT × (employees inside Reception)     // unless rider Poor Reception
     - PORTAL_EMPLOYEE_CAP_TAX × (extraplanar employees)
     - B1_LEASE_CAP_TAX if leasedB1
     - 100 per Corner Office occupant below Tier III
     + Σ founder and modifier cap deltas, founder first (Lean: -200)
cap  = max(cap, 1)

regenPerEvent = floor(cap * REGEN_BASE_PERMILLE / 1000)
              + Σ unit passive regenPerEvent (× 1500 inside a Legal Department)
              + 30 × Reception occupants if Reception is Tier III

loyalty         = cap
suppressThreshold = floor(cap * SUPPRESS_THRESHOLD_PERMILLE / 1000)
lastSuppressTick  = -1000
revenue         = 0
totalSales      = 0
```

Sums are computed in canonical unit order. `cap` is captured once as `capAtStart` and
never changes; the live `cap` is what Scandal erodes.

### 5.6 Initial match state

```
tick   = 0
rng    = mulberry32(seed)
seq    = 0
entries = []
ended  = false
```

---

## 6. Targeting

An ability that affects **employees** carries a two-level target: a floor selector and
an employee selector. Abilities of kind `sales`, `poach`, `scandal`, `curse` and `pr`
affect **firms** and carry no target. Every selector is a pure function of the
opponent's current state (or the caster's own, for self-side effects).

### 6.1 Floor selectors

A floor is **occupied** if it holds at least one unit. B1 is never selected except by
`all_floors`.

| Selector | Result | Tie-break |
| --- | --- | --- |
| `highest_occupied_floor` | The occupied floor with the greatest index in 0..3 | — |
| `lowest_occupied_floor` | The occupied floor with the least index in 0..3 | — |
| `most_populated_floor` | The floor in 0..3 with the most units | Lowest index |
| `least_populated_floor` | The occupied floor in 0..3 with the fewest units | Lowest index |
| `same_floor_index` | The target-side floor with the caster's floor index, if occupied | — |
| `random_floor` | One of the occupied floors in 0..3, chosen by §17 with `n` = count, floors listed in ascending index | — |
| `all_floors` | Every occupied floor, including B1, ascending index | — |

If a selector yields no floor, the ability **whiffs** (§9.5).

A unit adjacent to a Monitoring Station uses `most_populated_floor` in place of its
definition's floor selector.

### 6.2 Employee selectors

Applied to the units on a selected floor, after excluding units with `untargetable`
when the caster is the opponent.

| Selector | Result | Tie-break |
| --- | --- | --- |
| `lowest_cooldown_remaining` | The unit with the least `cdTotal(month) - cdProgress` | Lowest `unitIndex` |
| `highest_base_value` | The unit whose ability has the greatest constant `value` (0 for non-numeric) | Lowest `unitIndex` |
| `random` | One unit, chosen by §17 with `n` = count, units in `unitIndex` order | — |
| `all` | Every eligible unit, `unitIndex` order | — |

If exclusion leaves no unit, the ability whiffs.

### 6.3 Own-side selectors

Effects on the caster's own units (Standup, Wellness Program, Reorg, Deploy) use:

| Selector | Result |
| --- | --- |
| `adjacent` | Units adjacent to the caster (§5.3), `unitIndex` order. With the Boardroom aura, Management casters use the whole floor |
| `same_floor` | Units on the caster's floor, `unitIndex` order |
| `self` | The caster |

### 6.4 Stat and flag semantics

Content (`CONTENT_SCHEMA.md` §3) expresses every passive as a `stat` or a `flag` from a
closed list. This table is the sim's side of that contract: one row per word, what it
does, and where in this document it is applied. A word not in this table is not a word.

**Stats** — `amount` adds, `permille` multiplies; applied at setup (§5.4–§5.5) unless
noted. Room-granted `permille` stats gain `TENURE_STEP_PERMILLE × tier`.

| `stat` | Meaning | Applied |
| --- | --- | --- |
| `sales` | Multiplier on the subject's Sales value | §9.2 step 3 (`roomAura`) |
| `poach` | Multiplier on the subject's Poach value | §9.2 step 3 |
| `curse` | Multiplier on the subject's Curse value | §9.2 step 3 |
| `pr` | Multiplier on the subject's PR value | §9.2 step 3 |
| `flatSales` | Added to the subject's Sales before multipliers | §9.2 step 2 (`flatBonus`) |
| `cooldown` | Multiplier on the subject's `cdTotal` | §5.4 `cdMultPermille` |
| `loyaltyCap` | Added to the firm's Loyalty cap, once per subject unit | §5.5 |
| `loyaltyCapMult` | Multiplier on the firm's Loyalty cap after all additions | §5.5, final step |
| `regenPerEvent` | Added to the firm's regen, once per subject unit | §5.5 |
| `passiveMult` | Multiplier on the subject's own `loyaltyCap` and `regenPerEvent` contributions | §5.5 |
| `statusStacksBonus` | Added to `stacks` whenever the subject applies the named `status` | §12.3 |
| `burnoutMaxOverride` | Sets the subject's `burnoutMax`; lowest override wins | §5.4 |
| `burnoutMaxDelta` | Added to the subject's `burnoutMax` after overrides, floor 0 | §5.4 |
| `curseSelfCost` | Added (negative) to the subject's Curse self-cost permille, floor 0 | §9.3 |
| `retriggerBonus` | Multiplier applied to resolutions the subject retriggers | §9.2 step 6 |
| `floorOutput` | Multiplier on `floorMult` for the named `floor`, or every floor for `*` | §5.4 |
| `income`, `upkeep`, `rerollCost`, `severance`, `severanceMult` | Build-phase economy. The sim ignores them | — |

**Flags** — booleans on the subject; a flag set by any source is set.

| `flag` | Meaning | Applied |
| --- | --- | --- |
| `untargetable` | Excluded from enemy unit selectors, including `all` | §6.2 |
| `bureaucracyImmune`, `frozenImmune` | The status does nothing; entry tagged `immune` | §12.3 |
| `burnoutImmune` | `burnoutMax = 0` | §5.4 |
| `overtimePermanent` | Counts as 2 Overtime stacks always; never expires, never applies Burnout | §8.1, §12.3–§12.4 |
| `cannotBeRetriggered` | Retrigger whiffs with tag `retrigger_refused` | §13 |
| `wholeFloorAdjacency` | The subject's `adjacent` own-targets are every unit on its floor | §5.3 |
| `capProtected` | The subject's `loyaltyCap` contribution is a floor under Scandal erosion | §10.3 |
| `regenNeverSuppressed` | The firm's regen ignores `lastSuppressTick` | §11.1 |
| `receptionDisabled` | Reception occupants grant no cap | §5.5 |
| `everyFloorMostPopulated` | Against this firm, `most_populated_floor` returns every occupied floor in 0..3 | §6.1 |
| `floorSelectorMirror` | The firm's own `highest_occupied_floor` selections also resolve `lowest_occupied_floor`; targets are the union | §6.1 |
| `cannotBeLaidOff`, `landingOnly` | Build-phase only. The sim ignores them | — |

**Overrides** — `floorSelector` replaces the subject's floor selector with `to`
(§6.1, Monitoring Station); `tenureTier` sets every room of the subject firm to tier
`to` at setup (Old Money).

The `then` field on a `retrigger` effect applies its status to each target immediately
after that target's resolution — fire-then-burn, per unit (§13).

---

## 7. The tick loop

```
for tick in 0 .. QUARTER_TICKS - 1:
  A. Banners            (§14)   — if tick ∈ MONTH_START
  B. Expiry             (§12.4)
  D. Ready & resolve    (§8.2, §9)   — judged on progress accumulated through the previous tick
  C. Cooldown advance   (§8.1)
  E. Periodic events    (§11)
  F. (nothing)          (§15)   — the quarter never ends early (D-85)
Bell resolution         (§15)
```

The letters name the phases; the listing is the order they run in (D-62): readiness is
judged before the tick's advance, so a fresh 80-tick cooldown fires on tick 80, not 79.
Every phase runs to completion before the next begins. Phase D computes its ready list
once, at its start; units that become ready during phase D (there are none, since
progress only advances in C — but retriggers do not reset progress) are not added.
Nothing exits the loop before tick 1199.

---

## 8. Cooldowns and initiative

### 8.1 Advance

For each unit in canonical order:

```
if unit.frozenUntil > tick:
  rate = 0
else:
  rate = 1000
       + OVERTIME_RATE_PERMILLE   * overtimeStacks(unit)
       - BUREAUCRACY_RATE_PERMILLE * bureaucracyStacks(unit)
  rate = max(0, rate)
unit.cdProgress += rate
```

`overtimeStacks` is `len(unit.overtime)`, or 2 if `overtimePermanent`.
`bureaucracyStacks` is `len(unit.bureaucracy)`. `cdProgress` is in permille-ticks: a
unit with `cdTotal = 80000` (4.0 s) at rate 1000 has accumulated 80000 after the advance
of tick 79 and is ready in phase D of tick 80 (D-62).

### 8.2 Ready list and initiative

A unit is **ready** if `cdProgress >= cdTotal(month(tick))`. Collect every ready unit,
then sort by the key:

```
( cdTotal(month(tick)) ascending,
  sidePriority ascending,
  indexWithinSide ascending )
```

where `sidePriority(A) = tick mod 2` and `sidePriority(B) = 1 - (tick mod 2)`. On even
ticks A resolves first among equals; on odd ticks B does. This is the only place side
order is not "A first", and it exists so that simultaneity gives neither side a
standing edge.

Resolve each unit in that order (§9). After a unit's own resolution — not after a
retriggered one — set `cdProgress = 0`. A unit that was Frozen during phase D by an
earlier resolution in the same phase still fires; it was ready when the list was built.

---

## 9. Resolution

`resolve(unit, tick, depth, viaRetrigger)` — `depth` is 0 for a unit's own fire, 1 for a
retriggered fire.

### 9.1 Steps

1. `unit.fireCount += 1`.
2. Compute the ability value (§9.2), if the kind has one.
3. Select targets (§6), consuming the RNG only if the selector requires it.
4. Apply the primary effect by kind (§9.3).
5. Apply `extraEffects` in definition order, each with its own targeting (§9.4).
6. Emit the ledger entry (§16) for the primary effect; extra effects emit their own.

### 9.2 Value pipeline

For kinds `sales`, `poach`, `curse`, `pr`, and for the Scandal event in §11.2:

```
v = baseValue(unit, ability)                                  // §9.2.1
v = v + unit.flatBonus                                         // furniture flats for this dept and kind
v = floor(v * unit.roomAura(kind) / 1000)                      // room aura incl. Tenure; corridor 900; none 1000
v = floor(v * unit.floorMult / 1000)
v = floor(v * (1000 - BURNOUT_OUTPUT_PENALTY_PERMILLE * unit.burnout) / 1000)
v = floor(v * (viaRetrigger ? retriggerer.retriggerBonus : 1000) / 1000)
v = floor(v * (kind == pr ? 1000 : RUSH_MULT[month(tick)]) / 1000)
```

Seven steps, in that order, flooring after each. `pr` is not scaled by the month.
Scandal from Burnout skips the first six steps (§11.2).

#### 9.2.1 Base value forms

| Form | Definition field | Value |
| --- | --- | --- |
| constant | `value: 60` | 60 |
| per-tag | `value: { base: 300, perTag: "sales", each: 50 }` | `base + each × (own units with the tag)` |
| percent of target cap | `value: { permilleOfTargetCap: 150 }` | `floor(opponent.cap * 150 / 1000)` using the rival's live Loyalty cap |

### 9.3 Primary effects by kind

**`sales`** — target: own firm. Making money, in proportion to how firmly the firm's
clients stay (D-87): Poaching that drains Loyalty, and Scandal that cuts the cap under it,
both cut Sales.

```
earned = floor(v * seller.loyalty / max(1, seller.capAtStart))
seller.revenue    += earned
seller.totalSales += earned
```

**`poach`** — target: opposing firm.

```
applyPoach(attacker, defender, v, tick)        // §10.2
```

**`scandal`** — target: opposing firm. Used by abilities that deal Scandal directly
(none in the roster; the kind exists for the Contractual Obligation rider and for
content).

```
applyScandal(defender, v, creditTo = attacker) // §10.3
```

**`curse`** — target: opposing firm.

```
transfer(from = defender, to = attacker, v)    // §10.1, straight through Loyalty
selfCostPermille = max(0, CURSE_SELF_COST_PERMILLE - circleReduction - ofudaReduction)
                 // Summoning Circle occupant: 125; adjacent Ofuda: 125
self = floor(v * selfCostPermille / 1000)
if self > 0: applyPoach(attacker, attacker, self, tick)  // it suppresses the curser's own regen
```

**`pr`** — target: own firm.

```
applied = min(v, attacker.cap - attacker.loyalty)
attacker.loyalty += applied
```

Only `sales` counts toward `totalSales`.

**`status`** — targets from §6. For each target in order, apply the listed stacks
(§12.3). Status abilities have no value.

**`retrigger`** — targets from §6.3. For each target in order, `retriggerUnit`
(§13). Retrigger abilities have no value.

### 9.4 Extra effects

An ability may list `extraEffects`, each `{ kind: status | retrigger, targeting,
stacks }`. They resolve after the primary effect, in listed order, with the same
`depth`. Examples: Counsel's Cease & Desist is `poach 90` with an extra
`status bureaucracy 2 → highest_occupied / highest_base_value`; DevOps's Deploy is
`sales 90` with an extra `status overtime 1 → adjacent, dept engineering`.

Room Tier III clauses that add effects (Sales Floor, Summoning Circle) are appended to
the occupant's `extraEffects` at setup, after the definition's own.

### 9.5 Whiff

If a targeted effect selects no unit or no floor, the effect does nothing and emits a
`whiff` entry. A `sales` or `poach` never whiffs. A whiffed primary effect still runs its extra
effects, which may whiff independently. A whiff still resets cooldown.

---

## 10. Loyalty, overflow and Revenue

### 10.1 transfer

```
transfer(from, to, amount):
  taken = min(amount, from.revenue)
  from.revenue -= taken
  to.revenue   += taken
  return taken
```

A transfer never makes Revenue negative. What it could not take is lost, not owed
(`REVENUE_RACE.md` O-4); the entry still carries the amount that was attempted (§16.1),
so the ledger shows the shortfall.

### 10.2 applyPoach

```
applyPoach(attacker, defender, v, tick):
  if v >= defender.suppressThreshold:
    defender.lastSuppressTick = tick
  absorbed = min(v, defender.loyalty)
  defender.loyalty -= absorbed
  overflow = v - absorbed
  taken = 0
  if overflow > 0:
    taken = transfer(from = defender, to = the firm opposing defender, overflow)
  return (absorbed, overflow, taken)
```

Note that in the Curse self-cost case `attacker == defender`, and the overflow goes to
the *opponent*: breaking your own Loyalty with self-inflicted Poach hands the rival your
Revenue. That is intentional.

### 10.3 applyScandal

```
applyScandal(defender, raw, creditTo):
  defender.cap = max(1, defender.cap - raw)
  defender.loyalty = min(defender.loyalty, defender.cap)
  taken = transfer(from = defender, to = creditTo, floor(raw * SCANDAL_TRANSFER_PERMILLE / 1000))
  return taken
```

Scandal never touches `lastSuppressTick`. The cap floor of 1 keeps `suppressThreshold`
and regen arithmetic defined; `suppressThreshold` is computed from `capAtStart` and
does not shrink with the cap.

Legal Department Tier III: the cap contributions of its occupants are protected —
`defender.cap` may not fall below `Σ protected contributions`. Apply as a second floor
in the `max`.

---

## 11. Periodic events

Phase E, in the order listed, each only on ticks where its condition holds.
`tick > 0` throughout — nothing periodic fires at tick 0.

### 11.1 Regen — `tick mod REGEN_INTERVAL == 0`

For each firm, A then B:

```
suppressed = (tick - firm.lastSuppressTick) <= REGEN_SUPPRESS_WINDOW
if suppressed:
  amount = 0
else:
  amount = floor(firm.regenPerEvent * REGEN_MULT[month(tick)] / 1000)
applied = min(amount, firm.cap - firm.loyalty)
firm.loyalty += applied
emit regen entry (raw = amount, loyaltyDelta = applied, tag "suppressed" if suppressed)
```

A regen entry is emitted every event, including when `applied` is 0. Views decide what
to show; the record is complete.

### 11.2 Scandal from Burnout — `tick mod SCANDAL_INTERVAL == 0`

For each firm, A then B:

```
stacks = Σ unit.burnout over the firm's units
if stacks == 0: continue
raw   = floor(SCANDAL_PER_STACK * stacks * RUSH_MULT[month(tick)] / 1000)
taken = applyScandal(defender = firm, raw, creditTo = opponent)
emit scandal entry (sourceSide = firm, targetSide = firm, raw, capDelta = -raw, revenueDelta = -taken, tag "burnout")
```

The firm burns itself. The opponent gets the money.

### 11.3 Periodic furniture and rooms

In `furnIndex` order for furniture, then room order (side, floor, `roomId` ascending)
for rooms:

| Source | Condition | Effect |
| --- | --- | --- |
| Fax Machine | `tick mod 120 == 0` | The adjacent Legal unit with the highest base value: `retriggerUnit` (§13) with the Fax as retriggerer (bonus 1000). Whiff entry if none |
| Water Cooler | `tick mod 200 == 0` | Each adjacent unit: `burnout = max(0, burnout - 1)`; emit a status entry per unit changed |
| Break Room, Tier III | `tick mod 200 == 0` | Each unit adjacent to any occupant: as Water Cooler |

---

## 12. Status effects

### 12.1 Representation

| Status | State on the unit |
| --- | --- |
| Burnout | `burnout: int`, 0..`burnoutMax` |
| Overtime | `overtime: int[]` — expiry ticks, ascending; length ≤ `OVERTIME_MAX` |
| Bureaucracy | `bureaucracy: int[]` — expiry ticks, ascending; length ≤ `BUREAUCRACY_MAX` |
| Frozen | `frozenUntil: int` — the first tick at which the unit is no longer frozen |

### 12.2 Effects while held

| Status | Effect | Where applied |
| --- | --- | --- |
| Burnout | Sales, Poach, Curse and PR value × `(1000 − 50 × stacks)`; firm takes a Scandal each `SCANDAL_INTERVAL` | §9.2 step 5, §11.2 |
| Overtime | Cooldown rate +500 per stack | §8.1 |
| Bureaucracy | Cooldown rate −200 per stack | §8.1 |
| Frozen | Cooldown rate 0 | §8.1 |

### 12.3 Application

`applyStatus(unit, status, n, tick)`:

**Burnout.** `unit.burnout = min(unit.burnoutMax, unit.burnout + n)`. If
`burnoutMax` is 0 the unit is immune and nothing happens; emit the status entry with
`stacksApplied = 0` and tag `immune`.

**Overtime.** If `overtimePermanent`, nothing happens (tag `permanent`). Otherwise,
`n` times: if `len(overtime) < OVERTIME_MAX`, append `tick + OVERTIME_DURATION`;
else replace the smallest expiry with `tick + OVERTIME_DURATION`. Re-sort ascending.

**Bureaucracy.** If `bureaucracyImmune`, nothing (tag `immune`). Otherwise as
Overtime with `BUREAUCRACY_MAX` and `BUREAUCRACY_DURATION`.

**Frozen.** If `frozenImmune`, nothing. Otherwise
`frozenUntil = max(frozenUntil, tick + durationTicks)`.

One status entry is emitted per `(target, status)` application, carrying
`stacksRequested` and `stacksApplied`.

### 12.4 Expiry — phase B

For each unit in canonical order:

1. **Overtime.** While `overtime[0] <= tick`: remove it; then unless
   `overtimePermanent` or `burnoutMax == 0`, `applyStatus(unit, burnout, 1, tick)`
   and emit a status entry tagged `overtime_expired`.
2. **Bureaucracy.** Remove every expiry `<= tick`. No entry is emitted; expiries are
   derivable from the application entries.
3. **Frozen.** Nothing to do; `frozenUntil` is compared, not decremented.

Expiry runs before cooldown advance, so a stack expiring at tick `t` does not
contribute to rate at tick `t`.

---

## 13. Retriggers

`retriggerUnit(target, tick, depth, retriggerer)`:

```
if depth >= RETRIGGER_DEPTH_MAX:
  emit whiff entry (tag "retrigger_depth")
  return
if target.definition.cannotBeRetriggered:      // The Auditor
  emit whiff entry (tag "retrigger_refused")
  return
resolve(target, tick, depth + 1, viaRetrigger = true, retriggerer)
```

The target's `cdProgress` is **not** changed. A retriggered resolution may not itself
retrigger (the depth check), so a Team Lead delegating to a Director makes the Director
whiff on every adjacent unit — legibly, in the ledger.

Reorg (Director): for each adjacent unit in `unitIndex` order, `retriggerUnit`, then
`applyStatus(unit, burnout, 1)`. Fire-then-burn, per unit, not all-fire-then-all-burn.

---

## 14. Banners and month transitions

Phase A, on `tick ∈ MONTH_START`. Emit a `banner` entry with `sourceSide = "*"` and the
month index, then apply banner effects in this order:

1. For each firm, A then B:
   1. For each unit in canonical order: its enclosing room's banner effect, if any.
   2. For each furniture in `furnIndex` order: its banner effect, if any.
   3. For each rider in `globals.riders` order: its banner effect, if any.
   4. The founder's banner effect, if any; then, for each modifier in `globals.modifiers` order, its banner effect, if any.

| Tick | Source | Effect |
| --- | --- | --- |
| 0 | Yakult Cart | Each adjacent unit: Overtime +1 |
| 0 | Modifier *Overtime Culture* | Each own unit: Burnout +1, then Overtime +1 |
| 0 | Rider *Bad Influence* | Each unit adjacent to the rider's unit: Burnout +1 |
| 400, 800 | Server Room below Tier III | Each occupant: Burnout +1 |
| 1160 | Rider *Contractual Obligation* | `applyScandal(own firm, 200 × RUSH_MULT[3] / 1000, creditTo = opponent)` — i.e. 600 raw at the Bell |

Banner effects at tick 0 run before any cooldown has advanced. Banner effects run
before expiry, so an Overtime granted at tick 0 expires in phase B of tick 60.

---

## 15. End of match

### 15.1 Phase F

Nothing ends a quarter early (D-85). Phase F does nothing; it keeps its letter so the
phase names of D-62 stay stable.

### 15.2 Bell resolution

After tick 1199:

```
if A.revenue != B.revenue:          winner = the firm with more revenue
elif A.loyalty != B.loyalty:        winner = the firm with more loyalty
elif A.totalSales != B.totalSales:  winner = the firm with more totalSales
else:                               winner = "draw"
endTick = 1199
```

Modes interpret `draw` (campaign: loss without a strike; ranked: no rating change). The
sim does not.

---

## 16. Ledger entries and the replay

### 16.1 Entry

Every event in the match is one entry. Field order is normative for serialisation.

```jsonc
{
  "seq": 241,                    // 0-based, dense, in emission order
  "tick": 412,
  "kind": "sales",               // sales | poach | scandal | curse | pr | regen | status | retrigger | whiff | banner
  "sourceSide": "A",             // "A" | "B" | "*"
  "sourceUnit": 7,               // unitIndex, or -1 for firm-level, furniture and banners
  "sourceInstanceId": "e_7f3a",  // "" when sourceUnit is -1
  "sourceFloor": 2,              // FLOOR_INDEX, or -2 when not applicable
  "abilityId": "ability.ship_feature",   // or furniture/room/rider/modifier id, or "regen", "burnout", "banner"
  "targetSide": "B",             // "A" | "B" | "*"
  "targetUnits": [],             // unitIndex list; empty for firm-level
  "raw": 840,                    // the value after the pipeline, before absorption
  "loyaltyDelta": -840,          // change to targetSide's loyalty (negative for Poach; positive for pr/regen)
  "capDelta": 0,                 // change to targetSide's cap
  "revenueDelta": 0,             // change to targetSide's revenue; negative is always a transfer to the other firm
  "overflow": 0,                 // poach and curse self-cost only: the part past Loyalty
  "stacks": 0,                   // status only: stacksApplied
  "depth": 0,                    // 0, or 1 when retriggered
  "month": 1,                    // month index at emission, 0..3
  "tags": []                     // sorted ascending; e.g. ["suppressed"], ["burnout"], ["overtime_expired"]
}
```

Per-kind conventions:

| Kind | source | target | raw | loyaltyDelta | capDelta | revenueDelta |
| --- | --- | --- | --- | --- | --- | --- |
| sales | seller unit | seller firm | v | 0 | 0 | +earned (§9.3) |
| poach | attacker unit | defender firm | v | −absorbed | 0 | −taken (to the attacker) |
| curse | attacker unit | defender firm | v | 0 | 0 | −taken (to the attacker) |
| curse self-cost | attacker unit | attacker firm | self | −absorbed | 0 | −taken (to the opponent) — emitted as a second entry tagged `self_cost` |
| scandal (ability) | attacker unit | defender firm | raw | 0 or negative if loyalty was clamped to the cap | −raw | −taken (to the attacker) |
| scandal (event) | firm (`sourceUnit` −1) | same firm | raw | 0 or negative if loyalty was clamped to the cap | −raw | −taken (to the opponent) |
| pr | caster unit | caster firm | v | +applied | 0 | 0 |
| regen | firm | same firm | amount | +applied | 0 | 0 |
| status | caster unit or furniture | each target unit — one entry per target | 0 | 0 | 0 | 0 |
| retrigger | retriggerer | target unit — one entry per target, emitted **before** the target's resolution entries | 0 | 0 | 0 | 0 |
| whiff | caster | `*` | 0 | 0 | 0 | 0 |
| banner | `*` | `*` | month index | 0 | 0 | 0 |

`loyaltyDelta` on a Scandal entry is negative only when `loyalty` exceeded the new cap
and was clamped; the clamped amount is recorded so the entry accounts for every point
of Loyalty that moved.

A negative `revenueDelta` is always a transfer: the firm opposite `targetSide` gained
exactly that amount. Only `sales` entries have a positive one. The amount a transfer
*attempted* is derivable from the entry — a Poach's `overflow`, a Curse's `raw`, a
Scandal's `floor(raw × SCANDAL_TRANSFER_PERMILLE / 1000)` — so a transfer that could not
take it all shows the shortfall.

### 16.2 MatchResult

```jsonc
{
  "schemaVersion": 2,
  "seed": 3735928559,
  "round": 8,
  "rulesHash": "…",              // FNV-1a 32 of the RuleSet's canonical serialisation
  "snapshotHashA": "…",          // FNV-1a 32 of the snapshot's canonical serialisation
  "snapshotHashB": "…",
  "winner": "A",                 // "A" | "B" | "draw"
  "endTick": 1199,               // always 1199: every quarter runs to the Bell
  "finalRevenue": { "A": 11240, "B": 3875 },
  "finalLoyalty": { "A": 812, "B": 0 },
  "finalCap": { "A": 2100, "B": 1800 },
  "totalSales": { "A": 9310, "B": 4200 },
  "entries": [ … ],
  "stateHash": "0x9c3b1e77"
}
```

The **replay** is the `MatchResult`. There is no second format. A replay viewer plays
`entries` in `seq` order; the autopsy filters them; the balance harness reads
`winner`, `endTick` and the aggregates. Re-running `simulate` with the header's inputs
reproduces the file byte for byte.

### 16.3 stateHash

FNV-1a, 32-bit, over the UTF-8 bytes of the string:

```
winner | endTick | revenueA | revenueB | loyaltyA | loyaltyB | capA | capB | salesA | salesB | entryCount | rngState
```

joined with the ASCII pipe character and no spaces; integers in decimal; `rngState` as
the generator's 32-bit state after the last draw, in decimal. Output as `0x` plus eight
lowercase hex digits. This hash is what two implementations compare first; on a
mismatch, they diff `entries`.

### 16.4 Canonical serialisation

For `rulesHash` and `snapshotHashA/B`: JSON with object keys in the order this
document lists them, arrays in their given order, no whitespace, integers in decimal,
strings JSON-escaped. Fields not listed in this document are excluded from the hash
even if present in the file.

---

## 17. Random numbers

One generator per match, seeded once. **mulberry32**, chosen because it is tiny, fast,
32-bit throughout, and trivially identical across languages. The canonical algorithm,
as the C# reference implements it (all arithmetic `unchecked`, all values `uint`):

```csharp
uint state = seed;

uint Next()                                    // returns u32
{
    state += 0x6D2B79F5u;
    uint t = state;
    t = (t ^ (t >> 15)) * (t | 1u);
    t ^= t + (t ^ (t >> 7)) * (t | 61u);
    return t ^ (t >> 14);
}

uint Draw(uint n)                              // returns 0..n-1
    => (uint)(((ulong)Next() * n) >> 32);       // exact; no rejection sampling
```

In a language without native 32-bit wrapping (JavaScript) the same steps are written
with `Math.imul` and `>>> 0`; the results are identical. An earlier draft of this
listing omitted the `^=` on the fourth line and did not match canonical mulberry32; it
was corrected before any fixture was recorded (D-61).

`draw` is biased by at most `n / 2^32` and that is acceptable; what matters is that
every implementation is biased identically. `draw` is called **only** by
`random_floor` and `random` (§6). Nothing else consumes the generator, and the draws
occur in resolution order. `draw(1)` must still consume a value.

---

## 18. Determinism contract

An implementation conforms if and only if all of the following hold.

1. **Integers only.** No floating-point value is ever stored in match state or
   compared. Multipliers are permille; division is exact integer division with floor.
2. **Fixed iteration order.** Every loop over units, furniture, rooms, riders or
   modifiers uses the canonical orders in §5.2 and §14. Object-key iteration order is
   never relied on.
3. **One RNG, listed consumers.** Only §6's `random_floor` and `random` call `draw`.
4. **No ambient input.** No clock, no `System.Random` (or the language's equivalent),
   no environment, no I/O, no locale-dependent string operations. In the C# reference
   this is enforced by a banned-API analyzer and a reflection test on the assembly's
   references (`ARCHITECTURE.md` §1).
5. **Content is data.** The sim receives resolved definitions; it never parses files
   and never contains a definition.
6. **Complete emission.** Every state change in §9–§14 emits the entry §16.1 requires,
   with `seq` dense from 0.
7. **Hash agreement.** For every fixture in §19, the implementation's `stateHash` and
   `entries` match the recorded ones exactly.
8. **Rule changes bump the version.** Any change to a rule in this document increments
   `schemaVersion` and invalidates recorded fixtures, which are then regenerated and
   reviewed. A change to a §3 constant does not.

---

## 19. Conformance

The repository carries `fixtures/sim/` — a set of `(seed, snapshotA, snapshotB,
rules)` inputs with their recorded `MatchResult`. CI runs every fixture through the
reference implementation and fails on any hash mismatch. A second implementation
(server-side re-simulation for ranked, D-20) is conforming when it passes the same set.

Minimum fixture set, all at round 1 unless stated:

| Fixture | Exercises |
| --- | --- |
| `mirror_junior` | One Junior Developer each, corridor. Ends in a draw at the Bell. §20 |
| `overflow` | A's single Architect at round 16 against an empty tower. Large Sales. *Needs a new input: the Architect earns Sales since revision 2, so nothing overflows; a Poach source is wanted* |
| `regen_suppress` | QA Tester against a Recruiter. *Needs a new input: the QA Tester earns Sales since revision 2, so Loyalty is never chipped; a Poach below `SUPPRESS_THRESHOLD` is wanted* |
| `burnout_pierce` | Consultant against a Legal turtle. Scandal erosion; Bell ordering |
| `retrigger_depth` | Team Lead adjacent to a Director adjacent to three units. Depth whiffs |
| `tie_parity` | Two identical towers with identical cooldowns. Alternating initiative |
| `random_selector` | A Paralegal with `random_floor` against a three-floor tower. RNG draw order |
| `anomaly_selfcost` | Salaryman Ghost in a Summoning Circle with and without Ofuda. Curse and its self-cost |
| `banner_order` | Yakult Cart, Overtime Culture and Bad Influence on the same tower at tick 0 |
| `boss_act2` | The Compliance Office against the intended Act 2 counter-build: out-earn it, with Scandal and Curse |

---

## 20. Worked trace

Round 1. Each side: one Junior Developer on 1F at column 2, row 1, in the corridor.
No rooms other than each side's empty Reception, no furniture, seed irrelevant (no
random selectors). The Junior Developer's Ship Feature is a `sales` ability.

**Setup.** Both firms: `cap = 700`, `regenPerEvent = 21`, `suppressThreshold = 28`,
`loyalty = 700`, `revenue = 0`. Units: A0 (`unitIndex` 0), B0 (`unitIndex` 1).
`cdTotal = 80000`. Value pipeline for Ship Feature: `60 → +0 → ×900 = 54 → ×1000 = 54
→ ×1000 = 54 → ×1000 = 54 → ×1000 = 54`. Both earn **54**.

| Tick | Phase | Event | A revenue | B revenue | A loyalty | B loyalty |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | A | banner month 0 | 0 | 0 | 700 | 700 |
| 40 | E | regen A: 21, applied 0 (Loyalty is full). regen B: same | 0 | 0 | 700 | 700 |
| 80 | D | ready: A0, B0. Even tick → A first. A0 sells 54; B0 sells 54 | 54 | 54 | 700 | 700 |
| 80 | E | regen: not suppressed (nobody Poaches), applied 0 | 54 | 54 | 700 | 700 |
| 160 | D | both sell 54 | 108 | 108 | 700 | 700 |
| 240 | D | both sell 54 | 162 | 162 | 700 | 700 |

Neither firm Poaches, so Loyalty never moves and every regen entry applies 0. Month 1
has four fires (80, 160, 240, 320): each firm ends it with ¥216.

At tick 400 the banner fires: `RUSH_MULT` 1400 makes each sale `floor(54 × 1400 / 1000)
= 75`. Month 2 has five fires (400, 480, 560, 640, 720): `216 + 375 = 591`.

Crunch makes each sale 108, and it has five fires (800, 880, 960, 1040, 1120):
`591 + 540 = 1131`. Nothing fires in the Bell window (1120 is the last multiple of 80
below 1160). Bell resolution: Revenue 1131 = 1131, Loyalty 700 = 700, `totalSales`
1131 = 1131 → **draw**, `endTick = 1199`.

Ticks 80, 160, 240 … are all even, so A always resolves first in this trace. Parity
alternates only when a cooldown's tick count is odd: equal 60-tick cooldowns fire on 60,
120, 180 — all even — and still give A the edge. The `tie_parity` fixture records that;
Overtime and cooldown multipliers are what produce odd tick counts in play (D-62).

**Entry count** for this fixture: 4 banner entries (ticks 0, 400, 800, 1160); 29 regen
events (ticks 40 … 1160) × 2 firms = 58 regen entries; 14 fires (ticks 80 … 1120) × 2
units = 28 sales entries. **90 entries**, `seq` 0–89. An implementation producing a
different count has a phase-order bug. Expected `finalRevenue` `{A: 1131, B: 1131}`,
`finalLoyalty` `{A: 700, B: 700}`, `finalCap` `{A: 700, B: 700}`, `totalSales`
`{A: 1131, B: 1131}`.
