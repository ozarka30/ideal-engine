#!/usr/bin/env python3
"""Build the UI chrome from the Isle of Lore 2 UI Pack (D-74).

Every row of tools/dev/ui/slots.txt is `<manifest id> <element> <unit> <tone> [dark]`:

    ui.founder.confirm      button_square       18   operations   dark

`dark` shifts the whole ramp down a stop, for a primary action that has to carry
against a light panel.

The element is a folder under the pack's `ui_pack_elements`; `unit` is the 9-slice
corner size the pack's documentation gives for it. The element is recoloured onto a
greybox tone and 9-sliced at the pack's own resolution to SCALE x the manifest's size:
chrome is not pixel art, so it keeps its corners and is drawn down into its
manifest-sized rect with a smooth filter, the way D-67 treats text (D-76).

    python3 tools/dev/ui.py [--check]
"""
import json
import pathlib
import sys

from PIL import Image

SCALE = 2   # the pack's 18px corner is exactly 2x our 9px one, so 2x is lossless
ROOT = pathlib.Path(__file__).resolve().parents[2]
ELEMENTS = ROOT / "packs/stevencolling/isle_of_lore_2_ui/Sources/output/ui_pack_elements"
TABLE = ROOT / "tools" / "dev" / "ui" / "slots.txt"
PALETTE = ROOT / "manifest" / "greybox_palette.json"


def rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def ramp(tone, dark=False):
    """Four stops, darkest first: the pack's outline stays an outline.

    `dark` drops every stop by one, so the element's lit face lands on the tone's
    border rather than its fill -- a primary action against a light panel.
    """
    border, fill, hatch = rgb(tone["border"]), rgb(tone["fill"]), rgb(tone["hatch"])
    black = tuple(c * 40 // 100 for c in border)
    shadow = tuple(c * 70 // 100 for c in border)
    if dark:
        return [(0.0, black), (0.35, shadow), (0.70, border), (1.0, fill)]
    return [(0.0, shadow), (0.35, border), (0.70, fill), (1.0, hatch)]


def recolour(im, stops):
    out = im.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            t = (299 * r + 587 * g + 114 * b) / 255000
            for i in range(len(stops) - 1):
                t0, c0 = stops[i]
                t1, c1 = stops[i + 1]
                if t <= t1 or i == len(stops) - 2:
                    k = 0 if t1 == t0 else min(1.0, max(0.0, (t - t0) / (t1 - t0)))
                    px[x, y] = tuple(int(c0[j] + (c1[j] - c0[j]) * k) for j in range(3)) + (a,)
                    break
    return out


def nine(src, w, h, u):
    """9-slice src, whose corners are u px, out to w x h. Edges repeat by stretch."""
    if w < 2 * u or h < 2 * u:
        sys.exit(f"cannot 9-slice to {w}x{h}: the {u}px corners do not fit")
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sx, sw = [0, u, src.width - u], [u, src.width - 2 * u, u]
    sy, sh = [0, u, src.height - u], [u, src.height - 2 * u, u]
    dx, dw = [0, u, w - u], [u, w - 2 * u, u]
    dy, dh = [0, u, h - u], [u, h - 2 * u, u]
    for i in range(3):
        for j in range(3):
            if dw[i] <= 0 or dh[j] <= 0:
                continue
            part = src.crop((sx[i], sy[j], sx[i] + sw[i], sy[j] + sh[j]))
            out.alpha_composite(part.resize((dw[i], dh[j]), Image.NEAREST), (dx[i], dy[j]))
    return out


def main():
    check = "--check" in sys.argv
    entries = {e["id"]: e for e in json.loads((ROOT / "manifest/sprites.json").read_text(encoding="utf-8"))["entries"]}
    tones = json.loads(PALETTE.read_text(encoding="utf-8"))["tones"]
    done = 0
    for n, raw in enumerate(TABLE.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.split("#")[0].strip()
        if not line:
            continue
        parts = line.split()
        if len(parts) == 4:
            (slot, element, unit, tone), shade = parts, ""
        elif len(parts) == 5 and parts[4] == "dark":
            slot, element, unit, tone, shade = parts
        else:
            sys.exit(f"slots.txt:{n}: expected `<id> <element> <unit> <tone> [dark]`")
        if slot not in entries:
            sys.exit(f"slots.txt:{n}: {slot} is not a manifest entry")
        if tone not in tones:
            sys.exit(f"slots.txt:{n}: unknown tone {tone!r}")
        folder = ELEMENTS / f"{element}.standard"
        if not folder.is_dir():
            sys.exit(f"slots.txt:{n}: no element {element!r} in the pack")
        src = Image.open(sorted(folder.glob("*.png"))[0]).convert("RGBA")
        e = entries[slot]
        art = nine(recolour(src, ramp(tones[tone], shade == "dark")),
                   e["sprite"]["w"] * SCALE, e["sprite"]["h"] * SCALE, int(unit))
        dest = ROOT / e["sprite"]["asset"]
        if check:
            if not dest.exists():
                print(f"missing {e['sprite']['asset']}")
        else:
            dest.parent.mkdir(parents=True, exist_ok=True)
            art.save(dest)
        done += 1
    print(f"{'checked' if check else 'wrote'} {done} UI sprites")


if __name__ == "__main__":
    main()
