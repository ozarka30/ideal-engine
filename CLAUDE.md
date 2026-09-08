# Company Wars — working rules for agents

Pre-production of a pixel-art auto-battler for Steam. Design is complete and signed
off (D-53); implementation starts at `docs/ROADMAP.md` M0. Read `docs/INDEX.md` to
find the document that governs whatever you are about to change.

## What this repository is
- `docs/` — eight design documents, an open-questions register, an append-only decision log.
- `content/` — the content database (JSON). Every file validates against `schema/content.schema.json`.
- `manifest/` — the sprite manifest. Every visual slot with exact dimensions. Validates against `schema/manifest.schema.json`.
- `tools/planning/` — generators that produced `content/`, `schema/`, `manifest/` and `docs/CONTENT_SCHEMA.md`. Running all four on a clean checkout produces no diff.
- No game code exists yet. The first package is `packages/sim` (see the roadmap).

## Commands
```
pip install jsonschema
python3 tools/planning/gen_content.py && python3 tools/planning/gen_schema.py \
  && python3 tools/planning/gen_manifest.py && python3 tools/planning/gen_doc.py
git status   # must be clean afterwards
```

## Rules that differ from defaults
- **Content is data.** Never define an employee, room, recipe, rider, modifier or founder in code. Edit `tools/planning/gen_content.py` (the authoring source) and regenerate; do not hand-edit `content/*.json` or `docs/CONTENT_SCHEMA.md`.
- **The effect vocabulary is closed.** Adding a trigger, action, stat, flag, selector or scope is a schema change, a `SIMULATION_SPEC.md` §6.4 change and a fixture — never a content-only edit. See `docs/CONTENT_SCHEMA.md` §14.
- **No pixel size anywhere except `manifest/sprites.json`.** Layout code reads the manifest.
- **The sim is pure.** `packages/sim` imports nothing. No `Date`, `Math.random`, `performance`, `Math.pow`, `Math.sqrt`, float division. Integers and permille only (D-27).
- **Fixtures are read-only to agents.** If a spec and a fixture disagree, stop and report which; do not change either. A hook blocks writes under `fixtures/`.
- **One knob per commit** when tuning numbers. The Quarter Close curve, the bar scale, tick rate, grid sizes, Tenure tiers and retrigger depth are not knobs (`docs/BALANCE_PLAN.md` §9).
- **Decisions are appended, never edited.** A reversal is a new entry in `docs/DECISION_LOG.md` naming what it supersedes. Before changing a rule, grep the log for it.
- **Never gate a CI step on an art file existing.** Absent assets are valid; a dimension mismatch on a present one is not.
- **Never require text input on any screen** (D-55).

## Gotchas
- Round = fight. Campaign interludes do not advance the round (D-31).
- A room is a zone over tiles, not an object consuming them (D-12).
- Push has no target; only status and retrigger effects have selectors (D-32).
- `globals.founderId` is in every snapshot; a founder's effects apply like a modifier's and are empty in v1 (D-46).
- The sixteen-fight loop against templated rivals *is* ranked's loop (D-49). Do not build campaign-only shortcuts into it.
