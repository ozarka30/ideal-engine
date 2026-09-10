# Company Wars — working rules for agents

Pre-production of a pixel-art auto-battler for Steam. Design is complete and signed
off (D-53); implementation starts at `docs/ROADMAP.md` M0. Read `docs/INDEX.md` to
find the document that governs whatever you are about to change.

## What this repository is
- `docs/` — eight design documents, an open-questions register, an append-only decision log.
- `content/` — the content database (JSON). Every file validates against `schema/content.schema.json`.
- `manifest/` — the sprite manifest. Every visual slot with exact dimensions. Validates against `schema/manifest.schema.json`.
- `tools/planning/` — generators that produced `content/`, `schema/`, `manifest/` and `docs/CONTENT_SCHEMA.md`. Running all four on a clean checkout produces no diff.
- **Stack (D-60):** Godot 4 with C# on .NET 8; the simulation is `src/CompanyWars.Sim`, a class library that references no Godot assembly; Compatibility renderer; X11 on Linux; GodotSteam. `docs/ARCHITECTURE.md` has the project layout. The client assembly alone targets `net9.0` (Godot 4.7's Android template requires it); the repository's `global.json` rolls forward to any newer SDK.
- `src/` — the .NET libraries and tools (`Sim`, `Content`, `Manifest`, `Playback`, `Build`, `Harness`, `Tools`); `tests/` — xunit, one project per library; `fixtures/sim/` — the ten recorded conformance fixtures; `game/` — the Godot 4 project: the picker, battle and autopsy screens, screenshot fixtures under `game/__screenshots__/`. `src/CompanyWars.Sim/README.md` lists the readings the spec left open.
- **Positions come from the screen's scene (D-77).** Each `game/scenes/<Screen>.tscn` has a `Layout` node whose children are the slots; drag them in Godot's 2D editor and the game follows, with nothing to convert. `SceneLayout` reads them and hides the guides at runtime. The manifest keeps sizes, anchors, footprints and draw order and no longer owns position — its `layout` field is now unread by anything and should be deleted. `python3 tools/dev/scenes.py` scaffolds a screen's Layout from the manifest once; after that the scene is the truth and it refuses to overwrite. Side B's placement is still the mirror of side A's across the canvas, computed, never typed.

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
dotnet run --project src/CompanyWars.Harness -- --rounds 1,6,12,16   # balance smoke; --seeds N; --json writes harness_smoke.json
dotnet build game/CompanyWars.Game.csproj                            # needs only the Godot.NET.Sdk package
xvfb-run -s "-screen 0 1920x1080x24" godot --path game --resolution 1280x720 -- --screenshots   # 2x fixtures; run again at 1920x1080 for 3x
xvfb-run godot --path game -- --drive tools/dev/drive/first_round.json   # plays taps/keys/shots; writes game/__screenshots__/drive/
tools/dev/godot.sh [--templates]                  # prints the Godot 4.7.2 mono path, downloading it into ~/.cache/companywars if absent
python3 tools/dev/scenes.py                       # scaffolds a screen's Layout node from the manifest once (D-77); refuses to overwrite an existing one
python3 tools/dev/gallery.py                      # build/gallery.html: every screen, the last drive run, and any reference shots under game/__screenshots__/references/
python3 tools/dev/worklist.py                     # docs/ART_WORKLIST.md: every slot without art, ranked, with its exact path and size; creates the folders
python3 tools/dev/packs.py                        # docs/pack_index.csv: every PNG in packs/ with its size; docs/PACKS.md maps candidateSource to pack
python3 tools/dev/cut_sprites.py                  # re-cuts every sprite whose candidateSource names a pack file (D-70, D-71)
python3 tools/dev/tilesets.py --write             # mirrors all 59 pack tilemaps under res:// and gives each a TileSet palette (gitignored; run once per checkout)
python3 tools/dev/rooms.py                        # bakes game/scenes/rooms/*.tscn into the room plan PNGs (D-78)
python3 tools/dev/ui.py                           # builds the UI chrome from tools/dev/ui/slots.txt (D-74)
```

## Driving the game from an agent
- `.mcp.json` registers the Godot MCP server (`tools/dev/godot-mcp.sh` → Coding-Solo/godot-mcp): `run_project`,
  `get_debug_output`, `stop_project`, `get_project_info`. Its `run_project` passes no arguments, so park a drive
  script at `tools/dev/drive/current.json` (gitignored) before calling it; the game runs the script and prints
  `[drive]` lines that `get_debug_output` returns while the game is still running: end a script meant for the MCP
  with a long `wait` rather than `quit`, read the output, then `stop_project`. Shots land in
  `game/__screenshots__/drive/` (gitignored).
- Drive steps: `{"run": {"founder": "founder.sato", "seed": 1}}`, `{"scene": "res://…"}`, `{"tap": [x, y]}` in
  1× canvas pixels, `{"key": "Enter"}`, `{"wait": frames}`, `{"shot": "name"}`, `{"quit": true}`.
- Subagents in `.claude/agents/`: `playtest-reviewer` (Sonnet, reads shots against §19–§20) and `doc-check`
  (Haiku, doc drift). Use them for the review passes; the main session does the edits.

## Rules that differ from defaults
- **Content is data.** Never define an employee, room, recipe, rider, modifier or founder in code. Edit `tools/planning/gen_content.py` (the authoring source) and regenerate; do not hand-edit `content/*.json` or `docs/CONTENT_SCHEMA.md`.
- **The effect vocabulary is closed.** Adding a trigger, action, stat, flag, selector or scope is a schema change, a `SIMULATION_SPEC.md` §6.4 change and a fixture — never a content-only edit. See `docs/CONTENT_SCHEMA.md` §14.
- **No pixel size anywhere except `manifest/sprites.json`.** Layout code reads the manifest.
- **Text is not pixel art (D-67).** The two faces in `game/fonts/` render as antialiased vectors at the window's resolution; everything else scales by the integer factor with nearest filtering (`canvas_items` stretch). Do not bake fonts to bitmaps. The font files are licensed for the game, not for redistribution: keep the repository private.
- **The sim is pure.** `CompanyWars.Sim` references nothing but the .NET base library — never `Godot.NET.Sdk`. No `System.Random`, `DateTime`, `Stopwatch`, `System.IO`, `Math.Pow`, `Math.Sqrt`, and no `float`/`double`/`decimal` anywhere in it. `long` and permille only; RNG and hash in `unchecked uint` (D-27, D-61).
- **The sim's language is C#; the client's is C#.** No GDScript except where a Godot editor plugin demands it. **Read the current Godot documentation before using an API, not only when the build fails.** A compile error is the cheap failure; the expensive one is an API that compiles and behaves differently from memory — `SubViewport.UpdateMode.Once` disables the viewport after one frame, which a build will never tell you. Godot 3 API names are compile errors here, not silent no-ops.
- **Fixtures are read-only to agents.** If a spec and a fixture disagree, stop and report which; do not change either. A hook blocks writes under `fixtures/`.
- **One knob per commit** when tuning numbers. The Quarter Close curve, the bar scale, tick rate, grid sizes, Tenure tiers and retrigger depth are not knobs (`docs/BALANCE_PLAN.md` §9).
- **Decisions are appended, never edited.** A reversal is a new entry in `docs/DECISION_LOG.md` naming what it supersedes. Before changing a rule, grep the log for it.
- **Never gate a CI step on an art file existing.** Absent assets are valid; a dimension mismatch on a present one is not.
- **Art goes at the manifest's path.** A PNG at `sprite.asset` (relative to the repository root, now under `game/assets/` so `res://` can reach it — D-77) at exactly `sprite.w × sprite.h` replaces the placeholder, except a `ui` entry, which may be any integer multiple of it (D-76); `docs/ART_WORKLIST.md` lists every slot. Source packs stay under `packs/` and out of git; commit only the cut sprites.
- **Never require text input on any screen** (D-55).

## Gotchas
- Round = fight. Campaign interludes do not advance the round (D-31).
- A room is a zone over tiles, not an object consuming them (D-12).
- Push has no target; only status and retrigger effects have selectors (D-32).
- `globals.founderId` is in every snapshot; a founder's effects apply like a modifier's and are empty in v1 (D-46).
- The sixteen-fight loop against templated rivals *is* ranked's loop (D-49). Do not build campaign-only shortcuts into it.
