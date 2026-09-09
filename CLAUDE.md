# Company Wars — working rules for agents

Pre-production of a pixel-art auto-battler for Steam. Design is complete and signed
off (D-53); implementation starts at `docs/ROADMAP.md` M0. Read `docs/INDEX.md` to
find the document that governs whatever you are about to change.

## What this repository is
- `docs/` — eight design documents, an open-questions register, an append-only decision log.
- `content/` — the content database (JSON). Every file validates against `schema/content.schema.json`.
- `manifest/` — the sprite manifest. Every visual slot with exact dimensions. Validates against `schema/manifest.schema.json`.
- `tools/planning/` — generators that produced `content/`, `schema/`, `manifest/` and `docs/CONTENT_SCHEMA.md`. Running all four on a clean checkout produces no diff.
- **Stack (D-60):** Godot 4 with C# on .NET 8; the simulation is `src/CompanyWars.Sim`, a class library that references no Godot assembly; Compatibility renderer; X11 on Linux; GodotSteam. `docs/ARCHITECTURE.md` has the project layout.
- `src/` — the .NET libraries and tools (`Sim`, `Content`, `Manifest`, `Playback`, `Tools`); `tests/` — xunit, one project per library; `fixtures/sim/` — the ten recorded conformance fixtures; `game/` — the Godot 4 project: the picker, battle and autopsy screens, screenshot fixtures under `game/__screenshots__/`. `src/CompanyWars.Sim/README.md` lists the readings the spec left open.
- **Positions come from the manifest too.** A `layout` point is where the entry's anchor sits; side B's placement is the mirror of side A's across the canvas, computed, never typed.

## Commands
```
pip install jsonschema
python3 tools/planning/gen_content.py && python3 tools/planning/gen_schema.py \
  && python3 tools/planning/gen_manifest.py && python3 tools/planning/gen_doc.py
git status   # must be clean afterwards
dotnet build CompanyWars.sln && dotnet test CompanyWars.sln
dotnet run --project src/CompanyWars.Tools -- validate-content
dotnet run --project src/CompanyWars.Tools -- validate-manifest   # also writes art_coverage.json
dotnet run --project src/CompanyWars.Tools -- fixtures check       # `fixtures record` regenerates; needs the fixtures-approved label
dotnet build game/CompanyWars.Game.csproj                            # needs only the Godot.NET.Sdk package
xvfb-run godot --path game -- --screenshots       # screenshot fixtures; --headless renders nothing
```

## Rules that differ from defaults
- **Content is data.** Never define an employee, room, recipe, rider, modifier or founder in code. Edit `tools/planning/gen_content.py` (the authoring source) and regenerate; do not hand-edit `content/*.json` or `docs/CONTENT_SCHEMA.md`.
- **The effect vocabulary is closed.** Adding a trigger, action, stat, flag, selector or scope is a schema change, a `SIMULATION_SPEC.md` §6.4 change and a fixture — never a content-only edit. See `docs/CONTENT_SCHEMA.md` §14.
- **No pixel size anywhere except `manifest/sprites.json`.** Layout code reads the manifest.
- **The sim is pure.** `CompanyWars.Sim` references nothing but the .NET base library — never `Godot.NET.Sdk`. No `System.Random`, `DateTime`, `Stopwatch`, `System.IO`, `Math.Pow`, `Math.Sqrt`, and no `float`/`double`/`decimal` anywhere in it. `long` and permille only; RNG and hash in `unchecked uint` (D-27, D-61).
- **The sim's language is C#; the client's is C#.** No GDScript except where a Godot editor plugin demands it. Godot 3 API names are compile errors here, not silent no-ops; if a build fails on a Godot API, check the 4.x docs, do not guess.
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
