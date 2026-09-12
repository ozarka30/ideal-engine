#!/usr/bin/env python3
"""Emits schema/content.schema.json and validates every content file against it."""
import json, os, sys, glob
from collections import OrderedDict as OD
import jsonschema

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ID = {"type": "string", "pattern": r"^[a-z0-9]+\.[a-z0-9_]+$"}
MID = {"type": "string", "pattern": r"^[a-z0-9]+(\.[a-z0-9_]+)+$"}  # sprite-manifest id: two or more segments
INT = {"type": "integer"}
NONNEG = {"type": "integer", "minimum": 0}
PERMILLE = {"type": "integer", "minimum": 0}
STR = {"type": "string"}
BOOL = {"type": "boolean"}
def ref(n): return {"$ref": "#/$defs/" + n}
def arr(item, min_=0): return {"type": "array", "items": item, "minItems": min_}
def obj(props, req=None, extra=False):
    d = OD([("type", "object"), ("properties", props)])
    if req: d["required"] = req
    d["additionalProperties"] = extra
    return d
def enum(*vals): return {"type": "string", "enum": list(vals)}

DEPTS = ["engineering", "legal", "hr", "sales", "management", "extraplanar"]
FLOOR_SEL = ["highest_occupied_floor", "lowest_occupied_floor", "most_populated_floor",
             "least_populated_floor", "same_floor_index", "random_floor", "all_floors"]
UNIT_SEL = ["lowest_cooldown_remaining", "highest_base_value", "random", "all"]
OWN_SCOPE = ["self", "adjacent", "sameFloor", "occupants", "all"]
SUBJECT_SCOPE = ["self", "adjacent", "sameFloor", "occupants", "all", "firm", "allRooms"]
ON = ["ability", "afterFire", "static", "periodic", "banner", "economy", "onHire", "onAccept"]
DO = ["sales", "poach", "scandal", "curse", "pr", "status", "cleanse", "retrigger", "stat", "flag",
      "override", "consumeAdjacentFurniture", "roomTenure"]
STATS = ["sales", "poach", "curse", "pr", "flatSales", "cooldown", "loyaltyCap", "loyaltyCapMult",
         "regenPerEvent", "passiveMult", "statusStacksBonus", "burnoutMaxOverride", "burnoutMaxDelta",
         "curseSelfCost", "retriggerBonus", "floorOutput", "income", "upkeep", "rerollCost",
         "severance", "severanceMult"]
FLAGS = ["untargetable", "bureaucracyImmune", "frozenImmune", "burnoutImmune", "overtimePermanent",
         "cannotBeRetriggered", "wholeFloorAdjacency", "capProtected", "regenNeverSuppressed",
         "receptionDisabled", "everyFloorMostPopulated", "floorSelectorMirror",
         "cannotBeLaidOff", "landingOnly"]
OVERRIDES = ["floorSelector", "tenureTier"]
ROOM_KINDS = ["general", "reception", "security", "executive", "extraplanar"]
FLOOR_KINDS = ["portal", "reception", "operations", "executive"]
ARCHETYPES = ["generalist", "turtle", "burst", "economy", "burnout", "management"]

defs = OD()
defs["Id"] = ID
defs["ManifestId"] = MID
defs["Dept"] = enum(*DEPTS)
defs["FloorSelector"] = enum(*FLOOR_SEL)
defs["UnitSelector"] = enum(*UNIT_SEL)
defs["TargetEnemy"] = obj(OD([("side", enum("enemy")), ("floor", ref("FloorSelector")), ("unit", ref("UnitSelector"))]), ["side", "floor", "unit"])
defs["TargetOwn"] = obj(OD([("side", enum("own")), ("scope", enum(*OWN_SCOPE)), ("dept", arr(ref("Dept"), 1)),
                            ("tag", STR), ("pick", enum("highest_base_value", "lowest_cooldown_remaining", "random"))]), ["side", "scope"])
defs["TargetFirm"] = obj(OD([("side", enum("own", "enemy")), ("scope", enum("firm"))]), ["side", "scope"])
defs["Target"] = {"oneOf": [ref("TargetEnemy"), ref("TargetOwn"), ref("TargetFirm")]}
defs["Subject"] = obj(OD([("scope", enum(*SUBJECT_SCOPE)), ("dept", arr(ref("Dept"), 1)), ("notDept", arr(ref("Dept"), 1)), ("tag", STR)]), ["scope"])
defs["Value"] = {"oneOf": [
    NONNEG,
    obj(OD([("base", NONNEG), ("perTag", STR), ("each", NONNEG)]), ["base", "perTag", "each"]),
    obj(OD([("permilleOfTargetCap", PERMILLE)]), ["permilleOfTargetCap"]),
]}
defs["Then"] = obj(OD([("status", ID), ("stacks", {"type": "integer", "minimum": 1})]), ["status", "stacks"])

effect_props = OD([
    ("on", enum(*ON)), ("do", enum(*DO)), ("name", STR),
    ("value", ref("Value")), ("target", ref("Target")), ("subject", ref("Subject")),
    ("status", ID), ("stacks", {"type": "integer", "minimum": 1}), ("durationTicks", {"type": "integer", "minimum": 1}),
    ("stat", enum(*STATS)), ("amount", INT), ("permille", INT), ("floor", STR),
    ("flag", enum(*FLAGS)), ("override", enum(*OVERRIDES)), ("to", {"oneOf": [STR, INT]}),
    ("every", {"type": "integer", "minimum": 1}), ("everyN", {"type": "integer", "minimum": 1}),
    ("month", {"type": "integer", "minimum": 0, "maximum": 3}),
    ("fromTier", {"type": "integer", "minimum": 1, "maximum": 3}), ("untilTier", {"type": "integer", "minimum": 1, "maximum": 3}),
    ("then", ref("Then")), ("count", {"type": "integer", "minimum": 1}), ("rounds", {"type": "integer", "minimum": 1}),
])
def when(do_, req, **extra):
    d = {"if": {"properties": {"do": {"const": do_}}, "required": ["do"]}, "then": {"required": req}}
    d["then"].update(extra)
    return d
def when_on(on_, req):
    return {"if": {"properties": {"on": {"const": on_}}, "required": ["on"]}, "then": {"required": req}}
defs["Effect"] = {
    "type": "object", "properties": effect_props, "required": ["on", "do"], "additionalProperties": False,
    "allOf": [
        when("sales", ["value", "target"]), when("poach", ["value", "target"]), when("scandal", ["value", "target"]),
        when("curse", ["value", "target"]), when("pr", ["value", "target"]),
        when("status", ["status", "stacks", "target"]), when("cleanse", ["status", "stacks", "target"]),
        when("retrigger", ["target"]),
        when("stat", ["stat", "subject"], anyOf=[{"required": ["amount"]}, {"required": ["permille"]}]),
        when("flag", ["flag", "subject"]), when("override", ["override", "to", "subject"]),
        when("consumeAdjacentFurniture", ["subject", "count"]), when("roomTenure", ["rounds", "subject"]),
        when_on("periodic", ["every"]), when_on("banner", ["month"]), when_on("afterFire", ["everyN"]),
        {"if": {"properties": {"stat": {"const": "floorOutput"}}, "required": ["stat"]}, "then": {"required": ["floor"]}},
        {"if": {"properties": {"stat": {"const": "statusStacksBonus"}}, "required": ["stat"]}, "then": {"required": ["status"]}},
    ],
}
defs["Effects"] = arr(ref("Effect"))
defs["Footprint"] = obj(OD([("w", {"type": "integer", "minimum": 1, "maximum": 5}), ("h", {"type": "integer", "minimum": 1, "maximum": 3})]), ["w", "h"])

defs["Employee"] = obj(OD([
    ("id", ID), ("name", STR), ("dept", ref("Dept")), ("tier", {"type": "integer", "minimum": 1, "maximum": 3}),
    ("cost", NONNEG), ("extraplanar", BOOL), ("inShop", BOOL), ("tags", arr(STR)),
    ("cooldownTicks", {"type": "integer", "minimum": 20}),
    ("cooldownTicksByMonth", {"type": "object", "patternProperties": {"^[0-3]$": {"type": "integer", "minimum": 20}}, "additionalProperties": False}),
    ("initialProgressPermille", PERMILLE), ("placement", obj(OD([("floors", arr(ID, 1))]), ["floors"])),
    ("countsAsDept", ref("Dept")), ("effects", ref("Effects")), ("sprite", ref("ManifestId")), ("flavor", STR),
]), ["id", "name", "dept", "tier", "cost", "extraplanar", "inShop", "tags", "cooldownTicks", "initialProgressPermille", "effects", "sprite"])

defs["Room"] = obj(OD([
    ("id", ID), ("name", STR), ("kind", enum(*ROOM_KINDS)), ("footprint", ref("Footprint")), ("floors", arr(ID, 1)),
    ("landingLegal", BOOL), ("cost", NONNEG), ("fixed", BOOL), ("maxOccupants", {"type": "integer", "minimum": 1}),
    ("effects", ref("Effects")), ("tile", ref("ManifestId")), ("flavor", STR),
]), ["id", "name", "kind", "footprint", "floors", "landingLegal", "cost", "fixed", "effects", "tile"])

defs["Furniture"] = obj(OD([
    ("id", ID), ("name", STR), ("footprint", ref("Footprint")), ("wallMounted", BOOL), ("cost", NONNEG),
    ("rarity", enum("common", "uncommon")), ("floors", arr(ID, 1)), ("effects", ref("Effects")), ("sprite", ref("ManifestId")), ("flavor", STR),
]), ["id", "name", "footprint", "wallMounted", "cost", "rarity", "effects", "sprite"])

defs["RecipeMatch"] = {"oneOf": [
    obj(OD([("defId", ID)]), ["defId"]),
    obj(OD([("kind", enum("employee", "furniture")), ("tier", {"type": "integer", "minimum": 1, "maximum": 3}), ("dept", ref("Dept")), ("tag", STR)]), ["kind"]),
]}
defs["RecipeInput"] = obj(OD([("match", ref("RecipeMatch")), ("count", {"type": "integer", "minimum": 1, "maximum": 3}),
                              ("consumed", BOOL), ("adjacency", enum("adjacent", "any"))]), ["match", "count", "consumed", "adjacency"])
defs["RecipeResult"] = {"oneOf": [
    obj(OD([("kind", enum("employee", "furniture")), ("defId", ID)]), ["kind", "defId"]),
    obj(OD([("kind", enum("roomTier")), ("tier", {"type": "integer", "minimum": 1, "maximum": 3})]), ["kind", "tier"]),
    obj(OD([("kind", enum("roomTenure")), ("rounds", {"type": "integer", "minimum": 1})]), ["kind", "rounds"]),
]}
defs["Recipe"] = obj(OD([
    ("id", ID), ("name", STR), ("class", enum("promotion", "renovation", "ritual")),
    ("inputs", arr(ref("RecipeInput"), 1)), ("context", {"oneOf": [{"type": "null"}, obj(OD([("room", ID)]), ["room"])]}),
    ("result", ref("RecipeResult")), ("codexHint", STR),
]), ["id", "name", "class", "inputs", "context", "result", "codexHint"])

defs["Rider"] = obj(OD([("id", ID), ("name", STR), ("text", STR), ("effects", ref("Effects"))]), ["id", "name", "text", "effects"])
defs["Modifier"] = obj(OD([
    ("id", ID), ("name", STR), ("text", STR), ("rivalOnly", BOOL),
    ("boardMeeting", obj(OD([("cost", STR), ("benefit", STR)]), ["cost", "benefit"])), ("teaches", STR), ("effects", ref("Effects")),
]), ["id", "name", "text", "rivalOnly", "effects"])

defs["Floor"] = obj(OD([
    ("id", ID), ("index", {"type": "integer", "minimum": -1, "maximum": 3}), ("name", STR), ("kind", enum(*FLOOR_KINDS)),
    ("grid", ref("Footprint")), ("outputPermille", PERMILLE), ("roomKinds", arr(enum(*ROOM_KINDS), 1)),
    ("upkeepBudget", NONNEG), ("capTax", NONNEG), ("lease", NONNEG), ("starting", BOOL), ("requiresPortal", BOOL), ("targetable", BOOL),
    ("fixedRooms", arr(obj(OD([("defId", ID), ("rect", arr(NONNEG, 4))]), ["defId", "rect"]))),
]), ["id", "index", "name", "kind", "grid", "outputPermille", "roomKinds", "upkeepBudget", "capTax", "lease", "starting", "requiresPortal", "targetable"])

defs["Status"] = obj(OD([
    ("id", ID), ("name", STR), ("maxStacks", {"type": "integer", "minimum": 1}), ("durationTicks", {"oneOf": [{"type": "null"}, {"type": "integer", "minimum": 1}]}),
    ("expires", BOOL), ("cooldownRatePermillePerStack", INT), ("outputPenaltyPermillePerStack", NONNEG), ("scandalPerStackPerEvent", NONNEG),
    ("onExpire", arr(ref("Then"))), ("tone", STR), ("text", STR),
]), ["id", "name", "maxStacks", "durationTicks", "expires", "cooldownRatePermillePerStack", "outputPenaltyPermillePerStack", "scandalPerStackPerEvent", "onExpire", "tone", "text"])

defs["RuleSet"] = obj(OD([
    ("id", ID), ("schemaVersion", {"type": "integer", "minimum": 1}),
    ("time", obj(OD([("ticksPerSecond", NONNEG), ("quarterTicks", NONNEG), ("monthStart", arr(NONNEG, 4)), ("rushMult", arr(PERMILLE, 4)), ("regenMult", arr(PERMILLE, 4))]),
                 ["ticksPerSecond", "quarterTicks", "monthStart", "rushMult", "regenMult"])),
    ("loyalty", obj(OD([("baseCap", obj(OD([("constant", NONNEG), ("perRound", NONNEG)]), ["constant", "perRound"])), ("regenBasePermille", PERMILLE),
                        ("regenInterval", NONNEG), ("regenSuppressWindow", NONNEG), ("suppressThresholdPermille", PERMILLE)]),
                    ["baseCap", "regenBasePermille", "regenInterval", "regenSuppressWindow", "suppressThresholdPermille"])),
    ("scandal", obj(OD([("interval", NONNEG), ("perStack", NONNEG), ("transferPermille", PERMILLE)]), ["interval", "perStack", "transferPermille"])),
    ("curse", obj(OD([("selfCostPermille", PERMILLE)]), ["selfCostPermille"])),
    ("floors", obj(OD([("corridorMult", PERMILLE)]), ["corridorMult"])),
    ("tenure", obj(OD([("tierRounds", arr(NONNEG, 3)), ("stepPermille", PERMILLE), ("staffedPermille", PERMILLE)]), ["tierRounds", "stepPermille", "staffedPermille"])),
    ("portal", obj(OD([("receptionCapPerOccupant", NONNEG), ("employeeCapTax", NONNEG), ("b1LeaseCapTax", NONNEG)]), ["receptionCapPerOccupant", "employeeCapTax", "b1LeaseCapTax"])),
    ("retrigger", obj(OD([("depthMax", NONNEG)]), ["depthMax"])),
    ("upkeep", obj(OD([("loyaltyPerUnpaidBudget", NONNEG)]), ["loyaltyPerUnpaidBudget"])),
]), ["id", "schemaVersion", "time", "loyalty", "scandal", "curse", "floors", "tenure", "portal", "retrigger", "upkeep"])

TIERMAP = {"type": "object", "patternProperties": {"^(1|2|3|extraplanar)$": NONNEG}, "additionalProperties": False}
defs["Economy"] = obj(OD([
    ("id", ID), ("startingBudget", NONNEG),
    ("income", obj(OD([("constant", NONNEG), ("perTwoRounds", NONNEG), ("table", arr(NONNEG, 16))]), ["constant", "perTwoRounds", "table"])),
    ("winBonus", obj(OD([("fight", NONNEG), ("audit", NONNEG), ("boss", NONNEG)]), ["fight", "audit", "boss"])),
    ("employeeCost", TIERMAP), ("severance", TIERMAP),
    ("roomCostByTiles", {"type": "object", "patternProperties": {"^[0-9]+$": NONNEG}, "additionalProperties": False}),
    ("furnitureCost", obj(OD([("common", NONNEG), ("uncommon", NONNEG)]), ["common", "uncommon"])),
    ("rerollCost", NONNEG), ("renovationFee", enum("currentRoundIncome")), ("relocationFee", enum("currentRoundIncome")),
    ("relocationTenurePenaltyRounds", NONNEG), ("furnitureSellRefund", NONNEG),
    ("startingRoster", arr(ID)), ("startingRosterFloor", ID),
]), ["id", "startingBudget", "income", "winBonus", "employeeCost", "severance", "roomCostByTiles", "furnitureCost", "rerollCost", "renovationFee", "relocationFee", "relocationTenurePenaltyRounds", "furnitureSellRefund", "startingRoster", "startingRosterFloor"])

defs["Shop"] = obj(OD([
    ("id", ID), ("cardsPerTab", NONNEG), ("rerollCost", NONNEG),
    ("drawModel", obj(OD([("kind", enum("bag", "independent")), ("note", STR)]), ["kind"])),
    ("recruiterNode", obj(OD([("cardsPerTab", NONNEG), ("firstRerollFree", BOOL)]), ["cardsPerTab", "firstRerollFree"])),
    ("otherworld", obj(OD([("cards", NONNEG), ("rerollCost", NONNEG), ("requiresPortal", BOOL)]), ["cards", "rerollCost", "requiresPortal"])),
    ("tiers", arr(obj(OD([("rounds", arr(NONNEG, 2)), ("staff", {"type": "object", "patternProperties": {"^[123]$": NONNEG}, "additionalProperties": False}),
                          ("roomTiles", arr(NONNEG, 1)), ("furnitureRarity", arr(enum("common", "uncommon"), 1))]), ["rounds", "staff", "roomTiles", "furnitureRarity"]), 1)),
    ("roomsOnlyForOwnedFloors", BOOL), ("leases", arr(ID)),
]), ["id", "cardsPerTab", "rerollCost", "drawModel", "recruiterNode", "otherworld", "tiers", "roomsOnlyForOwnedFloors", "leases"])

defs["Mode"] = obj(OD([
    ("id", ID), ("name", STR), ("map", {"oneOf": [{"type": "null"}, ID]}), ("interludes", BOOL),
    ("rivalSource", obj(OD([("scripted", BOOL), ("templates", BOOL), ("ghosts", BOOL)]), ["scripted", "templates", "ghosts"])),
    ("rivalGimmicks", BOOL), ("gimmicksFromRound", {"oneOf": [{"type": "null"}, NONNEG]}),
    ("portalUnlock", {"oneOf": [obj(OD([("condition", enum("bossDefeated")), ("act", NONNEG)]), ["condition", "act"]),
                                obj(OD([("condition", enum("round")), ("round", NONNEG)]), ["condition", "round"])]}),
    ("winBonus", obj(OD([("fight", NONNEG), ("audit", NONNEG), ("boss", NONNEG)]), ["fight", "audit", "boss"])),
    ("strikes", NONNEG), ("rounds", NONNEG), ("metaUnlocks", BOOL), ("rating", BOOL), ("snapshotCapture", BOOL),
    ("drawIsLossWithoutStrike", BOOL), ("firstRunTutorial", BOOL),
]), ["id", "name", "map", "interludes", "rivalSource", "rivalGimmicks", "gimmicksFromRound", "portalUnlock", "winBonus", "strikes", "rounds", "metaUnlocks", "rating", "snapshotCapture", "drawIsLossWithoutStrike", "firstRunTutorial"])

defs["CampaignMap"] = obj(OD([
    ("id", ID),
    ("nodeKinds", {"type": "object", "additionalProperties": obj(OD([("fight", BOOL), ("icon", ref("ManifestId")), ("rivalRoundOffset", NONNEG), ("alwaysGimmick", BOOL), ("choices", NONNEG)]), ["fight", "icon"])}),
    ("acts", arr(obj(OD([("act", NONNEG), ("name", STR), ("columns", {"type": "string", "pattern": "^[FX]*B$"}), ("rounds", arr(NONNEG, 2)),
                         ("boss", ID), ("scriptedFightsFirstRun", arr(ID)), ("consultantAlwaysOffers", ID)]), ["act", "name", "columns", "rounds", "boss"]), 1)),
    ("layout", obj(OD([("rowsPerColumn", arr(NONNEG, 2)), ("bossRows", NONNEG), ("auditsPerAct", arr(NONNEG, 2)), ("auditNeverInFirstColumn", BOOL)]),
                   ["rowsPerColumn", "bossRows", "auditsPerAct", "auditNeverInFirstColumn"])),
]), ["id", "nodeKinds", "acts", "layout"])

defs["Tutorial"] = obj(OD([("id", ID), ("hints", arr(obj(OD([("round", NONNEG), ("text", STR)]), ["round", "text"])))]), ["id", "hints"])

defs["ShopListEntry"] = obj(OD([("defId", ID), ("weight", {"type": "integer", "minimum": 1}), ("minRound", {"type": "integer", "minimum": 1})]), ["defId", "weight", "minRound"])
defs["Template"] = obj(OD([
    ("id", ID), ("archetype", enum(*ARCHETYPES)), ("namePool", arr(STR, 1)), ("budgetPermille", PERMILLE),
    ("rooms", arr(ref("ShopListEntry"), 1)), ("staff", arr(ref("ShopListEntry"), 1)), ("gimmickPool", arr(ID)), ("founderPool", arr(ID, 1)),
    ("layout", obj(OD([("floorPreference", arr(ID, 1)), ("fillOrder", arr(ref("Dept"), 1))]), ["floorPreference", "fillOrder"])),
]), ["id", "archetype", "namePool", "founderPool", "budgetPermille", "rooms", "staff", "gimmickPool", "layout"])
defs["LeaseSchedule"] = obj(OD([("id", ID), ("byRound", {"type": "object", "patternProperties": {"^[0-9]+$": arr(ID)}, "additionalProperties": False})]), ["id", "byRound"])

defs["SnapshotRoom"] = obj(OD([("roomId", STR), ("defId", ID), ("rect", arr(NONNEG, 4)), ("tenureRounds", NONNEG)]), ["roomId", "defId", "rect", "tenureRounds"])
defs["SnapshotOccupant"] = obj(OD([("tile", arr(NONNEG, 2)), ("kind", enum("employee", "furniture")), ("defId", ID), ("instanceId", STR), ("attachments", {"type": "array", "maxItems": 0})]),
                               ["tile", "kind", "defId", "instanceId", "attachments"])
defs["SnapshotFloor"] = obj(OD([("index", {"type": "integer", "minimum": -1, "maximum": 3}), ("grid", ref("Footprint")), ("rooms", arr(ref("SnapshotRoom"))), ("occupants", arr(ref("SnapshotOccupant")))]),
                            ["index", "grid", "rooms", "occupants"])
defs["TowerSnapshot"] = obj(OD([
    ("schemaVersion", {"type": "integer", "minimum": 1}), ("contentVersion", STR), ("round", {"type": "integer", "minimum": 1, "maximum": 16}),
    ("floors", arr(ref("SnapshotFloor"), 1)),
    ("globals", obj(OD([("founderId", ID), ("modifiers", arr(ID)), ("riders", arr(obj(OD([("instanceId", STR), ("riderId", ID)]), ["instanceId", "riderId"]))), ("leasedB1", BOOL)]), ["founderId", "modifiers", "riders", "leasedB1"])),
]), ["schemaVersion", "contentVersion", "round", "floors", "globals"])
defs["ScriptedRival"] = obj(OD([
    ("id", ID), ("name", STR), ("kind", enum("scripted")), ("archetype", enum(*ARCHETYPES)), ("round", {"type": "integer", "minimum": 1, "maximum": 16}),
    ("gimmick", {"oneOf": [{"type": "null"}, ID]}), ("exemptFromBudget", BOOL), ("note", STR), ("snapshot", ref("TowerSnapshot")),
]), ["id", "name", "kind", "archetype", "round", "gimmick", "exemptFromBudget", "note", "snapshot"])

defs["Founder"] = obj(OD([
    ("id", ID), ("name", STR), ("title", STR), ("bio", STR), ("portrait", ref("ManifestId")), ("badge", ref("ManifestId")),
    ("inShop", BOOL), ("effects", ref("Effects")),
]), ["id", "name", "title", "bio", "portrait", "badge", "inShop", "effects"])

defs["Invariant"] = obj(OD([
    ("id", ID), ("name", STR), ("statement", STR), ("population", STR), ("measure", STR), ("comparator", STR),
    ("threshold", {}), ("cadence", enum("commit", "smoke+nightly", "nightly")), ("severity", enum("fail", "warn", "warn+fail")), ("source", STR),
]), ["id", "name", "statement", "population", "measure", "comparator", "threshold", "cadence", "severity", "source"])
defs["Balance"] = obj(OD([
    ("id", ID),
    ("seeds", obj(OD([("smoke", NONNEG), ("nightly", NONNEG), ("search", NONNEG)]), ["smoke", "nightly", "search"])),
    ("smokeRounds", arr({"type": "integer", "minimum": 1, "maximum": 16}, 1)),
    ("populations", {"type": "object", "additionalProperties": {"type": "object", "properties": {"description": STR}, "required": ["description"]}}),
    ("bands", {"type": "object", "additionalProperties": {"oneOf": [INT, arr(INT, 2)]}}),
    ("counters", arr(obj(OD([("winner", enum(*ARCHETYPES)), ("loser", enum(*(ARCHETYPES + ["none"]))), ("why", STR)]), ["winner", "loser", "why"]), 1)),
    ("bossCounters", arr(obj(OD([("boss", ID), ("favoured", enum(*ARCHETYPES)), ("favouredMin", PERMILLE),
                                 ("punished", {"oneOf": [{"type": "null"}, enum(*ARCHETYPES)]}), ("punishedMax", {"oneOf": [{"type": "null"}, PERMILLE]})]),
                             ["boss", "favoured", "favouredMin", "punished", "punishedMax"]), 1)),
    ("invariants", arr(ref("Invariant"), 1)),
]), ["id", "seeds", "smokeRounds", "populations", "bands", "counters", "bossCounters", "invariants"])

# file wrappers
defs["FloorFile"] = obj(OD([("floors", arr(ref("Floor"), 5))]), ["floors"])
defs["StatusFile"] = obj(OD([("statuses", arr(ref("Status"), 1))]), ["statuses"])
defs["EmployeeFile"] = obj(OD([("employees", arr(ref("Employee"), 1))]), ["employees"])
defs["RoomFile"] = obj(OD([("rooms", arr(ref("Room"), 1))]), ["rooms"])
defs["FurnitureFile"] = obj(OD([("furniture", arr(ref("Furniture")))]), ["furniture"])
defs["RecipeFile"] = obj(OD([("recipes", arr(ref("Recipe")))]), ["recipes"])
defs["RiderFile"] = obj(OD([("riders", arr(ref("Rider")))]), ["riders"])
defs["ModifierFile"] = obj(OD([("modifiers", arr(ref("Modifier")))]), ["modifiers"])
defs["ModeFile"] = obj(OD([("modes", arr(ref("Mode"), 1))]), ["modes"])
defs["FounderFile"] = obj(OD([("founders", arr(ref("Founder"), 1))]), ["founders"])
defs["TemplateFile"] = obj(OD([("leaseSchedule", ref("LeaseSchedule")), ("templates", arr(ref("Template"), 1))]), ["leaseSchedule", "templates"])
defs["Index"] = obj(OD([("contentVersion", STR), ("schema", STR), ("files", arr(obj(OD([("path", STR), ("def", STR)]), ["path", "def"]), 1))]), ["contentVersion", "schema", "files"])

schema = OD([
    ("$schema", "https://json-schema.org/draft/2020-12/schema"),
    ("$id", "https://companywars.invalid/schema/content.schema.json"),
    ("title", "Company Wars content database"),
    ("description", "Every content file under content/ validates against one $def named in content/index.json. The enums in Effect are the closed vocabulary: adding a value here is a rules change and a sim change; adding an entity to a content file is not."),
    ("$defs", defs),
])

def main():
    os.makedirs(os.path.join(ROOT, "schema"), exist_ok=True)
    with open(os.path.join(ROOT, "schema", "content.schema.json"), "w") as fh:
        json.dump(schema, fh, indent=2, ensure_ascii=False); fh.write("\n")
    jsonschema.Draft202012Validator.check_schema(schema)
    index = json.load(open(os.path.join(ROOT, "content", "index.json")))
    files = [("index.json", "Index")] + [(f["path"], f["def"]) for f in index["files"]]
    bad = 0
    for path, d in files:
        data = json.load(open(os.path.join(ROOT, "content", path)))
        v = jsonschema.Draft202012Validator({"$ref": "#/$defs/" + d, "$defs": defs})
        errs = sorted(v.iter_errors(data), key=lambda e: list(e.path))
        for e in errs[:5]:
            print("SCHEMA", path, "/".join(str(p) for p in e.path), "-", e.message[:160]); bad += 1
        if len(errs) > 5: print("  ...", len(errs) - 5, "more in", path); bad += len(errs) - 5
    print("schema violations:", bad, "| files validated:", len(files))
    sys.exit(1 if bad else 0)

if __name__ == "__main__":
    main()
