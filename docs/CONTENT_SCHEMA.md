# Company Wars — Content Schema

Status: **Phase 3 draft.** This document describes the content database under
`content/` and the JSON Schema under `schema/` that every file in it validates
against. The schema is the authority on *shape*; `SIMULATION_SPEC.md` is the authority
on what each shape *does* in a fight; this document is the guide to both, with the
rules for adding content without touching code.

Everything the game knows about employees, rooms, furniture, recipes, statuses, riders,
modifiers, floors, the shop, the economy, the two modes, the campaign map, the tutorial,
the rival templates and the scripted rival towers is in these files. Nothing of that
kind exists in code. The excerpts below are copied verbatim from the files by the script
that produces this document, so they cannot drift.

---

## Contents

1. [Layout and versioning](#1-layout-and-versioning)
2. [Identifiers](#2-identifiers)
3. [The effect vocabulary](#3-the-effect-vocabulary)
4. [Employees](#4-employees)
5. [Rooms](#5-rooms)
6. [Furniture](#6-furniture)
7. [Recipes](#7-recipes)
8. [Statuses](#8-statuses)
9. [Riders and modifiers](#9-riders-and-modifiers)
10. [Floors, economy, shop, modes, map, tutorial](#10-floors-economy-shop-modes-map-tutorial)
11. [Rivals: scripted towers and templates](#11-rivals-scripted-towers-and-templates)
12. [Validation: what the loader asserts](#12-validation-what-the-loader-asserts)
13. [Adding content without touching code](#13-adding-content-without-touching-code)
14. [What is a code change](#14-what-is-a-code-change)
15. [The catalogue at a glance](#15-the-catalogue-at-a-glance)

---

## 1. Layout and versioning

```
content/
  index.json              contentVersion, and the list of every file with its schema $def
  rules.json              the RuleSet — every constant in SIMULATION_SPEC §3
  economy.json            income table, prices, severance, leases, starting state
  floors.json             the five floor slots
  statuses.json           Burnout, Overtime, Bureaucracy, Frozen
  employees.json          the roster
  rooms.json              the room catalogue
  furniture.json          the furniture catalogue (on trial — D-34)
  recipes.json            promotions, renovations, rituals
  riders.json             extraplanar-hire drawbacks
  modifiers.json          Board Meeting modifiers and rival-only gimmicks, one list
  shop.json               stock tables by round, reroll costs, the Otherworld row
  modes.json              campaign and ranked as configuration
  map.json                the campaign's acts, columns and node kinds
  tutorial.json           first-run hint lines
  founders.json           the eight founders a player may choose; cosmetic in v1
  balance.json            the harness's populations, bands, counter web and seventeen invariants
  rivals/
    templates.json        semi-scripted rival archetypes and the lease schedule
    scripted/*.json       hand-authored rival towers: bosses and tutorial rivals
schema/
  content.schema.json     one JSON Schema (draft 2020-12) with a $def per file type
```

One file per type. A file is an object with a single array (`employees`, `rooms`, …)
or, for singletons, the object itself. `index.json` names every file and the `$def` it
validates against; the loader reads the index and nothing else to find content.

**`contentVersion`** is a semantic version string on `index.json`. Every tower
snapshot carries the `contentVersion` it was built against, and the sim refuses a match
whose two snapshots disagree with each other or with the loaded content. Bump rules:

| Change | Bump |
| --- | --- |
| Numbers only — a cost, a value, a cooldown, a multiplier | patch |
| A new entity, or a removed one that no stored snapshot references | minor |
| A removed or renamed entity that stored snapshots may reference; a schema change | major, with a snapshot migration |

`schemaVersion` on `rules.json` tracks `SIMULATION_SPEC.md`'s rule version, not
content. A constant change never bumps it; a rule change always does.

---

## 2. Identifiers

Every entity has an `id` of the form `<type>.<name>`: lowercase, digits and
underscores, exactly one dot. The prefix is the type and the loader asserts it:

| Prefix | Type | Example |
| --- | --- | --- |
| `emp.` | employee | `emp.counsel`, `emp.x_auditor` (extraplanar ids start `x_`) |
| `room.` | room | `room.server_room` |
| `furn.` | furniture | `furn.fax_machine` |
| `recipe.` | recipe | `recipe.senior_dev` |
| `status.` | status | `status.burnout` |
| `rider.` | rider | `rider.tenured` |
| `mod.` | modifier | `mod.lean`, `mod.g_deep_pockets` (gimmicks start `g_`) |
| `floor.` | floor | `floor.f2` |
| `founder.` | founder | `founder.sato` |
| `rival.` | scripted rival or template | `rival.boss_compliance_office`, `rival.t_turtle` |

Sprite, tile and icon references (`sprite`, `tile`, `icon`) point into the sprite
manifest (Phase 4) and may have more than two segments — `room.server_room.tile`,
`ui.map.node.audit`. The schema distinguishes `Id` from `ManifestId`. Ids are
referenced by string everywhere; there are no numeric keys.

Ids are permanent. Renaming one is a major version bump with a migration. A retired
entity keeps its id in a `retired` list rather than being deleted, so old snapshots
still resolve.

---

## 3. The effect vocabulary

Everything an employee, room, furniture piece, rider or modifier *does* is a list of
**effects**, and every effect is one object from a closed vocabulary. This is the whole
mechanism by which content stays out of code: the sim implements the vocabulary once,
and content composes it.

An effect has a **trigger** (`on`), an **action** (`do`), and depending on the action a
**subject** (who it is about), a **target** (who it acts on), and parameters.

### 3.1 Triggers — `on`

| `on` | Fires | Used by |
| --- | --- | --- |
| `ability` | When the unit's cooldown fills. Exactly one per employee | Employees |
| `afterFire` | After the unit's own ability resolves, every `everyN`th time | Employees (secondary effects), rooms and furniture (on a subject's fire) |
| `static` | Once, at match setup. Stats and flags | Everything |
| `periodic` | Every `every` ticks, tick > 0 | Furniture, rooms |
| `banner` | At the `month` transition (0 = Quarter Open, 3 = the Bell) | Rooms, furniture, riders, modifiers |
| `economy` | In the build phase; the sim never sees it | Sales passives, riders, modifiers |
| `onHire` | When the unit is purchased | Riders |
| `onAccept` | When a Board Meeting modifier is chosen | Modifiers |

### 3.2 Actions — `do`

| `do` | Needs | Meaning |
| --- | --- | --- |
| `sales` | `value`, `target` own firm | ¥ added to your own Revenue — SIMULATION_SPEC §9.3 |
| `poach` | `value`, `target` firm | Drains the rival's Loyalty, then moves ¥ from their Revenue to yours |
| `scandal` | `value`, `target` firm | Shrinks a Loyalty cap and moves ¥ at half rate to the other firm |
| `curse` | `value`, `target` firm | Moves ¥ from the rival straight through Loyalty; a quarter rebounds on your own |
| `pr` | `value`, `target` own firm | Direct Loyalty recovery |
| `status` | `status`, `stacks`, `target` | Apply stacks; `durationTicks` for Frozen |
| `cleanse` | `status`, `stacks`, `target` | Remove stacks |
| `retrigger` | `target` | Targets fire now, depth +1; optional `then` applies a status to each target after its resolution |
| `stat` | `stat`, `subject`, `amount` or `permille` | A match-start number on the subject — §3.5 |
| `flag` | `flag`, `subject` | A match-start boolean on the subject — §3.6 |
| `override` | `override`, `to`, `subject` | Replace a selector or a tier on the subject |
| `consumeAdjacentFurniture` | `subject`, `count` | Build-phase: destroy furniture next to the subject |
| `roomTenure` | `subject`, `rounds` | Build-phase: grant Tenure rounds |

### 3.3 Subject and target

**`subject`** is *who the effect is about*: who receives a stat, whose fire triggers an
`afterFire`, whose adjacency a periodic uses. Scopes: `self`, `adjacent`, `sameFloor`,
`occupants` (the room's), `all` (the firm's units), `firm`, `allRooms`. Optional
`dept` / `notDept` / `tag` filters. Employees default to `self`; rooms to
`occupants`; furniture to `adjacent`.

**`target`** is *who the action lands on*. Three shapes:

```json
{ "side": "enemy", "floor": "highest_occupied_floor", "unit": "highest_base_value" }
{ "side": "own",   "scope": "adjacent", "dept": ["engineering"], "pick": "highest_base_value" }
{ "side": "enemy", "scope": "firm" }
```

Enemy targets use the two-level selector vocabulary from `SIMULATION_SPEC.md` §6 —
floor: `highest_occupied_floor`, `lowest_occupied_floor`, `most_populated_floor`, `least_populated_floor`, `same_floor_index`, `random_floor`, `all_floors`; unit: `lowest_cooldown_remaining`, `highest_base_value`, `random`, `all`. Own targets use a
scope and an optional `pick`. Firm targets are for Sales, Poach, Scandal, Curse and PR,
which have no unit target (D-32).

### 3.4 Values

`value` is an integer, or one of two dynamic forms:

```json
{ "base": 300, "perTag": "sales", "each": 50 }
{ "permilleOfTargetCap": 150 }
```

### 3.5 Stats

`stat` names one of: `sales`, `poach`, `curse`, `pr`, `flatSales`, `cooldown`, `loyaltyCap`, `loyaltyCapMult`, `regenPerEvent`, `passiveMult`, `statusStacksBonus`, `burnoutMaxOverride`, `burnoutMaxDelta`, `curseSelfCost`, `retriggerBonus`, `floorOutput`, `income`, `upkeep`, `rerollCost`, `severance`, `severanceMult`.

Their sim semantics are in `SIMULATION_SPEC.md` §6.4. `amount` is an integer added;
`permille` is a multiplier. `floorOutput` also needs `floor` (a floor id, or `*`);
`statusStacksBonus` also needs `status`. Stats on a room are subject to Tenure: every
`permille` stat a room grants gains `rules.tenure.stepPermille` per tier.

### 3.6 Flags

`flag` names one of: `untargetable`, `bureaucracyImmune`, `frozenImmune`, `burnoutImmune`, `overtimePermanent`, `cannotBeRetriggered`, `wholeFloorAdjacency`, `capProtected`, `regenNeverSuppressed`, `receptionDisabled`, `everyFloorMostPopulated`, `floorSelectorMirror`, `cannotBeLaidOff`, `landingOnly`.

Semantics in `SIMULATION_SPEC.md` §6.4. Flags on the build-phase side
(`cannotBeLaidOff`, `landingOnly`) are read by the shop and
placement rules, never by the sim.

### 3.7 Tier gating

Any effect on a room may carry `fromTier` or `untilTier` (1–3). It is active only
while the room's Tenure tier is in range. This is how Tier III clauses and "until the
room is Institutional" penalties are written without a second mechanism.

---

## 4. Employees

One tile, one department, one tier, exactly one `ability` effect, any number of
others. `cooldownTicksByMonth` overrides the cooldown in a given month (the Senior
Developer's Crunch). `placement.floors` restricts where the unit may stand.
`countsAsDept` makes a unit satisfy another department's room auras. `inShop: false`
marks a unit that only exists as a recipe result. `attachments` does not appear here —
it is a snapshot field, always empty in v1 (D-13).

```json
{
  "id": "emp.counsel",
  "name": "Counsel",
  "dept": "legal",
  "tier": 2,
  "cost": 6,
  "extraplanar": false,
  "inShop": true,
  "tags": [],
  "cooldownTicks": 120,
  "initialProgressPermille": 0,
  "effects": [
    {
      "on": "ability",
      "do": "poach",
      "value": 90,
      "target": {
        "side": "enemy",
        "scope": "firm"
      },
      "name": "Cease & Desist"
    },
    {
      "on": "afterFire",
      "do": "status",
      "status": "status.bureaucracy",
      "stacks": 2,
      "target": {
        "side": "enemy",
        "floor": "highest_occupied_floor",
        "unit": "highest_base_value"
      },
      "everyN": 1
    },
    {
      "on": "static",
      "do": "stat",
      "stat": "loyaltyCap",
      "subject": {
        "scope": "self"
      },
      "amount": 250
    },
    {
      "on": "static",
      "do": "stat",
      "stat": "regenPerEvent",
      "subject": {
        "scope": "self"
      },
      "amount": 40
    }
  ],
  "sprite": "emp.counsel",
  "flavor": "Sends a letter. The letter has consequences."
}
```

A Sales passive is an `economy` effect the sim never reads:

```json
{
  "id": "emp.telemarketer",
  "name": "Telemarketer",
  "dept": "sales",
  "tier": 1,
  "cost": 2,
  "extraplanar": false,
  "inShop": true,
  "tags": [],
  "cooldownTicks": 40,
  "initialProgressPermille": 0,
  "effects": [
    {
      "on": "ability",
      "do": "sales",
      "value": 20,
      "target": {
        "side": "own",
        "scope": "firm"
      },
      "name": "Cold Call"
    },
    {
      "on": "economy",
      "do": "stat",
      "stat": "income",
      "subject": {
        "scope": "self"
      },
      "amount": 1
    }
  ],
  "sprite": "emp.telemarketer",
  "flavor": "¥20 every two seconds. Never a big number. That is the joke."
}
```

A ritual result, with a `countsAsDept` and two immunities:

```json
{
  "id": "emp.x_salaryman_who_never_left",
  "name": "Salaryman Who Never Left",
  "dept": "extraplanar",
  "tier": 3,
  "cost": 0,
  "extraplanar": true,
  "inShop": false,
  "tags": [
    "ritual"
  ],
  "cooldownTicks": 80,
  "initialProgressPermille": 0,
  "countsAsDept": "engineering",
  "effects": [
    {
      "on": "ability",
      "do": "curse",
      "value": 200,
      "target": {
        "side": "enemy",
        "scope": "firm"
      },
      "name": "Loyalty"
    },
    {
      "on": "static",
      "do": "flag",
      "flag": "bureaucracyImmune",
      "subject": {
        "scope": "self"
      }
    },
    {
      "on": "static",
      "do": "flag",
      "flag": "frozenImmune",
      "subject": {
        "scope": "self"
      }
    }
  ],
  "sprite": "emp.x_salaryman_who_never_left",
  "flavor": "Counts as Engineering for rooms. Immune to Bureaucracy and Frozen. Has a desk."
}
```

---

## 5. Rooms

A room is a zone: a footprint, the floors it is legal on, whether it may include the
landing column, a cost, and effects whose default subject is `occupants`. `fixed` rooms
(Reception) cannot be demolished and are placed by the floor definition.
`maxOccupants` limits employees, not furniture. `tile` is the manifest id of the
floor-tile sprite.

```json
{
  "id": "room.server_room",
  "name": "Server Room",
  "kind": "general",
  "footprint": {
    "w": 2,
    "h": 2
  },
  "floors": [
    "floor.f1",
    "floor.f2"
  ],
  "landingLegal": true,
  "cost": 9,
  "fixed": false,
  "effects": [
    {
      "on": "static",
      "do": "stat",
      "stat": "sales",
      "subject": {
        "scope": "occupants",
        "dept": [
          "engineering"
        ]
      },
      "permille": 1350
    },
    {
      "on": "banner",
      "do": "status",
      "status": "status.burnout",
      "stacks": 1,
      "target": {
        "side": "own",
        "scope": "occupants"
      },
      "month": 1,
      "untilTier": 3
    },
    {
      "on": "banner",
      "do": "status",
      "status": "status.burnout",
      "stacks": 1,
      "target": {
        "side": "own",
        "scope": "occupants"
      },
      "month": 2,
      "untilTier": 3
    }
  ],
  "tile": "room.server_room.tile",
  "flavor": "Engineering ×1.35. It is hot in there; occupants burn at each month until the room is Institutional."
}
```

The two banner effects carry `untilTier: 3` — that is the whole of "banner Burnout no
longer applies at Tier III". A Tier III clause is the same thing with `fromTier`:

```json
{
  "id": "room.boardroom",
  "name": "Boardroom",
  "kind": "executive",
  "footprint": {
    "w": 2,
    "h": 2
  },
  "floors": [
    "floor.f3"
  ],
  "landingLegal": true,
  "cost": 9,
  "fixed": false,
  "effects": [
    {
      "on": "static",
      "do": "stat",
      "stat": "sales",
      "subject": {
        "scope": "occupants"
      },
      "permille": 1200
    },
    {
      "on": "static",
      "do": "stat",
      "stat": "poach",
      "subject": {
        "scope": "occupants"
      },
      "permille": 1200
    },
    {
      "on": "static",
      "do": "flag",
      "flag": "wholeFloorAdjacency",
      "subject": {
        "scope": "occupants",
        "dept": [
          "management"
        ]
      }
    },
    {
      "on": "afterFire",
      "do": "status",
      "status": "status.overtime",
      "stacks": 1,
      "target": {
        "side": "own",
        "scope": "adjacent"
      },
      "everyN": 1,
      "subject": {
        "scope": "occupants",
        "dept": [
          "management"
        ]
      },
      "fromTier": 3
    }
  ],
  "tile": "room.boardroom.tile",
  "flavor": "Everyone ×1.2. Management retriggers reach the whole floor. Tier III: every retrigger is also a Standup."
}
```

Room costs are derived from footprint tile count via `economy.roomCostByTiles`, and the
`cost` field is asserted to match.

---

## 6. Furniture

Occupies tiles inside a room; default subject is `adjacent`. `wallMounted` furniture
has a 32 × 40 sprite with an 8-pixel overhang and `sortBias` −5 in the manifest
(`GAME_DESIGN.md` §19.7). `floors` restricts placement. Furniture is in v1 on trial
(D-34): if the trial fails, each furniture effect below moves onto a room as-is, since
the vocabulary is the same.

```json
{
  "id": "furn.fax_machine",
  "name": "Fax Machine",
  "footprint": {
    "w": 1,
    "h": 1
  },
  "wallMounted": false,
  "cost": 4,
  "rarity": "uncommon",
  "effects": [
    {
      "on": "periodic",
      "do": "retrigger",
      "target": {
        "side": "own",
        "scope": "adjacent",
        "dept": [
          "legal"
        ],
        "pick": "highest_base_value"
      },
      "every": 120
    }
  ],
  "sprite": "furn.fax_machine",
  "flavor": "Every 6s the adjacent Legal with the highest value fires again."
}
```

A subject-driven `afterFire` on furniture — "every fourth time a neighbour fires":

```json
{
  "id": "furn.copier",
  "name": "Copier",
  "footprint": {
    "w": 1,
    "h": 1
  },
  "wallMounted": false,
  "cost": 4,
  "rarity": "uncommon",
  "effects": [
    {
      "on": "afterFire",
      "do": "retrigger",
      "target": {
        "side": "own",
        "scope": "self"
      },
      "everyN": 4,
      "subject": {
        "scope": "adjacent"
      }
    }
  ],
  "sprite": "furn.copier",
  "flavor": "Every fourth time a neighbour fires, it fires again."
}
```

---

## 7. Recipes

A recipe is inputs, an optional room context, and a result. Inputs match by `defId` or
by `kind` + `tier`/`dept`/`tag`. `count` is how many; `consumed` whether the input is
destroyed; `adjacency` whether the inputs must be orthogonally adjacent to each other
(`adjacent`) or merely present anywhere in the tower (`any`). A `context` requires
the first input to stand inside that room. Results are an employee, a piece of
furniture, a room tier (the context room jumps to that tier) or Tenure rounds for the
context room.

```json
{
  "id": "recipe.senior_dev",
  "name": "Promotion Cycle",
  "class": "promotion",
  "inputs": [
    {
      "match": {
        "defId": "emp.junior_dev"
      },
      "count": 2,
      "consumed": true,
      "adjacency": "adjacent"
    },
    {
      "match": {
        "defId": "furn.whiteboard"
      },
      "count": 1,
      "consumed": false,
      "adjacency": "adjacent"
    }
  ],
  "context": null,
  "result": {
    "kind": "employee",
    "defId": "emp.senior_dev"
  },
  "codexHint": "Two juniors and a whiteboard."
}
```

A wildcard input and a room-context result:

```json
{
  "id": "recipe.director",
  "name": "Succession",
  "class": "promotion",
  "inputs": [
    {
      "match": {
        "defId": "emp.team_lead"
      },
      "count": 1,
      "consumed": true,
      "adjacency": "adjacent"
    },
    {
      "match": {
        "kind": "employee",
        "tier": 2
      },
      "count": 1,
      "consumed": true,
      "adjacency": "adjacent"
    }
  ],
  "context": {
    "room": "room.boardroom"
  },
  "result": {
    "kind": "employee",
    "defId": "emp.director"
  },
  "codexHint": "A Team Lead and any Tier 2, in the boardroom. The Tier 2 does not survive."
}
```

```json
{
  "id": "recipe.break_room_iii",
  "name": "Culture",
  "class": "renovation",
  "inputs": [
    {
      "match": {
        "defId": "furn.water_cooler"
      },
      "count": 1,
      "consumed": true,
      "adjacency": "adjacent"
    },
    {
      "match": {
        "defId": "furn.yakult_cart"
      },
      "count": 1,
      "consumed": true,
      "adjacency": "adjacent"
    }
  ],
  "context": {
    "room": "room.break_room"
  },
  "result": {
    "kind": "roomTier",
    "tier": 3
  },
  "codexHint": "Drinks in the break room."
}
```

The build phase checks recipes after every placement: a pattern with every input
present and adjacent, in the right context, offers the **Promote** glyph; a pattern
missing exactly one input, or complete in the wrong context, gives the near-miss
flicker (`GAME_DESIGN.md` §13.1). `codexHint` is the text a partially revealed codex
slot shows.

---

## 8. Statuses

Four, and their numbers are here rather than in `rules.json` because a status is
content — a fifth could be added. `cooldownRatePermillePerStack` is the per-stack
change to cooldown rate (§8.1 of the sim spec); `outputPenaltyPermillePerStack` and
`scandalPerStackPerEvent` are Burnout's; `onExpire` is Overtime's hangover.

```json
{
  "id": "status.overtime",
  "name": "Overtime",
  "maxStacks": 2,
  "durationTicks": 60,
  "expires": true,
  "cooldownRatePermillePerStack": 500,
  "outputPenaltyPermillePerStack": 0,
  "scandalPerStackPerEvent": 0,
  "onExpire": [
    {
      "status": "status.burnout",
      "stacks": 1
    }
  ],
  "tone": "operations",
  "text": "Haste. +50% cooldown speed per stack for 3s. When a stack expires the employee gains 1 Burnout."
}
```

The sim reads `status.*` ids from effects and looks up behaviour here. `Frozen` has
`durationTicks: null` because the applying ability sets it.

---

## 9. Riders and modifiers

A **rider** is a visible drawback rolled onto an extraplanar hire. A **modifier** is a
run-scoped global: chosen at a Board Meeting by the player, or carried by a rival as a
gimmick. Both are lists of effects with `subject` defaulting to the firm or the unit as
appropriate, and both ride in the snapshot's `globals`.

```json
{
  "id": "rider.contractual_obligation",
  "name": "Contractual Obligation",
  "text": "At the Bell, the firm causes itself a 200 Scandal.",
  "effects": [
    {
      "on": "banner",
      "do": "scandal",
      "month": 3,
      "value": 200,
      "target": {
        "side": "own",
        "scope": "firm"
      }
    }
  ]
}
```

```json
{
  "id": "mod.lean",
  "name": "Lean",
  "text": "Loyalty cap −200; income +¥2 per round.",
  "rivalOnly": false,
  "boardMeeting": {
    "cost": "−200 Loyalty cap",
    "benefit": "+¥2 income per round"
  },
  "effects": [
    {
      "on": "static",
      "do": "stat",
      "stat": "loyaltyCap",
      "subject": {
        "scope": "firm"
      },
      "amount": -200
    },
    {
      "on": "economy",
      "do": "stat",
      "stat": "income",
      "subject": {
        "scope": "firm"
      },
      "amount": 2
    }
  ]
}
```

A rival-only gimmick is the same type with `rivalOnly: true` and a `teaches` line the
dossier shows:

```json
{
  "id": "mod.g_regulatory_capture",
  "name": "Regulatory Capture",
  "text": "Regen is never suppressed.",
  "rivalOnly": true,
  "teaches": "The Act 2 boss's signature. Its Loyalty always comes back: answer it with Scandal, Curse, or more Sales.",
  "effects": [
    {
      "on": "static",
      "do": "flag",
      "flag": "regenNeverSuppressed",
      "subject": {
        "scope": "firm"
      }
    }
  ]
}
```

The sim does not know which side is the player. The shop refuses to offer a
`rivalOnly` modifier; that is the only enforcement, and it is on the build side.

A **founder** is the third modifier-shaped entity. Chosen at run start, it names a
portrait and a badge in the manifest and carries an `effects` list the sim applies
exactly as a modifier's, ordered first. In v1 every list is empty — the founder is the
player's face on the firm panel and beside the Loyalty bar — and it is in the snapshot's
`globals.founderId` so that a founder gaining a mechanic later is a content edit rather
than a format migration (D-46). Rival templates draw from a `founderPool`; scripted
rivals name theirs.

```json
{
  "id": "founder.the_founder",
  "name": "The Founder",
  "title": "Deceased, 1987",
  "bio": "Still listed on the letterhead. Still signs the quarterly memo. The signature is fresh.",
  "portrait": "founder.the_founder.portrait",
  "badge": "founder.the_founder.badge",
  "inShop": true,
  "effects": []
}
```

---

## 10. Floors, economy, shop, modes, map, tutorial

**`floors.json`** — five entries; `index` is the sim's `FLOOR_INDEX`; `grid` is the
tile size; `outputPermille` is the floor multiplier; `roomKinds` is legality;
`upkeepBudget`, `capTax`, `lease` are the economy; `targetable: false` on B1 is the
selector exclusion; `fixedRooms` places Reception.

**`economy.json`** — the income table (asserted equal to `constant + ceil(round / 2)`),
prices by tier and tile count, severance, reroll, the renovation and relocation fee
rules with the relocation Tenure penalty, and the starting roster and budget.

**`shop.json`** — cards per tab, reroll cost, the Recruiter node's upgrade, the
Otherworld row, and the tier table: for each round range, how many staff of each tier
are offered, which room sizes, which furniture rarities.

**`modes.json`** — the entire difference between campaign and ranked, as
`GAME_DESIGN.md` §16 lists it. A field that both modes set identically is a candidate
for removal from this file.

**`map.json`** — node kinds with their icons and behaviour; acts with their column
strings (`F` fight, `X` interlude, `B` boss), round spans, boss ids, and the scripted
fights the first run substitutes; layout rules for the map screen. The loader asserts
each act's `F` + `B` count equals its round span.

**`tutorial.json`** — one hint line per round for the first run.

**`balance.json`** — the balance harness's configuration: seed counts, the smoke
rounds, the populations it builds, the target bands, the archetype counter web, the
boss counters, and seventeen invariants each with a population, a measure, a
comparator, a threshold, a cadence and a severity. `BALANCE_PLAN.md` explains them.
An invariant is content; the harness implements its `measure` and reads the rest.

---

## 11. Rivals: scripted towers and templates

### 11.1 Scripted rivals

A scripted rival wraps a `TowerSnapshot` — the exact format `SIMULATION_SPEC.md` §4.2
consumes — with a name, archetype, round, gimmick, and a note saying what the tower is
for. `exemptFromBudget` is true only for bosses, which are meant to be richer than the
player. The snapshot's `globals.modifiers` carries the gimmick.

The Act 1 boss, abridged to its 1F:

```json
{
  "index": 1,
  "grid": {
    "w": 5,
    "h": 3
  },
  "rooms": [
    {
      "roomId": "f1_open",
      "defId": "room.open_plan",
      "rect": [
        0,
        0,
        2,
        3
      ],
      "tenureRounds": 4
    }
  ],
  "occupants": [
    {
      "tile": [
        0,
        0
      ],
      "kind": "employee",
      "defId": "emp.junior_dev",
      "instanceId": "e_003",
      "attachments": []
    },
    {
      "tile": [
        1,
        0
      ],
      "kind": "employee",
      "defId": "emp.senior_dev",
      "instanceId": "e_004",
      "attachments": []
    },
    {
      "tile": [
        0,
        1
      ],
      "kind": "employee",
      "defId": "emp.junior_dev",
      "instanceId": "e_005",
      "attachments": []
    },
    {
      "tile": [
        1,
        1
      ],
      "kind": "furniture",
      "defId": "furn.whiteboard",
      "instanceId": "f_006",
      "attachments": []
    },
    {
      "tile": [
        0,
        2
      ],
      "kind": "employee",
      "defId": "emp.qa_tester",
      "instanceId": "e_007",
      "attachments": []
    }
  ]
}
```

Every scripted snapshot passes the structural validation in §12 and, unless exempt, the
constructibility check. They are also the seed ghost pool for ranked (D-19) and the
first balance fixtures (D-36).

### 11.2 Templates and the expander

A template is an archetype's shopping list. Expanding one into a snapshot is
deterministic from `(templateId, round, seed)`:

1. Seed a mulberry32 generator (SIMULATION_SPEC §17) with `seed`.
2. **Budget.** `cumulativeIncome(round) × budgetPermille / 1000`, where cumulative
   income is the player's income table summed to this round plus the starting budget.
3. **Lease.** Floors from `leaseSchedule.byRound` for the greatest key ≤ round; their
   lease costs come off the budget.
4. **Rooms.** Repeatedly draw from `rooms` weighted by `weight` among entries with
   `minRound ≤ round`, place on the first floor in `layout.floorPreference` with space
   for the footprint (top-left scan, respecting legality and landing rules), pay the
   cost, until rooms have consumed 40% of the remaining budget or nothing fits.
5. **Staff.** Repeatedly draw from `staff` the same way, pay the cost, until budget is
   below the cheapest remaining entry. Reserve one slot in Reception if any Legal or
   Sales unit was drawn.
6. **Place.** Iterate departments in `layout.fillOrder`; for each unit of that
   department, place on the first room tile whose room grants that department a
   `permille` stat > 1000, then any room tile, then the landing column, then the
   corridor. Same-floor first, floors in preference order.
7. **Tenure.** Each room gets `min(round − 1, roomsPlacedIndex × 2)` — earlier
   purchases are older — clamped so no rival room is above the tier a player could
   reach by that round.
8. **Gimmick.** If the mode enables gimmicks and `round ≥ gimmicksFromRound`, or the
   node is an Audit, draw one from `gimmickPool`.
9. **Validate.** Run §12. On failure, remove the last purchase and go to 6. After
   three failures, fall back to the scripted rival for the nearest round.

The expander is build-side code, not sim code. It is specified here because the
snapshots it produces are content, and because the balance harness runs it.

---

## 12. Validation: what the loader asserts

The loader runs on start-up in development and in CI, and fails the build on any of
these. Each is either a schema rule or a referential rule.

**Schema** (`schema/content.schema.json`): every file validates against its `$def`.
`additionalProperties: false` everywhere — an unknown key is an error, not a warning.
Every `on`, `do`, `stat`, `flag`, `override`, selector and scope is an enum.

**Referential**, in addition:

- Every id is unique across all files and its prefix matches its type.
- Every `defId`, `status`, `floor`, `room`, `rider`, `modifier` and `boss` reference
  resolves.
- Every employee has exactly one `on: ability` effect.
- Every employee with `inShop: false` is the result of at least one recipe.
- A recipe whose result is `roomTier` or `roomTenure` has a `context`.
- Room `cost` equals `economy.roomCostByTiles[w × h]` (Reception excepted).
- Every template's `gimmickPool` contains only `rivalOnly` modifiers.
- Each map act's `F` + `B` count equals its round span.
- The income table equals its formula.

**Snapshot structure** (SIMULATION_SPEC §5.1), for every scripted rival:

- Floor grids match `floors.json`; no floor index repeats.
- Rooms are legal on their floor, the right size, inside the grid, non-overlapping,
  and off the landing column where `landingLegal` is false.
- No two occupants share a tile; furniture lies wholly inside one room and on a legal
  floor; employees respect `placement`; rooms respect `maxOccupants`.
- B1 has occupants only if `leasedB1`; only extraplanar units stand on B1.
- Every shop-bought extraplanar unit has a rider; every gimmick is in `modifiers`.

**Constructibility**, for every non-exempt snapshot: the total cost of its rooms,
staff, furniture and leases is at most the cumulative income to its round. This is the
same check that guards the ranked ghost pool (D-20).

---

## 13. Adding content without touching code

The test for "is this content?" is: *can it be written as effects from §3 on an entity
type from §4–§11?* If yes, it is a JSON edit and a version bump. Recipes for each:

**A new employee.** Add an entry to `employees.json` with a unique `emp.` id, one
`ability` effect and a `sprite` id. Add a manifest entry for the sprite (Phase 4) —
the greybox appears immediately. If it is a recipe result, add the recipe and set
`inShop: false`. Minor version bump.

**A new room.** Add to `rooms.json` with footprint, floors, cost matching the tile
table, effects with `occupants` subjects, and a `tile` id. Tier III clauses are
`fromTier: 3`. Minor bump.

**A new recipe.** Add to `recipes.json`. The codex gains a slot automatically; the
count in the codex header is `len(recipes)`. Minor bump.

**A new gimmick.** Add to `modifiers.json` with `rivalOnly: true` and a `teaches`
line; add it to a template's `gimmickPool` or a scripted rival's `modifiers`. Minor.

**A new scripted rival.** Author the snapshot, run the loader, fix what it rejects,
give it a `note` saying what it is for. Add it to `map.json` if it is a boss or a
first-run fight. Minor.

**A number.** Edit it. Patch bump. Bosses and templates are re-checked by the balance
harness on every content change, because they are fixtures.

**A new status.** Add to `statuses.json` — the sim reads status behaviour from the
file. But a status whose behaviour needs a *new kind* of effect (say, one that
redirects damage) is a code change, because the vocabulary does not have it.

---

## 14. What is a code change

Anything that adds a word to the vocabulary. Specifically:

- A new `on`, `do`, `stat`, `flag`, `override`, selector, scope or value form.
- A new entity type, or a new field on an existing one.
- A change to how any existing word behaves — that is a `SIMULATION_SPEC.md` rule
  change and bumps `schemaVersion`.
- A new floor slot, or a change to a grid size (the manifest and every screen layout
  depend on them).

The discipline is deliberate. A vocabulary that content can extend is a vocabulary that
cannot be audited, and the determinism contract depends on the sim's behaviour being
enumerable from one document. When a piece of content genuinely needs a new word, add
the word to the schema and the spec first, with a fixture, and then the content.

---

## 15. The catalogue at a glance

| Type | Count | Notes |
| --- | --- | --- |
| Employees | 40 | 30 across five departments; 5 extraplanar in the shop; 5 ritual results |
| Rooms | 16 | Reception is fixed; 3 executive, 2 extraplanar, 1 security |
| Furniture | 14 | On trial (Q-LYR-3) |
| Recipes | 40 | 21 promotions, 12 renovations, 7 rituals |
| Riders | 12 | Capped at 12 by design |
| Modifiers | 18 | 7 Board Meeting, 10 rival-only gimmicks, 1 build-side (Unpaid Upkeep, never offered) |
| Rival templates | 6 | One per archetype |
| Scripted rivals | 9 | 3 bosses, 6 first-run fights |
| Founders | 8 | Cosmetic in v1; effects reserved |

This is enough for a real run: sixteen fights against templated rivals from six
archetypes, three bosses, forty recipes to find. It is the first pass. Phase 5's
balance harness will move most of the numbers and probably retire a few entries.
