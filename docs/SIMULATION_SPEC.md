# Company Wars — Simulation Specification

Status: **Phase 2 draft.** This is the combat simulation as an implementable
specification. It is written so that two independent implementations, given the same
seed and the same two tower snapshots, produce byte-identical ledgers and the same
state hash. Where `GAME_DESIGN.md` and this document disagree, this document wins for
anything the sim computes.

Every number in §3 is an initial value owned by `BALANCE_PLAN.md`. Every *rule* is
owned here. Changing a number does not change this document's version; changing a
rule does.

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
10. [Goodwill, overflow and Market Share](#10-goodwill-overflow-and-market-share)
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
- **Completeness.** Every change to Goodwill, Goodwill cap, Market Share, or an
  employee's status is represented by exactly one ledger entry, in the order it
  happened.
- **Integrality.** No value in the sim is ever a non-integer.

Not in scope: the build phase, crafting, the economy, snapshot legality validation.
Those are deterministic too, but they are not this function.

---

## 2. Numeric model

All quantities are integers. JavaScript implementations must keep every intermediate
value within the exact-integer range of a double (|v| < 2^53); the multiplier chain in
§9.2 is ordered so that no intermediate exceeds ~10^12.

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
| `PUSH_MULT` | `[1000, 1400, 2000, 3000]` | Per month, applied to Push, Morale and Anomaly |
| `REGEN_MULT` | `[1000, 600, 200, 0]` | Per month, applied to regen |

`month(tick)` is the largest index `i` such that `MONTH_START[i] <= tick`.

### 3.2 Goodwill and Market Share

| Name | Value | Meaning |
| --- | --- | --- |
| `GOODWILL_BASE(round)` | `600 + 100 * round` | Base cap for both firms |
| `REGEN_BASE_PERMILLE` | 30 | Base regen per event as permille of cap |
| `REGEN_INTERVAL` | 40 | Ticks between regen events |
| `REGEN_SUPPRESS_WINDOW` | 20 | A suppressing hit within this many ticks blocks regen |
| `SUPPRESS_THRESHOLD_PERMILLE` | 40 | A Push hit must be at least this permille of the target's cap to suppress |
| `MORALE_INTERVAL` | 20 | Ticks between Burnout Morale events |
| `MORALE_PER_STACK` | 8 | Raw Morale per Burnout stack per event |
| `MORALE_RATE_PERMILLE` | 500 | Morale converts to Market Share at this permille of raw |
| `ANOMALY_SELF_COST_PERMILLE` | 250 | Attacker's own Goodwill takes this permille of raw Anomaly |
| `SHARE_TOTAL` | 10000 | Market Share resolution. Displayed as `share / 100` percent |
| `SHARE_START` | 5000 | |
| `SP_PER_PUSH_PERMILLE[round]` | see below | Share Points per point of Push, permille |

`SP_PER_PUSH_PERMILLE`, rounds 1–16:

```
[8000, 7000, 6000, 5200, 4500, 3900, 3400, 3000, 2600, 2300, 2000, 1750, 1500, 1300, 1150, 1000]
```

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
| `BURNOUT_PUSH_PENALTY_PERMILLE` | 50 per stack |
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
    "modifiers": ["mod.overtime_culture"],
    "riders": [ { "instanceId": "e_9c01", "riderId": "rider.tenured" } ],
    "leasedB1": true
  }
}
```

Definitions (`emp.*`, `room.*`, `furn.*`, `rider.*`, `mod.*`) resolve through the
content database at `contentVersion`. The sim is handed a resolved content table; it
never loads files.

`globals.modifiers` is the same field on both sides. A campaign rival's gimmick
(`GAME_DESIGN.md` §15.5) and a player's Board Meeting modifier are both entries in it;
the sim applies whatever it finds and does not know which side is the player. The
content database, not the sim, marks a modifier as rival-only.

### 4.3 RuleSet

Every constant in §3, plus:

| Field | Meaning |
| --- | --- |
| `round` | The match round, 1–16. Drives `GOODWILL_BASE` and `SP_PER_PUSH_PERMILLE` for **both** firms |
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
| `flatBonus` | Sum of adjacent furniture flat bonuses matching this unit's department and ability kind (Whiteboard +15 for Engineering Push) |
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
cap  = GOODWILL_BASE(round)
     + Σ unit passive goodwillCap (× 1500 if the unit is inside a Legal Department, permille)
     + Σ adjacent Filing Cabinet bonuses to Legal units
     + RECEPTION_CAP_PER_OCCUPANT × (employees inside Reception)     // unless rider Poor Reception
     - PORTAL_EMPLOYEE_CAP_TAX × (extraplanar employees)
     - B1_LEASE_CAP_TAX if leasedB1
     - 100 per Corner Office occupant below Tier III
     + Σ modifier cap deltas (Lean: -200)
cap  = max(cap, 1)

regenPerEvent = floor(cap * REGEN_BASE_PERMILLE / 1000)
              + Σ unit passive regenPerEvent (× 1500 inside a Legal Department)
              + 30 × Reception occupants if Reception is Tier III

goodwill        = cap
suppressThreshold = floor(cap * SUPPRESS_THRESHOLD_PERMILLE / 1000)
lastSuppressTick  = -1000
spCarry         = 0
totalPush       = 0
```

Sums are computed in canonical unit order. `cap` is captured once as `capAtStart` and
never changes; the live `cap` is what Morale erodes.

### 5.6 Initial match state

```
tick   = 0
share  = SHARE_START          // side A's claim, 0..SHARE_TOTAL
rng    = mulberry32(seed)
seq    = 0
entries = []
ended  = false
```

---

## 6. Targeting

An ability that affects **employees** carries a two-level target: a floor selector and
an employee selector. Abilities of kind `push`, `morale`, `anomaly` and `restore`
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

---

## 7. The tick loop

```
for tick in 0 .. QUARTER_TICKS - 1:
  A. Banners            (§14)   — if tick ∈ MONTH_START
  B. Expiry             (§12.4)
  C. Cooldown advance   (§8.1)
  D. Ready & resolve    (§8.2, §9)
  E. Periodic events    (§11)
  F. End check          (§15)   — if ended, stop
Bell resolution         (§15)
```

Every phase runs to completion before the next begins. Phase D computes its ready list
once, at its start; units that become ready during phase D (there are none, since
progress only advances in C — but retriggers do not reset progress) are not added.
Phase F is the only exit from the loop before tick 1199.

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
unit with `cdTotal = 80000` (4.0 s) at rate 1000 is ready after 80 ticks.

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

For kinds `push`, `anomaly`, `restore`, and for the Morale event in §11.2:

```
v = baseValue(unit, ability)                                  // §9.2.1
v = v + unit.flatBonus                                         // furniture flats for this dept and kind
v = floor(v * unit.roomAura(kind) / 1000)                      // room aura incl. Tenure; corridor 900; none 1000
v = floor(v * unit.floorMult / 1000)
v = floor(v * (1000 - BURNOUT_PUSH_PENALTY_PERMILLE * unit.burnout) / 1000)
v = floor(v * (viaRetrigger ? retriggerer.retriggerBonus : 1000) / 1000)
v = floor(v * (kind == restore ? 1000 : PUSH_MULT[month(tick)]) / 1000)
```

Seven steps, in that order, flooring after each. `restore` is not scaled by the month.
Morale from Burnout skips the first six steps (§11.2).

#### 9.2.1 Base value forms

| Form | Definition field | Value |
| --- | --- | --- |
| constant | `value: 60` | 60 |
| per-tag | `value: { base: 300, perTag: "sales", each: 50 }` | `base + each × (own units with the tag)` |
| percent of target cap | `value: { permilleOfTargetCap: 150 }` | `floor(opponent.cap * 150 / 1000)` using the live cap |

### 9.3 Primary effects by kind

**`push`** — target: opposing firm.

```
applyPush(attacker, defender, v, tick)         // §10.1
attacker.totalPush += v
```

**`morale`** — target: opposing firm. Used by abilities that deal Morale directly (none
in the Phase 2 roster; the kind exists for the Contractual Obligation rider and for
content).

```
applyMorale(defender, v, creditTo = attacker)  // §10.2
```

**`anomaly`** — target: opposing firm.

```
sp = convertToShare(attacker, v)               // §10.3, full rate
selfCostPermille = max(0, ANOMALY_SELF_COST_PERMILLE - circleReduction - ofudaReduction)
                 // Summoning Circle occupant: 125; adjacent Ofuda: 125
self = floor(v * selfCostPermille / 1000)
if self > 0: applyPush(attacker, attacker, self, tick)   // it suppresses the attacker's own regen
attacker.totalPush += v
```

**`restore`** — target: own firm.

```
applied = min(v, attacker.cap - attacker.goodwill)
attacker.goodwill += applied
```

**`status`** — targets from §6. For each target in order, apply the listed stacks
(§12.3). Status abilities have no value.

**`retrigger`** — targets from §6.3. For each target in order, `retriggerUnit`
(§13). Retrigger abilities have no value.

### 9.4 Extra effects

An ability may list `extraEffects`, each `{ kind: status | retrigger, targeting,
stacks }`. They resolve after the primary effect, in listed order, with the same
`depth`. Examples: Counsel's Cease & Desist is `push 90` with an extra
`status bureaucracy 2 → highest_occupied / highest_base_value`; DevOps's Deploy is
`push 90` with an extra `status overtime 1 → adjacent, dept engineering`.

Room Tier III clauses that add effects (Sales Floor, Summoning Circle) are appended to
the occupant's `extraEffects` at setup, after the definition's own.

### 9.5 Whiff

If a targeted effect selects no unit or no floor, the effect does nothing and emits a
`whiff` entry. A `push` never whiffs. A whiffed primary effect still runs its extra
effects, which may whiff independently. A whiff still resets cooldown.

---

## 10. Goodwill, overflow and Market Share

### 10.1 applyPush

```
applyPush(attacker, defender, v, tick):
  if v >= defender.suppressThreshold:
    defender.lastSuppressTick = tick
  absorbed = min(v, defender.goodwill)
  defender.goodwill -= absorbed
  overflow = v - absorbed
  sp = 0
  if overflow > 0:
    sp = convertToShare(creditTo = the firm opposing defender, overflow)
  return (absorbed, overflow, sp)
```

Note that in the Anomaly self-cost case `attacker == defender`, and the overflow
credits the *opponent*: overflowing your own Goodwill with self-inflicted damage gives
the rival share. That is intentional.

### 10.2 applyMorale

```
applyMorale(defender, raw, creditTo):
  defender.cap = max(1, defender.cap - raw)
  defender.goodwill = min(defender.goodwill, defender.cap)
  sp = convertToShare(creditTo, floor(raw * MORALE_RATE_PERMILLE / 1000))
  return sp
```

Morale never touches `lastSuppressTick`. The cap floor of 1 keeps `suppressThreshold`
and regen arithmetic defined; `suppressThreshold` is computed from `capAtStart` and
does not shrink with the cap.

Legal Department Tier III: the cap contributions of its occupants are protected —
`defender.cap` may not fall below `Σ protected contributions`. Apply as a second floor
in the `max`.

### 10.3 convertToShare

```
convertToShare(creditTo, push):
  creditTo.spCarry += push * SP_PER_PUSH_PERMILLE[rules.round]
  sp = floor(creditTo.spCarry / 1000)
  creditTo.spCarry -= sp * 1000
  before = share
  if creditTo is A: share = min(SHARE_TOTAL, share + sp)
  else:             share = max(0, share - sp)
  return share - before          // signed from A's perspective; may be smaller than sp at the clamp
```

The carry accumulator means chip damage below one Share Point is never lost and never
rounded up; over a fight, conversion is exact to the permille.

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
applied = min(amount, firm.cap - firm.goodwill)
firm.goodwill += applied
emit regen entry (raw = amount, goodwillDelta = applied, tag "suppressed" if suppressed)
```

A regen entry is emitted every event, including when `applied` is 0. Views decide what
to show; the record is complete.

### 11.2 Morale from Burnout — `tick mod MORALE_INTERVAL == 0`

For each firm, A then B:

```
stacks = Σ unit.burnout over the firm's units
if stacks == 0: continue
raw = floor(MORALE_PER_STACK * stacks * PUSH_MULT[month(tick)] / 1000)
sp  = applyMorale(defender = firm, raw, creditTo = opponent)
emit morale entry (sourceSide = firm, targetSide = firm, raw, capDelta = -raw, shareDelta = sp, tag "burnout")
```

The firm burns itself. The opponent gets the share.

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
| Burnout | Push, Anomaly and Restore value × `(1000 − 50 × stacks)`; firm takes Morale each `MORALE_INTERVAL` | §9.2 step 5, §11.2 |
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
   4. For each modifier in `globals.modifiers` order: its banner effect, if any.

| Tick | Source | Effect |
| --- | --- | --- |
| 0 | Yakult Cart | Each adjacent unit: Overtime +1 |
| 0 | Modifier *Overtime Culture* | Each own unit: Burnout +1, then Overtime +1 |
| 0 | Rider *Bad Influence* | Each unit adjacent to the rider's unit: Burnout +1 |
| 400, 800 | Server Room below Tier III | Each occupant: Burnout +1 |
| 1160 | Rider *Contractual Obligation* | `applyMorale(own firm, 200 × PUSH_MULT[3] / 1000, creditTo = opponent)` — i.e. 600 raw at the Bell |

Banner effects at tick 0 run before any cooldown has advanced. Banner effects run
before expiry, so an Overtime granted at tick 0 expires in phase B of tick 60.

---

## 15. End of match

### 15.1 Phase F

```
if share >= SHARE_TOTAL: winner = A; ended = true
elif share <= 0:         winner = B; ended = true
```

Checked once per tick, after phase E. A tick in which both firms' pushes would each
have claimed the bar cannot occur: resolutions are sequential and `share` is clamped
after every conversion, so the first to reach the edge ends the match at that tick's
phase F. `endTick` is that tick.

### 15.2 Bell resolution

If the loop completes tick 1199 without ending:

```
if share > SHARE_START:         winner = A
elif share < SHARE_START:       winner = B
elif A.goodwill != B.goodwill:  winner = the firm with more goodwill
elif A.totalPush != B.totalPush: winner = the firm with more totalPush
else:                           winner = "draw"
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
  "kind": "push",                // push | morale | anomaly | restore | regen | status | retrigger | whiff | banner
  "sourceSide": "A",             // "A" | "B" | "*"
  "sourceUnit": 7,               // unitIndex, or -1 for firm-level, furniture and banners
  "sourceInstanceId": "e_7f3a",  // "" when sourceUnit is -1
  "sourceFloor": 2,              // FLOOR_INDEX, or -2 when not applicable
  "abilityId": "ability.ship_feature",   // or furniture/room/rider/modifier id, or "regen", "burnout", "banner"
  "targetSide": "B",             // "A" | "B" | "*"
  "targetUnits": [],             // unitIndex list; empty for firm-level
  "raw": 840,                    // the value after the pipeline, before absorption
  "goodwillDelta": -840,         // change to targetSide's goodwill (negative for damage; positive for restore/regen)
  "capDelta": 0,                 // change to targetSide's cap
  "shareDelta": 0,               // change to share, signed from A's perspective
  "overflow": 0,                 // push only: the part that went to share
  "stacks": 0,                   // status only: stacksApplied
  "depth": 0,                    // 0, or 1 when retriggered
  "month": 1,                    // month index at emission, 0..3
  "tags": []                     // sorted ascending; e.g. ["suppressed"], ["burnout"], ["overtime_expired"]
}
```

Per-kind conventions:

| Kind | source | target | raw | goodwillDelta | capDelta | shareDelta |
| --- | --- | --- | --- | --- | --- | --- |
| push | attacker unit | defender firm | v | −absorbed | 0 | sp |
| anomaly | attacker unit | defender firm | v | 0 | 0 | sp |
| anomaly self-cost | attacker unit | attacker firm | self | −absorbed | 0 | sp (to opponent) — emitted as a second entry tagged `self_cost` |
| morale (event) | firm (`sourceUnit` −1) | same firm | raw | 0 or negative if goodwill was clamped to the cap | −raw | sp |
| restore | caster unit | caster firm | v | +applied | 0 | 0 |
| regen | firm | same firm | amount | +applied | 0 | 0 |
| status | caster unit or furniture | each target unit — one entry per target | 0 | 0 | 0 | 0 |
| retrigger | retriggerer | target unit — one entry per target, emitted **before** the target's resolution entries | 0 | 0 | 0 | 0 |
| whiff | caster | `*` | 0 | 0 | 0 | 0 |
| banner | `*` | `*` | month index | 0 | 0 | 0 |

`goodwillDelta` on a Morale entry is negative only when `goodwill` exceeded the new
cap and was clamped; the clamped amount is recorded so the entry accounts for every
point of Goodwill that moved.

### 16.2 MatchResult

```jsonc
{
  "schemaVersion": 1,
  "seed": 3735928559,
  "round": 8,
  "rulesHash": "…",              // FNV-1a 32 of the RuleSet's canonical serialisation
  "snapshotHashA": "…",          // FNV-1a 32 of the snapshot's canonical serialisation
  "snapshotHashB": "…",
  "winner": "A",                 // "A" | "B" | "draw"
  "endTick": 1043,
  "finalShare": 10000,
  "finalGoodwill": { "A": 812, "B": 0 },
  "finalCap": { "A": 2100, "B": 1800 },
  "totalPush": { "A": 9310, "B": 4200 },
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
winner | endTick | finalShare | goodwillA | goodwillB | capA | capB | pushA | pushB | entryCount | rngState
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
32-bit throughout, and trivially identical across languages:

```
state = seed >>> 0

next():                                  // returns u32
  state = (state + 0x6D2B79F5) >>> 0
  t = state
  t = Math.imul(t ^ (t >>> 15), t | 1) >>> 0
  t = (t + Math.imul(t ^ (t >>> 7), t | 61)) >>> 0
  return (t ^ (t >>> 14)) >>> 0

draw(n):                                 // returns 0..n-1
  return floor(next() * n / 4294967296)  // computed exactly; in JS: Number(BigInt(next()) * BigInt(n) / 4294967296n)
```

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
4. **No ambient input.** No clock, no `Math.random`, no environment, no I/O, no
   locale-dependent string operations.
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
| `overflow` | A's single Architect at round 16 against an empty tower. Overflow and carry arithmetic |
| `regen_suppress` | QA Tester chip below threshold against a Recruiter. Regen never suppressed |
| `burnout_pierce` | Consultant against a Legal turtle. Morale erosion; Bell ordering |
| `retrigger_depth` | Team Lead adjacent to a Director adjacent to three units. Depth whiffs |
| `tie_parity` | Two identical towers with identical cooldowns. Alternating initiative |
| `random_selector` | A Paralegal with `random_floor` against a three-floor tower. RNG draw order |
| `anomaly_selfcost` | Salaryman Ghost in a Summoning Circle with and without Ofuda |
| `banner_order` | Yakult Cart, Overtime Culture and Bad Influence on the same tower at tick 0 |
| `boss_act2` | The Compliance Office against the intended Act 2 counter-build |

---

## 20. Worked trace

Round 1. Each side: one Junior Developer on 1F at column 2, row 1, in the corridor.
No rooms other than each side's empty Reception, no furniture, seed irrelevant (no
random selectors).

**Setup.** Both firms: `cap = 700`, `regenPerEvent = 21`, `suppressThreshold = 28`,
`goodwill = 700`. Units: A0 (`unitIndex` 0), B0 (`unitIndex` 1). `cdTotal = 80000`.
Value pipeline for Ship Feature: `60 → +0 → ×900 = 54 → ×1000 = 54 → ×1000 = 54 →
×1000 = 54 → ×1000 = 54`. Both deal **54**.

| Tick | Phase | Event | A goodwill | B goodwill | share |
| --- | --- | --- | --- | --- | --- |
| 0 | A | banner month 0 | 700 | 700 | 5000 |
| 40 | E | regen A: 21, applied 0. regen B: same | 700 | 700 | 5000 |
| 80 | D | ready: A0, B0. Even tick → A first. A0 pushes 54 → B 646, B suppressed at 80. B0 pushes 54 → A 646, A suppressed at 80 | 646 | 646 | 5000 |
| 80 | E | regen: `80 − 80 = 0 ≤ 20` → both suppressed, 0 | 646 | 646 | 5000 |
| 120 | E | `120 − 80 = 40 > 20` → +21 each | 667 | 667 | 5000 |
| 160 | D | odd? no — 160 is even → A first again. Both push 54 | 613 | 613 | 5000 |
| 160 | E | suppressed | 613 | 613 | 5000 |
| 200 | E | +21 | 634 | 634 | 5000 |
| 240 | D | both push 54 | 580 | 580 | 5000 |

The pattern continues: −54 every 80 ticks, +21 every 80 ticks when not suppressed (one
regen event in two), net −33 per 80 ticks. Goodwill at the end of Month 1 is
`700 − 4 × 54 + 4 × 21 = 568`.

At tick 400 the banner fires: `PUSH_MULT` 1400 makes each hit `floor(54 × 1400 / 1000)
= 75`, and `REGEN_MULT` 600 makes regen `floor(21 × 600 / 1000) = 12`. Month 2 has
five fires (400, 480, 560, 640, 720) and five unsuppressed regen events, so Goodwill at
the end of Month 2 is `568 − 375 + 60 = 253`.

Crunch makes each hit 108 and each regen 4. After the fires at 800 and 880 and the
regens at 840 and 920, both firms sit at 45. At tick 960, A0 fires first (even tick):
B absorbs 45, overflow 63, `63 × 8000 = 504000` carry → 504 SP, share 5504. Then B0
fires: A absorbs 45, overflow 63 → share back to 5000. Both firms are at 0 Goodwill,
every later fire overflows in full, and the symmetric pushes cancel. Nothing fires in
the Bell window (1120 is the last multiple of 80 below 1160). Bell resolution: share
5000, Goodwill 0 = 0, `totalPush` equal → **draw**, `endTick = 1199`.

Ticks 80, 160, 240 … are all even, so A always resolves first in this trace; the
parity rule only matters when the cooldowns are not multiples of 40 ticks. The
`tie_parity` fixture exercises it with 3.0 s cooldowns (60 ticks), where 60, 120, 180
alternate parity.

**Entry count** for this fixture: 4 banner entries (ticks 0, 400, 800, 1160); 29 regen
events (ticks 40 … 1160) × 2 firms = 58 regen entries; 14 fires (ticks 80 … 1120) × 2
units = 28 push entries. **90 entries**, `seq` 0–89. An implementation producing a
different count has a phase-order bug. Expected `finalShare` 5000, `finalGoodwill`
`{A: 0, B: 0}`, `finalCap` `{A: 700, B: 700}`, `totalPush` `{A: 1131, B: 1131}`.
