# Company Wars — Balance Plan

Status: **Phase 5 draft.** The methodology, not the numbers. What the automated
matchup harness measures, the bands a healthy game sits inside, how a broken build is
found, the archetypes and how they are meant to beat each other, the process for
tuning from what the harness and players report, and the balance invariants as
machine-checkable assertions the headless sim runs in CI.

The invariants are data: `content/balance.json`, validated like every other content
file, read by `packages/harness`. This document explains them; it does not restate
them, and the excerpts below are copied from the file by the script that writes this
document.

Every number in this plan is an initial value. The plan's claim is not that they are
right; it is that when they are wrong the harness will say so before a player does.

---

## Contents

1. [What balance means here](#1-what-balance-means-here)
2. [The harness](#2-the-harness)
3. [Populations](#3-populations)
4. [Target bands](#4-target-bands)
5. [Archetypes and the counter web](#5-archetypes-and-the-counter-web)
6. [The invariants](#6-the-invariants)
7. [Detecting a broken build](#7-detecting-a-broken-build)
8. [The tuning process](#8-the-tuning-process)
9. [What is not a knob](#9-what-is-not-a-knob)
10. [Telemetry](#10-telemetry)
11. [What the harness cannot judge](#11-what-the-harness-cannot-judge)

---

## 1. What balance means here

Three things, in priority order:

1. **Every defence eventually breaks, and every fight ends.** Without this the game
   has a degenerate strategy (turtle) and a degenerate outcome (the Bell, every time).
   This is a *guarantee*, held by the pressure curve (D-05), and the harness proves it
   rather than estimates it.
2. **No archetype is the answer.** Six ways to build a tower, each inside a win-rate
   band against the field, each beaten by something. Depth from combination, not from
   one correct build.
3. **Fights are legible.** A balanced game whose ledger is unreadable is not balanced
   in any sense a player experiences. The harness measures the live ledger's line rate
   and the share of a win that one ability supplies, because both are things a player
   cannot see past.

Balance is *not* symmetry. Campaign rivals are meant to be beatable by a player who
understands the lesson and to beat one who does not; bosses are exempt from
constructibility on purpose. The harness holds rivals to a different standard (§5.2)
than it holds player-buildable archetypes to.

---

## 2. The harness

`packages/harness` is a Node program that calls `simulate()` — the same module the
client ships — over populations of snapshots and asserts the invariants. It never loads
a renderer. It runs in three cadences:

| Cadence | Trigger | Scope | Budget |
| --- | --- | --- | --- |
| **commit** | every push | Static checks and the cheapest invariants | seconds |
| **smoke** | every push | Rounds [1, 6, 12, 16], 20 seeds per population | under a minute |
| **nightly** | scheduled, and on demand | All rounds, 200 seeds, the search populations at 200 iterations | tens of minutes |

Output is `balance_report.json` — every invariant with its measured value, its
threshold, pass/warn/fail — and a markdown summary with the archetype heatmap, the
fight-length histogram, and the top ten builds the optimizer found. A failing nightly
opens an issue with the report attached. A failing smoke fails the push.

The harness is deterministic: seeds are fixed, expansions are deterministic (D-39),
the sim is deterministic (D-27, D-28). A report is reproducible from its commit.

---

## 3. Populations

Every invariant names the population it runs over. From `content/balance.json`:

| Population | What it is |
| --- | --- |
| `field` | Every archetype template expanded at the round, seeds x 6 archetypes, budget 1000 permille |
| `median_attacker` | The generalist template at median budget; results averaged over seeds |
| `strongest_defence` | A hill-climb over legal, constructible, gimmick-free builds at the round, maximising the tick at which Goodwill first reaches 0 against median_attacker; 200 iterations from the turtle template at 1200 permille |
| `mirror` | A template against itself, different seeds |
| `agent_run` | A greedy builder plays 16 rounds against field rivals, buying the highest harness-scored option each round; 100 runs |
| `optimizer` | A hill-climb over constructible builds maximising win rate against field at the round; 200 iterations; used for outlier detection and pick rates |

Two of these are **searches**, not samples. `strongest_defence` climbs toward the
build that holds Goodwill longest; `optimizer` climbs toward the build that wins most.
The invariants on them are the ones that catch what a sample would miss — the brief's
named risk that the pressure curve must beat the *strongest* defence, not the average
one, is `inv.break_guaranteed` over `strongest_defence`, and it is a search precisely
because an average would not prove it.

Searches are hill-climbs over the build reducer's action set: swap a hire, move a
unit, swap a room, re-lease. Two hundred iterations from a template start. Not
exhaustive, and not meant to be — a search that finds a degenerate build in two
hundred steps has found something a player will find in two hundred runs.

---

## 4. Target bands

Permille throughout, matching the sim. From `content/balance.json`:

| Band | Value | Meaning |
| --- | --- | --- |
| `archetypeVsField` | [420, 580] | Each archetype's win rate against the field, mirror excluded |
| `counterPair` | [580, 750] | A counter wins clearly and is not a wall |
| `mirror` | [470, 530] | A template against itself is even; this is the fairness check on tick-parity initiative |
| `fightMedianEndTick` | [700, 1000] | 35–50 seconds |
| `bellRateMax` | 250 | At most a quarter of fights go to the Bell |
| `drawRateMax` | 20 | Draws are rare |
| `firstShareMoveMedianTick` / `P90` | 200 / 400 | The bar moves inside 10 s in the median fight, inside 20 s in nine of ten |
| `singleHitShareMax` | 1500 | No single resolution moves the bar more than 15% |
| `liveLedgerLinesPerSecondP95` / `Fail` | 4 / 6 | The D-08 budget; warn above four, fail above six |
| `demolitionsPerRunMedianMax` | 1000 | One demolition per run, median — rooms are commitments |
| `relocationsPerRunMedianMax` | 2000 | Two relocations per run, median — the valve is not a habit (warn) |
| `selectorDensityMin` from round 8 | 500 | Half of late rivals carry a floor-selected status |
| `abilityShareOfWinnerPushP50Max` | 500 | No one card is most of a win |
| `deadContentPickRate` | 20 | Below 2% pick rate is a review flag |
| `simulateMedianMs` | 5 | The ARCHITECTURE §11 budget |

The archetype band is deliberately wide. A 42–58 band on six archetypes means the
harness tolerates a real best archetype at any moment; what it does not tolerate is
one that wins six in ten against everything. Narrowing the band is a decision for
after the first hundred nightly reports, not before.

---

## 5. Archetypes and the counter web

### 5.1 The six

| Archetype | Core | Wants | Fears |
| --- | --- | --- | --- |
| **generalist** | Open Plan Engineering, a Paralegal, a Sales Rep | Nothing in particular | Nothing in particular. The median |
| **turtle** | Legal Departments, HR restore, Reception full of Paralegals | Time | Morale — it ignores the cap |
| **burst** | Server Room Architects, DevOps Overtime, Team Leads | Overflow timing | Legal cap and regen; Bureaucracy on the Architect |
| **economy** | Sales Floors, income, chip that never stops | Late rounds | Burst before the income matters |
| **burnout** | Consultants, Headhunters, Training Rooms, HR to clean up | A stacked floor to Review | A retrigger core with cleanse |
| **management** | Boardroom Directors, Middle Managers, a few strong pieces to retrigger | Adjacency | Economy's suppression and scaling |

### 5.2 The web

The brief's triangle — Legal turtles beat burst, burst beats economy, Burnout pierces
turtles — is the starting point. Six archetypes need a web, not a triangle, and the
harness asserts each edge inside the `counterPair` band:

| Winner | Loser | Why |
| --- | --- | --- |
| turtle | burst | Legal cap and regen outlast a build whose damage arrives in bursts |
| burst | economy | Economy scales into rounds it does not survive |
| burnout | turtle | Morale ignores Goodwill and erodes the cap the turtle paid for |
| management | burnout | Retriggers with HR cleanse out-tempo a slow Burnout stack |
| economy | management | Chip that never stops suppressing regen, plus income that outgrows a retrigger core |
| generalist | none | The generalist is the median, not a counter; it sits inside the band against everyone |

The generalist has no edge in either direction: it is the archetype the band is
measured around, and `inv.archetype_band` is what keeps it honest.

### 5.3 Bosses

Bosses are held to `inv.boss_counters`, not to the archetype band — they are meant to
be lopsided. Each names the archetype that should beat it and, where the lesson has a
wrong answer, the archetype that should not:

| Boss | Favoured | Punished |
| --- | --- | --- |
| `boss_regional_rival` | generalist ≥ 500‰ | — |
| `boss_compliance_office` | burnout ≥ 600‰ | burst ≤ 300‰ |
| `boss_parent_company` | burnout ≥ 450‰ | burst ≤ 300‰ |

The Regional Rival has no punished archetype because its lesson is that the ledger is
readable, not that a build is wrong. The Compliance Office punishes burst because
"more Push" is the intuitive wrong answer to a turtle. The Parent Company punishes
burst because a concentrated tower is what its gimmick exists to hurt.

---

## 6. The invariants

Eighteen, each an entry in `content/balance.json` with a population, a measure, a
comparator, a threshold, a cadence and a severity. The harness reads the file; a new
invariant is a content edit plus a measure implementation, never a spec change.

| Invariant | Name | Population | Cadence | Severity |
| --- | --- | --- | --- | --- |
| `break_guaranteed` | No defence survives to the Bell | strongest_defence vs median_attacker | smoke+nightly | fail |
| `bar_moves_early` | Fights do not open flat | field vs field | smoke+nightly | fail |
| `archetype_band` | No archetype dominates the field | each archetype vs field | smoke+nightly | fail |
| `counter_pairs` | Counters exist and are not walls | counters | nightly | fail |
| `mirror_parity` | Mirrors are fair | mirror | nightly | fail |
| `fight_length` | Fights end in the window | field vs field | smoke+nightly | fail |
| `chip_cannot_suppress` | Chip cannot hold regen down alone | static: employees x rounds offered | commit | fail |
| `single_hit_cap` | No one hit claims the bar | field vs field | smoke+nightly | fail |
| `rider_net_negative` | The portal is a gamble, not an upgrade | field with substitution vs field | nightly | fail |
| `demolition_rare` | Rooms are commitments | agent_run | nightly | fail |
| `relocation_rare` | Relocation is a valve, not a habit | agent_run | nightly | warn |
| `selector_density` | Floors stay a decision | field per round >= 8 | nightly | fail |
| `ledger_rate` | The live ledger stays readable | field vs field | nightly | warn+fail |
| `ability_diversity` | Wins are not one card | field vs field, winners | nightly | fail |
| `boss_counters` | Bosses teach what they are meant to | bossCounters | nightly | fail |
| `template_constructible` | Rivals are affordable | templates x rounds x seeds | commit | fail |
| `dead_content` | Nothing is never picked | optimizer | nightly | warn |
| `sim_budget` | The sim stays fast | field at round 16 | commit | fail |

One entry, verbatim, so the shape is clear:

```json
{
  "id": "inv.break_guaranteed",
  "name": "No defence survives to the Bell",
  "statement": "Against median_attacker, the strongest_defence build's Goodwill reaches 0 before the Bell in every seed.",
  "population": "strongest_defence vs median_attacker",
  "measure": "max over seeds of first tick at which defender goodwill == 0",
  "comparator": "<",
  "threshold": 1160,
  "cadence": "smoke+nightly",
  "severity": "fail",
  "source": "DESIGN_BRIEF risk 3; D-05"
}
```

The three the planning prompt required are `break_guaranteed`, `bar_moves_early` and
`archetype_band`. The rest are the guards promised in earlier decisions — every
"`BALANCE_PLAN` asserts …" in `GAME_DESIGN.md`, `SIMULATION_SPEC.md` and the decision
log resolves to a row above. `chip_cannot_suppress` and `template_constructible` are
static and run on every commit; the searches run nightly.

**Severity.** `fail` blocks the push (smoke) or opens an issue (nightly). `warn` is a
review flag: `dead_content` never fails, because unpopular content is a design
question, not a defect. `ledger_rate` warns at the design budget and fails above it,
because a ledger at six lines a second is not a balance problem, it is an illegibility
problem, and content that causes it is wrong regardless of its win rate.

---

## 7. Detecting a broken build

A broken build is one the harness can find and a player can too. Four detectors, in
order of how early they fire:

1. **Static.** `chip_cannot_suppress` and the schema itself. A content edit that
   creates a 40-tick ability at suppression-threshold value fails the commit without
   running a single match.
2. **The bands.** `archetype_band` and `counter_pairs` on smoke. A change that pushes
   one template outside its band is caught on the push that made it.
3. **The searches.** `optimizer` nightly: if any found build exceeds 650‰ against
   the field, the report names it, its shopping list, and the ten entities with the
   highest marginal contribution — computed by re-running with each removed. That
   list is the nerf candidate list, ranked.
4. **Composition.** `ability_diversity` and `single_hit_cap`. A build can sit inside
   the win-rate band and still be one card; these are the detectors for a win that is
   not a build.

What the harness does *not* do is fix anything. Automated tuning — an optimizer
adjusting numbers to satisfy the invariants — is deliberately out of scope for v1,
because it converges on numbers nobody can explain. The harness finds, a human
decides, and §8 is the process.

---

## 8. The tuning process

One loop, run by a human with the harness as instrument:

1. **A signal.** A failing invariant, a warning, a nightly heatmap that looks wrong,
   telemetry (§10), or a playtest note.
2. **Reproduce.** Name the population, round and seed; run the harness on that alone;
   read the ledger of one representative match. The autopsy exists for developers too.
3. **One knob.** Change **one** number in `content/`, in this order of preference,
   stopping at the first that plausibly fixes it:
   1. **Cost** — the cheapest, most legible lever; a card that wins too much should
      cost more before it does less.
   2. **Cooldown** — changes tempo without changing what a card is.
   3. **Value** — the last numeric lever, because it changes what the card reads as.
   4. **New content** — a counter, if the problem is a missing answer rather than a
      wrong number. This is a Phase 3 edit with a fixture.
   5. **A rule** — last, because it is a `SIMULATION_SPEC` change, a schema version
      bump, and a fixture regeneration. Rules change when the numbers have run out,
      not before.
4. **Patch bump.** `contentVersion` patch; the commit message names the invariant and
   the knob.
5. **Smoke.** The push runs smoke; if it fails, the knob was wrong or a second one is
   needed — and a second one is a second commit.
6. **Nightly.** The full matrix confirms, or opens the next signal.

**One knob per commit** is the rule that makes the history readable. A commit that
changes three numbers cannot be bisected, cannot be reverted cleanly, and cannot teach
anyone what the numbers do.

**Bosses and templates are fixtures.** A knob that changes a rival's strength
re-runs `boss_counters` and `template_constructible`; a boss that stops teaching its
lesson is re-authored in the same change.

---

## 9. What is not a knob

Some numbers are load-bearing for something other than balance, and moving them for
balance breaks that thing. They are listed so the tuning loop skips them:

| Not a knob | Why |
| --- | --- |
| The Quarter Close curve — `pushMult`, `regenMult`, `monthStart` | D-05. The fixed frame every other number is tuned against; per-content or per-round tuning makes the space unsearchable |
| `SHARE_TOTAL`, `SHARE_START` | The bar's scale is the display's scale |
| Tick rate and quarter length | D-21; every fixture and every screen assumes them |
| Grid sizes | D-01; the manifest and every layout assumes them |
| Tenure tier rounds | D-25; the run's shape is built on 3 / 6 / 10 |
| Retrigger depth | D-10; a rules constant, not a balance one |

`SP_PER_PUSH_PERMILLE` — the conversion table — *is* a knob, and the main one for
fight length: it moves `fight_length` without touching any card.

---

## 10. Telemetry

**v1: local, opt-in, file-based.** With the setting on, every fight appends one line
to `telemetry/matches.jsonl`: `contentVersion`, mode, round, both archetypes (the
player's inferred by the expander's classifier, the rival's known), both snapshot
hashes, winner, endTick, first share move, Bell or not, the three autopsy findings,
and the `stateHash`. Nothing identifying. The player can open the folder.

A `tools/telemetry` command aggregates a folder of these into the same
`balance_report.json` shape the harness emits, so real play and simulated play are
compared on the same axes: where they disagree is where the templates are not modelling
what players build, and the fix is a template edit.

**Post-v1: server-side**, with ranked (`ARCHITECTURE.md` §12). Same line format,
same aggregation, plus rating.

The harness is the instrument for *can this be broken*; telemetry is the instrument
for *is it being broken*. Both are needed, and neither judges fun.

---

## 11. What the harness cannot judge

`GAME_DESIGN.md` §21 lists the questions only a person can answer, each with the
moment it becomes answerable. Balance has three more, and the plan is to ask them at
the vertical slice and again at every nightly heatmap review:

| Question | Signal it is wrong |
| --- | --- |
| Does a fight inside every band still *feel* flat? | The bar moves on schedule and nobody cares. Then the issue is presentation, not numbers, and the fix is in the battle screen |
| Is the counter web *legible* from the build screen? | You beat a turtle with Burnout and cannot say why. Then the dossier or the card text is wrong |
| Does the 42–58 band hide a best archetype everyone plays? | Telemetry pick rates are lopsided while the harness bands are green. Then narrow the band, or the templates are not what players build |

A green harness is necessary. It is not sufficient, and this plan does not pretend
otherwise.
