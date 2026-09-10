#!/usr/bin/env python3
"""Cut sprites the manifest already names a source for (D-70, D-71).

An entry's `candidateSource` is machine-readable when it is either:

  packs-relative path      e.g. `portraits/Portraits/transparent_bg/malepunk1_transparent.png`,
                           optionally `, halved 2:1` (a 2 x 2 box average, the one resample
                           ART_PIPELINE.md §9 allows, and only at author time)
  a Character Pack facing  e.g. `characterpack/Blackoutlinecharacters/MalePunk/Idle frame 1
                                (facing down)`, optionally `, violet spectral recolour`

Both resolve under packs/guttykreum/. Everything else is left to the art pass.

    python3 tools/dev/cut_sprites.py [--check]
"""
import json
import pathlib
import re
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT / "packs" / "guttykreum"
# ponytail: candidateSource is prose we write ourselves, so one regex is the whole parser
IDLE = re.compile(r"^characterpack/Blackoutlinecharacters/(?P<base>[\w-]+)/Idle frame 1 \(facing down\)"
                  r"(?:, (?P<tint>\w+) spectral recolour)?$")
TINTS = {"violet": (150, 120, 230), "green": (110, 200, 150), "red": (220, 110, 110)}


def idle_down(base):
    """Frame 1 of a body's Idle set. The four Idle frames are the four facings, not a loop."""
    frames = sorted((PACKS / "characterpack" / "Blackoutlinecharacters" / base / "Idle").glob("*.png"))
    if len(frames) != 4:
        sys.exit(f"{base}: expected 4 idle facings, found {len(frames)}")
    return Image.open(frames[0]).convert("RGBA")


def spectral(im, tint):
    """Duotone onto the tint hue, keeping luminance, at 78% alpha. The outline stays dark."""
    out = im.copy()
    px = out.load()
    tr, tg, tb = TINTS[tint]
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = (299 * r + 587 * g + 114 * b) // 1000
            k = 0.30 + 0.70 * lum / 255
            px[x, y] = (int(tr * k), int(tg * k), int(tb * k), a * 78 // 100)
    return out


def source_image(candidate):
    """The image a candidateSource names, or None when the line is prose."""
    m = IDLE.match(candidate)
    if m:
        im = idle_down(m["base"])
        return spectral(im, m["tint"]) if m["tint"] else im
    half = candidate.endswith(", halved 2:1")
    path = candidate[: -len(", halved 2:1")] if half else candidate
    if path.endswith(".png") and (PACKS / path).is_file():
        im = Image.open(PACKS / path).convert("RGBA")
        return im.resize((im.width // 2, im.height // 2), Image.BOX) if half else im
    return None


def main():
    check = "--check" in sys.argv
    manifest = json.loads((ROOT / "manifest" / "sprites.json").read_text(encoding="utf-8"))
    done = skipped = 0
    for e in manifest["entries"]:
        im = source_image(e["candidateSource"] or "")
        if im is None:
            skipped += 1
            continue
        want = (e["sprite"]["w"], e["sprite"]["h"])
        if im.size != want:
            sys.exit(f"{e['id']}: source is {im.size[0]}x{im.size[1]}, manifest says {want[0]}x{want[1]}")
        dest = ROOT / e["sprite"]["asset"]
        if check:
            if not dest.exists():
                print(f"missing {e['sprite']['asset']}")
        else:
            dest.parent.mkdir(parents=True, exist_ok=True)
            im.save(dest)
        done += 1
    print(f"{'checked' if check else 'wrote'} {done} sprites; {skipped} entries have no machine-readable source")


if __name__ == "__main__":
    main()
