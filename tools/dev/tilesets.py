#!/usr/bin/env python3
"""Mirror every pack tilemap under res:// and give each one a Godot TileSet (D-78).

Paint with them: add a TileMapLayer, set its tile_set to one of the generated .tres,
and the pack's whole sheet is a palette. Only cells with something in them become
tiles, so the palette has no holes to click through.

The sheets are licensed pack art, so game/assets/packs/ is gitignored and this
regenerates it. The individual cut tiles under each pack's Tiles/ folder are NOT
mirrored -- there are 21,648 of them and Godot would import every one; the sheet gives
access to the same art at a fraction of the cost.

    python3 tools/dev/tilesets.py            # what it would mirror
    python3 tools/dev/tilesets.py --write    # mirror the sheets and write the TileSets
"""
import pathlib
import shutil
import struct
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT / "packs"
MIRROR = ROOT / "game" / "assets" / "packs"
CELL = 32
MIN_CELLS = 64          # below this it is a cut tile, not a sheet


def sheets():
    """Every real tilemap sheet, as (vendor, pack, path, cols, rows)."""
    out = []
    for p in sorted(PACKS.rglob("*.png")):
        if "tilemap" not in str(p.parent).lower():
            continue
        w, h = struct.unpack(">II", p.open("rb").read(24)[16:24])
        cols, rows = w // CELL, h // CELL
        if cols * rows < MIN_CELLS:
            continue
        rel = p.relative_to(PACKS).parts
        out.append((rel[0], rel[1], p, cols, rows))
    return out


def name_for(vendor, pack, path):
    stem = path.stem.lower().replace(" ", "_")
    base = pack if stem in ("tilemap", "maintilemap", "main_tilemap", "fulltilemap") else f"{pack}_{stem}"
    return "".join(c for c in base if c.isalnum() or c == "_")


def occupied(path, cols, rows):
    """Cells with any opaque pixel. An empty cell in a palette is a hole to misclick into."""
    im = Image.open(path).convert("RGBA")
    alpha = im.getchannel("A")
    live = []
    for y in range(rows):
        for x in range(cols):
            if alpha.crop((x * CELL, y * CELL, (x + 1) * CELL, (y + 1) * CELL)).getbbox():
                live.append((x, y))
    return live


def main():
    write = "--write" in sys.argv
    found = sheets()
    print(f"{len(found)} sheets across {len({(v, p) for v, p, *_ in found})} packs")
    if not write:
        for v, p, path, c, r in found:
            print(f"  {name_for(v, p, path):<40} {c}x{r} cells")
        return

    MIRROR.mkdir(parents=True, exist_ok=True)
    total = 0
    for vendor, pack, path, cols, rows in found:
        name = name_for(vendor, pack, path)
        shutil.copyfile(path, MIRROR / f"{name}.png")
        live = occupied(path, cols, rows)
        total += len(live)
        tiles = "\n".join(f"{x}:{y}/0 = 0" for x, y in live)
        (MIRROR / f"{name}.tres").write_text(
            f'[gd_resource type="TileSet" load_steps=3 format=3]\n\n'
            f'[ext_resource type="Texture2D" path="res://assets/packs/{name}.png" id="1"]\n\n'
            f'[sub_resource type="TileSetAtlasSource" id="1"]\n'
            f'texture = ExtResource("1")\n'
            f'texture_region_size = Vector2i({CELL}, {CELL})\n'
            f'{tiles}\n\n'
            f'[resource]\n'
            f'tile_size = Vector2i({CELL}, {CELL})\n'
            f'sources/0 = SubResource("1")\n',
            encoding="utf-8", newline="\n")
        print(f"  {name:<40} {len(live):>5} tiles of {cols * rows}")
    print(f"{total} tiles in {len(found)} palettes -> {MIRROR.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
