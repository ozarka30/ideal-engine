#!/usr/bin/env python3
"""Bakes the game's bitmap fonts (ART_PIPELINE.md §4.1, §9; D-64, D-66).

Two faces, both from licensed TrueType sources that live in game/fonts/source/ and are not committed
(Steven Colling Font License 1.0 permits the bitmap export the game needs, not redistribution of the files):

    ui_8       Honey Pigeon at an 8 px line   — every body text: cards, ledger, inspector, hints
    header_16  Honeyblot Caps at a 16 px line — headers, banners, the Goodwill numbers, tall buttons

Each is rendered by FreeType in monochrome (no antialiasing, nearest filtering in the engine) and packed
into a BMFont (.fnt + .png) that Godot 4 imports as a FontFile. Glyphs a face lacks (arrows, minus) are
taken from DejaVu Sans at the same line height. When the sources are absent the committed output is left
alone. fallback_8 is the redistributable face (DejaVu Sans) the game uses if neither baked face exists.
Run from the repository root:

    python3 tools/planning/gen_font.py
"""
import os
import sys
from PIL import Image, ImageFont, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "game", "fonts")
SRC = os.path.join(OUT, "source")
DEJAVU = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
CHARS = [chr(c) for c in range(32, 127)] + list("×·—−▸▲▼→←‰¥…°")
# Symbols the licensed faces draw as blobs at these sizes, or lack outright; DejaVu's are the ones the UI was designed on.
BORROW = set("×·—−▸▲▼→←‰…°+")

FACES = [
    # name, source file, line height, texture width, point size (None: the largest that fits the line), pixel spacing
    # Pixel spacing: advance = ink width + 1 px, as a hand-made pixel font would; the face's fractional advances round unevenly at 8 px.
    ("ui_8", os.path.join(SRC, "HoneyPigeon.ttf"), 8, 128, None, True),
    ("header_16", os.path.join(SRC, "honeyblot_caps.ttf"), 16, 256, None, False),
    ("fallback_8", DEJAVU, 8, 128, 7, False),   # DejaVu at 7 pt overshoots the line by a row and is clamped, as D-64 shipped it
]


def pick_size(path, line, fixed=None):
    """The largest point size whose ascent + descent fits the line, or a fixed one."""
    if fixed is not None:
        f = ImageFont.truetype(path, fixed, layout_engine=ImageFont.Layout.BASIC)
        a, d = f.getmetrics()
        return fixed, f, a, d
    best = None
    for s in range(4, line + 4):
        f = ImageFont.truetype(path, s, layout_engine=ImageFont.Layout.BASIC)
        a, d = f.getmetrics()
        if a + d <= line:
            best = (s, f, a, d)
    if best is None:
        raise SystemExit(f"{path}: no size fits a {line} px line")
    return best


def has_glyph(font, ch):
    """True when the face has its own outline for ch: a missing glyph renders as the .notdef box, so compare against it."""
    if ch == " ":
        return True
    mask = font.getmask(ch, mode="1")
    if mask.getbbox() is None:
        return False
    notdef = font.getmask("\uE000", mode="1")   # private-use codepoint no text face assigns
    return mask.getbbox() != notdef.getbbox() or bytes(mask) != bytes(notdef)


def render(font, ch, pad):
    """Monochrome bitmap of one glyph with its top-left at (pad, pad) of the ascent box."""
    img = Image.new("1", (48, 48), 0)
    ImageDraw.Draw(img).text((pad, pad), ch, font=font, fill=1)
    px = img.load()
    xs = [x for x in range(48) for y in range(48) if px[x, y]]
    ys = [y for x in range(48) for y in range(48) if px[x, y]]
    if not xs:
        return None, 0, 0, 0, 0
    x0, x1, y0, y1 = min(xs), max(xs) + 1, min(ys), max(ys) + 1
    return img.crop((x0, y0, x1, y1)), x0 - pad, y0 - pad, x1 - x0, y1 - y0


def bake(name, path, line, tex_w, fixed=None, pixel_spacing=False):
    size, font, ascent, descent = pick_size(path, line, fixed)
    base = min(ascent, line - 1)                                   # baseline row within the line box
    fb_size, fb_font, fb_ascent, _ = pick_size(DEJAVU, line)
    pad = 4
    glyphs = []
    borrowed = 0
    for ch in CHARS:
        f, asc = font, ascent
        if (path != DEJAVU and ch in BORROW) or not has_glyph(font, ch):
            f, asc = fb_font, fb_ascent
            borrowed += 1
        adv = max(1, round(f.getlength(ch)))
        cell, xoff, ytop, w, h = render(f, ch, pad)
        if cell is None:
            glyphs.append((ch, None, 0, 0, 0, 0, max(2, adv) if pixel_spacing else adv))
            continue
        if pixel_spacing:
            xoff, adv = 0, w + 1
        # yoffset is from the top of the line box: the glyph's top relative to its face's ascent origin,
        # shifted so that face's baseline lands on `base`.
        yoff = ytop - (asc - base)
        yoff = max(-1, min(yoff, line - 1))
        glyphs.append((ch, cell, xoff, yoff, w, h, adv))
    # pack row-wise with 1 px gutters
    x, y, row_h = 0, 0, 0
    placed = []
    for ch, cell, xoff, yoff, w, h, adv in glyphs:
        if cell is None:
            placed.append((ch, 0, 0, 0, 0, xoff, yoff, adv))
            continue
        if x + w + 1 > tex_w:
            x, y, row_h = 0, y + row_h + 1, 0
        placed.append((ch, x, y, w, h, xoff, yoff, adv))
        x += w + 1
        row_h = max(row_h, h)
    tex_h = y + row_h + 1
    tex = Image.new("RGBA", (tex_w, max(1, tex_h)), (0, 0, 0, 0))
    for (ch, tx, ty, w, h, xoff, yoff, adv), (ch2, cell, *_rest) in zip(placed, glyphs):
        if cell is None:
            continue
        for cy in range(h):
            for cx in range(w):
                if cell.getpixel((cx, cy)):
                    tex.putpixel((tx + cx, ty + cy), (255, 255, 255, 255))
    os.makedirs(OUT, exist_ok=True)
    tex.save(os.path.join(OUT, f"{name}.png"))
    lines = [
        f'info face="{name}" size={line} bold=0 italic=0 charset="" unicode=1 stretchH=100 smooth=0 aa=1 padding=0,0,0,0 spacing=1,1 outline=0',
        f'common lineHeight={line} base={base} scaleW={tex_w} scaleH={tex_h} pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4',
        f'page id=0 file="{name}.png"',
        f'chars count={len(placed)}',
    ]
    for ch, tx, ty, w, h, xoff, yoff, adv in placed:
        lines.append(f'char id={ord(ch)} x={tx} y={ty} width={w} height={h} xoffset={xoff} yoffset={yoff} xadvance={adv} page=0 chnl=15')
    with open(os.path.join(OUT, f"{name}.fnt"), "w") as f:
        f.write("\n".join(lines) + "\n")
    print(f"{name}: {os.path.basename(path)} at {size} pt, ascent {ascent} descent {descent} base {base}, "
          f"{len(placed)} glyphs ({borrowed} from DejaVu), texture {tex_w}x{tex_h}")


if __name__ == "__main__":
    for name, path, line, tex_w, fixed, pixel_spacing in FACES:
        if not os.path.exists(path):
            print(f"{name}: source {os.path.relpath(path, ROOT)} absent; committed output kept", file=sys.stderr)
            continue
        bake(name, path, line, tex_w, fixed, pixel_spacing)
