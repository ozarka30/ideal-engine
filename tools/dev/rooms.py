#!/usr/bin/env python3
"""Room plans, authored as Godot scenes (D-73, D-78).

A room is `game/scenes/rooms/<room>.tscn`: one Sprite2D per 32x32 tile, each an
AtlasTexture region of a pack tilemap, laid out in the sprite's own coordinate space --
(0, 0) is the top-left of the plan, the top 32px band is the overhang above the
footprint, and the floor starts below it. Drag tiles in the 2D editor; `--bake`
composites the scene into the PNG at the entry's manifest path.

The tilemaps are mirrored into game/assets/packs/ so `res://` can reach them. They are
licensed pack art, so that folder is gitignored and `--scaffold` re-creates it.

    python3 tools/dev/rooms.py --scaffold   # mirror the sheets, write scenes from the recipes
    python3 tools/dev/rooms.py              # bake every scene to its PNG
"""
import json
import pathlib
import re
import shutil
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT / "packs" / "guttykreum"
MIRROR = ROOT / "game" / "assets" / "packs"
SCENES = ROOT / "game" / "scenes" / "rooms"
RECIPES = ROOT / "tools" / "dev" / "rooms"
CELL = 32
SHEETS = {
    "office": "japanese_office_interior/Tilemap/MainTileMap.png",
    "horror": "horror_interiors/Tilemap/MainTileMap.png",
    "essentials": "japanese_interior_essentials/Tilemap/MainTileMap.png",
}


def entries():
    data = json.loads((ROOT / "manifest" / "sprites.json").read_text(encoding="utf-8"))
    return {e["id"]: e for e in data["entries"]}


def mirror_sheets():
    MIRROR.mkdir(parents=True, exist_ok=True)
    for name, rel in SHEETS.items():
        src = PACKS / rel
        if not src.is_file():
            print(f"  no {name} sheet at {src.relative_to(ROOT)}; skipping")
            continue
        shutil.copyfile(src, MIRROR / f"{name}.png")
        print(f"  mirrored {name}")


def scaffold(by_id):
    """One-time: turn each .room recipe into a scene. Recipes are the old format (D-73)."""
    mirror_sheets()
    SCENES.mkdir(parents=True, exist_ok=True)
    for recipe in sorted(RECIPES.glob("*.room")):
        target, floor, puts = None, None, []
        for raw in recipe.read_text(encoding="utf-8").splitlines():
            line = raw.split("#")[0].strip()
            if not line:
                continue
            verb, *rest = line.split()
            if verb == "target":
                target = rest[0]
            elif verb == "floor":
                floor = (rest[0], *(int(v) for v in rest[1].split(",")))
            elif verb == "put":
                puts.append((rest[0], *(int(v) for v in rest[1].split(",")), *(int(v) for v in rest[3].split(","))))
        e = by_id[target]
        w, h = e["sprite"]["w"], e["sprite"]["h"]
        band = h - e["footprint"]["h"] * CELL          # the overhang strip above the footprint

        tiles = []
        if floor:
            sheet, col, row = floor
            for y in range(band, h, CELL):
                for x in range(0, w, CELL):
                    tiles.append((f"floor_{x // CELL}_{(y - band) // CELL}", sheet, col, row, x, y))
        for i, (sheet, col, row, px, py) in enumerate(puts):
            tiles.append((f"fitting_{i}", sheet, col, row, px, py))

        used = sorted({t[1] for t in tiles})
        ext = {s: str(i + 1) for i, s in enumerate(used)}
        subs, body = [], []
        for name, sheet, col, row, x, y in tiles:
            sid = f"{sheet}_{col}_{row}"
            if sid not in [s[0] for s in subs]:
                subs.append((sid, sheet, col, row))
            body.append(f'[node name="{name}" type="Sprite2D" parent="."]\n'
                        f'position = Vector2({x}, {y})\ncentered = false\n'
                        f'texture = SubResource("{sid}")\n')
        head = [f'[gd_scene load_steps={len(ext) + len(subs) + 1} format=3]', ""]
        head += [f'[ext_resource type="Texture2D" path="res://assets/packs/{s}.png" id="{ext[s]}"]' for s in used]
        head += [""]
        for sid, sheet, col, row in subs:
            head += [f'[sub_resource type="AtlasTexture" id="{sid}"]',
                     f'atlas = ExtResource("{ext[sheet]}")',
                     f'region = Rect2({col * CELL}, {row * CELL}, {CELL}, {CELL})', ""]
        head += [f'; {target} -- {w}x{h}, the top {band}px overhangs the footprint (D-73)',
                 '[node name="Room" type="Node2D"]', ""]
        (SCENES / f"{recipe.stem}.tscn").write_text("\n".join(head + body), encoding="utf-8", newline="\n")
        print(f"  {recipe.stem}.tscn <- {recipe.name} ({len(tiles)} tiles)")


NODE = re.compile(r'\[node name="[^"]+" type="Sprite2D" parent="\."\]\n'
                  r'position = Vector2\((-?\d+), (-?\d+)\)\n(?:centered = false\n)?'
                  r'texture = SubResource\("([^"]+)"\)')
REGION = re.compile(r'\[sub_resource type="AtlasTexture" id="([^"]+)"\]\n'
                    r'atlas = ExtResource\("([^"]+)"\)\nregion = Rect2\((\d+), (\d+), \d+, \d+\)')
EXT = re.compile(r'\[ext_resource type="Texture2D" path="res://assets/packs/(\w+)\.png" id="([^"]+)"\]')


def bake(by_id):
    sheets = {}
    for scene in sorted(SCENES.glob("*.tscn")):
        text = scene.read_text(encoding="utf-8")
        target = f"room.{scene.stem}.tile"
        if target not in by_id:
            sys.exit(f"{scene.name}: no manifest entry {target}")
        e = by_id[target]
        by_ref = {ref: name for name, ref in EXT.findall(text)}
        regions = {sid: (by_ref[ref], int(rx), int(ry)) for sid, ref, rx, ry in REGION.findall(text)}
        im = Image.new("RGBA", (e["sprite"]["w"], e["sprite"]["h"]), (0, 0, 0, 0))
        placed = 0
        for x, y, sid in NODE.findall(text):
            sheet, rx, ry = regions[sid]
            if sheet not in sheets:
                sheets[sheet] = Image.open(MIRROR / f"{sheet}.png").convert("RGBA")
            layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
            layer.paste(sheets[sheet].crop((rx, ry, rx + CELL, ry + CELL)), (int(x), int(y)))
            im.alpha_composite(layer)
            placed += 1
        dest = ROOT / e["sprite"]["asset"]
        dest.parent.mkdir(parents=True, exist_ok=True)
        im.save(dest)
        print(f"{target:<28} {placed:>2} tiles -> {e['sprite']['asset']}")


def main():
    by_id = entries()
    if "--scaffold" in sys.argv:
        scaffold(by_id)
    else:
        bake(by_id)


if __name__ == "__main__":
    main()
