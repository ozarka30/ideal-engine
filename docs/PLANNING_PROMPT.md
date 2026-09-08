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
6. **Win condition:** Market share tug-of-war. One shared bar, starts 50/50, both
   sides push, resolves on full claim or the quarterly bell.
7. **Crafting:** Full hidden-recipe combining, using employees, equipment and room
   context.
8. **Hiring:** Shop with paid rerolls. The portal unlocks mid-run and adds a
   second, riskier stock alongside the normal one.
9. **Two modes:** a ranked ladder for competitive players, and a Slay-the-Spire-
   style campaign. **One sim, one content database.** Modes are a configuration
   layer, never a rules fork.
10. **Platform and tech:** Steam. TypeScript + PixiJS + Vite, packaged via Tauri.
    The simulation is a pure headless TypeScript module with no rendering
    dependency.
11. **Art:** GuttyKreum "The Japan Collection", 32x32. Interiors and characters are
    top-down and own the build view; the isometric city packs own the exterior and
    battle view. Never mix perspectives inside one view.

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
   floor's output aggregates into the market share bar, how defensive effects
   resist push, status effect stacking and expiry, the determinism contract, and
   the replay format. This document must be precise enough that two independent
   implementations would agree on every match outcome.
3. **`CONTENT_SCHEMA.md`** — the data model. Schemas for every content type, with
   worked examples, plus the rules for adding new content without touching code.
4. **`ARCHITECTURE.md`** — module boundaries, the sim/render split, state
   management, save format and its migration strategy, the async PvP backend
   (ghost snapshot storage, matchmaking, the cold-start bot pool), Steam
   integration, and the build and packaging pipeline.
5. **`ART_PIPELINE.md`** — how pack tiles become in-game content. Atlas
   generation, the naming convention, the perspective rule and how it is enforced,
   the UI style guide, and the documented process for absorbing a future
   third-party pack through the portal fiction.
6. **`BALANCE_PLAN.md`** — the methodology, not the numbers. Target win-rate
   bands, what the automated matchup harness measures, how a broken build is
   detected, the archetypes that should exist and roughly how they should beat
   each other, and the process for tuning from telemetry.
7. **`ROADMAP.md`** — a phased build order from a playable vertical slice to a
   Steam release. Each phase states what becomes playable and what question that
   phase answers. Identify the earliest point at which the game is fun, and get
   there first.
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
- Does the campaign or the ranked ladder ship first? Which one carries the
  tutorial?

**The tug-of-war**
- Exactly how does each floor's output aggregate into the single bar?
- What stops a runaway lead from ending fights in ten seconds, and what stops
  every fight from timing out at the bell?
- How do defensive builds read as *doing something* on a bar that only moves one
  way at a time? This is the sharpest risk in the whole design.

**Floors**
- What makes each floor mechanically distinct beyond "more space"?
- How do floor-targeting abilities work, and does floor assignment stay a real
  decision all run or collapse into one obvious stacking pattern?

**Layers**
- Is furniture a distinct layer in v1, or folded into rooms? Layer bloat is a
  named risk — argue the call.
- What exactly can an employee carry, and does equipment exist separately?

**Recipes**
- How many recipes at launch, and how does a player discover them? Design the
  codex alongside the system, not after it.
- Do recipes consume the inputs, and can they be undone?

**The portal**
- What is the risk that makes extraplanar hires a real gamble rather than a
  straight upgrade?
- What unlocks it in each mode, and how does the occult reveal land narratively?

**Async PvP**
- What exactly is stored in a ghost snapshot, and how are players matched?
- How is the bot pool seeded so that day-one players fight something credible?
- What is the anti-cheat posture, given the client owns the sim?

## Quality bar

- **A player who loses must be able to find out why.** Post-battle diagnosis —
  timeline, per-floor contribution, what fired and when — is a first-class
  feature, not polish. Design it in Phase 2, not later.
- **Depth must come from combination, not from quantity.** More employees is not
  more game. Interactions between fewer, sharper pieces is.
- **Every mechanic must be legible on the screen it happens on.** If a synergy
  cannot be seen firing, it will not be believed.
- **The satire should be in the mechanics, not just the flavour text.** Severance
  fees, burnout, middle managers who only retrigger other people's work — the
  joke lands hardest when it is also the strategy.

## Anti-goals

- No real-time input during combat, ever. The fight is a consequence, not a
  performance.
- No mode-specific rules forks. One sim, one content database.
- No mixing art perspectives inside a single view.
- No content authored in code.
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
