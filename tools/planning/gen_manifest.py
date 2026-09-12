#!/usr/bin/env python3
"""Generates manifest/sprites.json, manifest/greybox_palette.json, schema/manifest.schema.json
from content/*.json and the screen specs in GAME_DESIGN.md §19. Scratchpad tool; outputs are the deliverable."""
import json, os, sys
from collections import OrderedDict as OD
import jsonschema

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
def load(p): return json.load(open(os.path.join(ROOT, "content", p)))
employees = load("employees.json")["employees"]
rooms = load("rooms.json")["rooms"]
furniture = load("furniture.json")["furniture"]
statuses = load("statuses.json")["statuses"]
founders = load("founders.json")["founders"]
CONTENT_VERSION = load("index.json")["contentVersion"]

BC = OD([("x", 0.5), ("y", 1.0)])   # bottom-centre
TL = OD([("x", 0.0), ("y", 0.0)])   # top-left
CC = OD([("x", 0.5), ("y", 0.5)])   # centre
TC = OD([("x", 0.5), ("y", 0.0)])   # top-centre
BL = OD([("x", 0.0), ("y", 1.0)])   # bottom-left

entries = []
def path_for(id_, perspective):
    return "game/assets/%s/%s.png" % (perspective, id_.replace(".", "/"))

def entry(id_, kind, category, label, w, h, anchor, footprint=None, sortBias=0, screens=(), visibility=3,
          perspective="ui", reads="", candidate=None, verify=False, releaseGate=True, rect=None, frames=None, note=None):
    d = OD([("id", id_), ("kind", kind), ("category", category), ("label", label)])
    d["footprint"] = OD([("w", footprint[0]), ("h", footprint[1])]) if footprint else None
    sp = OD([("w", w), ("h", h), ("anchor", anchor), ("asset", path_for(id_, perspective)), ("sourceRect", None)])
    if frames: sp["frames"] = frames
    d["sprite"] = sp
    d["sortBias"] = sortBias
    d["perspective"] = perspective
    d["screens"] = list(screens)
    d["visibility"] = visibility
    d["reads"] = reads
    d["candidateSource"] = candidate
    d["verify"] = verify
    d["releaseGate"] = releaseGate
    if rect: d["layout"] = OD([("x", rect[0]), ("y", rect[1])])
    if note: d["note"] = note
    entries.append(d)
    return d

# ---------------------------------------------------------------- employees
# Which Character Pack body each employee wears. Age and formality carry the tier
# (students and youths at T1, working adults at T2, elders and traditional dress at T3);
# no two employees in the same department share a body. `tools/dev/cut_employees.py`
# reads these names back out of the manifest to cut the sprites (D-70).
EMP_BASE = {
    "emp.intern": "MaleYouth", "emp.junior_dev": "MaleStudent1", "emp.qa_tester": "FemaleStudent",
    "emp.senior_dev": "MaleBusinessMan", "emp.devops": "MalePunk", "emp.sysadmin": "MaleCasual",
    "emp.architect": "MaleBusinessManOld", "emp.cto": "MaleTraditional",
    "emp.paralegal": "FemaleTrendy", "emp.compliance_officer": "MaleStudent",
    "emp.counsel": "FemaleOfficeWorker", "emp.patent_attorney": "MaleTrafficCop",
    "emp.general_counsel": "FemaleElder",
    "emp.recruiter": "FemaleYouth", "emp.office_manager": "FemaleBaker",
    "emp.hr_manager": "FemaleCafeMaid", "emp.trainer": "MaleCasual",
    "emp.head_of_people": "MaleBusinessManOld",
    "emp.telemarketer": "MaleStudent1", "emp.sales_rep": "FemaleTrendy",
    "emp.account_manager": "MaleBusinessMan", "emp.headhunter": "MalePunk",
    "emp.sales_director": "FemaleElder", "emp.key_account_manager": "MaleTraditional",
    "emp.team_lead": "MaleYouth", "emp.project_manager": "FemaleStudent",
    "emp.middle_manager": "MaleTrafficCop", "emp.consultant": "FemaleOfficeWorker",
    "emp.director": "MaleBusinessManOld", "emp.vp_operations": "MaleTraditional",
    "emp.x_salaryman_ghost": "MaleBusinessMan", "emp.x_office_lady": "FemaleOfficeWorker",
    "emp.x_auditor": "MaleTrafficCop", "emp.x_fax_spirit": "Gutty-Chan",
    "emp.x_kappa_intern": "MaleYouth", "emp.x_salaryman_who_never_left": "MaleBusinessManOld",
    "emp.x_the_chairman": "MaleTraditional", "emp.x_the_partner": "FemaleElder",
    "emp.x_efficiency_wraith": "Witch", "emp.x_recruiting_oni": "MalePunk",
}
# Extraplanar bodies are recoloured; the hue names the tone the recolour lands on.
EMP_TINT = {"emp.x_kappa_intern": "green", "emp.x_recruiting_oni": "red"}
for e in employees:
    xp = e["extraplanar"]
    vis = 1 if (e["tier"] == 1 and e["inShop"]) else (2 if e["inShop"] and not xp else (3 if xp and e["inShop"] else 4))
    base = EMP_BASE[e["sprite"]]
    src = "characterpack/Blackoutlinecharacters/%s/Idle frame 1 (facing down)" % base
    if xp: src += ", %s spectral recolour" % EMP_TINT.get(e["sprite"], "violet")
    entry(e["sprite"], "employee", "anomalous" if xp else "people", e["name"], 32, 32, BC, footprint=(1, 1),
          screens=["build", "shop", "battle.inset", "autopsy"], visibility=vis, perspective="topdown",
          reads="A %s%s at a glance; department must be readable from silhouette or uniform colour" % ("spectral " if xp else "", e["dept"]),
          candidate=src,
          frames=OD([("idleDown", [0, 0])]))
# ---------------------------------------------------------------- rooms
for r in rooms:
    vis = 1 if r["id"] in ("room.reception", "room.open_plan", "room.server_room", "room.sales_floor", "room.legal_dept", "room.break_room") else (4 if r["kind"] == "extraplanar" else 2)
    fw, fh = r["footprint"]["w"], r["footprint"]["h"]
    # One composed plan per room type, at the footprint's size plus a 32px band of
    # overhang above it for the back-row fittings (D-73); anchored bottom-left, so
    # §2.1 derives that band rather than anyone typing it.
    entry(r["tile"], "tile", "anomalous" if r["kind"] == "extraplanar" else ("structure" if r["kind"] == "reception" else "operations"),
          r["name"] + " floor plan", fw * 32, fh * 32 + 32, BL, footprint=(fw, fh), sortBias=-10,
          screens=["build", "battle.inset", "shop"], visibility=vis, perspective="topdown",
          reads="The room itself at %dx%d tiles: floor, and fittings along the back row that say '%s' without a label. Every tile stays walkable — people stand here" % (fw, fh, r["name"]),
          candidate="tools/dev/rooms/%s.room" % r["id"].split(".", 1)[1])
entry("ui.room_sign", "ui", "interface", "Room sign strip", 32, 8, TL, sortBias=-9, screens=["build"], visibility=1,
      reads="A label strip: room name in font.ui.8 with up to three Tenure pips at the right")
# ---------------------------------------------------------------- furniture
for f in furniture:
    w, h = f["footprint"]["w"], f["footprint"]["h"]
    if f["wallMounted"]:
        sw, sh, bias = 32, 40, -5
    else:
        sw, sh, bias = 32 * w, 32 * h, 0
    vis = 1 if f["rarity"] == "common" and "floors" not in f else (4 if f.get("floors") == ["floor.b1"] else 2)
    entry(f["sprite"], "furniture", "anomalous" if f.get("floors") == ["floor.b1"] else "support", f["name"], sw, sh, BC,
          footprint=(w, h), sortBias=bias, screens=["build", "shop", "battle.inset"], visibility=vis, perspective="topdown",
          reads="The object itself, readable at 32px; wall-mounted pieces overhang 8px above their tile",
          candidate="Office Interior" if f.get("floors") != ["floor.b1"] else "Horror Interiors",
          note="wall-mounted: 32x40 sprite over a 1x1 footprint; the top 8px is overhang" if f["wallMounted"] else None)
# ---------------------------------------------------------------- founders
FS = FS_SCREEN = ["founder"]
# Which Portraits-pack face each founder wears (D-71). `tools/dev/cut_founders.py` reads
# these names back out of the manifest; the badges take the same face's Character Pack body.
FOUNDER_FACE = {
    "founder.sato": "oldbusinessman1", "founder.hoshino": "femaletrendy1",
    "founder.okada": "malepunk1", "founder.nakagawa": "femalestudent1",
    "founder.moriyama": "youngbusinessman1", "founder.ueda": "femalebaker1",
    "founder.the_founder": "femaleelder1", "founder.kitamura": "maletraditional1",
}
FOUNDER_BODY = {
    "founder.sato": "MaleBusinessManOld", "founder.hoshino": "FemaleTrendy",
    "founder.okada": "MalePunk", "founder.nakagawa": "FemaleStudent",
    "founder.moriyama": "MaleBusinessMan", "founder.ueda": "FemaleBaker",
    "founder.the_founder": "FemaleElder", "founder.kitamura": "MaleTraditional",
}
for fo in founders:
    entry(fo["portrait"], "ui", "people", fo["name"] + " portrait", 96, 96, TL, screens=["founder", "build", "map", "codex"], visibility=1,
          reads="A face with a title: %s, %s. Reads as a person you would follow or would not" % (fo["name"], fo["title"]),
          candidate="portraits/Portraits/transparent_bg/%s_transparent.png" % FOUNDER_FACE[fo["id"]])
    entry(fo["badge"], "ui", "people", fo["name"] + " badge", 32, 32, TL, screens=["battle", "map"], visibility=2,
          reads="The same person at 32px; recognisable beside the Goodwill bar",
          candidate="characterpack/Blackoutlinecharacters/%s/Idle frame 1 (facing down)" % FOUNDER_BODY[fo["id"]])
    entry(fo["id"] + ".thumb", "ui", "people", fo["name"] + " thumbnail", 48, 48, TL, screens=FS_SCREEN, visibility=3,
          reads="The same face at 48px, for the select grid; legible at a glance in a column of eight",
          candidate="portraits/Portraits/transparent_bg/%s_transparent.png, halved 2:1" % FOUNDER_FACE[fo["id"]])
entry("ui.founder.tile", "ui", "people", "Founder grid tile", 64, 64, TL, screens=FS, visibility=3,
      reads="A 48x48 thumbnail inset at (8,8); selected state is a distinct border, unmissable at a glance (D-72)")
entry("ui.founder.grid", "ui", "interface", "Founder grid", 152, 312, TL, screens=FS, visibility=3, rect=(8, 32), reads="The roster column: two columns at x = 16 + col × 72 and four rows at y = 40 + row × 72, of 64 × 64 tiles")
entry("ui.founder.detail", "ui", "interface", "Founder detail panel", 464, 312, TL, screens=FS, visibility=3, rect=(168, 32), reads="The selected founder in full: portrait, name and title, battle badge, bio, what the run starts with, and the commit row")
entry("ui.founder.confirm", "ui", "interface", "FOUND THE FIRM button", 160, 24, TL, screens=FS, visibility=3, rect=(456, 306),
      reads="The screen's one commit: 160x24, the label centred in font.ui.16 with 15px clear of the 9px corners, filled in the operations tone's dark end so it carries against the panel. Its centre line matches the firm-name field beside it")
entry("ui.battle.founder", "ui", "people", "Battle founder badge frame", 36, 36, TL, screens=["battle"], visibility=2, rect=(8, 48), reads="A 2px frame around a 32x32 badge; A at (8,48), B mirrored at (596,48)")
entry("ui.map.dossier_badge", "ui", "people", "Dossier founder badge frame", 36, 36, TL, screens=["map"], visibility=3, reads="A 2px frame around the rival founder's 32x32 badge at (160,4) inside the dossier")
entry("ui.build.firm_panel", "ui", "interface", "Inspector default: the firm", 200, 304, TL, screens=["build"], visibility=1, rect=(432, 32),
      reads="Shown when nothing is selected: founder portrait 96x96 at (440,40); firm name at (544,44); founder name and title at (544,54) and (544,64); run stats from y=144: round, strikes, fights won, Goodwill cap, floors leased, staff count")

# ---------------------------------------------------------------- build screen
B = ["build"]
entry("ui.build.topbar", "ui", "interface", "Build top bar", 640, 24, TL, screens=B, visibility=1, rect=(0, 0), reads="Round, budget, upkeep, strikes, READY")
entry("ui.build.tower", "ui", "interface", "Tower viewport frame", 176, 304, TL, screens=B, visibility=1, rect=(8, 32), reads="Frame around the three-floor viewport")
entry("ui.build.shaft", "ui", "structure", "Elevator shaft", 16, 304, TL, screens=B, visibility=1, rect=(8, 32), reads="A vertical shaft with floor labels; the landing column sits to its right", candidate="Office Interior (elevator doors)")
entry("ui.build.floor_frame", "ui", "structure", "Floor viewport frame", 160, 96, TL, screens=B, visibility=1, reads="Frame for one 5x3 floor; dimmed at 50% when not selected")
entry("ui.build.floor_void", "ui", "structure", "Hatched void for undersized floors", 32, 32, TL, screens=B, visibility=2, reads="Hatched tile filling the unused part of a 4x2 or 3x3 floor's 160x96 slot")
entry("ui.build.landing_highlight", "ui", "interface", "Landing column highlight", 1, 32, TL, screens=B, visibility=1, reads="A 1px vertical highlight on the left edge of landing tiles")
entry("ui.build.shop", "ui", "interface", "Shop panel", 232, 304, TL, screens=B, visibility=1, rect=(192, 32), reads="Panel with tab bar, card rows, Otherworld row, Lease section")
entry("ui.build.tab", "ui", "interface", "Shop tab", 76, 16, TL, screens=B, visibility=1, reads="STAFF / ROOMS / FURNITURE tab, selected state distinct")
entry("ui.build.lease_button", "ui", "structure", "Lease tag", 72, 24, TL, screens=B, visibility=1, reads="Tag over an unleased floor: the lease price over the upkeep it adds; the whole floor is the button (D-83)")
entry("ui.build.inspector", "ui", "interface", "Inspector panel", 200, 304, TL, screens=B, visibility=1, rect=(432, 32), reads="Portrait, name, department, detail rows, action button")
entry("ui.build.action_button", "ui", "interface", "Inspector action button", 90, 20, TL, screens=B, visibility=1, reads="LAY OFF · ¥1; for rooms, RELOCATE · ¥13 and DEMOLISH · ¥13 side by side at x=440 and x=534")
entry("ui.build.room_compare", "ui", "interface", "Room relocation comparison", 184, 24, TL, screens=B, visibility=1,
      reads="Three lines of font.ui.8 for a selected room: 'here x1.40 · Tier II'; 'on 2F x1.38 now, x1.61 by round 14'; 'relocate: -3 Tenure rounds, ¥13'")
entry("ui.build.ready", "ui", "interface", "READY button", 80, 16, TL, screens=B, visibility=1, rect=(552, 4), reads="The commit. No confirmation")
entry("ui.build.ready_shop", "ui", "interface", "READY button under the shop", 232, 20, TL, screens=B, visibility=1, rect=(192, 312), reads="The same commit where the thumb already is on touch (D-68); font.ui.16")
entry("ui.build.hint", "ui", "interface", "Hint line", 640, 16, TL, screens=B, visibility=1, rect=(0, 344), reads="One line of font.ui.8")
entry("ui.build.promote_glyph", "ui", "interface", "Promote glyph", 16, 16, CC, sortBias=9, screens=B, visibility=1, reads="An unmistakable 'combine available' mark, pulsing")
entry("ui.build.nearmiss_glyph", "ui", "interface", "Near-miss glyph", 8, 8, CC, sortBias=9, screens=B, visibility=1, reads="A small '?' that flickers once")
entry("ui.card.applicant", "ui", "people", "Applicant card", 52, 80, TL, screens=["build", "shop", "reward"], visibility=1,
      reads="Name and price only (D-82): idle sprite frame on a lighter stage; name; price tag along the bottom. What it does is read in the inspector")
entry("ui.card.room", "ui", "operations", "Room card", 52, 80, TL, screens=["build", "shop"], visibility=1,
      reads="As applicant card: the whole room plan drawn down to fit the stage; name; price tag (D-82)")
entry("ui.card.furniture", "ui", "support", "Furniture card", 52, 80, TL, screens=["build", "shop"], visibility=1, reads="As applicant card: sprite on the stage; name; price tag (D-82)")
entry("ui.card.otherworld", "ui", "anomalous", "Otherworld Temp Agency card", 52, 80, TL, screens=["build", "shop"], visibility=3, reads="As applicant card in the anomalous tone; its rider is read in the inspector (D-82)")
entry("ui.card.selector", "ui", "support", "Card selector", 64, 92, TL, screens=["build", "shop"], visibility=1, reads="The picked card's frame: four rounded corner brackets 6px outside the card, drawn over it")
entry("ui.card.price", "ui", "structure", "Card price tag", 46, 13, TL, screens=["build", "shop"], visibility=1, reads="Rounded tag along the card's bottom edge; the price in Honeyblot Caps at the height of the card's price_text slot (D-82)")
entry("ui.aura_badge", "ui", "interface", "Aura badge", 12, 8, OD([("x", 1.0), ("y", 0.0)]), sortBias=8, screens=B, visibility=1, reads="'x1.2' in font.ui.8 on a tag, top-right of the tile")
entry("ui.link_line", "ui", "interface", "Furniture link line", 1, 1, TL, sortBias=7, screens=B, visibility=1, reads="1px line from furniture to each triggered employee; tiles the length")
entry("ui.tenure_pip", "ui", "interface", "Tenure pip", 4, 4, TL, sortBias=-9, screens=B, visibility=1, reads="A filled 4x4 pip; up to three in the sign")
entry("ui.portrait", "ui", "people", "Inspector portrait", 96, 96, TL, screens=["build"], visibility=1,
      reads="A face. Larger than the sprite, same character", candidate="Portraits pack, transparent_bg (96x96, D-71)")
for d in ["engineering", "legal", "hr", "sales", "management", "extraplanar"]:
    entry("ui.dept." + d, "icon", "interface", "Department icon: " + d, 8, 8, TL, screens=["build", "shop", "battle.inset"], visibility=1, reads="Department at 8px: a glyph, not a letter")
entry("ui.tier_pip", "icon", "interface", "Tier pip", 4, 4, TL, screens=["build", "shop"], visibility=1, reads="Filled pip; one to three")
for s in statuses:
    entry("ui.status." + s["id"].split(".")[1], "icon", s["tone"], "Status icon: " + s["name"], 8, 8, TL, screens=["battle.inset", "autopsy", "build"], visibility=2, reads="The status at 8px, with a stack count beside it")
# ---------------------------------------------------------------- battle screen
BT = ["battle"]
entry("bg.battle.street", "background", "structure", "Battle street backdrop", 640, 360, TL, screens=BT, visibility=2, perspective="exterior", rect=(0, 0),
      reads="A Japanese city street at dusk seen front-on, two lots facing each other across it", candidate="Osaka / Dotonbori / Dark Tokyo street tiles")
entry("ui.battle.bar", "ui", "interface", "Market Share bar", 320, 12, TL, screens=BT, visibility=2, rect=(160, 8), reads="Two-colour fill from 50/50, 10% ticks, percent labels at both ends")
entry("ui.battle.goodwill_bar", "ui", "interface", "Goodwill bar", 200, 16, TL, screens=BT, visibility=2, rect=(8, 28),
      reads="A frame whose right (or left, mirrored) end marks the live cap; fill inside it; number in font.ui.16 overlaid; dims when suppressed; flashes on break")
entry("ui.battle.banner", "ui", "interface", "Month banner", 160, 12, TL, screens=BT, visibility=2, rect=(240, 48), reads="'— CRUNCH —' centred; slides in dimmed one second early")
entry("ui.battle.ledger", "ui", "interface", "Ledger panel", 308, 56, TL, screens=BT, visibility=2, rect=(8, 304), reads="Header + six lines of font.ui.8; amount, name, source, xN badge; tone by kind")
entry("ui.battle.ledger_rollup", "ui", "interface", "Ledger roll-up line", 308, 8, TL, screens=BT, visibility=2, reads="'+3 more · Fl.2' dimmed")
entry("ui.battle.floor_inset", "ui", "interface", "Floor inset", 168, 104, TL, screens=BT, visibility=2, reads="A 4px frame around a 160x96 top-down floor; most recent firer highlighted")
entry("ui.battle.controls", "ui", "interface", "Playback controls", 72, 16, TL, screens=BT, visibility=2, rect=(520, 48), reads="1x 2x 4x and skip; left of founder B's badge, which starts at x=596")
entry("ui.battle.result", "ui", "interface", "Result banner", 640, 24, TL, screens=BT, visibility=2, rect=(0, 0), reads="'Q7 · WON · 71.2% MARKET SHARE'")
entry("fx.tower.floor_segment", "fx", "structure", "Tower floor segment", 96, 32, BC, screens=BT, visibility=2, perspective="exterior", rect=(200, 280),
      reads="One storey of an office-block facade, three 32px tiles wide, carrying a 5x3 grid of unlit windows; tiles vertically", candidate="Osaka Tilemap.png facade band at (352, 32) 96x32, windows authored over it (D-69)")
entry("fx.tower.floor_segment_empty", "fx", "structure", "Unleased floor segment", 96, 32, BC, screens=BT, visibility=2, perspective="exterior", reads="The same storey as bare wall, no windows, in the structure tone", candidate="Osaka Tilemap.png wall band at (352, 32) 96x32")
entry("fx.tower.roof", "fx", "structure", "Tower roof", 96, 16, BC, screens=BT, visibility=2, perspective="exterior", reads="Parapet cap seen front-on, with a water tank or signage", candidate="Osaka Tilemap.png building cap at (256, 0) 96x16")
entry("fx.tower.basement", "fx", "anomalous", "B1 basement segment", 96, 24, TC, screens=BT, visibility=4, perspective="exterior", reads="A below-street storey, darker, one lit window", candidate="Osaka wall band at (352, 32) 96x24, darkened with a Horror Interiors tint")
entry("fx.tower.window_occupant", "fx", "people", "Window occupant", 8, 8, CC, screens=BT, visibility=2, perspective="exterior", reads="One per employee, filling its window in the facade's 5x3 grid; tone by department (D-68)")
entry("fx.window_burst", "fx", "interface", "Window burst", 16, 16, CC, screens=BT, visibility=2, perspective="exterior", reads="A 4-frame burst at the firer's window; tone by damage kind", frames=OD([("burst", [0, 0, 4])]))
entry("fx.floating_number", "fx", "interface", "Floating number", 40, 8, CC, screens=BT, visibility=2, reads="font.ui.8 digits rising 16px over 20 ticks")
# ---------------------------------------------------------------- autopsy
AU = ["autopsy"]
entry("ui.autopsy.banner", "ui", "interface", "Autopsy banner", 640, 24, TL, screens=AU, visibility=2, rect=(0, 0), reads="Result line")
entry("ui.autopsy.timeline", "ui", "interface", "Share timeline", 624, 48, TL, screens=AU, visibility=2, rect=(8, 32), reads="60 columns of 10px, month lines, draggable playhead")
entry("ui.autopsy.floors", "ui", "interface", "Per-floor contribution", 200, 120, TL, screens=AU, visibility=2, rect=(8, 88), reads="Five rows, two bars each, labelled")
entry("ui.autopsy.findings", "ui", "interface", "Findings panel", 200, 120, TL, screens=AU, visibility=2, rect=(8, 216), reads="Three findings in font.ui.8")
entry("ui.autopsy.filters", "ui", "interface", "Filter chips", 416, 16, TL, screens=AU, visibility=2, rect=(216, 88), reads="Kind, side and floor chips; selected state distinct")
entry("ui.autopsy.ledger", "ui", "interface", "Full ledger", 416, 228, TL, screens=AU, visibility=2, rect=(216, 108), reads="8px rows, scroll, playhead-selected row highlighted")
entry("ui.autopsy.continue", "ui", "interface", "CONTINUE button", 80, 16, TL, screens=AU, visibility=2, rect=(552, 340), reads="CONTINUE")
# ---------------------------------------------------------------- map
MP = ["map"]
entry("bg.map", "background", "structure", "Campaign map backdrop", 640, 360, TL, screens=MP, visibility=3, perspective="exterior", rect=(0, 0), reads="A city district seen from above, muted, paths readable over it", candidate="Osaka / Kanagawa street and roof tiles")
entry("ui.map.header", "ui", "interface", "Map header", 640, 24, TL, screens=MP, visibility=3, rect=(0, 0), reads="Act name and strikes")
for k in ["takeover", "audit", "recruiter", "board", "consultant", "boss"]:
    entry("ui.map.node." + k, "icon", "interface", "Map node: " + k, 24, 24, CC, screens=MP, visibility=3, reads="The node kind at 24px, distinct from the other five at a glance")
entry("ui.map.path", "ui", "interface", "Map path segment", 1, 1, TL, screens=MP, visibility=3, reads="1px path; tiles the length")
entry("ui.map.marker", "ui", "people", "Firm marker", 16, 16, CC, screens=MP, visibility=3, reads="The player's position: a small building or crest")
entry("ui.map.dossier", "ui", "interface", "Rival dossier", 200, 88, TL, screens=MP, visibility=3, reads="Name, archetype, floors, gimmick, a 5-cell floor strip at (4,64) of 16x8 cells")
entry("ui.map.legend", "ui", "interface", "Map legend", 640, 24, TL, screens=MP, visibility=3, rect=(0, 336), reads="Six node kinds with names")
entry("ui.map.floor_cell", "ui", "interface", "Dossier floor cell", 16, 8, TL, screens=MP, visibility=3, reads="Occupied / empty cell")
# ---------------------------------------------------------------- codex
CX = ["codex"]
entry("ui.codex.header", "ui", "interface", "Codex header", 640, 24, TL, screens=CX, visibility=3, rect=(0, 0), reads="'CODEX · 14 / 40', class chips, page arrows")
entry("ui.codex.recipe", "ui", "interface", "Recipe card", 176, 40, TL, screens=CX, visibility=3, reads="Three input slots 32x32 at x=4,40,76; arrow 16x8 at (112,12); result slot at (132,4); class glyph 8x8 at (168,4)")
entry("ui.codex.slot_outline", "ui", "interface", "Undiscovered slot", 32, 32, TL, screens=CX, visibility=3, reads="A dashed outline")
entry("ui.codex.arrow", "icon", "interface", "Recipe arrow", 16, 8, TL, screens=CX, visibility=3, reads="An arrow")
for c in ["promotion", "renovation", "ritual"]:
    entry("ui.codex.class." + c, "icon", "interface", "Recipe class glyph: " + c, 8, 8, TL, screens=CX, visibility=3, reads="The class at 8px")
# ---------------------------------------------------------------- shared / fonts / menus
entry("font.ui.8", "font", "interface", "UI pixel font, 8px line", 0, 8, TL, screens=["build", "battle", "autopsy", "map", "codex", "menu"], visibility=1,
      reads="Variable-width pixel font, ~5px average glyph, tabular digits; 8px line height at 1x", note="Width 0: a font entry declares line height only; glyph metrics live in the font file")
entry("font.ui.16", "font", "interface", "UI pixel font at 2x", 0, 16, TL, screens=["battle"], visibility=2, reads="font.ui.8 at exactly 2x, integer-scaled")
entry("font.ui.32", "font", "interface", "Screen title face, 32px line", 0, 32, TL, screens=["founder"], visibility=3,
      reads="Honeyblot Caps at a 32px line for a screen's own title, set on the background with no plate behind it (D-75)")
entry("bg.menu", "background", "structure", "Main menu backdrop", 640, 360, TL, screens=["menu"], visibility=3, perspective="exterior", rect=(0, 0), reads="The tower at night from the street", candidate="Backgrounds pack")
entry("ui.menu.button", "ui", "interface", "Menu button", 160, 16, TL, screens=["menu"], visibility=3, reads="A labelled button with hover state")
entry("ui.strike", "icon", "interface", "Strike icon", 8, 8, TL, screens=["build", "map"], visibility=1, reads="A strike: filled when spent")
entry("ui.budget_glyph", "icon", "interface", "Budget glyph", 8, 8, TL, screens=["build", "shop"], visibility=1, reads="A yen mark at 8px")
entry("ui.invalid_tile", "ui", "invalid", "Invalid placement flash", 32, 32, TL, sortBias=9, screens=B, visibility=1, releaseGate=False, reads="Unmissable red over a tile for one flash",
      note="Never has art. Exempt from the release gate by design: the invalid tone is the art")
entry("ui.debug.sortkey", "ui", "invalid", "Greybox debug sort-key label", 32, 8, TL, sortBias=10, screens=B, visibility=4, releaseGate=False, reads="Debug overlay only", note="Debug; exempt")
entry("ui.debug.coverage", "ui", "invalid", "Asset coverage overlay", 200, 64, TL, screens=["build", "battle", "autopsy", "map", "codex", "menu"], visibility=4, releaseGate=False, reads="Debug overlay; exempt")

# ---------------------------------------------------------------- derived: overhang
def overhang(e):
    fp = e["footprint"]
    if not fp or e["kind"] in ("ui", "icon", "font", "background", "fx"): return None
    sp = e["sprite"]; tw, th = fp["w"] * 32, fp["h"] * 32
    ax, ay = sp["anchor"]["x"], sp["anchor"]["y"]
    # sprite rect relative to footprint rect, both anchored at the footprint's anchor point
    # footprint anchor point in footprint space: (ax*tw, ay*th); sprite top-left = anchorPoint - (ax*sw, ay*sh)
    sx = ax * tw - ax * sp["w"]; sy = ay * th - ay * sp["h"]
    left = max(0, -sx); top = max(0, -sy)
    right = max(0, sx + sp["w"] - tw); bottom = max(0, sy + sp["h"] - th)
    return OD([("left", int(left)), ("top", int(top)), ("right", int(right)), ("bottom", int(bottom))])
for e in entries:
    e["overhang"] = overhang(e)   # derived at generation; the loader recomputes and asserts equality

# ---------------------------------------------------------------- palette
# Sanzo Wada's "A Dictionary of Color Combinations" no. 321. Four colours; the seven
# tones are mixed from them so the whole palette stays inside one 1930s scheme.
DRAB, SULPHER, OLIVE, SALVIA = "#b59392", "#f5ecc2", "#253122", "#97acc8"

def _rgb(h): return tuple(int(h.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
def _hex(t): return "#%02x%02x%02x" % t
def mix(a, b, k):
    """k of the way from a to b, in permille, because the sim's habits are catching."""
    ra, rb = _rgb(a), _rgb(b)
    return _hex(tuple((ra[i] * (1000 - k) + rb[i] * k) // 1000 for i in range(3)))

def tone(base, source):
    """fill, and a border and hatch that walk it toward the scheme's dark and light ends."""
    light = sum(_rgb(base)) > 384
    return OD([("fill", base), ("border", mix(base, OLIVE, 550)), ("hatch", mix(base, SULPHER, 400)),
               ("text", OLIVE if light else SULPHER), ("source", source)])

palette = OD([
    ("id", "greybox.palette"),
    ("note", "Sanzo Wada combination 321: Light Brown Drab #b59392, Sulpher Yellow #f5ecc2, Deep Slate Olive #253122, Salvia Blue #97acc8. Each tone's fill is one of those four or a mix of them; border walks 55% toward the olive, hatch 40% toward the yellow, and text is whichever end contrasts. Applied but not yet logged as a decision: Q-GBX-3's plan to sample from Office Interior still stands in the docs. 'invalid' is deliberately outside the scheme.",),
    ("tones", OD([
        ("structure", tone(mix(DRAB, OLIVE, 450), "Light Brown Drab toward Deep Slate Olive — wall and floor neutrals")),
        ("operations", tone(mix(SULPHER, OLIVE, 550), "Sulpher Yellow toward Deep Slate Olive — the olive-green of desks and commit buttons; deliberately off the Salvia axis so a button reads against interface chrome")),
        ("people", tone(DRAB, "Light Brown Drab, unmixed")),
        ("support", tone(mix(SULPHER, DRAB, 450), "Sulpher Yellow toward Light Brown Drab — cabinet ochre")),
        ("interface", tone(SALVIA, "Salvia Blue, unmixed")),
        ("anomalous", tone(mix(SALVIA, DRAB, 500), "Salvia Blue and Light Brown Drab — the extraplanar violet")),
        ("invalid", OD([("fill", "#e0202a"), ("border", "#8a0e15"), ("hatch", "#ff5c64"), ("text", "#ffffff"), ("source", "none — deliberately outside the scheme")])),
    ])),
    ("ground", OD([("backdrop", OLIVE), ("source", "Deep Slate Olive — what every screen sits on")])),
    ("rendering", OD([("footprintAlpha", 850), ("overhangAlpha", 450), ("borderPx", 1), ("hatchPitchPx", 4), ("labelFont", "font.ui.8"),
                      ("labelFallback", ["id+footprint+dims", "id", "dot"]), ("dotSize", 4)])),
])

manifest = OD([
    ("manifestVersion", 1),
    ("contentVersion", CONTENT_VERSION),
    ("tileSize", 32),
    ("canvas", OD([("w", 640), ("h", 360)])),
    ("scales", [2, 3, 4, 6]),
    ("assetRoot", "game/assets/"),
    ("palette", "manifest/greybox_palette.json"),
    ("visibilityTiers", OD([("1", "on the build screen every round"), ("2", "on the battle or autopsy screen every fight"), ("3", "seen most runs: map, codex, rarer content"), ("4", "seen rarely: B1, rituals, bosses, debug")])),
    ("entries", entries),
])

# ---------------------------------------------------------------- schema
ANCHOR = {"type": "object", "properties": {"x": {"type": "number", "minimum": 0, "maximum": 1}, "y": {"type": "number", "minimum": 0, "maximum": 1}}, "required": ["x", "y"], "additionalProperties": False}
schema = OD([
    ("$schema", "https://json-schema.org/draft/2020-12/schema"),
    ("$id", "https://companywars.invalid/schema/manifest.schema.json"),
    ("title", "Company Wars sprite manifest"),
    ("description", "Single source of truth for every visual slot. No layout code may hardcode a pixel size; everything reads this file. Absent asset files are valid. The validator fails only on a dimension mismatch when a file is present."),
    ("type", "object"),
    ("properties", OD([
        ("manifestVersion", {"type": "integer", "minimum": 1}), ("contentVersion", {"type": "string"}),
        ("tileSize", {"const": 32}), ("canvas", {"type": "object", "properties": {"w": {"const": 640}, "h": {"const": 360}}, "required": ["w", "h"], "additionalProperties": False}),
        ("scales", {"type": "array", "items": {"type": "integer", "minimum": 1}}), ("assetRoot", {"type": "string"}), ("palette", {"type": "string"}),
        ("visibilityTiers", {"type": "object"}),
        ("entries", {"type": "array", "minItems": 1, "items": {"$ref": "#/$defs/Entry"}}),
    ])),
    ("required", ["manifestVersion", "contentVersion", "tileSize", "canvas", "scales", "assetRoot", "palette", "visibilityTiers", "entries"]),
    ("additionalProperties", False),
    ("$defs", OD([
        ("Entry", OD([
            ("type", "object"),
            ("properties", OD([
                ("id", {"type": "string", "pattern": r"^[a-z0-9]+(\.[a-z0-9_]+)+$"}),
                ("kind", {"enum": ["employee", "furniture", "tile", "ui", "icon", "fx", "background", "font"]}),
                ("category", {"enum": ["structure", "operations", "people", "support", "interface", "anomalous", "invalid"]}),
                ("label", {"type": "string", "minLength": 1}),
                ("footprint", {"oneOf": [{"type": "null"}, {"type": "object", "properties": {"w": {"type": "integer", "minimum": 1}, "h": {"type": "integer", "minimum": 1}}, "required": ["w", "h"], "additionalProperties": False}]}),
                ("sprite", OD([("type", "object"), ("properties", OD([
                    ("w", {"type": "integer", "minimum": 0}), ("h", {"type": "integer", "minimum": 1}), ("anchor", ANCHOR),
                    ("asset", {"type": "string", "pattern": r"^game/assets/(topdown|exterior|ui)/.+\.png$"}),
                    ("sourceRect", {"oneOf": [{"type": "null"}, {"type": "array", "items": {"type": "integer", "minimum": 0}, "minItems": 4, "maxItems": 4}]}),
                    ("frames", {"type": "object", "additionalProperties": {"type": "array", "items": {"type": "integer", "minimum": 0}, "minItems": 2, "maxItems": 3}}),
                ])), ("required", ["w", "h", "anchor", "asset", "sourceRect"]), ("additionalProperties", False)])),
                ("sortBias", {"type": "integer", "minimum": -10, "maximum": 10}),
                ("perspective", {"enum": ["topdown", "exterior", "ui"]}),
                ("screens", {"type": "array", "items": {"enum": ["build", "shop", "battle", "battle.inset", "autopsy", "map", "codex", "menu", "reward", "founder"]}, "minItems": 1}),
                ("visibility", {"type": "integer", "minimum": 1, "maximum": 4}),
                ("reads", {"type": "string", "minLength": 1}),
                ("candidateSource", {"oneOf": [{"type": "null"}, {"type": "string"}]}),
                ("verify", {"type": "boolean"}),
                ("releaseGate", {"type": "boolean"}),
                ("layout", {"type": "object", "properties": {"x": {"type": "integer"}, "y": {"type": "integer"}}, "required": ["x", "y"], "additionalProperties": False}),
                ("note", {"type": "string"}),
                ("overhang", {"oneOf": [{"type": "null"}, {"type": "object", "properties": {k: {"type": "integer", "minimum": 0} for k in ["left", "top", "right", "bottom"]}, "required": ["left", "top", "right", "bottom"], "additionalProperties": False}]}),
            ])),
            ("required", ["id", "kind", "category", "label", "footprint", "sprite", "sortBias", "perspective", "screens", "visibility", "reads", "candidateSource", "verify", "releaseGate", "overhang"]),
            ("additionalProperties", False),
            ("allOf", [
                {"if": {"properties": {"kind": {"enum": ["employee", "furniture", "tile"]}}}, "then": {"properties": {"footprint": {"type": "object"}}}},
                {"if": {"properties": {"kind": {"enum": ["employee", "furniture", "tile"]}}}, "then": {"properties": {"perspective": {"const": "topdown"}}}},
                {"if": {"properties": {"perspective": {"const": "exterior"}}}, "then": {"properties": {"screens": {"items": {"enum": ["battle", "map", "menu"]}}}}},
                {"if": {"properties": {"perspective": {"const": "topdown"}}}, "then": {"properties": {"screens": {"items": {"enum": ["build", "shop", "battle.inset", "autopsy", "reward", "codex"]}}}}},
            ]),
        ])),
    ])),
])

def main():
    os.makedirs(os.path.join(ROOT, "manifest"), exist_ok=True)
    with open(os.path.join(ROOT, "manifest", "sprites.json"), "w") as fh: json.dump(manifest, fh, indent=2, ensure_ascii=False); fh.write("\n")
    with open(os.path.join(ROOT, "manifest", "greybox_palette.json"), "w") as fh: json.dump(palette, fh, indent=2, ensure_ascii=False); fh.write("\n")
    with open(os.path.join(ROOT, "schema", "manifest.schema.json"), "w") as fh: json.dump(schema, fh, indent=2, ensure_ascii=False); fh.write("\n")
    jsonschema.Draft202012Validator.check_schema(schema)
    v = jsonschema.Draft202012Validator(schema)
    errs = sorted(v.iter_errors(manifest), key=lambda e: list(e.path))
    for e in errs[:10]: print("SCHEMA", "/".join(str(p) for p in e.path), "-", e.message[:160])
    ids = [e["id"] for e in entries]
    dups = {i for i in ids if ids.count(i) > 1}
    if dups: print("DUP", dups)
    # every content sprite/tile referenced exists
    refs = [e["sprite"] for e in employees] + [r["tile"] for r in rooms] + [f["sprite"] for f in furniture]
    missing = [r for r in refs if r not in ids]
    if missing: print("MISSING", missing)
    # perspective rule: no screen mixes interior top-down and exterior facade art except via the inset
    by_screen = {}
    for e in entries:
        for s in e["screens"]: by_screen.setdefault(s, set()).add(e["perspective"])
    mixed = {s: p for s, p in by_screen.items() if "topdown" in p and "exterior" in p}
    print("entries:", len(entries), "| schema errors:", len(errs), "| dup ids:", len(dups), "| missing refs:", len(missing), "| mixed-perspective screens:", mixed or "none")
    tiers = {t: sum(1 for e in entries if e["visibility"] == t) for t in (1, 2, 3, 4)}
    print("by tier:", tiers, "| verify:", sum(1 for e in entries if e["verify"]), "| exempt:", sum(1 for e in entries if not e["releaseGate"]))
    sys.exit(1 if (errs or dups or missing or mixed) else 0)

if __name__ == "__main__":
    main()
