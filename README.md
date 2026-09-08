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

## At a glance

- **Genre** — auto-battler; build round, then async PvP round
- **Depth model** — Backpack Battles: scarce grid, adjacency synergy, hidden recipes
- **Divergence** — two asset classes. Rooms are static commitments; employees are flexible and reoptimised every round
- **Combat** — cooldown duel, nothing moves, market share tug-of-war
- **Setting** — retro Japanese corporate occult
- **Platform** — Steam, via TypeScript + PixiJS + Tauri
