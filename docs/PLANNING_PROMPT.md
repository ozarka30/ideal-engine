# Company Wars — Master Planning Prompt

> **How to use this.** Paste the whole of this file into a fresh planning session,
> together with `docs/DESIGN_BRIEF.md`. It is written to be self-contained: it
> tells the planning agent what the game is, what is already settled, what still
> needs designing, what to produce, and what "good" means. Work through it in the
> phases given rather than answering it in one pass.

---

## Your role

You are the lead designer and technical architect for **Company Wars**, a
PvP auto-battler shipping to Steam. No code exists yet and none should be written
during this planning work. Your job is to turn the locked foundation below into a
complete, buildable design — one detailed enough that coding agents can implement
it without re-deriving intent, and honest enough that its weak points are written
down rather than glossed.

Design is a conversation. Where a decision is genuinely the human's to make —
tone, feel, what the game is *about* — stop and ask, and bring two or three
concrete options with a recommendation and a stated trade-off. Where a decision
is a matter of craft — data structures, balance methodology, file layout — make
the call yourself, state it, and give the reasoning in one line.

## The game

An auto-battler where you build a haunted Japanese office tower floor by floor,
hire the staff to fill it, and send it into quarterly combat against another
player's building for market share.

The player alternates between a **build round** (spend budget, hire staff,
construct rooms, rearrange the tower) and an **async PvP round** (your tower
fights a stored snapshot of another player's tower; you watch, you do not
intervene). The depth target is Backpack Battles: a scarce spatial grid, adjacency
synergy, hidden combine recipes, and a fight whose outcome was fully determined
the moment you pressed Ready.

## Locked foundation — do not relitigate

These are decided. Build on them. If one of them turns out to be load-bearing in a
way that breaks something else, say so explicitly and explain the conflict — do
not quietly design around it.

1. **Title:** Company Wars.
2. **Setting:** Retro Japanese corporate occult. 90s salaryman satire that quietly
   turns supernatural.
3. **Build space:** A multi-floor building. Small stacked grids joined by an
   elevator. Floors have distinct mechanical identity. The portal is a basement
   floor.
4. **Two asset classes.** Rooms are static: multi-tile footprints, bought once,
   expensive to undo, they define zones and auras. Employees are flexible: small
   units, freely repositioned between rounds, they carry the cooldowns and do the
   acting. The tension between commitment and reoptimisation is the game's core
   strategic identity.
5. **Combat:** Cooldown duel. Nothing moves. Employees fire on their own
   cooldowns. Layout is a pure build-time puzzle.
6. **Win condition:** Market share tug-of-war fronted by **Goodwill**. Each firm
   holds a Goodwill buffer with a visible number and a running **ledger** of named
   entries beneath it. Incoming push depletes Goodwill first; only once Goodwill
   breaks does further push move the shared Market Share bar. The bar starts
   50/50 and resolves on full claim or at the quarterly bell. The ledger is
   simultaneously the live defensive readout and the post-battle autopsy — design
   it as one component, not two.
7. **Crafting:** Full hidden-recipe combining, using employees, equipment and room
   context.
8. **Hiring:** Shop with paid rerolls. The portal unlocks mid-run and adds a
   second, riskier stock alongside the normal one.
9. **Two modes, campaign first.** The Slay-the-Spire-style campaign ships first
   and carries the tutorial; the ranked ladder follows. **One sim, one content
   database.** Modes are a configuration layer, never a rules fork. Campaign-first
   defers the entire async PvP backend out of v1 — plan for it, but do not build
   it early, and treat scripted campaign rival towers as the eventual seed ghost
   pool.
10. **Platform and tech:** Steam. TypeScript + PixiJS + Vite, packaged via Tauri.
    The simulation is a pure headless TypeScript module with no rendering
    dependency.
11. **Art:** GuttyKreum "The Japan Collection", 32x32. Interiors and characters are
    top-down and own the build view; the isometric city packs own the exterior and
    battle view. Never mix perspectives inside one view.
12. **Greybox-first workflow.** Every visual element is built as a labelled
    placeholder at the exact dimensions, anchor and grid footprint the real asset
    will use, declared in a shared sprite manifest. Greybox and final art are the
    same manifest entry, never two code paths. Dropping real art in must be a file
    copy, not a layout pass. A greybox is never approximate — one whose dimensions
    are unknown is a blocker, not a placeholder.
13. **The campaign ships fully playable in greybox.** Art is replaced
    incrementally and out of order afterwards, at the developer's pace. A screen
    that is part real art and part placeholder is the ordinary state for months,
    not a transient one. There is no art pass milestone, no roadmap phase may be
    gated on art existing, and art completeness is a *release* gate tracked
    separately from development progress.

`docs/DESIGN_BRIEF.md` carries the full detail, including the asset pack
inventory, the battle presentation direction, and the systems sketch. Read it
before starting.

## The development context — this shapes the design, not just the code

Implementation will be done primarily by coding agents, with a human stepping in
for layout work and design problems agents find hard. Design accordingly:

- **The sim must be headless, deterministic and pure.** Given a seed and two tower
  states it produces an identical result every time, with no rendering, no clock,
  no randomness outside the seeded generator. This is what makes agent-driven
  balancing possible: an agent can run 10,000 matchups in CI and report win rates.
  Treat this as a hard architectural constraint, not a preference.
- **All content is data, in text files.** Employees, rooms, furniture, recipes,
  status effects, shop tiers, encounters — authored as versioned JSON or TS data
  with a schema, never embedded in logic and never in a binary format.
- **Prefer designs an agent can test.** A mechanic whose correctness can only be
  judged by feel is expensive here. Where you specify something feel-dependent,
  say so and name what a human needs to check.
- **Specify visuals as greybox specs, not descriptions.** Every screen, panel and
  entity you design must state its pixel dimensions, anchor, and grid footprint —
  enough that an agent can build the placeholder and a human can read the
  screenshot as a spec. "A card showing the applicant" is not a specification.
- **Asset correctness is a CI check, not a review step.** Design so that a script
  can walk the sprite manifest and assert every present asset file matches its
  declared dimensions.
- **Write for a reader with no memory of this conversation.** Every document must
  stand alone.

## Deliverables

Produce these as separate markdown files under `docs/`. Do not attempt all of them
at once — follow the phase order in the next section.

1. **`GAME_DESIGN.md`** — the full design. The core loop beat by beat, the build
   phase, the fight phase, the economy, floors and their identities, the room
   catalogue, the employee roles, status effects, the recipe system, the portal
   and its risk/reward, both modes and how they differ, progression and unlocks,
   and the new-player experience.
2. **`SIMULATION_SPEC.md`** — the combat sim as an implementable specification.
   Tick rate, cooldown and initiative rules, exact tie-break ordering, how each
   floor's output aggregates into Goodwill damage and then into the bar, Goodwill
   regeneration and overflow, which effects pierce Goodwill, the Quarter Close
   pressure curve, status effect stacking and expiry, the determinism contract,
   and the replay and ledger-entry formats. This document must be precise enough
   that two independent implementations would agree on every match outcome.
3. **`CONTENT_SCHEMA.md`** — the data model. Schemas for every content type, with
   worked examples, plus the rules for adding new content without touching code.
4. **`ARCHITECTURE.md`** — module boundaries, the sim/render split, state
   management, save format and its migration strategy, Steam integration, and the
   build and packaging pipeline. Also specify the async PvP backend — ghost
   snapshot storage, matchmaking, anti-cheat posture — as a *deferred* component:
   v1 ships campaign-only, but the tower snapshot format and the sim's entry
   points must be designed now so that adding ranked later is additive rather than
   a rewrite.
5. **`ART_PIPELINE.md`** — how pack tiles become in-game content, and how the
   greybox-first workflow is enforced. Must cover:
   - **The sprite manifest schema.** Every visual slot's id, kind, grid footprint,
     pixel dimensions, anchor, atlas source rect and asset path. This is the single
     source of truth; no layout code may hardcode a pixel size.
   - **Footprint versus visual bounds.** These differ constantly in top-down 32x32
     art — a 2x2 room whose sprite is 64x88 because it overhangs the tile behind
     it. Specify how both are declared, how the greybox draws both (solid
     footprint, hatched overhang), and the draw-order rule for overhanging sprites
     (y-sort plus an explicit tie-break bias). Settle this in greybox or art will
     layer wrongly the moment it lands.
   - **The greybox renderer.** Labelled placeholders showing id, footprint and
     dimensions on screen, tone-coded by category, so a greybox screenshot reads as
     a design document.
   - **The validation check.** A CI script asserting that every manifest entry with
     a present asset file matches its declared dimensions, failing the build on
     mismatch.
   - **Mixed-state coherence.** Greybox and finished art share every screen for
     months, so placeholder tones are drawn from the pack's own palette,
     desaturated and category-coded — never arbitrary grey. A part-arted screen
     must read as deliberate, not broken.
   - **Absent files are valid.** Manifest entries declare their asset path before
     the file exists; adding art never edits the manifest. The validation check
     fails only on a dimension mismatch when a file is present, never on absence.
   - **Tooling.** A slicer that reads a GuttyKreum sheet and emits manifest stubs;
     an in-game overlay reporting asset coverage; and — most important for this
     workflow — a **ranked art worklist** command. For every entry still lacking
     art it emits a spec sheet: id, exact dimensions, anchor, footprint, which
     screens it appears on and how often, what it must read as, the exact target
     file path, and where known a candidate source tile from the packs, since much
     of this work is selection and slicing rather than drawing. Optionally a
     template PNG at correct dimensions with footprint and anchor marked. Rank by
     visibility — a shop card seen every round outranks a basement room seen twice
     a run.
   - **The definition of "art complete"** that serves as the release gate.
   - **Pixel discipline.** 32x32 base, nearest-neighbour, integer scale factors
     only, integer-snapped positions, fixed globally.
   - Atlas generation, the naming convention, the perspective rule and how it is
     enforced, the UI style guide, and the documented process for absorbing a
     future third-party pack through the portal fiction.
6. **`BALANCE_PLAN.md`** — the methodology, not the numbers. Target win-rate
   bands, what the automated matchup harness measures, how a broken build is
   detected, the archetypes that should exist and roughly how they should beat
   each other, and the process for tuning from telemetry. State the balance
   invariants as machine-checkable assertions the headless sim runs in CI. At
   minimum:
   - No defensive build may hold Goodwill unbroken to the bell against a median
     attacker of the same round.
   - The Market Share bar must begin moving within a stated number of seconds in
     the median matchup, or fights open flat.
   - No single archetype may exceed its win-rate band against the field.
   The intended counter-triangle is a starting point, not a conclusion: Legal
   turtles beat burst, burst beats economy scaling, Burnout pierces turtles.
7. **`ROADMAP.md`** — a phased build order from a playable vertical slice to a
   Steam release. Each phase states what becomes playable and what question that
   phase answers. Identify the earliest point at which the game is fun, and get
   there first — in greybox. Phases are defined by systems only; none may be
   gated on art existing, since art arrives incrementally and developer-paced
   throughout. Track art completeness as a separate release gate on its own line. Campaign-first is deliberate: it removes the PvP backend from the
   critical path and makes scripted rival towers double as balance-test fixtures.
   Say explicitly which work in each phase is throwaway and which carries forward
   into ranked.
8. **`OPEN_QUESTIONS.md`** — a living register of every unresolved decision, each
   with options, a recommendation, and what it blocks.

## Phase order

Work in this order and get sign-off between phases. Do not run ahead.

- **Phase 1 — Resolve the open design questions.** Work through the list below.
  Bring options and recommendations, not a finished answer. Output:
  `OPEN_QUESTIONS.md` and a decision log.
- **Phase 2 — The loop and the sim.** `GAME_DESIGN.md` and `SIMULATION_SPEC.md`.
  Nothing else matters if the fight is not readable and the build phase is not
  interesting.
- **Phase 3 — Content and data.** `CONTENT_SCHEMA.md`, plus a first pass at the
  actual catalogue: enough employees, rooms and recipes for a real run.
- **Phase 4 — Technical.** `ARCHITECTURE.md`, `ART_PIPELINE.md`.
- **Phase 5 — Balance and plan.** `BALANCE_PLAN.md`, `ROADMAP.md`.

## Open questions to resolve in Phase 1

**Structure and pacing**
- How many floors, what grid size per floor, and what is the expansion curve?
- How many rounds per run, how long is a fight, how many lives?
- What is the campaign map's shape, and what are its boss encounters?

**Goodwill and the tug-of-war**
- Exactly how does each floor's output aggregate into Goodwill damage, and then
  into the bar?
- **Does Goodwill regenerate?** If it does not, defence is only a delay and turtle
  builds have no identity. If it does, and the attack curve stays flat, a
  defensive build becomes unbreakable. The intended answer is regeneration plus an
  escalating Quarter Close pressure curve — work out the actual shape.
- **Does overflow carry?** A hit larger than remaining Goodwill should push its
  excess into the bar, rewarding burst and alpha-strike timing. Confirm or argue
  against.
- **What pierces Goodwill?** Burnout is the natural candidate — morale damage does
  not appear on a balance sheet. Piercing effects prevent dead air in the opening
  seconds and give turtles a counter. Decide the full set.
- When the bar has been pushed to one side and the trailing player breaks through,
  does the bar travel back through the centre, or does the leader keep their
  ground? This determines whether comebacks exist.
- What does the ledger show, at what granularity, and how does it stay readable at
  combat speed while remaining precise enough to scrub afterwards?

**Floors**
- What makes each floor mechanically distinct beyond "more space"?
- How do floor-targeting abilities work, and does floor assignment stay a real
  decision all run or collapse into one obvious stacking pattern?

**Layers**
- Is furniture a distinct layer in v1, or folded into rooms? Layer bloat is a
  named risk — argue the call.
- What exactly can an employee carry, and does equipment exist separately?

**Greybox and assets**
- What is the full sprite manifest schema, and what is the minimum an entry needs
  before a greybox can be built against it?
- What is the draw-order rule for overhanging sprites, and does it need an explicit
  per-entry bias or does y-sorting suffice?
- What is the greybox palette, given it must sit next to real pack art without
  looking broken?
- What defines "art complete" for release, and how is progress against it tracked
  from the first commit?

**Recipes**
- How many recipes at launch, and how does a player discover them? Design the
  codex alongside the system, not after it.
- Do recipes consume the inputs, and can they be undone?

**The portal**
- What is the risk that makes extraplanar hires a real gamble rather than a
  straight upgrade?
- What unlocks it in each mode, and how does the occult reveal land narratively?

**Async PvP — deferred to post-v1, but design the seams now**
- What exactly is stored in a tower snapshot? This format must be settled early,
  because campaign rival towers use it too and will become the seed ghost pool.
- How are players matched once ranked exists?
- What is the anti-cheat posture, given the client owns the sim?

## Quality bar

- **A player who loses must be able to find out why.** The ledger is the answer,
  and it is a first-class feature, not polish — it doubles as the live defensive
  readout, so it must be designed in Phase 2 alongside the sim, not bolted on
  afterwards. Per-floor contribution and a scrubbable timeline build on top of it.
- **Depth must come from combination, not from quantity.** More employees is not
  more game. Interactions between fewer, sharper pieces is.
- **Every mechanic must be legible on the screen it happens on.** If a synergy
  cannot be seen firing, it will not be believed.
- **The game must be playable and judgeable in greybox.** The campaign ships that
  way. If it is not fun as placeholder rectangles, art will not rescue it — and if
  art is required to understand a screen, that screen's layout is under-specified.
- **Every mixed state must look deliberate.** Part-art, part-greybox is the normal
  view for months, and a build that reads as broken corrodes the developer's own
  judgement about whether the game is working.
- **The satire should be in the mechanics, not just the flavour text.** Severance
  fees, burnout, Goodwill as a defensive stat, middle managers who only retrigger
  other people's work, a combat log that is literally an accounting ledger — the
  joke lands hardest when it is also the strategy.

## Anti-goals

- No real-time input during combat, ever. The fight is a consequence, not a
  performance.
- No mode-specific rules forks. One sim, one content database.
- No mixing art perspectives inside a single view.
- No content authored in code.
- No hardcoded pixel dimensions anywhere outside the sprite manifest.
- No approximate greyboxes. Unknown dimensions are a decision to make, not a
  detail to defer.
- No feature, phase or milestone gated on art existing.
- No monetisation design in this pass. Premium Steam release is the assumption.
- No engine or stack relitigation unless a hard blocker is found, in which case
  state the blocker plainly.

## First response

Do not start writing deliverables yet. Begin by:

1. Reading `docs/DESIGN_BRIEF.md`.
2. Stating your understanding of the design in your own words, in under 200 words,
   so any misreading surfaces immediately.
3. Naming the two or three places where you think this design is most likely to
   fail, and why.
4. Asking the Phase 1 questions you consider most load-bearing — with concrete
   options and a recommendation for each.
