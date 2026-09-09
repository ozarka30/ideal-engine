# Company Wars — Art Pipeline

Status: **Phase 4 draft.** How pack tiles become in-game content, and how the
greybox-first workflow is enforced rather than hoped for. Builds on the locked art
direction in `DESIGN_BRIEF.md` §4 and §7, the screen and entity specifications in
`GAME_DESIGN.md` §19, and decisions D-14 to D-16 and D-26.

The pipeline has one rule, from which everything else follows: **the sprite manifest
is the only place a pixel dimension may be written down.** Layout code reads the
manifest. The greybox renderer reads the manifest. The validator reads the manifest.
The worklist is generated from the manifest. Real art is a file that appears at the
path the manifest already declared.

The manifest exists: `manifest/sprites.json`, 182 entries as of this document, every
one a greybox from day one. Excerpts below are from that file.

---

## Contents

1. [The manifest](#1-the-manifest)
2. [Footprint versus visual bounds](#2-footprint-versus-visual-bounds)
3. [Draw order](#3-draw-order)
4. [The greybox renderer](#4-the-greybox-renderer)
5. [The validation check](#5-the-validation-check)
6. [Mixed-state coherence](#6-mixed-state-coherence)
7. [Tooling](#7-tooling)
8. [Art complete](#8-art-complete)
9. [Pixel discipline](#9-pixel-discipline)
10. [Atlas generation](#10-atlas-generation)
11. [Naming and paths](#11-naming-and-paths)
12. [The perspective rule](#12-the-perspective-rule)
13. [UI style guide](#13-ui-style-guide)
14. [Absorbing a third-party pack](#14-absorbing-a-third-party-pack)
15. [Pack-fit verification](#15-pack-fit-verification)
16. [Coverage as of this document](#16-coverage-as-of-this-document)

---

## 1. The manifest

`manifest/sprites.json` validates against `schema/manifest.schema.json`. The file
header pins the tile size, the logical canvas, the permitted scales, and the content
version it was generated against; the body is one entry per visual slot.

### 1.1 An entry

```json
{
  "id": "furn.whiteboard",
  "kind": "furniture",
  "category": "support",
  "label": "Whiteboard",
  "footprint": {
    "w": 1,
    "h": 1
  },
  "sprite": {
    "w": 32,
    "h": 40,
    "anchor": {
      "x": 0.5,
      "y": 1.0
    },
    "asset": "assets/topdown/furn/whiteboard.png",
    "sourceRect": null
  },
  "sortBias": -5,
  "perspective": "topdown",
  "screens": [
    "build",
    "shop",
    "battle.inset"
  ],
  "visibility": 1,
  "reads": "The object itself, readable at 32px; wall-mounted pieces overhang 8px above their tile",
  "candidateSource": "Office Interior",
  "verify": false,
  "releaseGate": true,
  "note": "wall-mounted: 32x40 sprite over a 1x1 footprint; the top 8px is overhang",
  "overhang": {
    "left": 0,
    "top": 8,
    "right": 0,
    "bottom": 0
  }
}
```

### 1.2 Fields

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | yes | Dot-namespaced, two or more segments. Content files reference these (`sprite`, `tile`, `icon`) |
| `kind` | yes | `employee`, `furniture`, `tile`, `ui`, `icon`, `fx`, `background`, `font` |
| `category` | yes | Greybox tone: `structure`, `operations`, `people`, `support`, `interface`, `anomalous`, `invalid` |
| `label` | yes | Drawn on the placeholder |
| `footprint` | yes | `{ w, h }` in tiles for grid kinds; `null` for everything else. The schema requires an object for `employee`, `furniture`, `tile` |
| `sprite.w`, `sprite.h` | yes | Pixels at 1×. A `font` entry has `w: 0` and `h` = line height |
| `sprite.anchor` | yes | Explicit, 0–1 in each axis. Never assumed |
| `sprite.asset` | yes | The path the file will live at. Declared before the file exists |
| `sprite.sourceRect` | yes | `null` in the source manifest; filled by the atlas step in the derived manifest (§10) |
| `sprite.frames` | no | Named frame ranges into a strip: `[col, row]` or `[col, row, count]` |
| `sortBias` | yes | −10..10. Default 0 |
| `perspective` | yes | `topdown`, `iso`, `ui`. The schema forbids `topdown` entries on isometric screens and vice versa |
| `screens` | yes | Where it appears; feeds the worklist and the perspective check |
| `visibility` | yes | 1–4, feeds the worklist ranking (§7.3) |
| `reads` | yes | What the asset must read as at a glance. This is the brief to whoever makes it |
| `candidateSource` | yes | Which pack to slice from, where known; `null` otherwise |
| `verify` | yes | `true` on the 13 entries whose dimensions were decided rather than derived (§15) |
| `releaseGate` | yes | `false` only on debug overlays and the invalid flash, which never get art |
| `layout` | no | `{ x, y }` on the logical canvas, for fixed-position UI regions |
| `overhang` | yes | **Derived** (§2). The generator writes it; the loader recomputes it and fails on disagreement |

### 1.3 What the manifest is not

It is not the content database: an employee's cooldown lives in `content/`, and its
sprite lives here, joined by the `sprite` id. It is not an atlas: atlases are built
from it (§10). It is not edited when art arrives: art arrives at the path that was
already there.

---

## 2. Footprint versus visual bounds

A 2 × 2 room's floor is 64 × 64 pixels. A wall-mounted whiteboard occupies one tile and
draws 32 × 40, rising eight pixels over the tile behind it. The tile a thing *occupies*
and the rectangle it *draws into* differ constantly in top-down 32 × 32 art, and
conflating them is exactly how art layers wrongly the day it lands.

### 2.1 Derivation

The manifest carries both, and only one is authored. Given the footprint `(fw, fh)` in
tiles, the sprite `(sw, sh)` in pixels, and the anchor `(ax, ay)`:

```
tw = fw * 32 ; th = fh * 32                       // footprint rect in pixels
sx = ax * tw - ax * sw                             // sprite top-left relative to footprint top-left
sy = ay * th - ay * sh
overhang.left   = max(0, -sx)
overhang.top    = max(0, -sy)
overhang.right  = max(0, sx + sw - tw)
overhang.bottom = max(0, sy + sh - th)
```

For the whiteboard: `tw = th = 32`, `sw = 32`, `sh = 40`, anchor `(0.5, 1.0)` →
`sy = 32 − 40 = −8` → `overhang.top = 8`. The anchor being explicit is what makes this
computable; an assumed anchor would make every overhang a guess.

Overhang is never authored because a manifest that can contradict itself is worse than
no manifest. The generator writes it for convenience; the loader recomputes it and
treats a mismatch as a corrupted file.

### 2.2 What the greybox draws

- **Footprint:** a solid fill in the category tone at 85% alpha, with a 1-pixel darker
  border so adjacent tiles read as separate.
- **Overhang:** the part of the sprite rect outside the footprint, hatched at 45% alpha
  in the category's hatch tone, 4-pixel pitch.
- **Anchor:** a 3 × 3 cross in the text tone at the anchor point.
- **Label:** see §4.

A greybox screenshot therefore shows exactly which pixels the real art may touch, which
tile it stands on, and where its anchor is — which is the whole specification an artist
or a slicer needs.

---

## 3. Draw order

The rule from D-15, restated because it must be settled in greybox or every art drop
will re-litigate it:

```
sort key = (floorIndex ASC, anchorTileRow ASC, sortBias ASC, tileCol ASC, id ASC)
```

- `anchorTileRow` is the tile row containing the sprite's anchor point — for
  bottom-centre-anchored sprites, the bottom tile of the footprint.
- `sortBias` conventions: room floor tiles −10, room signs −9, wall-mounted furniture
  −5, floor-standing furniture and employees 0, link lines +7, aura badges +8, glyphs
  +9, debug overlays +10.
- The trailing `id` makes the order **total**: two implementations, or the same one
  twice, draw the same screen identically, which is what makes greybox screenshots
  usable as regression tests (§7.5).

The greybox debug overlay (`ui.debug.sortkey`) can print the key on every placeholder,
because a wrong `sortBias` is invisible until art lands and this is the only way to see
it before then.

---

## 4. The greybox renderer

Every manifest entry renders as a placeholder when no file exists at `sprite.asset`.
Same entry, same code path, different texture: the renderer asks the asset loader for
the path, and the loader returns either the decoded image or a generated placeholder
texture with the same dimensions.

### 4.1 The placeholder

At the entry's exact `sprite.w × sprite.h`, in the category tone from
`manifest/greybox_palette.json`:

| Element | Rule |
| --- | --- |
| Fill | Footprint solid at 85%; overhang hatched at 45%; non-grid kinds solid over the whole rect |
| Border | 1 px, the tone's `border` colour |
| Label | `font.ui.8` in the tone's `text` colour: line 1 the `id`, line 2 `footprint` as `2x2` and dimensions as `64x88`, line 3 the anchor as `bc` / `tl` / `c` |
| Fallback | If the rect cannot hold three lines: `id` only. If it cannot hold that: a 4 × 4 dot in the tone. Text is never scaled |
| Anchor | A 3 × 3 cross |

A `font` entry's placeholder is a fallback pixel font shipped with the build, so text
renders before the real face arrives. An `fx` entry with `frames` renders its
placeholder as a single frame with the frame count in the label.

### 4.2 The palette

`manifest/greybox_palette.json` is the one file that defines the tones. Six are meant
to be sampled from the Office Interior pack at roughly 35% saturation and 55% value
(Q-GBX-3, awaiting sign-off; the file currently holds placeholder values at those
targets, to be replaced by sampled ones in the pack-fit pass, §15). The seventh —
`invalid` — is deliberately outside the palette:

```json
"invalid": {
  "fill": "#e0202a", "border": "#8a0e15", "hatch": "#ff5c64", "text": "#ffffff",
  "source": "none — deliberately outside the palette"
}
```

A broken thing must look broken. A merely unfinished thing must not.

### 4.3 Where the tones go

| Category | Used for |
| --- | --- |
| `structure` | Room tiles for Reception, floor frames, the shaft, tower segments, backdrops |
| `operations` | Working-room tiles and room cards |
| `people` | Employees, applicant cards, portraits, the map marker |
| `support` | Furniture and furniture cards |
| `interface` | Every UI panel, bar, chip, icon, font |
| `anomalous` | Everything extraplanar: B1 tiles, Otherworld cards, spectral staff, Burnout's icon |
| `invalid` | Validation failures, the placement flash, debug overlays |

---

## 5. The validation check

`tools/validate-manifest` runs in CI on every commit and in development on every
start-up. It fails the build on any of:

1. **Schema.** The manifest does not validate against `schema/manifest.schema.json`.
2. **Derived fields.** A recomputed `overhang` differs from the stored one.
3. **References.** A `sprite`, `tile` or `icon` id in `content/` has no manifest entry.
4. **Uniqueness.** Two entries share an `id` or an `asset` path.
5. **Perspective.** A `topdown` entry lists an isometric screen or vice versa (§12).
6. **Dimensions.** For every entry whose `sprite.asset` file **is present**: the
   image's pixel size must equal `sprite.w × sprite.h`, or, when `sourceRect` is set,
   the rect must lie within the image and match `w × h`. For entries with `frames`,
   the image must be at least `(maxCol + count) × w` wide and `(maxRow + 1) × h` tall.

**Absence is valid.** An entry whose file does not exist is simply still greybox. The
check never fails on a missing file, never warns about one, and never needs the
manifest edited when one appears. That is the entire reason adding art is a file copy.

The check emits `art_coverage.json` (§7.2) as a side effect, so coverage is a
by-product of validation rather than a separate step someone might skip.

---

## 6. Mixed-state coherence

For months, every screen will be part real pixel art and part placeholder. The design
treats that as the ordinary state, and three rules keep it from reading as broken:

1. **Tones come from the pack's own palette**, desaturated. A placeholder is a muted
   version of the colour world the real art lives in, so a half-arted build screen
   reads as *stylised*, not *unfinished*.
2. **Category coding is consistent.** Rooms are always the same family of tone,
   people another, furniture another. The eye learns the code in one session and stops
   noticing it.
3. **Nothing is ever approximately placed.** Because every placeholder is at the exact
   size and anchor, art dropping in changes texture and nothing else. There is no
   moment where the layout shifts — which is the moment a mixed screen would otherwise
   look wrong.

The coverage overlay (§7.2) exists so that the question "what is still greybox" has an
answer that is not "look at the screen and guess". The eye should not have to
distinguish placeholder from art; the tool does.

---

## 7. Tooling

Four tools, all reading the manifest. They are build-side code and live in
`packages/tools` (`ARCHITECTURE.md` §2).

### 7.1 The slicer

`tools/slice <sheet.png> --grid 32 [--kind furniture] [--prefix furn.]`

Reads a GuttyKreum sheet, finds non-empty 32 × 32 cells (and, with `--merge`, runs of
adjacent non-empty cells), and emits **manifest stubs**: one candidate entry per cell
group with `sprite.w/h` from the group, anchor defaulted by kind (bottom-centre for
grid kinds, top-left otherwise), `footprint` guessed from the group size,
`sourceRect` set, `id` and `label` left as `TODO_<sheet>_<col>_<row>`, and `reads`
empty. Stubs go to `manifest/stubs/<sheet>.json`, never into `sprites.json` directly —
a human or an agent names them, fixes footprints, and merges.

The slicer's job is to make real assets flow *into* the manifest rather than being
hand-wired, and to make the pack inventory searchable: `tools/slice --index` writes a
contact sheet per pack with every cell's coordinates, so `candidateSource` can name a
specific cell.

### 7.2 The coverage overlay and `art_coverage.json`

The validator writes:

```json
{
  "contentVersion": "0.1.0",
  "manifestVersion": 1,
  "total": 182,
  "gated": 179,
  "withArt": 0,
  "invalid": 0,
  "byTier": { "1": { "gated": 67, "withArt": 0 }, "2": { "gated": 69, "withArt": 0 },
              "3": { "gated": 34, "withArt": 0 }, "4": { "gated": 9, "withArt": 0 } },
  "byScreen": { "build": { "gated": 118, "withArt": 0 }, "battle": { "gated": 26, "withArt": 0 } },
  "byKind": { "employee": { "gated": 40, "withArt": 0 } },
  "verifyPending": 13
}
```

The in-game overlay (`ui.debug.coverage`, toggled with a debug key) draws the same
numbers in the corner of every screen, plus — on the current screen — a count of
visible placeholders. The README badge reads `withArt / gated`.

### 7.3 The ranked art worklist

`tools/worklist [--tier N] [--screen build] [--out worklist/]`

The most important tool in this workflow, because replacement is developer-paced and
unordered, and a coverage number is not a plan. For every gated entry without art, it
emits a **spec sheet**, and it ranks them.

**Ranking.** Score = `visibility tier` (1 first), then within a tier by the number of
screens the entry appears on (more first), then by kind (`employee`, `tile`,
`furniture`, `ui`, `icon`, `fx`, `background`, `font`), then by `id`. A shop card seen
every round outranks a basement room seen twice a run, mechanically.

**A spec sheet**, as `worklist/0001_ui.card.applicant.md`:

```
# 0001 · ui.card.applicant — Applicant card
Tier 1 · screens: build, shop, reward · kind: ui · tone: people

Dimensions   52 x 80 px at 1x        Anchor  top-left (0.0, 0.0)
Footprint    —                        Sort    0
Target file  assets/ui/ui/card/applicant.png
Source rect  null (single image)

Must read as
  Portrait slot (10,4,32,32); name (2,40,48,8); dept icon 8x8 at (2,50) + tier pips;
  ability two lines (2,60,48,16); cost tag (2,72,20,8); rider strip replaces
  ability line 2 for extraplanar

Candidate source
  none recorded — UI is drawn, not sliced

Template   worklist/0001_ui.card.applicant.template.png (52x80, footprint and anchor marked)
```

**The template PNG** is generated at exact dimensions: footprint solid, overhang
hatched, anchor cross, 1 px border, and the id in the corner — the greybox itself,
exported as a file to paint over. Whoever makes the asset paints on top and saves to
the target path. The dimensions cannot be wrong because the canvas was the spec.

### 7.4 The pack index

`tools/packs --index` lists every pack under `packs/`, its sheets, and whether its
licence record (§14, Q-RISK-1) is present. Used by the slicer and by the worklist's
`candidateSource` lookups.

### 7.5 Screenshot fixtures

Because draw order is total (§3) and placeholders are deterministic, a greybox screen
renders identically on every run. `tools/screenshot <screen> <fixture>` renders a named
screen from a fixture state (a save file or a snapshot) to a PNG; CI compares against
the committed one and fails on a pixel diff. When art lands, the fixture is regenerated
in the same commit — the diff *is* the review.

---

## 8. Art complete

D-16, as the release gate:

1. Every entry with `releaseGate: true` has a present file at `sprite.asset`.
2. The validator passes on every present file.
3. A full campaign playthrough capture renders zero entries in the `invalid` tone.
4. The perspective rule holds on every screen (§12).
5. Every `verify: true` entry has been verified (§15) — its `verify` flag cleared by
   a commit that either confirms the dimensions or changes them.

A second, earlier gate — **store-ready** — requires tiers 1 and 2 only, because Steam's
store page needs real screenshots of the build and battle screens months before
release (D-56; `ROADMAP.md` §8). It is the same coverage report read at a different
threshold.

Tiers **order** the worklist. They never exempt. The only exempt entries are the three
with `releaseGate: false`: the invalid flash and two debug overlays, which never get
art because the tone is the art.

Tracked from the first commit: `art_coverage.json` is written on every build, the
README badge reads it, and `ROADMAP.md` carries the gate on its own line, never as a
phase dependency. The gate is far away and strict; the worklist ranking is what makes
the *felt* completeness run ahead of the measured one.

---

## 9. Pixel discipline

Fixed globally, in the manifest header, and asserted by the renderer at start-up:

| Rule | Value |
| --- | --- |
| Base tile | 32 × 32 |
| Logical canvas | 640 × 360 (D-26) |
| Scale factors | integers only: 2, 3, 4, 6. Never fractional. Letterbox otherwise |
| Filtering | nearest-neighbour, everywhere, including UI and fonts |
| Positions | integer logical pixels. The renderer rounds any computed position before draw and asserts in debug builds that nothing fractional reached it |
| Fonts | `font.ui.8` at 1× is **Honey Pigeon** baked to an 8 px line; `font.ui.16` is **Honeyblot Caps** baked to a 16 px line for headers, banners, tall buttons and the Goodwill numbers (D-66); no other sizes. Both by Steven Colling under his Font License 1.0: the bitmap export ships, the font files do not enter the repository. Digits are proportional; columns that need alignment right-align on the number, not the glyph |
| Camera | integer logical offsets; the build viewport scrolls by whole floors |

A greybox authored at a fractional scale would not match the art that replaces it;
the renderer refusing fractional scale is what makes that impossible rather than
merely discouraged.

---

## 10. Atlas generation

`tools/atlas` runs at build time, never at author time:

1. Reads the source manifest and every present asset.
2. Packs present assets into per-screen-group atlases: `topdown` (build, inset,
   shop, codex), `iso` (battle, map, menu), `ui`. A maximum of 2048 × 2048 per atlas,
   1 px padding, no rotation.
3. Emits `dist/atlas/<group>.png` and a **derived manifest** `dist/manifest.json` in
   which `sprite.asset` points at the atlas and `sourceRect` is filled.
4. Entries without art keep `sourceRect: null` in the derived manifest and render as
   placeholders at runtime exactly as in development.

The source manifest is never modified by the atlas step. Development runs against the
source manifest and loose files; release runs against the derived one. Both paths go
through the same loader, and the validator runs on both.

---

## 11. Naming and paths

| Thing | Convention | Example |
| --- | --- | --- |
| Manifest id | `<kind-ish>.<name>[.<part>]`, lowercase, underscores | `room.server_room.tile`, `ui.map.node.audit` |
| Asset path | `assets/<perspective>/<id with dots as slashes>.png` | `assets/topdown/room/server_room/tile.png` |
| Stub | `manifest/stubs/<sheet>.json` | `manifest/stubs/office_interior_01.json` |
| Atlas | `dist/atlas/<group>.png` | `dist/atlas/topdown.png` |
| Fixture screenshot | `fixtures/screens/<screen>_<fixture>.png` | `fixtures/screens/build_round8_turtle.png` |
| Worklist sheet | `worklist/<rank>_<id>.md` | `worklist/0001_ui.card.applicant.md` |
| Pack | `packs/<vendor>/<pack>/` with `LICENSE.md` beside the sheets | `packs/guttykreum/office_interior/` |

Ids are permanent. Renaming one is a manifest version bump and a rename of the asset
path in the same commit, with the validator asserting no orphaned file remains.

---

## 12. The perspective rule

Top-down interiors and characters own the build view and the battle inset. The
isometric city packs own the exterior battle view, the map, and the menu. They never
share a pixel.

Enforced three ways:

1. **Schema.** A `topdown` entry may only list `build`, `shop`, `battle.inset`,
   `autopsy`, `reward`, `codex`; an `iso` entry only `battle`, `map`, `menu`. An entry
   that lists both is a schema error.
2. **Validator.** After loading, the set of perspectives per screen is computed; any
   screen holding both `topdown` and `iso` fails. `battle.inset` is a distinct screen
   id precisely so that the inset (top-down) and the battle (iso) are checked apart.
3. **Renderer.** Each screen declares its perspective, and the asset loader refuses
   to hand a screen an entry of the wrong one, in debug builds with an assertion.

The inset is the one place the two views touch: a top-down floorplan drawn inside a
frame over the isometric street. The frame is a `ui` entry; the floorplan inside it is
`topdown`; the street behind is `iso`. Three entries, three perspectives, one
composed screen — and the rule holds because the inset has its own screen id.

---

## 13. UI style guide

Enough for the greybox to be built consistently; the finished look is a Phase 5+
art concern, developer-paced like everything else.

| Element | Rule |
| --- | --- |
| Panels | 1 px border in the `interface` border tone, fill in the `interface` fill tone, 4 px inner padding |
| Text | `font.ui.8`, left-aligned, 8 px line pitch; numbers tabular, right-aligned in columns |
| Emphasis | The `interface` text tone for normal, the category tone for the thing named (a Push amount in `operations`, a Morale amount in `anomalous`) |
| Buttons | 16 px tall, label centred, 1 px border; hover inverts fill and text; pressed offsets label 1 px down |
| Chips | 8 px tall, 2 px padding, selected state inverts |
| Bars | 1 px frame, fill inset by 1 px; the Goodwill bar's frame end moves with the cap (D-35) |
| Cards | 52 × 80, category tone, 2 px padding, portrait slot top-centre |
| Icons | 8 × 8 for departments, statuses, strikes, budget; 24 × 24 for map nodes; drawn, not text |
| Motion | Ledger scroll 1 line per 4 ticks; banners slide 12 px over 10 ticks; floating numbers rise 16 px over 20 ticks; nothing eases — linear, integer steps |
| Tone of voice | Corporate-flat. Labels are nouns. The satire is in the mechanics, not the copy |

---

## 14. Absorbing a third-party pack

The Otherworld Temp Agency exists so that art from outside the GuttyKreum collection
arrives as an in-fiction foreign body rather than an inconsistency. The process:

1. **Licence first.** Add the pack under `packs/<vendor>/<pack>/` with `LICENSE.md`
   beside the sheets recording the licence text, the purchase record, and a one-line
   verdict on commercial use and in-game redistribution. `tools/packs --index` lists
   packs missing this file; the release gate (Q-RISK-1) requires none be missing.
2. **Slice into stubs.** `tools/slice` as §7.1.
3. **Categorise as `anomalous`.** Every entry from a non-GuttyKreum pack takes the
   `anomalous` category and, if it is an employee, the `extraplanar` department. Its
   placeholder tone and its fiction agree: this is from somewhere else.
4. **Respect the perspective rule.** A foreign pack in a third perspective (say,
   side-on) cannot be used in the interior or exterior views at all. It may only
   appear on its own screen — a portal event, a codex page — with its own screen id.
   The schema enum is extended in the same commit, which makes the addition
   reviewable.
5. **Pixel discipline still applies.** 32 × 32 base, nearest-neighbour, integer scale.
   A pack at another tile size is resampled by the slicer at author time (integer
   factors only), never at runtime.

The result is that a Kappa Intern from a different artist looks like a Kappa Intern
from a different plane, which is what the design already says it is.

---

## 15. Pack-fit verification

Q-GBX-5. Thirteen entries carry `verify: true` because their dimensions were decided
to fit the canvas rather than derived from the packs — the four tower pieces, the
inspector portrait, and the eight founder portraits, which share the inspector
portrait's size and therefore its verification:

| Entry | Decided | Pack to check |
| --- | --- | --- |
| `fx.tower.floor_segment` | 96 × 32, bottom-centre | Japanese City / Osaka / Dotonbori |
| `fx.tower.floor_segment_empty` | 96 × 32 | same |
| `fx.tower.roof` | 96 × 16 | same |
| `fx.tower.basement` | 96 × 24, top-centre | same, with a Horror Interiors tint |
| `ui.portrait` | 64 × 64 | Portraits |
| `founder.*.portrait` (8) | 64 × 64 | Portraits — one verification covers all nine portrait entries |

**Procedure**, first task of implementation, before any of these is greyboxed (the
eight founder portraits and the inspector portrait are one check, not nine):

1. Open each pack. Find the isometric building tiles and the portrait sheet.
2. For each entry, slice a candidate at the declared size and place it in a 640 × 360
   mock of the battle screen (or the inspector) at the declared position.
3. If it reads: clear `verify`, record the `candidateSource` cell, commit.
4. If it does not: change the manifest entry's dimensions, change the screen layout
   in `GAME_DESIGN.md` §19 that depends on it, clear `verify`, commit — *then* greybox.

The rule is that a greybox is never built against a dimension known to be wrong. This
is an afternoon with the packs open, and it needs the packs, which the planning
environment does not have.

The same pass samples the six palette hues from Office Interior and replaces the
placeholder hex values in `manifest/greybox_palette.json` (Q-GBX-3).

---

## 16. Coverage as of this document

| | Entries |
| --- | --- |
| Total | 182 |
| Release-gated | 179 |
| With art | 0 |
| Tier 1 — build screen every round | 68 |
| Tier 2 — battle and autopsy every fight | 69 |
| Tier 3 — map, codex, rarer content | 34 |
| Tier 4 — B1, rituals, bosses, debug | 11 |
| Awaiting pack-fit verification | 13 |
| Exempt from the gate | 3 |

Every one of the 179 is a labelled placeholder at exact dimensions from the first
commit. The campaign ships when the number in the third row equals the number in the
second, and not before, and nothing else waits for it.
