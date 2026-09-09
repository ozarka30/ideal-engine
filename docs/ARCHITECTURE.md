# Company Wars — Architecture

Status: **Phase 4, rewritten for the chosen stack (D-60).** Module boundaries, the
sim/render split, state management, the save format and its migrations, Steam
integration, the build and packaging pipeline, and the deferred async PvP backend.
Builds on `SIMULATION_SPEC.md`, `CONTENT_SCHEMA.md`, `ART_PIPELINE.md`, the
stack research in `RESEARCH_NOTES.md` §9, and decisions D-18 to D-20, D-27, D-28,
D-38, D-42 to D-45 and D-59 to D-61.

**The stack:** Godot 4 (4.6 or later) with C# on .NET 8, the simulation as a plain
.NET class library that references no Godot assembly, the Compatibility (OpenGL)
renderer, the X11 display driver on Linux, and GodotSteam for Steamworks. Locked
decision 10's packaging and language are superseded by D-59 and D-60; its requirement
that the sim be a pure headless module is kept and is the organising principle of
everything below: **the simulation is a pure function, and everything else is arranged
so that it stays one.**

---

## Contents

1. [The dependency rule](#1-the-dependency-rule)
2. [Projects](#2-projects)
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
        ┌───────────────────┐
        │  CompanyWars.Sim  │  ← references nothing but the .NET base library. Not Godot. Not content files.
        └─────────┬─────────┘
                  │
   ┌──────────────┼──────────────────┐
   │              │                  │
┌──┴──────────┐ ┌─┴──────────────┐ ┌─┴──────────────┐
│ .Content    │ │ .Harness       │ │ .Build         │  ← build phase: reducer, economy, recipes, expander, constructibility
└──┬──────────┘ └────────────────┘ └─┬──────────────┘
   │                                 │
┌──┴──────────┐                  ┌───┴────────────┐
│ .Manifest   │                  │  game/ (Godot) │  ← the client: scenes, screens, playback, stores, the Platform implementation
└─────────────┘                  └────────────────┘
```

Arrows point at dependencies. The rule, enforced three ways:

- **Project references.** A .NET project can only use what it references. `Sim`
  references nothing. `Content` and `Manifest` reference `Sim` for shared types.
  `Build` references `Sim`, `Content`, `Manifest`. The Godot project references all
  of them and `Godot.NET.Sdk`; nothing references the Godot project. `Harness`
  references `Sim`, `Content`, `Build` — never the Godot project. `Playback`
  references `Sim` only; the Godot project references it for every battle and autopsy view.
- **A reflection test.** `Sim.Tests` asserts that `CompanyWars.Sim.dll` references
  only `System.*` assemblies. It fails the build if anyone adds a package.
- **A banned-API analyzer.** `Microsoft.CodeAnalysis.BannedApiAnalyzers` with a
  `BannedSymbols.txt` in `Sim` forbids `System.Random`, `System.DateTime`,
  `System.Diagnostics.Stopwatch`, `System.Environment`, every `System.IO` type,
  `System.Math.Pow`, `System.Math.Sqrt`, and the `float`/`double`/`decimal` types.
  Integers and permille only (D-27).

The one that matters most is the first line of the diagram: if `Sim` ever references
Godot, a clock, or a content file, the determinism contract is already broken, and the
balance harness and the future server stop being the same code as the game.

---

## 2. Projects

One solution, `CompanyWars.sln`. C# throughout, `Nullable` enabled,
`TreatWarningsAsErrors`, one `Directory.Build.props`.

| Project | Target | Purpose | Key types |
| --- | --- | --- | --- |
| `src/CompanyWars.Sim` | `net8.0` class library | `SIMULATION_SPEC.md`, implemented | `Simulate(seed, a, b, rules)`, `TowerSnapshot`, `MatchResult`, `LedgerEntry`, `StateHash`, `Mulberry32` |
| `src/CompanyWars.Content` | `net8.0` class library | Loads and validates `content/` against the schema; resolves ids; exposes typed definitions | `ContentDb`, `ContentLoader`, `ContentValidator` |
| `src/CompanyWars.Manifest` | `net8.0` class library | Loads and validates `manifest/sprites.json`; derived fields; coverage | `SpriteManifest`, `ManifestValidator`, `Coverage` |
| `src/CompanyWars.Playback` | `net8.0` class library | Views over a `MatchResult` (§3): the playback clock, the derived Goodwill and share frames, the live ledger with its coalescing and line budget (D-08), the autopsy's timeline, floor bars, findings and filters. References `Sim` only, so the line budget is a unit test | `PlaybackClock`, `MatchView`, `LiveLedger`, `Autopsy` |
| `src/CompanyWars.Build` | `net8.0` class library | The build phase as a reducer over actions; the economy; recipes; the rival template expander; snapshot legality | `BuildReducer`, `BuildState`, `RecipeMatcher`, `TemplateExpander`, `Constructibility` |
| `src/CompanyWars.Harness` | `net8.0` console | Balance harness: populations, invariants, reports | `Matrix`, `Invariants`, `Report` |
| `src/CompanyWars.Tools` | `net8.0` console | `validate-content`, `validate-manifest`, `slice`, `worklist`, `atlas`, `packs`, `screenshot-compare`, `fixtures` | |
| `game/` | Godot 4 project, `CompanyWars.Game.csproj` (`Godot.NET.Sdk`) | The client: scenes, screens, the greybox renderer, playback, stores, input, the `Platform` implementation | `ScreenRouter`, `RenderList`, `GreyboxTextures`, `GodotSteamPlatform` |
| `tests/*.Tests` | xunit, one per library | Conformance fixtures, reducer fixtures, loaders, migrations | |
| `services/CompanyWars.Ranked` | **Deferred.** ASP.NET minimal API | Server re-simulation, ghost pool, matchmaking, rating | — |

`content/`, `manifest/`, `schema/`, `fixtures/`, `packs/` and `assets/` are repository
directories, not projects; the libraries read them by path in development and from
the export's resource pack in release. `tools/planning/` (Python) remains the
authoring source for the catalogue; `CompanyWars.Tools` supersedes its validators once
it exists.

---

## 3. The sim/render split

The sim runs once per fight, synchronously, when the player presses Ready:

```csharp
var snapshot = build.Commit(state);                        // Ready: Tenure ticks, undo clears
var result   = Simulator.Simulate(seed, snapshot, rival, rules);
playback.Load(result);                                     // the client now owns a finished result
```

Then the client plays `result.Entries` against a **playback clock**: a tick counter
advanced in `_Process(delta)` by an accumulator — elapsed seconds times twenty ticks,
never one tick per frame — at 1×, 2× or 4×, or jumped to `EndTick` for skip. The
Goodwill bars, the Market Share bar, the window bursts and the ledger are views over
`Entries` filtered to `Tick <= playbackTick`.

Rules that follow:

- **The client never calls into `Sim` except `Simulate()`.** No per-tick sim calls;
  no "ask the sim what Goodwill is now" — the client computes it from entries.
- **Playback state is derived, not stored.** Goodwill at tick `t` is `CapAtStart` plus
  the sum of `GoodwillDelta` for entries with `Tick <= t`; the client keeps a running
  accumulator for speed and can recompute from scratch at any tick, which is what makes
  the autopsy scrubber free.
- **The sim runs on the main thread in v1.** A match is a few milliseconds (§11). If a
  future content volume makes it slow, `Simulate` moves to a `Task` without changing
  its signature, because it takes and returns plain data.
- **The harness is the same call.** `CompanyWars.Harness` calls `Simulate` from a
  console app with no Godot assembly loaded. That is the whole point of the split.

---

## 4. State management

Three stores, deliberately not one, because they have different lifetimes and
persistence.

### 4.1 RunState — persisted

Everything about the current run that must survive a restart:

```csharp
public sealed record RunState(
    int SchemaVersion,            // 1
    string ContentVersion,
    Mode Mode,                    // Campaign | Ranked
    uint RunSeed,                 // everything random in the run derives from it
    int Round,                    // 1..16
    int Strikes,                  // remaining, from 5 (D-58)
    int Budget,
    MapPosition? Map,             // null in ranked
    TowerSnapshot Tower,          // the committed tower — the same type the sim eats
    ShopState Shop,
    ImmutableArray<string> Modifiers,
    bool PortalOpen,
    ImmutableArray<FightRecord> History,
    uint Rng);                    // build-phase generator state
```

`Tower` is a `TowerSnapshot`, not a client model converted into one. There is exactly
one representation of a tower and it is the one the sim reads (D-43).

### 4.2 BuildState — transient, reducer-driven

The build phase is a **pure reducer**: `BuildReducer.Apply(state, action)` over a
closed action set — `Hire`, `Place`, `Move`, `LayOff`, `Sell`, `BuyRoom`, `Demolish`,
`Relocate`, `Lease`, `Reroll`, `AcceptCraft`, `DeclineCraft`, `Ready`. The undo stack
is the action log; undo replays all but the last action from the round's start state.
This is D-17 made literal, and it is what makes the build phase testable by fixture
and a saved run replayable.

`BuildState` holds the working tower, the action log, pending craft offers, and the
derived overlays the screen draws (legality, aura badges, link lines, compatibility
glow). `Ready` produces the committed `TowerSnapshot` and clears the log.

### 4.3 PlaybackState — transient, derived

`Result`, `PlaybackTick`, `Speed`, `Paused`, and the running accumulators. Discarded
after the autopsy; the `FightRecord` in `RunState.History` keeps the replay path.

### 4.4 ProfileState — persisted, separate file

Cross-run: codex discoveries, unlocks, settings, statistics, rating (ranked), and
`LastFounderId`. Never read by the sim. The founder *of the current run* lives in the
snapshot's `Globals`, because the sim applies its effects like a modifier's (D-46).

### 4.5 Events

Stores raise C# events; screens subscribe. No framework, no Godot signals across the
library boundary — the libraries know nothing of Godot, and the Godot layer adapts
store events to scene updates. Input produces actions; actions go to the reducer; the
reducer produces state; state raises events; events redraw. One direction.

---

## 5. Save format and migration

### 5.1 Files

| File | Contents | Where |
| --- | --- | --- |
| `profile.json` | `ProfileState` | `user://`; Steam Cloud |
| `run.json` | `RunState` for the current run, or absent | `user://`; Steam Cloud |
| `replays/<runId>/<round>.json` | `MatchResult` per fight of the current run | `user://`; not synced |

`System.Text.Json`, indented, UTF-8, with a fixed property order from the record
declarations. No binary formats. Atomic writes: write `.tmp`, flush, rename. A corrupt
file is moved aside with a timestamp, never overwritten; the game starts from the
profile if the run is unreadable and from defaults if both are.

### 5.2 Versioning

Every file carries `SchemaVersion` and `ContentVersion`. Two independent axes:

- **Schema.** A change to the shape of `RunState`, `ProfileState` or `TowerSnapshot`.
  Migrated by code.
- **Content.** A change to what ids exist. A run saved on content `0.1.0` and loaded on
  `0.2.0` runs `content/migrations.json` — `{ from, to, renames, retired }` — over
  every id in the tower and shop. A retired id with no rename is replaced by a refund
  of its cost (the fiction is a resignation) and the load reports it.

### 5.3 Migrators

`CompanyWars.Build/Migrations/`: one pure function per schema step, `V1ToV2`, chained,
each with a fixture pair under `fixtures/saves/`. CI runs every fixture through the
full chain. A migrator is never deleted while a fixture references it. The
`TowerSnapshot` inside a save migrates with the same code that will migrate the ranked
ghost pool, because it is the same type (D-18).

### 5.4 Determinism across save and load

Everything random in a run derives from `RunSeed`: fight seeds are
`Hash(RunSeed, Round)`, shop bags shuffle from the stored build-phase generator,
template expansions take `Hash(RunSeed, Round, NodeIndex)`. Saving and loading
mid-build reproduces the same shop, the same rival, and — since the tower is the
snapshot — the same fight.

---

## 6. Content and manifest loading

Both load at start-up, validate fully before the first screen, and are immutable
afterwards.

- **Content.** `ContentLoader` reads `content/index.json`, validates every file
  against its `$def` with `JsonSchema.Net` (draft 2020-12), runs the referential checks
  in `CONTENT_SCHEMA.md` §12, resolves every id, and returns a frozen `ContentDb`.
  The sim receives resolved definitions from it — a dictionary from id to definition —
  never the loader.
- **Manifest.** `ManifestValidator` reads `manifest/sprites.json`, validates against
  `schema/manifest.schema.json`, recomputes `overhang` and asserts equality, checks
  every content `sprite`/`tile`/`icon` reference resolves, and returns a frozen map.
  `GreyboxTextures` then, per entry, either loads the file at `sprite.asset` or
  synthesises a placeholder `ImageTexture` at the declared size.
- **Types.** C# records for every content type, hand-written to mirror the schema and
  asserted against it by a test that round-trips every content file through the
  records and compares. A schema change without a matching record change fails the
  build.

In development, a file watcher in the Godot project reloads content and manifest on
change and rebuilds the current screen. Hot reload of content is the fastest balance
loop there is.

---

## 7. The client

One Godot project, `game/`, on the **Compatibility renderer** (`rendering/renderer/
rendering_method = gl_compatibility`) — a 2D pixel game loses nothing on it and it is
the overlay-friendly path (§8). One `PackedScene` per screen — `Menu`, `Founder`,
`Map`, `Build`, `Battle`, `Autopsy`, `Codex` — and a `ScreenRouter` autoload that
holds exactly one active.

**Pixel discipline, as project settings, asserted at start-up:**

| Setting | Value |
| --- | --- |
| `display/window/size/viewport_width/height` | 640 × 360 |
| `display/window/stretch/mode` | `viewport` |
| `display/window/stretch/aspect` | `keep` |
| `display/window/stretch/scale_mode` | `integer` |
| `rendering/textures/canvas_textures/default_texture_filter` | `Nearest` |
| `rendering/2d/snap/snap_2d_transforms_to_pixel` | on |
| `display/display_server/driver.linuxbsd` | `x11` |

**Rendering.** The tower, the battle exterior and the inset are each a single `Node2D`
whose `_Draw()` walks a `RenderList` — every visible manifest entry as
`(sortKey, entryId, x, y, frame)` sorted by the five-key total order
(`ART_PIPELINE.md` §3) — and calls `DrawTextureRectRegion` in that order. No
scene-tree z-ordering, no `YSort`; the list is the order, which is what makes greybox
screenshots reproducible. Positions are integers before they reach the draw call,
asserted in debug builds.

**UI panels** are `Control` nodes so the human can do layout in the editor, but their
rects come from the manifest at runtime through a `ManifestRect` component; the editor
shows them, the manifest is the source. No pixel size is typed into a scene file.

**Text.** A `FontFile` imported from the pixel font (`ART_PIPELINE.md` §13) with
filtering off, drawn at 1× and exactly 2× only.

**Input.** Mouse and keyboard in v1; every interactive element registers with a
per-screen focus list from day one so that controller navigation is an adapter later
(D-47). Actions, not callbacks: a drag-drop on the build screen ends in
`reducer.Apply(new Place(...))`.

**Audio.** A single `IAudio` facade with a no-op implementation, so adding sound later
is not a refactor.

The greybox renderer is not a mode. `GreyboxTextures` returns a placeholder for any
absent file, so the same screen code runs in both states, always.

---

## 8. Steam integration

Behind one interface in the Godot project, so that development, tests and the harness
never need Steam running:

```csharp
public interface IPlatform
{
    Task InitAsync();
    IStorage Storage { get; }             // Read/Write/List by name
    void UnlockAchievement(string id);
    bool OverlayEnabled { get; }
    string UserId { get; }                // opaque; a ghost-pool owner label later
    void Shutdown();
}
```

- `NullPlatform` — `user://` storage, no-op everything else. Development, tests,
  the harness, and the fallback when Steam is absent.
- `GodotSteamPlatform` — over **GodotSteam** (GDExtension build, SDK 1.65, with the
  community C# bindings; the module build is the alternative the spike may prefer).
  `SteamInit` in the first autoload; `RunCallbacks` pumped from `_Process`. Storage
  maps to Steam Cloud through the Remote Storage API with write batches, not
  Auto-Cloud (`profile.json` and `run.json` only; replays stay local); Dynamic Cloud
  Sync enabled only once the game reloads on the file-changed callback. Achievements
  are a fixed list in `content/achievements.json` (a Phase 5 addition) unlocked from
  `ProfileState` transitions. Session tickets for the future server come from
  `getAuthTicketForWebApi`, never `GetAuthSessionTicket`.

**The overlay path is pinned**, not assumed. Godot's Vulkan renderer does not get the
overlay when launched outside Steam, and 2026 issues report it missing on SteamOS
under Forward+ and under Wayland. The Compatibility renderer and the X11 driver are
the known-good combination; the M0 spike (§9.4) confirms it on the developer's own
Deck in gaming mode before any screen is built. Text input is never required (D-55),
so the on-screen keyboard is a nicety rather than a dependency.

Steam Deck: 1280 × 800 renders at 2× with a 40-pixel letterbox; the 8 px font is 16 px
on screen; mouse-and-keyboard in v1 with a shipped Steam Input default template.

---

## 9. Build and packaging

### 9.1 Development

`dotnet build` builds every library and the Godot project's assembly. `dotnet test`
runs every fixture without Godot. `dotnet run --project src/CompanyWars.Harness --
--rounds 1..16 --matches 200` runs the balance matrix. The editor opens `game/`;
`godot --path game` runs the client from the command line.

### 9.2 CI, on every commit

```
1. dotnet build (warnings as errors); the reflection test on Sim's references
2. validate-content            (schema + referential + snapshot structure + constructibility)
3. validate-manifest           (schema + derived fields + references + perspective + dimensions of present files)
4. sim conformance fixtures    (every fixture in fixtures/sim/ hash-matches, in dotnet test)
5. unit tests                  (sim properties, build reducer, migrations, loaders)
6. screenshot fixtures         (xvfb-run godot --path game -- --screenshots; Mesa llvmpipe; byte-identical PNGs)
7. balance smoke               (a few hundred matchups; the invariants in BALANCE_PLAN must hold)
8. fixture guard               (fails if fixtures/** changed without a human-applied `fixtures-approved` label)
9. export                      (godot --headless --export-release for each platform; atlas step; derived manifest)
```

Steps 2–4 and 7 are the checks the brief calls CI checks rather than review steps.
Step 6 needs a virtual display because `--headless` renders nothing; the gdUnit4
GitHub Action already runs Godot this way. Step 8 exists because agents that can edit
a test will, under pressure, edit the test; a solo developer cannot approve their own
pull request, so a label only a person can apply is the guard, and the same rule is a
`PreToolUse` hook in `.claude/settings.json`. Runners use `chickensoft-games/setup-godot`
for the .NET-enabled editor and export templates on all three OSes.

### 9.3 Release

A tag-triggered workflow: `godot --headless --export-release` for Windows, macOS and
Linux with the official export templates (never custom-built ones, which fail on the
Deck's glibc); the Linux depot set to **Steam Linux Runtime 4.0 (steamrt4)** in
Steamworks; the macOS export signed and notarised from a Linux runner with `rcodesign`,
with `libsteam_api.dylib` in `Contents/MacOS` and the two Steamworks entitlements;
Windows unsigned (Steam does not require it). Version stamped from the tag into the
project and the About screen. Upload through `game-ci/steam-deploy` with a restricted
build account. The release workflow alone runs the **art gates** (`ART_PIPELINE.md`
§8, D-56) and the **licence gate** (Q-RISK-1), which is how "no phase is gated on art"
and "the release is" are both true (D-44).

**Test builds before release.** `.github/workflows/builds.yml` exports Windows, Linux and macOS
on every push to `main` with the official templates, packs `content/`, `schema/` and
`manifest/` beside the executable (inside the bundle on macOS), and refreshes a rolling
`nightly` pre-release. There is no web build: Godot 4 cannot export C# to the web (D-63).

### 9.4 The M0 stack spike

Before any screen exists, one day, on the developer's own Deck:

1. A hello-world Godot 4 C# export with the settings in §7, as a real app-ID build,
   launched from the Steam client on Windows, macOS and the Deck in **gaming mode**;
   confirm the overlay, an achievement toast, and Cloud sync. Repeat with Forward+ and
   with Wayland to document which combinations break.
2. The same Linux build inside the steamrt4 container; confirm GodotSteam's shared
   library loads.
3. The sim library referenced from both an xunit project and the Godot project; the
   mirror fixture hash-matching in both; ten thousand matches timed standalone.
4. A greybox screenshot under `xvfb-run` on an Ubuntu runner, twice and on two runner
   images, byte-identical.
5. The macOS export signed and notarised from Linux; Gatekeeper launch; overlay under
   Metal.
6. The .NET version Godot pins matches the intended server runtime.

---

## 10. Testing

| Layer | What | How |
| --- | --- | --- |
| Sim | Conformance | `fixtures/sim/*` — inputs and recorded `MatchResult`; hash and entry equality (SIMULATION_SPEC §19) |
| Sim | Properties | No non-integer anywhere; `Seq` dense; share clamped; the mirror fixture's entry count |
| Sim | Purity | The reflection test on assembly references; the banned-API analyzer |
| Build | Reducer | Action-list fixtures: apply, assert end state, assert undo reproduces every intermediate |
| Build | Recipes | Every recipe has a fixture that triggers it and one near-miss |
| Build | Expander | Every template at every round expands to a constructible snapshot for 50 seeds |
| Content | Loader | The full validation suite; one fixture per rejection class; the record/schema round-trip |
| Manifest | Loader | The full validation suite; a fixture with a present file at the wrong size |
| Migrations | Chain | One fixture per version step |
| Client | Screens | Screenshot fixtures per screen per state, under xvfb, byte-exact |
| Client | Scenes | gdUnit4 for the few things that need a scene tree: the router, the reducer adapter |
| Balance | Invariants | The harness, per `BALANCE_PLAN.md` |

There is no end-to-end test that plays the game. The pieces are pure enough that
composing tested pieces is the integration, and the screenshot fixtures catch the
composition.

---

## 11. Performance budgets

Asserted by the harness and the client in debug builds:

| Thing | Budget | Why |
| --- | --- | --- |
| One `Simulate()` call | ≤ 5 ms on a laptop, late-run towers, .NET 8 JIT | 10,000 matchups in under a minute |
| Harness, 10,000 matchups | ≤ 60 s single-threaded | Agent-driven balancing in CI |
| Content + manifest load | ≤ 200 ms | Hot reload feels instant |
| Frame at 3× on integrated graphics | ≤ 4 ms | 60 fps with headroom; a few hundred draw calls |
| Save write | ≤ 20 ms | Called on every Ready |
| Export | ≤ 150 MB with the .NET runtime embedded | All packs atlased; no video |
| Cold start on the Deck | ≤ 3 s | Measured in the spike |

---

## 12. The async PvP backend — deferred

Ships after campaign (locked decision 9). Nothing here is built in v1; everything here
is what v1 must not preclude (D-18 to D-20).

### 12.1 Components

```
client ──submit snapshot──▶ ranked API ──▶ ghost pool (by round, rating band, strength score)
client ◀──ghost + seed────  ranked API
client ──claim result─────▶ ranked API ──▶ re-simulate (CompanyWars.Sim in ASP.NET) ──▶ rating update
```

- **API.** `SubmitSnapshot(round, snapshot)`, `RequestMatch(round)`,
  `ReportResult(matchId, claimed)`; authenticated with a web-API ticket from
  `getAuthTicketForWebApi`, validated server-side.
- **Ghost pool.** Snapshots keyed by `(round, ratingBand, strengthScore)` — the
  harness's own scoring of the snapshot, because players cannot gauge a ghost from its
  record — stored exactly as sent after **legality validation** (`Constructibility`,
  the same class the loader uses). The pool **decays** rather than replacing beaten
  ghosts with their beaters, so its difficulty does not drift upward.
- **Matchmaking.** D-19: draw deterministically with the match seed; widen rating,
  then round, then fall back to `TemplateExpander` with a server seed — the cold-start
  answer, and the same expander the campaign uses.
- **Re-simulation.** D-20: the server references `CompanyWars.Sim.dll` — the identical
  assembly — and its `MatchResult` is authoritative; the client's is compared by
  `StateHash` and a mismatch is logged.
- **Rating.** Elo-style, per fight, server-side, mirrored into `ProfileState`.

### 12.2 What v1 must preserve

| v1 obligation | Why ranked needs it |
| --- | --- |
| `Sim` references nothing but the base library | The same DLL runs in ASP.NET unchanged |
| `TowerSnapshot` is exactly what the sim reads | It is the wire format and the ghost format |
| `ContentVersion` on every snapshot and result | The server refuses a mismatch |
| `StateHash` in every `MatchResult` | The one number client and server compare |
| `Constructibility` in `Build` | Reused verbatim as the pool's admission check |
| `TemplateExpander` deterministic from a seed | Reused verbatim as the cold-start fallback |
| Campaign rivals stored as snapshots in the repo | They seed the pool on day one |

### 12.3 Anti-cheat posture, restated

A client that owns the sim cannot be trusted with a result. The client's result is
advisory, the server re-simulates with the same assembly, snapshots are validated for
legality before entering the pool, and the campaign is deliberately unpoliced.

---

## 13. What v1 must not do

- Put a field on `TowerSnapshot` the sim does not read.
- Reference `Godot.NET.Sdk`, `System.Random`, `DateTime` or any floating-point type
  from `CompanyWars.Sim`.
- Let the Godot project reach into `Sim` for anything but `Simulate` and types.
- Type a pixel size into a scene file or a script; the manifest is the only place.
- Ship a content definition in a `.cs` file.
- Branch on `Mode` anywhere below `Build`'s configuration layer.
- Gate any CI step on the presence of an art file.
- Use the Forward+ renderer or the Wayland driver in a shipped build until a spike has
  shown the overlay working with them on the Deck.
