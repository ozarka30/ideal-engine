#!/usr/bin/env python3
"""Room plans, authored as Godot scenes (D-73, D-78).

A room is `game/scenes/rooms/<room>.tscn`: one Sprite2D per 32x32 tile, each an
AtlasTexture region of a pack tilemap, laid out in the sprite's own coordinate space --
(0, 0) is the top-left of the plan, the top 32px band is the overhang above the
footprint, and the floor starts below it. Drag tiles in the 2D editor; `--bake`
composites the scene into the PNG at the entry's manifest path.

Sheets live under res:// because tools/dev/tilesets.py mirrors them there; run that
first on a fresh checkout. They are licensed pack art, so that folder is gitignored.

    python3 tools/dev/rooms.py              # bake every scene to its PNG
"""
import json
import pathlib
import re
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT / "packs" / "guttykreum"
MIRROR = ROOT / "game" / "assets" / "packs"
SCENES = ROOT / "game" / "scenes" / "rooms"
CELL = 32
# Sheets are mirrored under res:// by tools/dev/tilesets.py; a scene names one of those.


def entries():
    data = json.loads((ROOT / "manifest" / "sprites.json").read_text(encoding="utf-8"))
    return {e["id"]: e for e in data["entries"]}


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
    bake(by_id)


if __name__ == "__main__":
    main()
