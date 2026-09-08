# Company Wars — Open Questions Register

Status: **living document**. Phase 1 output. This is the register of every design
decision that was open after `DESIGN_BRIEF.md`, each with the options considered, a
recommendation, the trade-off being accepted, and what the answer blocks.

A question here is never deleted. When it closes it gets a `DECIDED` status and a
pointer into `DECISION_LOG.md`. When a later phase reopens one, it comes back here
with a new entry rather than an edit in place, so the history of a reversal is
visible.

Read `DESIGN_BRIEF.md` first. Nothing in this document relitigates the locked
foundation; where a recommendation puts pressure on a locked decision it says so
explicitly under **Tension with the lock**.

---

## How to read an entry

| Field | Meaning |
| --- | --- |
| **Blocks** | Which deliverables or later questions cannot be finished until this closes |
| **Options** | The genuinely different answers, not variations on one |
| **Recommendation** | The answer to adopt absent a reason not to |
| **Trade-off** | What the recommendation gives up. Every recommendation gives something up |
| **Status** | `DECIDED` (craft call, taken, see decision log) or `NEEDS SIGN-OFF` (the human's call) |

**Status vocabulary.** `DECIDED` means the call has been taken — either as a matter
of craft under the planning prompt's grant of authority, or by the human where the
call was theirs. The decision log records which. It is still reversible; it is not
still pending. `NEEDS SIGN-OFF` means the call is about tone, feel, scope or what
the game is *about*, and is the human's to make. Later phases may proceed on the
recommendation for `NEEDS SIGN-OFF` items, but must mark anything built on one.

**ID scheme.** `Q-<AREA>-<n>`. Areas: `STR` structure and pacing, `GW` Goodwill and
the tug-of-war, `FLR` floors, `LYR` layers, `GBX` greybox and assets, `RCP`
recipes, `PTL` the portal, `PVP` async PvP seams, `RISK` risks that are also
decisions.

---

## Summary

| ID | Question | Recommendation in one line | Status |
| --- | --- | --- | --- |
| [Q-STR-1](#q-str-1--how-many-floors-and-what-grid-size) | Floors and grid size | 5 floor slots, 5x3 standard, start with 2 | DECIDED · [D-01](DECISION_LOG.md#d-01) |
| [Q-STR-2](#q-str-2--what-is-the-floor-expansion-curve) | Expansion curve | Floors are the biggest purchase in the game; three unlocks per run | DECIDED · [D-02](DECISION_LOG.md#d-02) |
| [Q-STR-3](#q-str-3--rounds-per-run-fight-length-and-lives) | Rounds, fight length, lives | 16 fights, 60s quarter, 3 lives, ~45min run | DECIDED · [D-21](DECISION_LOG.md#d-21) |
| [Q-STR-4](#q-str-4--what-shape-is-the-campaign-map-and-what-are-its-bosses) | Campaign map and bosses | 3 acts, branching, 5 node types, 3 named bosses | DECIDED · [D-22](DECISION_LOG.md#d-22) |
| [Q-GW-1](#q-gw-1--how-does-floor-output-aggregate-into-goodwill-damage-and-then-into-the-bar) | Output aggregation | Per-ability resolution tagged by floor; no separate floor cadence | DECIDED · [D-03](DECISION_LOG.md#d-03) |
| [Q-GW-2](#q-gw-2--does-goodwill-regenerate) | Regeneration | Yes — discrete 2s ticks, suppressed 1s after any hit | DECIDED · [D-04](DECISION_LOG.md#d-04) |
| [Q-GW-3](#q-gw-3--what-is-the-shape-of-the-quarter-close-pressure-curve) | Quarter Close curve | Three months plus a Bell; push up, regen down, stepped | DECIDED · [D-05](DECISION_LOG.md#d-05) |
| [Q-GW-4](#q-gw-4--does-overflow-carry) | Overflow | Yes, in full, uncapped | DECIDED · [D-06](DECISION_LOG.md#d-06) |
| [Q-GW-5](#q-gw-5--what-pierces-goodwill) | Piercing | Two sources only: Burnout (morale) and portal Anomaly | DECIDED · [D-07](DECISION_LOG.md#d-07) |
| [Q-GW-6](#q-gw-6--does-the-bar-travel-back-through-the-centre) | Comebacks | Yes, free travel, no ratchet | DECIDED · [D-23](DECISION_LOG.md#d-23) |
| [Q-GW-7](#q-gw-7--what-does-the-ledger-show-and-at-what-granularity) | Ledger granularity | Full fidelity in the log, coalesced in the live view, budget of 4 lines/sec | DECIDED · [D-08](DECISION_LOG.md#d-08) |
| [Q-FLR-1](#q-flr-1--what-makes-each-floor-mechanically-distinct) | Floor identity | Four levers: multiplier, room legality, exposure, upkeep | DECIDED · [D-09](DECISION_LOG.md#d-09) |
| [Q-FLR-2](#q-flr-2--how-do-floor-targeting-abilities-work) | Floor targeting | Closed selector vocabulary, pure over the snapshot, explicit tie-breaks | DECIDED · [D-10](DECISION_LOG.md#d-10) |
| [Q-FLR-3](#q-flr-3--does-the-elevator-do-anything) | The elevator | Yes — landing column gives vertical adjacency | DECIDED · [D-11](DECISION_LOG.md#d-11) |
| [Q-LYR-1](#q-lyr-1--is-furniture-a-distinct-layer-in-v1) | Furniture layer | Keep furniture, cut equipment | DECIDED · [D-12](DECISION_LOG.md#d-12) |
| [Q-LYR-2](#q-lyr-2--what-can-an-employee-carry) | Employee carry | Nothing in v1; schema keeps an empty `attachments[]` | DECIDED · [D-13](DECISION_LOG.md#d-13) |
| [Q-LYR-3](#q-lyr-3--does-furniture-earn-its-tile) | Furniture on trial | Keep it through the vertical slice; fold into rooms if the named test fails | OPEN — playtest gate |
| [Q-ECO-1](#q-eco-1--how-is-the-reward-for-a-correct-commitment-made-impactful) | Reward shape | Tenure — rooms compound while they stay put and stay staffed | DECIDED · [D-25](DECISION_LOG.md#d-25) |
| [Q-ECO-2](#q-eco-2--does-a-run-need-a-mid-run-recovery-valve) | Recovery valve | Paid relocation is the valve; no free Restructuring | DECIDED · [D-52](DECISION_LOG.md#d-52) |
| [Q-GBX-1](#q-gbx-1--what-is-the-minimum-a-manifest-entry-needs-before-a-greybox-can-be-built) | Manifest minimum | Nine required fields; overhang is derived, never authored | DECIDED · [D-14](DECISION_LOG.md#d-14) |
| [Q-GBX-2](#q-gbx-2--what-is-the-draw-order-rule-for-overhanging-sprites) | Draw order | y-sort plus explicit `sortBias`; five-key total order | DECIDED · [D-15](DECISION_LOG.md#d-15) |
| [Q-GBX-3](#q-gbx-3--what-is-the-greybox-palette) | Greybox palette | Six pack-sampled hues, desaturated, category-coded, one source file | NEEDS SIGN-OFF |
| [Q-GBX-4](#q-gbx-4--what-defines-art-complete) | Art complete | Every entry reachable in a normal run has passing art. No tier exemptions | DECIDED · [D-16](DECISION_LOG.md#d-16) |
| [Q-GBX-5](#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions) | Pack-fit of decided dimensions | Verify 96 × 32 floor segments and 64 × 64 portraits against the packs before any greybox | OPEN — Phase 4 action |
| [Q-RCP-1](#q-rcp-1--how-many-recipes-at-launch-and-how-are-they-discovered) | Recipe count and discovery | ~40 in three classes; codex from run 1 with near-miss feedback | NEEDS SIGN-OFF |
| [Q-RCP-2](#q-rcp-2--do-recipes-consume-inputs-and-can-they-be-undone) | Consumption and undo | Consume; reversible only inside the same build round | DECIDED · [D-17](DECISION_LOG.md#d-17) |
| [Q-PTL-1](#q-ptl-1--what-makes-an-extraplanar-hire-a-real-gamble) | Portal risk | Visible per-hire rider plus a Goodwill tax; Anomaly meter as a second layer | NEEDS SIGN-OFF |
| [Q-PTL-2](#q-ptl-2--what-unlocks-the-portal-and-how-does-the-reveal-land) | Portal unlock | Post Act-1 boss in campaign, fixed round in ranked; hints from round 1 | NEEDS SIGN-OFF |
| [Q-PVP-1](#q-pvp-1--what-is-stored-in-a-tower-snapshot) | Snapshot contents | Layout, definitions, run modifiers. No budget, no RNG, no cosmetics | DECIDED · [D-18](DECISION_LOG.md#d-18) |
| [Q-PVP-2](#q-pvp-2--how-are-players-matched-once-ranked-exists) | Matchmaking | Bucketed by round and rating; ghost chosen from the match seed | DECIDED · [D-19](DECISION_LOG.md#d-19) |
| [Q-PVP-3](#q-pvp-3--what-is-the-anti-cheat-posture-given-the-client-owns-the-sim) | Anti-cheat | Server re-simulation of submitted snapshots; client result advisory | DECIDED · [D-20](DECISION_LOG.md#d-20) |
| [Q-ARCH-1](#q-arch-1--when-does-controller-navigation-arrive) | Controller navigation | Post-v1; mouse and keyboard ship first; Steam Deck verification waits on it | DECIDED · [D-47](DECISION_LOG.md#d-47) |
| [Q-UX-1](#q-ux-1--when-does-the-ux-review-happen) | UX review timing | Wireframes deferred; the greybox vertical slice is the UX milestone | DECIDED · [D-48](DECISION_LOG.md#d-48) |
| [Q-RISK-1](#q-risk-1--is-the-guttykreum-licence-cleared-for-commercial-release) | Asset licence | Verify before any spend; treat as a release blocker with an owner and a date | NEEDS SIGN-OFF |
| [Q-RISK-2](#q-risk-2--is-the-room-commitment-tension-actually-load-bearing) | Room commitment | Demolition is genuinely painful; rewards for good commitment scale to compensate | DECIDED · [D-24](DECISION_LOG.md#d-24) |

---

## Structure and pacing

### Q-STR-1 · How many floors, and what grid size?

**Blocks:** `GAME_DESIGN` (build phase), `CONTENT_SCHEMA` (floor and room schemas),
`ART_PIPELINE` (every footprint), `BALANCE_PLAN` (roster size drives every curve).

The binding constraint is not space, it is **legibility**. Every employee on the
board fires on a cooldown and writes to the ledger. Floors x grid area x fill rate
sets the ledger's event rate, and the ledger is the feature the whole design rests
on. Size the building backwards from "a human can follow this", not forwards from
"a floor should feel roomy".

**Options**

- **A — Few large floors.** 3 floors of 8x5. Familiar bag-like packing per floor,
  vertical axis nearly decorative. ~120 tiles.
- **B — Many small floors.** 6 floors of 4x3. Vertical identity is strong, each
  floor is a tiny puzzle, but no single floor is interesting to arrange. ~72 tiles.
- **C — Middle, asymmetric.** 5 floor slots of differing size and shape: standard
  floors 5x3, a small high-multiplier Executive floor, a small basement. ~65 tiles.

**Recommendation: C.** Five slots — `G` (Reception), `1F`, `2F`, `3F` (Executive),
`B1` (Portal). Standard floors are **5 wide x 3 deep = 15 tiles**. Executive is
**4x2 = 8**. B1 is **3x3 = 9**. Maximum 62 tiles, of which rooms consume roughly
half, giving a late-run roster of **12–16 employees**. Start a run with `G` and
`1F` only.

Asymmetric sizes do the work that a stat line cannot: a floor that is a different
*shape* forces a different arrangement, which is the cheapest possible source of
per-floor identity and costs nothing to balance.

**Trade-off:** 15 tiles is small enough that a single 2x3 room dominates a floor.
That is intentional — it makes room choice a commitment — but it means the room
catalogue must be mostly 2x2 and smaller, and 3x3 rooms cannot exist on standard
floors at all.

**Status:** DECIDED · [D-01](DECISION_LOG.md#d-01)

---

### Q-STR-2 · What is the floor expansion curve?

**Blocks:** `GAME_DESIGN` (economy), `BALANCE_PLAN` (power curve per round).

**Options**

- **A — Steady drip.** A floor unlocks every few rounds automatically. Predictable,
  removes the decision.
- **B — Purchasable, cheap.** Floors cost budget; everyone buys them as soon as
  they appear. Effectively A with extra clicks.
- **C — Purchasable, brutal.** A floor costs roughly three rounds of income. Buying
  one means fielding a weaker tower for two or three fights in exchange for a
  higher ceiling. Skipping a floor is a legitimate strategy.

**Recommendation: C.** A new floor is the single most expensive thing in the game
and is the game's only real tempo-versus-scaling decision. Three floor unlocks are
purchasable per run (`2F`, `3F`, `B1`); `B1` is additionally gated by the portal
(see [Q-PTL-2](#q-ptl-2--what-unlocks-the-portal-and-how-does-the-reveal-land)).
Prices escalate; a wide-and-shallow tower and a tall-and-tall tower should both be
viable, and `BALANCE_PLAN` must assert that they are.

Fiction does real work here: you are **signing a lease**. It comes with per-round
upkeep, so an over-extended tower bleeds budget it cannot spend on staff. Upkeep is
what stops "buy every floor" from being strictly correct.

**Trade-off:** Upkeep is a second economy knob and a second failure mode (a player
who over-leases and cannot recover). Mitigate with a visible projected-upkeep
readout at purchase time, not with a softer number.

**Status:** DECIDED · [D-02](DECISION_LOG.md#d-02)

---

### Q-STR-3 · Rounds per run, fight length, and lives

**Blocks:** `GAME_DESIGN` (pacing), `SIMULATION_SPEC` (quarter length in ticks),
`BALANCE_PLAN` (per-round power targets), `ROADMAP` (vertical slice scope).

This one is feel, so it is the human's call. The numbers below are internally
consistent; changing one requires changing the others.

**Options**

- **A — Short and sharp.** 10 fights, 45s quarter, 2 lives. ~25 minute run. Closest
  to Backpack Battles. Fewest rounds to balance. Least room for a build to develop
  across three floors.
- **B — Standard.** 16 fights across 3 acts, 60s quarter, 3 lives. ~45 minute run.
  Enough rounds for a tower to be built, broken by a bad matchup, and rebuilt.
- **C — Long campaign.** 24 fights, 60s quarter, 4 lives. ~70 minute run. Room for
  real narrative escalation; risks a losing run being a 40 minute funeral.

**Recommendation: B.** 16 fights, acts of 6 / 6 / 4. Quarter length **60 seconds =
1,200 ticks at 20 Hz**. Three lives, framed as **strikes on your performance
review**. Build phase is untimed; typical is 60–120s.

A 45 minute run is the right size for the depth on offer: long enough that the
room-commitment tension has consequences, short enough that a run lost to a bad
Act 1 does not cost an evening. 60s is the ceiling, not the target — the Quarter
Close curve ([Q-GW-3](#q-gw-3--what-is-the-shape-of-the-quarter-close-pressure-curve))
should resolve the median fight at **35–50s**, with the bell as a backstop rather
than a routine outcome. Playback speed (1x / 2x / 4x / skip-to-result) is a render
concern only and never touches the sim.

**Trade-off:** 16 rounds x 60s is roughly 16 minutes of watching per run. If the
fight is not readable, that is 16 minutes of noise, and the run length amplifies
every readability failure rather than hiding it.

**Status:** DECIDED · [D-21](DECISION_LOG.md#d-21) — recommendation accepted.

---

### Q-STR-4 · What shape is the campaign map, and what are its bosses?

**Blocks:** `GAME_DESIGN` (campaign mode), `CONTENT_SCHEMA` (encounter schema),
`ROADMAP` (content volume per act).

**Options**

- **A — Linear with choices at nodes.** Simple, cheap, no routing decisions.
- **B — Slay the Spire branching.** Layered DAG, visible one act ahead, node types
  telegraphed, boss fixed per act. Proven, and the brief already names it.
- **C — Open district map.** Wander a city, pick targets. Expensive, hard to pace,
  hard to balance — the round number stops being a reliable difficulty proxy, which
  breaks the automated matchup harness.

**Recommendation: B**, with five node types:

| Node | Fiction | Function |
| --- | --- | --- |
| **Hostile Takeover** | A rival firm | Ordinary fight against a scripted rival tower |
| **Recruiter** | A hiring fair | Shop-only node, better stock, no fight |
| **Board Meeting** | A choice with consequences | Event node; run modifiers, risk/reward |
| **Consultant** | Expensive advice | Reveals one undiscovered recipe, or upgrades a room |
| **Audit** | Elite fight | Harder rival, guaranteed rare reward |

Three bosses, one per act, each attacking a different assumption rather than just
having bigger numbers:

1. **Act 1 — The Regional Rival.** A mirror of a competent generalist tower.
   Teaches that the ledger is readable. Its defeat opens the basement.
2. **Act 2 — The Compliance Office.** A Legal turtle with very high Goodwill regen.
   Unwinnable without piercing damage; the fight that teaches Burnout exists.
3. **Act 3 — The Parent Company.** Occupies five floors, uses floor-targeting
   heavily, and punishes concentrated towers. The run's thesis statement.

**Trade-off:** Three scripted bosses is three hand-authored towers that must also
survive every balance change, since they are fixtures as well as content. Budget
for re-authoring them each time the pressure curve moves.

**Status:** DECIDED · [D-22](DECISION_LOG.md#d-22) — recommendation accepted.

---

## Goodwill and the tug-of-war

### Q-GW-1 · How does floor output aggregate into Goodwill damage, and then into the bar?

**Blocks:** `SIMULATION_SPEC` (the whole resolution pipeline), `GAME_DESIGN`
(floor identity), the ledger format, and therefore the replay format.

**Options**

- **A — Flat.** Every ability hits the enemy Goodwill directly. Floors are pure
  organisation with no mechanical weight. Simplest, and wastes the building.
- **B — Two-stage with a floor cadence.** Employees accumulate output into a floor
  buffer; each floor emits a Push packet on its own timer. Gives floors real
  identity and makes the per-floor window bursts trivially easy to present — but
  it destroys causality in the ledger. "Floor 3 dealt 900" is not an answer to
  "why did I lose".
- **C — Per-ability resolution, floor-tagged.** Each ability resolves immediately
  and individually. Floor multipliers and room auras are applied at resolve time.
  Every resolution carries `(floor, employee, ability)`. Presentation aggregates by
  floor for the window bursts; the ledger does not.

**Recommendation: C.** The resolution formula:

```
push = base(ability)
     * roomAura(employee.tile)      // multiplicative, from the room the employee occupies
     * furnitureTrigger(...)         // additive bonuses from adjacent furniture
     * floorMultiplier(floor)        // floor identity
     * statusModifiers(employee)     // Overtime, Bureaucracy, Burnout
     * monthMultiplier(tick)         // Quarter Close pressure curve
```

Applied in that order, with a single rounding step at the end (round-half-up on an
integer, no floats reaching the ledger). Aggregation for presentation is a *view*
over the same event stream, computed by the renderer, never by the sim.

This is the answer that keeps the ledger's promise. Floor identity survives because
`floorMultiplier` is real; battle presentation survives because a per-floor sum is
one `groupBy` away; and the post-fight autopsy survives because nothing was ever
summed away.

**Trade-off:** The sim emits more events than a bucketed design would — roughly
6–12 per second in a full late-run fight. That is a live-view presentation problem
(handled in [Q-GW-7](#q-gw-7--what-does-the-ledger-show-and-at-what-granularity)),
not a simulation cost, and it is the right place to pay.

**Status:** DECIDED · [D-03](DECISION_LOG.md#d-03)

---

### Q-GW-2 · Does Goodwill regenerate?

**Blocks:** `SIMULATION_SPEC`, `BALANCE_PLAN` invariant 1, the existence of the
turtle archetype at all.

The brief states the intended answer is yes-with-escalation, and names the trap:
regeneration plus a flat attack curve is an unbreakable build. The real question is
the *shape*, and specifically whether regeneration is continuous or discrete.

**Options**

- **A — None.** Defence is pure delay. Turtles have no identity. Rejected by the
  brief and correctly so.
- **B — Continuous flat regen.** `goodwill += rate * dt`. Simple, but binary: either
  regen exceeds incoming DPS and the tower is immortal, or it does not and regen is
  irrelevant. There is no interesting middle band, and worse, it is *invisible* —
  a number drifting upward is not drama.
- **C — Continuous with post-hit suppression.** Regen pauses for a window after any
  Push lands. Creates a genuine skill expression for attackers (sustained pressure
  beats bursty pressure at denying regen) and gives defenders a real counter
  (survive the burst, recover in the gap). Still visually a drifting number.
- **D — Discrete ticks with post-hit suppression.** Regen resolves on a fixed
  cadence as a **single named ledger entry**, suppressed if the side took damage
  recently.

**Recommendation: D.** Regeneration resolves every **40 ticks (2s)** as one ledger
line — `+320  Retainer Renewed  HR · Fl.2` — and is **suppressed if that side took
any Push in the preceding 20 ticks (1s)**. Amount is the sum of the tower's regen
sources, scaled by the current month's regen multiplier
([Q-GW-3](#q-gw-3--what-is-the-shape-of-the-quarter-close-pressure-curve)).
Goodwill is capped; excess regen is discarded, not banked.

D is C with the presentation problem solved. The brief invented the ledger to make
defensive play look like something is happening; a discrete, named, positive entry
every two seconds *is* that something. It also makes the regen model trivially
assertable in CI, because regen events are discrete and countable rather than an
integral over a curve.

**Trade-off:** A 2s cadence means regen timing can be gamed — an attacker who lands
one cheap hit every 1.9s denies regen entirely for a fraction of the cost of real
pressure. This is a genuine exploit surface. Guard it in `BALANCE_PLAN` with a
minimum-Push threshold below which a hit does not suppress, and assert that no
single low-cost ability can sustain suppression alone.

**Status:** DECIDED · [D-04](DECISION_LOG.md#d-04)

---

### Q-GW-3 · What is the shape of the Quarter Close pressure curve?

**Blocks:** `SIMULATION_SPEC`, `BALANCE_PLAN` invariant 1 (which is *only*
satisfiable by this curve), fight length, and therefore run length.

The curve's job is a guarantee, not a feel: **no defensive build, however
optimised, survives to the bell.** That must hold against the strongest possible
tower at each round, not the average one.

**Options**

- **A — Linear ramp.** Push multiplier rises smoothly 1.0 → 2.0 across the quarter.
  Smooth, but has no moment, and a smooth curve is hard to read on screen.
- **B — Stepped months.** Three named steps with a banner at each transition. The
  fight gains three acts. Legible, satirical, and a step function is far easier to
  reason about and assert than a ramp.
- **C — Regen decay only.** Leave push flat, decay regen to zero. Guarantees
  breakthrough eventually, but produces slow, undramatic endings.
- **D — Sudden death.** Flat until a hard cut-off, then enormous multipliers.
  Dramatic, but makes the first 80% of the fight not matter.

**Recommendation: B, applied to both push and regen.** One curve, two effects:

| Phase | Ticks | Push x | Regen x | Ledger banner |
| --- | --- | --- | --- | --- |
| **Month 1** | 0–399 | 1.0 | 1.0 | `— Q OPEN —` |
| **Month 2** | 400–799 | 1.4 | 0.6 | `— MONTH 2 —` |
| **Month 3 (Crunch)** | 800–1159 | 2.0 | 0.2 | `— CRUNCH —` |
| **The Bell** | 1160–1199 | 3.0 | 0.0 | `— QUARTER CLOSE —` |

The multipliers are a **global constant, not per-content data.** No card, room or
modifier may alter them. This is the single most important balance decision in the
document: the pressure curve is the fixed frame everything else is tuned against,
and the moment it becomes tunable per-content the balance space stops being
searchable.

**Trade-off:** A step function means power spikes at three moments, and a build
whose combo comes online at tick 795 feels cheated by five ticks. Accept it —
telegraph the transition a second early with an audio/visual cue so the spike is
anticipated rather than surprising.

**Tension with the lock:** none. This is the shape the brief asked for.

**Status:** DECIDED · [D-05](DECISION_LOG.md#d-05)

---

### Q-GW-4 · Does overflow carry?

**Blocks:** `SIMULATION_SPEC`, burst archetype viability.

**Options**

- **A — Full carry, uncapped.** A hit larger than remaining Goodwill puts its excess
  straight into the bar. Rewards burst and alpha-strike timing exactly as intended.
- **B — Full carry, capped per hit.** Prevents one enormous strike from claiming the
  bar outright.
- **C — Reduced-efficiency carry.** Overflow converts at, say, 60%. A hidden tax.
- **D — Discarded.** Excess is lost. Quietly buffs chip damage and punishes exactly
  the play pattern the design wants to reward.

**Recommendation: A.** Confirm the brief. Full carry, no cap, no efficiency loss.

The argument for A over B and C is the ledger. A capped or taxed overflow produces
a ledger line whose number does not match what the player saw happen, and the
ledger's entire value is that it is arithmetic the player can check. `-1,200 Cease
& Desist` must mean 1,200. If burst turns out to be dominant, fix it in cooldowns
and costs — visible, checkable numbers — not in a hidden conversion rule.

**Trade-off:** Uncapped overflow means a sufficiently large single hit can end a
fight from near-parity, which will occasionally feel unfair. That is the price of an
honest ledger, and it is worth it. `BALANCE_PLAN` carries the guard: assert that no
single ability at any round can deliver more than a stated fraction of full bar
capacity in one resolution.

**Status:** DECIDED · [D-06](DECISION_LOG.md#d-06)

---

### Q-GW-5 · What pierces Goodwill?

**Blocks:** `SIMULATION_SPEC` (damage type model), `CONTENT_SCHEMA` (damage kinds),
the counter-triangle, the dead-air risk.

**Options**

- **A — One source (Burnout).** Maximum clarity, minimum design space. Turtles have
  exactly one counter, which makes drafting into it feel deterministic.
- **B — Two sources (Burnout, portal Anomaly).** Clean to learn, and the second
  source arrives mid-run as a reward, so the opening rounds stay simple.
- **C — Pierce as a per-ability flag.** Any card may be marked piercing. Maximum
  flexibility, and it dissolves the concept — if a third of the catalogue pierces,
  Goodwill stops being a buffer and becomes a rounding error.

**Recommendation: B**, and make pierce a property of the **damage kind**, not of the
ability, so it cannot proliferate:

| Kind | Behaviour | Source |
| --- | --- | --- |
| **Push** | Depletes Goodwill; overflow carries to the bar | Default for everything |
| **Morale** | Bypasses Goodwill entirely, moves the bar directly at a reduced rate, and lowers the target's Goodwill *cap* and regen | Burnout — the stacking DoT |
| **Anomaly** | Bypasses Goodwill at full rate, but the attacker takes a fraction of it as self-inflicted Goodwill loss | Portal / extraplanar staff only |

Three kinds, two of which pierce, and each pierce carries a real cost — Morale is
slow, Anomaly hurts you back. That is enough to break a turtle and not enough to
make Goodwill pointless.

Morale also solves the **dead-air risk** directly: because Morale moves the bar from
tick 1 regardless of Goodwill, a fight is never visually static in its opening
seconds provided at least one side fields any Burnout source. `BALANCE_PLAN` should
assert bar movement within a stated time in the median matchup.

**Trade-off:** Damage kinds mean three numbers on every card instead of one, and
three colours in the ledger. That is real UI cost. It is cheaper than the
alternative, which is a Goodwill mechanic that either cannot be broken or does not
matter.

**Status:** DECIDED · [D-07](DECISION_LOG.md#d-07)

---

### Q-GW-6 · Does the bar travel back through the centre?

**Blocks:** `SIMULATION_SPEC` (bar model), whether comebacks exist, how the last
15 seconds of a losing fight feel.

**Options**

- **A — Free travel.** The bar is one position. A trailing player who breaks through
  pushes it back through the centre and can win from anywhere. Comebacks fully
  exist. Ground taken is never safe.
- **B — Ratchet.** Territory claimed is kept; the contest is only over the
  remainder. Leads are meaningful and a won fight stays won — but a player who falls
  behind at 20s watches a foregone conclusion for 40s. That is the worst possible
  outcome for a game whose fight is not interactive.
- **C — Recapture friction.** Free travel, but pushing into ground the opponent
  holds costs a multiplier (say 1.25x). Comebacks exist but cost more than the
  original push did. Leads have weight without being safe.

**Recommendation: A**, with **C held as the tuning lever** if leads prove
meaningless in playtest.

The reasoning is that the fight is a spectator event. The player cannot intervene,
so the only thing sustaining attention is uncertainty. B destroys uncertainty
precisely when the player most needs a reason to keep watching, and it does so in
the fights they lose — the ones where they most need to be watching the ledger to
learn why. A also keeps the model arithmetically trivial, which matters for the
replay format and for CI assertions.

Friction (C) is deliberately deferred rather than rejected: it is a single
multiplier that can be introduced without changing any content, so it costs nothing
to leave on the shelf.

**Trade-off:** Under A, a dominant 50-second performance can be erased in the Bell
window, where the push multiplier is 3.0. That is a real feel-bad. Watch it in
playtest; if it bites, the fix is to cap Bell-window multipliers rather than to
adopt the ratchet.

**Status:** DECIDED · [D-23](DECISION_LOG.md#d-23) — recommendation accepted. Comebacks
exist; recapture friction stays on the shelf as the tuning lever.

---

### Q-GW-7 · What does the ledger show, and at what granularity?

**Blocks:** `SIMULATION_SPEC` (event and replay format), `GAME_DESIGN` (battle UI),
`ART_PIPELINE` (the ledger panel is a manifest entry with fixed dimensions), and the
quality bar's central promise.

Under [Q-GW-1](#q-gw-1--how-does-floor-output-aggregate-into-goodwill-damage-and-then-into-the-bar)
the sim emits 6–12 events per second per side in a late-run fight. A raw feed at
that rate is unreadable. A pre-aggregated feed is unusable for diagnosis. The
answer must be both.

**Recommendation: separate the record from the view.**

**The record.** Every resolution is logged at full fidelity, in tick order, and is
the replay. One entry:

```jsonc
{
  "tick": 412,
  "side": "A",                  // who this entry belongs to (the ledger is per-firm)
  "kind": "push",               // push | morale | anomaly | regen | status | banner
  "sourceFloor": 3,
  "sourceId": "emp_7f3a",       // instance id, resolvable to a name in the snapshot
  "abilityId": "ability.ship_feature",
  "targetSide": "B",
  "raw": 840,                   // before mitigation, after all multipliers
  "goodwillDelta": -840,        // what actually happened to the buffer
  "shareDelta": 0,              // SP moved, if any
  "overflow": 0,
  "tags": ["engineering", "crit"]
}
```

The ledger, the per-floor contribution breakdown, the scrub timeline and the replay
are all **views over this one array**. There is no second format, and the
post-battle autopsy is not a separate feature — it is this list with a filter on it.
That is the brief's "design it as one component, not two", taken literally.

**The live view.** Bottom-up scrolling, six lines visible, with three rules:

1. **Coalescing.** Entries sharing `(sourceId, abilityId, kind)` within a **1.0s
   window** merge into one line with a `x N` badge and a summed value.
2. **Rate budget.** At most **4 new lines per second**. Overflow beyond that
   collapses into a single dimmed roll-up line (`+3 more · Fl.2`) that expands
   normally in the scrub view. The budget is a hard constraint on content design,
   not just on UI: if a build routinely exceeds it, that build is illegible and
   `BALANCE_PLAN` should flag it.
3. **Emphasis.** Lines are colour-coded by `kind` and weighted by magnitude relative
   to current Goodwill, so the hit that actually mattered is the one that catches
   the eye. Banners (`— CRUNCH —`) are full-width and break the scroll.

**The scrub view.** Same array, no coalescing, with a timeline scrubber, filters by
floor / employee / kind, and a per-floor contribution bar chart derived by
`groupBy(sourceFloor)`. Available immediately post-fight and from the run history.

**Trade-off:** The 4-lines-per-second budget will occasionally hide something the
player wanted to see live. That is acceptable because nothing is lost — it is in the
scrub view a keypress away. The alternative, an uncapped feed, loses everything by
showing everything.

**Status:** DECIDED · [D-08](DECISION_LOG.md#d-08)

---

## Floors

### Q-FLR-1 · What makes each floor mechanically distinct?

**Blocks:** `GAME_DESIGN` (floors section), `CONTENT_SCHEMA` (room legality tags),
`BALANCE_PLAN` (per-floor win-rate contribution).

"More space" is not identity. Four levers are available, and the recommendation is
to use all four lightly rather than one heavily, because a single strong lever
collapses into one correct stacking pattern.

**The four levers:** output multiplier · room legality · exposure to floor-targeting
· per-round upkeep.

**Recommendation**

| Floor | Grid | Output x | Room legality | Exposure | Upkeep |
| --- | --- | --- | --- | --- | --- |
| **G — Reception** | 5x3 | 0.90 | Reception, Security, Lobby | Absorbs all `lowest_floor` targeting | Lowest |
| **1F — Operations** | 5x3 | 1.00 | General | Normal | Low |
| **2F — Operations** | 5x3 | 1.15 | General | Normal | Medium |
| **3F — Executive** | 4x2 | 1.45 | Executive, General | Absorbs all `highest_floor` targeting | High |
| **B1 — Portal** | 3x3 | 1.00 | Extraplanar only | Cannot be targeted by floor selectors | High, paid in Goodwill |

Reception additionally grants a **flat Goodwill bonus** proportional to its occupied
tiles — front-of-house is where reputation lives — which gives the weakest-output
floor a reason to be staffed rather than left empty.

The anti-collapse argument: Executive has the best multiplier, the smallest grid,
the highest upkeep, *and* eats every `highest_floor` ability in the game. Stacking
it is a legible, punishable choice rather than a free optimum. B1 is
targeting-immune but cannot host normal staff and costs Goodwill regen, so it is a
specialist floor, not a safe one.

**Trade-off:** Room legality tags mean the room catalogue must be authored per-floor
and the shop must filter by what the player owns, which is real content and UI
work. Without it, floors differ only by a number, and a number alone will collapse.

**Status:** DECIDED · [D-09](DECISION_LOG.md#d-09)

---

### Q-FLR-2 · How do floor-targeting abilities work?

**Blocks:** `SIMULATION_SPEC` (targeting and determinism), `CONTENT_SCHEMA`
(ability schema).

**Recommendation: a closed selector vocabulary.** Abilities may not express
arbitrary targeting; they name one selector from a fixed list. Each selector is a
**pure function of the opponent's snapshot at resolve time** — no memory, no state,
no randomness outside the seeded generator.

| Selector | Resolves to |
| --- | --- |
| `highest_occupied_floor` | Greatest floor index with ≥1 employee |
| `lowest_occupied_floor` | Least floor index with ≥1 employee |
| `most_populated_floor` | Floor with the most employees |
| `least_populated_floor` | Floor with the fewest employees, ≥1 |
| `same_floor_index` | The mirror of the source's own floor, if occupied |
| `random_floor` | Seeded draw from occupied floors |
| `all_floors` | Every occupied floor, value split evenly, remainder to the lowest index |

**Tie-breaks are part of the specification, not the implementation.** Every selector
resolves ties by: fewest employees → lowest floor index → lowest instance id. B1 is
excluded from all selectors except `all_floors`. A selector that finds no legal
target produces a `whiff` ledger entry rather than being skipped silently, because
a player who cannot see an ability *fail* will read it as a bug.

**Does floor assignment stay a real decision?** Only if the selectors above are
common enough in the rival pool to punish concentration. That is a content-density
requirement, and it belongs in `BALANCE_PLAN` as an assertion: at every round, some
stated fraction of the rival pool must field at least one floor selector. Without
that, concentration wins and the vertical axis dies.

**Trade-off:** A closed vocabulary means some card idea will eventually not be
expressible. Adding a selector is a code change, deliberately — it is a rules
change, and rules changes should not be authorable in a JSON file.

**Status:** DECIDED · [D-10](DECISION_LOG.md#d-10)

---

### Q-FLR-3 · Does the elevator do anything?

**Blocks:** `GAME_DESIGN` (build phase adjacency), `CONTENT_SCHEMA` (tile
adjacency model), `ART_PIPELINE` (the shaft is a manifest entry).

Not asked in the prompt, raised here because the brief names the elevator as the
thing joining the floors and then gives it no mechanics. Left alone, the vertical
axis has no adjacency at all, and every synergy is trapped on its own floor.

**Options**

- **A — Decorative.** The elevator is art. Floors never interact spatially. Simple,
  and it wastes the building a second time.
- **B — Landing column.** The leftmost column of each floor is a **landing**. Tiles
  in a landing column are adjacent to the landing tiles of the floors immediately
  above and below, in addition to their normal orthogonal neighbours.
- **C — Full vertical adjacency.** Every tile is adjacent to the tile above and
  below it. Turns the tower into a 3D grid — enormous combinatorial depth,
  unreadable UI, and the top-down build view cannot show it.

**Recommendation: B.** Three tiles per floor gain a scarce, expensive, visible
vertical channel. It gives Middle Management ("retriggers a neighbour") somewhere
to live, it makes floor *ordering* matter, and it is drawable in a top-down view as
a highlight on one column. C is a better toy and a worse game.

**Trade-off:** The landing column becomes contested real estate, which risks every
optimal build looking the same down the left-hand side. Watch for it; the counter is
to make some strong rooms illegal on the landing column.

**Status:** DECIDED · [D-11](DECISION_LOG.md#d-11)

---

## Layers

### Q-LYR-1 · Is furniture a distinct layer in v1?

**Blocks:** `GAME_DESIGN` (build phase), `CONTENT_SCHEMA` (placement model),
`ROADMAP` (vertical slice scope). Named risk 1 in the brief.

First, a structural clarification that the answer depends on: **rooms are zones, not
objects.** A room is a rectangle drawn over floor tiles; it does not consume them.
Employees and furniture occupy the tiles *inside* it and gain its aura. This is what
makes "employees must occupy a room to gain its effect" work, and it means the
scarce resource is **tiles inside good rooms** — which is the polyomino packing
pressure the depth model needs.

Given that, the layer question is: what competes for those tiles?

**Options**

- **A — Rooms only.** Employees occupy room tiles; rooms carry all the modifiers.
  Fewest layers, and the build phase becomes "put good staff in good rooms" — one
  decision, made once, with no packing tension at all.
- **B — Rooms + furniture, no equipment.** Furniture occupies tiles inside rooms,
  competing directly with employees for them. Every piece of furniture is an
  employee you did not field. Triggers fire from adjacency.
- **C — Rooms + furniture + equipment.** As B, plus a per-employee item slot with
  its own inventory UI, its own shop stock, and its own recipe interactions.

**Recommendation: B. Keep furniture, cut equipment.**

Furniture is the highest depth-per-unit-complexity element in the design: it costs
one placement rule, it creates the central scarcity (a tile is either output or
support, never both), and it is the natural third input to recipes. Equipment is the
opposite — a fourth layer, a second inventory screen, and a multiplicative increase
in the balance surface, in exchange for customisation that promotions already
provide.

This is the direct answer to the brief's named layer-bloat risk: the layer to cut is
equipment, and it should be cut now rather than deferred, because deferring it means
designing around a hole.

**Trade-off:** Without equipment, individual employees are less customisable and
the shop has one fewer thing to sell. Promotions ([Q-RCP-1](#q-rcp-1--how-many-recipes-at-launch-and-how-are-they-discovered))
must carry that weight, which raises the required recipe count.

**Status:** DECIDED · [D-12](DECISION_LOG.md#d-12), confirmed on equipment by
[D-34](DECISION_LOG.md#d-34). Furniture is provisional — see
[Q-LYR-3](#q-lyr-3--does-furniture-earn-its-tile).

---

### Q-LYR-2 · What can an employee carry?

**Blocks:** `CONTENT_SCHEMA` (employee schema), post-v1 expansion room.

**Recommendation: nothing, in v1.** An employee is defined by its definition id, its
level, its statuses and its tile. Customisation happens by *replacement* — two
Junior Devs plus a Whiteboard become a Senior Dev — not by accessorising.

The employee schema nonetheless carries an `attachments: []` array from the first
commit, always empty, serialised, and included in the snapshot hash. This is the
brief's "keep the expansion room" instruction taken literally: adding equipment
post-v1 becomes a content and UI change, not a save-format migration and a snapshot
version bump that invalidates every stored ghost.

**Trade-off:** An always-empty field is a small, permanent smell in the schema and
in every serialised snapshot. It is far cheaper than migrating a ghost pool.

**Status:** DECIDED · [D-13](DECISION_LOG.md#d-13)

---

### Q-LYR-3 · Does furniture earn its tile?

**Blocks:** nothing before the vertical slice. After it: the shape of the room
catalogue and the third input of every recipe.

Raised by [D-34](DECISION_LOG.md#d-34). Furniture is in v1 because it is the cheapest
tile scarcity in the design, and it is on trial because the human is unsure it is
worth a layer. The question is not whether furniture is *fun* — that is not
answerable — but whether it produces a decision the game would otherwise lack.

**The test.** Twenty vertical-slice runs, greybox, by the developer. Furniture fails
if either holds:

- Furniture is placed only when a recipe wants it — it is a crafting reagent, not a
  layout choice.
- The player never faces a real choice between a piece of furniture and a hire for the
  same tile — scarcity never bites.

**If it fails:** every furniture effect folds into a room aura or a Tier III clause;
recipes take a room context as their third input; the manifest entries and greyboxes
are deleted. The sim is unchanged, because furniture is flat bonuses and periodic
events and rooms already carry both.

**If it passes:** Phase 3's catalogue extends it. Nothing else changes.

**Status:** OPEN — a playtest gate, answerable only once the slice exists.

---

## Economy and reward shape

### Q-ECO-1 · How is the reward for a correct commitment made impactful?

**Blocks:** `GAME_DESIGN` (rooms, economy), `CONTENT_SCHEMA` (room schema),
`BALANCE_PLAN` (outcome variance bands), `SIMULATION_SPEC` (rooms carry a per-match
state the snapshot must include).

Raised by [D-24](DECISION_LOG.md#d-24). Making reconstruction painful is only half a
decision — a game that punishes changing your mind and does not pay for getting it
right is just a game that punishes you. The compensating half needs a mechanism.

**Options**

- **A — Steeper room auras.** Correctly-staffed rooms simply multiply harder. Trivial
  to implement, and it rewards being *right now* rather than having *been right then*.
  It pays the same whether you built the room in round 2 or bought it in round 12, so
  it does nothing for commitment specifically.
- **B — Steeper recipe results.** Promotions become leaps rather than increments.
  Rewards good crafting, not good placement, and crafting is already reversible within
  the round — so it is not the thing being committed to.
- **C — Tenure.** A room accrues Tenure for every round it stays in place *and* stays
  meaningfully staffed. At thresholds it gains a permanent step to its aura. Tenure is
  forfeited entirely on demolition.
- **D — All three.**

**Recommendation: C, with A as a secondary lever.**

Tenure is the only option that pays for the thing that is actually being risked. The
cost of a room is that you cannot move it; Tenure makes not moving it the source of
the reward. That closes the loop rather than bolting a bonus onto the side of it — and
it is what makes [Q-RISK-2](#q-risk-2--is-the-room-commitment-tension-actually-load-bearing)'s
demolition cost scale with how good the room was, without needing a rule that says so.

Proposed shape, to be tuned in `BALANCE_PLAN` rather than settled here:

| Tier | Reached at | Effect | Fiction |
| --- | --- | --- | --- |
| — | rounds 0–2 | none | *Newly Fitted* |
| I | round 3 | aura step | *Established* |
| II | round 6 | aura step | *Departmental* |
| III | round 10 | aura step, plus the room's unique clause comes online | *Institutional* |

Thresholds are counted in rounds *held*, not rounds elapsed, so a room bought in round
9 can still reach Tier I. In a 16-round run ([D-21](DECISION_LOG.md#d-21)) only genuinely
early commitments reach Tier III, which is the intended scarcity. "Meaningfully staffed"
needs a concrete definition — proposed: at least half the room's tiles occupied by
employees at the moment the round is committed.

It is also the strongest satire in the economy. The firm that has been in the same
building since 1987 beats the one that reorganises every quarter, and it beats it
*because* it never reorganised.

**Trade-off, and it is a real one.** Tenure is per-match state that lives on the room,
so it must enter the tower snapshot ([D-18](DECISION_LOG.md#d-18)) — which means
authored campaign rivals must declare plausible Tenure values, and a rival tower's
Tenure is now a balance knob someone has to set. That is genuine ongoing cost. It also
widens outcome variance in both directions, which is the subject of
[Q-ECO-2](#q-eco-2--does-a-run-need-a-mid-run-recovery-valve).

**Status:** DECIDED · [D-25](DECISION_LOG.md#d-25)

---

### Q-ECO-2 · Does a run need a mid-run recovery valve?

**Blocks:** `GAME_DESIGN` (campaign nodes, new-player experience), `BALANCE_PLAN`
(run-level variance bands).

Painful demolition and compounding Tenure point the same direction: **runs snowball**,
both ways. A run whose early commitments were right runs away with it. A run whose
early commitments were wrong is effectively decided by round 8 — and under
[D-21](DECISION_LOG.md#d-21) the player then has eight more fights and three lives to
spend finding that out.

That is the cost of the stance in [D-24](DECISION_LOG.md#d-24), and it is worth paying;
a decision that cannot go badly is not a decision. But the losing half of it lands
hardest on exactly the player least equipped to have avoided it, and "you may as well
concede at round 8" is the specific failure that ends runs early and sessions with them.

**Options**

- **A — Nothing.** The commitment is total. Cleanest expression of the design, harshest
  new-player experience, and it makes a mid-run misstep functionally a lost run.
- **B — One Restructuring per run.** A single use, acquired from a **Board Meeting**
  node: one free relocation with the room's full Tenure carried (relocation itself
  exists and costs a fee plus three Tenure rounds — D-51). Scarce enough to be a real
  decision about *when* to spend it; present enough that a single early mistake is
  recoverable.
- **C — Tenure decays rather than resets.** Demolition drops the room one tier instead
  of clearing it. Softer, and it blunts the whole mechanism — the pain of demolition is
  precisely that it is total.
- **D — Escalating fee, no valve.** Cheap the first time, punitive thereafter. Rewards
  early experimentation and punishes late reoptimisation, which is backwards: late
  reoptimisation is the interesting decision.

**Recommendation: B.** One Restructuring, gated behind a map node so acquiring it is
itself a routing decision. It preserves the stance completely — demolition is still
total, ordinary relocation still costs a fee and a tier — while giving a run exactly
one free move, which is enough to keep a misstep from being a concession. With
Relocate in the game (D-51), the question has narrowed to whether that single free
move should exist at all.

**Trade-off:** It is one more run-scoped resource for a new player to understand, and
holding it too long is its own trap. Both are acceptable; a player who wasted their
Restructuring made a decision, which is the point.

**Status:** DECIDED · [D-52](DECISION_LOG.md#d-52) — human call: no free Restructuring.
Paid relocation (D-51) is the recovery valve; it costs a round's income and a tier of
Tenure every time, and that is the intended shape. The Board Meeting pool is seven
modifiers.

---

## Greybox and assets

### Q-GBX-1 · What is the minimum a manifest entry needs before a greybox can be built?

**Blocks:** `ART_PIPELINE` (the whole document), and every screen in `GAME_DESIGN`,
since the brief forbids approximate greyboxes — an entry that cannot be completed is
a design decision that has not been made.

**Recommendation: nine required fields**, and an entry missing any of them is a
build error rather than a warning.

```jsonc
{
  "id": "room.server_room",           // 1. required, unique, dot-namespaced by kind
  "kind": "room",                     // 2. required: room|employee|furniture|ui|fx|tile
  "category": "operations",           // 3. required, drives greybox tone coding
  "label": "Server Room",             // 4. required, drawn on the placeholder
  "footprint": { "w": 2, "h": 2 },    // 5. required for grid kinds, null for ui/fx
  "sprite": {
    "w": 64, "h": 88,                 // 6. required, pixels at 1x
    "anchor": { "x": 0.5, "y": 1.0 }, // 7. required, explicit, never assumed
    "asset": "packs/gk/office/server_room.png",  // 8. required, may not yet exist
    "sourceRect": null                //    optional: [x,y,w,h] into an atlas
  },
  "sortBias": 0,                      // 9. required, default 0, range -10..10
  "screens": ["build", "shop"],       //    optional, feeds the worklist ranking
  "visibility": 3                     //    optional, 1..4, feeds worklist ranking
}
```

**Overhang is derived, never authored.** Given `footprint`, `sprite.w/h`, `anchor`
and the tile size, the overhang rectangle is computable. Authoring it invites it to
disagree with the fields it is computed from — and a manifest that can contradict
itself is worse than no manifest.

**Absent assets are valid.** `sprite.asset` is declared before the file exists. The
validator fails on a *mismatch*, never on absence. Adding art is a file copy; it
never edits the manifest.

**Trade-off:** Nine required fields makes stubbing a new entity heavier than it
would otherwise be, which is exactly the intended friction — the slicer tool
generates conforming stubs so the cost lands on the tool, not the designer.

**Status:** DECIDED · [D-14](DECISION_LOG.md#d-14)

---

### Q-GBX-2 · What is the draw-order rule for overhanging sprites?

**Blocks:** `ART_PIPELINE`, and every art drop after the first — settle it in
greybox or art layers wrongly the moment it lands.

**Options**

- **A — Pure y-sort on the anchor row.** Handles the common case; cannot express a
  wall-mounted whiteboard that must draw behind a desk on the same row.
- **B — y-sort plus explicit per-entry bias.** One extra integer. Handles same-row
  ordering.
- **C — Explicit layer indices.** Full control, and it becomes a manual ordering
  problem across hundreds of entries.

**Recommendation: B**, with a **five-key total order** so that draw order is fully
determined and never depends on array order or hash iteration:

```
sort by: floorIndex ASC, anchorTileRow ASC, sortBias ASC, tileCol ASC, id ASC
```

The last key is not decoration. A total order means a screenshot is reproducible,
which means a greybox screenshot can be a regression test.

`sortBias` is required (default 0) rather than optional, so that authoring one is a
normal act rather than an exception. Range -10..10; wall-mounted furniture uses -5
by convention, floor-standing uses 0, ceiling and hanging fixtures +5.

**Trade-off:** A per-entry bias is a hand-tuned number that can be wrong, and being
wrong is invisible until the art lands. Mitigate with a greybox debug overlay that
draws the sort key on each placeholder.

**Status:** DECIDED · [D-15](DECISION_LOG.md#d-15)

---

### Q-GBX-3 · What is the greybox palette?

**Blocks:** `ART_PIPELINE`, and the brief's mixed-state coherence requirement —
this palette sits beside real pack art for months.

**Options**

- **A — Neutral greys.** Honest, unmistakable for art, and makes every part-arted
  screen read as broken. Rejected by the brief.
- **B — Sampled from the pack, desaturated, category-coded.** Placeholders sit in
  the same colour world as the finished art; a mixed screen reads as *stylised*
  rather than unfinished.
- **C — Fully art-directed placeholders.** Pretty, and it destroys the ability to
  tell at a glance what is real. Fatal to the coverage overlay's usefulness.

**Recommendation: B.** Six hues sampled from the GuttyKreum Office Interior palette,
pulled to roughly 35% saturation and 55% value, declared in one file
(`content/greybox_palette.json`) that nothing else may hardcode:

| Category | Hue source | Used for |
| --- | --- | --- |
| `structure` | Wall/floor neutrals | Rooms, zones, the grid |
| `operations` | Desk teal | Operations rooms and their furniture |
| `people` | Uniform rose | Employees |
| `support` | Cabinet ochre | Furniture, fixtures |
| `interface` | Terminal slate | UI panels, the ledger, cards |
| `anomalous` | Horror-pack violet | Portal, extraplanar, B1 |

Plus one non-negotiable: `invalid` in unmissable red, for an entry whose asset
mismatched or whose dimensions are undeclared. A broken thing must look broken; a
merely-unfinished thing must not.

Placeholder rendering: footprint filled at 85% alpha, overhang hatched at 45%, a 1px
darker border so adjacency reads, and the id / footprint / pixel dimensions drawn in
a pixel font at 1x. Any placeholder too small to carry its label drops to id-only,
then to a coloured dot — but never scales the text, which would break pixel
discipline.

**Trade-off:** A palette-matched placeholder is, by design, less obviously a
placeholder. The coverage overlay and the worklist exist to answer "what is still
greybox" — the eye should not have to.

**Status:** NEEDS SIGN-OFF — this is art direction, and it is the view the developer
will look at every day for months. Phase 4 proceeded on the recommendation:
`manifest/greybox_palette.json` holds the seven tones with placeholder hex values at
the stated saturation and value targets; the pack-fit pass
([Q-GBX-5](#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions))
replaces them with sampled ones.

---

### Q-GBX-4 · What defines "art complete"?

**Blocks:** `ART_PIPELINE`, `ROADMAP` (the release gate line), and the brief's named
risk that the art backlog never closes.

**Options**

- **A — Every manifest entry.** Includes debug and unreachable entries. Unachievable
  by construction, so the gate never closes and stops being believed.
- **B — Tiered, with exemptions.** Tiers 1–3 complete, tier 4 may ship greybox.
  Achievable, and it means shipping visible placeholders on Steam.
- **C — Reachability-based.** Every entry reachable in a normal campaign run must
  have a present asset that passes dimension validation. Debug, developer and
  unreachable entries are exempt and must be explicitly flagged as such.

**Recommendation: C.** The gate is:

1. Every manifest entry with `releaseGate: true` (default for all content-reachable
   entries) has a present asset file.
2. The dimension validator passes on every present asset.
3. Zero entries render in the `invalid` tone in a full campaign playthrough capture.
4. The perspective rule holds: no screen mixes top-down and isometric sources.

Tiers still exist, but they **order the worklist rather than exempt anything**. That
is the important distinction: ranking is a scheduling tool, not a licence.

**Tracked from commit 1.** CI emits `art_coverage.json` on every build — total
entries, entries with assets, coverage by tier and by screen — and the number is
surfaced in the README badge and the in-game overlay. It is a *release* gate and
appears on its own line in `ROADMAP`, never as a phase dependency.

**Trade-off:** C makes the gate strict, and a strict gate that is far away can be
demoralising. The worklist ranking is the mitigation: the top fraction of the list
carries most of the perceived polish, so the curve of *felt* completeness runs well
ahead of the curve of measured completeness.

**Status:** DECIDED · [D-16](DECISION_LOG.md#d-16)

---

### Q-GBX-5 · Can the packs produce the decided battle and portrait dimensions?

**Blocks:** the first greybox of `fx.tower.floor_segment`, `fx.tower.roof`,
`fx.tower.basement` and `ui.portrait` in Phase 4. Nothing in Phase 2 or 3.

Raised by `GAME_DESIGN.md` §19. The brief forbids approximate greyboxes, so Phase 2
*decided* the dimensions the isometric battle view and the inspector portrait use
rather than leaving them open: floor segments are 96 × 32, the roof 96 × 16, the
basement 96 × 24, the portrait 64 × 64. Those numbers were chosen to fit the 640 × 360
canvas; they were not derived from the packs.

**What has to happen.** Before any of those four entries is greyboxed, open the
Japanese City / Osaka / Dotonbori packs and the Portraits pack and confirm a slice at
each declared size reads correctly. If one cannot, change the manifest entry — and
the screen layout that depends on it — *then* greybox. The rule is that a greybox is
never built against a dimension that is known to be wrong.

**Recommendation.** Do this as the first task of Phase 4, before the manifest schema
is finalised, so that any change is a spec edit rather than a re-layout. It is an
afternoon with the packs open, not a design question.

**Status:** OPEN — a verification, not a decision. Needs the packs, which are not yet
owned: the plan is to buy the Japan Collection complete edition, so every pack in the
brief's inventory will be available and the manifest's candidate-source lines stand.
The check waits on the purchase and nothing in Phase 5 waits on the check. Phase 4
flagged thirteen manifest entries with `verify: true` (the four tower pieces, the
inspector portrait, the eight founder portraits — the nine portraits are one check);
the release gate refuses to close while any is set. Procedure in `ART_PIPELINE.md`
§15.

---

## Recipes

### Q-RCP-1 · How many recipes at launch, and how are they discovered?

**Blocks:** `GAME_DESIGN` (crafting and codex), `CONTENT_SCHEMA` (recipe schema),
`ROADMAP` (content volume), retention.

**Options on count**

- **A — ~20.** Learnable in three runs. Discovery stops being a retention driver by
  run four.
- **B — ~40.** Roughly a dozen runs to see most of it. Enough that two players'
  knowledge differs for a long while.
- **C — ~80.** Months of discovery, and a combinatorial balance surface that
  automated testing will struggle to cover, plus 80 hand-authored result entities.

**Recommendation: B — approximately 40**, in three classes:

| Class | Count | Form | Available |
| --- | --- | --- | --- |
| **Promotions** | ~20 | employee + employee (+ room context) → senior employee | From round 1 |
| **Renovations** | ~12 | furniture + furniture, or furniture + room → upgraded fixture | From round 1 |
| **Rituals** | ~8 | cross-class, requires a B1 room | After the portal opens |

**Discovery**, designed alongside the system as the brief requires:

1. **The codex exists from the first run**, showing every recipe as a slot outline —
   the *number* of recipes is public, the contents are not. An empty codex with
   visible holes reads as depth; a hidden codex reads as absence.
2. **Near-miss feedback.** Placing a valid combination in an invalid room context, or
   with one input wrong, produces a distinct non-committal signal ("the whiteboard
   flickers") rather than silence. This is what turns guessing into deduction.
3. **Partial reveals.** A discovered recipe reveals its inputs. An *undiscovered* one
   reveals its class and its input count once you have held any of its inputs.
4. **The Consultant node** buys a full reveal outright, which is what makes it worth
   spending a node on.
5. **Discoveries persist across runs** in a meta-codex. Re-deriving a known recipe
   each run is tedium, not depth.

**Trade-off:** 40 recipes means ~40 result entities, each needing a manifest entry,
a greybox and eventually art. That is a significant share of the art worklist and
should be weighted accordingly when ranking it.

**Status:** NEEDS SIGN-OFF — count is a scope decision with a large content bill.
Phase 3 proceeded on the recommendation: `content/recipes.json` holds forty (21
promotions, 12 renovations, 7 rituals). Cutting is a deletion; the number is not
load-bearing anywhere else.

---

### Q-RCP-2 · Do recipes consume inputs, and can they be undone?

**Blocks:** `GAME_DESIGN` (build phase), `SIMULATION_SPEC` (nothing — crafting is
build-phase only, and that is worth stating explicitly).

**Options**

- **A — Consume, irreversible immediately.** Highest stakes, and it punishes misclicks
  in a game where a misclick and a strategy look identical.
- **B — Consume, reversible until Ready.** The build round is a scratchpad; pressing
  Ready commits everything at once.
- **C — Non-consuming.** Recipes are unlocks rather than transformations. Removes the
  cost entirely and with it most of the decision.

**Recommendation: B.** Crafting consumes its inputs and **costs no budget** — the
inputs are the cost. Everything done during a build round, crafting included, is
reversible via a general undo up until **Ready**, after which it is permanent.

This preserves the commitment tension where it actually lives — across rounds, in
rooms and in staff you can no longer un-hire without a severance fee — while
removing the class of loss that teaches nothing. It also matters for the agentic
workflow: a build round that is a pure function from (start state, action list) to
(end state) is testable; one with irreversible mid-round side effects is not.

**Trade-off:** Undo weakens the drama of a big irreversible combine. Recover it with
presentation — commit-on-Ready can animate every craft at once as the quarter opens.

**Status:** DECIDED · [D-17](DECISION_LOG.md#d-17)

---

## The portal

### Q-PTL-1 · What makes an extraplanar hire a real gamble?

**Blocks:** `GAME_DESIGN` (portal), `CONTENT_SCHEMA` (rider schema), `BALANCE_PLAN`
(portal builds must not be strictly better).

The failure mode is obvious and common: the risky option is simply the strong
option, and by round 8 every build is a portal build.

**Options**

- **A — Hidden stats.** You pay before you see. Gambling, but shallow, and it makes
  a bad outcome feel like theft rather than a bet.
- **B — Curse meter.** Extraplanar staff accrue Anomaly stacks; thresholds fire bad
  events. Escalating and thematic, but the cost is diffuse and arrives late, so at
  the moment of purchase it does not feel like a decision.
- **C — Visible riders.** Every portal hire is strong and carries a rolled,
  **visible** permanent drawback accepted at purchase. "Cannot be laid off." "Your
  Executive floor output x0.8." "At Quarter Close, deal 200 Morale to yourself."
- **D — Goodwill upkeep.** Each extraplanar employee permanently reduces starting
  Goodwill or regen. A clean, universal, arithmetic cost.

**Recommendation: C + D, with B as an optional second layer.**

- **C** makes the purchase a decision rather than a dice roll — you see the price and
  choose. Riders are content, so they are authorable, testable and rankable.
- **D** is the flat structural cost that stops portal staff being free power even when
  a rider happens to be mild. It also ties the portal directly into the Goodwill
  economy, which is where the game's tension already lives — a portal build is
  *inherently* a glass-cannon build, without needing a rule that says so.
- **B** is genuinely good but is a second system with its own UI, its own thresholds
  and its own boss encounter ("an Audit from Below" when the meter maxes). Recommend
  scoping it as post-vertical-slice, and only if the run needs another escalating
  arc. Do not build it in v1's first pass.

**Trade-off:** Riders are hand-authored content that interacts with everything, which
is exactly the kind of content that produces balance outliers. Cap the rider pool
small (~12) and assert in CI that no rider is net-positive.

**Status:** NEEDS SIGN-OFF — the shape of the risk is a tone decision as much as a
mechanical one.

---

### Q-PTL-2 · What unlocks the portal, and how does the reveal land?

**Blocks:** `GAME_DESIGN` (campaign structure, mode configuration), `ROADMAP`.

The constraint from the locked foundation is that modes are a **configuration
layer, never a rules fork**. So the unlock must be one rule with two configurations,
not two rules.

**Recommendation.** One rule: *the portal opens when the run's `portalUnlock`
condition is met*, where the condition is run configuration.

- **Campaign:** condition is "Act 1 boss defeated" (~round 6). Narrative, tutorialised,
  and it gives the Act 1 boss a reward that is not a number.
- **Ranked:** condition is "round ≥ 5", identical for both players, so the ladder
  stays symmetric.

Unlocking the portal makes `B1` purchasable and adds the extraplanar stock to the
shop as a second, separately-rerolled column.

**The reveal.** Escalate from round 1, in the background, before it is ever
mechanical: a lift panel with a `B1` button that is not lit; a fax arriving with no
sender; an employee in the roster the player is certain they did not hire; the
Reception plant dead in one round and fine the next. Then the Act 1 boss falls and
the button lights.

The satire works best if the supernatural is treated administratively. The Otherworld
Temp Agency does not haunt you — it invoices you.

**Trade-off:** Round-6 unlock means the first five rounds have a whole shop column
missing, which is a slower opening. That is the correct trade for a reveal that lands,
and the early rounds are also where a new player most needs fewer options.

**Status:** NEEDS SIGN-OFF — narrative timing is the human's call.

---

## Async PvP — deferred, seams designed now

### Q-PVP-1 · What is stored in a tower snapshot?

**Blocks:** `ARCHITECTURE`, `SIMULATION_SPEC` (sim entry point), `CONTENT_SCHEMA`,
and every scripted campaign rival — they use this format, which is what makes them
the eventual seed ghost pool.

This is the highest-leverage decision in the deferred set, because it is the one that
must be right *now* even though it ships later.

**Recommendation.** A snapshot is everything the sim needs and nothing else:

```jsonc
{
  "schemaVersion": 1,
  "contentVersion": "0.4.2",         // content DB version; mismatch = migrate or reject
  "ownerLabel": "Kobayashi Holdings", // display only, never read by the sim
  "round": 8,
  "floors": [
    {
      "index": 2,
      "kind": "operations",
      "grid": { "w": 5, "h": 3 },
      "rooms": [
        { "defId": "room.server_room", "rect": [1, 0, 2, 2], "level": 1 }
      ],
      "occupants": [
        {
          "tile": [1, 0],
          "kind": "employee",
          "defId": "emp.senior_dev",
          "instanceId": "emp_7f3a",   // stable, used by the ledger
          "level": 2,
          "statuses": [],
          "attachments": []           // always empty in v1, reserved
        }
      ]
    }
  ],
  "globals": {
    "goodwillMax": 4200,
    "goodwillRegen": 160,
    "modifiers": ["mod.overtime_culture"],
    "portalRiders": ["rider.cannot_be_laid_off"]
  },
  "checksum": "…"
}
```

**Explicitly excluded:** budget, shop stock, reroll counts, RNG state, run history,
map position, cosmetics, and anything the player could change without changing how
the tower fights. A snapshot is a *combatant*, not a save file. The save file
contains a snapshot; it is not one.

The sim entry point that this implies, and which must exist from the first line of
sim code:

```ts
simulate(seed: number, a: TowerSnapshot, b: TowerSnapshot, rules: RuleSet): MatchResult
```

Pure, headless, deterministic, no clock, no I/O. Campaign rivals are authored
snapshots. Ranked ghosts are captured snapshots. Balance fixtures are generated
snapshots. One format, one entry point, three uses — and adding ranked later becomes
additive rather than a rewrite, which is exactly the brief's requirement.

**Trade-off:** Snapshots must be versioned and migrated from day one, including the
campaign rivals stored in the repo. That is real ongoing cost, paid in exchange for a
ghost pool that does not become worthless on the first content patch.

**Status:** DECIDED · [D-18](DECISION_LOG.md#d-18)

---

### Q-PVP-2 · How are players matched once ranked exists?

**Blocks:** post-v1 `ARCHITECTURE` only. Recorded now so the snapshot carries the
fields matchmaking will need.

**Recommendation.** Ghosts are bucketed by `(round, ratingBand)`. A match draws
deterministically from the bucket using the match seed, so a match is reproducible
from `(seed, ghostId, ghostId)` alone — which makes any disputed result re-runnable.
Fallback widens the rating band, then the round band, then falls back to the scripted
campaign rival pool. That fallback is the cold-start answer: the campaign ships first,
so by the time ranked exists there are dozens of authored towers per round to seed it.

Consequence for the snapshot: it must carry `round`, and the ghost record wrapping it
must carry a rating. Rating lives on the record, not in the snapshot, because the sim
must never see it.

**Trade-off:** None material at this stage; this is a placeholder decision recorded to
protect the snapshot format.

**Status:** DECIDED · [D-19](DECISION_LOG.md#d-19)

---

### Q-PVP-3 · What is the anti-cheat posture, given the client owns the sim?

**Blocks:** post-v1 `ARCHITECTURE`. Stated plainly now because it constrains nothing
if planned and everything if discovered late.

**The honest position:** a client that owns the simulation cannot be trusted with a
result, and no amount of obfuscation changes that. There are exactly two workable
postures.

**Options**

- **A — Trust the client, detect statistically.** Cheap. Catches the careless, not the
  determined. Ladder integrity is a matter of hope.
- **B — Server re-simulation.** The server runs the *same headless TypeScript module*
  on the two submitted snapshots and the match seed. The client's result is advisory —
  it exists so the player sees a fight immediately, not so the ladder believes it.

**Recommendation: B.** The locked requirement that the sim be pure and headless is
precisely what makes B cheap: it is the same module, run in Node, with no rendering
dependency to strip out. Two supporting pieces:

1. **Snapshot legality validation.** Before a snapshot enters the ghost pool, assert it
   is *constructible* — could a player at that round afford this tower under the
   economy rules? This catches fabricated towers, and it is dual-use: the same
   validator checks that hand-authored campaign rivals are legal.
2. **Campaign is unpoliced, deliberately.** Single-player. Do not spend a line of code
   defending it.

**Trade-off:** B requires server infrastructure that campaign-first v1 does not
otherwise need. It is deferred work, not deferred design — nothing in v1 must be built
for it, but nothing in v1 may make it impossible. Keeping `simulate()` pure is the
whole of that obligation.

**Status:** DECIDED · [D-20](DECISION_LOG.md#d-20)

---

## Architecture

### Q-ARCH-1 · When does controller navigation arrive?

**Blocks:** Steam Deck verification, and therefore the Steam Deck compatibility badge.
Nothing in the campaign's playability on desktop.

Raised by `ARCHITECTURE.md` §7–§8. The build phase is drag-and-drop over a tile grid;
that maps to a cursor-on-grid controller scheme cleanly enough, but it is real UI work
with its own screenshot fixtures, and it is not on the path to the game being fun.

**Options**

- **A — v1.** Ship with controller navigation. Delays the vertical slice by the size
  of the work; nothing else changes.
- **B — Post-v1, designed for.** Ship mouse-and-keyboard; keep every screen's
  interactive elements in a navigable list from the start so that adding a cursor
  scheme is a new input adapter, not a re-layout.
- **C — Never.** Desktop only. Cheapest, and it forgoes the Deck, where a 45-minute
  auto-battler is at home.

**Recommendation: B.** The `Platform` split and the action-based input model already
make it an adapter. The one thing v1 must do is keep focusable elements enumerable
per screen, which costs nothing now and everything later.

**Status:** DECIDED · [D-47](DECISION_LOG.md#d-47) — recommendation accepted; post-v1.

---

### Q-UX-1 · When does the UX review happen?

**Blocks:** nothing formally. In practice, the build screen's layout is the thing a
player touches most, and a layout problem found after the greybox is built costs a
re-layout plus every screenshot fixture that depends on it.

Raised by the human at the end of Phase 4. The planning prompt's phases have no UX
step: `GAME_DESIGN.md` §19 specifies *layouts* — rects, anchors, footprints — which is
what an agent needs to build the greybox, but a layout is not a UX. Information
hierarchy, click counts, what is visible without hover, whether the eye lands on the
right thing during a fight: those are judged by *using* the screen, and the brief's
own position is that the game must be judgeable in greybox.

**Options**

- **A — Now, on paper.** Review §19's rects as wireframes. Cheap, and it can only
  catch layout-level problems: overlaps, things too small to read, a panel in the wrong
  place. It cannot judge feel or flow.
- **B — At the greybox vertical slice.** The first roadmap milestone. Every screen is
  real, interactive and deterministic; screenshot fixtures exist; changes are cheap
  because nothing has art. This is where "is the ledger readable at 4 lines a second"
  and "does Ready feel like a decision" get answered — the §21 questions.
- **C — After art.** Too late by construction: the whole workflow exists so that
  nothing waits on art.

**Recommendation: A now, B as the real pass.** Do a short paper review of the two
most-seen screens — build and battle — as rendered wireframes from the manifest, before
any greybox is built, to catch the class of problem that is cheap now and expensive in
a week. Then treat the vertical slice as the UX milestone it already is: `ROADMAP.md`
should name it that way, and the §21 human checks are its checklist. The distinction
that matters is that A can fix a rect and B can fix a design.

**Status:** DECIDED · [D-48](DECISION_LOG.md#d-48) — wireframes deferred; the vertical
slice (`ROADMAP.md` M2) is the UX milestone.

---

## Risks that are also decisions

### Q-RISK-1 · Is the GuttyKreum licence cleared for commercial release?

**Blocks:** commercial release. Blocks nothing before it.

The brief flags this as a ship blocker. It is recorded here as an open item because a
flag with no owner and no date is not a plan.

**Recommendation.** Before any money or significant art-selection time is spent:
obtain and archive the licence text for **every** pack in the inventory, confirm
commercial use and in-game redistribution, and record the result in the repository
alongside the packs. If any pack fails, the perspective split and the manifest make
substitution survivable — but only if it is found early.

Two things reduce the exposure meaningfully and both are already in the design: the
manifest means swapping a pack is a file-path change rather than a layout pass, and
the Otherworld Temp Agency fiction means a replacement pack in a different style can
be absorbed rather than hidden.

**Status:** NEEDS SIGN-OFF — needs a named owner and a date, which is the human's.
The packs are to be bought as the complete edition, which makes this one licence
record to read rather than eleven: read it at purchase, before the first slice, and
put the verdict in `packs/guttykreum/LICENSE.md` so the release gate can find it.

---

### Q-RISK-2 · Is the room-commitment tension actually load-bearing?

**Blocks:** `GAME_DESIGN` (economy), `BALANCE_PLAN` (the invariant), and every number
fitted against the budget curve.

Locked decision 4 says the tension between static rooms and flexible employees is the
core strategic identity of the game. That tension only exists if undoing a room is
genuinely painful. If demolition is cheap, or if budget by round 8 affords a routine
full rebuild, rooms become slow employees and the game's single point of divergence
from Backpack Battles quietly disappears — without anything visibly breaking, which is
what makes it dangerous.

**Options**

- **A — Forgiving.** Demolition costs a small fee. Rooms are a soft preference.
- **B — Costly.** A Renovation fee of roughly one round's income, no refund.
- **C — Costly and compounding.** As B, plus the room loses whatever it had accrued by
  sitting where it was. The fee is the visible cost; the accrual is the real one.

**Recommendation: C.** Demolition refunds nothing and costs a **Renovation fee of
approximately one round's income**, and the room forfeits its accrued Tenure
([Q-ECO-1](#q-eco-1--how-is-the-reward-for-a-correct-commitment-made-impactful)).
Because a working room accrues and a room that never worked does not, the true cost of
demolition scales with how good the room was — which is exactly where the pain belongs.
Ripping out a mistake costs a round. Changing your mind about something that was
working costs the run's accumulated advantage.

Do not leave this as a design intention. It is measurable, and it belongs in
`BALANCE_PLAN` as an assertion: **in simulated runs, the median number of rooms
demolished after placement stays below a stated threshold.** If agents rebuild freely,
the tension is gone, and the number will say so long before a human notices.

**Trade-off:** Painful demolition punishes early mistakes hardest, which is worst for
new players. The campaign's early acts should be forgiving in **budget**, not in
demolition cost — keep the rule sharp and the resources loose. The compounding half
raises the stakes further, which is what
[Q-ECO-2](#q-eco-2--does-a-run-need-a-mid-run-recovery-valve) exists to guard.

**Status:** DECIDED · [D-24](DECISION_LOG.md#d-24) — human call: reconstruction should
be painful, with the rewards for good decisions scaled up to compensate.

---

## What remains open, and when it bites

Seven items remain open. None blocked any phase; all five are drafted. Each is
listed here against the moment it first bites, so it can be answered when it is
actually needed rather than in a batch.

| Open question | First blocks | Cost of proceeding on the recommendation |
| --- | --- | --- |
| [Q-PTL-1](#q-ptl-1--what-makes-an-extraplanar-hire-a-real-gamble) portal risk | Phase 2 — `GAME_DESIGN` portal section | Low. The rider pool is content; changing its shape does not move the sim |
| [Q-PTL-2](#q-ptl-2--what-unlocks-the-portal-and-how-does-the-reveal-land) portal unlock | Phase 2 — `GAME_DESIGN` campaign structure | Low. One configured condition, two values |
| [Q-RCP-1](#q-rcp-1--how-many-recipes-at-launch-and-how-are-they-discovered) recipe count | Phase 3 — catalogue volume | Medium. ~40 result entities is a large share of the art worklist |
| [Q-GBX-3](#q-gbx-3--what-is-the-greybox-palette) greybox palette | Phase 4 — `ART_PIPELINE` | Low to change on paper, high to change once screens exist |
| [Q-RISK-1](#q-risk-1--is-the-guttykreum-licence-cleared-for-commercial-release) asset licence | Release, and any art spend | Not a design decision. It needs an owner and a date, and it is cheapest to answer now |
| [Q-GBX-5](#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions) pack-fit | Phase 4 — first greybox of the battle view | Not a decision. An afternoon with the packs open |
| [Q-LYR-3](#q-lyr-3--does-furniture-earn-its-tile) furniture trial | Vertical slice | A playtest gate with a fold-in plan already written |

