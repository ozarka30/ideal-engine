#!/usr/bin/env python3
"""Bake a room plan from its Godot scene (D-73, D-78).

A room is `game/scenes/rooms/<room>.tscn`, laid out in the sprite's own coordinate
space: (0, 0) is the top-left of the plan, the top band is the overhang above the
footprint, and the floor starts below it. Two kinds of node are baked, in scene order,
so the PNG is what the 2D editor shows:

  Sprite2D        one 32x32 tile, an AtlasTexture region of a pack sheet
  TileMapLayer    a painted layer against one of the TileSets tools/dev/tilesets.py
                  writes -- paint with the whole pack rather than placing tiles by hand

A layer or sprite may carry a `scale`; a half-scaled layer bakes at 16px per tile,
which is how a small room fits more detail than its footprint has cells. Scaling is
nearest, matching the project's texture filter, so the bake is what the editor showed.

Sheets live under res:// because tools/dev/tilesets.py mirrors them there; run that
first on a fresh checkout. They are licensed pack art, so that folder is gitignored.

    python3 tools/dev/rooms.py                       # bake every scene to its PNG
    python3 tools/dev/rooms.py boardroom             # or just the ones named
    python3 tools/dev/rooms.py --new sales_floor     # start a blank room: floor laid, nothing else
"""
import base64
import json
import pathlib
import re
import struct
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
MIRROR = ROOT / "game" / "assets" / "packs"
SCENES = ROOT / "game" / "scenes" / "rooms"
CELL = 32

SECTION = re.compile(r"^\[(\w+)([^\]]*)\]$")
ATTR = re.compile(r'(\w+)="([^"]*)"')
VEC2 = re.compile(r"Vector2\(\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*\)")
RECT2 = re.compile(r"Rect2\(\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*\)")
REF = re.compile(r'(?:Ext|Sub)Resource\("([^"]+)"\)')


def sections(text):
    """The .tscn as (kind, attrs, properties), in file order. Tolerates any property order."""
    out, cur = [], None
    for raw in text.splitlines():
        line = raw.strip()
        if not line or line.startswith(";"):
            continue
        m = SECTION.match(line)
        if m:
            cur = (m.group(1), dict(ATTR.findall(m.group(2))), {})
            out.append(cur)
        elif cur is not None and "=" in line:
            k, _, v = line.partition("=")
            cur[2][k.strip()] = v.strip()
    return out


_sheets = {}


def sheet(res_path):
    """The mirrored pack sheet a res:// path names, loaded once."""
    name = res_path.rsplit("/", 1)[-1]
    if name.endswith(".tres"):                       # a TileSet: read the texture it wraps
        tres = (MIRROR / name).read_text(encoding="utf-8")
        m = re.search(r'path="res://assets/packs/([^"]+\.png)"', tres)
        if not m:
            sys.exit(f"{name}: cannot find the texture this TileSet wraps")
        name = m.group(1)
    if name not in _sheets:
        path = MIRROR / name
        if not path.is_file():
            sys.exit(f"missing {path.relative_to(ROOT)} -- run tools/dev/tilesets.py --write")
        _sheets[name] = Image.open(path).convert("RGBA")
    return _sheets[name]


def cells(data):
    """Godot's tile_map_data: a 2-byte header then 12 bytes per cell."""
    raw = base64.b64decode(data)
    for i in range(2, len(raw) - 11, 12):
        x, y, _source, ax, ay, _alt = struct.unpack("<hhhhhh", raw[i:i + 12])
        yield x, y, ax, ay


def stamp(canvas, tile, x, y, scale):
    if scale != 1:
        w, h = max(1, round(tile.width * scale)), max(1, round(tile.height * scale))
        # Nearest, not an average: project.godot sets default_texture_filter=0, so this is
        # what Godot shows. A box filter would soften every edge and stop it being pixel art.
        tile = tile.resize((w, h), Image.NEAREST)
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    layer.paste(tile, (round(x), round(y)))
    canvas.alpha_composite(layer)


def bake(scene, entry):
    text = scene.read_text(encoding="utf-8")
    parsed = sections(text)
    ext = {a["id"]: a["path"] for k, a, _ in parsed if k == "ext_resource" and "path" in a}
    atlas = {}
    for kind, attrs, props in parsed:
        if kind == "sub_resource" and attrs.get("type") == "AtlasTexture":
            r = RECT2.search(props.get("region", ""))
            a = REF.search(props.get("atlas", ""))
            if r and a:
                atlas[attrs["id"]] = (ext[a.group(1)], int(float(r.group(1))), int(float(r.group(2))))

    im = Image.new("RGBA", (entry["sprite"]["w"], entry["sprite"]["h"]), (0, 0, 0, 0))
    tiles = 0
    for kind, attrs, props in parsed:
        if kind != "node":
            continue
        pos = VEC2.search(props.get("position", ""))
        px, py = (float(pos.group(1)), float(pos.group(2))) if pos else (0.0, 0.0)
        sc = VEC2.search(props.get("scale", ""))
        scale = float(sc.group(1)) if sc else 1.0

        if attrs.get("type") == "Sprite2D":
            ref = REF.search(props.get("texture", ""))
            if not ref or ref.group(1) not in atlas:
                continue
            res, ax, ay = atlas[ref.group(1)]
            src = sheet(res)
            stamp(im, src.crop((ax, ay, ax + CELL, ay + CELL)), px, py, scale)
            tiles += 1
        elif attrs.get("type") == "TileMapLayer":
            ref = REF.search(props.get("tile_set", ""))
            data = props.get("tile_map_data", "")
            m = re.search(r'PackedByteArray\("([^"]*)"\)', data)
            if not ref or not m:
                continue
            src = sheet(ext[ref.group(1)])
            step = CELL * scale
            for cx, cy, ax, ay in cells(m.group(1)):
                tile = src.crop((ax * CELL, ay * CELL, ax * CELL + CELL, ay * CELL + CELL))
                stamp(im, tile, px + cx * step, py + cy * step, scale)
                tiles += 1
    return im, tiles


FLOOR_CELL = (27, 23)      # japanese_office_interior v3: the blue tile autotile's centre fill


def scaffold(name, entry):
    """A blank room to paint into: the floor laid across the footprint, nothing else."""
    dest = SCENES / f"{name}.tscn"
    if dest.exists():
        print(f"  {name}: already exists, leaving it alone")
        return
    w, h = entry["sprite"]["w"], entry["sprite"]["h"]
    band = h - entry["footprint"]["h"] * CELL          # overhang above the footprint
    step = CELL // 2                                    # tiles are painted at half scale
    ax, ay = FLOOR_CELL
    cells_out = []
    for cy in range(band // step, h // step):
        for cx in range(w // step):
            cells_out.append(struct.pack("<hhhhhh", cx, cy, 0, ax, ay, 0))
    data = base64.b64encode(struct.pack("<H", 0) + b"".join(cells_out)).decode()
    lines = [
        "[gd_scene load_steps=2 format=3]",
        "",
        '[ext_resource type="TileSet" path="res://assets/packs/japanese_office_interior.tres" id="1"]',
        "",
        f"; {name}: the plan is {w}x{h}; the top {band}px overhangs the footprint and is where",
        "; back-row fittings go. Tiles are painted at half scale, so a layer cell is 16px.",
        '[node name="Room" type="Node2D"]',
        "",
        '[node name="Floor" type="TileMapLayer" parent="."]',
        "scale = Vector2(0.5, 0.5)",
        f'tile_map_data = PackedByteArray("{data}")',
        'tile_set = ExtResource("1")',
        "",
    ]
    dest.write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(f"  {name}: {w}x{h}, floor laid, ready to paint")


def main():
    wanted = [a for a in sys.argv[1:] if not a.startswith("-")]
    entries = {e["id"]: e for e in json.loads((ROOT / "manifest" / "sprites.json").read_text(encoding="utf-8"))["entries"]}
    if "--new" in sys.argv:
        for name in wanted:
            target = f"room.{name}.tile"
            if target not in entries:
                sys.exit(f"no manifest entry {target}")
            scaffold(name, entries[target])
        return
    for scene in sorted(SCENES.glob("*.tscn")):
        if wanted and scene.stem not in wanted:
            continue
        target = f"room.{scene.stem}.tile"
        if target not in entries:
            sys.exit(f"{scene.name}: no manifest entry {target}")
        e = entries[target]
        im, tiles = bake(scene, e)
        dest = ROOT / e["sprite"]["asset"]
        dest.parent.mkdir(parents=True, exist_ok=True)
        im.save(dest)
        print(f"{target:<28} {tiles:>3} tiles -> {e['sprite']['asset']}")


if __name__ == "__main__":
    main()
