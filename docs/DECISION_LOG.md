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
| [D-65](#d-65) | Cards and the inspector explain every entity in plain language generated from its content; the firm panel carries a primer on how a fight works | Craft | Playtest |
| [D-66](#d-66) | The shipped faces are Honey Pigeon (body, 8 px line) and Honeyblot Caps (headers, 16 px line), baked to bitmaps; the font files stay out of the repository | Human | `ART_PIPELINE.md` §9 |
| [D-67](#d-67) | Text is not pixel art: the faces render as antialiased vectors at the window's resolution under the `canvas_items` stretch; the font files ship in the project. Supersedes D-66's baking | Human | `ART_PIPELINE.md` §9 |
| [D-68](#d-68) | Reference-driven screen changes: employees visible in the battle facades and bursts from their windows; price first and tier as a colour on cards; a READY under the shop; founder stakes line; settings screen and build tag; lead-change cue; staff bars on the autopsy | Craft | UI review |
| [D-69](#d-69) | No pack is isometric: the battle towers are front-on facade bands, the `iso` perspective becomes `exterior`, and the four tower dimensions pass verification | Craft | [Q-GBX-5](OPEN_QUESTIONS.md#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions) |
| [D-70](#d-70) | Employees wear Character Pack bodies: Idle frame 1, one body per employee, no repeat inside a department, tier carried by age and formality; extraplanar staff are the same bodies under a spectral recolour | Craft | Phase 4 |
| [D-71](#d-71) | Portraits are 96 × 96, the pack's own size: the nine portrait entries and the two layouts that place them grow, and each founder is cast to a face from the Portraits pack | Craft | [Q-GBX-5](OPEN_QUESTIONS.md#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions) |
| [D-72](#d-72) | Founder select is a roster column plus a detail panel, not a wall of equal cards; a 48 × 48 thumbnail slot per founder, halved 2:1 at author time | Craft | UI review |
| [D-73](#d-73) | A room's art is one composed plan at its footprint size, plus a 32 px band of overhang above it for back-row fittings; every tile inside stays walkable | Craft | Phase 4 |
| [D-74](#d-74) | The UI chrome is the Isle of Lore 2 UI Pack, halved 2:1 and recoloured to the greybox tones; §14's `anomalous` rule is scoped to world art, and `ui` entries are exempt | Craft | Phase 4 · [Q-RISK-1](OPEN_QUESTIONS.md#q-risk-1--is-the-guttykreum-licence-clear-for-a-commercial-steam-release) |
| [D-75](#d-75) | A screen's title is Honeyblot Caps at a new 32 px line, set on the backdrop with no plate; the founder header slot is deleted | Craft | UI review |
| [D-76](#d-76) | UI chrome is not pixel art either: it ships at an integer multiple of its declared size and is drawn down with a smooth filter, as D-67 already does for text | Craft | UI review |
| [D-77](#d-77) | A screen's scene owns position; the manifest keeps sizes, anchors, footprints and draw order. Art moves to `game/assets/` so the editor can see it | Human | UI workflow |
| [D-78](#d-78) | Room plans are authored as Godot scenes and baked to their PNG; the build screen's floor rows and shaft become nodes too. The `.room` recipe format is retired | Human | UI workflow |
| [D-79](#d-79) | A room renders live from its scene through a SubViewport at 2x, not from a baked PNG: baking at half scale threw away the resolution that painting at half scale bought | Human | UI workflow |
| [D-80](#d-80) | The GuttyKreum licence is recorded and clear for release; the sheets a room draws from are committed, the rest stay a local palette | Human | [Q-RISK-1](OPEN_QUESTIONS.md#q-risk-1--is-the-guttykreum-licence-clear-for-a-commercial-steam-release) |
| [D-81](#d-81) | The sim is handled headlessly and anything visible is editable in Godot: a visible number belongs in a scene, not in a `_Draw` | Human | UI workflow |
| [D-82](#d-82) | A shop card is its face, name and price; the rest is read in the inspector once the card is picked. Supersedes D-68's tier band and price size | Human | UI review |
| [D-83](#d-83) | Floors are leased from the tower: an unleased floor is greyed under a screen with its price, and tapping it leases it; the shop's lease row is removed | Human | UI review |
| [D-84](#d-84) | A floor's look is a scene per business, rendered live like a room; what a floor is stays content. One business, `basic`, for now | Human | UI workflow |
| [D-85](#d-85) | The fight is a revenue race: most ¥ at the Bell wins; Sales earn, Poach, Scandal and Curse take, Client Loyalty protects. Ids renamed to match; no early finish | Human | Money rework |
| [D-86](#d-86) | The balance plan restated for the race: archetypes renamed fortress, raider, earner, scandal; `fight_length` becomes `late_swing` (100–250‰ of quarters won from behind after Crunch); `bellRateMax` retired | Craft | Money rework |
| [D-87](#d-87) | Sales earn in proportion to Client Loyalty (`v × loyalty / capAtStart`); the archetype band applies from round 4 | Human | Money rework |
| [D-88](#d-88) | Sales scale by Loyalty over the current cap, not the starting cap, so a Scandal no longer cuts a firm's Sales for good | Human | Money rework |
| [D-89](#d-89) | Legal bills its hours: the Paralegal, Compliance Officer and General Counsel earn Sales, then file, clear or freeze | Human | Money rework |
| [D-90](#d-90) | An archetype may be weak early and strong late: the band judges its mean over the run; each round stays within 300–700‰ | Human | Money rework |
| [D-91](#d-91) | Management keeps its clients: the Team Lead's Delegate is followed by PR 60 | Human | Money rework |

Fifty-eight craft decisions and thirty-three human calls taken. Eight items remain open in
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

Superseded by: [D-85](#d-85), for the kinds — they become Sales, Poach, Scandal and Curse; that piercing Loyalty is a property of the kind stands.

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

Superseded by: [D-85](#d-85). The score is each firm's Revenue in ¥; there are no Share Points and no conversion table.

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

Superseded by: [D-85](#d-85), in part. The per-side bar stands as Client Loyalty; the Market Share bar gives way to each firm's Revenue and a lead bar showing each firm's share of the quarter's takings.

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

---

## D-65

**Cards and the inspector explain every entity in plain language generated from its content,
and the firm panel carries a primer on how a fight works.** `CompanyWars.Build.Explain` turns an
employee, room or furniture definition into sentences — *Ship Feature: 60 Push every 4.0 s* —
plus a one-line glossary for the damage kind it deals, and for an employee a *where to put it*
block listing the rooms whose aura covers its department, the furniture that helps it, its
ability's reach and the floor multipliers. A staff card shows department, tier, cooldown and the
ability in two words (*35 Push*, *80 Restore*, *Cleanse*); picking up a card opens the inspector on
it before it is placed. Nothing is authored per entity: a new employee explains itself from its
effects, and a test checks that no explanation leaks a vocabulary token.

*Why:* The first hands-on playtest opened the build screen and could not tell what a unit did,
how damage happened or where anything should go. §19.1's detail rows printed the effect vocabulary
(`push 35`, `static: stat push ×1.3`), which is the schema's language, not the player's. §20's
"every mechanic legible on the screen where it matters" was unmet on the screen that matters
most.

*Consequence:* `GAME_DESIGN.md` §19.1's inspector and firm-panel rows are amended; the build
screenshot fixture is re-recorded and a `build_inspect` fixture (the inspector open on a hire) is
added. The wording is a placeholder in the same sense as the greybox art: the writer may
re-phrase any sentence in `Explain.cs` without touching content or layout.

Authority: Craft · `GAME_DESIGN.md` §19.1, §20

---

## D-66

**The game's faces are Honey Pigeon for body text at the 8 px line and Honeyblot Caps for headers at the
16 px line, both by Steven Colling, licensed for the project. `tools/planning/gen_font.py` bakes each into a
monochrome BMFont at exactly its line height; glyphs a face lacks (arrows, the minus sign) are borrowed
from DejaVu Sans at the same line. The TrueType sources live in `game/fonts/source/`, ignored by git;
only the baked bitmaps and the licence text are committed.**

*Why:* The owner chose the faces. The licence permits a bitmap export the game needs and forbids
redistributing the font files, which a repository would do. `font.ui.16` was specified as the 8 px face at
exactly 2×; a separate header face at a true 16 px line is a better use of the slot and keeps the
one-size-per-slot rule (§9: no other sizes).

*Consequence:* `ART_PIPELINE.md` §9's font row is amended; the m5x7 / Pixel Operator candidates are
dropped. Every screenshot fixture is re-recorded. Digits are proportional in both faces, so the ledger
and bars right-align numbers rather than relying on tabular glyphs.

Authority: Human · `ART_PIPELINE.md` §4.1, §9

---

## D-67

**Text is not pixel art. The two faces render as vector fonts, antialiased at the window's resolution; the
project's stretch mode is `canvas_items` so sprites still scale by the integer factor with nearest filtering
while text is drawn at full resolution. The TrueType files ship in `game/fonts/`. This supersedes D-66's
bitmap baking and `ART_PIPELINE.md` §9's "nearest-neighbour everywhere, including fonts".**

*Why:* The owner ruled that the chosen faces are hand-drawn text, not pixel fonts, and should look like it.
Baked at an 8 px line they were at the edge of legibility and lost their character; rendered at 2× or 3× the
same sizes read cleanly and keep the 8 px and 16 px line boxes every layout depends on.

*Consequence:* `project.godot` and the pixel-discipline assert change to `canvas_items`; screenshot fixtures
are captured at the window's size, so the 2× and 3× sets come from two runs at 1280×720 and 1920×1080;
word wrap measures with the face rather than counting characters. The licence permits the files inside the
product but not making them available to others: the repository must be private.

Authority: Human · `ART_PIPELINE.md` §9, `ARCHITECTURE.md` §7

---

## D-68

**Nine changes from the comparison of the five screens against the auto battlers and tower games they
will be judged beside. (1) Every employee is visible in its battle facade as an 8 × 8 window marker at its
tile's column and row, in its department's tone, and a burst leaves the firer's window rather than the
segment's centre; the floor inset remains the detailed view. (2) Shop cards lead with the price in
`font.ui.16` and carry a tier band along the top edge in a tier tone. (3) A second READY sits under the
shop's lease row, where the thumb already is on touch. (4) The founder bio ends with a stakes line: the
starting budget, roster and floor, and the passive or its absence. (5) A settings screen with fullscreen
and window scale, every control a button, saved to `user://settings.cfg` and never applied in screenshot
or drive mode. (6) A build tag on the title and settings screens, written by the builds workflow into the
data pack. (7) The share bar brightens the new leader's half for a second when the lead changes. (8) The
autopsy's floor panel gains a FLOORS / STAFF toggle; STAFF lists the player's five employees by output.
(9) The menu gains SETTINGS.**

*Why:* The reference games all show the fighters, lead with cost in the shop, keep the commit action by
the shop on touch, state a character's stakes before the run, and chart damage by unit after a fight.
Ours did none of those, and each is a small change against slots that already exist.

*Consequence:* `GAME_DESIGN.md` §19.1, §19.2, §19.3 and §19.7 rows are amended; two manifest slots are
added (`fx.tower.window_occupant`, `ui.build.ready_shop`); every screenshot fixture is re-recorded. §19.2's
"bursts at the segment's centre" is superseded. Founder passives stay empty (D-46); the stakes line says so.

Authority: Craft · `GAME_DESIGN.md` §19, `ARCHITECTURE.md` §7

Superseded by: [D-82](#d-82), for the tier band and the price's size in (2); the price still leads.

---

## D-69

**No GuttyKreum pack is isometric. The battle towers are front-on facades: a storey is three 32 px wall
tiles wide by one tile tall, the two towers stand left and right of the street, and the manifest's
`iso` perspective is renamed `exterior` — assets move from `assets/iso/` to `assets/exterior/`. The four
tower dimensions are verified unchanged at 96 × 32, 96 × 32, 96 × 16 and 96 × 24. This supersedes the
premise, in `DESIGN_BRIEF.md` §4 and `GAME_DESIGN.md` §19.2, that the city packs are isometric.**

*Why:* The packs arrived and the premise was simply wrong: GuttyKreum draws top-down floors and
front-on walls in one tileset, and there is no isometric art anywhere in the thirty-three packs. The
useful part of the premise survives — the exterior and the interior must not mix on one screen — so the
enum keeps its job under a name that is true. The dimensions survive too, and not by luck: a facade band
is a whole number of 32 px tiles, so 96 × 32 was already the natural cut.

*Consequence:* The perspective enum is `topdown | exterior | ui`; the schema, `ManifestValidator` and the
atlas groups follow. `verify` clears on the four tower entries, leaving nine (the portraits, which fail
their own check — Q-GBX-5). A leased segment carries fifteen unlit windows in a 5 × 3 grid at an 18 × 9
pitch and the occupant sprite lights one; an unleased segment is bare wall, which is what makes an empty
floor read from across the street. `assets/exterior/fx/tower/` now holds four cut sprites from the Osaka
tilemap, the first art in the repository.

Authority: Craft · `ART_PIPELINE.md` §12, §15 · `GAME_DESIGN.md` §19.2 · Question: [Q-GBX-5](OPEN_QUESTIONS.md#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions)

---

## D-70

**Every employee wears a body from `characterpack/Blackoutlinecharacters`: frame 1 of that body's Idle
set, which is the facing-down standing pose. No two employees in the same department share a body, and
tier is carried by age and formality — students and youths at T1, working adults at T2, elders and
traditional dress at T3. The ten extraplanar employees are the same bodies under a spectral duotone
recolour at 78% alpha, violet except the Kappa Intern (green) and the Recruiting Oni (red). The mapping
lives in `gen_manifest.py`'s `EMP_BASE` and reaches the manifest as each entry's `candidateSource`;
`tools/dev/cut_employees.py` reads it back out and cuts the forty PNGs.**

*Why:* The pack has nineteen bodies and the roster has forty employees, so something has to repeat. Tier
is the axis worth spending the silhouette on: a player reads "this is a senior hire" from across the
board and reads department from the card's glyph and the window tone anyway (D-68). Repeating a body
across departments costs less than repeating one inside a department, where two cards sit side by side.

*Consequence:* The pack's "Idle" folder is four facings, not an animation loop, so there is no idle
animation to play — an employee is one static frame, and the eight-frame walk cycles go unused until
something moves. Department is not yet readable from the sprite itself; a per-department recolour pass
would fix that and is the next art step, not a blocker. Forty-four of 184 slots now have art.

Authority: Craft · `ART_PIPELINE.md` §7.1 · `assets/PACKS.md`

---

## D-71

**Portraits are 96 × 96, not 64 × 64. `ui.portrait` and the eight `founder.*.portrait` entries take the
Portraits pack's own size, from its `transparent_bg` cut, and the two layouts that place them move with
them: the build inspector and firm panel put the portrait at (440, 40) with their text at x = 544 and
their body from y = 144, and the founder card grows to 104 × 120 in a 152 × 128 cell, pushing the bio to
y = 288 and the firm-name row to y = 332. Each founder is cast to a specific face — Sato to
`oldbusinessman1`, Hoshino to `femaletrendy1`, Okada to `malepunk1`, Nakagawa to `femalestudent1`,
Moriyama to `youngbusinessman1`, Ueda to `femalebaker1`, The Founder to `femaleelder1`, Kitamura to
`maletraditional1` — and each badge takes the matching Character Pack body. This supersedes the 64 × 64
in `GAME_DESIGN.md` §19.1, §19.7 and §19.8.**

*Why:* 96 is not an integer downscale of 64, so §9's pixel discipline rules out resampling, and a
64 × 64 crop was tried and rejected: it cuts through the face and takes the hair silhouette with it — the
baker's cap, the elder's bun, the glasses — which is exactly what tells eight founders apart at a glance.
Redrawing eight faces to fit a number chosen before the pack was open is the tail wagging the dog.

*Consequence:* Q-GBX-5 closes and `verify` is now clear on every manifest entry. The build panel loses
32 px of vertical budget below the portrait — the stats and the six-sentence primer still fit, with a few
pixels to spare, and that is the tightest thing on the screen. The founder-select screen keeps its
four-by-two grid; the bottom row lands 12 px clear of the canvas edge. Sixty manifest entries now have
art and 122 remain on the worklist.

Authority: Craft · `GAME_DESIGN.md` §19.1, §19.7, §19.8 · `ART_PIPELINE.md` §15 · Question: [Q-GBX-5](OPEN_QUESTIONS.md#q-gbx-5--can-the-packs-produce-the-decided-battle-and-portrait-dimensions)

---

## D-72

**Founder select is a roster column and a detail panel. The left column is eight 64 × 64 tiles in two
columns of four, each holding a new 48 × 48 `founder.*.thumb`; the right panel, 464 × 312 at (168, 32),
gives the selected founder a 96 × 96 portrait, name and title, the battle badge that represents them in
a fight, the bio, and a *YOU START WITH* block drawn from `content/economy.json` — the starting roster as
sprites, the starting budget, the starting floor — with the firm-name field and **FOUND THE FIRM** on its
bottom row. The thumbnails are the 96 × 96 portraits halved 2:1 by a box average at author time, which
`ART_PIPELINE.md` §9's integer-factor rule permits and nearest-neighbour would ruin. This supersedes the
four-by-two card grid of D-71 and the `ui.founder.bio` strip; `ui.founder.card` becomes
`ui.founder.tile`.**

*Why:* Eight equal cards make the player compare eight things at once and read none of them. Every
selection screen worth copying — and the references we were handed are all of them — puts a compact grid
of faces on one side and spends the rest of the screen on the one being considered. Ours had a
three-line bio strip under the grid doing that job in a tenth of the space.

*Consequence:* Eight new manifest entries (192 total), and the founder screen's Godot layout and its
screenshot fixtures must be rebuilt — the numbers here are from a mock, not from the running screen. The
*YOU START WITH* block is identical on all eight panels because founders are cosmetic in v1 (D-46);
stating that plainly beats implying a difference that is not there. When founders gain effects, the
block is where the difference will show.

Authority: Craft · `GAME_DESIGN.md` §19.7 · `ART_PIPELINE.md` §9

---

## D-73

**A room's art is one composed plan at the room's own footprint — 64 × 64 for a 2 × 2, 64 × 96 for a
2 × 3, 32 × 64 for a 1 × 2 — plus a 32 px band above it, anchored bottom-left so `ART_PIPELINE.md` §2.1
derives the band as top overhang. The plan is floor across the footprint and fittings along the back
row: a counter for Reception, racks for the Server Room, a table and chairs for the Boardroom, shelves
of files for Legal. Every tile inside the footprint stays walkable and no fitting may sit where a person
will stand. Plans are composed from the pack tilemaps by `tools/dev/compose.py` from a recipe under
`tools/dev/rooms/*.room`; the recipe is the committed record of which cells a room is made of. This
supersedes the single 32 × 32 tileable `room.*.tile` of `GAME_DESIGN.md` §19.8.**

*Why:* A repeated floor tile cannot say "Server Room" — the pack has no floor that reads as a server
room, because in the source art rooms are told apart by what is in them. Composing the room the way the
pack's own example maps do is the only way to get a room that looks like that room.

*Consequence:* The back row of a room carries its fittings, which is where the fiction and the mechanics
agree — people work in front of the thing that defines the room. The 32 px overhang means a room placed
on a floor's back row draws into the wall band above it, which is what that band is for. Player-placed
furniture and the room's own fittings can still collide visually if furniture lands on the back row;
that is a placement rule to settle when furniture placement is built, not an art problem. The entry ids
keep the `.tile` suffix — they are stable keys referenced from `content/rooms.json`, and renaming keys
for prose costs more than it buys. Six of sixteen rooms are composed; the other ten need their cells
picked.

Authority: Craft · `GAME_DESIGN.md` §19.8 · `ART_PIPELINE.md` §2 · supersedes the tile model, not [D-12](#d-12) — a room is still a zone over tiles, not an object consuming them

---

## D-74

**The UI chrome comes from Steven Colling's Isle of Lore 2: UI Pack. Every element is halved 2:1 by a
box average at author time — the pack draws an 18 px 9-slice corner and our canvas is 640 × 360, so 9 px
is what fits — then recoloured from its blue-and-white onto a four-stop ramp built from a greybox tone,
then 9-sliced to the manifest's size. The mapping is a table, `tools/dev/ui/slots.txt`, one row of
`<manifest id> <element> <unit> <tone>`, and `tools/dev/ui.py` builds every slot from it. Cards take
their *category* tone rather than `interface`. Controls 16 px tall take the pack's `box` (5 px corners);
20 px and taller take `button_square` (9 px). `ART_PIPELINE.md` §14's rule that non-GuttyKreum art is
categorised `anomalous` is scoped to world art — `topdown` and `exterior` entries — and `ui` entries are
exempt.**

*Why:* Our chrome was 1 px rectangles because nothing better existed, and 89 of the remaining slots are
`ui`. This pack is not a foreign style in any meaningful sense: its author drew Honeyblot Caps and Honey
Pigeon, the two faces already in `game/fonts/` (D-67), and the pack's own documentation names them as
the fonts it was drawn against. The `anomalous` rule exists so a Kappa Intern from another artist reads
as being from another plane; a panel border cannot read as anything, because the player never meets one
inside the fiction. Applying the rule to chrome would have tinted the entire interface violet, which is
the rule doing the opposite of its purpose.

*Consequence:* **This is the first pack in the repository with a licence record** — Steven Colling Game
Asset License 1.0, recorded at `packs/stevencolling/isle_of_lore_2_ui/LICENSE.md`: commercial use and
modification granted, no attribution required, redistribution only as content files of the project. That
last clause is why `packs/` is gitignored and only the recoloured sprites are committed. Q-RISK-1 is
unchanged for the GuttyKreum packs, which still have no record. `tools/dev/packs.py` now keys packs as
`vendor/pack` and flags a missing `LICENSE.md` per pack. Nineteen chrome slots are built; the rest of
the 89 need rows in the table, and the small glyph slots — 8 × 8 departments, pips, the budget mark —
are not 9-sliceable and still have to be drawn.

Authority: Craft · `ART_PIPELINE.md` §13, §14 · Question: [Q-RISK-1](OPEN_QUESTIONS.md#q-risk-1--is-the-guttykreum-licence-clear-for-a-commercial-steam-release)

---

## D-75

**A screen's own title is type, not a panel. `ui.founder.header` is deleted — a 640 × 24 plate behind
`CHOOSE A FOUNDER` was a slot that existed only to hold text — and the title is set at (16, 0) in a new
`font.ui.32`: Honeyblot Caps at a 32 px line, exactly 2× `font.ui.16`. The font ladder becomes 8, 16, 32,
each an integer double of the last. This amends `ART_PIPELINE.md` §9's "no other sizes" and supersedes
the header row of `GAME_DESIGN.md` §19.7.**

*Why:* The plate did nothing the backdrop was not already doing, and it boxed the title into 24 px on a
screen with room to spare. Honeyblot Caps is a display face — it was drawn to be seen large, and at a
16 px line under a panel border it was being used as a label. The 32 px line lands the title in the band
above the panels, which already start at y = 32, so nothing moves.

*Consequence:* One fewer slot to draw, and the title now depends on the face rather than on art, which
means it follows D-67 — vector, antialiased at the window's resolution, never baked. Six other 640 × 24
header slots stand unchanged: the map header, the codex header, the autopsy banner, the battle result
banner and the menu. They are not all titles — a result banner is a state readout and probably wants its
plate — so each is its own call rather than a sweep.

Authority: Craft · `GAME_DESIGN.md` §19.7 · `ART_PIPELINE.md` §9

## D-76

**UI chrome is not pixel art. A `ui` entry's file may be any integer multiple of its declared
`sprite.w × sprite.h`, the same multiple in both axes; `tools/dev/ui.py` writes 2×, and the renderer
draws the texture down into its manifest-sized rect with a smooth filter. Layout stays in canvas units
and nothing about the 640 × 360 grid moves. Pixel art is unchanged: nearest filtering, integer scale.
This extends D-67 from text to chrome and amends `ART_PIPELINE.md` §5's dimension rule and §9.**

*Why:* The Isle of Lore 2 pack is drawn smooth, with an 18 px 9-slice corner. Squeezing it into a 9 px
corner to fit the canvas grid threw away three quarters of its pixels, and then the game's 2× nearest
upscale magnified what survived — the corners came back as stair-steps. The pack's corner is exactly 2×
ours, so writing chrome at 2× is lossless in both directions. The precedent was already set: D-67 ruled
that hand-drawn text should not be pretend pixel art, and a hand-drawn rounded panel is the same
argument with the same answer.

*Consequence:* At a 2× window a 2× chrome texture maps 1:1 to device pixels, so that case is exact. At 3×
it is a 1.5× upscale of an already-antialiased source, and the artefacts predicted for it were looked for
in a captured 1920 × 1080 fixture and are not visible — the pack's own soft edges hide the uneven
sampling, and pixel art beside it stays hard-edged. **No filter change is needed at 2× or 3×; 4× and 6×
are unmeasured** and should be looked at before either ships. `ManifestValidator` accepts the multiple;
the placeholder path is unchanged, so a slot without art still renders at exactly its declared size.

Authority: Craft · `ART_PIPELINE.md` §5, §9, §13 · extends [D-67](#d-67)

---

## D-77

**A screen's own scene is the authority for position. Each `game/scenes/<Screen>.tscn` gains a `Layout`
node whose children are the screen's slots — a `ColorRect` per drawn element, named for what the code
asks for, positioned and sized where it belongs. `SceneLayout` reads them; the screen draws there. The
guides are hidden at load, so the 2D editor shows them and the game never does. The manifest keeps what
it validates well — sizes, anchors, footprints, overhang, draw order, perspective, coverage — and gives
up position. The art tree moves from `assets/` to `game/assets/` so `res://` can reach it. This
supersedes the rule in `CLAUDE.md` that positions come from the manifest, and `GAME_DESIGN.md` §19's
rect tables become the intended layout rather than the live one.**

*Why:* The owner wants to move things by dragging them, and every alternative made that worse. The
manifest could not answer for most of a screen anyway — `FounderScreen` held eleven positions as C#
literals against three in the manifest, so the rule was already being broken, quietly, in the place it
mattered most. Naming the scene as the authority makes the practice and the rule agree, and removes the
conversion step entirely: there is nothing to sync because there is only one copy.

*Consequence:* Headless work is untouched. `CompanyWars.Sim` references nothing but the base library and
`Build` and `Playback` are pure, so the harness, the ten conformance fixtures and all 69 tests never load
Godot — layout was only ever read by the client. Screenshot fixtures still need a real window, which was
already true and unrelated. What is genuinely lost: no validator now holds sizes and positions together,
and a `.tscn` is a Godot format rather than a contract other tools can read — if anything outside the
client ever needs to know where something sits, it will have to parse a scene or be told.

All four screens that had manifest positions — founder, build, battle, autopsy — are migrated, and
`tools/dev/scenes.py` scaffolded the last three from the manifest's own rects, so the move is
pixel-neutral: every one of the twelve screenshot fixtures is byte-identical across it. The remaining
screens (menu, picker, summary, settings, greybox) never used manifest layout at all. **Nothing reads
`sprite.layout` any more**, so the field should now be deleted from the generator, the schema, the C#
record and `ManifestLayout`; it is dead weight until that happens.

A slot's node is a `Sprite2D` where its art exists and a `ColorRect` where it does not, so the 2D editor
shows the real screen rather than a diagram. Two things the scaffolder has to get right and a hand-written
scene easily will not: a layout point is where the *anchor* sits, not the top-left, so the anchor is baked
into the node's position; and chrome ships at 2× (D-76), so its sprite is scaled to half. The first of
those was wrong on the first run and moved the battle towers by 48 × 32 px, which the fixture comparison
caught.

Authority: Human · `ARCHITECTURE.md` §7 · `GAME_DESIGN.md` §19 · supersedes the position half of [D-15](#d-15)'s neighbourhood in `CLAUDE.md`

---

## D-78

**A room plan is authored as `game/scenes/rooms/<room>.tscn` — one `Sprite2D` per 32 × 32 tile, each an
`AtlasTexture` region of a pack tilemap, in the plan's own coordinate space with the overhang band at the
top. `tools/dev/rooms.py --scaffold` mirrors the tilemaps into `game/assets/packs/` so `res://` can reach
them and writes the scenes; `tools/dev/rooms.py` bakes each scene into the PNG at its manifest path. The
`.room` recipe format and `tools/dev/compose.py` are retired — one authoring format, not two. On the build
screen, the three visible floor rows and the lift shaft become `Layout` nodes, so the grid follows them;
the 32 px tile pitch does not, because grid sizes are not a knob (`BALANCE_PLAN.md` §9).**

*Why:* The recipes worked but nobody can see a room in `put office 58,0 at 0,0`. Arranging a server room's
racks is a job for the eye, and the same argument that moved screen layout into scenes (D-77) applies to
what a room is made of. Baking to a PNG rather than drawing the scene at runtime keeps everything the
manifest validates — one texture per room, its size, its anchor, its overhang, its place in the draw
order — exactly as it was.

*Consequence:* The mirrored tilemaps are licensed pack art, so `game/assets/packs/` is gitignored and
`--scaffold` recreates it; a checkout without the packs opens the room scenes with missing textures, which
is already true of anything that regenerates art. The migration is pixel-neutral — the six baked rooms and
all twelve screenshot fixtures are byte-identical across it, including the build grid change, which was
caught being 32 px off the first time by exactly that comparison.

Authority: Human · `ART_PIPELINE.md` §7.1 · extends [D-73](#d-73) and [D-77](#d-77)

---

## D-79

**A room renders live from its scene. `BuildScreen` instantiates `res://scenes/rooms/<room>.tscn` into a
`SubViewport` sized at 2× the plan and draws that texture where the baked PNG used to go. The bake still
exists for coverage and for anything that reads the manifest, but the screen no longer uses it.**

*Why:* a room is painted with half-scaled tiles, which is how a 2 × 2 room fits a conference table. Baking
that to the plan's own size resolves each 32 px tile down to 16 px **permanently**, and the canvas then
scales it back up — so painting at half scale bought resolution that baking immediately threw away. Drawn
live at 2×, the same tile covers 32 viewport pixels and lands 1:1 on a 2× window. The difference is
visible: fine detail inside a tile survives one path and dissolves in the other.

*Consequence:* a `SubViewport` and not a child node, because the build screen is immediate-mode: a room has
to sit above the floor void and below the people standing on it, and child nodes cannot be interleaved into
a `_Draw`. Update mode is `Always`, not `Once` — the documentation is explicit that `UPDATE_ONCE` renders a
single frame and then sets itself to `UPDATE_DISABLED`, so a viewport that ticks before its child scene is
ready would stay blank for good, and no build error would ever say so. The scenes reference a pack sheet at
runtime, so that sheet now ships (D-80). The battle screen's floor inset still draws the baked texture and
should follow.

Authority: Human · `GAME_DESIGN.md` §19.1 · extends [D-78](#d-78)

---

## D-80

**The GuttyKreum licence is recorded and clear for a commercial release. It grants derivative works and
use in any number of monetised projects, with distribution as part of the product. It forbids three
things: use in a logo or trademark; redistributing the assets other than as part of the product; and
letting a player extract the assets and use them elsewhere. Q-RISK-1 closes. Only the pack sheets a room
scene actually draws from are committed — currently `japanese_office_interior` — and the rest of the
mirrored palette stays gitignored.**

*Why:* the second restriction is the one with teeth for a repository. Shipping a sheet inside the game is
permitted; publishing it on its own is not. That is the same reasoning D-67 applied to the two fonts, and
it has the same consequence: **the repository must stay private**. Committing only the sheets a scene
draws from keeps the exposure to what the product actually contains rather than the whole 1 GB collection.

*Consequence:* Q-RISK-1 is answered for GuttyKreum and was already answered for Isle of Lore, so the
licence half of the release gate is closed. The third restriction is not yet satisfied: loose PNGs under
`res://` are extractable from an exported build, so the export step needs to pack them rather than ship
them as files. That is a packaging task, not an art one, and it is open. Both `.gitignore` rules that
hid art this session — a bare `build/` and a bare `packs/` — are now anchored to the repository root.

Authority: Human · `ART_PIPELINE.md` §14 · Question: [Q-RISK-1](OPEN_QUESTIONS.md#q-risk-1--is-the-guttykreum-licence-clear-for-a-commercial-steam-release)

---

## D-81

**Two halves of the project, and a rule for each. The simulation is handled headlessly: `CompanyWars.Sim`
references nothing but the base library, `Build` and `Playback` are pure, and the harness, the ten
conformance fixtures and all sixty-nine tests run without Godot ever loading. Anything a player can see
goes the other way: it is a node in a scene, positioned and sized in the 2D editor. A visible number found
in a `_Draw` is not a constant to tidy — it is a node that has not been made yet.**

*Why:* the owner edits by dragging, and the two halves want opposite things. Balance work needs to run a
thousand fights a second with no window; layout work needs to be seen and nudged. Keeping the boundary
sharp means neither compromises the other, and it is already how the code is built — D-60 put the sim
behind an assembly that cannot reference Godot, and D-77 moved position into the scenes.

*Consequence:* the build screen's shop tabs, card row, reroll, lease row and the four top-bar readouts are
now `Layout` nodes rather than offsets from the shop panel, and the change is pixel-neutral — the same
window renders byte-identically before and after. What remains in code is genuinely internal to a widget:
where a card's price sits inside the card. Those become editable when a card is its own scene, which is
the next step this rule implies. The autopsy's banner lines, its FLOORS/STAFF tab row, its bar rows and its findings column follow, and so
do the menu, settings and summary screens, which had no `Layout` at all. Picker and Greybox are developer
tools and stay as they are: a debug rival list does not need a draggable layout. What is left in code is
either derived from a node — a bar that stretches with its panel — or a widget's own insides, which
become editable when that widget is a scene, as the shop card now is.

Authority: Human · `ARCHITECTURE.md` §1, §7 · extends [D-60](#d-60) and [D-77](#d-77)

## D-82

**A shop card is its face, its name and its price. Department, tier, cooldown, the ability, passives and
an extraplanar rider leave the card for the inspector, which already shows all of them the moment a card
is picked and before it is placed. The tier band goes with them. The price moves to a rounded tag along the
card's bottom edge, `ui.card.price`, set in Honeyblot Caps at the height of its own slot inside the tag — under D-68's
`font.ui.16`, so it sits inside its row. The picked card is framed by the UI pack's corner-bracket
selector, `ui.card.selector`.**

*Why:* the owner set the shop beside the shop grids it will be judged against — a rhythm game's outfit
shop and a roguelite's item grid — where a tile is a picture and a price and the panel beside the grid
does the reading. At 52 × 80 the stat lines were eight-pixel abbreviations, *Eng T1 3.0 s* and *35 Push*,
that the inspector then spelled out in full: the card said everything twice, once illegibly. A 2 px tier
band said nothing the second line did not.

*Consequence:* supersedes the tier band and the price's size in D-68 (2), and the sentence in
`GAME_DESIGN.md` §5.1 that a card states everything the sim will use. Nothing becomes hidden — it is one
tap away, as it is in the genre — and the price is still the card's largest mark. `Ui.TierTone` and
`BuildScreen.ShortAction`, which existed only for the card, are deleted. The header face now serves any
size above the 8 px body line, not only 16 and 32.

Authority: Human · `GAME_DESIGN.md` §5.1, §9, §20 · supersedes part of [D-68](#d-68)

## D-83

**Floors are leased from the tower, not the shop. An unleased floor draws as the empty floor, greyed under a
screen, with a tag in its middle — the lease price over the upkeep it will add — and tapping anywhere on the
floor leases it: the screen lifts and the floor is live. A floor that needs the portal reads LOCKED until
the portal opens. The shop's Lease label and its three lease buttons are removed; the shop's READY stays at
the foot of the shop, where D-68 put it for the thumb.**

*Why:* the owner wanted a lease bought where it is used. A floor is a place in the tower, and a button for
it three panels away asked the player to connect the two; the three buttons' 16 px labels also overflowed
their 72 px width. On the floor itself the price has the whole floor to sit on, and buying it reads as
unlocking the space.

*Consequence:* `GAME_DESIGN.md` §5.4 and the §19.1 shop, tower and READY rows are amended.
`ui.build.lease_button` becomes the tag, built from the UI pack's `button_square` in the dark `structure`
ramp, and its insides are a widget scene, `game/scenes/widgets/LeaseTag.tscn`, whose `screen` slot's colour
is the tint: `SceneLayout` now reads a ColorRect's colour as well as its box. A floor off the three in view
is reached by scrolling the tower, as before. D-68 (3) stands; only the row it sat under is gone.

Authority: Human · `GAME_DESIGN.md` §5.4, §19.1 · amends the placement named in [D-68](#d-68)

## D-84

**A floor's look is a scene. Each floor has `game/scenes/floors/<business>/<floor>.tscn`, authored at its
160 × 96 build-screen size and rendered live through a SubViewport at 2x, exactly as a room is (D-79). The
build screen draws it where it drew the floor frame — under the void, rooms, grid, people and the lease
cover. What a floor *is* — its grid, output, room kinds, upkeep, lease and fixed rooms — stays content,
read by the sim. There is one business for now, `basic`, and every founder uses it.**

*Why:* the owner plans each founder as a different kind of business, and a business is mostly how its
floors look. One frame panel shared by every floor could not carry that; a scene per floor can, and it is
edited the way rooms and screens already are (D-78, D-81).

*Consequence:* the five floor scenes start as the old frame panel with an empty half-scale tile layer on
the office tileset, so the screen is unchanged until they are painted; a floor with no scene falls back
to the frame. When founders carry a business type, the folder becomes the founder's and `basic` the
fallback — a field on the founder, so a schema change. The build screen's sample floors instance these
scenes, so painting one updates the preview. Each floor scene carries a locked `Guides` node — a line where
tiles meet, a red wash where nothing can be placed, each fixed room outlined and named, and what the floor
takes — written from content by `tools/dev/floor_guides.py` and hidden when the game renders the floor.

Authority: Human · `GAME_DESIGN.md` §19.1 · extends [D-79](#d-79) and [D-81](#d-81)

## D-85

**The fight is a revenue race: the firm that makes the most money in the quarter wins. Each firm has
Revenue in ¥, starting at ¥0. Sales add to your own Revenue; Poach, Scandal and Curse take from the
rival's; Client Loyalty — Goodwill, renamed — shields Revenue from Poaching and is rebuilt by PR and by
clients drifting back. Every quarter runs to the Bell, where more Revenue wins; there is no early
finish. Every effect id, stat and field named for the old fight is renamed to the new word — `push`
becomes `sales` or `poach`, `morale` `scandal`, `anomaly` `curse`, `restore` `pr`, `goodwill` `loyalty`.
The score is shown in ¥ labelled Revenue, and a fight's Revenue does not carry into the build Budget.
`REVENUE_RACE.md` holds the rules until they are folded into the spec.**

*Why:* the owner's goal is that the winner is the company that makes the most money, and that every
mechanic a player sees relates to it. Under the Goodwill fight a firm could not earn anything without
attacking first — Push had to drain the rival's Goodwill before it moved anything — and the score was a
tug-of-war bar, not money. The race separates making money from taking it, and keeps the sim's
arithmetic: the value pipeline, the Quarter Close curve, rooms, floors, statuses and retriggers are
unchanged; what changes is what the value is applied to.

*Consequence:* supersedes D-30 and parts of D-07 and D-35; D-04, D-05, D-06 and D-23 stand, restated
for Revenue and Loyalty. The fight half of D-53's sign-off is reopened and closes when the last stage
lands. Stage 1 — this entry, `REVENUE_RACE.md`, `GAME_DESIGN.md` §2, §6.4 and §11 — is done; until
stage 2 lands, `SIMULATION_SPEC.md` and the code still run the Goodwill fight. Stage 2 changes the spec,
the sim and the schema's effect vocabulary, and re-records the ten fixtures under the owner's
`fixtures-approved` label; stage 3 rewrites content and the balance plan, retiring `bellRateMax` and
restating the invariants that assume Goodwill; stage 4 changes the screens. Each stage is approved
before it starts.

Authority: Human · `REVENUE_RACE.md`, `GAME_DESIGN.md` §2, §6.4, §11 · supersedes [D-30](#d-30), parts of [D-07](#d-07) and [D-35](#d-35)

Superseded by: [D-87](#d-87), in part. Sales earn in proportion to Client Loyalty; something now blocks them.

## D-86

**The balance plan is restated for the revenue race. Four archetypes take the race's names — turtle
becomes fortress, burst raider, economy earner, burnout scandal — in the template ids
(`rival.t_fortress`, …), the schema's archetype enum and the counter web, which is redrawn as
`REVENUE_RACE.md` §5. `inv.fight_length` is replaced by `inv.late_swing`: between 100‰ and 250‰ of
decided field quarters must be won by a firm that was behind or level at some tick after Crunch began,
and draws stay at most 20‰. `bellRateMax` is retired. `break_guaranteed`, `bar_moves_early`,
`single_hit_cap`, `chip_cannot_suppress` and `ability_diversity` keep their purpose, restated in Loyalty
and ¥. The Account Manager becomes Sales's Poacher (Steal the Account), and the Headhunter's Burnout
ability, which was called Poach, becomes Job Offer, so that the word Poach means one thing.**

*Why:* the old bands assumed an early finish. Every quarter now lasts sixty seconds, so a median end
tick and a Bell rate measure nothing; what the length band protected was a fight still open late (D-21,
D-23). The swing rate's floor is D-23 (comebacks exist) and its ceiling is the `GAME_DESIGN.md` §21 test
(a lead lost late more than one fight in four feels like theft). A median "lead settles" tick was tried
first and dropped: in a race the stronger engine usually leads from its first sale, so the median
settles in Month 1 even when late swings are common.

*Consequence:* the harness prints the swing rate per round. At the first measurement it is 96–141‰, and
the archetype bands are far out: the earner wins 750–990‰ against the field and management 80–390‰.
Stage 3 continues one knob per commit. `REVENUE_RACE.md` §4's department table is followed except for
HR, which still has no Burnout on the rival.

Authority: Craft · `BALANCE_PLAN.md` §4–§6, `REVENUE_RACE.md` §4–§5 · extends [D-85](#d-85)

## D-87

**Sales earn in proportion to the firm's Client Loyalty: an employee's Sales add
`floor(v × loyalty / capAtStart)` to its firm's Revenue, so a firm whose clients are being poached, or
whose cap a Scandal has cut, sells less. The ledger's `raw` keeps the full value and `revenueDelta` what
was earned. The archetype band applies from round 4, when tier-2 staff arrive: rounds 1–3 have only
tier-1 staff and no card that can hurt Sales, so a pure earner wins them whatever the numbers.**

*Why:* stage 3's harness found the pure earner winning 795–990‰ against the field. In the race as D-85
wrote it nothing blocked Sales, so every card that did not earn barely paid, and none of nine single
knobs moved the earner below about 715‰. A prototype of this rule brought the earner to 330–520‰ from
round 6 and turned the counter web the way `REVENUE_RACE.md` §5 draws it — the raider beats the earner,
the fortress rises — and it gives Loyalty a job in a race: Poaching hurts from its first hit, and PR and
Loyalty passives protect a firm's earnings.

*Consequence:* amends D-85's "Sales add to your own Revenue; nothing blocks it". `SIMULATION_SPEC.md`
revision 3 changes §9.3 and §16.1; the §20 worked trace is unchanged, because with no Poach Loyalty stays
at its cap. Burnout now costs a firm its own Sales too, through the Scandal that cuts its cap, so Scandal
and management are the next knobs. The ten fixtures need re-recording under `fixtures-approved`, as for
stage 2.

Authority: Human · `SIMULATION_SPEC.md` §9.3, §16.1, `BALANCE_PLAN.md` §4 · amends [D-85](#d-85)

Superseded by: [D-88](#d-88), in part. Loyalty is measured against the current cap, not the starting one.

## D-88

**Sales earn in proportion to Loyalty over the firm's current cap, not its starting cap:
`floor(v × loyalty / cap)`. A Scandal lowers the cap a firm's Loyalty is measured against, so it hurts
through its transfer and by leaving Poaching less to chew through, not by cutting that firm's Sales for
the rest of the quarter.**

*Why:* measured against the starting cap, every point a Scandal shaved off a cap was a permanent cut to
that firm's Sales. Management, whose Middle Managers' Overtime leaves Burnout on every expiry, ground its
own cap to 1 by mid-quarter and earned 12–20% of what it sold, and the scandal archetype won 754–845‰
against the field. Over the current cap the prototype held scandal at 314–470‰ and management at
162–394‰ from round 6, and Poaching still cuts a rival's Sales as D-87 intended.

*Consequence:* amends D-87's formula; `SIMULATION_SPEC.md` revision 3's §9.3 says `cap`. The raider and
management remain outside the band and are the next knobs.

Authority: Human · `SIMULATION_SPEC.md` §9.3 · amends [D-87](#d-87)

## D-89

**Legal bills its hours. The Paralegal (¥40 every 3 s), the Compliance Officer (¥40 every 4 s) and
the General Counsel (¥200 every 10 s) each earn Sales as their ability, Billable Hours, and after each
bill file their Bureaucracy, clear it next door, or Freeze the rival's best person. The fortress is
Loyalty guarding an income, not Loyalty alone.**

*Why:* in the race the fortress earned almost nothing — about ¥3.7k of its own Sales in a round-16
quarter against the earner's ~¥39k — so it won 178–363‰ against the field whatever it held. Of three
designs trialled (Legal billing; a Sales engine behind Legal and HR; both), billing alone brought the
fortress into the band from round 12 (414/531/513‰ at rounds 6/12/16), made it beat the raider 677‰ as
the counter web asks, and took the failing invariants from 12 to 11.

*Consequence:* amends `REVENUE_RACE.md` §4's Legal row ("Poach with Bureaucracy"): Legal earns too. A
Paralegal now earns what a Sales Rep does for the same ¥3 and still brings Bureaucracy and Loyalty; the
nightly pick-rate checks (`dead_content`, the optimizer) watch whether it takes over. The
`random_selector` fixture's overlay now re-aims the Paralegal's Bureaucracy, which is no longer its
ability.

Authority: Human · `GAME_DESIGN.md` §9, `BALANCE_PLAN.md` §5.1 · amends [D-85](#d-85)

## D-90

**An archetype may be weak early and strong late. `inv.archetype_band` judges each archetype's mean
win rate over the measured rounds, from round 4, against the 420–580‰ band; in any single round it need
only stay inside 300–700‰ (`archetypeRoundSpike`), so an archetype that is a write-off or a wall in one
round still fails.**

*Why:* the owner is content for archetypes to have a curve through the run rather than sit inside the
band at every round. Judging each round separately failed exactly those curves.

*Consequence:* the harness prints each round against the spike band and one line of means, and its
failure count counts archetypes, not archetype-rounds. The owner set the principle; 300–700‰ is a first
value, not a signed-off number.

Authority: Human · `BALANCE_PLAN.md` §4, §6 · extends [D-87](#d-87)

## D-91

**Management keeps its clients: the Team Lead's Delegate is followed by PR 60 to its own firm.
Management makes its best person work again and rebuilds Loyalty while it does, so its earners keep
selling under Poach.**

*Why:* management earns through retriggers of whoever stands beside its Team Leads, and had nothing
that kept Loyalty; under D-87 any Poach emptied it and its Sales stopped. It averaged 358‰ against the
field after its template changes. Of four designs trialled at 40 seeds — retriggers ×1.5, ×2, Delegate
followed by PR, and both — Delegate PR alone brought it into the band (685/366/308‰ at rounds 6/12/16,
mean 453‰) and moved the raider from 637‰ to 605‰. PR 40 and PR 80 were worse.

*Consequence:* the fortress falls from 450‰ to 389‰, because management now holds its Loyalty against
the fortress's Counsel; the fortress is the next knob. The generalist and the raider also buy Team
Leads and gain a little PR.

Authority: Human · `GAME_DESIGN.md` §9, `BALANCE_PLAN.md` §5.1 · extends [D-89](#d-89)
