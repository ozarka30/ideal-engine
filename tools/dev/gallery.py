#!/usr/bin/env python3
"""Builds a single self-contained HTML contact sheet of every rendered screen, for viewing without the engine.

    python3 tools/dev/gallery.py                # -> build/gallery.html
    python3 tools/dev/gallery.py --out path.html

Sources, all optional:
  game/__screenshots__/*_2x.png          the committed fixtures (one row per screen)
  game/__screenshots__/actual/*_2x.png   the last render, shown beside the fixture when it differs
  game/__screenshots__/drive/*.png       the last drive run, in step order
  game/__screenshots__/references/<screen>/*.png|jpg
                                         reference shots you drop in (gitignored: they are other people's
                                         screenshots), shown beside the screen they are named after

Images are embedded as data URIs so the file travels on its own. Open it in a browser, or attach it to a CI run.
"""
import argparse
import base64
import glob
import html
import os
import subprocess
import time

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SHOTS = os.path.join(ROOT, "game", "__screenshots__")


def data_uri(path):
    ext = os.path.splitext(path)[1].lower()
    mime = "image/jpeg" if ext in (".jpg", ".jpeg") else "image/png"
    with open(path, "rb") as f:
        return f"data:{mime};base64,{base64.b64encode(f.read()).decode()}"


def same(a, b):
    with open(a, "rb") as fa, open(b, "rb") as fb:
        return fa.read() == fb.read()


def figure(path, caption, cls=""):
    return (f'<figure class="{cls}"><img src="{data_uri(path)}" alt="{html.escape(caption)}" loading="lazy">'
            f'<figcaption>{html.escape(caption)}</figcaption></figure>')


def build(out):
    fixtures = sorted(glob.glob(os.path.join(SHOTS, "*_2x.png")))
    drive = sorted(glob.glob(os.path.join(SHOTS, "drive", "*.png")))
    try:
        commit = subprocess.check_output(["git", "-C", ROOT, "log", "-1", "--format=%h %s"], text=True).strip()
    except Exception:  # noqa: BLE001 - the gallery must build outside a checkout too
        commit = "(not a git checkout)"
    sections = []
    for fx in fixtures:
        screen = os.path.basename(fx)[: -len("_2x.png")]
        parts = [figure(fx, f"{screen} · committed fixture, 2×", "main")]
        actual = os.path.join(SHOTS, "actual", os.path.basename(fx))
        if os.path.exists(actual) and not same(fx, actual):
            parts.append(figure(actual, f"{screen} · last render DIFFERS from the fixture", "main differs"))
        refs = sorted(glob.glob(os.path.join(SHOTS, "references", screen, "*.*")))
        for r in refs:
            parts.append(figure(r, f"reference · {os.path.basename(r)}", "ref"))
        sections.append(f'<section id="{screen}"><h2>{screen}</h2><div class="row">{"".join(parts)}</div></section>')
    if drive:
        parts = [figure(d, os.path.basename(d)[:-4], "drive") for d in drive]
        sections.append(f'<section id="drive"><h2>last drive run</h2><div class="row">{"".join(parts)}</div></section>')
    nav = " · ".join(f'<a href="#{os.path.basename(f)[:-7]}">{os.path.basename(f)[:-7]}</a>' for f in fixtures)
    if drive:
        nav += ' · <a href="#drive">drive</a>'
    page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Company Wars screens</title>
<style>
:root{{--bg:#2b2a2e;--panel:#3a393f;--text:#e9e6df;--muted:#a9a49a;--accent:#6b9a92;--warn:#d24b4b}}
body{{margin:0;padding:24px 16px 48px;background:var(--bg);color:var(--text);font:14px/1.5 system-ui,sans-serif}}
h1{{font-size:20px;margin:0 0 4px}} h2{{font-size:15px;letter-spacing:.06em;text-transform:uppercase;color:var(--accent);margin:32px 0 8px}}
.meta{{color:var(--muted);margin-bottom:8px}} nav a{{color:var(--accent);text-decoration:none}}
.row{{display:flex;flex-wrap:wrap;gap:12px;align-items:flex-start}}
figure{{margin:0;background:var(--panel);padding:8px;border-radius:4px;max-width:100%}}
figure.main img{{width:min(100%,1280px)}} figure.ref img,figure.drive img{{width:min(100%,640px)}}
figure.differs{{outline:2px solid var(--warn)}}
img{{display:block;max-width:100%;height:auto}} figcaption{{color:var(--muted);font-size:12px;margin-top:6px}}
</style></head><body>
<h1>Company Wars screens</h1>
<div class="meta">{html.escape(commit)} · built {time.strftime("%Y-%m-%d %H:%M")} · fixtures at 2×; drop reference shots in game/__screenshots__/references/&lt;screen&gt;/</div>
<nav>{nav}</nav>
{"".join(sections) if sections else "<p>No screenshots found. Render the fixtures first.</p>"}
</body></html>"""
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        f.write(page)
    print(f"wrote {os.path.relpath(out, ROOT)}: {len(fixtures)} screens, {len(drive)} drive shots, {os.path.getsize(out) // 1024} KB")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "build", "gallery.html"))
    build(ap.parse_args().out)
