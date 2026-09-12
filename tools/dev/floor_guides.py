#!/usr/bin/env python3
"""Write the painting guides into every floor scene (D-84).

A floor scene is painted by hand; its `Guides` node is not. This reads what each floor is -- its
grid, the rooms fixed to it, the room kinds it takes -- from content/, and the tile and slot sizes
from the manifest, and rewrites the `Guides` subtree of every game/scenes/floors/<business>/<floor>.tscn:
a line where tiles meet, a red wash where nothing can be placed, an outline and a name for each fixed
room, and a label naming the floor and what it takes. The build screen hides `Guides` when it renders a
floor, so they are seen only in the editor. Everything else in the scene is left alone.

    python3 tools/dev/floor_guides.py [--check]

Rerun after a content change or when a business folder is added; --check lists stale scenes and exits 1.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
FLOORS = ROOT / "game" / "scenes" / "floors"
LINE = "1, 1, 1, 0.45"
VOID = "0.9, 0.15, 0.15, 0.35"
ROOM = "0.3, 0.6, 1, 0.25"


def load(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def rect(name, x, y, w, h, color):
    return (f'[node name="{name}" type="ColorRect" parent="Guides"]\n'
            f"offset_left = {x}.0\noffset_top = {y}.0\noffset_right = {x + w}.0\noffset_bottom = {y + h}.0\n"
            f"mouse_filter = 2\ncolor = Color({color})\n")


def label(name, x, y, w, h, text, halign=0, valign=0):
    """Wrapped to its box, so a long name takes two lines instead of running off the floor. Alignment: 0 start, 1 centre, 2 end."""
    return (f'[node name="{name}" type="Label" parent="Guides"]\n'
            f"offset_left = {x}.0\noffset_top = {y}.0\noffset_right = {x + w}.0\noffset_bottom = {y + h}.0\n"
            "theme_override_colors/font_color = Color(1, 1, 1, 1)\n"
            "theme_override_colors/font_outline_color = Color(0, 0, 0, 1)\n"
            "theme_override_constants/outline_size = 2\n"
            "theme_override_font_sizes/font_size = 8\n"
            f'text = "{text}"\n'
            "autowrap_mode = 3\n"
            + (f"horizontal_alignment = {halign}\n" if halign else "")
            + (f"vertical_alignment = {valign}\n" if valign else ""))


def guides(floor, rooms, tile, slot_w, slot_h):
    gw, gh = floor["grid"]["w"] * tile, floor["grid"]["h"] * tile
    out = ['[node name="Guides" type="Node2D" parent="."]\n'
           "; written by tools/dev/floor_guides.py from content -- rerun it instead of editing; hidden in game\n"
           "z_index = 1\n"
           "metadata/_edit_lock_ = true\nmetadata/_edit_group_ = true\n"]
    for name, x, y, w, h in (("not_floor_right", gw, 0, slot_w - gw, slot_h), ("not_floor_below", 0, gh, gw, slot_h - gh)):
        if w > 0 and h > 0:
            out.append(rect(name, x, y, w, h, VOID))
            if w >= 48:
                out.append(label(f"{name}_label", x, y, w, h, "not floor", 1, 1))
    for c in range(floor["grid"]["w"] + 1):
        out.append(rect(f"col_{c}", min(c * tile, gw - 1), 0, 1, gh, LINE))
    for r in range(floor["grid"]["h"] + 1):
        out.append(rect(f"row_{r}", 0, min(r * tile, gh - 1), gw, 1, LINE))
    for i, fixed in enumerate(floor.get("fixedRooms") or []):
        c, r, w, h = fixed["rect"]
        out.append(rect(f"room_{i}", c * tile, r * tile, w * tile, h * tile, ROOM))
        out.append(label(f"room_{i}_label", c * tile + 2, r * tile + 2, w * tile - 4, h * tile - 4, f"{rooms[fixed['defId']]} (its room scene)", 0, 2))
    out.append(label("takes", 2, 2, slot_w - 4, 30, f"{floor['name']} · {', '.join(floor['roomKinds'])} rooms"))
    return out


def strip(text):
    """The scene without its Guides subtree: every section whose node is Guides or under it."""
    keep = []
    for section in re.split(r"(?m)^(?=\[)", text):
        m = re.match(r'\[node name="([^"]+)"[^\]]*?parent="([^"]*)"', section)
        if m and ((m[1] == "Guides" and m[2] == ".") or m[2] == "Guides" or m[2].startswith("Guides/")):
            continue
        keep.append(section)
    return "".join(keep)


def signature(text):
    """What the Guides subtree says, as Godot would compare it: each node's name, parent and
    non-default properties, ignoring node ids, comments and property order. Godot rewrites all
    three when it saves a scene, so comparing text would call every saved scene stale."""
    out = set()
    for section in re.split(r"(?m)^(?=\[)", text):
        m = re.match(r'\[node name="([^"]+)"[^\]]*?parent="([^"]*)"', section)
        if not m or not ((m[1] == "Guides" and m[2] == ".") or m[2] == "Guides" or m[2].startswith("Guides/")):
            continue
        lines = section.strip().splitlines()[1:]
        props = frozenset(l for l in lines if "=" in l and not l.startswith(";") and not re.fullmatch(r"offset_\w+ = 0\.0", l))
        out.add((m[1], m[2], props))
    return out


def main():
    manifest = load("manifest/sprites.json")
    tile = manifest["tileSize"]
    slot = next(e for e in manifest["entries"] if e["id"] == "ui.build.floor_frame")["sprite"]
    floors = {f["id"].split(".")[1]: f for f in load("content/floors.json")["floors"]}
    rooms = {r["id"]: r["name"] for r in load("content/rooms.json")["rooms"]}
    stale = []
    for scene in sorted(FLOORS.glob("*/*.tscn")):
        floor = floors.get(scene.stem)
        if floor is None:
            sys.exit(f"{scene.relative_to(ROOT)}: no floor.{scene.stem} in content/floors.json")
        old = scene.read_text(encoding="utf-8")
        new = strip(old).rstrip("\n") + "\n\n" + "\n".join(guides(floor, rooms, tile, slot["w"], slot["h"]))
        if signature(new) != signature(old):
            stale.append(scene.relative_to(ROOT).as_posix())
            if "--check" not in sys.argv:
                scene.write_text(new, encoding="utf-8", newline="\n")
    if "--check" in sys.argv:
        print("\n".join(stale) or "floor guides up to date")
        sys.exit(1 if stale else 0)
    print(f"wrote guides into {len(stale)} floor scene(s)")


if __name__ == "__main__":
    main()
