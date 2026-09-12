# Document index

Read the row that matches what you are about to change. Every document stands alone;
none needs the conversation that produced it.

| Read | Before changing | One line |
| --- | --- | --- |
| `DESIGN_BRIEF.md` | anything in the locked foundation | The decisions that predate every other document. Not relitigated |
| `PLANNING_PROMPT.md` | the process | The brief the design phase was run against |
| `OPEN_QUESTIONS.md` | anything with a `Q-` id, or before proposing a new mechanic | Every open decision with options, a recommendation, and what it blocks. Ten open |
| `DECISION_LOG.md` | any rule, number or format that feels arbitrary | Eighty-five append-only entries with the one-line reason that did the work. Grep it first |
| `GAME_DESIGN.md` | the loop, a screen, a room, an employee, the economy, the campaign | The full design, with pixel rects for every screen in §19 and the human-check list in §21 |
| `SIMULATION_SPEC.md` | `packages/sim`, a status, a selector, the ledger or replay format | The combat sim as a conformance target. Two implementations must agree byte for byte |
| `CONTENT_SCHEMA.md` | `content/`, `schema/content.schema.json`, `tools/planning/gen_content.py` | The closed effect vocabulary, every content type with verbatim examples, the loader's checks, what is and is not a code change |
| `ARCHITECTURE.md` | package layout, state, saves, Steam, CI, the ranked backend | The dependency rule, the sim/render split, the reducer, migrations, gates |
| `ART_PIPELINE.md` | `manifest/`, the renderer, atlases, any asset | The manifest schema, overhang, draw order, the greybox renderer, the validator, the worklist, the art gates |
| `BALANCE_PLAN.md` | `content/balance.json`, any number, the harness | Populations, bands, the counter web, nineteen invariants, the tuning loop, what is not a knob |
| `ROADMAP.md` | scope, order, what to build next | Six milestones with exit criteria; release gates; post-v1; what the human does |
| `RESEARCH_NOTES.md` | the stack, Steam, licensing, or when a genre precedent is claimed | The verification pass: findings, trust levels, what changed because of them |
| `REVENUE_RACE.md` | the fight, while the money rework is open | Draft proposal: the quarter as a revenue race — every fight word mapped to money, the new rules, the counter web, the stages. Approved as D-85; the rule for the race until stages 2–4 fold it into the spec |

Data, not prose:

| Path | What |
| --- | --- |
| `content/index.json` | Every content file and its schema `$def`; the content version |
| `content/balance.json` | The harness's invariants and bands, as data |
| `manifest/sprites.json` | 193 visual slots; the only place a pixel size may be written |
| `manifest/greybox_palette.json` | The seven placeholder tones |
| `schema/*.schema.json` | What every JSON file must satisfy |
