#!/usr/bin/env python3
"""Bakes the fallback pixel font the build ships until the real face lands (ART_PIPELINE.md §4.1, §9).

DejaVu Sans (Bitstream Vera licence, redistributable) rendered by FreeType in monochrome at a size whose
ascent+descent is exactly 8 px, packed into a BMFont (.fnt + .png) that Godot 4 imports as a FontFile.
font.ui.16 is the same face drawn at exactly 2x with nearest filtering. Run from the repository root:

    python3 tools/planning/gen_font.py

Outputs game/fonts/fallback_8.fnt and game/fonts/fallback_8.png; both are committed."""
import os
from PIL import Image, ImageFont, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "game", "fonts")
FACE = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
LINE = 8
CHARS = [chr(c) for c in range(32, 127)] + list("×·—−▸▲▼→←‰¥…°")

def bake():
    font = ImageFont.truetype(FACE, 7, layout_engine=ImageFont.Layout.BASIC)
    ascent, descent = font.getmetrics()
    base = min(ascent, LINE - 1)                                   # baseline row within the 8 px line
    glyphs = []
    for ch in CHARS:
        adv = max(1, round(font.getlength(ch)))
        img = Image.new("1", (16, 16), 0)
        ImageDraw.Draw(img).text((2, 2), ch, font=font, fill=1)   # origin at (2,2): top of the ascent box
        px = img.load()
        xs = [x for x in range(16) for y in range(16) if px[x, y]]
        ys = [y for x in range(16) for y in range(16) if px[x, y]]
        if not xs:
            glyphs.append((ch, None, 0, 0, 0, 0, adv))
            continue
        x0, x1, y0, y1 = min(xs), max(xs) + 1, min(ys), max(ys) + 1
        cell = img.crop((x0, y0, x1, y1))
        # yoffset is relative to the top of the 8 px line: the glyph's top row minus the ascent origin, shifted so the baseline sits at `base`.
        yoff = (y0 - 2) - (ascent - base)
        yoff = max(-1, min(yoff, LINE - 1))
        glyphs.append((ch, cell, x0 - 2, yoff, x1 - x0, y1 - y0, adv))
    # pack row-wise with 1 px gutters
    tex_w = 128
    x, y, row_h = 0, 0, 0
    placed = []
    for ch, cell, xoff, yoff, w, h, adv in glyphs:
        if cell is None:
            placed.append((ch, 0, 0, 0, 0, xoff, yoff, adv)); continue
        if x + w + 1 > tex_w:
            x, y, row_h = 0, y + row_h + 1, 0
        placed.append((ch, x, y, w, h, xoff, yoff, adv))
        x += w + 1
        row_h = max(row_h, h)
    tex_h = y + row_h + 1
    tex = Image.new("RGBA", (tex_w, max(1, tex_h)), (0, 0, 0, 0))
    for (ch, tx, ty, w, h, xoff, yoff, adv), (ch2, cell, *_rest) in zip(placed, glyphs):
        if cell is None: continue
        for cy in range(h):
            for cx in range(w):
                if cell.getpixel((cx, cy)):
                    tex.putpixel((tx + cx, ty + cy), (255, 255, 255, 255))
    os.makedirs(OUT, exist_ok=True)
    tex.save(os.path.join(OUT, "fallback_8.png"))
    lines = [
        f'info face="CompanyWarsFallback" size={LINE} bold=0 italic=0 charset="" unicode=1 stretchH=100 smooth=0 aa=1 padding=0,0,0,0 spacing=1,1 outline=0',
        f'common lineHeight={LINE} base={base} scaleW={tex_w} scaleH={tex_h} pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4',
        'page id=0 file="fallback_8.png"',
        f'chars count={len(placed)}',
    ]
    for ch, tx, ty, w, h, xoff, yoff, adv in placed:
        lines.append(f'char id={ord(ch)} x={tx} y={ty} width={w} height={h} xoffset={xoff} yoffset={yoff + (ascent - base) - (ascent - base)} xadvance={adv} page=0 chnl=15')
    with open(os.path.join(OUT, "fallback_8.fnt"), "w") as f:
        f.write("\n".join(lines) + "\n")
    print(f"baked {len(placed)} glyphs, ascent {ascent} descent {descent} base {base}, texture {tex_w}x{tex_h}")

if __name__ == "__main__":
    bake()
