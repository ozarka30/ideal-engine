# Company Wars — Design Brief

Status: **locked foundation**. This document records decisions already made. It is
the ground truth that `PLANNING_PROMPT.md` builds on. Anything not listed here is
still open and should be treated as a design question, not an assumption.

---

## 1. One-line pitch

An auto-battler where you build a haunted Japanese office tower floor by floor,
hire the staff to fill it, and send it into quarterly combat against another
player's building for market share.

## 2. Genre and reference points

- **Primary reference: Backpack Battles.** The target is its *depth model* — a
  scarce spatial grid, adjacency synergies, hidden combine recipes, and a
  cooldown-driven fight you cannot influence once it starts. Depth lives entirely
  in the build phase.
- **Divergence from Backpack Battles:** two asset classes instead of one.
  - **Rooms** are *static* — multi-tile footprints, bought once, expensive and
    painful to undo. They define zones and auras.
  - **Employees** are *flexible* — small units, freely repositioned between
    rounds, they carry the cooldowns and do the acting.
  - This creates a commitment-vs-reoptimisation tension Backpack Battles does not
    have, and it is the core strategic identity of the game.
- **Secondary reference (campaign mode only): Slay the Spire** — branching map,
  scripted encounters, run-scoped modifiers, meta-unlocks.

## 3. Locked decisions

| Area | Decision |
| --- | --- |
| Title | **Company Wars** |
| Genre | Auto-battler; shop/build round then async PvP round |
| Setting & tone | Retro Japanese corporate occult — 90s salaryman satire that quietly turns supernatural |
| Build space | **Multi-floor building.** Several small grids stacked vertically, connected by an elevator. Floors have distinct mechanical identity. Portal is literally a basement floor |
| Combat model | **Cooldown duel.** Nothing moves during combat. Employees fire abilities on their own cooldowns. Layout is a pure build-time puzzle |
| Win condition | **Market share tug-of-war fronted by Goodwill.** Each firm holds a Goodwill buffer; push depletes Goodwill first and only moves the shared bar once it breaks. Bar starts 50/50, resolves on full claim or the quarterly bell |
| Crafting | **Full hidden-recipe crafting.** Employees + equipment + room context combine into upgraded staff. Discovery is a primary retention driver |
| Hiring | **Shop with paid rerolls.** The portal unlocks mid-run and adds a second, riskier stock of extraplanar hires alongside the normal one |
| Modes | **Two, campaign first.** A Slay-the-Spire-style campaign ships first and carries the tutorial; a ranked ladder follows. Both share one sim and one content database |
| Platform | **Steam** |
| Tech | TypeScript + PixiJS + Vite; packaged for Steam via Tauri. Sim is a pure headless TS module |
| Development style | Primarily agentic coding, with human intervention for layout and hard design problems |
| Art workflow | **Greybox-first, incremental replacement.** Every visual element is built as a labelled placeholder at the exact dimensions and anchor the real asset will use, declared in a shared sprite manifest. Dropping real art in is a file copy, not a layout pass |
| Art scheduling | **The campaign ships fully playable in greybox.** Art is replaced incrementally and out of order thereafter, as the developer gets to it. There is no art pass milestone and no phase may be gated on art existing |

## 4. Art direction and the perspective split

Base asset pack: **GuttyKreum's "The Japan Collection"** (itch.io). All tiles are
32x32 pixel art. Relevant packs:

| Pack | Use |
| --- | --- |
| Office Interior (869 tiles) | Rooms, furniture, floor surfaces. Includes executive desks, fax machines, 90s PCs, water coolers, filing cabinets, whiteboards, monitoring stations, a Yakult cart |
| JRPG Characters Vol. 1 (20 sprites, 4-dir 8-frame walks) | Employee sprites |
| Portraits | Hiring shop / applicant cards |
| Interior Essentials (721 tiles) | Break rooms, lounges, non-office interiors |
| Horror Interiors | Basement / portal floor — the occult reveal, still in-style |
| Bar, Train Station, Train Interiors, School Interiors | Campaign event locations |
| Japanese City / Osaka / Dotonbori (isometric) | Exteriors, meta map, battle backdrop |
| Backgrounds | Menus, transitions |

**The city packs are isometric; the interior packs and characters are top-down.**
Do not mix them inside one view. This is resolved by assigning each perspective a
job:

- **Interior / build view — top-down.** The floorplan grid the player edits.
- **Exterior / battle view — isometric.** Two buildings facing each other across a
  street.

Extending to non-GuttyKreum art later is handled diegetically: the **Otherworld
Temp Agency** behind the portal supplies extraplanar contractors, so any future
pack in any style arrives as an in-fiction foreign body rather than an
inconsistency.

**Ship blocker to verify before any commercial release:** confirm the GuttyKreum
licence permits commercial use and redistribution-in-game for every pack used.

## 5. Battle presentation

The known hard problem with multi-floor: how does a player watch, and more
importantly *understand*, a fight spread across four grids?

Direction to develop:

- Camera sits on the street. Both towers face each other, drawn with isometric
  city art.
- Effects pop from windows — a floor firing an ability produces a burst, a
  floating number, an icon at that floor's windows.
- Hovering a floor opens its top-down floorplan as an inset/tooltip, showing which
  employee fired and what triggered.
- The market share bar is the single always-visible readout.
- **Post-battle diagnosis is mandatory, not optional.** A scrubbable timeline plus
  a per-floor contribution breakdown. In a build-craft game, a player who cannot
  work out *why* they lost will quit. This is a first-class feature, not polish.

## 6. Goodwill and the ledger

The tug-of-war's central weakness is that a bar which only moves one way at a time
makes defensive play read as *nothing happening*. The fix is a buffer layer with
its own visible number, and a running ledger beneath it.

**Goodwill** is each firm's defensive buffer — named for the real balance-sheet
line item, which makes it both accurate accounting and a pun. Incoming push
depletes the target's Goodwill; only once Goodwill breaks does further push move
the shared Market Share bar. "Break their Goodwill, then take their Market Share."

Beneath each firm's Goodwill number sits a **ledger** — a scrolling feed of named
entries that resolve into that number:

```
Q3 LEDGER · GOODWILL 4,200
  +840   Ship Feature      Senior Dev · Fl.3
 -1200   Cease & Desist    [RIVAL] Legal
  +300   Overtime          Junior Dev · Fl.2
```

This does two jobs at once. Live, it gives defensive builds something visibly
happening and turns the fight into readable drama. Scrubbed afterwards, it *is*
the post-battle autopsy — the diagnosis feature and the defence readout are the
same component. It also makes the satire mechanical rather than decorative:
corporate warfare rendered as bookkeeping.

Three rules to settle in design (see `PLANNING_PROMPT.md` Phase 1):

- **Regeneration.** Goodwill should regenerate, or defence is only a delay and
  turtle builds have no identity. But regeneration plus a flat attack curve means
  an unbreakable build, so the Quarter Close pressure curve must escalate attack
  values until any defence eventually yields.
- **Overflow.** A hit larger than remaining Goodwill should carry its excess
  straight into the bar. This rewards burst and alpha-strike timing; discarding
  the excess would quietly buff chip damage instead.
- **Piercing.** Some effects should bypass Goodwill entirely — Burnout is the
  natural candidate, since morale damage does not appear on a balance sheet. This
  prevents dead air in the opening seconds and supplies a counter-archetype:
  Legal turtles beat burst, burst beats economy, Burnout pierces turtles.

## 7. Greybox-first art workflow

Development proceeds in greybox: every visual element is a labelled placeholder
rectangle drawn at **exactly** the dimensions, anchor and grid footprint the real
asset will occupy. Real art is then dropped in without a layout pass.

This only works under one rule: **a greybox is never approximate.** The common
failure mode is placeholders that are roughly right, which means real art still
needs repositioning — precisely the work this is meant to eliminate. A greybox
whose dimensions are unknown is a blocker, not a placeholder. If the size of the
real thing has not been decided, decide it before building the greybox.

### One manifest, two render modes

Greybox and final art are the **same manifest entry**, never two code paths. A
shared sprite manifest declares each slot; the renderer draws a labelled
rectangle when no file exists at the declared path, and the texture when one does.
"Dropping assets over them" is then literally true — copy a PNG to the path and it
appears.

```jsonc
{
  "id": "room.server_room",
  "kind": "room",
  "footprint": { "w": 2, "h": 2 },          // grid cells occupied
  "sprite": {
    "w": 64, "h": 88,                       // pixels at 1x
    "anchor": { "x": 0.5, "y": 1.0 },       // bottom-centre
    "asset": "packs/guttykreum/office_interior/server_room.png",
    "sourceRect": null                      // or [x, y, w, h] into an atlas
  },
  "sortBias": 0
}
```

No layout code may hardcode a pixel size. Everything reads the manifest.

### Footprint is not visual bounds

The detail that actually breaks asset drop-in is not size, it is the difference
between the **grid cells an object occupies** and the **pixel rectangle it draws
into**. A server room occupies 2x2 cells but its sprite is 64x88, because the rack
rises above the tile it stands on. Top-down 32x32 art overhangs constantly.

So the manifest carries both, and the greybox draws both: a solid fill over the
footprint, and a hatched outline over the overhang. Anchor and pivot are declared
explicitly rather than assumed. Draw order for overhanging sprites (y-sorting plus
`sortBias` for ties) must be settled in greybox, or art will layer wrongly the
moment it arrives.

### Greyboxes are readable specs

A placeholder shows its id, its footprint, and its pixel dimensions on screen. A
greybox screenshot is then a design document — anyone can read what the real asset
must be. Use desaturated tones coded by category (rooms, employees, furniture, UI)
so screens stay legible without ever being mistaken for art direction.

### Validation is automated, not eyeballed

A CI check walks the manifest and, for every entry whose asset file is present,
asserts the image's real dimensions match the declared `sprite.w` and `sprite.h`
(or its `sourceRect`). A mismatch fails the build. This turns "it should just show
up as we intend" from a hope into an assertion, and lets an agent verify an art
drop without looking at it.

Related tooling to plan: a slicer that reads a GuttyKreum sheet and emits manifest
stubs, so real assets flow into the manifest rather than being hand-wired; and an
in-game overlay reporting asset coverage — how much of the build is still greybox,
what is temporary stand-in art, and what is final.

### Incremental replacement is the normal state

The campaign is built playable in greybox, and art replaces placeholders
incrementally and out of order over the following months. A screen that is part
real pixel art and part placeholder is therefore the *ordinary* condition, not a
brief transitional one. Three things follow.

**Greybox tones are drawn from the pack's own palette, desaturated** — not
arbitrary greys. A partially-arted screen has to look deliberate rather than
broken, because that is the view during most of development.

**No art pass milestone exists.** Roadmap phases are defined by systems only.
Nothing may be gated on art existing, or the art backlog becomes the critical
path. Art completeness is a *release* gate, tracked separately from development
progress.

**Manifest entries declare their asset path before the file exists.** Adding art
never edits the manifest — the path was already there. The validation check must
therefore treat an absent file as valid (that entry is simply still greybox) and
fail only on a dimension mismatch when a file *is* present.

### The art worklist

Because replacement is developer-driven and unordered, coverage reporting is not
enough on its own; there needs to be a ranked worklist. Plan a command that emits,
for every manifest entry still lacking art, a spec sheet containing:

- id, exact pixel dimensions, anchor, and grid footprint
- which screens it appears on, and how often it is on screen
- what it needs to read as at a glance
- the exact target file path to drop the finished asset at
- where known, a candidate source tile from the GuttyKreum packs — much of this
  work is selection and slicing rather than drawing
- optionally, a template PNG generated at the correct dimensions with the
  footprint and anchor marked

Rank the list by visibility: a shop card seen every round outranks a basement room
seen twice a run. The top fraction of that list carries most of the perceived
polish, which matters when art is fitted around other work.

### Pixel discipline

32x32 base, nearest-neighbour filtering, integer scale factors only, positions
snapped to integers. Fixed once, globally. Greyboxes authored at a fractional
scale will not match the art that replaces them.

## 8. Systems sketch (starting point, not settled)

Mappings from the Backpack Battles depth model:

| Source system | Company Wars form |
| --- | --- |
| Polyomino packing in a scarce bag | Room footprints on a scarce floor grid |
| Adjacency buffs | Employees must occupy a room to gain its effect; rooms are auras, furniture are triggers |
| Recipe crafting | Promotions and renovations — e.g. 2 Junior Devs + a whiteboard becomes a Senior Dev |
| Cooldown combat archetypes | Engineering pushes, Legal resists, HR restores, Sales grows economy, Middle Management retriggers neighbours |
| Status effects | **Burnout** (stacking damage over time), **Overtime** (haste at a cost), **Bureaucracy** (slow) |
| Gold / reroll / sell | **Budget**, reroll is reposting the job listing, selling is a layoff **with a severance fee** — so flexibility is real but never free |

Floor identity gives the vertical axis meaning rather than just more space:
Ground/Reception is exposed, mid floors are the engine, Executive is expensive and
high-multiplier, B4 is the portal and is cursed. Rival abilities that target "the
highest floor" or "the lowest floor" make floor assignment a genuine decision even
though nothing moves in combat.

## 9. Known risks

1. **Layer bloat.** Rooms + furniture + employees + equipment may be one layer too
   many. Plan for MVP to fold equipment into furniture and keep the expansion room.
2. **Two modes is two balance problems.** Mitigated by forcing one sim and one
   content database, with modes as a configuration layer only.
3. **Goodwill adds a layer to balance.** The Quarter Close pressure curve must
   now guarantee break-through against the *strongest* possible defensive build at
   each round, not merely an average one. This is an automated-testing obligation,
   and belongs in `BALANCE_PLAN.md` as an explicit invariant.
4. **Dead air at the start of a fight.** If Goodwill absorbs everything for the
   opening seconds, the bar sits still and fights open flat. Mitigated by piercing
   effects and by tuning starting Goodwill low relative to round-appropriate
   output.
5. **Recipe discovery needs an in-game codex** or it reads as opaque rather than
   deep.
6. **Cold-start PvP** — deferred, not solved. Campaign-first removes it from v1
   entirely; scripted campaign rival towers are intended to seed the ghost pool
   when ranked mode is built.
7. **The art backlog may never close.** Incremental, developer-paced replacement
   means the game can sit half-arted indefinitely, which blocks a Steam release
   even though it never blocks development. Mitigated by ranking the worklist so
   the highest-visibility assets land first, and by defining "art complete" as an
   explicit release gate tracked from day one rather than discovered at the end.
8. **Mixed-state visual coherence.** Greybox and finished art share every screen
   for months. If placeholders are not palette-matched and tone-coded, the game
   reads as broken to the person looking at it every day, which is corrosive to
   judgement about whether it is fun.

## 10. Still open

Carried into `PLANNING_PROMPT.md` as work to be done:

- How exactly floor output aggregates into Goodwill damage and then into the bar.
- Goodwill regeneration rate, overflow handling, and which effects pierce it.
- The shape of the Quarter Close pressure curve.
- Number of floors, grid dimensions per floor, and the expansion curve.
- Round count, fight duration, and lives per run.
- Whether furniture is a distinct layer in v1.
- Economy numbers of every kind.
- The full sprite manifest schema, and the draw-order rule for overhanging sprites.
- The definition of "art complete" that serves as the release gate.
