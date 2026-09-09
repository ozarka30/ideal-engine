# Company Wars — Decision Log

Status: **append-only**. Phase 1 output, alongside
[`OPEN_QUESTIONS.md`](OPEN_QUESTIONS.md).

This is the record of decisions taken during design, with the reasoning compressed to
the line that actually did the work. It exists so that a reader with no memory of the
conversation — a coding agent, a future contributor, the author in six months — can
find out *why* something is the way it is without re-deriving it or, worse,
"improving" it back to the option that was already rejected.

**Append-only.** Entries are never edited or deleted. A reversal is a new entry that
names what it supersedes, and the superseded entry gets a `Superseded by` line. The
history of a reversal is the most useful thing in a document like this.

**Authority.** Every entry records who the call belonged to:

- **Craft** — data structures, ordering rules, formats, methodology. Made by the
  designer under the planning prompt's grant of authority to make such calls without
  asking. Reversible, but currently taken.
- **Human** — tone, feel, scope, what the game is about. Recorded here only once the
  human has signed off. Until then it lives in `OPEN_QUESTIONS.md` as
  `NEEDS SIGN-OFF`.
- **Locked** — from `DESIGN_BRIEF.md`, predating this log. Not relitigated here.

---

## Index

| # | Decision | Authority | Question |
| --- | --- | --- | --- |
| [D-01](#d-01) | Five floor slots; 5x3 standard, asymmetric Executive and basement | Craft | Q-STR-1 |
| [D-02](#d-02) | Floors are purchased, expensive, and carry per-round upkeep | Craft | Q-STR-2 |
| [D-03](#d-03) | Per-ability resolution tagged by floor; aggregation is a view | Craft | Q-GW-1 |
| [D-04](#d-04) | Goodwill regenerates in discrete 2s ticks, suppressed 1s after a hit | Craft | Q-GW-2 |
| [D-05](#d-05) | Quarter Close is a four-phase step function, global and untunable | Craft | Q-GW-3 |
| [D-06](#d-06) | Overflow carries in full, uncapped | Craft | Q-GW-4 |
| [D-07](#d-07) | Pierce is a property of damage kind; exactly three kinds exist | Craft | Q-GW-5 |
| [D-08](#d-08) | One event array; ledger, replay and autopsy are views over it | Craft | Q-GW-7 |
| [D-09](#d-09) | Floor identity uses four light levers, not one heavy one | Craft | Q-FLR-1 |
| [D-10](#d-10) | Floor targeting is a closed selector vocabulary with specified tie-breaks | Craft | Q-FLR-2 |
| [D-11](#d-11) | The elevator's landing column provides scarce vertical adjacency | Craft | Q-FLR-3 |
| [D-12](#d-12) | Rooms are zones; furniture stays, equipment is cut | Craft | Q-LYR-1 |
| [D-13](#d-13) | Employees carry nothing; `attachments[]` is reserved and always empty | Craft | Q-LYR-2 |
| [D-14](#d-14) | Nine required manifest fields; overhang is derived, absence is valid | Craft | Q-GBX-1 |
| [D-15](#d-15) | Draw order is a five-key total order with a required `sortBias` | Craft | Q-GBX-2 |
| [D-16](#d-16) | "Art complete" is reachability-based; tiers rank, they do not exempt | Craft | Q-GBX-4 |
| [D-17](#d-17) | Crafting consumes inputs and is reversible only within the build round | Craft | Q-RCP-2 |
| [D-18](#d-18) | The tower snapshot format, and the pure `simulate()` entry point | Craft | Q-PVP-1 |
| [D-19](#d-19) | Ranked matchmaking buckets by round and rating; rating lives outside the snapshot | Craft | Q-PVP-2 |
| [D-20](#d-20) | Anti-cheat is server re-simulation plus snapshot legality validation | Craft | Q-PVP-3 |
| [D-21](#d-21) | 16 fights across three acts, a 60s quarter, three lives | Human | Q-STR-3 |
| [D-22](#d-22) | Branching campaign map, five node types, three thesis bosses | Human | Q-STR-4 |
| [D-23](#d-23) | Comebacks exist — the bar travels freely back through the centre | Human | Q-GW-6 |
| [D-24](#d-24) | Reconstruction is painful; rewards for good commitment scale to match | Human | Q-RISK-2 |
| [D-25](#d-25) | Tenure — rooms compound while they stay put and stay staffed | Craft | Q-ECO-1 |
| [D-26](#d-26) | 640 × 360 logical canvas at integer scale | Craft | Phase 2 |
| [D-27](#d-27) | Fixed-point permille arithmetic; ordered multiplier chain, floor after each step | Craft | Phase 2 |
| [D-28](#d-28) | mulberry32, seeded once, consumed only by the two random selectors | Craft | Phase 2 |
| [D-29](#d-29) | Simultaneous resolutions alternate side priority by tick parity | Craft | Phase 2 |
| [D-30](#d-30) | Market Share is 10,000 points with a per-firm remainder carry | Craft | Phase 2 |
| [D-31](#d-31) | Interludes do not advance the round; unpaid upkeep is paid in Goodwill cap | Craft | Phase 2 |
| [D-32](#d-32) | Push has no target; floor selectors apply only to employee-affecting effects | Craft | Phase 2 |
| [D-33](#d-33) | One entry per event, tagged source and target; the replay is the result | Craft | Phase 2 |
| [D-34](#d-34) | Equipment is cut for good; furniture stays in v1 on trial with a named test | Human | Q-LYR-1, Q-LYR-3 |
| [D-35](#d-35) | Goodwill is shown as a per-side bar with its number; the frame erodes with the cap | Human | Phase 2 |
| [D-36](#d-36) | Campaign player rules equal ranked; the difference is scripted and semi-scripted rivals with gimmicks | Human | Phase 2 |
| [D-37](#d-37) | Content is a closed effect vocabulary; adding a word is a code change, adding an entity is not | Craft | Phase 3 |
| [D-38](#d-38) | One JSON file per content type, one JSON Schema, an index with the content version | Craft | Phase 3 |
| [D-39](#d-39) | Semi-scripted rivals are expanded from templates by a deterministic, specified algorithm | Craft | Phase 3 |
| [D-40](#d-40) | The loader validates schema, references, snapshot structure and constructibility in CI | Craft | Phase 3 |
| [D-41](#d-41) | The manifest is generated from content and screen specs; overhang derived; five entries flagged for pack-fit | Craft | Phase 4 |
| [D-42](#d-42) | One-direction package graph with the sim at the root, enforced by lint | Craft | Phase 4 |
| [D-43](#d-43) | The build phase is a pure reducer; undo is action-log replay; the committed tower is the snapshot type itself | Craft | Phase 4 |
| [D-44](#d-44) | Art and licence gates live only in the release workflow | Craft | Phase 4 |
| [D-45](#d-45) | Steam sits behind a Platform interface; the Rust side owns Steamworks; the webview never links it | Craft | Phase 4 |
| [D-46](#d-46) | A chosen founder avatar, cosmetic in v1, carried in the snapshot with an empty effects list | Human | Phase 4 |
| [D-47](#d-47) | Controller navigation is post-v1; focusable elements stay enumerable from the first screen | Human | Q-ARCH-1 |
| [D-48](#d-48) | Paper wireframes deferred; the greybox vertical slice is the UX milestone | Human | Q-UX-1 |
| [D-49](#d-49) | The vertical slice is the ranked-shaped sixteen-round loop; the campaign map comes after it | Craft | Phase 5 |
| [D-50](#d-50) | Balance invariants are content the harness reads; one knob per commit; the pressure curve is not a knob | Craft | Phase 5 |
| [D-51](#d-51) | Relocate: move a room for the fee and three Tenure rounds; Restructuring becomes one free relocation | Human | Q-ECO-2 |
| [D-52](#d-52) | No free Restructuring; paid relocation is the only recovery valve | Human | Q-ECO-2 |
| [D-53](#d-53) | The design phase is signed off; implementation begins at ROADMAP M0 | Human | — |
| [D-54](#d-54) | The shop draws from a per-tab bag without replacement | Craft | Research |
| [D-55](#d-55) | No screen may require text input | Craft | Research |
| [D-56](#d-56) | A store-ready art gate on visibility tiers 1–2, ahead of art complete | Craft | Research |
| [D-57](#d-57) | `inv.standing_pat_loses`: Tenure alone must lose to active spending | Craft | Research |
| [D-58](#d-58) | Five strikes per run, both modes | Human | Q-STR-5 |
| [D-59](#d-59) | Tauri is dropped; the stack is re-selected from research | Human | Q-TECH-1 |
| [D-60](#d-60) | Godot 4 with C#; the sim as a plain .NET library; Compatibility renderer; X11; GodotSteam | Human | Q-TECH-1 |
| [D-61](#d-61) | The mulberry32 listing corrected to the canonical algorithm; the C# listing is normative | Craft | — |
| [D-62](#d-62) | Within a tick, readiness is judged before cooldowns advance: phases run A, B, D, C, E, F | Human | — |
| [D-63](#d-63) | No browser build: Godot 4 cannot export C# to the web; desktop nightly builds are the playtest channel | Craft | Q-TECH-1 |
| [D-64](#d-64) | Undo and Drop are buttons as well as keys; the battle controls move off founder B's badge; the fallback pixel font is baked from DejaVu Sans | Craft | UX review |

Forty-seven craft decisions and seventeen human calls taken. Eight items remain open in
[`OPEN_QUESTIONS.md`](OPEN_QUESTIONS.md); one blocks a Phase 4 greybox, one is a
vertical-slice playtest gate.

---

## D-01

**Five floor slots — `G`, `1F`, `2F`, `3F` (Executive), `B1` (Portal). Standard
floors are 5x3; Executive is 4x2; B1 is 3x3. A run starts with `G` and `1F`.**

*Why:* The binding constraint is ledger legibility, not space — sizing backwards from
"a human can follow this fight" gives a late-run roster of 12–16 employees, and 62
maximum tiles is what produces that. Asymmetric floor shapes generate per-floor
identity for free, before a single stat is involved.

*Consequence:* Rooms must be predominantly 2x2 and smaller; 3x3 rooms cannot exist on
standard floors.

Authority: Craft · Question: [Q-STR-1](OPEN_QUESTIONS.md#q-str-1--how-many-floors-and-what-grid-size)

---

## D-02

**Floors are purchased, cost roughly three rounds of income, and carry per-round
upkeep. Three are purchasable per run.**

*Why:* A floor has to be the game's tempo-versus-scaling decision, and it can only be
that if buying one visibly weakens the next two or three fights; upkeep is what stops
"buy everything" from being trivially correct.

*Consequence:* Over-leasing is a real losing line, so the purchase UI must show
projected upkeep before committing.

Authority: Craft · Question: [Q-STR-2](OPEN_QUESTIONS.md#q-str-2--what-is-the-floor-expansion-curve)

---

## D-03

**Every ability resolves individually and immediately, tagged with
`(floor, employee, ability)`. Floor multipliers and room auras apply at resolve time.
Per-floor aggregation is a renderer-side view, never a sim step.**

*Why:* A floor-buffered design would make the battle presentation easier and destroy
causality in the ledger — "Floor 3 dealt 900" does not answer "why did I lose", and
answering that question is the design's central promise.

*Consequence:* The sim emits 6–12 events/second in a late fight, pushing the
readability problem into the live view, where
[D-08](#d-08) handles it. Multiplier order is fixed, with a single rounding step at
the end so no float reaches the ledger.

Authority: Craft · Question: [Q-GW-1](OPEN_QUESTIONS.md#q-gw-1--how-does-floor-output-aggregate-into-goodwill-damage-and-then-into-the-bar)

---

## D-04

**Goodwill regenerates as a discrete ledger entry every 40 ticks (2s), suppressed if
that side took Push in the preceding 20 ticks (1s). Excess above cap is discarded.**

*Why:* Continuous regen is invisible, and an invisible defensive mechanic fails the
exact problem the ledger was invented to solve — a discrete, named, positive entry
every two seconds is what makes turtling look like something happening.

*Consequence:* Regen timing is gameable by cheap chip damage, so a minimum-Push
suppression threshold is required and belongs in `BALANCE_PLAN` as an assertion.
Discrete events are also countable in CI, which a continuous integral is not.

Authority: Craft · Question: [Q-GW-2](OPEN_QUESTIONS.md#q-gw-2--does-goodwill-regenerate)

---

## D-05

**The Quarter Close curve is a four-phase step function over a 1,200-tick quarter —
Month 1 (push x1.0 / regen x1.0), Month 2 (x1.4 / x0.6), Crunch (x2.0 / x0.2), Bell
(x3.0 / x0.0). It is a global constant that no content may modify.**

*Why:* This curve is the fixed frame every other number is tuned against; the moment
it becomes per-content data the balance space stops being searchable, and the
guarantee it exists to provide — that no defence survives to the bell — stops being
provable.

*Consequence:* Three discrete power spikes, so each transition must be telegraphed a
second early. Applying the curve to regen as well as push is what makes the guarantee
hold against the *strongest* defence rather than the average one.

Authority: Craft · Question: [Q-GW-3](OPEN_QUESTIONS.md#q-gw-3--what-is-the-shape-of-the-quarter-close-pressure-curve)

---

## D-06

**Overflow carries into the Market Share bar in full, uncapped and untaxed.**

*Why:* The ledger's value is that it is arithmetic the player can check, and a capped
or discounted overflow produces a line whose number does not match what the player
watched happen.

*Consequence:* A single large hit can end a near-parity fight, so burst must be
constrained through visible cooldowns and costs, guarded by a `BALANCE_PLAN`
assertion bounding any single resolution as a fraction of bar capacity.

Authority: Craft · Question: [Q-GW-4](OPEN_QUESTIONS.md#q-gw-4--does-overflow-carry)

---

## D-07

**Piercing is a property of the damage kind, not the ability. Three kinds exist:
Push (default, buffered, overflows), Morale (pierces, moves the bar at a reduced
rate, lowers Goodwill cap and regen), Anomaly (pierces at full rate, costs the
attacker Goodwill).**

*Why:* A per-ability pierce flag proliferates until Goodwill is a rounding error;
binding pierce to a kind caps the design space structurally instead of relying on
authoring discipline.

*Consequence:* Morale also solves the dead-air risk — the bar moves from tick 1
whenever either side fields Burnout. Cards carry three numbers and the ledger three
colours, which is the UI cost of the mechanic being meaningful.

Authority: Craft · Question: [Q-GW-5](OPEN_QUESTIONS.md#q-gw-5--what-pierces-goodwill)

---

## D-08

**The sim emits one full-fidelity, tick-ordered event array. The live ledger, the
replay, the scrub timeline and the per-floor breakdown are all views over it. The live
view coalesces same-`(source, ability, kind)` entries within 1.0s and is capped at
4 new lines per second.**

*Why:* The brief requires the live defensive readout and the post-battle autopsy to be
one component, and the only way to be both is to record everything and filter at the
view — anything summed away in the sim is unrecoverable for diagnosis.

*Consequence:* The 4-lines/second budget is a constraint on content design as well as
UI; a build that routinely exceeds it is illegible and `BALANCE_PLAN` should say so.
There is exactly one serialisation format for combat history.

Authority: Craft · Question: [Q-GW-7](OPEN_QUESTIONS.md#q-gw-7--what-does-the-ledger-show-and-at-what-granularity)

---

## D-09

**Floor identity comes from four light levers used together — output multiplier, room
legality, exposure to floor-targeting, and per-round upkeep. Reception additionally
grants flat Goodwill proportional to occupancy.**

*Why:* Any single strong lever collapses into one correct stacking pattern; four weak
ones interact, and the Executive floor having the best multiplier *and* the smallest
grid *and* the highest upkeep *and* every `highest_floor` ability aimed at it is what
makes stacking a punishable choice rather than a free optimum.

*Consequence:* The room catalogue must be authored per-floor and the shop must filter
on owned floors — real content and UI work, without which floors differ only by a
number and will collapse.

Authority: Craft · Question: [Q-FLR-1](OPEN_QUESTIONS.md#q-flr-1--what-makes-each-floor-mechanically-distinct)

---

## D-10

**Floor targeting uses a closed vocabulary of seven selectors, each a pure function of
the opponent's snapshot, with tie-breaks specified as part of the rules: fewest
employees, then lowest floor index, then lowest instance id. A selector with no legal
target emits a `whiff` entry rather than being skipped.**

*Why:* Targeting is a rules concern; leaving it expressible in content data would make
every new card a potential determinism bug, and unspecified tie-breaks are the classic
way two implementations of the same spec disagree.

*Consequence:* Adding a selector is deliberately a code change. Floor assignment only
stays a real decision if selectors are dense enough in the rival pool to punish
concentration — a `BALANCE_PLAN` assertion, not a hope.

Authority: Craft · Question: [Q-FLR-2](OPEN_QUESTIONS.md#q-flr-2--how-do-floor-targeting-abilities-work)

---

## D-11

**The leftmost column of each floor is a landing column; its tiles are adjacent to the
landing tiles of the floors directly above and below.**

*Why:* Without it the tower has no vertical adjacency at all and every synergy is
trapped on its own floor, which makes the building an organisational chart rather than
a board; a single scarce column gives Middle Management somewhere to live and makes
floor *ordering* matter, while staying drawable in a top-down view.

*Consequence:* Landing tiles become contested and risk every optimal build looking
identical down the left-hand side — countered by making some strong rooms illegal on
the landing column.

Authority: Craft · Question: [Q-FLR-3](OPEN_QUESTIONS.md#q-flr-3--does-the-elevator-do-anything)

---

## D-12

**Rooms are zones drawn over tiles, not objects consuming them. Furniture occupies
tiles inside rooms and competes with employees for them. Equipment does not exist in
v1.**

*Why:* Furniture creates the central scarcity — a tile is either output or support,
never both — for the cost of one placement rule, which is the best depth-per-complexity
trade available; equipment would add a fourth layer and a second inventory screen for
customisation that promotions already provide.

*Consequence:* This is the answer to the brief's layer-bloat risk, and it is cut now
rather than deferred, because designing around a hole is worse than not having the
feature. Recipes take furniture, not equipment, as their third input, and promotions
must carry the customisation weight — which raises the required recipe count.

Authority: Craft · Question: [Q-LYR-1](OPEN_QUESTIONS.md#q-lyr-1--is-furniture-a-distinct-layer-in-v1)

---

## D-13

**Employees carry nothing. The employee schema nonetheless carries a serialised,
always-empty `attachments[]` from the first commit.**

*Why:* Keeping the field costs one always-empty array; omitting it costs a snapshot
version bump that invalidates every stored ghost on the day equipment is added.

*Consequence:* A small permanent smell in the schema, accepted deliberately as the
cheapest possible insurance on the format that
[D-18](#d-18) makes load-bearing.

Authority: Craft · Question: [Q-LYR-2](OPEN_QUESTIONS.md#q-lyr-2--what-can-an-employee-carry)

---

## D-14

**A manifest entry requires nine fields: `id`, `kind`, `category`, `label`,
`footprint`, `sprite.w/h`, `sprite.anchor`, `sprite.asset`, `sortBias`. Overhang is
derived from those, never authored. An absent asset file is valid; only a dimension
mismatch fails validation.**

*Why:* The brief forbids approximate greyboxes, so an entry that cannot be completed
is a design decision that has not been made — making the fields required turns that
from a review note into a build error. Deriving overhang removes the possibility of
a manifest contradicting itself.

*Consequence:* Stubbing an entity is heavier than it would otherwise be, which is why
the slicer generates conforming stubs — the friction lands on the tool, not the
author. Adding art is a file copy that never edits the manifest.

Authority: Craft · Question: [Q-GBX-1](OPEN_QUESTIONS.md#q-gbx-1--what-is-the-minimum-a-manifest-entry-needs-before-a-greybox-can-be-built)

---

## D-15

**Draw order is the total order `(floorIndex, anchorTileRow, sortBias, tileCol, id)`.
`sortBias` is a required field, default 0, range -10..10, by convention -5 wall-mounted
/ 0 floor-standing / +5 hanging.**

*Why:* Pure y-sorting cannot express a wall-mounted whiteboard drawing behind a desk on
the same row, and a *total* order — the trailing `id` key — is what makes a greybox
screenshot reproducible, and therefore usable as a regression test.

*Consequence:* A hand-tuned bias can be wrong and is invisible until art lands, so the
greybox debug overlay must be able to draw the sort key on each placeholder.

Authority: Craft · Question: [Q-GBX-2](OPEN_QUESTIONS.md#q-gbx-2--what-is-the-draw-order-rule-for-overhanging-sprites)

---

## D-16

**"Art complete" means every manifest entry reachable in a normal campaign run has a
present asset passing dimension validation, nothing renders in the `invalid` tone, and
no screen mixes perspectives. Visibility tiers rank the worklist; they never exempt an
entry. Coverage is emitted by CI from the first commit.**

*Why:* A gate covering literally every entry is unachievable and therefore stops being
believed, while a tiered gate with exemptions means shipping visible placeholders —
reachability is the line that is both strict and reachable.

*Consequence:* The gate is far away and strict, which the worklist ranking mitigates:
the top of the list carries most of the perceived polish, so felt completeness runs
well ahead of measured completeness. It is a release gate on its own `ROADMAP` line,
never a phase dependency.

Authority: Craft · Question: [Q-GBX-4](OPEN_QUESTIONS.md#q-gbx-4--what-defines-art-complete)

---

## D-17

**Crafting consumes its inputs, costs no budget, and is reversible by undo until Ready
is pressed. After Ready it is permanent. Crafting is build-phase only and never occurs
during a fight.**

*Why:* The commitment tension the design needs lives *across* rounds — in rooms and in
severance fees — not in punishing a misclick that looks identical to a strategy; and a
build round that is a pure function from (start state, action list) to (end state) is
testable in a way one with irreversible mid-round side effects is not.

*Consequence:* The drama of an irreversible combine is lost and must be recovered in
presentation — animate every craft at once on commit, as the quarter opens.

Authority: Craft · Question: [Q-RCP-2](OPEN_QUESTIONS.md#q-rcp-2--do-recipes-consume-inputs-and-can-they-be-undone)

---

## D-18

**A tower snapshot contains schema and content versions, round, floors with their
grids, rooms and occupants, and globals (Goodwill max and regen, run modifiers, portal
riders). It excludes budget, shop stock, RNG state, map position and cosmetics. The sim
entry point is `simulate(seed, a, b, rules): MatchResult` — pure, headless,
deterministic.**

*Why:* A snapshot is a combatant, not a save file, and keeping it to exactly what the
sim reads is what lets campaign rivals, ranked ghosts and CI balance fixtures be the
same format with the same entry point — which is the difference between adding ranked
later and rewriting for it.

*Consequence:* Snapshots must be versioned and migrated from day one, including the
authored campaign rivals stored in the repo. That ongoing cost buys a ghost pool that
survives content patches. This is the highest-leverage decision in the deferred set,
because it must be right now and only pays out later.

Authority: Craft · Question: [Q-PVP-1](OPEN_QUESTIONS.md#q-pvp-1--what-is-stored-in-a-tower-snapshot)

---

## D-19

**Ranked ghosts bucket by `(round, ratingBand)`; a match draws deterministically from
the match seed, so any result is re-runnable from `(seed, ghostId, ghostId)`. Fallback
widens rating, then round, then draws from the scripted campaign rival pool. Rating
lives on the ghost record, never in the snapshot.**

*Why:* Recorded now purely to protect the snapshot format — matchmaking needs `round`
in the snapshot and needs rating *out* of it, and discovering either later means a
format migration.

*Consequence:* The campaign shipping first is also the cold-start answer: by the time
ranked exists there are dozens of authored towers per round to seed the pool.

Authority: Craft · Question: [Q-PVP-2](OPEN_QUESTIONS.md#q-pvp-2--how-are-players-matched-once-ranked-exists)

---

## D-20

**Anti-cheat for ranked is server re-simulation using the same headless module, with
the client result treated as advisory. Snapshots are validated for constructibility
before entering the ghost pool. Campaign is deliberately unpoliced.**

*Why:* A client that owns the simulation cannot be trusted with a result and no
obfuscation changes that; the locked requirement that the sim be pure and headless is
exactly what makes running it server-side cheap rather than a second implementation.

*Consequence:* Nothing in v1 must be *built* for this, but nothing in v1 may make it
impossible — keeping `simulate()` pure is the entire obligation. The constructibility
validator is dual-use: it also checks that hand-authored campaign rivals are legal
towers.

Authority: Craft · Question: [Q-PVP-3](OPEN_QUESTIONS.md#q-pvp-3--what-is-the-anti-cheat-posture-given-the-client-owns-the-sim)

---

## D-21

**A run is 16 fights across three acts of 6 / 6 / 4. The quarter is 60 seconds —
1,200 ticks at 20 Hz. Three lives, framed as strikes on a performance review. Build
phase untimed.**

*Why:* 45 minutes is the right size for the depth on offer — long enough that a room
committed in Act 1 still matters in Act 3, short enough that a run lost to a bad
opening does not cost an evening.

*Consequence:* 1,200 ticks is now a fixed constant in `SIMULATION_SPEC`, and the
Quarter Close phase boundaries in [D-05](#d-05) are absolute tick numbers rather than
fractions. 60s is a ceiling, not a target: the median fight should resolve at 35–50s
with the bell as a backstop. Roughly 16 minutes of watching per run means run length
amplifies every readability failure rather than hiding it — which makes the event
budget in [D-08](#d-08) load-bearing rather than tidy.

Authority: Human · Question: [Q-STR-3](OPEN_QUESTIONS.md#q-str-3--rounds-per-run-fight-length-and-lives)

---

## D-22

**The campaign map is a Slay-the-Spire branching DAG with five node types — Hostile
Takeover, Recruiter, Board Meeting, Consultant, Audit — and one boss per act: the
Regional Rival, the Compliance Office, the Parent Company.**

*Why:* Each boss attacks a different assumption rather than carrying bigger numbers —
a mirror that proves the ledger is readable, a turtle unwinnable without piercing, and
a five-floor tower that punishes concentration — so the act structure teaches the three
things a player must understand to be good at the game.

*Consequence:* Three hand-authored rival towers that are balance fixtures as well as
content, and must be re-authored whenever the pressure curve moves. The Act 1 boss
carries the portal unlock, so [Q-PTL-2](OPEN_QUESTIONS.md#q-ptl-2--what-unlocks-the-portal-and-how-does-the-reveal-land)
is now scoped to timing and presentation, not to the trigger.

Authority: Human · Question: [Q-STR-4](OPEN_QUESTIONS.md#q-str-4--what-shape-is-the-campaign-map-and-what-are-its-bosses)

---

## D-23

**The Market Share bar is a single free-travelling position. A trailing firm that
breaks through pushes it back through the centre. No ratchet, no territory held.**

*Why:* The fight is a spectator event the player cannot influence, so uncertainty is
the only thing holding attention — and a ratchet destroys it precisely in the fights
the player most needs to sit and watch, which are the ones they lose.

*Consequence:* A dominant 50-second performance can be erased inside the Bell window
where the push multiplier is 3.0. If that feel-bad bites in playtest, the fix is to cap
Bell-window multipliers, not to adopt the ratchet. Recapture friction (a multiplier on
pushing into held ground) stays on the shelf as the tuning lever if leads prove
meaningless — it is one constant and needs no content change. The bar model also stays
arithmetically trivial, which keeps the replay format and the CI assertions simple.

Authority: Human · Question: [Q-GW-6](OPEN_QUESTIONS.md#q-gw-6--does-the-bar-travel-back-through-the-centre)

---

## D-24

**Demolishing a room refunds nothing, costs a Renovation fee of roughly one round's
income, and forfeits the room's accrued Tenure. To compensate, the rewards for a
correct commitment scale up rather than staying flat.**

*Why:* A decision that can be cheaply undone is not a decision — and locked decision 4
puts the whole strategic identity of the game on rooms being expensive to change, so
the cost has to be felt rather than merely stated.

*Consequence:* The pain is now two-sided, which is the important part: making
reconstruction costly without raising the payoff for getting it right would produce a
game that only punishes. The mechanism for the reward half is
[D-25](#d-25). Because a working room accrues and a failed one does not, the real cost
of demolition scales with how good the room was — tearing out a mistake costs a round,
changing your mind about something that was working costs the run's accumulated
advantage. Early acts must be forgiving in *budget* rather than in demolition cost, and
the whole thing becomes a `BALANCE_PLAN` assertion on median rooms demolished per run
rather than a design intention nobody measures.

Authority: Human · Question: [Q-RISK-2](OPEN_QUESTIONS.md#q-risk-2--is-the-room-commitment-tension-actually-load-bearing)

---

## D-25

**Rooms accrue Tenure for each round they stay in place and stay meaningfully staffed,
gaining a permanent aura step at tiers I / II / III (3 / 6 / 10 rounds held). Tenure is
forfeited entirely on demolition. Steeper room auras are the secondary lever;
steeper recipe results are not used for this purpose.**

*Why:* Tenure is the only candidate that pays for the thing actually being risked —
the cost of a room is that you cannot move it, so making *not moving it* the source of
the reward closes the loop instead of bolting a bonus onto the side of it.

*Consequence:* Tenure is per-match state on the room, so it enters the tower snapshot
([D-18](#d-18)) and authored campaign rivals must declare plausible values — a real
ongoing authoring cost and a new balance knob. Counting rounds *held* rather than
rounds elapsed keeps a late purchase from being worthless. It also widens run-level
outcome variance in both directions, which is what
[Q-ECO-2](OPEN_QUESTIONS.md#q-eco-2--does-a-run-need-a-mid-run-recovery-valve) exists to
guard, and it makes "meaningfully staffed" a definition that must be pinned down
precisely rather than left to judgement.

Authority: Craft · Question: [Q-ECO-1](OPEN_QUESTIONS.md#q-eco-1--how-is-the-reward-for-a-correct-commitment-made-impactful)

---

## D-26

**All screens are laid out on a 640 × 360 logical canvas and rendered at an integer
scale: 2× at 720p, 3× at 1080p, 6× at 4K, letterboxed otherwise.**

*Why:* It is the only common logical size that lands on an integer at both 1080p and
the Steam Deck's 1280 × 800, and 32-pixel tiles at 3× are large enough that a 5 × 3
floor reads from across a room.

*Consequence:* The build view cannot show five stacked floors at once (480 px), so it
shows the selected floor with its neighbours above and below and scrolls by whole
floors — which is also what makes landing-column adjacency visible. 1440p renders at
2× with a border; that is accepted rather than fractional scaling.

Authority: Craft · Phase 2, `GAME_DESIGN.md` §19

---

## D-27

**Every sim quantity is an integer. Multipliers are permille and are applied one at a
time in a fixed order, flooring after each step. No intermediate may exceed 2^53.**

*Why:* Two implementations agree on integer arithmetic and disagree on floating point,
and multiplying permille factors together before dividing overflows exact-integer
range in JavaScript by the fifth factor.

*Consequence:* The multiplier order in `SIMULATION_SPEC.md` §9.2 is normative, and
changing it is a rule change that bumps the schema version.

Authority: Craft · Phase 2, `SIMULATION_SPEC.md` §2, §9.2

---

## D-28

**The match RNG is mulberry32, seeded once from the 32-bit match seed, and consumed
only by the `random_floor` and `random` selectors, in resolution order. Ranged draws
use `floor(u32 × n / 2^32)` with no rejection sampling.**

*Why:* mulberry32 is a dozen lines of 32-bit integer operations that port identically
to any language, and restricting its consumers to two named selectors means the
determinism contract can be audited by reading one section.

*Consequence:* Content cannot introduce randomness except through those selectors.
The draw is very slightly biased; every implementation is biased identically, which is
the property that matters.

Authority: Craft · Phase 2, `SIMULATION_SPEC.md` §17

---

## D-29

**When several units are ready on the same tick, they resolve in ascending cooldown
order, then by side priority that alternates with tick parity, then by index within
side.**

*Why:* Any fixed side order gives one player a standing edge in every simultaneous
exchange, and a game whose fights are mirror-symmetric by design should not have a
first-mover advantage baked into the rules.

*Consequence:* Mirror matches with cooldowns that are not multiples of two ticks
alternate who lands first; the `tie_parity` fixture exists to lock this.

Authority: Craft · Phase 2, `SIMULATION_SPEC.md` §8.2

---

## D-30

**Market Share is 10,000 Share Points, displayed as a percentage to one decimal. Push
converts to Share Points through a per-round permille table with a per-firm remainder
carry, so no chip damage is ever rounded away.**

*Why:* Late-round Push values are an order of magnitude larger than early ones, and
the bar must read the same in round 1 and round 16 — so the conversion, not the bar,
scales with the round; the carry makes the conversion exact rather than lossy.

*Consequence:* The conversion table is a `BALANCE_PLAN` knob with a direct effect on
fight length. A 100-point bar was rejected because one Junior Developer hit would move
it by a whole percent at round 1.

Authority: Craft · Phase 2, `SIMULATION_SPEC.md` §3.2, §10.3

---

## D-31

**Campaign interludes (Recruiter, Board Meeting, Consultant) are map nodes that do not
advance the round counter, grant no income, and do not tick Tenure. Upkeep that cannot
be paid in Budget is paid in Goodwill cap at 100 per `¥1`, for that round only.**

*Why:* Sixteen fights per run (D-21) and three-to-ten-round Tenure thresholds (D-25)
both need a round to mean a fight, so detours cannot count; and a firm that cannot pay
rent losing reputation rather than staff is both the honest consequence and the joke.

*Consequence:* A player can never be in debt and can never lose a floor to arrears;
they can only enter a fight with a smaller buffer. The upkeep-to-Goodwill rate is a
`BALANCE_PLAN` knob.

Authority: Craft · Phase 2, `GAME_DESIGN.md` §3, §15

---

## D-32

**Push, Morale, Anomaly and Restore target a firm and carry no selector. Floor and
employee selectors exist only on effects that act on employees — statuses and
retriggers.**

*Why:* Floors have no hit points; Goodwill belongs to the firm. "Target the highest
floor" can only ever mean "act on the employees there", so giving Push a floor target
would have been a field that did nothing.

*Consequence:* This narrows D-10 without contradicting it. The Parent Company's
"floor-targeting on every ability" means every status it applies is floor-selected,
and a concentrated tower feels that in Bureaucracy and Burnout, not in raw damage.

Authority: Craft · Phase 2, `SIMULATION_SPEC.md` §6

---

## D-33

**Each event emits exactly one ledger entry carrying `sourceSide`, `targetSide` and
signed deltas. Which firm's ledger it appears on, and with what sign, is a view
decision. The replay file is the `MatchResult` itself; there is no separate format.**

*Why:* A Push by A on B is one event that debits B's Goodwill and credits A's Share,
and recording it once with both sides named is the only representation from which
both ledgers, the autopsy and the replay can be derived without disagreement.

*Consequence:* The live ledger's "+840 Ship Feature" on A's panel and "−840 Ship
Feature [RIVAL]" on B's are the same entry rendered twice, which is what the brief's
example already showed. Supersedes the `side` field sketched in Q-GW-7's entry
example; the field list in `SIMULATION_SPEC.md` §16.1 is now normative.

Authority: Craft · Phase 2, `SIMULATION_SPEC.md` §16

---

## D-34

**Equipment is cut from v1 and not deferred. Furniture stays in v1 on trial: it is kept
until the vertical slice can answer whether it earns its layer, and the test is named
in `GAME_DESIGN.md` §21. If it fails, its effects fold into room auras and Tier III
clauses and recipes take rooms as their third input.**

*Why:* The human's call — equipment is one layer too many, and furniture is worth
testing rather than deciding on paper; it is the cheapest source of tile scarcity in
the design, and the sim treats it as flat bonuses and periodic events, both of which
survive a fold into rooms unchanged.

*Consequence:* D-12 is confirmed on equipment and made provisional on furniture. The
vertical slice must be built with furniture in, so the fold is a content edit rather
than a re-architecture, and Q-LYR-3 holds the gate.

Authority: Human · Question: [Q-LYR-1](OPEN_QUESTIONS.md#q-lyr-1--is-furniture-a-distinct-layer-in-v1), [Q-LYR-3](OPEN_QUESTIONS.md#q-lyr-3--does-furniture-earn-its-tile)

---

## D-35

**Each firm's Goodwill is displayed as a bar on its own side of the battle screen, with
the number overlaid. The bar fills and empties; its frame shortens as Morale erodes the
cap; it dims while regen is suppressed. The Market Share bar moves only while a firm's
Goodwill bar is empty.**

*Why:* A number that drifts is not a shape the eye can track at combat speed; a bar
that visibly drains and then *stays empty while the other bar starts moving* makes the
two-stage rule legible without a word of explanation.

*Consequence:* Presentation only — nothing in the sim changes. The eroding frame is the
first place Morale becomes visible without reading the ledger, which is what makes the
Compliance Office fight teachable.

Authority: Human · Phase 2, `GAME_DESIGN.md` §19.2

---

## D-36

**The player's rules, shop, economy and building are identical in campaign and ranked.
The campaign's difference is the map layer and the opponent: scripted bosses and
tutorial rivals, semi-scripted templated rivals everywhere else, and rival-only
gimmicks carried in the same `globals.modifiers` field the player's Board Meeting
modifiers use. The win-reward pick is cut; a win pays `¥3` in both modes.**

*Why:* The human's framing — campaign is PvP against scripted or semi-scripted
opponents with a special buff or mechanic — is a stronger form of the locked "one sim,
modes as configuration" rule, and it removes the last place the two modes' economies
diverged.

*Consequence:* Rival templates become the same artefact as balance fixtures and, later,
ghost-pool seeds. Gimmicks are content, shown in a pre-fight dossier so the player
builds against something visible. A build that works in campaign works in ranked by
construction.

Authority: Human · Phase 2, `GAME_DESIGN.md` §3, §15.5, §16

---

## D-37

**Every passive, ability, trigger and gimmick in the game is a list of effects from
one closed vocabulary — eight triggers, twelve actions, twenty stats, fifteen flags,
two overrides, the selector and scope lists — enforced as JSON Schema enums. Adding a
word is a schema change, a sim change and a fixture; adding an entity is a JSON edit.**

*Why:* Content that can extend its own vocabulary cannot be audited, and the
determinism contract depends on the sim's behaviour being enumerable from one
document; a closed algebra is the only shape under which "no content in code" and
"two implementations agree" are both true.

*Consequence:* Some future card idea will not be expressible, on purpose. The stat and
flag semantics table (`SIMULATION_SPEC.md` §6.4) and the schema enums must stay in
one-to-one correspondence, and CI checks that they do.

Authority: Craft · Phase 3, `CONTENT_SCHEMA.md` §3, §14

---

## D-38

**One file per content type under `content/`, each an object holding one array; one
JSON Schema (draft 2020-12) with a `$def` per file type; an `index.json` naming every
file, its `$def`, and the content version. Semantic versioning: numbers are a patch,
new entities a minor, removals and schema changes a major with a snapshot migration.**

*Why:* Per-type files are what a coding agent and a human both read and diff most
easily at this catalogue's size, and one schema file with `additionalProperties:
false` everywhere is what turns "the vocabulary is closed" from a policy into a build
failure.

*Consequence:* Tower snapshots carry `contentVersion` and the sim refuses a mismatch,
so every content patch that changes a number silently invalidates nothing and every
one that removes an entity is forced to write a migration.

Authority: Craft · Phase 3, `CONTENT_SCHEMA.md` §1

---

## D-39

**Ordinary campaign rivals are expanded from archetype templates — a weighted shopping
list, a layout preference and a gimmick pool — by a nine-step algorithm that is
deterministic from `(templateId, round, seed)` and ends with the same validation a
stored snapshot gets.**

*Why:* Sixteen rounds times several rivals each is too many towers to hand-author and
keep balanced, and a deterministic expander means the rival pool, the balance fixtures
and the ranked cold-start seed are the same artefact (D-36).

*Consequence:* The expander is build-side code with a specification in a content
document, which is unusual and deliberate — its outputs are content. Bosses and the
six first-run fights stay hand-authored because each exists to teach one thing.

Authority: Craft · Phase 3, `CONTENT_SCHEMA.md` §11.2

---

## D-40

**The content loader asserts, in CI and on every development start-up: schema
validity with no unknown keys; every reference resolves; one ability per employee;
every non-shop employee is a recipe result; room costs match the tile table; the map's
fight counts match its round spans; every scripted snapshot passes structural
validation; every non-exempt snapshot is constructible on its round's income.**

*Why:* Asset correctness is a CI check in this project, and content correctness must
be the same kind of thing — a rule the build enforces, not a review step someone
remembers. The Phase 3 checks already caught a fight-count error in the Phase 2
campaign map that a read-through had missed.

*Consequence:* Authoring a rival that cannot be afforded is a build failure unless it
is flagged as a boss, which is also what keeps the ranked ghost pool honest later
(D-20).

Authority: Craft · Phase 3, `CONTENT_SCHEMA.md` §12

---

## D-41

**`manifest/sprites.json` is generated from the content catalogue and the screen
specifications, not hand-authored: every content `sprite`, `tile` and `icon` reference
becomes an entry, every screen region in `GAME_DESIGN.md` §19 becomes an entry.
Overhang is derived and asserted. Five entries whose dimensions were decided to fit the
canvas carry `verify: true`; three debug entries carry `releaseGate: false`.**

*Why:* A manifest that content references must contain every id content references,
and generating it from content is the only way that stays true without a reviewer
remembering to check; deriving overhang is the only way the manifest cannot contradict
itself.

*Consequence:* 156 entries exist before any code does, each a complete greybox spec.
The `verify` flag is what keeps Q-GBX-5 honest: the release gate refuses to close while
any is set, and clearing one is a commit that either confirms or changes the number.

Authority: Craft · Phase 4, `ART_PIPELINE.md` §1, §15

---

## D-42

**A pnpm workspace with a one-direction dependency graph — `sim` at the root importing
nothing, then `content` and `manifest`, then `build`, then `game`, then the Tauri
shell — enforced by an import lint that fails CI on a reversed edge.**

*Why:* Every guarantee the design rests on (determinism, headless balancing, server
re-simulation) is a statement about what the sim does not depend on, and a lint is the
only form of that statement that survives a year of agent-driven commits.

*Consequence:* The harness and the future ranked server consume `packages/sim`
unchanged. The renderer can never reach into the sim except through `simulate()`.

Authority: Craft · Phase 4, `ARCHITECTURE.md` §1–§2

---

## D-43

**The build phase is a pure reducer over a closed action set. The undo stack is the
action log, and undo is replay-all-but-the-last from the round's start state. The
committed tower is a `TowerSnapshot` — the same type the sim reads — not a client
model converted into one.**

*Why:* D-17 promised that a build round is a function from a start state and an
action list to an end state; making the reducer literally that is what makes undo
trivially correct, the build phase testable by fixture, and a saved run replayable.
One tower representation removes an entire class of "the thing I built is not the
thing that fought" bugs.

*Consequence:* Every build-phase feature is an action with a fixture. The save file's
tower is the snapshot, so save migration and ghost-pool migration are the same code.

Authority: Craft · Phase 4, `ARCHITECTURE.md` §4–§5

---

## D-44

**The art-complete gate and the pack-licence gate run only in the tag-triggered
release workflow. The commit workflow never checks for the presence of an asset file
or a licence record.**

*Why:* "No phase or milestone may be gated on art existing" and "art completeness is a
release gate" are both locked, and the only way both hold in CI is for the two gates to
live in a workflow that ordinary commits never trigger.

*Consequence:* A commit with zero art is green. A release with one missing asset is
red. The badge in the README shows the distance between them.

Authority: Craft · Phase 4, `ARCHITECTURE.md` §9.3

---

## D-45

**Steam integration sits behind a `Platform` interface with a `NullPlatform` for
development and the harness. The `SteamPlatform` lives in the Tauri Rust process over
the `steamworks` crate and is reached through a handful of Tauri commands. The
TypeScript build never links Steamworks and is identical with or without it.**

*Why:* Development, CI and the balance harness must run without Steam present, and
keeping the Steam API lifecycle in one Rust crate is what keeps every other package
free of it.

*Consequence:* Steam Cloud syncs the profile and current run only; replays stay local.
Achievements become a content file in Phase 5. Controller navigation for Steam Deck is
deferred and recorded as Q-ARCH-1.

Authority: Craft · Phase 4, `ARCHITECTURE.md` §8

---

## D-46

**Each run begins by choosing a founder from eight portraits and naming the firm. The
founder appears on the build screen's firm panel, beside the Goodwill bar in every
fight, and in the rival dossier. It is content (`content/founders.json`) with a
portrait, a badge and an `effects` list that is empty in v1, and it rides in the
snapshot's `globals.founderId` where the sim applies its effects like a modifier's.**

*Why:* The human's call — an avatar the player picks, present in the UI now, with
buildings, staff or abilities possibly attached later. Putting it in the snapshot with
an empty effects list is the same move as `attachments[]` (D-13): the format cost is
paid once, now, so that giving a founder a mechanic later is a content edit.

*Consequence:* Sixteen new manifest entries (eight portraits at 64 × 64, awaiting the
same pack-fit verification as the inspector portrait; eight badges at 32 × 32), one new
screen, and one new content type. The sim's determinism contract is unchanged because a
founder is a modifier. Rival templates draw a founder from an archetype pool so the
dossier always has a face.

Authority: Human · Phase 4, `GAME_DESIGN.md` §3, §19.7; `CONTENT_SCHEMA.md` §9

---

## D-47

**Controller navigation ships after v1. Mouse and keyboard are the v1 input model.
Every screen keeps its interactive elements enumerable from the first commit so that a
cursor scheme is an input adapter later, not a re-layout. Steam Deck verification is
therefore a post-v1 goal, not a release gate.**

*Why:* The human's call — it is real UI work with its own fixtures and it is not on the
path to the game being fun, which is what the vertical slice exists to find out.

*Consequence:* The build runs on a Deck at 2× with mouse and keyboard but is not
verified for it. `ROADMAP.md` §8 says so on its own line.

Authority: Human · Question: [Q-ARCH-1](OPEN_QUESTIONS.md#q-arch-1--when-does-controller-navigation-arrive)

---

## D-48

**No paper wireframe pass for now. The greybox vertical slice (M2) is the UX
milestone: twenty developer runs, the answerable human-check questions from
`GAME_DESIGN.md` §21, and layout notes turned into changes or register entries before
M3 begins.**

*Why:* The human passed on the wireframes; the brief's own position is that the game
must be judgeable in greybox, and UX is judged by using a screen, which the slice is
the first moment anyone can do.

*Consequence:* M2 cannot be exited by an agent. If the slice is not fun, the roadmap is
rewritten from M2 down rather than continued.

Authority: Human · Question: [Q-UX-1](OPEN_QUESTIONS.md#q-ux-1--when-does-the-ux-review-happen)

---

## D-49

**The vertical slice is the sixteen-round linear loop — found a firm, build, Ready,
fight, autopsy, next round, three strikes — against templated rivals, with no map, no
interludes, no portal and no recipes. It is the earliest point at which the game is
fun, and it is exactly ranked's loop with templates in place of ghosts.**

*Why:* Fun lives in the build round and the fight it causes; the map, the portal and
the recipes deepen a loop that must already work, and testing them first would hide a
loop that does not. Choosing the ranked shape for the slice means the slice is never
thrown away — the campaign wraps it, and ranked substitutes into it.

*Consequence:* The only throwaway work in the whole plan is three debug pages and one
label (`ROADMAP.md` §10). M2's exit is a human judgement, and a negative one sends the
plan back to `GAME_DESIGN.md`, which is the correct place for it to go.

Authority: Craft · Phase 5, `ROADMAP.md` §4

---

## D-50

**The balance invariants are content — `content/balance.json`, seventeen entries with
population, measure, comparator, threshold, cadence and severity — read by the
harness, validated by the schema, and reported on every push and every night. Tuning
follows one loop with one number per commit, in the order cost, cooldown, value, new
content, rule. The Quarter Close curve, the bar's scale, the tick rate, the grids, the
Tenure tiers and the retrigger depth are not knobs.**

*Why:* An invariant that lives in a document is a hope; one that lives in a file the
build reads is an assertion, and this project's stance is that correctness of every
kind is a CI check. One knob per commit is what keeps the balance history bisectable
and teachable. The not-a-knob list exists because those numbers are load-bearing for
something other than balance, and the tuning loop must not be allowed to reach them.

*Consequence:* Adding an invariant is a content edit plus a measure implementation.
Automated tuning is explicitly out of scope for v1 — the harness finds, a human
decides — because numbers an optimizer chose are numbers nobody can explain.

Authority: Craft · Phase 5, `BALANCE_PLAN.md` §6, §8, §9

---

## D-51

**A room may be relocated — moved with its occupants and furniture to a legal
rectangle on any owned floor — for a Relocation fee equal to the current round's
income and a loss of three Tenure rounds (one tier's worth, floored at zero).
Demolition stays total. The Restructuring modifier becomes one free relocation per
run with full Tenure carried.** *Restructuring itself was removed by D-52.*

*Why:* The human raised the case the demolition rule was never designed for: a room
built in round 2 on 1F, and a better floor leased in round 7 that did not exist when
the room was placed. Punishing a choice the player never had reads as unfair even when
the arithmetic is kind — and it is kind: a Tier II room on 1F already equals a fresh
one on 2F. Relocation gives "the building grew" a price rather than a trap, while
demolition keeps "I changed my mind" as expensive as D-24 requires.

*Consequence:* A new build action with a fixture; a comparison block in the inspector
so the choice is visible arithmetic; `inv.relocation_rare` in the harness as a warning
that the valve is not being used as a habit; and a sharper Q-ECO-2, which now asks
only whether the one free relocation exists.

Authority: Human · Question: [Q-ECO-2](OPEN_QUESTIONS.md#q-eco-2--does-a-run-need-a-mid-run-recovery-valve)

---

## D-52

**There is no free relocation. The Restructuring modifier is removed from the Board
Meeting pool and its flag from the vocabulary. Paid relocation (D-51) — a round's
income and three Tenure rounds, every time — is the run's only recovery valve.**

*Why:* The human's call, on seeing that a paid move already answers the case
Restructuring was invented for: a free move on top of it would make the one escape
hatch a routine one, and the tension D-24 exists to protect is that moving a room is
never free.

*Consequence:* Supersedes the Restructuring half of D-51 and closes Q-ECO-2. The Board
Meeting pool is seven modifiers; `restructuringCharge` is gone from the schema; the
harness's `inv.relocation_rare` is now the only guard on the valve, and it warns rather
than fails.

Authority: Human · Question: [Q-ECO-2](OPEN_QUESTIONS.md#q-eco-2--does-a-run-need-a-mid-run-recovery-valve) · Supersedes part of [D-51](#d-51)

---

## D-53

**The design phase is signed off as drafted: the eight documents, the content
database, the sprite manifest and the schemas. Implementation begins at
`ROADMAP.md` M0, subject to the research verification pass recorded in
`docs/RESEARCH_NOTES.md`.**

*Why:* The human reviewed the five phases and raised no issue, and asked for a
research pass over genre practice, the stack, Steam, licensing and agentic process
before moving forward. Sign-off and verification are recorded separately so that a
research finding that changes a decision is a new log entry, not a reopening of the
phase.

*Consequence:* Seven items stay open in the register, none blocking M0. Any research
finding that alters a locked or logged decision is appended here with the source.

Authority: Human · Phase 5 close

---

## D-54

**Each shop tab draws from a bag: the cards eligible at the round, shuffled, drawn
without replacement, refilled only when empty. A reroll never repeats a card until
every eligible card has been offered once.**

*Why:* Super Auto Pets' most repeated shop complaint is the same card cycling back
after a paid reroll, and the genre's mitigations are shared pools and pity systems;
a bag is the simplest of them, it is deterministic from the run seed, and it is one
field in `content/shop.json`.

*Consequence:* The build-phase RNG consumes one shuffle per bag refill rather than one
draw per card. `inv.dead_content` gains meaning: a card the optimizer never picks is
now guaranteed to have been *offered*.

Authority: Craft · `RESEARCH_NOTES.md` §2

---

## D-55

**No screen in the game requires text input. The firm-name field has a generated
default and typing is optional; nothing else takes text.**

*Why:* The Steam Deck's on-screen keyboard is drawn by the Steam overlay, and the
overlay does not work in Tauri webviews (Q-TECH-1). A required text field would make
the game unplayable on a Deck under the current stack; an optional one costs nothing.

*Consequence:* One sentence in `GAME_DESIGN.md` §19.7 and a rule for every future
screen. Independent of how Q-TECH-1 is answered, since a game that never needs a
keyboard is better on a couch either way.

Authority: Craft · `RESEARCH_NOTES.md` §1, §4

---

## D-56

**A second, earlier art gate — store-ready — requires every manifest entry in
visibility tiers 1 and 2 to have validated art. It sits ahead of art complete on the
roadmap's release-gate lines and is read from the same coverage report.**

*Why:* Steam's Coming Soon page needs five real gameplay screenshots, capsules and a
trailer months before release, and greybox screenshots on a store page cost wishlists;
tiers 1 and 2 are exactly the build and battle screens those screenshots show.

*Consequence:* The worklist ranking, which already puts tiers 1 and 2 first, now has a
deadline attached to its top half. Nothing in development waits on this either: it is
a release-side gate like the other (D-44).

Authority: Craft · `RESEARCH_NOTES.md` §4

---

## D-57

**A new harness invariant, `inv.standing_pat_loses`: a builder that stops buying after
round 8 and relies on Tenure alone must win at most 40% against the field in rounds
12–16.**

*Why:* Teamfight Tactics' documented lesson is that passive interest must not outpay
active spending or the game solves toward holding; Tenure is interest-shaped, and D-24
made holding deliberately rewarding, so the failure mode needs a guard the harness can
run.

*Consequence:* Eighteen invariants become nineteen. If the invariant fails, the knob is
the Tenure step or the late-round income, in that order.

Authority: Craft · `RESEARCH_NOTES.md` §2

---

## D-58

**A run ends at five strikes, not three, in both modes. Sixteen fights, so four losses
are allowed: a 75% win floor.**

*Why:* The human's call on Q-STR-5. The genre's reference games end at ten wins or
five losses, a floor near 70%; three strikes over a fixed sixteen allowed two losses,
an 87% floor, and the genre's evidence is that early luck-losses at that severity drive
churn.

*Consequence:* Supersedes the strike count in D-21; its other numbers stand. One field
in `content/modes.json`, five strike icons in the top bar instead of three.

Authority: Human · Question: [Q-STR-5](OPEN_QUESTIONS.md#q-str-5--how-many-strikes) · Supersedes part of [D-21](#d-21)

---

## D-59

**Tauri is dropped. The stack is re-selected from a research pass over Godot 4 (C#
and GDScript), MonoGame/FNA, Unity, Bevy, Electron + PixiJS, LÖVE and raylib against
the six constraints the design already fixes. Until Q-TECH-1 is answered, no code that
depends on an engine or packaging layer is written.**

*Why:* The human's call, on the research finding that the Steam overlay cannot hook
any Tauri webview and that Linux and Steam Deck could not be verified without a spike.
The planning prompt permitted relitigating the stack only on a hard blocker stated
plainly; this was one, and the human chose not to carry the risk. With no code
written, the cost of re-selection is the research itself.

*Consequence:* Supersedes locked decision 10 on packaging, and possibly on the engine
and language, depending on the answer. Everything designed for the sim, the content,
the manifest, the greybox workflow and the harness is stack-independent by
construction and stands. `ARCHITECTURE.md` §2, §7, §8 and §9 are rewritten once the
stack is chosen; the dependency rule, the stores, the save format and the `Platform`
interface survive any answer.

Authority: Human · Question: [Q-TECH-1](OPEN_QUESTIONS.md#q-tech-1--tauri-or-electron-given-the-steam-overlay-and-the-deck) · Supersedes part of locked decision 10

---

## D-60

**The stack is Godot 4 (4.6 or later) with C# on .NET 8. The simulation is
`CompanyWars.Sim`, a class library that references nothing but the .NET base library —
never a Godot assembly. The client uses the Compatibility (OpenGL) renderer and the
X11 display driver on Linux. Steamworks is GodotSteam. MonoGame with the same sim
library is the recorded fallback if the overlay spike fails.**

*Why:* The human's call, accepting the recommendation three independent research
passes reached (`RESEARCH_NOTES.md` §9). The deciding constraint is the headless sim:
it must run in the game, in a CI harness at ten thousand matches a minute, and later
on a server, producing identical hashes, and only a language with a first-class
standalone runtime does that cleanly. C# sits with Java at the top of real-repository
agent benchmarks, and it turns the Godot-3-versus-4 API mistake — the most common
agent error on Godot — into a compile error rather than a silent runtime no-op.
Electron was excluded for the reason Tauri was; Unity for agent-hostile scene files;
Bevy for quarterly API churn; GDScript for trapping the sim inside the engine.

*Consequence:* Supersedes locked decision 10 on engine, language and packaging; keeps
its requirement that the sim be a pure headless module, now enforced by project
references, a reflection test and a banned-API analyzer. `ARCHITECTURE.md` is
rewritten. The one live risk — the overlay under Vulkan and Wayland — is a settings
choice confirmed by a one-day spike on the developer's own Steam Deck at M0. Nothing
in the sim spec, content, manifest, greybox workflow or balance plan changes.

Authority: Human · Question: [Q-TECH-1](OPEN_QUESTIONS.md#q-tech-1--tauri-or-electron-given-the-steam-overlay-and-the-deck) · Supersedes locked decision 10

---

## D-61

**The mulberry32 listing in `SIMULATION_SPEC.md` §17 is corrected to the canonical
algorithm — the fourth step is `t ^= t + ((t ^ (t >> 7)) * (t | 61))`, with the XOR the
earlier draft omitted — and the C# `unchecked uint` listing is now the normative one.**

*Why:* Rewriting the RNG for C# exposed that the JavaScript draft dropped an `^=`; it
would have produced a deterministic but non-standard generator, which is harmless for
play and a trap for any future cross-check against a reference implementation. No
fixture had been recorded, so it is a correction, not a schema bump.

*Consequence:* The `random_selector` conformance fixture, when recorded at M0, is
against the canonical generator. `Draw(n)` is specified as
`(uint)(((ulong)Next() * n) >> 32)` — exact, no rejection sampling, identical in every
language.

Authority: Craft · `SIMULATION_SPEC.md` §17

---

## D-62

**Within a tick the phases run A (banners), B (expiry), D (ready and resolve), C
(cooldown advance), E (periodic), F (end check): a unit's readiness is judged on the
progress accumulated through the previous tick, and only then do cooldowns advance.
`SIMULATION_SPEC.md` §7 is corrected to list the phases in that order.**

*Why:* M0's implementation exposed that §7 and §20 disagreed. §7 listed the advance
before the ready check, which puts the first fire of an 80-tick cooldown on tick 79
and gives the mirror fixture `totalPush` 1077; §20's worked trace and the M0 exit
criterion pin tick 80 and 1131. The human chose the §20 reading: a 4.0 s cooldown
fires on the tick its eightieth tick of progress completes, so the trace's tick
numbers, its month boundaries and the signed-off aggregates all hold. The ten
conformance fixtures were recorded under this reading, so this is a correction of the
text, not a rule change; `schemaVersion` stays at 1.

*Consequence:* The tick loop in `Match.Run.cs` is the normative order. §20's remark
that 60-tick cooldowns "alternate parity" is also wrong (60, 120, 180 are all even) and
is corrected: parity alternates only when a cooldown's tick count is odd, which happens
through Overtime and cooldown multipliers, and the `tie_parity` fixture records what
the rule does with equal 60-tick cooldowns.

Authority: Human · `SIMULATION_SPEC.md` §7, §8, §20

---

## D-63

**There is no browser build. Godot 4 cannot export a C# project to the web (the 4.7.2
editor refuses with "Exporting to Web is currently not supported in Godot 4 when using
C#/.NET"), so playtest builds are desktop exports: every push to `main` produces Windows,
Linux and macOS zips as workflow artifacts and refreshes a rolling `nightly` pre-release.**

*Why:* A GitHub Pages build was asked for as the easiest way to test. It would need either
a GDScript client, which D-60 and the working rules exclude, or a second client in another
stack, which is the one thing the sim/render split was built to avoid. The stack's cost
here is real and is recorded rather than worked around.

*Consequence:* Testing needs a download, not a link. The export presets live in
`game/export_presets.cfg`; content, schema and manifest ride beside the executable (inside
the bundle on macOS) until the resource-pack loader lands with the release pipeline
(ARCHITECTURE.md §9.3). If Godot gains .NET web export in a later 4.x, this decision is
reversed by a new entry, not by editing this one.

Authority: Craft · `ARCHITECTURE.md` §9.3

---

## D-64

**Three corrections from the M2 UX review. (1) `GAME_DESIGN.md` §19.1's "Undo is a key, not a
button" is superseded: UNDO and DROP are buttons left of READY, floor up/down are buttons in
the shaft, and a REROLL button sits under the cards. (2) §19.2's playback controls move from
(560, 48, 72, 12) to (520, 48, 72, 16): the spec's own rect overlapped founder B's badge at
x = 596. (3) The fallback pixel font the build ships (`ART_PIPELINE.md` §4.1) is baked from
DejaVu Sans by `tools/planning/gen_font.py` into an 8 px BMFont, so `font.ui.8` and
`font.ui.16` render at exactly 1× and 2× with every glyph the UI uses.**

*Why:* A first playtest build must run by touch on a phone and, later, by controller on the
Deck (D-47), and a key-only undo fails both. The controls collision was visible in the first
battle screenshot. Godot's built-in TTF fallback rendered "×" as a different letter and
overran the 8 px line, which made the multiplier badges — the build screen's main readout —
unreadable; a baked bitmap face fixes that without deciding the shipped face (Q: m5x7 or Pixel
Operator, `ART_PIPELINE.md` §9), which stays the human's licence call.

*Consequence:* §19.1 and §19.2 are edited to match; `manifest/sprites.json` carries the new
controls rect; screenshot fixtures are re-recorded. The baked font is a placeholder in the
same sense as every other greybox asset: when the real face lands at the manifest's path,
nothing else changes.

Authority: Craft · `GAME_DESIGN.md` §19.1–§19.2, `ART_PIPELINE.md` §4.1

