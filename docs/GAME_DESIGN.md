# Company Wars — Game Design

Status: **Phase 2 draft.** Builds on `DESIGN_BRIEF.md` (locked foundation) and the
decisions in `DECISION_LOG.md` (D-01 to D-25). Where this document states a number,
the number is an initial value owned by `BALANCE_PLAN.md`; where it states a rule, the
rule is design. The combat rules referenced here are specified exactly in
`SIMULATION_SPEC.md`, which wins on any disagreement.

This document stands alone. A reader needs the brief for *why* and this document for
*what*; they should not need the conversation that produced either.

---

## Contents

1. [The game in one screen](#1-the-game-in-one-screen)
2. [Vocabulary](#2-vocabulary)
3. [The core loop, beat by beat](#3-the-core-loop-beat-by-beat)
4. [The building](#4-the-building)
5. [The build phase](#5-the-build-phase)
6. [The economy](#6-the-economy)
7. [Rooms](#7-rooms)
8. [Furniture](#8-furniture)
9. [Employees](#9-employees)
10. [Status effects](#10-status-effects)
11. [The fight](#11-the-fight)
12. [The ledger and the autopsy](#12-the-ledger-and-the-autopsy)
13. [Recipes and the codex](#13-recipes-and-the-codex)
14. [The portal](#14-the-portal)
15. [Campaign mode](#15-campaign-mode)
16. [Ranked mode and the configuration layer](#16-ranked-mode-and-the-configuration-layer)
17. [Progression and unlocks](#17-progression-and-unlocks)
18. [The new-player experience](#18-the-new-player-experience)
19. [Screen specifications](#19-screen-specifications)
20. [Legibility rules](#20-legibility-rules)
21. [What a human has to check](#21-what-a-human-has-to-check)

---

## 1. The game in one screen

You run a small firm in a haunted office tower. Each round you spend Budget on staff,
rooms and furniture, arrange them across the floors, and press Ready. Your tower then
fights another tower for sixty seconds. Nothing moves. Your employees fire their
abilities on cooldowns, their output is scaled by the room they stand in and the floor
they stand on, and it lands on the rival's **Goodwill** — a defensive buffer with a
visible bar and number. When Goodwill is empty, further damage moves the shared
**Market Share** bar. Claim the whole bar, or lead it when the quarterly bell rings, and you win.

Beneath each Goodwill number scrolls a **ledger** of named entries. It is the fight's
running commentary while it happens and its complete post-mortem afterwards. A player
who loses can always find out why.

Rooms are the thing you cannot take back. They are bought once, expensive to remove,
and they get *better* the longer they stay put. Employees are the thing you rearrange
every round. The game is the argument between those two facts.

---

## 2. Vocabulary

| Term | Meaning |
| --- | --- |
| **Run** | One campaign or ranked attempt: 16 fights, 5 strikes |
| **Round** | One build phase followed by one fight. Rounds are numbered 1–16 |
| **Interlude** | A campaign map node with no fight (Recruiter, Board Meeting, Consultant). Does not advance the round counter |
| **Quarter** | The fight. 60 seconds, 1,200 ticks, divided into Month 1, Month 2, Crunch and the Bell |
| **Budget** | Currency. Displayed as `¥` |
| **Goodwill** | Each firm's defensive buffer. Has a current value and a cap |
| **Market Share** | The shared bar, 0–100%. Starts at 50% |
| **Push** | Damage that depletes Goodwill, then overflows into Market Share |
| **Morale** | Damage that ignores Goodwill and erodes its cap. Produced by Burnout |
| **Anomaly** | Damage that ignores Goodwill and costs the attacker Goodwill. Produced by extraplanar staff |
| **Restore** | Direct recovery of Goodwill from an active ability |
| **Regen** | Passive recovery of Goodwill, every 2 seconds, suppressed by recent hits |
| **Tenure** | Rounds a room has been held while staffed. Grants tiers at 3 / 6 / 10 |
| **Strike** | A life. A lost fight costs one. Three ends the run |
| **Rider** | A visible permanent drawback attached to an extraplanar hire |
| **Snapshot** | A tower serialised for combat. Campaign rivals, ranked ghosts and test fixtures are all snapshots |
| **Founder** | The player's chosen avatar for a run: a portrait, a name, a title. Cosmetic in v1; carries an empty effects list reserved for later |

---

## 3. The core loop, beat by beat

One round, from arrival to departure. Every step names the screen it happens on.

**0. Found the firm.** *(Founder select screen, once per run.)* The player picks a
founder from eight portraits and names the firm. Both modes. The founder appears on
the build screen's firm panel, beside the Goodwill bar in every fight, in the run
history, and — for rivals — in the dossier. In v1 the choice changes nothing the sim
sees; the founder carries an empty effects list so that buildings, staff or abilities
can be attached to founders later without a format change (D-46).

**1. Arrive.** *(Map screen, campaign only.)* The player picks the next node from the
ones their current position connects to. Fight nodes begin a round; interludes do not.

**2. Income.** *(Build screen, top bar.)* Budget increases by the round's income plus
every Sales employee's passive, then decreases by floor upkeep. Upkeep that cannot be
paid is not carried as debt — it is paid in Goodwill instead, at 100 Goodwill cap per
unpaid `¥1`, for this round only. The top bar shows the arithmetic.

**3. Build.** *(Build screen.)* Untimed. The player hires from the shop, rerolls it,
buys rooms and furniture, places and moves everything, lays people off, demolishes,
leases new floors, and combines things into better things. Every action is undoable
until Ready. The tower on screen is always the tower that will fight.

**4. Ready.** *(Build screen → commit.)* The undo stack clears. Crafts that were
pending resolve in one animation as the quarter opens. Each room that existed at the
previous commit and is at least half-staffed now gains one round of Tenure. The tower
is snapshotted; that snapshot is what fights.

**5. Fight.** *(Battle screen.)* Sixty seconds, rendered from the simulation's output.
The player can change playback speed or skip to the result. They cannot intervene.

**6. Result.** *(Battle screen → result banner.)* Win or lose. A loss costs a strike.
The autopsy opens automatically on a loss and on the first fight of a run; otherwise it
is one button away.

**7. Reward.** *(Top bar.)* A win gives `¥3`, in both modes. Nothing else. A boss
win in Act 1 opens the portal. The campaign's extra opportunities come from interlude
nodes, never from winning fights — the player's economy is the same in campaign and
ranked, so a build that works in one works in the other.

**8. Depart.** Back to the map, or to the next round in ranked.

Time per round is roughly 60–120 seconds building and 35–60 seconds watching. A run is
about 45 minutes.

---

## 4. The building

Five floor slots, joined by an elevator on the left. A run begins with the ground floor
and 1F; the rest are leased.

| Floor | Grid | Tiles | Output | Room legality | Upkeep | Lease |
| --- | --- | --- | --- | --- | --- | --- |
| **3F — Executive** | 4 × 2 | 8 | × 1.45 | Executive, General | `¥4` / round | `¥36` |
| **2F — Operations** | 5 × 3 | 15 | × 1.15 | General | `¥2` / round | `¥28` |
| **1F — Operations** | 5 × 3 | 15 | × 1.00 | General | none | starting |
| **G — Reception** | 5 × 3 | 15 | × 0.90 | Reception, Security, General | none | starting |
| **B1 — Portal** | 3 × 3 | 9 | × 1.00 | Extraplanar only | −150 Goodwill cap | `¥20`, portal required |

Grids are drawn as top-down 32 × 32 tiles. Column 0 of every floor is the **landing
column**, adjacent to the elevator shaft; landing tiles count as adjacent to the
landing tiles directly above and below them. Nothing else is vertically adjacent.

**What makes floors different** is the combination, not any one column of that table:

- **G** is where reputation lives. Every employee inside the Reception room adds
  +100 to the Goodwill cap, which is why the weakest-output floor is worth staffing.
  It is also the floor every `lowest_floor` ability lands on.
- **1F and 2F** are the engine — the most tiles, the widest room legality, and the
  place a build's core usually sits.
- **3F** is the best multiplier in the building on the smallest grid, with the highest
  upkeep, and every `highest_floor` ability in the game aimed at it. Stacking the
  Executive floor is a legible, punishable choice, not a free optimum.
- **B1** cannot be targeted by floor selectors at all, but only extraplanar rooms may
  be built there and leasing it costs Goodwill rather than Budget. It is a specialist
  floor, never a safe one.

The starting tower: G with a fixed 2 × 2 **Reception** room in columns 1–2, rows 0–1,
which cannot be demolished; 1F empty; two **Junior Developers** on 1F; `¥12`.

---

## 5. The build phase

Everything the player does between fights, in the order they usually do it.

### 5.1 The shop

Three tabs — **Staff**, **Rooms**, **Furniture** — each showing four cards. Rerolling
a tab costs `¥1` and replaces its four cards. Each tab draws from a **bag**: the
eligible cards for the round, shuffled, drawn without replacement, refilled only when
empty (D-54). A reroll therefore never shows the same card twice until every eligible
card has been offered once, which is the genre's answer to "the shop keeps cycling the
same thing". Stock quality scales with round:

| Round | Staff tiers offered | Rooms | Furniture |
| --- | --- | --- | --- |
| 1–3 | T1 ×4 | small (1×2, 2×1) and 2×2 | common |
| 4–8 | T1 ×2, T2 ×2 | adds 2×3 | adds uncommon |
| 9–16 | T1, T2 ×2, T3 | all | all |

Rooms are offered only for floors the player owns. After the portal opens, the Staff
tab gains a second row, **Otherworld Temp Agency**, with two extraplanar cards and its
own `¥1` reroll.

A card shows its face, its name and its price (D-82). Everything the sim will use —
department, tier, cooldown, kind and value of the ability, targeting, passives, and — for
extraplanar staff — the rider — is in the inspector the moment the card is picked, before
it is placed. Nothing is hidden; it is one tap away.

### 5.2 Placement

Drag from the shop, or from anywhere in the tower, to a tile. Rules:

- An **employee** occupies one tile. It may stand on a tile inside a room, in the
  corridor (any tile not inside a room), or on a landing tile.
- **Furniture** occupies one or two tiles and must be inside a room.
- A **room** is a rectangle of tiles on one floor. Rooms may not overlap. A room may
  be placed over tiles already holding employees or furniture, which then count as
  inside it. Rooms may be legal only on certain floors, and some are illegal on the
  landing column.
- Nothing may be placed on the elevator.

The moment something is placed, the screen shows what it is getting: an aura badge
on every employee inside a room, a 1-pixel link from each piece of furniture to the
employees it triggers, and a tier marker on each room sign. A player never has to
read a tooltip to know whether a synergy is live.

### 5.3 Moving and removing

Moving an employee or furniture is free and unlimited. Moving a **room** is a
distinct, costly action — **Relocate** — separate from demolishing it.

- **Lay off** an employee: drag it out of the tower. Refunds nothing; costs a
  **severance fee** — `¥1` for T1, `¥2` for T2, `¥3` for T3, `¥4` for extraplanar.
  Some riders forbid it entirely.
- **Sell furniture**: refunds nothing, costs nothing.
- **Demolish** a room: refunds nothing; costs a **Renovation fee** equal to the
  current round's income; the room's Tenure is forfeited; its occupants become
  corridor occupants.
- **Relocate** a room: moves it, with its occupants and furniture, to a legal
  rectangle on any owned floor. Costs a **Relocation fee** equal to the current
  round's income. The room keeps its Tenure less three rounds — one tier's worth,
  floored at zero (D-51). The firm moved offices; most of what it knew came with it.
  This exists for the case the player could not have planned for: a room built in
  round 2, and a floor leased in round 7 that did not exist when the room was placed.
  There is no free version of it (D-52): the fee and the Tenure loss are the point.

### 5.4 Leasing

Floors are leased from the tower itself (D-83). An unleased floor draws greyed under a
screen, with a tag showing the price and the projected upkeep line before the player
commits; tapping the floor leases it and the screen lifts. Order is free — 3F may be
leased before 2F — but B1 requires the portal to be open, and its tag reads LOCKED until then.

### 5.5 Crafting

When placement completes a recipe pattern, the result is offered, not forced: the
inputs pulse and a **Promote** glyph appears over them. Accepting consumes the inputs
and places the result on the tile of the first input. Declining leaves everything as
it is. Both are undoable until Ready. Section 13 has the full system.

### 5.6 Undo and Ready

Every action pushes onto an undo stack. **Undo** is a single key and steps back one
action; there is no redo. **Ready** commits: the stack clears, pending crafts animate,
Tenure ticks, the snapshot is taken.

There is deliberately no confirmation dialog on Ready. The build phase is untimed and
undo is always available, so the moment of commitment should feel like a decision
rather than a checkbox.

---

## 6. The economy

Numbers below are initial values and belong to `BALANCE_PLAN.md`.

### 6.1 Income

| Round | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Income `¥` | 10 | 10 | 11 | 11 | 12 | 12 | 13 | 13 | 14 | 14 | 15 | 15 | 16 | 16 | 17 | 17 |

Formula: `9 + ceil(round / 2)`. Total over a run: `¥216`, plus `¥12` starting Budget,
plus `¥3` per fight won, plus Sales passives. There is no interest on savings: hoarding
is not a strategy, commitment is.

### 6.2 Prices

| Item | Price |
| --- | --- |
| Employee T1 / T2 / T3 | `¥3` / `¥6` / `¥10` |
| Extraplanar employee | `¥5` + rider |
| Room 1×2 or 2×1 / 2×2 / 2×3 | `¥5` / `¥9` / `¥13` |
| Furniture common / uncommon | `¥2` / `¥4` |
| Reroll (any tab) | `¥1` |
| Lease 2F / 3F / B1 | `¥28` / `¥36` / `¥20` |
| Renovation fee (demolish a room) | current round's income |
| Relocation fee (move a room to another floor) | current round's income, and −3 Tenure rounds |
| Severance T1 / T2 / T3 / extraplanar | `¥1` / `¥2` / `¥3` / `¥4` |

### 6.3 What the numbers are for

A 2F lease costs about two and a half rounds of income; a 2×2 room costs most of one.
By round 6 a player has seen roughly `¥78`. The intended shape: two or three rooms and
six to eight staff by the first boss, one leased floor, and a real question about
whether the second lease is worth the fights it weakens.

The economy is deliberately tight enough that a full-floor rebuild is never routine.
`BALANCE_PLAN` asserts it: the median number of rooms demolished per simulated run
must stay below a stated threshold, or the room-commitment tension has silently
collapsed (D-24).

### 6.4 Goodwill, per round

| Round | 1 | 4 | 8 | 12 | 16 |
| --- | --- | --- | --- | --- | --- |
| Base Goodwill cap | 700 | 1,000 | 1,400 | 1,800 | 2,200 |

Formula: `600 + 100 × round`. Passives add to it; portal taxes subtract. Base regen per
2-second event is 3% of cap. A hit must be at least 4% of cap to suppress regen.

---

## 7. Rooms

A room is a **zone** drawn over tiles. It does not consume them. Employees and
furniture inside a room gain its aura; the room's Tenure raises that aura over time.

### 7.1 Tenure

| Tier | Rounds held | Aura change | Sign |
| --- | --- | --- | --- |
| — | 0–2 | none | *Newly Fitted* |
| **I** | 3 | +10 points on every multiplier the room grants | *Established* |
| **II** | 6 | +10 more | *Departmental* |
| **III** | 10 | +10 more, and the room's Tier III clause comes online | *Institutional* |

A round counts as held if the room existed at the previous commit and at least half its
tiles hold employees at this commit. Rounds are counted, not elapsed: a room bought in
round 9 can still reach Tier I. Tenure is forfeited on demolition, reduced by three
rounds on relocation, and is part of the tower snapshot.

The arithmetic is deliberately kind to staying put. An Open Plan Office at Tier II on
1F grants ×1.40; a new one on 2F grants ×1.20 × 1.15 = ×1.38, after a `¥28` lease and
`¥2` a round. Relocating is for when the building grew around a room, not a routine
upgrade — the inspector shows both numbers so the player can see which it is.

Multipliers are written as percentages here and as permille in the sim: a room granting
× 1.20 grants 1,200; Tier I makes it 1,300.

### 7.2 Catalogue — Phase 2 set

Enough rooms for a real run. The authoritative catalogue is `content/rooms.json`
(sixteen rooms as of Phase 3, described in `CONTENT_SCHEMA.md`); this table is the
Phase 2 subset that the design was written against, kept here so the reasoning reads
in one place.

| Room | Size | Floors | Cost | Aura on occupants | Tier III clause |
| --- | --- | --- | --- | --- | --- |
| **Reception** | 2×2 | G (fixed, starting) | — | Each occupant: +100 Goodwill cap. Push × 0.80 | Occupants also grant +30 regen per event |
| **Open Plan Office** | 2×3 | 1F, 2F | `¥13` | Engineering push × 1.20 | Every occupant: cooldown × 0.90 |
| **Server Room** | 2×2 | 1F, 2F | `¥9` | Engineering push × 1.35. At the Month 2 and Crunch banners every occupant gains 1 Burnout (it is hot in there) | Banner Burnout no longer applies |
| **Legal Department** | 2×2 | 1F, 2F, 3F | `¥9` | Legal passives × 1.50. Bureaucracy applied by occupants: +1 stack | Occupants' Goodwill contributions cannot be eroded by Morale |
| **Sales Floor** | 2×2 | 1F, 2F | `¥9` | Sales push × 1.20. Sales income passive +1 each | Sales occupants' abilities also apply 1 Bureaucracy |
| **Break Room** | 1×2 | G, 1F, 2F, 3F | `¥5` | Non-HR occupants: push × 0.50, immune to Burnout. HR occupants: restore × 1.50 | Occupants cleanse 1 Burnout from every adjacent employee every 10s |
| **Security Desk** | 1×2 | G only, not landing | `¥5` | Occupants cannot be selected by enemy employee selectors | Occupants also cannot receive Bureaucracy |
| **Boardroom** | 2×2 | 3F only | `¥9` | All push × 1.20. Management retriggers reach the whole floor, not only adjacent tiles | Every Management fire is also a Standup: 1 Overtime to adjacent |
| **Corner Office** | 1×2 | 3F only | `¥5` | Single occupant only: push × 1.60, Goodwill cap −100 | Cap penalty removed |
| **Summoning Circle** | 2×2 | B1 only | `¥9` | Extraplanar occupants: Anomaly self-cost halved. Required by all Ritual recipes | Occupants' Anomaly also applies 1 Burnout to its target |

**Corridor** (no room): occupants get no aura and push × 0.90. Standing in the corridor
is always legal and never good.

### 7.3 Room specification

Every room entry in content declares, and the manifest mirrors:

| Field | Meaning |
| --- | --- |
| `footprint` | `{ w, h }` in tiles |
| `floors` | Legal floor kinds |
| `landingLegal` | May the rectangle include column 0 |
| `aura` | List of `{ filter, stat, permille }` — e.g. `{ dept: "engineering", push: 1200 }` |
| `flat` | List of flat additions — e.g. `{ goodwillCap: +100 per occupant }` |
| `tierIII` | The unique clause, by effect id |
| `banners` | Effects fired at month transitions, if any |

Visually a room is its floor tiles (32 × 32 each, tileable, one variant per room type)
with a 1-pixel wall on the top edge and a **sign** — a 32 × 8 label strip anchored at
the top-left tile — carrying the name and Tenure pips. Rooms are the ground layer:
`sortBias` −10.

---

## 8. Furniture

Furniture occupies tiles inside rooms and does things to the employees **orthogonally
adjacent** to it. A tile holding furniture is a tile not holding an employee; that is
the cost.

**Furniture is in v1 on trial** (D-34). Equipment is cut for good; furniture stays
because it is the cheapest source of tile scarcity in the design, and it is kept only
until the vertical slice can answer whether it earns its layer. The test is named in
§21. If it fails, every furniture effect folds into a room's aura or Tier III clause,
and recipes take rooms as their third input instead. Nothing in the sim changes either
way — furniture is a set of flat bonuses and periodic events, and both survive the
fold.

| Furniture | Size | Cost | Effect on adjacent employees | Notes |
| --- | --- | --- | --- | --- |
| **Whiteboard** | 1×1, wall | `¥2` | Engineering: +15 flat push | Recipe input. `sortBias` −5 |
| **90s PC** | 1×1 | `¥2` | Engineering: cooldown × 0.90 | |
| **Filing Cabinet** | 1×1 | `¥2` | Legal: +60 Goodwill cap each | Recipe input |
| **Fax Machine** | 1×1 | `¥4` | Every 6s, the adjacent Legal employee with the highest base value fires immediately | A retrigger source that is not a person |
| **Water Cooler** | 1×1 | `¥2` | Every 10s, remove 1 Burnout from each | Recipe input |
| **Yakult Cart** | 1×1 | `¥4` | At the Quarter Open banner: 1 Overtime each | Haste with a hangover |
| **Monitoring Station** | 1×2 | `¥4` | Their floor-targeting abilities use `most_populated_floor` instead of their default | Targeting override |
| **Executive Desk** | 1×2 | `¥4` | Management: retrigger value × 1.25 | 3F only |
| **Ofuda** | 1×1, wall | `¥2` | Extraplanar: Anomaly self-cost −50% | B1 only. Stacks with Summoning Circle to zero |

Furniture has no Tenure. Selling it refunds nothing and costs nothing.

Wall-mounted furniture (Whiteboard, Ofuda) has a 1×1 footprint and a 32 × 40 sprite —
it overhangs the tile behind it by 8 pixels — with `sortBias` −5 so it draws behind
anything standing in front. This is the first place the footprint-versus-bounds rule
bites, and it is in the Phase 2 catalogue on purpose.

---

## 9. Employees

An employee is one tile, one department, one tier, one ability, and possibly one
passive. Nothing is hidden: the inspector shows all of it the moment the card is picked (D-82).

### 9.1 Departments

| Department | Verb | Identity in the fight |
| --- | --- | --- |
| **Engineering** | pushes | The main Push source. Wants rooms and furniture |
| **Legal** | resists | Raises Goodwill cap and regen; applies Bureaucracy and Frozen |
| **HR** | restores | Active Goodwill restore; cleanses Burnout |
| **Sales** | grows | Chip Push in the fight; Budget income between fights; the only enemy-side Burnout source outside Management |
| **Management** | retriggers | Makes other people fire. Never fires anything itself |
| **Extraplanar** | pierces | Anomaly damage with a self-cost; always comes with a rider |

### 9.2 Roster — Phase 2 set

The authoritative roster is `content/employees.json` (forty as of Phase 3). This is
the Phase 2 subset the design was written against. Cooldowns in seconds; the sim uses
ticks (× 20). `Value` is the base before any
multiplier. Targeting is written `floor / employee`; see `SIMULATION_SPEC.md` §6 for
the selector vocabulary.

**Engineering**

| Name | Tier | Cooldown | Ability | Passive |
| --- | --- | --- | --- | --- |
| Junior Developer | 1 | 4.0 | **Ship Feature** — 60 Push | — |
| QA Tester | 1 | 2.0 | **Bug Report** — 25 Push | — |
| Senior Developer | 2 | 4.0 | **Ship Feature** — 150 Push | During Crunch, cooldown 3.0 |
| DevOps | 2 | 6.0 | **Deploy** — 90 Push; then 1 Overtime to each adjacent Engineering | — |
| Architect | 3 | 8.0 | **Refactor** — 400 Push | Every third Refactor also grants 1 Overtime to every adjacent Engineering |

**Legal**

| Name | Tier | Cooldown | Ability | Passive |
| --- | --- | --- | --- | --- |
| Paralegal | 1 | 3.0 | **Paperwork** — 1 Bureaucracy to `most_populated / lowest_cooldown_remaining` | +100 Goodwill cap |
| Counsel | 2 | 6.0 | **Cease & Desist** — 90 Push, then 2 Bureaucracy to `highest_occupied / highest_base_value` | +250 cap, +40 regen per event |
| General Counsel | 3 | 10.0 | **Injunction** — Frozen 3.0s on `highest_occupied / highest_base_value` | +500 cap, +80 regen per event |

**HR**

| Name | Tier | Cooldown | Ability | Passive |
| --- | --- | --- | --- | --- |
| Recruiter | 1 | 5.0 | **Team Building** — Restore 80 | +30 regen per event |
| HR Manager | 2 | 6.0 | **Wellness Program** — Restore 150; remove 2 Burnout from every own employee on this floor | — |
| Head of People | 3 | 8.0 | **Retention Bonus** — Restore 300 | Own employees' Burnout maximum is 3 instead of 5 |

**Sales**

| Name | Tier | Cooldown | Ability | Passive |
| --- | --- | --- | --- | --- |
| Sales Rep | 1 | 3.0 | **Cold Call** — 40 Push | +`¥1` income per round |
| Account Manager | 2 | 5.0 | **Close Deal** — 110 Push | +`¥2` income per round |
| Headhunter | 2 | 7.0 | **Poach** — 2 Burnout to `highest_occupied / highest_base_value` | +`¥1` income per round |
| Sales Director | 3 | 12.0 | **Quarterly Target** — Push = 300 + 50 per Sales employee in the tower | +`¥3` income per round |

**Management**

| Name | Tier | Cooldown | Ability | Passive |
| --- | --- | --- | --- | --- |
| Team Lead | 1 | 6.0 | **Delegate** — the adjacent employee with the highest base value fires now | — |
| Middle Manager | 2 | 8.0 | **Standup** — 1 Overtime to every adjacent employee | — |
| Consultant | 2 | 9.0 | **Efficiency Review** — 1 Burnout to every enemy employee on `most_populated / all` | — |
| Director | 3 | 10.0 | **Reorg** — every adjacent employee fires now, then gains 1 Burnout | — |

"Adjacent" for Management includes the landing column's vertical adjacency. This is
where Middle Management lives in the tower, and it is why the landing column matters.

**Extraplanar** — see §14.

### 9.3 What the roster is shaped to do

- **Engineering** wants a room and furniture around it: an Architect in a Tier II
  Server Room next to a Whiteboard and a 90s PC is the game's Push ceiling.
- **Legal** stacks cap and regen so that Push alone cannot get through in time —
  and then loses to a Headhunter and a Consultant, because Morale does not care about
  the cap.
- **HR** keeps a Burnout-heavy own build alive: a Director's Reorg burns people out;
  an HR Manager on the same floor un-burns them.
- **Sales** is the economy engine that also happens to be the counter-turtle piece.
  A Sales Floor with three reps is `¥3`/round and enough chip to keep regen suppressed.
- **Management** is nothing on its own and multiplies everything around it. A Team Lead
  next to an Architect is a second Architect for `¥3`.

Depth comes from combination, not quantity: this is twenty-one employees, and the
intended late tower fields twelve to sixteen of them.

### 9.4 Employee specification

| Field | Meaning |
| --- | --- |
| `dept`, `tier`, `cost` | As above |
| `cooldownTicks` | Cooldown in ticks (seconds × 20) |
| `initialProgressPermille` | Charge at Quarter Open; 0 by default |
| `ability` | `{ kind, value, targeting, effects[] }` |
| `passives[]` | Match-start contributions: `goodwillCap`, `regenPerEvent`, `income` |
| `tags[]` | For recipes and rider filters |
| `attachments[]` | Always empty in v1 (D-13) |

Sprites: 32 × 32, anchor bottom-centre, footprint 1×1, `sortBias` 0. The build view
shows the down-facing idle frame; the battle view never shows employees directly.

---

## 10. Status effects

Four, all on employees. Firm-level state (cap, regen, suppression) is in the sim spec.

| Status | Stacks | Per stack | Expiry | Removed by |
| --- | --- | --- | --- | --- |
| **Burnout** | max 5 | Owner's firm takes `8 × stacks` Morale every second; owner's push × (1 − 0.05 × stacks) | Never, within a fight. Reset at round end | Water Cooler, HR abilities, Break Room Tier III |
| **Overtime** | max 2 | Cooldown rate +50% | 3.0s per stack; on expiry the owner gains 1 Burnout | Expiry |
| **Bureaucracy** | max 3 | Cooldown rate −20% | 5.0s per stack, each stack independently | Expiry |
| **Frozen** | single | Cooldown does not advance | Duration; re-application extends | Expiry |

Overtime is haste at a cost, and the cost is the game's satire in one rule: the sprint
always ends, and the person who sprinted is worse afterwards. Bureaucracy is the Legal
department's whole personality. Burnout is what a turtle cannot see coming, because the
balance sheet does not have a line for it.

---

## 11. The fight

What the player watches. The rules live in `SIMULATION_SPEC.md`; this section is what
they feel like.

### 11.1 The quarter

| Phase | Time | Push | Regen | On screen |
| --- | --- | --- | --- | --- |
| **Month 1** | 0–20s | × 1.0 | × 1.0 | `— Q OPEN —` banner. Yakult Cart fires |
| **Month 2** | 20–40s | × 1.4 | × 0.6 | `— MONTH 2 —`. Server Rooms burn |
| **Crunch** | 40–58s | × 2.0 | × 0.2 | `— CRUNCH —`. Senior Devs accelerate. Server Rooms burn again |
| **The Bell** | 58–60s | × 3.0 | × 0 | `— QUARTER CLOSE —`. Nothing regenerates |

Each transition is telegraphed one second early by the banner sliding in dimmed, then
lighting up on the tick. A build whose combo comes online at 39.9s should see it coming.

### 11.2 What happens to a hit

1. An employee's cooldown fills. It fires.
2. Its base value is adjusted by furniture, then multiplied by its room's aura
   (including Tenure), its floor, its statuses, and the month. One integer comes out.
3. **Push** hits the rival's Goodwill. If it breaks Goodwill, the excess carries into
   Market Share in full. A hit of at least 4% of the rival's cap suppresses their regen
   for one second.
4. **Morale** skips Goodwill: it lowers the rival's Goodwill cap by the raw amount and
   moves Market Share at half rate.
5. **Anomaly** skips Goodwill and moves Market Share at full rate, and the attacker's
   own Goodwill takes a quarter of the raw amount.
6. **Restore** raises the caster's own Goodwill, up to cap. It is not suppressed.
7. A ledger entry is written for every one of these.

Every two seconds, each firm that has not taken a suppressing hit in the last second
writes a **regen** entry to its ledger. Every second, each firm whose employees hold
Burnout writes a **Morale** entry to the rival's.

### 11.3 How it ends

Market Share reaches 0% or 100%, or the Bell rings. At the Bell, the leader wins. At
exactly 50%, the firm with more Goodwill remaining wins; if equal, the firm that dealt
more total Push; if still equal, a **draw** — in campaign it counts as a loss with no
strike ("no growth this quarter"); in ranked, no rating change.

### 11.4 What it should feel like

Month 1 is Goodwill being chipped and regenerating — the ledger is where the drama is,
and the bar barely moves unless someone brought Burnout. Month 2 is the break: one
side's Goodwill hits zero and the bar starts to travel. Crunch is the swing. The Bell
is two seconds of triple damage with no defence, and it can erase a lead.

That last fact is deliberate (D-23). If it feels bad in playtest, the fix is a cap on
Bell-window multipliers, never a ratchet.

---

## 12. The ledger and the autopsy

One component. Live during the fight, scrubbable after it. The complete entry format is
`SIMULATION_SPEC.md` §11; this section is what the player sees.

### 12.1 Live

Each firm has a ledger panel beneath its Goodwill number: six visible lines, newest at
the bottom, scrolling up. A line:

```
 -1,200  Cease & Desist    Counsel · Fl.2   ×2
```

Amount, name, source, and a multiplier badge if entries were coalesced. Colour by
kind: Push in the firm's colour, Morale in the anomalous tone, Regen and Restore in
the support tone, Status in slate, banners full-width.

Three rules keep it readable at combat speed:

1. Entries with the same source, ability and kind within 1.0 second merge into one
   line with a `×N` badge.
2. At most 4 new lines per second. Anything beyond collapses into one dimmed roll-up
   line — `+3 more · Fl.2` — which expands in the autopsy.
3. Lines are weighted by magnitude relative to the firm's current Goodwill. The hit
   that mattered is the one that catches the eye.

### 12.2 The autopsy

Opens automatically on every loss and on the first fight of every run. Shows:

- **A timeline** of Market Share over the sixty seconds with the month boundaries
  marked and a playhead the player can drag. Dragging scrolls the ledger.
- **Per-floor contribution**: for each floor of each tower, total Push, Morale and
  Anomaly dealt — a horizontal bar per floor, both towers side by side.
- **The full ledger**, no coalescing, filterable by side, floor, employee and kind.
- **Three findings**, generated from the entry list: when Goodwill broke and what
  broke it; the largest single hit of the fight; how many seconds regen was
  suppressed. These are the answer to "why did I lose" for a player who does not want
  to read the whole list.

The autopsy is reachable from the run history for every fight of the current run.

---

## 13. Recipes and the codex

### 13.1 The system

A recipe is a pattern: a set of inputs (employees and furniture, by tag) plus an
optional room context, that produces one result. When placement completes a pattern:

1. The inputs pulse and a **Promote** glyph appears.
2. Accepting consumes the inputs and places the result on the first input's tile.
   Furniture inputs are consumed unless the recipe says otherwise.
3. Declining leaves everything in place. The glyph stays while the pattern holds.

Crafting costs no Budget — the inputs are the cost. It is undoable until Ready and
permanent afterwards. It never happens during a fight.

**Near-miss feedback.** A pattern missing exactly one input, or complete but in the
wrong room context, produces a distinct signal — the inputs flicker once and a `?`
glyph appears for a moment. It reveals that *a* recipe is close, not which. This is
what turns guessing into deduction.

### 13.2 Classes

| Class | Inputs | Available | Launch count |
| --- | --- | --- | --- |
| **Promotions** | employee + employee, often + furniture or room | Round 1 | ~20 |
| **Renovations** | furniture + furniture, or furniture in a specific room | Round 1 | ~12 |
| **Rituals** | anything + extraplanar, in a Summoning Circle | After the portal | ~8 |

### 13.3 Phase 2 recipes

Enough to prove the system. The full forty are in `content/recipes.json`.

| Inputs | Context | Result | Notes |
| --- | --- | --- | --- |
| Junior Developer + Junior Developer, adjacent Whiteboard | — | Senior Developer | Whiteboard is kept |
| Senior Developer + Senior Developer | Server Room | Architect | |
| Paralegal + Paralegal, adjacent Filing Cabinet | — | Counsel | Cabinet is kept |
| Counsel + Counsel | Legal Department | General Counsel | |
| Sales Rep + Sales Rep | Sales Floor | Account Manager | |
| Sales Rep + Paralegal | — | Headhunter | Cross-department |
| Recruiter + Water Cooler | Break Room | HR Manager | Cooler is consumed |
| Team Lead + any Tier 2 | Boardroom | Director | The Tier 2 is consumed. The satire is intentional |
| Whiteboard + Whiteboard | — | Monitoring Station | Renovation |
| Water Cooler + Yakult Cart | Break Room | Break Room gains Tier III clause early | Renovation that upgrades the room |
| Junior Developer + Salaryman Ghost | Summoning Circle | Salaryman Who Never Left | Ritual — see §14 |

### 13.4 The codex

Present from the first run. Every recipe has a slot; undiscovered ones show as an
outline with their class. The *number* of recipes is public from the start. An empty
codex with visible holes reads as depth; a hidden one reads as absence.

Progressive reveal:

- Hold any input of an undiscovered recipe: the slot shows its input count.
- Trigger a near-miss: the slot shows the inputs you had.
- Discover it: the slot fills. It stays filled in every future run.
- Visit a **Consultant** node: pick one outlined slot and reveal it outright.
- **Compatibility glow.** Hovering any card in the shop, or any placed employee or
  furniture, softly highlights the owned items it shares an undiscovered recipe with.
  It says *these two go together*, not what they make — Backpack Battles' hint line,
  which is the disclosure step players there ask for most.

The codex is a screen (§19.6) and also a hover on any card: a card shows the count of
recipes it participates in, discovered and not.

---

## 14. The portal

### 14.1 Opening it

The portal opens when the run's `portalUnlock` condition is met — in campaign, the
Act 1 boss falling (round 6); in ranked, the start of round 5. The condition is
configuration; the rule is the same in both modes.

Opening it does three things: B1 becomes leasable, the Staff tab gains the Otherworld
Temp Agency row, and the elevator panel's `B1` button lights.

### 14.2 The reveal

Nothing supernatural is mechanical before round 6, and everything supernatural is
administrative. From round 1, in the background: the lift panel has a `B1` button that
is not lit. A fax arrives with no sender. The Reception plant is dead one round and fine
the next. The applicant pool includes, once, a card whose portrait is a chair.

Then the Regional Rival falls, and the button lights, and the next shop has a row
headed **OTHERWORLD TEMP AGENCY — contractors available**. The Agency does not haunt
you. It invoices you.

### 14.3 The risk

Every extraplanar hire is strong and comes with two costs the player can see before
paying (recommended in Q-PTL-1, awaiting sign-off; built on here):

- **A rider**, rolled from the pool when the card is generated and shown in the
  inspector when the card is picked (D-82). Accepting the hire accepts the rider.
  Permanent for the run.
- **A Goodwill tax**: each extraplanar employee lowers the firm's Goodwill cap by 100.
  Leasing B1 lowers it by a further 150. A portal build is inherently a glass cannon.

Rider pool — Phase 2 set of eight; capped at twelve at launch:

| Rider | Effect |
| --- | --- |
| *Tenured* | Cannot be laid off |
| *Union Dispute* | Every reroll costs `¥1` more while employed |
| *Bad Influence* | At Quarter Open, adjacent employees gain 1 Burnout |
| *Executive Aversion* | 3F output × 0.80 while employed |
| *Poor Reception* | Reception grants no Goodwill cap while employed |
| *Overhead* | Upkeep +`¥1` per round |
| *Hungry* | On hire, consumes one adjacent piece of furniture |
| *Contractual Obligation* | At the Bell, the firm takes 200 Morale |

`BALANCE_PLAN` asserts that no rider is net-positive and that no extraplanar employee
beats its Tier equivalent at equal cost once the tax is counted.

### 14.4 Extraplanar roster — Phase 2 set

| Name | Cooldown | Ability | Passive |
| --- | --- | --- | --- |
| **Salaryman Ghost** | 3.0 | **Overtime Eternal** — 90 Anomaly | Permanently holds 2 Overtime that never expire and never cause Burnout |
| **Office Lady of the Third Floor** | 6.0 | **Filing** — 60 Anomaly, then 1 Bureaucracy to `same_floor_index / lowest_cooldown_remaining` | Must be placed on 3F |
| **The Auditor** | 12.0 | **Audit** — Anomaly equal to 15% of the rival's Goodwill cap | Cannot be retriggered |
| **Salaryman Who Never Left** (Ritual result) | 4.0 | **Loyalty** — 200 Anomaly | Immune to Bureaucracy and Frozen. Counts as Engineering for rooms |

### 14.5 Anomaly

Anomaly ignores Goodwill and moves Market Share at full rate. In return the attacker's
own Goodwill takes 25% of the raw amount as a hit — and that hit suppresses their own
regen like any other. A Summoning Circle halves the self-cost; an Ofuda halves it
again, to zero. Getting the self-cost to zero is a three-tile, one-room, one-floor
investment on a floor that costs Goodwill to lease. That is the whole gamble.

---

## 15. Campaign mode

### 15.1 Shape

Three acts on a branching map. Each act is a fixed number of columns; the player picks
one node per column from the ones their current node connects to.

| Act | Columns | Fights | Interludes | Rounds |
| --- | --- | --- | --- | --- |
| **1** | `F F X F F X F B` | 5 + boss | 2 | 1–6 |
| **2** | `F F X F F X F B` | 5 + boss | 2 | 7–12 |
| **3** | `F X F X F B` | 3 + boss | 2 | 13–16 |

`F` columns hold Hostile Takeover nodes, one of which per column may be an **Audit**
instead. `X` columns hold interludes only. `B` is the boss. Sixteen fights, six
interludes, every run. The column strings are the content (`content/map.json`), and
the loader asserts that each act's `F` and `B` count equals its round span.

| Node | What happens |
| --- | --- |
| **Hostile Takeover** | A fight against a rival from this round's pool (§15.5) |
| **Audit** | A harder fight — the rival is drawn from the pool two rounds ahead and always carries a gimmick. Win bonus `¥5` instead of `¥3` |
| **Recruiter** | Build phase with an upgraded shop: six cards per tab, first reroll free. No fight |
| **Board Meeting** | A choice of two or three run modifiers with a cost and a benefit each. No fight |
| **Consultant** | Reveal one codex recipe, or grant one room +1 Tenure round. No fight |
| **Boss** | The act's boss tower, with its signature mechanic. Win bonus `¥5`; Act 1 opens the portal |

### 15.2 Bosses

Each attacks an assumption rather than carrying bigger numbers.

**Act 1 — The Regional Rival.** A competent generalist: an Open Plan Office of
Engineering, a Paralegal, a Sales Rep, no tricks. It exists to prove the ledger is
readable — a player who beats it should be able to say, from the autopsy, which of
their employees did the work. Its defeat opens the portal.

**Act 2 — The Compliance Office.** A Legal Department at Tier II with two Counsel and a
General Counsel, a Reception full of Paralegals, and a regen rate that Push alone
cannot beat before the Bell. It is unwinnable without Morale or Anomaly. This is the
fight that teaches Burnout.

**Act 3 — The Parent Company.** Five floors staffed, a Consultant and a Headhunter, a
Director in a Boardroom, and floor-targeting on every ability that has a target. It
punishes concentration: a tower that stacked 3F loses it; a tower that left G empty
loses its Reception. The run's thesis statement.

Bosses are authored snapshots. They are also balance fixtures, and they are re-authored
whenever the pressure curve or the base Goodwill table moves.

### 15.3 Strikes

Losing any fight — boss included — costs one strike and the map continues. A run ends
at five strikes (D-58), or after fight 16. A run that reaches fight 16 with strikes remaining
is a win; its score is strikes remaining and total Market Share claimed.

### 15.4 Board Meeting modifiers — Phase 2 set

| Modifier | Cost | Benefit |
| --- | --- | --- |
| *Overtime Culture* | Every employee starts each fight with 1 Burnout | Every employee starts with 1 Overtime |
| *Lean* | Goodwill cap −200 | Income +`¥2` per round |
| *Family Firm* | Severance doubled | All rooms gain +1 Tenure round now |
| *Compliance Review* | Reroll costs `¥2` | Bureaucracy applied by own staff +1 stack |

Modifiers are snapshot globals. They ride into the fight in `globals.modifiers`.

### 15.5 Rivals: scripted, semi-scripted, and their gimmicks

The campaign's whole difference from ranked is who you fight (D-36). The player's
rules, shop, economy and building are identical; the opponent is not a stored player
but a tower built to teach or test something. Three kinds:

| Kind | Authored how | Used for |
| --- | --- | --- |
| **Scripted** | Hand-authored snapshot, checked in | The three bosses; the first six tutorial rivals |
| **Semi-scripted** | A **rival template** — an archetype, a round, and a seeded variation — expanded into a snapshot at run start and validated for constructibility | Every ordinary Hostile Takeover and Audit |
| **Ghost** | A captured player snapshot | Ranked only |

A rival template names an archetype (`turtle`, `burst`, `economy`, `burnout`,
`management`, `generalist`), a round, a budget envelope, and a seed; the expander
picks rooms and staff from the archetype's shopping list within the envelope, places
them by the archetype's layout rules, and assigns Tenure consistent with the round.
The result is a snapshot like any other. Sixteen rounds times several rivals each is
too many towers to hand-author and keep balanced, and templates are also what the
balance harness runs, so the rival pool and the test fixtures are the same thing.

**Gimmicks.** A rival may carry a **rival-only modifier** — a buff or a mechanic the
player can never have — in `globals.modifiers`, the same field Board Meeting
modifiers use. The sim does not know the difference; the content database marks which
modifiers are rival-only. Every Audit carries one; every boss carries its signature
one; ordinary rivals carry one from round 8 onward. Phase 2 set:

| Gimmick | Effect | Teaches |
| --- | --- | --- |
| *Deep Pockets* | Goodwill cap × 1.5 | Push alone is slow; bring Burnout |
| *Franchise* | Every floor counts as `most_populated_floor` | Floor-selected statuses land everywhere |
| *Old Money* | All rooms at Tier III | What Tenure looks like fully grown |
| *Night Shift* | All staff hold 1 permanent Overtime; no Burnout on expiry | Haste without the cost, and how to slow it |
| *Regulatory Capture* (Compliance Office) | Regen is never suppressed | The Act 2 boss's signature; unwinnable without Morale or Anomaly |
| *Conglomerate* (Parent Company) | Every status ability is floor-selected, and `highest_occupied_floor` also hits `lowest_occupied_floor` | The Act 3 boss's signature; concentration is punished twice |
| *Mirror* (Regional Rival) | None — the tower is a competent copy of the player's own archetype at this round | The Act 1 boss's signature is having no gimmick; the ledger is the lesson |

A gimmick is always shown before the fight. The map node's hover opens a **dossier**
(§19.5) with the rival's name, archetype, floor count and gimmick, so the player is
building against something they can see.

---

## 16. Ranked mode and the configuration layer

Ranked is the same game with a different configuration. There is one sim and one
content database; the following table is the *entire* difference.

| Setting | Campaign | Ranked |
| --- | --- | --- |
| `map` | branching, three acts | none — sixteen rounds in sequence |
| `interludes` | on | off |
| `rivalSource` | scripted bosses and tutorial rivals; semi-scripted templates otherwise | ghost pool by round and rating band; templates as fallback |
| `rivalGimmicks` | on | off |
| `portalUnlock` | Act 1 boss defeated | round ≥ 5 |
| `winBonus` | `¥3`; Audit and boss `¥5` | `¥3` |
| `strikes` | 3 | 3 |
| `metaUnlocks` | apply | everything available |
| `rating` | none | Elo-style, per fight |
| `snapshotCapture` | never | after every Ready, into the ghost pool |
| `founderSelect` | at run start | at run start |

Anything not in that table is identical by construction. A rule that would need a
second row in the sim is a rule that does not get built.

Ranked ships after campaign. Nothing in campaign v1 builds the ghost pool, matchmaking
or the server re-simulation — but every campaign rival is a snapshot in the ghost
format, and `simulate()` is pure, so adding ranked is additive (D-18 to D-20).

---

## 17. Progression and unlocks

Meta-progression never touches the sim. It changes what is *offered*, not how anything
fights.

| Unlock | Trigger | Grants |
| --- | --- | --- |
| **Codex** | Per recipe discovered | Recipe stays revealed in all future runs |
| **Roster tier 2** | 8 total fights won | Adds DevOps, Headhunter, Consultant to the shop pool |
| **Roster tier 3** | 20 total fights won | Adds Architect, Head of People, Sales Director, Director |
| **Rooms tier 2** | First Act 2 reached | Adds Boardroom, Corner Office, Security Desk |
| **Board Meeting pool** | First run completed | Adds three further modifiers |
| **Titles** | Various | Cosmetic firm name suffixes on the battle screen |

Ranked ignores all of it — everything is available — so the ladder is never a
question of who has unlocked more.

---

## 18. The new-player experience

The first campaign run is the tutorial. It is scripted only in what the shop offers
and who the first rivals are; the rules are never simplified, and there is no separate
tutorial mode.

| Round | Rival | What the player learns | How |
| --- | --- | --- | --- |
| 1 | *Two-Desk Startup* — two Junior Devs, no rooms | Place, Ready, watch, read the ledger | The autopsy opens after the fight regardless of result and highlights one entry: the largest hit |
| 2 | *Copy Shop* | The shop and rerolling | Shop starts with an obviously good card; the hint line names the reroll key |
| 3 | *Cram School* | Rooms are auras | The shop guarantees an Open Plan Office; placing an employee inside shows the badge |
| 4 | *Print Works* | Furniture and near-miss | The shop guarantees a Whiteboard; two Junior Devs beside it trigger the first Promote |
| 5 | *Bento Chain* | Severance is real | The player has more staff than good tiles; laying one off shows the fee |
| 6 | **The Regional Rival** | The ledger is the answer | Boss. The portal opens |
| 7 | *Compliance-adjacent* | Legal exists and it stalls you | First rival with a Counsel |
| 8–12 | — | Burnout | The Act 2 boss is unwinnable without it; the Consultant interlude in Act 2 always offers a Morale recipe |

A hint line at the bottom of the build screen shows one sentence per round for the
first run only. It is never modal.

---

## 19. Screen specifications

All screens are laid out on a **640 × 360 logical canvas**, rendered at an integer
scale: 2× on 1280 × 720, 3× on 1920 × 1080, 6× on 3840 × 2160, letterboxed otherwise.
Coordinates are `(x, y, w, h)` in logical pixels from the top-left; anchors are
top-left unless stated. Every region below is a manifest entry of kind `ui`; every
entity is a manifest entry of its own kind. No pixel size in this section may be
hardcoded anywhere — the manifest reads these values, code reads the manifest.

Fonts: `font.ui.8` is an 8-pixel-line pixel font with variable-width glyphs averaging
5 pixels; `font.ui.16` is the same face at exactly 2×. Numbers use tabular glyphs.

### 19.1 Build screen

| Region | Rect | Contents |
| --- | --- | --- |
| `ui.build.topbar` | (0, 0, 640, 24) | Round `Q3 · FIGHT 7/16` at (8, 8); Budget `¥ 24` at (200, 8); upkeep `−¥4/qtr` at (280, 8); five strike icons 8×8 from (400, 8); **READY** button (552, 4, 80, 16) |
| `ui.build.ready_shop` | (192, 312, 232, 20) | A second **READY** at the foot of the shop in `font.ui.16`, where the thumb already is on touch (D-68); the top-right one stays for keyboard and mouse |
| `ui.build.tower` | (8, 32, 176, 304) | Elevator shaft (8, 32, 16, 304) with floor labels drawn inside it; three floor viewports stacked: above at y=32, **selected** at y=136, below at y=240, each 160 × 96 at x=24. Unselected floors dimmed 50%, still interactive. Scrolls by whole floors. An unleased floor draws greyed under a screen with a lease tag (`ui.build.lease_button`, laid out by `game/scenes/widgets/LeaseTag.tscn`) showing the price and upkeep; the whole floor is the button (D-83) |
| `ui.build.shop` | (192, 32, 232, 304) | Tab bar (192, 32, 232, 16); four cards 52 × 80 at x = 192, 248, 304, 360, y = 56; Otherworld row label (192, 140, 232, 8) and two cards at x = 192, 248, y = 152 |
| `ui.build.inspector` | (432, 32, 200, 304) | Portrait slot 96 × 96 at (440, 40) (D-71); name `font.ui.8` at (544, 44); dept and tier at (544, 54); from y = 144 (D-65): the aura and floor multiplier where it stands, then *WHAT IT DOES* — the ability as a sentence, its passives, a one-line glossary of the kind it deals — and *WHERE TO PUT IT* — the rooms that boost its department, the furniture it likes beside it, its reach, the floor multipliers; while a shop card is carried the inspector shows the same block for the card; for a room, the comparison block (440, 276, 184, 24) — *here ×1.40 · Tier II* / *on 2F ×1.38 now, ×1.61 by round 14* / *relocate: −3 Tenure rounds, ¥13*; action buttons at y = 308: **LAY OFF · ¥1** (440, 308, 184, 20) for staff, or **RELOCATE · ¥13** (440, 308, 90, 20) and **DEMOLISH · ¥13** (534, 308, 90, 20) for rooms |
| `ui.build.firm_panel` | (432, 32, 200, 304) | The inspector's default state when nothing is selected: founder portrait 96 × 96 at (440, 40) (D-71); firm name at (544, 44); founder name and title at (544, 54) and (544, 64); run stats from y = 144 — round, strikes, fights won, Goodwill cap, floors leased, staff count; below them *HOW A FIGHT WORKS*, the six-sentence primer (D-65) |
| `ui.build.hint` | (0, 344, 640, 16) | One line of hint text, first run only; otherwise the hovered element's one-line summary |

Floor viewport internals: tiles 32 × 32 at `(24 + col × 32, floorY + row × 32)`.
Executive (4 × 2) draws 128 × 64 top-left-aligned in its 160 × 96 slot with the
remainder hatched in the `structure` tone. B1 (3 × 3) draws 96 × 96. Landing column
tiles carry a 1-pixel highlight on their left edge.

Undo is a key and a button: **UNDO** and **DROP** sit left of READY at (464, 4, 40, 16) and
(508, 4, 40, 16) so the build phase works by touch and, later, by controller (D-47, D-64).
A **REROLL** button (192, 140, 116, 16) sits under the cards; the Otherworld row moves to
y = 160 until the portal is open. Ready has no confirmation.

### 19.2 Battle screen

| Region | Rect | Contents |
| --- | --- | --- |
| `bg.battle.street` | (0, 0, 640, 360) | Street backdrop, seen front-on |
| `ui.battle.bar` | (160, 8, 320, 12) | Market Share bar; A fills from the left; ticks every 10%; percent labels at each end in `font.ui.8` |
| `ui.battle.goodwill.a` | (8, 28, 200, 16) | Goodwill **bar**: frame 200 × 16; fill from the left, width = `goodwill / capAtStart × 200`; the frame's right end sits at `cap / capAtStart × 200` so Morale erosion visibly shortens what can be refilled; the number `4,200` in `font.ui.16` overlaid left-aligned at (12, 28). Dims 50% while regen is suppressed; flashes on break |
| `ui.battle.goodwill.b` | (432, 28, 200, 16) | Mirror: fill from the right, frame erodes from the left, number right-aligned |
| `ui.battle.banner` | (240, 48, 160, 12) | Month banner, centred |
| `ui.battle.founder` | 36 × 36 | Founder badge in a 2 px frame; A at (8, 48), B at (596, 48). The firm name in `font.ui.8` beneath at y = 86 |
| `fx.tower.floor_segment` | 96 × 32, anchor bottom-centre | One per above-ground floor. Tower A stacks upward from (200, 280); Tower B from (440, 280). Four segments: G at the base, 3F at the top, y = 280, 248, 216, 184 |
| `fx.tower.roof` | 96 × 16, anchor bottom-centre | Above the top segment |
| `fx.tower.basement` | 96 × 24, anchor top-centre | B1, drawn below street level at y = 280, darker tone |
| `fx.tower.window_occupant` | 8 × 8, anchor centre | One per employee, filling its window in the segment's 5 × 3 grid. The window at floor tile (c, r) has its top-left at (8 + 18c, 4 + 9r) within the segment, so its 8 × 8 centre is (12 + 18c, 8 + 9r); side B mirrors the column, c′ = 4 − c, so the towers face each other. Tone by department; lights in the burst tone for a second after the employee fires (D-68) |
| `fx.window_burst` | 16 × 16, anchor centre | Spawned at the firer's window when an ability resolves (D-68; was the segment's centre); tone by kind; a floating `font.ui.8` number rises 16 px over 20 ticks |
| `ui.battle.ledger.a` | (8, 304, 308, 56) | Header line, then six lines at 8 px |
| `ui.battle.ledger.b` | (324, 304, 308, 56) | Same |
| `ui.battle.floor_inset` | 168 × 104, anchored at the hovered segment, clamped to screen | The hovered floor's top-down grid at 1×, employees drawn, the most recent firer highlighted |
| `ui.battle.controls` | (520, 48, 72, 16) | `1× 2× 4× ▸▸`; left of founder B's badge frame, which starts at x = 596 (D-64) |

A leased floor's segment carries all fifteen windows unlit; the occupant sprite lights
the ones with an employee in them. Empty floor slots (unleased) draw as
`fx.tower.floor_segment_empty` — the same wall band with no windows cut into it at all,
so an unleased floor reads as blank masonry from across the street. The two towers use
the city packs' facades; the inset uses the top-down interior packs. They never share a
pixel.

The 96 × 32 floor segment and 96 × 16 roof were **decided dimensions**, and the packs
were checked against them (Q-GBX-5, D-69): three 32 px wall tiles wide by one storey
tall is exactly 96 × 32, and the parapet cap is 96 × 16, so the numbers stand and the
`verify` flags are cleared.

### 19.3 Autopsy screen

| Region | Rect | Contents |
| --- | --- | --- |
| `ui.autopsy.banner` | (0, 0, 640, 24) | `Q7 · LOST · 38.1% MARKET SHARE` |
| `ui.autopsy.timeline` | (8, 32, 624, 48) | Market Share over time: sixty columns of 10 px, month boundaries as 1-pixel lines, a draggable playhead |
| `ui.autopsy.floors` | (8, 88, 200, 120) | A FLOORS / STAFF toggle across the top (16 px). FLOORS: five rows, one per floor slot, each with two horizontal bars (A, B) of total Push + Morale + Anomaly dealt, labelled. STAFF: your five employees with the most output, one bar each with the floor and the number (D-68), the chart players sell by |
| `ui.autopsy.findings` | (8, 216, 200, 120) | Three findings, `font.ui.8`, up to three lines each |
| `ui.autopsy.filters` | (216, 88, 416, 16) | Chips: `ALL PUSH MORALE ANOMALY REGEN STATUS` and `A B` and `G 1 2 3 B1` |
| `ui.autopsy.ledger` | (216, 108, 416, 228) | Full ledger, 8 px rows, scroll; the playhead selects the row |
| `ui.autopsy.continue` | (552, 340, 80, 16) | **CONTINUE** |

### 19.4 Campaign map

| Region | Rect | Contents |
| --- | --- | --- |
| `ui.map.header` | (0, 0, 640, 24) | `ACT 2 · THE COMPLIANCE OFFICE` and strikes |
| `ui.map.act` | (32, 40, 576, 280) | Up to eight columns at x = 32 + col × 72; three rows at y = 64 + row × 88; node icons 24 × 24 centred in a 72 × 88 cell; connecting paths 1 px; boss column always single-row, centred |
| `ui.map.node.*` | 24 × 24 | One icon per node kind: `takeover`, `audit`, `recruiter`, `board`, `consultant`, `boss` |
| `ui.map.marker` | 16 × 16, anchor centre | The firm's position |
| `ui.map.legend` | (0, 336, 640, 24) | Node kind legend |

### 19.5 Rival dossier

| Region | Rect | Contents |
| --- | --- | --- |
| `ui.map.dossier` | 200 × 88, anchored to the hovered node, clamped to screen | Rival founder badge 32 × 32 in a frame at (160, 4); rival name `font.ui.8` at (4, 4); archetype and floor count at (4, 14); gimmick name at (4, 28) and its one-line effect at (4, 38), two lines; a 5-cell floor strip at (4, 64) showing which floors are occupied, 16 × 8 per cell; for bosses, the signature mechanic in the `anomalous` tone |

Opens on hover over any fight node, in campaign only. There is no reward overlay; the
win bonus is written to the top bar.

### 19.6 Codex

| Region | Rect | Contents |
| --- | --- | --- |
| `ui.codex.header` | (0, 0, 640, 24) | `CODEX · 14 / 40`, class filter chips, page arrows |
| `ui.codex.grid` | (16, 32, 608, 304) | Recipe cards 176 × 40 in three columns at x = 16, 224, 432 and six rows at y = 32 + row × 48; eighteen per page |
| `ui.codex.recipe` | 176 × 40, top-left | Input slots 32 × 32 at (4, 4), (40, 4), (76, 4); arrow 16 × 8 at (112, 12); result slot 32 × 32 at (132, 4); class glyph 8 × 8 at (168, 4). The recipe name and any room context are the hover text in `ui.build.hint` |

Undiscovered recipes draw outlined slots only; partially revealed ones fill the slots
the player has held. Discovered ones draw the sprites.

### 19.7 Founder select

| Region | Rect | Contents |
| --- | --- | --- |
| Screen title | (16, 0) | `CHOOSE A FOUNDER` in `font.ui.32`, set straight on the backdrop with no plate behind it (D-75). It is text, not a slot — there is no `ui.founder.header` entry |
| `ui.founder.grid` | (8, 32, 152, 312) | The roster column: eight `ui.founder.tile`s in two columns at x = 16 + col × 72 and four rows at y = 40 + row × 72 (D-72) |
| `ui.founder.tile` | 64 × 64 | A `founder.*.thumb` inset at (8, 8); the selected tile's border is distinct and unmissable — it is the only thing telling the player which of the eight the panel is describing |
| `ui.founder.detail` | (168, 32, 464, 312) | The selected founder in full. Portrait 96 × 96 at (184, 48) in a frame; name in `font.ui.16` at (296, 50); title at (296, 72); the founder's battle badge 32 × 32 at (296, 96) with *in the fight* beside it at (334, 106); bio up to three lines from y = 164 at a 12 px pitch; a rule at y = 204; *YOU START WITH* at (184, 214) and the starting roster drawn as sprites from (184, 228) at a 40 px pitch, with the roster and the starting budget spelled out at (272, 236) and (272, 248); the founder passive or "no founder passive in this build" at (184, 276) (D-68, D-46) |
| Firm name | (424, 56) | The firm's name in `font.ui.16`, under a *FIRM NAME* caption in `font.ui.8` at (424, 44). It is text, not a slot — a name the game filled in is a readout, and a plate around it only claims to be a control. Default is the founder's surname plus *Holdings*. The name itself is the target for the optional rename; typing is never required (D-55), because the Steam Deck's on-screen keyboard is drawn by an overlay the stack may not have |
| `ui.founder.confirm` | (456, 306, 160, 24) | **FOUND THE FIRM**, the screen's one commit. Label centred in `font.ui.16` — 130 px wide, leaving 15 px clear of the 9 px corners — on the `operations` tone's dark end, so the primary action is the darkest thing on a light panel rather than the palest. Its centre line matches the firm-name field beside it |

Shown once at run start in both modes, as a roster column and a detail panel rather
than a wall of equal cards (D-72): eight faces are a menu, and the one you are reading
about deserves the rest of the screen. The profile remembers the last choice and
pre-selects it. Every founder starts the same run — the starting block says so in the
same words on all eight panels, which is D-46 made visible rather than hidden. Founders are content (`content/founders.json`, eight in v1) with a
portrait, a badge and an empty effects list; when a founder gains a mechanic, it is an
effect in that list, applied like a modifier, and the sim already reads it.

### 19.8 Entities

| Entity | Sprite | Anchor | Footprint | `sortBias` | Notes |
| --- | --- | --- | --- | --- | --- |
| `ui.card.applicant` | 52 × 80 | top-left | — | 0 | Laid out by `game/scenes/widgets/Card.tscn`, whose slots are relative to the card's own corner (D-81). The card is its face, name and price (D-82): the idle sprite frame on a lighter stage, the name, and the price in Honeyblot Caps on a rounded tag along the bottom edge (`ui.card.price`), at the height of its own `price_text` slot, still the card's largest mark. Tier, department, cooldown, the ability and an extraplanar rider are read in the inspector once the card is picked |
| `ui.card.room` | 52 × 80 | top-left | — | 0 | As the applicant card: the room tile on the stage, name, price tag (D-82) |
| `ui.card.furniture` | 52 × 80 | top-left | — | 0 | As the applicant card: the sprite on the stage, name, price tag (D-82) |
| `ui.card.selector` | 64 × 92 | top-left | — | 0 | The picked card's frame: the UI pack's four rounded corner brackets, 6 px outside the card, drawn over it |
| `ui.card.price` | 46 × 13 | top-left | — | 0 | The card's price tag: the UI pack's `box` in the dark `structure` ramp; the price in Honeyblot Caps at the height of the card's `price_text` slot, centred (D-82) |
| `emp.*` | 32 × 32 | bottom-centre | 1 × 1 | 0 | Down-facing idle frame in build view |
| `room.*` floor plan | 32 fw × (32 fh + 32) | bottom-left | per room | −10 | One composed plan per room type: floor across the footprint, fittings along the back row, the top 32 px overhanging into the wall band above (D-73). Every tile stays walkable |
| `room.*.sign` | 32 × 8 | top-left | — | −9 | Name + Tenure pips, at the room's top-left tile |
| `furn.*` floor-standing | 32 × 32 | bottom-centre | 1 × 1 | 0 | |
| `furn.*` wall-mounted | 32 × 40 | bottom-centre | 1 × 1 | −5 | 8 px overhang above the tile |
| `furn.*` 1 × 2 | 32 × 64 | bottom-centre | 1 × 2 | 0 | |
| `ui.aura_badge` | 12 × 8 | top-right of the employee's tile | — | +8 | `×1.2`, `×1.5` etc. |
| `ui.link_line` | 1 px | — | — | +7 | Furniture → triggered employee |
| `ui.tenure_pip` | 4 × 4 | in the sign | — | −9 | 0–3 pips |
| `ui.portrait` | 96 × 96 | top-left | — | 0 | Inspector portrait, at the Portraits pack's own size (D-71) |
| `founder.*.portrait` | 96 × 96 | top-left | — | 0 | One per founder, from `portraits/Portraits/transparent_bg` (D-71) |
| `founder.*.thumb` | 48 × 48 | top-left | — | 0 | The same face halved 2:1 at author time, for the select grid (D-72) |
| `founder.*.badge` | 32 × 32 | top-left | — | 0 | One per founder; battle screen and dossier |

Every one of these is a greybox on day one: a rectangle in its category tone with its
id, footprint and dimensions drawn on it. Rooms draw a solid footprint; wall-mounted
furniture draws a solid 32 × 32 footprint and a hatched 32 × 8 overhang above it.

---

## 20. Legibility rules

Every mechanic must be visible on the screen it happens on. These are the specific
obligations that fall out of this design:

| Mechanic | Where it must be seen | How |
| --- | --- | --- |
| Room aura | Build screen | Badge on every employee inside, showing the multiplier |
| Furniture trigger | Build screen | 1 px link line to each affected employee |
| Tenure | Build screen | Pips on the room sign; tier name on hover |
| Relocation trade-off | Build screen | The inspector's comparison block: this floor now, the other floor now and later, the Tenure cost |
| Landing adjacency | Build screen | Highlight on the landing column; link lines cross floors |
| Illegal placement | Build screen | The tile flashes `invalid` tone; the reason is the hint line |
| Near-miss | Build screen | Inputs flicker once, `?` glyph |
| Recipe available | Build screen | Inputs pulse, **Promote** glyph |
| Ability resolution | Battle screen | Window burst on the floor, floating number, ledger line |
| Goodwill level | Battle screen | Each firm's Goodwill bar, with the number on it |
| Goodwill break | Battle screen | The Goodwill bar empties and flashes, the Market Share bar begins to move, ledger banner `— GOODWILL BROKEN —` |
| Cap erosion (Morale) | Battle screen | The Goodwill bar's frame shortens |
| Regen suppressed | Battle screen | The Goodwill bar dims while suppressed |
| Rival gimmick | Map screen | The dossier, before the fight is chosen |
| Whose firm this is | Build, battle, map | The founder's portrait on the firm panel, the badge beside the Goodwill bar, the rival's badge in the dossier |
| Month transition | Battle screen | Banner, telegraphed one second early |
| Status applied | Battle screen | Ledger line; the floor inset shows a status glyph on the employee |
| Why I lost | Autopsy | Three findings, per-floor bars, full ledger |

If a synergy cannot be seen firing, it will not be believed. If a screen needs art to
be understood, its layout is under-specified.

---

## 21. What a human has to check

These are feel-dependent and cannot be judged by the harness. Each names the question
and the earliest moment it can be answered.

| Question | When | Signal that it is wrong |
| --- | --- | --- |
| Can you follow a full late-run fight from the live ledger alone? | First greybox fight with 12+ employees per side | You are reading the autopsy to find out what happened, not to confirm it |
| Does 60 seconds feel long? | First ten greybox fights | You are reaching for 2× before Month 2 |
| Does losing a lead in the Bell feel unfair? | First twenty fights | It happens more than one fight in four, or it feels like theft rather than tension |
| Does a Promote glyph feel like a discovery? | First near-miss | The `?` reads as an error |
| Are Tenure tiers felt, not just seen? | First Tier II | You cannot say without looking what a Tier II room is doing for you |
| Is demolishing a room a decision? | First time you want to | You do it without pausing |
| Does furniture earn its tile? (D-34) | Vertical slice, twenty runs | You place furniture only when a recipe wants it, or you never choose between a desk and a hire. Either means fold it into rooms |
| Does the part-arted build screen look deliberate? | First real asset dropped in | The screen reads as broken, not stylised |
