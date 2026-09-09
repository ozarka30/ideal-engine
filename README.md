# Company Wars

An auto-battler where you build a haunted Japanese office tower floor by floor,
hire the staff to fill it, and send it into quarterly combat against another
player's building for market share.

Pre-production. No code yet — design first. The design phase is complete in draft:
eight documents, a content database, a sprite manifest, and the schemas they validate
against. The first implementation milestone is `ROADMAP.md` M0.

## Documents

| File | Purpose |
| --- | --- |
| [`docs/DESIGN_BRIEF.md`](docs/DESIGN_BRIEF.md) | Locked decisions, art direction, systems sketch, known risks |
| [`docs/PLANNING_PROMPT.md`](docs/PLANNING_PROMPT.md) | Master prompt for the design phase — paste into a fresh planning session alongside the brief |
| [`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md) | Living register of open design questions — options, recommendation, and what each blocks |
| [`docs/DECISION_LOG.md`](docs/DECISION_LOG.md) | Append-only record of decisions taken, with the reasoning that did the work |
| [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) | The full design — loop, build phase, economy, floors, rooms, staff, statuses, recipes, portal, modes, screens |
| [`docs/SIMULATION_SPEC.md`](docs/SIMULATION_SPEC.md) | The combat sim as an implementable spec — tick loop, arithmetic, targeting, ledger and replay formats, determinism contract |
| [`docs/CONTENT_SCHEMA.md`](docs/CONTENT_SCHEMA.md) | The data model — the closed effect vocabulary, every content type with verbatim examples, validation rules, how to add content without code |
| [`content/`](content/) | The content database: forty employees, sixteen rooms, fourteen furniture, forty recipes, riders, modifiers, eight founders, floors, economy, shop, modes, map, six rival templates, nine scripted rival towers |
| [`schema/content.schema.json`](schema/content.schema.json) | JSON Schema every content file validates against |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Package graph, sim/render split, state stores, save format and migrations, Steam, CI and release, the deferred ranked backend |
| [`docs/ART_PIPELINE.md`](docs/ART_PIPELINE.md) | The manifest, footprint vs bounds, draw order, the greybox renderer, validation, tooling, the art-complete gate, pixel discipline, perspective rule |
| [`manifest/sprites.json`](manifest/sprites.json) | The sprite manifest: 182 visual slots, every one a greybox spec with exact dimensions, anchor, footprint and target path |
| [`manifest/greybox_palette.json`](manifest/greybox_palette.json) | The seven greybox tones |
| [`schema/manifest.schema.json`](schema/manifest.schema.json) | JSON Schema the manifest validates against |
| [`docs/BALANCE_PLAN.md`](docs/BALANCE_PLAN.md) | The harness, the populations, the bands, the six-archetype counter web, seventeen CI invariants, the tuning loop, what is not a knob |
| [`content/balance.json`](content/balance.json) | The invariants as data the harness reads |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) | Six milestones from foundations to ship-ready, each with what becomes playable and the question it answers; release gates on their own lines; post-v1; what the human does |
| [`docs/RESEARCH_NOTES.md`](docs/RESEARCH_NOTES.md) | The post-sign-off verification pass: genre practice, stack risks, Steam requirements, licensing, agentic process — what was found, how far to trust it, what changed |
| [`docs/INDEX.md`](docs/INDEX.md) | Routing table: which document to read before changing what |
| [`src/`](src/) | The .NET 8 libraries: `CompanyWars.Sim` (the spec, implemented; references nothing but the base library), `CompanyWars.Content` (loader and §12 validator), `CompanyWars.Manifest` (loader, derived fields, coverage), `CompanyWars.Playback` (views over a result: clock, bars, the coalesced ledger, the autopsy), `CompanyWars.Build` (the reducer, economy, shop bags, legality, the template expander, the run), `CompanyWars.Harness` (the balance smoke), `CompanyWars.Tools` (`validate-content`, `validate-manifest`, `fixtures`, `screenshot-compare`) |
| [`tests/`](tests/) | xunit, one project per library: the purity tests on the sim assembly, the §20 trace asserted tick by tick, the record round-trip, twenty-six content rejection fixtures, the wrong-size asset check |
| [`fixtures/sim/`](fixtures/sim/) | The ten conformance fixtures of `SIMULATION_SPEC.md` §19, recorded inputs and results; read-only to agents |
| [`game/`](game/) | The Godot 4 project: menu, founder select, build, battle, autopsy and summary screens, the debug picker, screenshot fixtures |
| [`src/CompanyWars.Sim/README.md`](src/CompanyWars.Sim/README.md) | Where each spec section lives in code, and the eleven readings the spec left open |

## At a glance

- **Genre** — auto-battler; build round, then async PvP round
- **Depth model** — Backpack Battles: scarce grid, adjacency synergy, hidden recipes
- **Divergence** — two asset classes. Rooms are static commitments; employees are flexible and reoptimised every round
- **Combat** — cooldown duel, nothing moves, market share tug-of-war
- **Setting** — retro Japanese corporate occult
- **Platform** — Steam, via Godot 4 with C# (D-60); the simulation is a plain .NET class library with no engine dependency, so the balance harness and the future server run the same code as the game

## Design phase progress

| Phase | Deliverables | Status |
| --- | --- | --- |
| 1 — Open questions | `OPEN_QUESTIONS.md`, `DECISION_LOG.md` | Signed off (D-53) |
| 2 — Loop and sim | `GAME_DESIGN.md`, `SIMULATION_SPEC.md` | Signed off (D-53) |
| 3 — Content and data | `CONTENT_SCHEMA.md`, `content/`, `schema/` | Signed off (D-53) |
| 4 — Technical | `ARCHITECTURE.md`, `ART_PIPELINE.md`, `manifest/` | Signed off (D-53) |
| 5 — Balance and plan | `BALANCE_PLAN.md`, `ROADMAP.md`, `content/balance.json` | Signed off (D-53) |

Sixty-two decisions are logged — forty-five craft calls and seventeen human ones.
The design phase is signed off (D-53) and a research verification pass has been run;
its findings are in `docs/RESEARCH_NOTES.md`, and the stack is Godot 4 with C# (D-60) after Tauri was dropped (D-59); five strikes
are adopted (D-58); the title still needs a registry search. Eight items are open, none blocking M0; each is listed against the phase where it first bites.
See the summary table in
[`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md).

## Implementation progress

M0 (foundations) is landed except for the Steam Deck spike; M1 (the fight) and M2 (the loop) are landed except
for the human judgements each ends on. `dotnet build CompanyWars.sln && dotnet test CompanyWars.sln` runs the
suites green, the ten sim fixtures hash-match across processes, the validators run green on the committed
files and red on every rejection fixture, five screens render byte-identically under a virtual display, and
the balance harness runs the three smoke invariants (currently failing, as a first pass should). Status and
what remains are in `docs/ROADMAP.md` §2–§4.
