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
| 1 — Open questions | `OPEN_QUESTIONS.md`, `DECISION_LOG.md` | Drafted, awaiting sign-off |
| 2 — Loop and sim | `GAME_DESIGN.md`, `SIMULATION_SPEC.md` | Not started |
| 3 — Content and data | `CONTENT_SCHEMA.md`, first catalogue pass | Not started |
| 4 — Technical | `ARCHITECTURE.md`, `ART_PIPELINE.md` | Not started |
| 5 — Balance and plan | `BALANCE_PLAN.md`, `ROADMAP.md` | Not started |

Twenty of the twenty-nine Phase 1 questions are closed as craft calls; nine need a
human decision before Phase 2 can proceed without marked assumptions. See the summary
table in [`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md).
