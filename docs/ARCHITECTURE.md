# Company Wars — Architecture

Status: **Phase 4 draft.** Module boundaries, the sim/render split, state management,
the save format and its migrations, Steam integration, the build and packaging
pipeline, and the deferred async PvP backend. Builds on locked decision 10 (TypeScript
+ PixiJS + Vite, packaged via Tauri; the sim is a pure headless module), on
`SIMULATION_SPEC.md`, `CONTENT_SCHEMA.md` and `ART_PIPELINE.md`, and on decisions D-18
to D-20, D-27, D-28 and D-38.

The architecture has one organising principle: **the simulation is a pure function,
and everything else is arranged so that it stays one.** The module graph, the state
model, the save format and the ranked backend are all shaped by that.

---

## Contents

1. [The dependency rule](#1-the-dependency-rule)
2. [Packages](#2-packages)
3. [The sim/render split](#3-the-simrender-split)
4. [State management](#4-state-management)
5. [Save format and migration](#5-save-format-and-migration)
6. [Content and manifest loading](#6-content-and-manifest-loading)
7. [The client](#7-the-client)
8. [Steam integration](#8-steam-integration)
9. [Build and packaging](#9-build-and-packaging)
10. [Testing](#10-testing)
11. [Performance budgets](#11-performance-budgets)
12. [The async PvP backend — deferred](#12-the-async-pvp-backend--deferred)
13. [What v1 must not do](#13-what-v1-must-not-do)

---

## 1. The dependency rule

```
        ┌──────────┐
        │   sim    │  ← depends on nothing. Not even content: it receives resolved definitions.
        └────┬─────┘
             │
   ┌─────────┼─────────────┐
   │         │             │
┌──┴───┐ ┌───┴────┐  ┌─────┴────┐
│content│ │harness │  │  build   │  ← build phase: shop, placement, crafting, economy, rival expander
└──┬───┘ └────────┘  └─────┬────┘
   │                       │
┌──┴───────┐          ┌────┴────┐
│ manifest │          │  game   │  ← PixiJS client: screens, playback, state stores
└──────────┘          └────┬────┘
                           │
                 ┌─────────┴─────────┐
                 │   apps/desktop    │  ← Tauri shell, platform bridge
                 └───────────────────┘
```

Arrows point at dependencies. The rule, enforced by a lint on package imports:

- `sim` imports nothing from the workspace and nothing from npm.
- `content` and `manifest` depend on `sim` only for shared types.
- `build` depends on `sim`, `content`, `manifest`. It is deterministic but not pure:
  it owns the build-phase RNG (shop draws) and the undo stack.
- `game` depends on everything above and on PixiJS. Nothing depends on `game`.
- `harness` depends on `sim`, `content`, `build` (for the expander). Never on `game`.
- `apps/desktop` depends on `game`. It is the only package that knows Tauri exists.

A dependency in the wrong direction fails CI. The one that matters most is the first:
if `sim` ever imports a renderer type, a clock, or a content file, the determinism
contract is already broken.

---

## 2. Packages

A pnpm workspace. TypeScript throughout, strict mode, one `tsconfig` base.

| Package | Purpose | Key exports |
| --- | --- | --- |
| `packages/sim` | `SIMULATION_SPEC.md`, implemented | `simulate(seed, a, b, rules)`, `TowerSnapshot`, `MatchResult`, `LedgerEntry`, `stateHash`, `mulberry32` |
| `packages/content` | Loads and validates `content/` against the schema; resolves ids; exposes typed definitions | `loadContent(dir)`, `ContentDb`, `validateContent`, generated types from the schema |
| `packages/manifest` | Loads and validates `manifest/sprites.json`; derived fields; coverage | `loadManifest`, `validateManifest`, `coverage()`, `overhang()` |
| `packages/build` | The build phase as a reducer over actions; the economy; recipes; the rival template expander; snapshot legality | `applyAction`, `BuildState`, `checkRecipes`, `expandTemplate`, `isConstructible` |
| `packages/game` | The client: screens, the greybox renderer, playback, stores, input | `createGame(platform)` |
| `packages/tools` | CLI: `validate-content`, `validate-manifest`, `slice`, `worklist`, `atlas`, `screenshot`, `packs`, `fixtures` | |
| `packages/harness` | Balance harness: run N matchups from templates and fixtures, assert invariants, emit reports | `runMatrix`, `assertInvariants` |
| `apps/desktop` | Tauri shell; the `Platform` implementation for Steam | |
| `services/ranked` | **Deferred.** Server re-simulation, ghost pool, matchmaking, rating | — |

`content/`, `manifest/`, `schema/`, `fixtures/`, `packs/` and `assets/` are
repository directories, not packages; packages read them by path in development and
from the bundle in release.

---

## 3. The sim/render split

The sim runs once per fight, synchronously, when the player presses Ready:

```
snapshot = build.commit(buildState)            // Ready: Tenure ticks, undo clears
result   = sim.simulate(seed, snapshot, rival, rules)
game.playback.load(result)                     // the renderer now owns a finished result
```

Then the renderer plays `result.entries` against a **playback clock** — a tick counter
it advances at 20, 40 or 80 ticks per second for 1×, 2× and 4×, or jumps to
`result.endTick` for skip. Nothing the renderer does can change `result`; it is data
the sim has already finished producing. The Goodwill bars, the Market Share bar, the
window bursts and the ledger are all views over `entries` filtered to
`tick <= playbackTick`.

Consequences, each of which is a rule:

- **The renderer never calls into `sim` except `simulate()`.** No per-tick sim calls,
  no "ask the sim what the Goodwill is now" — it computes that from entries.
- **Playback state is derived, not stored.** Goodwill at playback tick `t` is
  `capAtStart + Σ goodwillDelta for entries with tick ≤ t`. The renderer keeps a
  running accumulator for speed but can recompute from scratch at any tick, which is
  what makes the autopsy scrubber free.
- **The sim runs on the main thread in v1.** A match is a few milliseconds (§11); a
  worker would add a serialisation boundary for no benefit. If a future content
  volume makes it slow, `simulate` moves to a worker without changing its signature,
  because it already takes and returns plain data.
- **The harness is the same call.** `packages/harness` calls `simulate` in Node with
  no renderer loaded, which is the whole point of the split.

---

## 4. State management

Three stores, deliberately not one, because they have different lifetimes and
different persistence.

### 4.1 RunState — persisted

Everything about the current run that must survive a restart:

```ts
interface RunState {
  schemaVersion: 1;
  contentVersion: string;
  mode: "campaign" | "ranked";
  runSeed: number;            // u32; everything random in the run derives from it
  round: number;              // 1..16
  strikes: number;            // remaining
  budget: number;
  map: { act: number; column: number; row: number; visited: NodeRef[] } | null;
  tower: TowerSnapshot;       // the committed tower — the same type the sim eats
  shop: ShopState;            // current cards per tab, rerolls this round
  modifiers: string[];        // accepted Board Meeting modifiers
  portalOpen: boolean;
  history: FightRecord[];     // per fight: rivalId, seed, result summary, replay path
  rng: number;                // build-phase generator state
}
```

`tower` is a `TowerSnapshot`, not a client-side model that gets converted into one.
There is exactly one representation of a tower, and it is the one the sim reads.

### 4.2 BuildState — transient, reducer-driven

The build phase is a **pure reducer**: `applyAction(state, action) → state`, over a
closed action set (`hire`, `place`, `move`, `layOff`, `sell`, `buyRoom`, `demolish`,
`lease`, `reroll`, `acceptCraft`, `declineCraft`, `ready`). The undo stack is the
**action log**: undo is "replay all but the last action from the round's start
state". This is D-17 made literal — a build round is a function from (start state,
action list) to (end state), and that is what makes it testable and what makes undo
trivially correct.

`BuildState` holds the working tower, the action log, pending craft offers, and the
derived legality/aura/link overlays the screen draws. `ready` produces the committed
`TowerSnapshot` and clears the log.

### 4.3 PlaybackState — transient, derived

`result`, `playbackTick`, `speed`, `paused`, and the running accumulators. Discarded
after the autopsy; the `FightRecord` in `RunState.history` keeps the replay path.

### 4.4 ProfileState — persisted, separate file

Cross-run: codex discoveries, unlocks, settings, statistics, rating (ranked), and the
last founder chosen (`lastFounderId`, pre-selected on the next run). Never read by the
sim. The founder *of the current run* lives in the tower snapshot's `globals`, because
the sim applies its effects — empty in v1 — like a modifier's.

### 4.5 Events

Stores notify the renderer through a minimal typed event bus; no framework. The
renderer subscribes, the stores never know a renderer exists. Input produces actions;
actions go to the reducer; the reducer produces state; state produces events; events
redraw. One direction.

---

## 5. Save format and migration

### 5.1 Files

| File | Contents | Where |
| --- | --- | --- |
| `profile.json` | `ProfileState` | app data dir; Steam Cloud |
| `run.json` | `RunState` for the current run, or absent | app data dir; Steam Cloud |
| `replays/<runId>/<round>.json` | `MatchResult` per fight of the current run | app data dir; not synced |

JSON, pretty-printed, UTF-8. No binary formats (the brief forbids them for content, and
a save file is content the player made). Atomic writes: write to `.tmp`, fsync,
rename. A corrupt file is moved aside with a timestamp, never overwritten; the game
starts from the profile if the run is unreadable and from defaults if both are.

### 5.2 Versioning

Every file carries `schemaVersion` and `contentVersion`. Two independent axes:

- **Schema.** A change to the shape of `RunState`, `ProfileState` or
  `TowerSnapshot`. Migrated by code.
- **Content.** A change to what ids exist. A run saved on content `0.1.0` and loaded
  on `0.2.0` runs the content migration table (`content/migrations.json`, a list of
  `{ from, to, renames: {old: new}, retired: [ids] }`) over every id in the tower and
  shop. A retired id with no rename is replaced by a refund of its cost — the fiction
  is a resignation — and the load reports it.

### 5.3 Migrators

`packages/build/migrate/`: one function per schema step, `v1→v2`, `v2→v3`, chained.
Each is pure, takes the previous version's shape and returns the next, and ships with
a fixture pair (`fixtures/saves/v1_run.json` → expected `v2`). CI runs every fixture
through the full chain to the current version. A migrator is never deleted while any
fixture references it.

The `TowerSnapshot` inside a save migrates with the same code that migrates snapshots
in the ranked ghost pool (§12), because it is the same type. That is why the snapshot
was kept to exactly what the sim reads (D-18): the smaller the type, the fewer the
migrations.

### 5.4 Determinism across save and load

Everything random in a run derives from `runSeed`: fight seeds are
`hash(runSeed, round)`, shop draws come from the stored build-phase generator, template
expansions take `hash(runSeed, round, nodeIndex)`. Saving and loading mid-build
reproduces the same shop, the same rival, and — since the tower is the snapshot — the
same fight. A saved run is a replayable run.

---

## 6. Content and manifest loading

Both load at start-up, both validate fully before the first screen appears, and both
are immutable after that.

- **Content.** `loadContent` reads `content/index.json`, validates every file against
  its `$def`, runs the referential checks from `CONTENT_SCHEMA.md` §12, resolves every
  id to an object, and returns a frozen `ContentDb`. The sim receives resolved
  definitions from it — a map from id to definition — and never the loader.
- **Manifest.** `loadManifest` reads `manifest/sprites.json`, validates, recomputes
  `overhang` and asserts equality, checks every content `sprite`/`tile`/`icon`
  reference resolves, and returns a frozen map. The asset loader then, per entry,
  either decodes the file at `sprite.asset` or synthesises a placeholder texture.
- **Types.** `packages/content` generates TypeScript types from
  `schema/content.schema.json` at build time (`json-schema-to-typescript`), so a
  schema change is a compile error everywhere it matters.

In development, a file watcher reloads content and manifest on change and rebuilds
the current screen. Hot reload of content is the fastest balance loop there is, and
it costs nothing once loading is a pure function of the directory.

---

## 7. The client

PixiJS 8, one `Application`, one root container per screen, one screen active at a
time. Screens: `menu`, `founder`, `map`, `build`, `battle`, `autopsy`, `codex`,
`reward` (the result banner and win bonus — not an overlay of picks).

- **Rendering.** A single `RenderList` per frame: every visible entry becomes a
  `(sortKey, entryId, x, y, frame)` tuple, sorted by the five-key order
  (`ART_PIPELINE.md` §3), drawn in that order. No scene-graph z-ordering; the list is
  the order. Positions are integers before they reach PixiJS, asserted in debug.
- **Scaling.** The root container scales by the integer factor for the window;
  letterboxed with the `structure` tone. `nearest` filtering set once, globally, on
  the base texture defaults.
- **Input.** Mouse and keyboard in v1. Actions, not callbacks: a drag-drop on the
  build screen ends in `applyAction({ type: "place", ... })`. Controller navigation
  is post-v1 and is listed in `OPEN_QUESTIONS.md` as such.
- **Text.** A bitmap font from `font.ui.8`; PixiJS `BitmapText` at 1× and 2× only.
- **Audio.** Not designed in this pass. A single `audio` facade with no-op
  implementation so that adding it later is not a refactor.

The greybox renderer is not a mode. It is what the asset loader returns when the file
is absent, so the same screen code runs in both states, always.

---

## 8. Steam integration

Behind one interface, so that development never needs Steam running:

```ts
interface Platform {
  init(): Promise<void>;
  storage: { read(name): Promise<string | null>; write(name, data): Promise<void>; list(): Promise<string[]> };
  achievements: { unlock(id): void };
  overlay: { isEnabled(): boolean };
  userId(): string;            // opaque; used only as a ghost-pool owner label later
  shutdown(): void;
}
```

- `NullPlatform` — local filesystem storage, no-op everything else. Used in
  development, in the harness, and as the fallback when Steam is not present.
- `SteamPlatform` — in `apps/desktop`, implemented in the Tauri Rust process over the
  `steamworks` crate, exposed to the webview through a handful of Tauri commands.
  Storage maps to Steam Cloud (`profile.json` and `run.json` only; replays stay
  local). Achievements are a fixed list authored in `content/achievements.json`
  (a Phase 5 addition) and unlocked from `ProfileState` transitions.

The webview never links Steamworks. The Rust side owns the Steam API lifecycle
(`SteamAPI_Init` at launch, callbacks pumped on a timer, shutdown on exit) and the
webview talks to it through the command bridge. That keeps Steam-specific code in one
crate and keeps the TypeScript build identical with or without it.

Steam Deck: 1280 × 800 renders at 2× with a 40-pixel letterbox; text is legible at 2×;
input is mouse-and-keyboard in v1 with controller navigation deferred.

---

## 9. Build and packaging

### 9.1 Development

`pnpm dev` runs Vite with the game against loose files and the source manifest, hot
reloading content, manifest and code. `pnpm harness -- --rounds 1..16 --matches 200`
runs the balance matrix in Node. `pnpm tools <cmd>` runs a tool.

### 9.2 CI, on every commit

```
1. lint + typecheck             (all packages; the dependency-direction lint)
2. validate-content             (schema + referential + snapshot structure + constructibility)
3. validate-manifest            (schema + derived fields + references + perspective + dimensions of present files)
4. sim conformance fixtures     (every fixture in fixtures/sim/ must hash-match)
5. unit tests                   (sim, build reducer, migrations, content loader, manifest loader)
6. screenshot fixtures          (greybox screens must pixel-match)
7. balance smoke                (a few hundred matchups; the invariants in BALANCE_PLAN must hold)
8. build                        (Vite → dist; atlas step; derived manifest)
```

Steps 2–4 are the ones the brief calls out as CI checks rather than review steps.
Step 7 is small on every commit and full on a nightly.

### 9.3 Release

`tauri build` on a matrix of Windows, macOS and Linux runners, producing a bundle per
platform with the atlased `dist/`. Version stamped from the git tag into `package.json`,
`tauri.conf.json` and the About screen. Upload through `steamcmd` with per-platform
depot scripts in `apps/desktop/steam/`, run by a release workflow that only triggers on
a tag. The release workflow additionally runs the **art gate** (`ART_PIPELINE.md` §8)
and the **licence gate** (every pack has a `LICENSE.md` with a positive verdict,
Q-RISK-1) and refuses to upload if either fails. Those two gates exist only in the
release workflow, never in the commit workflow, which is how "no phase is gated on art"
and "the release is" are both true.

---

## 10. Testing

| Layer | What | How |
| --- | --- | --- |
| Sim | Conformance | `fixtures/sim/*` — inputs and recorded `MatchResult`; hash and entry equality (SIMULATION_SPEC §19) |
| Sim | Properties | Integer-only invariants: no NaN, no float, `seq` dense, share clamped, entry count formula for the mirror fixture |
| Build | Reducer | Action-list fixtures: apply, assert end state, assert undo reproduces every intermediate state |
| Build | Recipes | Every recipe in `content/` has a fixture that triggers it, and one near-miss |
| Build | Expander | Every template at every round expands to a constructible snapshot for 50 seeds |
| Content | Loader | The full validation suite; a fixture of each rejection class |
| Manifest | Loader | The full validation suite; a fixture with a present file at the wrong size |
| Migrations | Chain | One fixture per version step |
| Client | Screens | Screenshot fixtures per screen per state |
| Balance | Invariants | The harness, per `BALANCE_PLAN.md` |

There is no end-to-end test that plays the game. The pieces are pure enough that
composing tested pieces is the integration, and the screenshot fixtures catch the
composition.

---

## 11. Performance budgets

Asserted by the harness and the client in debug builds, not aspirations:

| Thing | Budget | Why |
| --- | --- | --- |
| One `simulate()` call | ≤ 5 ms in Node on a laptop, late-run towers | 10,000 matchups in under a minute for the harness |
| Harness, 10,000 matchups | ≤ 60 s single-threaded | Agent-driven balancing in CI |
| Content + manifest load | ≤ 200 ms | Hot reload feels instant |
| Frame at 3× on integrated graphics | ≤ 4 ms | 60 fps with headroom; the render list is a few hundred sprites |
| Save write | ≤ 20 ms | Called on every Ready |
| Bundle | ≤ 150 MB | All packs atlased; no video |

The sim budget is the one that shapes design: a mechanic that needs per-tick pathing
or spatial queries across the whole tower would blow it, and the brief's "nothing
moves" is what keeps it comfortably inside.

---

## 12. The async PvP backend — deferred

Ships after campaign (locked decision 9). Nothing here is built in v1; everything here
is what v1 must *not preclude*. The design is recorded so that adding ranked is
additive (D-18 to D-20).

### 12.1 Components

```
client ──submit snapshot──▶ ranked API ──▶ ghost pool (by round, rating band)
client ◀──ghost + seed────  ranked API
client ──claim result─────▶ ranked API ──▶ re-simulate (packages/sim in Node) ──▶ rating update
```

- **API.** Three calls: `submitSnapshot(round, snapshot)`, `requestMatch(round)`,
  `reportResult(matchId, claimedResult)`. Authenticated with a Steam session ticket
  validated server-side.
- **Ghost pool.** Snapshots keyed by `(round, ratingBand)`, each wrapped in a record
  carrying owner label, rating at capture, `contentVersion`, capture time. Snapshots
  are stored exactly as the client sent them, after **legality validation** — the
  same `isConstructible` and structural checks the content loader runs.
- **Matchmaking.** D-19: draw deterministically from the bucket with the match seed;
  widen rating, then round, then fall back to the template expander with a server
  seed. The fallback is the cold-start answer, and it is the same expander the
  campaign uses.
- **Re-simulation.** D-20: the server runs `simulate(seed, a, b, rules)` from
  `packages/sim` — the identical module — and the server's `MatchResult` is
  authoritative. The client's claimed result is compared by `stateHash`; a mismatch
  is logged and the server's stands. Rating updates from the server result only.
- **Rating.** Elo-style, per fight, stored on the profile server-side and mirrored
  into `ProfileState` for display.

### 12.2 What v1 must preserve

| v1 obligation | Why ranked needs it |
| --- | --- |
| `simulate()` pure, no I/O, no npm deps | It runs unchanged in Node on the server |
| `TowerSnapshot` is exactly what the sim reads | It is the wire format and the ghost format; every extra field is a migration |
| `contentVersion` pinned on every snapshot and result | The server refuses a mismatch instead of silently mis-simulating |
| `stateHash` in every `MatchResult` | The one number client and server compare |
| Constructibility validation in `packages/build` | Reused verbatim as the ghost-pool admission check |
| Template expander deterministic from a seed | Reused verbatim as the cold-start fallback |
| Campaign rivals stored as snapshots in the repo | They seed the pool on day one |

### 12.3 Anti-cheat posture, restated

A client that owns the sim cannot be trusted with a result, and no obfuscation changes
that. The posture is therefore: the client's result is advisory, the server
re-simulates, snapshots are validated for legality before they enter the pool, and the
campaign is deliberately unpoliced. The cost of this posture is one server running one
already-written module; the cost of any other posture is a ladder nobody believes.

### 12.4 Not designed here

Server hosting, region, cost, retention policy, abuse handling, and the client UI for
ranked. All post-v1; none constrains v1.

---

## 13. What v1 must not do

A short list, because each item is easy and each would cost a rewrite later:

- Put a field on `TowerSnapshot` the sim does not read.
- Call `Date`, `Math.random` or `performance.now` anywhere under `packages/sim`.
- Let `packages/game` import `packages/sim` for anything but `simulate` and types.
- Hardcode a pixel size outside `manifest/sprites.json`.
- Ship a content definition in a `.ts` file.
- Branch on `mode` anywhere below `packages/build`'s configuration layer.
- Gate any CI step on the presence of an art file.
