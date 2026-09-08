# Company Wars

An auto-battler where you build a haunted Japanese office tower floor by floor,
hire the staff to fill it, and send it into quarterly combat against another
player's building for market share.

Pre-production. No code yet — design first.

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
| [`manifest/sprites.json`](manifest/sprites.json) | The sprite manifest: 181 visual slots, every one a greybox spec with exact dimensions, anchor, footprint and target path |
| [`manifest/greybox_palette.json`](manifest/greybox_palette.json) | The seven greybox tones |
| [`schema/manifest.schema.json`](schema/manifest.schema.json) | JSON Schema the manifest validates against |

## At a glance

- **Genre** — auto-battler; build round, then async PvP round
- **Depth model** — Backpack Battles: scarce grid, adjacency synergy, hidden recipes
- **Divergence** — two asset classes. Rooms are static commitments; employees are flexible and reoptimised every round
- **Combat** — cooldown duel, nothing moves, market share tug-of-war
- **Setting** — retro Japanese corporate occult
- **Platform** — Steam, via TypeScript + PixiJS + Tauri

## Design phase progress

| Phase | Deliverables | Status |
| --- | --- | --- |
| 1 — Open questions | `OPEN_QUESTIONS.md`, `DECISION_LOG.md` | Complete |
| 2 — Loop and sim | `GAME_DESIGN.md`, `SIMULATION_SPEC.md` | Drafted, awaiting sign-off |
| 3 — Content and data | `CONTENT_SCHEMA.md`, `content/`, `schema/` | Drafted, awaiting sign-off |
| 4 — Technical | `ARCHITECTURE.md`, `ART_PIPELINE.md`, `manifest/` | Drafted, awaiting sign-off |
| 5 — Balance and plan | `BALANCE_PLAN.md`, `ROADMAP.md` | Not started |

Forty-six decisions are logged — thirty-eight craft calls and eight human ones. Ten
items are open, none blocking; each is listed against the phase where it first bites.
See the summary table in
[`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md).
