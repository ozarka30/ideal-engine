#!/usr/bin/env python3
"""Index the source art packs under packs/ (ART_PIPELINE.md §7.4).

Writes docs/pack_index.csv -- one row per PNG: pack (as `vendor/pack`), path, w, h.
The packs themselves are gitignored (licensed, ~1 GB); the index is committed so a
slot's candidateSource can name a real file without the packs on disk.

    python3 tools/dev/packs.py          # rewrite the index, print the summary
"""
import csv
import pathlib
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT / "packs"
INDEX = ROOT / "docs" / "pack_index.csv"


def png_size(path):
    """(w, h) from the IHDR chunk, or None if this is not a PNG."""
    with path.open("rb") as f:
        head = f.read(24)
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n" or head[12:16] != b"IHDR":
        return None
    return struct.unpack(">II", head[16:24])


def main():
    if not PACKS.is_dir():
        sys.exit(f"no {PACKS}; unzip the packs there first")
    rows = []
    for png in sorted(PACKS.rglob("*.png")):
        size = png_size(png)
        if size is None:
            print(f"skip (not a PNG): {png.relative_to(ROOT)}", file=sys.stderr)
            continue
        rel = png.relative_to(ROOT).as_posix()
        parts = rel.split("/")
        rows.append(("/".join(parts[1:3]), rel, size[0], size[1]))

    INDEX.parent.mkdir(parents=True, exist_ok=True)
    with INDEX.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(("pack", "path", "w", "h"))
        w.writerows(rows)

    packs = {}
    for pack, _, pw, ph in rows:
        p = packs.setdefault(pack, {"n": 0, "sizes": {}})
        p["n"] += 1
        p["sizes"][(pw, ph)] = p["sizes"].get((pw, ph), 0) + 1
    print(f"{len(rows)} sprites in {len(packs)} packs -> {INDEX.relative_to(ROOT)}")
    for name in sorted(packs):
        p = packs[name]
        top = sorted(p["sizes"].items(), key=lambda kv: -kv[1])[:3]
        common = ", ".join(f"{w}x{h}x{n}" for (w, h), n in top)
        licence = "" if (PACKS / name / "LICENSE.md").exists() else "  NO LICENCE"
        print(f"  {name:<44} {p['n']:>5}  {common}{licence}")


if __name__ == "__main__":
    main()
