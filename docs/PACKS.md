# Source packs

Thirty-three GuttyKreum packs plus Steven Colling's Isle of Lore 2 UI Pack, unzipped to
`packs/<vendor>/<pack>/`. `packs/` is gitignored
(licensed, ~1 GB); this file and `docs/pack_index.csv` are the committed record, so a slot's
`candidateSource` can name a real file without the packs on disk.

```
python3 tools/dev/packs.py     # rewrite docs/pack_index.csv from packs/, print the roster
```

`pack_index.csv` is `pack,path,w,h` for all 21,648 PNGs — grep it:

```
grep ',32,32$' docs/pack_index.csv | grep japanese_office_interior/Tiles
```

## Layout inside a pack

Most packs share one shape:

| Folder | What |
| --- | --- |
| `Tiles/` | every cell pre-cut, one PNG each, 32×32, numbered not named (`GK_JO_A_001.png`) |
| `Tilemap/MainTileMap.png` | the same cells as one sheet — the contact sheet to read when picking |
| `RPGMakerVXAce/` | the same art in RM's sheet format; ignore |
| `Animated_Tiles/`, `AnimationedTiles/` | frame folders per animated tile |

Because `Tiles/` is already cut at 32×32, most slots are a copy to the manifest path, not a slice
(ART_PIPELINE.md §7.1). Numbered filenames mean picking still needs eyes on `MainTileMap.png`.

## What is where

| Slug | Use for | Cells |
| --- | --- | --- |
| `japanese_office_interior` | **the game's core**: floors, desks, cabinets, PCs, plants, whiteboards | 869 × 32×32 |
| `characterpack` | employees — 19 bodies, `Idle/` (four facings, **not** a loop) + `Walking{N,S,E,W}/` (8 frames) | 684 × 32×32 |
| `portraits` | founder and inspector portraits — `colored_bg`, `transparent_bg`, `illustration_bg`, `windowed` | 96×96, 96×112 |
| `horror_interiors` | B1, rituals, the extraplanar tint | 842 × 32×32 |
| `japanese_interior_essentials` | generic interior filler | 729 × 32×32 |
| `ui` | frames, 9-slice panels, buttons, cursors, a font | 2,938 × 16×16 |
| `the_japan_collection_icons` | 225 icons, each at 24×24 and 48×48 — department glyphs, budget, strikes | 24×24, 48×48 |
| `japan_collection_backgrounds` | 105 full-screen paintings (1280×720, 1920×1080 and other cuts) | — |
| `japanese_city_complete`, `osaka`, `dotonbori_city`, `kanagawa`, `dark_tokyo` | exteriors, the map screen, the tower | 32×32 |
| `japanese_bar`, `japanese_school`, `japanese_corner_store`, `bakery`, `arcade`, `batting_center`, `onsen`, `train_station`, `train_interiors`, `train_exterior`, `overgrown_backstreets`, `forest_graveyard`, `temples_shrines`, `templesand_shrines`, `festival`, `trees_vol1`, `urban_accessories`, `japan_collection_vol2`, `japan_collection_vol3` | rivals, campaign interludes, set dressing | 32×32 |
| `characters_vol1` | four large character illustrations (157×244 up to 471×732), not tiles | — |

`temples_shrines` and `templesand_shrines` are two different downloads of the same pack at the same
version; `templesand_shrines` is the larger and supersedes the other.

## `candidateSource` → pack

The manifest's `candidateSource` strings predate the packs being on disk. They resolve as:

| `candidateSource` says | Pack |
| --- | --- |
| JRPG Characters Vol. 1 | `characterpack` — superseded by D-70, which names a body per employee in the manifest |
| Office Interior, Office Interior floor tiles | `japanese_office_interior` |
| Horror Interiors | `horror_interiors` |
| Portraits pack | `portraits` — `transparent_bg`, 96×96 (D-71) |
| Japanese City / Osaka / Dotonbori | `japanese_city_complete`, `osaka`, `dotonbori_city` |
| Backgrounds pack | `japan_collection_backgrounds` |

Note `characters_vol1` is **not** "Characters Vol. 1" in the `candidateSource` sense — that pack is
large illustrations, and the walk cycles the worklist wants are in `characterpack`.

## Open against these packs

1. **No licence record for the GuttyKreum packs.** ART_PIPELINE.md §14 wants
   `packs/<vendor>/<pack>/LICENSE.md` with the licence text, the purchase record and a verdict on
   commercial use; the GuttyKreum packs' own `Readme.txt` files are patron shoutouts, nothing more.
   Q-RISK-1 gates release on this. Needs the itch.io receipts — not something to write from the files.
   Isle of Lore 2 ships its licence and is recorded (D-74); `python3 tools/dev/packs.py` prints
   `NO LICENCE` against every pack still missing one.
2. **Portraits are 96×96, not 64×64.** The nine portrait entries carry `verify: true` for exactly
   this (§15, Q-GBX-5). 96 is not an integer downscale of 64; either the entries and the screen
   layouts move to 96×96 (or the 96×112 `windowed` cut), or the portraits get redrawn. Open.
3. ~~Nothing in any pack is isometric.~~ Settled: the towers are front-on facade bands and the
   `iso` perspective is now `exterior` (D-69). The four `fx.tower.*` sprites are cut from
   `osaka/Tilemap/Tilemap.png` and sit at their manifest paths.

## Cutting

```
python3 tools/dev/cut_sprites.py     # every sprite whose candidateSource names a pack file (D-70, D-71)
python3 tools/dev/compose.py         # room plans, from the tools/dev/rooms/*.room recipes (D-73)
python3 tools/dev/ui.py              # UI chrome, from the tools/dev/ui/slots.txt table (D-74)
```

## Isle of Lore 2: UI Pack (Steven Colling)

`packs/stevencolling/isle_of_lore_2_ui/` — the game's UI chrome (D-74). Elements are in
`Sources/output/ui_pack_elements/<name>.standard/`; most are 9-sliceable and the pack's
`Documentation/Documentation.htm` gives each one's corner size. It also ships icon sets: symbol icons
in `ui_pack_controls/ui_pack_icons.{standard,outline}`, plus keyboard, mouse and gamepad prompts.

Everything is drawn at an 18 px 9-slice corner and blue-on-white for recolouring; `tools/dev/ui.py`
halves it to a 9 px corner and maps it onto a greybox tone. Licence recorded and clear —
`LICENSE.md` beside the pack. Same author as the two fonts in `game/fonts/` (D-67).
