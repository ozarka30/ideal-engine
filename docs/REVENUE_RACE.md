# Company Wars — The Revenue Race

Status: **approved as D-85.** Stage 1 — this document, the decision, and `GAME_DESIGN.md`
§2, §6.4 and §11 — is done. Stages 2–4 (§7) are each approved before they start. Until
stage 2 lands, `SIMULATION_SPEC.md` and the code still run the Goodwill fight; this document
is the rule for the race until its rules are folded into the spec.

## 1. The goal in one line

**The firm that makes the most money this quarter wins.**

Every mechanic in the fight is either a way to make money, a way to take or cost the rival
money, or a way to protect your own. A player who reads the word on a card should know
which of the three it is.

Today that is not true. A firm cannot earn anything without hitting the rival first: Push
must drain the rival's Goodwill before it moves the shared Market Share bar, and the bar is
a tug-of-war, not money. This proposal separates earning from attacking and makes the score
a number of yen.

## 2. The words

| New word | Replaces | What it is | Which of the three |
| --- | --- | --- | --- |
| **Revenue** | Market Share | Each firm's ¥ taken this quarter. Starts at ¥0, never goes below ¥0 | The score |
| **Sales** | Push (as a self effect) | ¥ added straight to your own Revenue | Make money |
| **Client Loyalty** | Goodwill | How firmly your clients stay. Shields your Revenue from Poaching; has a current value and a cap | Protect |
| **Poach** | Push (as an attack) | Drains the rival's Loyalty; once it is empty, every Poach moves ¥ from their Revenue to yours | Take money |
| **Scandal** | Morale | Shrinks the rival's Loyalty cap for the rest of the quarter, and moves ¥ to you at half rate | Take money |
| **Curse** | Anomaly | Moves ¥ from the rival to you straight through their Loyalty; a quarter of it rebounds on your own Loyalty | Take money |
| **PR** | Restore | Rebuilds your own Loyalty | Protect |
| **Clients drift back** | Regen | Loyalty recovers every 2 s, unless you were Poached in the last second | Protect |
| **Quarter-end rush** | Quarter Close multipliers | Sales, Poach, Scandal and Curse are worth more as the quarter closes | Pressure |
| **The Bell** | The Bell | The quarter closes; whoever has more Revenue wins | The score |

The four statuses already speak office and keep their names; their text says what they do
to money: **Burnout** (−5% output per stack; burned-out staff leak clients — a Scandal on
their own firm every second), **Overtime** (+50% work speed per stack), **Bureaucracy**
(−20% work speed per stack), **Frozen** (does no work). **Cleanse** and **retrigger** keep
their meaning.

The build budget stays **Budget ¥**. The fight's score is always labelled **Revenue**, so the
two are never confused: Revenue decides who won the quarter; the board sets next quarter's
Budget from income, as now.

## 3. The rules

Everything not named here is unchanged: the tick loop, cooldowns, initiative, targeting,
the value pipeline, rooms, floors, Tenure, statuses, retriggers, the RNG and the ledger's
shape. This is a change to what the value is *applied to*, not to how it is computed.

### 3.1 Each firm holds

- `revenue` — ¥, a `long`, starts at 0, clamped at 0.
- `loyalty` and `loyaltyCap` — exactly today's `goodwill` and `cap`, with today's per-round
  base (`600 + 100 × round`), passives, portal taxes, regen and suppression.
- `totalSales` — the tie-break, replacing `totalPush`.

### 3.2 The kinds

The value `v` comes out of today's seven-step pipeline, including the month multiplier.

| Kind | Target | Effect |
| --- | --- | --- |
| `sales` | Own firm | `own.revenue += v; own.totalSales += v` |
| `poach` | Rival | Today's `applyPush` against the rival's Loyalty, including suppression. The overflow is a **transfer**: `taken = min(overflow, rival.revenue)`; `rival.revenue -= taken; own.revenue += taken` |
| `scandal` | Rival | Today's `applyMorale`: `rival.loyaltyCap -= raw` (floor 1, Legal Tier III protection as now), then transfer `floor(raw / 2)` from the rival to you |
| `curse` | Rival | Transfer `v` from the rival to you, ignoring Loyalty. Then `floor(v / 4)` (less the Summoning Circle and Ofuda reductions, as now) is applied as a Poach *against your own Loyalty*, whose overflow transfers your Revenue to the rival — today's self-cost rule |
| `pr` (was `restore`) | Own firm | Unchanged: `loyalty = min(cap, loyalty + v)` |
| `status`, `cleanse`, `retrigger` | As now | Unchanged |

A transfer never takes more than the rival holds; what it could not take is lost, and the
ledger records both the amount attempted and the amount taken.

Burnout's periodic event (today §11.2) is a Scandal a firm deals to itself: the cap shrinks
and half the raw amount transfers to the rival. Unchanged in effect.

There is no conversion table any more. `SP_PER_PUSH_PERMILLE`, `SHARE_TOTAL`, `SHARE_START`
and the Share Point carry are deleted: Revenue is counted in the pipeline's own units, and
because both firms fight in the same round the scale is always fair.

### 3.3 How it ends

The quarter always runs to the Bell. At tick 1199:

1. More Revenue wins.
2. Equal: more Loyalty remaining wins.
3. Equal: more total Sales wins.
4. Equal: a draw, interpreted by the mode as now.

There is no early finish (§8, O-1).

### 3.4 What it should feel like

Month 1 is two engines starting: Sales ticking up both totals, the first Poaches chipping
Loyalty. Month 2 is the first break — one firm's Loyalty empties and its Revenue starts to
walk. Crunch is the swing. The Bell is two seconds at triple value, and a firm behind on
Revenue can still take the quarter with a well-timed Poach or Curse (D-23's comeback, now in
yen).

## 4. What each department does

Every firm needs a way to make money and a way to reach the rival. The roster today has 15
Push abilities (Engineering, Legal, Sales), 10 Anomaly (extraplanar), 7 status, 3 Restore,
3 retrigger and 2 cleanse. The content stage reassigns them by department:

| Department | Mostly | Because |
| --- | --- | --- |
| Engineering | **Sales** — product revenue | They build what sells |
| Sales | **Sales**, with a few **Poach** — winning the rival's clients | The deal-makers |
| Legal | **Poach** with Bureaucracy — lawsuits that pry clients loose and tie the rival up | Cease & Desist already reads this way |
| HR | **PR**, cleanse, and Burnout on the rival | Keeping your people, draining theirs |
| Management | Retriggers and statuses | Making the earners work harder |
| Extraplanar | **Curse** | The occult deal: big, direct, and it costs you |

The founders' future business types (D-84) fit this without new rules: a law firm leans on
Poach, a tech firm on Sales, an occult firm on Curse — by what their shops and floors favour.

## 5. The counter web, redrawn

The six archetypes keep their slots and change their meaning. The harness re-tunes the
numbers; the edges are the proposal:

| Archetype | Was | Core | Wants | Fears |
| --- | --- | --- | --- | --- |
| **generalist** | generalist | Some Sales, a Poacher, a PR | Nothing in particular | Nothing in particular |
| **earner** | economy | Engineering and Sales on Sales Floors | A quiet quarter | Raiders — its pile is the prize |
| **fortress** | turtle | Legal, HR PR, Reception full of Paralegals | Poachers | Scandal — it ignores Loyalty |
| **raider** | burst | Poachers timed for the rush | A rival with Revenue and no Loyalty | Fortress Loyalty and regen |
| **scandal** | burnout | Consultants, Headhunters, Training Rooms | A rival that paid for Loyalty | Cleanse and tempo |
| **management** | management | Directors retriggering earners | Adjacency | Raiders and scandal |

| Winner | Loser | Why |
| --- | --- | --- |
| raider | earner | The earner has the Revenue and none of the Loyalty to keep it |
| fortress | raider | Loyalty and regen soak Poaches that arrive in bursts |
| earner | fortress | Loyalty does not stop Sales; the fortress makes too little |
| scandal | fortress | Scandal erodes the cap the fortress paid for |
| management | scandal | Retriggers and HR cleanse out-tempo a slow Burnout stack |
| earner | management | Sales that never stop outgrow a retrigger core |

## 6. What this supersedes

- **D-30** (Market Share is 10,000 points) — replaced by Revenue in ¥.
- **D-07** (three damage kinds, pierce a property of kind) — the kinds become Sales, Poach,
  Scandal and Curse; which pierce Loyalty is still a property of the kind.
- **D-35** (Goodwill shown as a per-side bar) — the bar is Client Loyalty; the battle screen
  adds each firm's Revenue and a lead bar showing each firm's share of the quarter's takings.
- **D-23** (comebacks exist) stands, restated in yen.
- **D-04** (regen cadence), **D-05** (the Quarter Close curve, not a knob) and **D-06**
  (overflow carries in full) stand; D-06 now describes the Poach transfer.
- The fight half of **D-53**'s sign-off is reopened for this change and closed again by D-85.

## 7. The stages

Each is approved before the next.

1. **This document**, then D-85 and the rewritten `GAME_DESIGN.md` §2, §6.4 and §11.
2. **The sim.** `SIMULATION_SPEC.md` §3.2, §9.3, §10, §11.2, §15 and the §20 worked trace;
   `CompanyWars.Sim`; the schema's effect vocabulary. The ten conformance fixtures are
   re-recorded — agents cannot, so this needs the owner's `fixtures-approved` label.
3. **Content and balance.** The roster, rooms, furniture, riders and modifiers rewritten in
   the new kinds (`tools/planning/gen_content.py`); `BALANCE_PLAN.md` §5–§6 — the web above,
   and the invariants that assume Goodwill (`break_guaranteed`, `bellRateMax`,
   `strongest_defence`) restated for a race; the harness re-tuned one knob per commit.
4. **The screens.** The battle screen's bars, the ledger and autopsy wording, the build
   screen's primer and card text, and the screenshot fixtures from CI.

## 8. Decisions taken

| # | Question | Decided |
| --- | --- | --- |
| O-1 | Should a quarter end early — a **Buyout** when one firm's lead passes a threshold? | **No early finish** (owner). Every quarter runs the full 60 s and more Revenue at the Bell wins; `bellRateMax` is retired |
| O-2 | Rename the effect ids themselves, or only the words players see? | **Rename the ids** (owner), everywhere: `push` → `sales` or `poach` by what the ability does, `morale` → `scandal`, `anomaly` → `curse`, `restore` → `pr`; `goodwill` → `loyalty` in every stat and field (`goodwillCap` → `loyaltyCap`); `totalPush` → `totalSales` |
| O-3 | Is the fight's score shown in ¥, like the budget? | **¥, labelled Revenue** (owner). The label keeps it apart from Budget |
| O-4 | When a Poach overflows a rival with no Revenue left, is the rest lost or owed? | **Lost**, as recommended; the owner may still reverse it. A debt is a new rule for a rare case |
