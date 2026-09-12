#!/usr/bin/env python3
"""Generates content/*.json for Company Wars Phase 3 from compact Python data.
Scratchpad tool. The JSON it emits is the deliverable; this script is not."""
import json, os, sys
from collections import OrderedDict as OD

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")), "content")
CONTENT_VERSION = "0.1.0"

# ---------------------------------------------------------------- helpers
def eff(on, do, **kw):
    d = OD([("on", on), ("do", do)])
    d.update(kw)
    return d

def enemy(floor, unit):
    return OD([("side", "enemy"), ("floor", floor), ("unit", unit)])

def own(scope, dept=None, pick=None, tag=None):
    d = OD([("side", "own"), ("scope", scope)])
    if dept: d["dept"] = dept if isinstance(dept, list) else [dept]
    if tag: d["tag"] = tag
    if pick: d["pick"] = pick
    return d

def firm(side):
    return OD([("side", side), ("scope", "firm")])

def subj(scope, dept=None, notDept=None, tag=None):
    d = OD([("scope", scope)])
    if dept: d["dept"] = dept if isinstance(dept, list) else [dept]
    if notDept: d["notDept"] = notDept if isinstance(notDept, list) else [notDept]
    if tag: d["tag"] = tag
    return d

# passives / stats
def stat(name, subject, amount=None, permille=None, **kw):
    d = eff("static", "stat", stat=name, subject=subject)
    if amount is not None: d["amount"] = amount
    if permille is not None: d["permille"] = permille
    d.update(kw)
    return d

def flag(name, subject, **kw):
    d = eff("static", "flag", flag=name, subject=subject)
    d.update(kw)
    return d

SELF = subj("self")

# ---------------------------------------------------------------- rules
rules = OD([
    ("id", "rules.default"),
    ("schemaVersion", 2),
    ("time", OD([("ticksPerSecond", 20), ("quarterTicks", 1200),
                 ("monthStart", [0, 400, 800, 1160]),
                 ("rushMult", [1000, 1400, 2000, 3000]),
                 ("regenMult", [1000, 600, 200, 0])])),
    ("loyalty", OD([("baseCap", OD([("constant", 600), ("perRound", 100)])),
                    ("regenBasePermille", 30), ("regenInterval", 40),
                    ("regenSuppressWindow", 20), ("suppressThresholdPermille", 40)])),
    ("scandal", OD([("interval", 20), ("perStack", 8), ("transferPermille", 375)])),
    ("curse", OD([("selfCostPermille", 250)])),
    ("floors", OD([("corridorMult", 900)])),
    ("tenure", OD([("tierRounds", [3, 6, 10]), ("stepPermille", 100),
                   ("staffedPermille", 500)])),
    ("portal", OD([("receptionCapPerOccupant", 100), ("employeeCapTax", 100),
                   ("b1LeaseCapTax", 150)])),
    ("retrigger", OD([("depthMax", 1)])),
    ("upkeep", OD([("loyaltyPerUnpaidBudget", 100)])),
])

# ---------------------------------------------------------------- statuses
statuses = [
    OD([("id", "status.burnout"), ("name", "Burnout"), ("maxStacks", 5), ("durationTicks", None),
        ("expires", False), ("cooldownRatePermillePerStack", 0),
        ("outputPenaltyPermillePerStack", 50), ("scandalPerStackPerEvent", 8),
        ("onExpire", []), ("tone", "anomalous"),
        ("text", "Stacking. Each second the firm causes itself a Scandal per stack; the employee's output falls 5% per stack. Never expires in a fight.")]),
    OD([("id", "status.overtime"), ("name", "Overtime"), ("maxStacks", 2), ("durationTicks", 60),
        ("expires", True), ("cooldownRatePermillePerStack", 500),
        ("outputPenaltyPermillePerStack", 0), ("scandalPerStackPerEvent", 0),
        ("onExpire", [OD([("status", "status.burnout"), ("stacks", 1)])]), ("tone", "operations"),
        ("text", "Haste. +50% cooldown speed per stack for 3s. When a stack expires the employee gains 1 Burnout.")]),
    OD([("id", "status.bureaucracy"), ("name", "Bureaucracy"), ("maxStacks", 3), ("durationTicks", 100),
        ("expires", True), ("cooldownRatePermillePerStack", -200),
        ("outputPenaltyPermillePerStack", 0), ("scandalPerStackPerEvent", 0),
        ("onExpire", []), ("tone", "interface"),
        ("text", "Slow. -20% cooldown speed per stack; each stack lasts 5s independently.")]),
    OD([("id", "status.frozen"), ("name", "Frozen"), ("maxStacks", 1), ("durationTicks", None),
        ("expires", True), ("cooldownRatePermillePerStack", -1000),
        ("outputPenaltyPermillePerStack", 0), ("scandalPerStackPerEvent", 0),
        ("onExpire", []), ("tone", "interface"),
        ("text", "Cooldown does not advance. Duration set by the applying ability; re-application extends.")]),
]

# ---------------------------------------------------------------- floors
floors = [
    OD([("id", "floor.b1"), ("index", -1), ("name", "B1 — Portal"), ("kind", "portal"),
        ("grid", OD([("w", 3), ("h", 3)])), ("outputPermille", 1000),
        ("roomKinds", ["extraplanar"]), ("upkeepBudget", 0), ("capTax", 150), ("lease", 20),
        ("starting", False), ("requiresPortal", True), ("targetable", False)]),
    OD([("id", "floor.g"), ("index", 0), ("name", "G — Reception"), ("kind", "reception"),
        ("grid", OD([("w", 5), ("h", 3)])), ("outputPermille", 900),
        ("roomKinds", ["reception", "security", "general"]), ("upkeepBudget", 0), ("capTax", 0), ("lease", 0),
        ("starting", True), ("requiresPortal", False), ("targetable", True),
        ("fixedRooms", [OD([("defId", "room.reception"), ("rect", [1, 0, 2, 2])])])]),
    OD([("id", "floor.f1"), ("index", 1), ("name", "1F — Operations"), ("kind", "operations"),
        ("grid", OD([("w", 5), ("h", 3)])), ("outputPermille", 1000),
        ("roomKinds", ["general"]), ("upkeepBudget", 0), ("capTax", 0), ("lease", 0),
        ("starting", True), ("requiresPortal", False), ("targetable", True)]),
    OD([("id", "floor.f2"), ("index", 2), ("name", "2F — Operations"), ("kind", "operations"),
        ("grid", OD([("w", 5), ("h", 3)])), ("outputPermille", 1150),
        ("roomKinds", ["general"]), ("upkeepBudget", 2), ("capTax", 0), ("lease", 28),
        ("starting", False), ("requiresPortal", False), ("targetable", True)]),
    OD([("id", "floor.f3"), ("index", 3), ("name", "3F — Executive"), ("kind", "executive"),
        ("grid", OD([("w", 4), ("h", 2)])), ("outputPermille", 1450),
        ("roomKinds", ["executive", "general"]), ("upkeepBudget", 4), ("capTax", 0), ("lease", 36),
        ("starting", False), ("requiresPortal", False), ("targetable", True)]),
]

# ---------------------------------------------------------------- economy
economy = OD([
    ("id", "economy.default"),
    ("startingBudget", 12),
    ("income", OD([("constant", 9), ("perTwoRounds", 1),
                   ("table", [10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17])])),
    ("winBonus", OD([("fight", 3), ("audit", 5), ("boss", 5)])),
    ("employeeCost", OD([("1", 3), ("2", 6), ("3", 10), ("extraplanar", 5)])),
    ("severance", OD([("1", 1), ("2", 2), ("3", 3), ("extraplanar", 4)])),
    ("roomCostByTiles", OD([("2", 5), ("4", 9), ("6", 13)])),
    ("furnitureCost", OD([("common", 2), ("uncommon", 4)])),
    ("rerollCost", 1),
    ("renovationFee", "currentRoundIncome"),
    ("relocationFee", "currentRoundIncome"),
    ("relocationTenurePenaltyRounds", 3),
    ("furnitureSellRefund", 0),
    ("startingRoster", ["emp.junior_dev", "emp.junior_dev"]),
    ("startingRosterFloor", "floor.f1"),
])

# ---------------------------------------------------------------- employees
E = []
def emp(id, name, dept, tier, cost, cd, effects, tags=(), extra=False, inShop=True,
        placement=None, cdByMonth=None, initial=0, flavor="", countsAs=None):
    d = OD([("id", id), ("name", name), ("dept", dept), ("tier", tier), ("cost", cost),
            ("extraplanar", extra), ("inShop", inShop), ("tags", list(tags)),
            ("cooldownTicks", cd)])
    if cdByMonth: d["cooldownTicksByMonth"] = cdByMonth
    d["initialProgressPermille"] = initial
    if placement: d["placement"] = placement
    if countsAs: d["countsAsDept"] = countsAs
    d["effects"] = effects
    d["sprite"] = "emp." + id.split(".", 1)[1]
    d["flavor"] = flavor
    E.append(d)

ENG, LEG, HR, SAL, MGT, XP = "engineering", "legal", "hr", "sales", "management", "extraplanar"
# The revenue race (D-85): Sales earn for your own firm; Poach and Curse take from the rival's; PR rebuilds Loyalty.
def sales(v, **kw): return eff("ability", "sales", value=v, target=firm("own"), **kw)
def poach(v, **kw): return eff("ability", "poach", value=v, target=firm("enemy"), **kw)
def curse(v, **kw): return eff("ability", "curse", value=v, target=firm("enemy"), **kw)
def pr(v, **kw): return eff("ability", "pr", value=v, target=firm("own"), **kw)
# A room or modifier that boosted Push for everyone boosts both Sales and Poach; one that names a department boosts what it does.
def output(subject, **kw): return [stat("sales", subject, **kw), stat("poach", subject, **kw)]
def status(st, n, target, on="ability", **kw): return eff(on, "status", status=st, stacks=n, target=target, **kw)
def cleanse(st, n, target, on="ability", **kw): return eff(on, "cleanse", status=st, stacks=n, target=target, **kw)
def retrig(target, on="ability", **kw): return eff(on, "retrigger", target=target, **kw)
def after(effect, everyN=1):
    effect["on"] = "afterFire"; effect["everyN"] = everyN; return effect
BUR, OVT, BRN, FRZ = "status.bureaucracy", "status.overtime", "status.burnout", "status.frozen"
HI_HI = enemy("highest_occupied_floor", "highest_base_value")
MP_LO = enemy("most_populated_floor", "lowest_cooldown_remaining")

# Engineering
emp("emp.intern", "Intern", ENG, 1, 2, 60, [sales(35)], tags=("junior",),
    flavor="Unpaid, in the sense that the invoice is emotional.")
emp("emp.junior_dev", "Junior Developer", ENG, 1, 3, 80, [sales(60, name="Ship Feature")], tags=("junior",),
    flavor="Ships it. Whether it works is a Month 2 problem.")
emp("emp.qa_tester", "QA Tester", ENG, 1, 3, 40, [sales(25, name="Bug Report")],
    flavor="Small, frequent, and never quite enough to register.")
emp("emp.senior_dev", "Senior Developer", ENG, 2, 6, 80, [sales(150, name="Ship Feature")],
    cdByMonth=OD([("2", 60)]), flavor="Accelerates during Crunch. This is not a compliment.")
emp("emp.devops", "DevOps", ENG, 2, 6, 120,
    [sales(90, name="Deploy"), after(status(OVT, 1, own("adjacent", dept=ENG)))],
    flavor="Every deploy is someone else's overtime.")
emp("emp.sysadmin", "Sysadmin", ENG, 2, 6, 100, [sales(100, name="Reboot"), flag("bureaucracyImmune", SELF)],
    flavor="Cannot be slowed. Has never once been in a meeting.")
emp("emp.architect", "Architect", ENG, 3, 10, 160,
    [sales(350, name="Refactor"), after(status(OVT, 1, own("adjacent", dept=ENG)), everyN=3)],
    flavor="Every third Refactor is the one that makes everyone stay late.")
emp("emp.cto", "CTO", ENG, 3, 10, 200,
    [sales(250, name="Roadmap"), stat("cooldown", subj("sameFloor", dept=ENG), permille=900)],
    flavor="Technical leadership: everyone on the floor works 10% faster and 100% more anxiously.")

# Legal
emp("emp.paralegal", "Paralegal", LEG, 1, 3, 60,
    [sales(40, name="Billable Hours"), after(status(BUR, 1, MP_LO)), stat("loyaltyCap", SELF, amount=100)],
    flavor="Bills an hour, then generates forms. The forms are the weapon.")
emp("emp.compliance_officer", "Compliance Officer", LEG, 1, 3, 80,
    [sales(40, name="Billable Hours"), after(cleanse(BUR, 1, own("adjacent"))), stat("loyaltyCap", SELF, amount=150)],
    flavor="Bills for unblocking the people next to them. Files a report about it.")
emp("emp.counsel", "Counsel", LEG, 2, 6, 120,
    [poach(90, name="Cease & Desist"), after(status(BUR, 2, HI_HI)),
     stat("loyaltyCap", SELF, amount=250), stat("regenPerEvent", SELF, amount=40)],
    flavor="Sends a letter. The letter has consequences.")
emp("emp.patent_attorney", "Patent Attorney", LEG, 2, 6, 140,
    [poach(130, name="Litigation"), stat("loyaltyCap", SELF, amount=150)],
    flavor="Legal that attacks. Rare, expensive, and very slow.")
emp("emp.general_counsel", "General Counsel", LEG, 3, 10, 200,
    [sales(200, name="Billable Hours"), after(status(FRZ, 1, HI_HI, durationTicks=60)),
     stat("loyaltyCap", SELF, amount=500), stat("regenPerEvent", SELF, amount=80)],
    flavor="Freezes their best person for three seconds. Bills for six.")

# HR
emp("emp.recruiter", "Recruiter", HR, 1, 3, 100,
    [pr(80, name="Team Building"), stat("regenPerEvent", SELF, amount=30)],
    flavor="Rebuilds Loyalty. Has a lanyard.")
emp("emp.office_manager", "Office Manager", HR, 1, 3, 120,
    [cleanse(BRN, 1, own("adjacent"), name="Snack Run"), stat("loyaltyCap", SELF, amount=50)],
    flavor="Removes one Burnout from each neighbour. Knows where the good stapler is.")
emp("emp.hr_manager", "HR Manager", HR, 2, 6, 120,
    [pr(150, name="Wellness Program"), after(cleanse(BRN, 2, own("sameFloor")))],
    flavor="A wellness program is a Burnout cleanse with a budget line.")
emp("emp.trainer", "Trainer", HR, 2, 6, 160,
    [status(OVT, 1, own("adjacent"), name="Workshop"), stat("regenPerEvent", SELF, amount=20)],
    flavor="Everyone leaves the workshop energised. For three seconds.")
emp("emp.head_of_people", "Head of People", HR, 3, 10, 160,
    [pr(300, name="Retention Bonus"), stat("burnoutMaxOverride", subj("all"), amount=3)],
    flavor="Nobody in the building can burn out past three stacks. The fourth was never on the table.")

# Sales
emp("emp.telemarketer", "Telemarketer", SAL, 1, 2, 40,
    [sales(20, name="Cold Call"), eff("economy", "stat", stat="income", subject=SELF, amount=1)],
    flavor="¥20 every two seconds. Never a big number. That is the joke.")
emp("emp.sales_rep", "Sales Rep", SAL, 1, 3, 60,
    [sales(40, name="Cold Call"), eff("economy", "stat", stat="income", subject=SELF, amount=1)],
    flavor="Grows the budget between fights and the Revenue during them.")
emp("emp.account_manager", "Account Manager", SAL, 2, 6, 100,
    [poach(90, name="Steal the Account"), eff("economy", "stat", stat="income", subject=SELF, amount=2)],
    flavor="Takes the rival's biggest client to lunch. Expenses it.")
emp("emp.headhunter", "Headhunter", SAL, 2, 6, 140,
    [status(BRN, 2, HI_HI, name="Job Offer"), eff("economy", "stat", stat="income", subject=SELF, amount=1)],
    flavor="Burns out their best person by offering them a job. A Scandal does not appear on a balance sheet until it does.")
emp("emp.sales_director", "Sales Director", SAL, 3, 10, 240,
    [sales(OD([("base", 300), ("perTag", "sales"), ("each", 50)]), name="Quarterly Target"),
     eff("economy", "stat", stat="income", subject=SELF, amount=3)],
    flavor="Hits a number that grows with every salesperson in the building.")
emp("emp.key_account_manager", "Key Account Manager", SAL, 3, 10, 160,
    [sales(200, name="Enterprise Deal"), after(pr(100)),
     eff("economy", "stat", stat="income", subject=SELF, amount=2)],
    flavor="A deal so large it improves the balance sheet on the way out.")

# Management
emp("emp.team_lead", "Team Lead", MGT, 1, 3, 120,
    [retrig(own("adjacent", pick="highest_base_value"), name="Delegate"), after(pr(60))],
    flavor="Makes the best person next to them do it again, then tells the client it was the plan.")
emp("emp.project_manager", "Project Manager", MGT, 1, 3, 100,
    [status(BUR, 1, enemy("same_floor_index", "lowest_cooldown_remaining"), name="Scope Creep")],
    flavor="Slows down the rival's mirror floor with requirements.")
emp("emp.middle_manager", "Middle Manager", MGT, 2, 6, 160,
    [status(OVT, 1, own("adjacent"), name="Standup")],
    flavor="Everyone adjacent works faster. Everyone adjacent burns out. Both are the standup.")
emp("emp.consultant", "Consultant", MGT, 2, 6, 180,
    [status(BRN, 1, enemy("most_populated_floor", "all"), name="Efficiency Review")],
    flavor="Burns out an entire floor of the rival's building. Invoices separately.")
emp("emp.director", "Director", MGT, 3, 10, 200,
    [retrig(own("adjacent"), name="Reorg", then=OD([("status", BRN), ("stacks", 1)]))],
    flavor="Everyone adjacent fires, then gains Burnout. Fire-then-burn, per person.")
emp("emp.vp_operations", "VP Operations", MGT, 3, 10, 240,
    [retrig(own("sameFloor"), name="All-Hands")],
    flavor="The whole floor fires again. No Burnout. That is what the extra four seconds buy.")

# Extraplanar — shop
emp("emp.x_salaryman_ghost", "Salaryman Ghost", XP, 2, 5, 60,
    [curse(90, name="Overtime Eternal"), flag("overtimePermanent", SELF)], extra=True,
    flavor="Holds two Overtime forever and never burns out. He did that already.")
emp("emp.x_office_lady", "Office Lady of the Third Floor", XP, 2, 5, 120,
    [curse(60, name="Filing"), after(status(BUR, 1, enemy("same_floor_index", "lowest_cooldown_remaining")))],
    extra=True, placement=OD([("floors", ["floor.f3"])]),
    flavor="Must be placed on 3F. Nobody has asked why.")
emp("emp.x_auditor", "The Auditor", XP, 3, 5, 240,
    [curse(OD([("permilleOfTargetCap", 150)]), name="Audit"), flag("cannotBeRetriggered", SELF)],
    extra=True, flavor="Curses 15% of the rival's Loyalty cap in ¥ straight out of their Revenue. Cannot be hurried.")
emp("emp.x_fax_spirit", "Fax Spirit", XP, 2, 5, 100,
    [curse(40, name="Transmission"), after(retrig(own("adjacent", dept=LEG, pick="highest_base_value")))],
    extra=True, flavor="A fax machine that died and kept working.")
emp("emp.x_kappa_intern", "Kappa Intern", XP, 1, 5, 40,
    [curse(30, name="Splash"), stat("regenPerEvent", SELF, amount=40)], extra=True,
    flavor="Keeps the water cooler full. Do not ask what with.")
# Extraplanar — ritual results
emp("emp.x_salaryman_who_never_left", "Salaryman Who Never Left", XP, 3, 0, 80,
    [curse(200, name="Loyalty"), flag("bureaucracyImmune", SELF), flag("frozenImmune", SELF)],
    extra=True, inShop=False, countsAs=ENG, tags=("ritual",),
    flavor="Counts as Engineering for rooms. Immune to Bureaucracy and Frozen. Has a desk.")
emp("emp.x_the_chairman", "The Chairman", XP, 3, 0, 300,
    [curse(600, name="Board Resolution"), stat("loyaltyCap", SELF, amount=-300)],
    extra=True, inShop=False, tags=("ritual",),
    flavor="A ¥600 Curse every fifteen seconds. The firm's Loyalty pays for the chair.")
emp("emp.x_the_partner", "The Partner", XP, 3, 0, 160,
    [curse(250, name="Equity"), stat("loyaltyCap", SELF, amount=-200)],
    extra=True, inShop=False, tags=("ritual",), flavor="Made partner. Nobody remembers the vote.")
emp("emp.x_efficiency_wraith", "Efficiency Wraith", XP, 2, 0, 100,
    [curse(50, name="Review"), after(status(BRN, 1, enemy("most_populated_floor", "all")))],
    extra=True, inShop=False, tags=("ritual",), flavor="A Consultant that never left either.")
emp("emp.x_recruiting_oni", "Recruiting Oni", XP, 2, 0, 140,
    [curse(80, name="Offer"), after(status(BRN, 2, HI_HI))],
    extra=True, inShop=False, tags=("ritual",), flavor="The offer is very good. The offer is not optional.")

employees = E

# ---------------------------------------------------------------- rooms
R = []
def room(id, name, w, h, floors_, cost, effects, kind="general", landing=True, fixed=False,
         maxOcc=None, tiles=None, flavor=""):
    d = OD([("id", id), ("name", name), ("kind", kind),
            ("footprint", OD([("w", w), ("h", h)])), ("floors", floors_),
            ("landingLegal", landing), ("cost", cost), ("fixed", fixed)])
    if maxOcc: d["maxOccupants"] = maxOcc
    d["effects"] = effects
    d["tile"] = tiles or ("room." + id.split(".", 1)[1] + ".tile")
    d["flavor"] = flavor
    R.append(d)

OCC = subj("occupants")
room("room.reception", "Reception", 2, 2, ["floor.g"], 0, [
    stat("loyaltyCap", OCC, amount=100),
    *output(OCC,permille=800),
    stat("regenPerEvent", OCC, amount=30, fromTier=3),
], kind="reception", fixed=True, flavor="Where client loyalty lives. Every person at the desk is +100 Loyalty.")
room("room.open_plan", "Open Plan Office", 2, 3, ["floor.f1", "floor.f2"], 13, [
    stat("sales", subj("occupants", dept=ENG),permille=1200),
    stat("cooldown", OCC, permille=900, fromTier=3),
], flavor="Engineering ×1.2. At Tier III everyone in it works faster, because the walls came down.")
room("room.server_room", "Server Room", 2, 2, ["floor.f1", "floor.f2"], 9, [
    stat("sales", subj("occupants", dept=ENG),permille=1350),
    status(BRN, 1, own("occupants"), on="banner", month=1, untilTier=3),
    status(BRN, 1, own("occupants"), on="banner", month=2, untilTier=3),
], flavor="Engineering ×1.35. It is hot in there; occupants burn at each month until the room is Institutional.")
room("room.legal_dept", "Legal Department", 2, 2, ["floor.f1", "floor.f2", "floor.f3"], 9, [
    stat("passiveMult", subj("occupants", dept=LEG), permille=1500),
    stat("statusStacksBonus", subj("occupants", dept=LEG), amount=1, status=BUR),
    flag("capProtected", OCC, fromTier=3),
], flavor="Legal passives ×1.5 and every Bureaucracy they apply gains a stack. Tier III: their Loyalty cannot be eroded.")
room("room.sales_floor", "Sales Floor", 2, 2, ["floor.f1", "floor.f2"], 9, [
    stat("sales", subj("occupants", dept=SAL),permille=1200),
    eff("economy", "stat", stat="income", subject=subj("occupants", dept=SAL), amount=1),
    status(BUR, 1, MP_LO, on="afterFire", everyN=1, subject=subj("occupants", dept=SAL), fromTier=3),
], flavor="Sales ×1.2 and +¥1 each. Tier III: their calls also slow someone down.")
room("room.break_room", "Break Room", 1, 2, ["floor.g", "floor.f1", "floor.f2", "floor.f3"], 5, [
    *output(subj("occupants", notDept=HR), permille=500),
    flag("burnoutImmune", subj("occupants", notDept=HR)),
    stat("pr", subj("occupants", dept=HR), permille=1500),
    cleanse(BRN, 1, own("adjacent"), on="periodic", every=200, subject=OCC, fromTier=3),
], flavor="Non-HR occupants do half the work and cannot burn out. HR's PR goes half again as far.")
room("room.security_desk", "Security Desk", 1, 2, ["floor.g"], 5, [
    flag("untargetable", OCC),
    flag("bureaucracyImmune", OCC, fromTier=3),
], kind="security", landing=False, flavor="Occupants cannot be selected by the rival's abilities.")
room("room.boardroom", "Boardroom", 2, 2, ["floor.f3"], 9, [
    *output(OCC,permille=1200),
    flag("wholeFloorAdjacency", subj("occupants", dept=MGT)),
    status(OVT, 1, own("adjacent"), on="afterFire", everyN=1, subject=subj("occupants", dept=MGT), fromTier=3),
], kind="executive", flavor="Everyone ×1.2. Management retriggers reach the whole floor. Tier III: every retrigger is also a Standup.")
room("room.corner_office", "Corner Office", 1, 2, ["floor.f3"], 5, [
    *output(OCC,permille=1600),
    stat("loyaltyCap", OCC, amount=-100, untilTier=3),
], kind="executive", maxOcc=1, flavor="One person, ×1.6, and −100 Loyalty for the ego until Tier III.")
room("room.summoning_circle", "Summoning Circle", 2, 2, ["floor.b1"], 9, [
    stat("curseSelfCost", subj("occupants", dept=XP), permille=-125),
    status(BRN, 1, HI_HI, on="afterFire", everyN=1, subject=subj("occupants", dept=XP), fromTier=3),
], kind="extraplanar", flavor="Halves the Curse self-cost. Required by every Ritual.")
room("room.meeting_room", "Meeting Room", 2, 2, ["floor.f1", "floor.f2"], 9, [
    *output(OCC,permille=1100),
    stat("cooldown", subj("occupants", dept=MGT), permille=850),
    stat("cooldown", OCC, permille=900, fromTier=3),
], flavor="Everyone ×1.1 and Management 15% faster. Where Team Leads become Middle Managers.")
room("room.mail_room", "Mail Room", 1, 2, ["floor.g", "floor.f1"], 5, [
    stat("cooldown", OCC, permille=900),
    *output(OCC,permille=900),
    stat("cooldown", OCC, permille=900, fromTier=3),
], flavor="Things move quickly and hit lightly.")
room("room.kitchenette", "Kitchenette", 1, 2, ["floor.g", "floor.f1", "floor.f2", "floor.f3"], 5, [
    stat("regenPerEvent", subj("occupants", dept=HR), amount=25),
    stat("burnoutMaxOverride", OCC, amount=3),
    stat("regenPerEvent", OCC, amount=15, fromTier=3),
], flavor="HR regenerates; nobody in it can burn past three stacks.")
room("room.training_room", "Training Room", 2, 2, ["floor.f1", "floor.f2"], 9, [
    *output(OCC,permille=900),
    status(OVT, 1, own("occupants"), on="periodic", every=100, subject=OCC),
    cleanse(BRN, 1, own("occupants"), on="periodic", every=200, subject=OCC, fromTier=3),
], flavor="Overtime for everyone every five seconds. A Burnout engine unless HR is nearby.")
room("room.executive_lounge", "Executive Lounge", 2, 2, ["floor.f3"], 9, [
    stat("loyaltyCap", OCC, amount=150),
    *output(OCC,permille=800),
    stat("regenPerEvent", OCC, amount=20, fromTier=3),
], kind="executive", flavor="The fortress's executive floor. +150 Loyalty per occupant, ×0.8 output.")
room("room.archive", "Archive", 1, 2, ["floor.b1"], 5, [
    stat("loyaltyCap", OCC, amount=150),
    stat("curse", OCC, permille=800),
    flag("untargetable", OCC, fromTier=3),
], kind="extraplanar", flavor="Offsets the portal tax. Nothing in the archive works very hard.")
rooms = R

# ---------------------------------------------------------------- furniture
F = []
def furn(id, name, w, h, cost, effects, wall=False, floors_=None, rarity="common", flavor=""):
    d = OD([("id", id), ("name", name), ("footprint", OD([("w", w), ("h", h)])),
            ("wallMounted", wall), ("cost", cost), ("rarity", rarity)])
    if floors_: d["floors"] = floors_
    d["effects"] = effects
    d["sprite"] = "furn." + id.split(".", 1)[1]
    d["flavor"] = flavor
    F.append(d)
ADJ = subj("adjacent")
furn("furn.whiteboard", "Whiteboard", 1, 1, 2, [stat("flatSales", subj("adjacent", dept=ENG), amount=15)],
     wall=True, flavor="+¥15 Sales to adjacent Engineering. Recipe input.")
furn("furn.pc_90s", "90s PC", 1, 1, 2, [stat("cooldown", subj("adjacent", dept=ENG), permille=900)],
     flavor="Adjacent Engineering 10% faster. Beige.")
furn("furn.filing_cabinet", "Filing Cabinet", 1, 1, 2, [stat("loyaltyCap", subj("adjacent", dept=LEG), amount=60)],
     flavor="+60 Loyalty per adjacent Legal. Recipe input.")
furn("furn.fax_machine", "Fax Machine", 1, 1, 4,
     [retrig(own("adjacent", dept=LEG, pick="highest_base_value"), on="periodic", every=120)],
     rarity="uncommon", flavor="Every 6s the adjacent Legal with the highest value fires again.")
furn("furn.water_cooler", "Water Cooler", 1, 1, 2,
     [cleanse(BRN, 1, own("adjacent"), on="periodic", every=200)], flavor="Every 10s, −1 Burnout to each neighbour. Recipe input.")
furn("furn.yakult_cart", "Yakult Cart", 1, 1, 4, [status(OVT, 1, own("adjacent"), on="banner", month=0)],
     rarity="uncommon", flavor="At Quarter Open, everyone adjacent gets Overtime. The hangover is scheduled.")
furn("furn.monitoring_station", "Monitoring Station", 1, 2, 4,
     [eff("static", "override", override="floorSelector", to="most_populated_floor", subject=ADJ)],
     rarity="uncommon", flavor="Adjacent employees aim their statuses at the most populated floor.")
furn("furn.executive_desk", "Executive Desk", 1, 2, 4, [stat("retriggerBonus", subj("adjacent", dept=MGT), permille=1250)],
     floors_=["floor.f3"], rarity="uncommon", flavor="Adjacent Management retriggers ×1.25.")
furn("furn.ofuda", "Ofuda", 1, 1, 2, [stat("curseSelfCost", subj("adjacent", dept=XP), permille=-125)],
     wall=True, floors_=["floor.b1"], flavor="Halves the Curse self-cost of adjacent extraplanar staff. Stacks with the Circle to zero.")
furn("furn.desk_phone", "Desk Phone", 1, 1, 2, [stat("flatSales", subj("adjacent", dept=SAL), amount=10)],
     flavor="+¥10 Sales to adjacent Sales staff.")
furn("furn.shredder", "Shredder", 1, 1, 4, [cleanse(BUR, 1, own("adjacent"), on="periodic", every=160)],
     rarity="uncommon", flavor="Every 8s, −1 Bureaucracy to each neighbour.")
furn("furn.potted_plant", "Potted Plant", 1, 1, 2, [stat("loyaltyCap", ADJ, amount=30)],
     flavor="+30 Loyalty per neighbour. Dies mysteriously in round 3 of the first run.")
furn("furn.copier", "Copier", 1, 1, 4, [retrig(own("self"), on="afterFire", everyN=4, subject=ADJ)],
     rarity="uncommon", flavor="Every fourth time a neighbour fires, it fires again.")
furn("furn.vending_machine", "Vending Machine", 1, 1, 2, [stat("burnoutMaxDelta", ADJ, amount=-1)],
     flavor="Neighbours burn out one stack less. Snacks.")
furniture = F

# ---------------------------------------------------------------- recipes
RC = []
def inp(match, count=1, consumed=True, adjacency="adjacent"):
    d = OD([("match", match), ("count", count), ("consumed", consumed), ("adjacency", adjacency)])
    return d
def byId(i): return OD([("defId", i)])
def byTier(t, kind="employee"): return OD([("kind", kind), ("tier", t)])
def recipe(id, name, cls, inputs, result, context=None, hint=""):
    d = OD([("id", id), ("name", name), ("class", cls), ("inputs", inputs)])
    d["context"] = OD([("room", context)]) if context else None
    d["result"] = result
    d["codexHint"] = hint
    RC.append(d)
def toEmp(i): return OD([("kind", "employee"), ("defId", i)])
def toFurn(i): return OD([("kind", "furniture"), ("defId", i)])
def roomTier(t): return OD([("kind", "roomTier"), ("tier", t)])
def roomTenure(n): return OD([("kind", "roomTenure"), ("rounds", n)])

P = "promotion"; RN = "renovation"; RT = "ritual"
recipe("recipe.junior_dev", "Graduate Scheme", P, [inp(byId("emp.intern"), 2), inp(byId("furn.pc_90s"), consumed=False)], toEmp("emp.junior_dev"), hint="Two interns and something to type on.")
recipe("recipe.senior_dev", "Promotion Cycle", P, [inp(byId("emp.junior_dev"), 2), inp(byId("furn.whiteboard"), consumed=False)], toEmp("emp.senior_dev"), hint="Two juniors and a whiteboard.")
recipe("recipe.sysadmin", "Nobody Else Would", P, [inp(byId("emp.junior_dev")), inp(byId("emp.qa_tester"))], toEmp("emp.sysadmin"), hint="A developer and a tester, left alone together.")
recipe("recipe.architect", "Ivory Tower", P, [inp(byId("emp.senior_dev"), 2)], toEmp("emp.architect"), context="room.server_room", hint="Two seniors, somewhere hot.")
recipe("recipe.cto", "Technical Leadership", P, [inp(byId("emp.senior_dev")), inp(byId("emp.devops"))], toEmp("emp.cto"), context="room.open_plan", hint="A senior and a deployer, in the open.")
recipe("recipe.devops", "Ops Now Too", P, [inp(byId("emp.junior_dev")), inp(byId("emp.team_lead"))], toEmp("emp.devops"), hint="A junior who was delegated to once too often.")
recipe("recipe.counsel", "Called to the Bar", P, [inp(byId("emp.paralegal"), 2), inp(byId("furn.filing_cabinet"), consumed=False)], toEmp("emp.counsel"), hint="Two paralegals and a cabinet.")
recipe("recipe.general_counsel", "Partner Track", P, [inp(byId("emp.counsel"), 2)], toEmp("emp.general_counsel"), context="room.legal_dept", hint="Two Counsel, in their department.")
recipe("recipe.patent_attorney", "Prior Art", P, [inp(byId("emp.paralegal")), inp(byId("emp.qa_tester"))], toEmp("emp.patent_attorney"), hint="Someone who reads contracts and someone who reads code.")
recipe("recipe.compliance_officer", "Box-Ticking", P, [inp(byId("emp.paralegal")), inp(byId("emp.office_manager"))], toEmp("emp.compliance_officer"), hint="Legal and the person with the keys.")
recipe("recipe.account_manager", "Quota", P, [inp(byId("emp.sales_rep"), 2)], toEmp("emp.account_manager"), context="room.sales_floor", hint="Two reps on the floor.")
recipe("recipe.headhunter", "Poaching Licence", P, [inp(byId("emp.sales_rep")), inp(byId("emp.paralegal"))], toEmp("emp.headhunter"), hint="Sales with legal cover.")
recipe("recipe.sales_director", "Pipeline", P, [inp(byId("emp.account_manager"), 2)], toEmp("emp.sales_director"), context="room.sales_floor", hint="Two closers on the floor.")
recipe("recipe.key_account_manager", "Enterprise", P, [inp(byId("emp.account_manager")), inp(byId("emp.counsel"))], toEmp("emp.key_account_manager"), hint="A closer and a lawyer.")
recipe("recipe.hr_manager", "Wellness Initiative", P, [inp(byId("emp.recruiter")), inp(byId("furn.water_cooler"))], toEmp("emp.hr_manager"), context="room.break_room", hint="A recruiter, a cooler, a break room. The cooler does not survive.")
recipe("recipe.trainer", "Onboarding", P, [inp(byId("emp.recruiter")), inp(byId("emp.project_manager"))], toEmp("emp.trainer"), hint="HR and a planner.")
recipe("recipe.head_of_people", "People Function", P, [inp(byId("emp.hr_manager"), 2)], toEmp("emp.head_of_people"), context="room.kitchenette", hint="Two HR managers, near the kettle.")
recipe("recipe.director", "Succession", P, [inp(byId("emp.team_lead")), inp(byTier(2))], toEmp("emp.director"), context="room.boardroom", hint="A Team Lead and any Tier 2, in the boardroom. The Tier 2 does not survive.")
recipe("recipe.middle_manager", "Alignment", P, [inp(byId("emp.team_lead"), 2)], toEmp("emp.middle_manager"), context="room.meeting_room", hint="Two leads, one meeting.")
recipe("recipe.consultant", "External Perspective", P, [inp(byId("emp.project_manager")), inp(byId("emp.paralegal"))], toEmp("emp.consultant"), hint="A planner and a lawyer become something worse.")
recipe("recipe.vp_operations", "Reorganisation", P, [inp(byId("emp.middle_manager"), 2)], toEmp("emp.vp_operations"), context="room.boardroom", hint="Two middle managers, upstairs.")

recipe("recipe.monitoring_station", "Situational Awareness", RN, [inp(byId("furn.whiteboard"), 2)], toFurn("furn.monitoring_station"), hint="Two whiteboards.")
recipe("recipe.break_room_iii", "Culture", RN, [inp(byId("furn.water_cooler")), inp(byId("furn.yakult_cart"))], roomTier(3), context="room.break_room", hint="Drinks in the break room.")
recipe("recipe.shredder", "Document Retention", RN, [inp(byId("furn.filing_cabinet"), 2)], toFurn("furn.shredder"), hint="Two cabinets.")
recipe("recipe.copier", "Multifunction", RN, [inp(byId("furn.pc_90s")), inp(byId("furn.fax_machine"))], toFurn("furn.copier"), hint="A computer and a fax.")
recipe("recipe.fax_machine", "Switchboard", RN, [inp(byId("furn.desk_phone"), 2)], toFurn("furn.fax_machine"), hint="Two phones.")
recipe("recipe.reception_tenure", "Front of House", RN, [inp(byId("furn.potted_plant"), 2)], roomTenure(3), context="room.reception", hint="Greenery at the desk.")
recipe("recipe.legal_dept_iii", "Precedent", RN, [inp(byId("furn.filing_cabinet")), inp(byId("furn.pc_90s"))], roomTier(3), context="room.legal_dept", hint="Paper and a database, in Legal.")
recipe("recipe.open_plan_tenure", "Standing Desks", RN, [inp(byId("furn.whiteboard")), inp(byId("furn.pc_90s"))], roomTenure(3), context="room.open_plan", hint="Board and machine, in the open.")
recipe("recipe.corner_office_iii", "Made It", RN, [inp(byId("furn.executive_desk")), inp(byId("furn.potted_plant"))], roomTier(3), context="room.corner_office", hint="A desk and a plant, in the corner.")
recipe("recipe.kitchenette_iii", "Free Snacks", RN, [inp(byId("furn.vending_machine")), inp(byId("furn.water_cooler"))], roomTier(3), context="room.kitchenette", hint="Snacks and water, in the kitchen.")
recipe("recipe.sales_floor_tenure", "Boiler Room", RN, [inp(byId("furn.yakult_cart")), inp(byId("furn.desk_phone"))], roomTenure(3), context="room.sales_floor", hint="A drink and a phone, on the floor.")
recipe("recipe.summoning_circle_iii", "Consecration", RN, [inp(byId("furn.ofuda"), 2)], roomTier(3), context="room.summoning_circle", hint="Two charms, in the circle.")

recipe("recipe.x_never_left", "Retention", RT, [inp(byId("emp.junior_dev")), inp(byId("emp.x_salaryman_ghost"))], toEmp("emp.x_salaryman_who_never_left"), context="room.summoning_circle", hint="A junior and a ghost.")
recipe("recipe.x_chairman", "Quorum", RT, [inp(byId("emp.x_salaryman_ghost")), inp(byId("emp.x_office_lady")), inp(byId("emp.x_auditor"))], toEmp("emp.x_the_chairman"), context="room.summoning_circle", hint="Three of the Agency's finest.")
recipe("recipe.x_fax_spirit", "Dead Line", RT, [inp(byId("furn.fax_machine")), inp(byId("furn.ofuda"))], toEmp("emp.x_fax_spirit"), context="room.summoning_circle", hint="A fax and a charm. The fax was already haunted.")
recipe("recipe.x_kappa_intern", "Work Experience", RT, [inp(byId("emp.intern")), inp(byId("furn.ofuda"))], toEmp("emp.x_kappa_intern"), context="room.summoning_circle", hint="An intern and a charm.")
recipe("recipe.x_partner", "Equity Event", RT, [inp(byTier(3)), inp(byId("emp.x_office_lady"))], toEmp("emp.x_the_partner"), context="room.summoning_circle", hint="Any Tier 3 and the Office Lady. The Tier 3 does not survive.")
recipe("recipe.x_efficiency_wraith", "Post-Engagement", RT, [inp(byId("emp.consultant")), inp(byId("emp.x_salaryman_ghost"))], toEmp("emp.x_efficiency_wraith"), context="room.summoning_circle", hint="A consultant and a ghost.")
recipe("recipe.x_recruiting_oni", "Aggressive Hiring", RT, [inp(byId("emp.headhunter")), inp(byId("emp.x_kappa_intern"))], toEmp("emp.x_recruiting_oni"), context="room.summoning_circle", hint="A headhunter and the kappa.")
recipes = RC

# ---------------------------------------------------------------- riders
riders = []
def rider(id, name, text, effects):
    riders.append(OD([("id", id), ("name", name), ("text", text), ("effects", effects)]))
rider("rider.tenured", "Tenured", "Cannot be laid off.", [eff("economy", "flag", flag="cannotBeLaidOff", subject=SELF)])
rider("rider.union_dispute", "Union Dispute", "Every reroll costs ¥1 more while employed.", [eff("economy", "stat", stat="rerollCost", subject=subj("firm"), amount=1)])
rider("rider.bad_influence", "Bad Influence", "At Quarter Open, adjacent employees gain 1 Burnout.", [status(BRN, 1, own("adjacent"), on="banner", month=0)])
rider("rider.executive_aversion", "Executive Aversion", "3F output ×0.8 while employed.", [stat("floorOutput", subj("firm"), permille=800, floor="floor.f3")])
rider("rider.poor_reception", "Poor Reception", "Reception grants no Loyalty while employed.", [flag("receptionDisabled", subj("firm"))])
rider("rider.overhead", "Overhead", "Upkeep +¥1 per round.", [eff("economy", "stat", stat="upkeep", subject=subj("firm"), amount=1)])
rider("rider.hungry", "Hungry", "On hire, consumes one adjacent piece of furniture.", [eff("onHire", "consumeAdjacentFurniture", subject=SELF, count=1)])
rider("rider.contractual_obligation", "Contractual Obligation", "At the Bell, the firm causes itself a 200 Scandal.", [eff("banner", "scandal", month=3, value=200, target=firm("own"))])
rider("rider.night_terrors", "Night Terrors", "At Crunch, adjacent employees gain 1 Burnout.", [status(BRN, 1, own("adjacent"), on="banner", month=2)])
rider("rider.cold_spot", "Cold Spot", "Adjacent employees are 10% slower.", [stat("cooldown", ADJ, permille=1100)])
rider("rider.unpaid_invoices", "Unpaid Invoices", "Severance for all staff +¥1 while employed.", [eff("economy", "stat", stat="severance", subject=subj("firm"), amount=1)])
rider("rider.landing_only", "Lift Attendant", "Must be placed on the landing column.", [eff("economy", "flag", flag="landingOnly", subject=SELF)])

# ---------------------------------------------------------------- modifiers
modifiers = []
def mod(id, name, text, effects, rivalOnly=False, board=None, teaches=None):
    d = OD([("id", id), ("name", name), ("text", text), ("rivalOnly", rivalOnly)])
    if board: d["boardMeeting"] = board
    if teaches: d["teaches"] = teaches
    d["effects"] = effects
    modifiers.append(d)
ALL = subj("all")
mod("mod.overtime_culture", "Overtime Culture", "Every employee starts each fight with 1 Burnout and 1 Overtime.",
    [status(BRN, 1, own("all"), on="banner", month=0), status(OVT, 1, own("all"), on="banner", month=0)],
    board=OD([("cost", "1 Burnout on everyone at Quarter Open"), ("benefit", "1 Overtime on everyone at Quarter Open")]))
mod("mod.lean", "Lean", "Loyalty cap −200; income +¥2 per round.",
    [stat("loyaltyCap", subj("firm"), amount=-200), eff("economy", "stat", stat="income", subject=subj("firm"), amount=2)],
    board=OD([("cost", "−200 Loyalty cap"), ("benefit", "+¥2 income per round")]))
mod("mod.family_firm", "Family Firm", "Severance doubled; all rooms gain +1 Tenure round now.",
    [eff("economy", "stat", stat="severanceMult", subject=subj("firm"), permille=2000), eff("onAccept", "roomTenure", rounds=1, subject=subj("allRooms"))],
    board=OD([("cost", "Severance ×2"), ("benefit", "+1 Tenure round to every room")]))
mod("mod.compliance_review", "Compliance Review", "Reroll costs ¥2; Bureaucracy you apply gains a stack.",
    [eff("economy", "stat", stat="rerollCost", subject=subj("firm"), amount=1), stat("statusStacksBonus", ALL, amount=1, status=BUR)],
    board=OD([("cost", "Reroll ¥2"), ("benefit", "+1 stack on every Bureaucracy you apply")]))
mod("mod.open_door", "Open Door Policy", "Loyalty regrowth +40 per event; Loyalty cap −100.",
    [stat("regenPerEvent", subj("firm"), amount=40), stat("loyaltyCap", subj("firm"), amount=-100)],
    board=OD([("cost", "−100 Loyalty cap"), ("benefit", "+40 Loyalty regrowth per event")]))
mod("mod.hostile_environment", "Hostile Environment", "All output ×1.1; nobody can burn past 3 stacks — because they leave.",
    [*output(ALL,permille=1100), stat("burnoutMaxOverride", ALL, amount=3)],
    board=OD([("cost", "Burnout capped at 3 — HR cleanses have less to do"), ("benefit", "All output ×1.1")]))
mod("mod.golden_handcuffs", "Golden Handcuffs", "Severance is free; income −¥1 per round.",
    [eff("economy", "stat", stat="severanceMult", subject=subj("firm"), permille=0), eff("economy", "stat", stat="income", subject=subj("firm"), amount=-1)],
    board=OD([("cost", "−¥1 income per round"), ("benefit", "Free severance")]))
# rival-only gimmicks
mod("mod.g_deep_pockets", "Deep Pockets", "Loyalty cap ×1.5.", [stat("loyaltyCapMult", subj("firm"), permille=1500)], rivalOnly=True, teaches="Poaching alone is slow; bring Burnout, or out-earn it.")
mod("mod.g_franchise", "Franchise", "Every floor counts as the most populated.", [flag("everyFloorMostPopulated", subj("firm"))], rivalOnly=True, teaches="Floor-selected statuses land everywhere.")
mod("mod.g_old_money", "Old Money", "All rooms at Tier III.", [eff("static", "override", override="tenureTier", to=3, subject=subj("allRooms"))], rivalOnly=True, teaches="What Tenure looks like fully grown.")
mod("mod.g_night_shift", "Night Shift", "All staff hold permanent Overtime; no Burnout on expiry.", [flag("overtimePermanent", ALL)], rivalOnly=True, teaches="Haste without the cost, and how to slow it.")
mod("mod.g_regulatory_capture", "Regulatory Capture", "Regen is never suppressed.", [flag("regenNeverSuppressed", subj("firm"))], rivalOnly=True, teaches="The Act 2 boss's signature. Its Loyalty always comes back: answer it with Scandal, Curse, or more Sales.")
mod("mod.g_conglomerate", "Conglomerate", "Every status is floor-selected; highest_occupied_floor also hits lowest_occupied_floor.", [flag("floorSelectorMirror", subj("firm"))], rivalOnly=True, teaches="The Act 3 boss's signature. Concentration is punished twice.")
mod("mod.g_mirror", "Mirror", "No gimmick. The tower is a competent copy of your own archetype at this round.", [], rivalOnly=True, teaches="The Act 1 boss's signature is having none. The ledger is the lesson.")
mod("mod.g_skeleton_crew", "Skeleton Crew", "Half the staff; each does double the work.", [*output(ALL,permille=2000)], rivalOnly=True, teaches="Fewer, larger hits — overflow timing.")
mod("mod.g_zaibatsu", "Zaibatsu", "Every floor's output ×1.15.", [stat("floorOutput", subj("firm"), permille=1150, floor="*")], rivalOnly=True, teaches="A flat power lead; win on structure, not stats.")
mod("mod.g_ghost_floor", "Ghost Floor", "B1 is leased at no Loyalty cost.", [stat("loyaltyCap", subj("firm"), amount=150)], rivalOnly=True, teaches="A portal build with the tax waived.")
# Build-side, never offered: one copy per ¥1 of upkeep that could not be paid this round (GAME_DESIGN §3.2, D-31).
mod("mod.unpaid_upkeep", "Unpaid Upkeep", "Loyalty cap −100 for this round; rent the firm could not pay.", [stat("loyaltyCap", subj("firm"), amount=-100)])

# ---------------------------------------------------------------- shop
shop = OD([
    ("id", "shop.default"),
    ("cardsPerTab", 4), ("rerollCost", 1),
    ("drawModel", OD([("kind", "bag"), ("note", "Each tab draws from a per-round bag without replacement; the bag refills from the tier table only when exhausted, so a reroll never repeats a card until every eligible card has been offered once (D-54)")])),
    ("recruiterNode", OD([("cardsPerTab", 6), ("firstRerollFree", True)])),
    ("otherworld", OD([("cards", 2), ("rerollCost", 1), ("requiresPortal", True)])),
    ("tiers", [
        OD([("rounds", [1, 3]), ("staff", OD([("1", 4), ("2", 0), ("3", 0)])), ("roomTiles", [2, 4]), ("furnitureRarity", ["common"])]),
        OD([("rounds", [4, 8]), ("staff", OD([("1", 2), ("2", 2), ("3", 0)])), ("roomTiles", [2, 4, 6]), ("furnitureRarity", ["common", "uncommon"])]),
        OD([("rounds", [9, 16]), ("staff", OD([("1", 1), ("2", 2), ("3", 1)])), ("roomTiles", [2, 4, 6]), ("furnitureRarity", ["common", "uncommon"])]),
    ]),
    ("roomsOnlyForOwnedFloors", True),
    ("leases", ["floor.f2", "floor.f3", "floor.b1"]),
])

# ---------------------------------------------------------------- modes
modes = [
    OD([("id", "mode.campaign"), ("name", "Campaign"), ("map", "map.campaign"), ("interludes", True),
        ("rivalSource", OD([("scripted", True), ("templates", True), ("ghosts", False)])),
        ("rivalGimmicks", True), ("gimmicksFromRound", 8),
        ("portalUnlock", OD([("condition", "bossDefeated"), ("act", 1)])),
        ("winBonus", OD([("fight", 3), ("audit", 5), ("boss", 5)])),
        ("strikes", 5), ("rounds", 16), ("metaUnlocks", True), ("rating", False), ("snapshotCapture", False),
        ("drawIsLossWithoutStrike", True), ("firstRunTutorial", True)]),
    OD([("id", "mode.ranked"), ("name", "Ranked"), ("map", None), ("interludes", False),
        ("rivalSource", OD([("scripted", False), ("templates", True), ("ghosts", True)])),
        ("rivalGimmicks", False), ("gimmicksFromRound", None),
        ("portalUnlock", OD([("condition", "round"), ("round", 5)])),
        ("winBonus", OD([("fight", 3), ("audit", 3), ("boss", 3)])),
        ("strikes", 5), ("rounds", 16), ("metaUnlocks", False), ("rating", True), ("snapshotCapture", True),
        ("drawIsLossWithoutStrike", False), ("firstRunTutorial", False)]),
]

# ---------------------------------------------------------------- map
campaign_map = OD([
    ("id", "map.campaign"),
    ("nodeKinds", OD([
        ("takeover", OD([("fight", True), ("icon", "ui.map.node.takeover")])),
        ("audit", OD([("fight", True), ("icon", "ui.map.node.audit"), ("rivalRoundOffset", 2), ("alwaysGimmick", True)])),
        ("recruiter", OD([("fight", False), ("icon", "ui.map.node.recruiter")])),
        ("board", OD([("fight", False), ("icon", "ui.map.node.board"), ("choices", 3)])),
        ("consultant", OD([("fight", False), ("icon", "ui.map.node.consultant")])),
        ("boss", OD([("fight", True), ("icon", "ui.map.node.boss")])),
    ])),
    ("acts", [
        OD([("act", 1), ("name", "The Regional Rival"), ("columns", "FFXFFXFB"), ("rounds", [1, 6]), ("boss", "rival.boss_regional_rival"),
            ("scriptedFightsFirstRun", ["rival.tut_01_two_desk_startup", "rival.tut_02_copy_shop", "rival.tut_03_cram_school", "rival.tut_04_print_works", "rival.tut_05_bento_chain"])]),
        OD([("act", 2), ("name", "The Compliance Office"), ("columns", "FFXFFXFB"), ("rounds", [7, 12]), ("boss", "rival.boss_compliance_office"),
            ("scriptedFightsFirstRun", ["rival.tut_07_compliance_adjacent"]),
            ("consultantAlwaysOffers", "recipe.headhunter")]),
        OD([("act", 3), ("name", "The Parent Company"), ("columns", "FXFXFB"), ("rounds", [13, 16]), ("boss", "rival.boss_parent_company")]),
    ]),
    ("layout", OD([("rowsPerColumn", [2, 3]), ("bossRows", 1), ("auditsPerAct", [1, 2]), ("auditNeverInFirstColumn", True)])),
])

# ---------------------------------------------------------------- rival templates
def shoplist(*pairs): return [OD([("defId", i), ("weight", w), ("minRound", r)]) for i, w, r in pairs]
FOUNDERS_BY_ARCH = {"generalist": ["founder.sato", "founder.kitamura"], "fortress": ["founder.okada", "founder.sato"],
                    "raider": ["founder.hoshino", "founder.okada"], "earner": ["founder.nakagawa", "founder.hoshino"],
                    "scandal": ["founder.moriyama", "founder.ueda"], "management": ["founder.moriyama", "founder.the_founder"]}
def T(id, arch, names, rooms_, staff, gimmicks, layout, budget=950):
    return OD([("id", id), ("archetype", arch), ("namePool", names), ("founderPool", FOUNDERS_BY_ARCH[arch]), ("budgetPermille", budget),
               ("rooms", rooms_), ("staff", staff), ("gimmickPool", gimmicks), ("layout", layout)])
LAY = lambda fl, fill: OD([("floorPreference", fl), ("fillOrder", fill)])
templates = [
    T("rival.t_generalist", "generalist", ["Kobayashi Holdings", "Sato & Sons", "Nakamura Trading", "Fujiwara Logistics"],
      shoplist(("room.open_plan", 3, 2), ("room.sales_floor", 2, 3), ("room.legal_dept", 1, 5), ("room.break_room", 1, 4)),
      shoplist(("emp.junior_dev", 4, 1), ("emp.sales_rep", 3, 1), ("emp.paralegal", 2, 1), ("emp.qa_tester", 2, 1), ("emp.senior_dev", 3, 4), ("emp.team_lead", 2, 3), ("emp.account_manager", 2, 5), ("emp.architect", 1, 9), ("emp.recruiter", 1, 3)),
      ["mod.g_zaibatsu", "mod.g_skeleton_crew"], LAY(["floor.f1", "floor.f2", "floor.g"], ["engineering", "sales", "legal", "management"])),
    T("rival.t_fortress", "fortress", ["Compliance Partners", "Harada Legal", "Mizuno Assurance", "Ishikawa & Ishikawa"],
      shoplist(("room.legal_dept", 4, 2), ("room.executive_lounge", 2, 7), ("room.kitchenette", 1, 3), ("room.security_desk", 1, 4)),
      shoplist(("emp.paralegal", 5, 1), ("emp.recruiter", 1, 1), ("emp.sales_rep", 3, 1), ("emp.compliance_officer", 2, 2), ("emp.counsel", 3, 4), ("emp.patent_attorney", 1, 5), ("emp.hr_manager", 2, 5), ("emp.general_counsel", 1, 9), ("emp.head_of_people", 1, 10)),
      ["mod.g_deep_pockets", "mod.g_regulatory_capture"], LAY(["floor.f1", "floor.g", "floor.f2"], ["legal", "hr", "sales"])),
    T("rival.t_raider", "raider", ["Kurokawa Capital", "Tanaka Acquisitions", "Ono & Partners", "Yamashita Ventures"],
      shoplist(("room.meeting_room", 4, 2), ("room.mail_room", 1, 2), ("room.legal_dept", 2, 4), ("room.corner_office", 1, 8)),
      shoplist(("emp.junior_dev", 3, 1), ("emp.sales_rep", 2, 1), ("emp.paralegal", 2, 1), ("emp.team_lead", 3, 2), ("emp.account_manager", 4, 4), ("emp.patent_attorney", 3, 4), ("emp.counsel", 2, 5), ("emp.middle_manager", 2, 6), ("emp.director", 1, 11)),
      ["mod.g_skeleton_crew", "mod.g_night_shift"], LAY(["floor.f1", "floor.f2", "floor.f3"], ["sales", "legal", "management", "engineering"])),
    T("rival.t_earner", "earner", ["Suzuki Sales Co.", "Matsumoto Direct", "Tanaka Systems", "Hayashi Retail"],
      shoplist(("room.sales_floor", 4, 2), ("room.mail_room", 1, 2), ("room.open_plan", 1, 5), ("room.boardroom", 1, 9)),
      shoplist(("emp.sales_rep", 5, 1), ("emp.telemarketer", 3, 1), ("emp.junior_dev", 3, 1), ("emp.senior_dev", 2, 4), ("emp.key_account_manager", 1, 9), ("emp.sales_director", 2, 10), ("emp.middle_manager", 1, 7)),
      ["mod.g_zaibatsu", "mod.g_deep_pockets"], LAY(["floor.f1", "floor.f2", "floor.g"], ["sales", "engineering", "management"]), budget=1050),
    T("rival.t_scandal", "scandal", ["Watanabe Consulting", "Kimura Advisory", "Nishimura Group", "The Yoshida Practice"],
      shoplist(("room.meeting_room", 3, 2), ("room.training_room", 2, 4), ("room.break_room", 2, 3), ("room.kitchenette", 1, 5)),
      shoplist(("emp.project_manager", 3, 1), ("emp.sales_rep", 2, 1), ("emp.headhunter", 4, 4), ("emp.consultant", 4, 5), ("emp.hr_manager", 2, 5), ("emp.office_manager", 2, 2), ("emp.trainer", 1, 7), ("emp.head_of_people", 1, 10)),
      ["mod.g_franchise", "mod.g_night_shift"], LAY(["floor.f1", "floor.f2", "floor.g"], ["management", "sales", "hr"])),
    T("rival.t_management", "management", ["Ito Corporation", "Shimizu Enterprises", "Goto Industrial", "Maeda Holdings"],
      shoplist(("room.meeting_room", 3, 2), ("room.boardroom", 3, 7), ("room.open_plan", 2, 2), ("room.server_room", 1, 4)),
      shoplist(("emp.team_lead", 3, 1), ("emp.junior_dev", 3, 1), ("emp.senior_dev", 5, 4), ("emp.director", 2, 9), ("emp.vp_operations", 1, 11), ("emp.architect", 1, 10)),
      ["mod.g_old_money", "mod.g_conglomerate"], LAY(["floor.f1", "floor.f3", "floor.f2"], ["management", "engineering"])),
]
lease_schedule = OD([("id", "rival.lease_schedule"), ("byRound", OD([("1", []), ("4", ["floor.f2"]), ("8", ["floor.f2", "floor.f3"]), ("11", ["floor.f2", "floor.f3", "floor.b1"])]))])

# ---------------------------------------------------------------- founders
founders = []
def founder(id, name, title, bio):
    key = id.split(".", 1)[1]
    founders.append(OD([("id", id), ("name", name), ("title", title), ("bio", bio),
                        ("portrait", "founder.%s.portrait" % key), ("badge", "founder.%s.badge" % key),
                        ("inShop", True), ("effects", [])]))
founder("founder.sato", "Sato Kenji", "The Lifer", "Thirty-one years at the same desk. Has never once been promoted and has never once been wrong.")
founder("founder.hoshino", "Hoshino Mari", "The Closer", "Left a bigger firm on a Friday with the client list in her handbag.")
founder("founder.okada", "Okada Tetsuo", "The Fixer", "Legal, technically. Nobody has seen the contract he is always carrying.")
founder("founder.nakagawa", "Nakagawa Yui", "The Wunderkind", "Twenty-four. Shipped the product before the company existed.")
founder("founder.moriyama", "Moriyama Hiro", "The Manager", "Has a whiteboard. Has never written on it. Everyone works harder near it anyway.")
founder("founder.ueda", "Ueda Kaori", "The Nurturer", "Runs HR like a kitchen. People stay for the food and the not-being-fired.")
founder("founder.the_founder", "The Founder", "Deceased, 1987", "Still listed on the letterhead. Still signs the quarterly memo. The signature is fresh.")
founder("founder.kitamura", "Kitamura Sho", "The Salaryman", "Ordinary in every respect. The lift stops at B1 for him and nobody else.")

# ---------------------------------------------------------------- balance
def inv(id, name, statement, population, measure, comparator, threshold, cadence, severity, source):
    return OD([("id", id), ("name", name), ("statement", statement), ("population", population), ("measure", measure),
               ("comparator", comparator), ("threshold", threshold), ("cadence", cadence), ("severity", severity), ("source", source)])
balance = OD([
    ("id", "balance.default"),
    ("seeds", OD([("smoke", 40), ("nightly", 200), ("search", 200)])),
    ("smokeRounds", [1, 6, 12, 16]),
    ("populations", OD([
        ("field", OD([("description", "Every archetype template expanded at the round, seeds x 6 archetypes, budget 1000 permille"), ("templates", "all"), ("budgetPermille", 1000)])),
        ("median_attacker", OD([("description", "The generalist template at median budget; results averaged over seeds"), ("templates", ["rival.t_generalist"]), ("budgetPermille", 1000)])),
        ("strongest_defence", OD([("description", "A hill-climb over legal, constructible, gimmick-free builds at the round, maximising the tick at which Loyalty first reaches 0 against median_attacker; 200 iterations from the fortress template at 1200 permille"), ("search", "maximize_break_tick"), ("start", "rival.t_fortress"), ("budgetPermille", 1200), ("iterations", 200)])),
        ("mirror", OD([("description", "A template against itself, different seeds"), ("templates", "all")])),
        ("agent_run", OD([("description", "A greedy builder plays 16 rounds against field rivals, buying the highest harness-scored option each round; 100 runs"), ("runs", 100)])),
        ("optimizer", OD([("description", "A hill-climb over constructible builds maximising win rate against field at the round; 200 iterations; used for outlier detection and pick rates"), ("search", "maximize_win_rate"), ("iterations", 200)])),
    ])),
    ("bands", OD([
        ("archetypeVsField", [420, 580]), ("archetypeBandFromRound", 4), ("archetypeRoundSpike", [300, 700]),
        ("counterPair", [580, 750]),
        ("mirror", [470, 530]),
        ("lateSwingRate", [100, 250]),
        ("drawRateMax", 20),
        ("firstRevenueMoveMedianTick", 200), ("firstRevenueMoveP90Tick", 400),
        ("singleHitRevenueMaxPermille", 150),
        ("liveLedgerLinesPerSecondP95", 4), ("liveLedgerLinesPerSecondFail", 6),
        ("demolitionsPerRunMedianMax", 1000),
        ("relocationsPerRunMedianMax", 2000),
        ("selectorDensityFromRound", 8), ("selectorDensityMin", 500),
        ("abilityShareOfWinnerRevenueP50Max", 500),
        ("deadContentPickRate", 20),
        ("simulateMedianMs", 5),
    ])),
    ("counters", [
        OD([("winner", "raider"), ("loser", "earner"), ("why", "The earner has the Revenue and none of the Loyalty to keep it")]),
        OD([("winner", "fortress"), ("loser", "raider"), ("why", "Loyalty and regen soak Poaches that arrive in bursts")]),
        OD([("winner", "earner"), ("loser", "fortress"), ("why", "Loyalty does not stop Sales; the fortress makes too little")]),
        OD([("winner", "scandal"), ("loser", "fortress"), ("why", "Scandal erodes the cap the fortress paid for")]),
        OD([("winner", "management"), ("loser", "scandal"), ("why", "Retriggers and HR cleanse out-tempo a slow Burnout stack")]),
        OD([("winner", "earner"), ("loser", "management"), ("why", "Sales that never stop outgrow a retrigger core")]),
        OD([("winner", "generalist"), ("loser", "none"), ("why", "The generalist is the median, not a counter; it sits inside the band against everyone")]),
    ]),
    ("bossCounters", [
        OD([("boss", "rival.boss_regional_rival"), ("favoured", "generalist"), ("favouredMin", 500), ("punished", None), ("punishedMax", None)]),
        OD([("boss", "rival.boss_compliance_office"), ("favoured", "scandal"), ("favouredMin", 600), ("punished", "raider"), ("punishedMax", 300)]),
        OD([("boss", "rival.boss_parent_company"), ("favoured", "scandal"), ("favouredMin", 450), ("punished", "raider"), ("punishedMax", 300)]),
    ]),
    ("invariants", [
        inv("inv.break_guaranteed", "No defence survives to the Bell", "Against median_attacker, the strongest_defence build's Loyalty reaches 0 before the Bell in every seed.",
            "strongest_defence vs median_attacker", "max over seeds of first tick at which defender loyalty == 0", "<", 1160, "smoke+nightly", "fail", "DESIGN_BRIEF risk 3; D-05; D-85"),
        inv("inv.bar_moves_early", "Fights do not open flat", "In field matches Revenue first moves early.",
            "field vs field", "median and P90 over matches of the first tick with revenueDelta != 0", "median<=200,p90<=400", [200, 400], "smoke+nightly", "fail", "DESIGN_BRIEF risk 4; PLANNING_PROMPT"),
        inv("inv.archetype_band", "No archetype dominates the field", "From round 4, when tier-2 staff arrive, each archetype's win rate against the field, mirror excluded, averaged over the rounds measured, lies inside the band; in any single round it stays inside archetypeRoundSpike, so an archetype may be weak early and strong late but is never a wall or a write-off (D-90). Rounds 1-3 have only tier-1 staff and nothing that can hurt Sales, so they are exempt (D-87).",
            "each archetype vs field, round >= archetypeBandFromRound", "mean over rounds of win rate permille; each round's win rate", "mean in; each round in archetypeRoundSpike", [420, 580], "smoke+nightly", "fail", "PLANNING_PROMPT invariant 3"),
        inv("inv.counter_pairs", "Counters exist and are not walls", "Each listed counter pair's win rate lies inside the counter band.",
            "counters", "win rate permille of winner vs loser", "in", [580, 750], "nightly", "fail", "DESIGN_BRIEF §6 triangle; this plan"),
        inv("inv.mirror_parity", "Mirrors are fair", "A template against itself with different seeds is close to even.",
            "mirror", "win rate permille of side A", "in", [470, 530], "nightly", "fail", "D-29"),
        inv("inv.late_swing", "Crunch is the swing", "Every quarter runs to the Bell (D-85). In field matches a firm behind or level after Crunch begins still wins sometimes, but not so often that a lead means nothing; draws are rare. From round 4, like the archetype band (D-92).",
            "field vs field, round >= archetypeBandFromRound", "permille of decided matches whose winner was not strictly ahead at some tick from the start of Crunch; draw rate", "swing in [100,250]; draw<=20", [[100, 250], 20], "smoke+nightly", "fail", "D-23; D-85; GAME_DESIGN §11.4, §21"),
        inv("inv.chip_cannot_suppress", "Chip cannot hold regen down alone", "No shop-available employee whose base Poach value reaches the round's suppression threshold has a cooldown under 60 ticks.",
            "static: employees x rounds offered", "for each (employee, round): basePoach >= suppressThreshold(round) implies cooldownTicks >= 60", "all", True, "commit", "fail", "D-04 trade-off"),
        inv("inv.single_hit_cap", "No one hit claims the quarter", "No single resolution in a field match moves more than 15% of the quarter's combined Revenue.",
            "field vs field", "max over entries of |revenueDelta| as permille of finalRevenue A + B", "<=", 150, "smoke+nightly", "fail", "D-06 trade-off"),
        inv("inv.rider_net_negative", "The portal is a gamble, not an upgrade", "Each shop extraplanar employee with each rider, substituted into field towers for its cost-matched tier equivalent, does not raise win rate above the cap.",
            "field with substitution vs field", "win rate permille of the substituted tower", "<=", 550, "nightly", "fail", "GAME_DESIGN §14.3"),
        inv("inv.demolition_rare", "Rooms are commitments", "A greedy agent playing full runs rarely demolishes.",
            "agent_run", "median rooms demolished per run, permille", "<=", 1000, "nightly", "fail", "D-24; Q-RISK-2"),
        inv("inv.relocation_rare", "Relocation is a valve, not a habit", "A greedy agent playing full runs relocates rooms rarely; if it relocates freely, the fee or the Tenure penalty is too soft.",
            "agent_run", "median rooms relocated per run, permille", "<=", 2000, "nightly", "warn", "D-51"),
        inv("inv.standing_pat_loses", "Holding is not a strategy", "A builder that stops buying after round 8 and relies on Tenure alone loses to the field in the late rounds; if it does not, Tenure outpays active spending.",
            "agent_run variant: no purchases after round 8", "win rate permille vs field over rounds 12-16", "<=", 400, "nightly", "fail", "D-57; TFT interest-versus-spending lesson"),
        inv("inv.selector_density", "Floors stay a decision", "From round 8, at least half of field towers carry a floor-selected status ability.",
            "field per round >= 8", "fraction permille of towers with >=1 enemy-targeted status effect", ">=", 500, "nightly", "fail", "D-10"),
        inv("inv.ledger_rate", "The live ledger stays readable", "After coalescing, the live ledger's line rate stays under budget.",
            "field vs field", "P95 over 1-second windows of coalesced lines per firm", "warn>4, fail>6", [4, 6], "nightly", "warn+fail", "D-08"),
        inv("inv.ability_diversity", "Wins are not one card", "In field wins, no single ability supplies most of the winner's Revenue.",
            "field vs field, winners", "P50 over matches of max share permille of the winner's Revenue gained from one abilityId", "<=", 500, "nightly", "fail", "PLANNING_PROMPT quality bar: combination over quantity"),
        inv("inv.boss_counters", "Bosses teach what they are meant to", "Each boss loses to its favoured archetype at least as often as listed and beats its punished archetype at least as often as listed.",
            "bossCounters", "win rate permille of the archetype vs the boss", "favoured>=min; punished<=max", None, "nightly", "fail", "GAME_DESIGN §15.2"),
        inv("inv.template_constructible", "Rivals are affordable", "Every template at every round expands to a constructible snapshot for every seed.",
            "templates x rounds x seeds", "count of expansions failing constructibility", "==", 0, "commit", "fail", "D-39; D-40"),
        inv("inv.dead_content", "Nothing is never picked", "Content the optimizer picks less than the floor rate is flagged for review.",
            "optimizer", "pick rate permille per entity", ">=", 20, "nightly", "warn", "this plan"),
        inv("inv.sim_budget", "The sim stays fast", "A single simulate() call stays under budget at round 16.",
            "field at round 16", "median wall-clock ms in Node", "<=", 5, "commit", "fail", "ARCHITECTURE §11"),
    ]),
])

# ---------------------------------------------------------------- tutorial
tutorial = OD([("id", "tutorial.first_run"), ("hints", [
    OD([("round", 1), ("text", "Your two developers are already on 1F. Press READY, then read the ledger at the bottom of the fight.")]),
    OD([("round", 2), ("text", "The shop restocks each round. REROLL replaces a tab's cards for ¥1 and never repeats one until the bag is empty.")]),
    OD([("round", 3), ("text", "Rooms are auras. Put an employee inside one and watch the badge appear.")]),
    OD([("round", 4), ("text", "Furniture affects its neighbours. Two juniors beside a whiteboard is a promotion.")]),
    OD([("round", 5), ("text", "Laying someone off costs severance. Flexibility is real. It is never free.")]),
    OD([("round", 6), ("text", "The Regional Rival is a competent copy of you. Read the ledger afterwards.")]),
    OD([("round", 7), ("text", "That was a lawyer. Legal makes Loyalty hard to break. There is a way around a balance sheet.")]),
    OD([("round", 8), ("text", "The B1 button is lit. The Agency does not haunt you. It invoices you.")]),
])])

# ---------------------------------------------------------------- scripted snapshots
_iid = [0]
def occ(col, row, kind, defId):
    _iid[0] += 1
    return OD([("tile", [col, row]), ("kind", kind), ("defId", defId),
               ("instanceId", ("e_" if kind == "employee" else "f_") + "%03d" % _iid[0]), ("attachments", [])])
def E_(c, r, d): return occ(c, r, "employee", d)
def F_(c, r, d): return occ(c, r, "furniture", d)
def rm(rid, defId, rect, tenure=0): return OD([("roomId", rid), ("defId", defId), ("rect", rect), ("tenureRounds", tenure)])
GRID = {-1: (3, 3), 0: (5, 3), 1: (5, 3), 2: (5, 3), 3: (4, 2)}
def fl(index, rooms_=(), occupants=()):
    w, h = GRID[index]
    return OD([("index", index), ("grid", OD([("w", w), ("h", h)])), ("rooms", list(rooms_)), ("occupants", list(occupants))])
RECEPTION = rm("g_reception", "room.reception", [1, 0, 2, 2])

def snapshot(id, name, round_, floors_, modifiers_=(), riders_=(), leasedB1=False, gimmick=None,
             archetype="generalist", exempt=False, note="", founder="founder.sato"):
    _iid[0] = 0
    d = OD([("id", id), ("name", name), ("kind", "scripted"), ("archetype", archetype), ("round", round_),
            ("gimmick", gimmick), ("exemptFromBudget", exempt), ("note", note),
            ("snapshot", OD([("schemaVersion", 1), ("contentVersion", CONTENT_VERSION), ("round", round_),
                             ("floors", floors_),
                             ("globals", OD([("founderId", founder), ("modifiers", list(modifiers_)), ("riders", list(riders_)), ("leasedB1", leasedB1)]))]))])
    return d

scripted = []
_iid[0] = 0
scripted.append(snapshot("rival.tut_01_two_desk_startup", "Two-Desk Startup", 1, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])]),
    fl(1, [], [E_(1, 1, "emp.junior_dev"), E_(2, 1, "emp.junior_dev")]),
], founder="founder.nakagawa", note="Fight 1. Two Junior Devs in the corridor. Exists so the ledger's first entries are readable."))
scripted.append(snapshot("rival.tut_02_copy_shop", "Copy Shop", 2, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])]),
    fl(1, [], [E_(1, 1, "emp.junior_dev"), E_(2, 1, "emp.sales_rep"), E_(3, 1, "emp.qa_tester")]),
], founder="founder.kitamura", note="Fight 2. Three staff, no rooms. The player has just learned the shop."))
scripted.append(snapshot("rival.tut_03_cram_school", "Cram School", 3, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])], [E_(1, 0, "emp.recruiter")]),
    fl(1, [rm("f1_open", "room.open_plan", [1, 0, 2, 3], 1)], [E_(1, 0, "emp.junior_dev"), E_(2, 0, "emp.junior_dev"), E_(1, 1, "emp.junior_dev")]),
], founder="founder.ueda", note="Fight 3. First rival with a room. The aura badge is the lesson."))
scripted.append(snapshot("rival.tut_04_print_works", "Print Works", 4, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])], [E_(1, 0, "emp.paralegal")]),
    fl(1, [rm("f1_open", "room.open_plan", [1, 0, 2, 3], 2)], [E_(1, 0, "emp.junior_dev"), E_(2, 0, "emp.senior_dev"), E_(1, 1, "emp.junior_dev"), F_(2, 1, "furn.whiteboard")]),
], founder="founder.nakagawa", note="Fight 4. A Senior Dev next to a Whiteboard: the rival has done the recipe the player is about to discover."))
scripted.append(snapshot("rival.tut_05_bento_chain", "Bento Chain", 5, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])], [E_(1, 0, "emp.sales_rep"), E_(2, 0, "emp.telemarketer")]),
    fl(1, [rm("f1_sales", "room.sales_floor", [0, 0, 2, 2], 2), rm("f1_break", "room.break_room", [3, 1, 1, 2], 1)],
       [E_(0, 0, "emp.sales_rep"), E_(1, 0, "emp.sales_rep"), E_(0, 1, "emp.sales_rep"), F_(1, 1, "furn.desk_phone"), E_(3, 1, "emp.recruiter")]),
], archetype="earner", founder="founder.hoshino", note="Fight 5. An earner: Sales on a Sales Floor and nothing to take the player's money. The player learns severance this round."))
scripted.append(snapshot("rival.boss_regional_rival", "The Regional Rival", 6, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])], [E_(1, 0, "emp.paralegal"), E_(2, 0, "emp.sales_rep")]),
    fl(1, [rm("f1_open", "room.open_plan", [0, 0, 2, 3], 4)],
       [E_(0, 0, "emp.junior_dev"), E_(1, 0, "emp.senior_dev"), E_(0, 1, "emp.junior_dev"), F_(1, 1, "furn.whiteboard"), E_(0, 2, "emp.qa_tester")]),
    fl(2, [rm("f2_sales", "room.sales_floor", [1, 0, 2, 2], 2)], [E_(1, 0, "emp.sales_rep"), E_(2, 0, "emp.sales_rep"), E_(1, 1, "emp.team_lead")]),
], modifiers_=["mod.g_mirror"], gimmick="mod.g_mirror", exempt=True, founder="founder.moriyama",
   note="Act 1 boss. A competent generalist with a Tier I Open Plan and a leased 2F. No tricks. Its defeat opens the portal. When the Mirror gimmick is implemented, this authored tower is the fallback if the player's archetype cannot be inferred."))
scripted.append(snapshot("rival.tut_07_compliance_adjacent", "Harada Legal (Regional Office)", 7, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2])], [E_(1, 0, "emp.paralegal"), E_(2, 0, "emp.paralegal")]),
    fl(1, [rm("f1_legal", "room.legal_dept", [1, 0, 2, 2], 3), rm("f1_break", "room.break_room", [4, 0, 1, 2], 1)],
       [E_(1, 0, "emp.counsel"), E_(2, 0, "emp.paralegal"), F_(1, 1, "furn.filing_cabinet"), E_(2, 1, "emp.paralegal"), E_(4, 0, "emp.recruiter"), E_(3, 2, "emp.sales_rep")]),
], archetype="fortress", founder="founder.okada", note="Fight 7. The first Counsel the player meets. Loyalty that will not break in Month 1."))
scripted.append(snapshot("rival.boss_compliance_office", "The Compliance Office", 12, [
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2], 10), rm("g_security", "room.security_desk", [3, 0, 1, 2], 6)],
       [E_(1, 0, "emp.paralegal"), E_(2, 0, "emp.paralegal"), E_(1, 1, "emp.paralegal"), E_(2, 1, "emp.paralegal"), E_(3, 0, "emp.compliance_officer")]),
    fl(1, [rm("f1_legal", "room.legal_dept", [0, 0, 2, 2], 6), rm("f1_break", "room.break_room", [3, 0, 1, 2], 4)],
       [E_(0, 0, "emp.counsel"), E_(1, 0, "emp.counsel"), E_(0, 1, "emp.general_counsel"), F_(1, 1, "furn.filing_cabinet"), E_(3, 0, "emp.hr_manager")]),
    fl(2, [rm("f2_legal", "room.legal_dept", [1, 0, 2, 2], 5), rm("f2_kitchen", "room.kitchenette", [4, 0, 1, 2], 3)],
       [E_(1, 0, "emp.counsel"), E_(2, 0, "emp.patent_attorney"), E_(1, 1, "emp.paralegal"), F_(2, 1, "furn.filing_cabinet"), E_(4, 0, "emp.recruiter")]),
], modifiers_=["mod.g_regulatory_capture"], gimmick="mod.g_regulatory_capture", archetype="fortress", exempt=True, founder="founder.okada",
   note="Act 2 boss. Regen is never suppressed; two Legal Departments at Tier I and II; a Reception full of Paralegals at Tier III. Poaching alone cannot break it; out-earn it, or bring Scandal or Curse."))
scripted.append(snapshot("rival.boss_parent_company", "The Parent Company", 16, [
    fl(-1, [rm("b1_circle", "room.summoning_circle", [1, 0, 2, 2], 4)],
       [E_(1, 0, "emp.x_salaryman_ghost"), E_(2, 0, "emp.x_auditor"), F_(1, 1, "furn.ofuda")]),
    fl(0, [rm("g_reception", "room.reception", [1, 0, 2, 2], 10), rm("g_security", "room.security_desk", [3, 0, 1, 2], 8)],
       [E_(1, 0, "emp.paralegal"), E_(2, 0, "emp.sales_rep"), E_(3, 0, "emp.general_counsel")]),
    fl(1, [rm("f1_open", "room.open_plan", [0, 0, 2, 3], 10), rm("f1_meeting", "room.meeting_room", [3, 0, 2, 2], 7)],
       [E_(0, 0, "emp.architect"), E_(1, 0, "emp.senior_dev"), F_(0, 1, "furn.whiteboard"), E_(1, 1, "emp.devops"), F_(0, 2, "furn.pc_90s"), E_(1, 2, "emp.senior_dev"),
        E_(3, 0, "emp.consultant"), E_(4, 0, "emp.headhunter"), E_(3, 1, "emp.project_manager")]),
    fl(2, [rm("f2_server", "room.server_room", [1, 0, 2, 2], 8), rm("f2_sales", "room.sales_floor", [3, 1, 2, 2], 6)],
       [E_(1, 0, "emp.architect"), E_(2, 0, "emp.senior_dev"), F_(1, 1, "furn.whiteboard"), E_(2, 1, "emp.cto"),
        E_(3, 1, "emp.account_manager"), E_(4, 1, "emp.account_manager"), E_(3, 2, "emp.sales_director")]),
    fl(3, [rm("f3_board", "room.boardroom", [0, 0, 2, 2], 6), rm("f3_corner", "room.corner_office", [2, 0, 1, 2], 6)],
       [E_(0, 0, "emp.director"), E_(1, 0, "emp.vp_operations"), E_(0, 1, "emp.middle_manager"), E_(2, 0, "emp.key_account_manager"), E_(3, 0, "emp.trainer")]),
], modifiers_=["mod.g_conglomerate"],
   riders_=[OD([("instanceId", "e_001"), ("riderId", "rider.tenured")]), OD([("instanceId", "e_002"), ("riderId", "rider.overhead")])],
   leasedB1=True, gimmick="mod.g_conglomerate", archetype="management", exempt=True, founder="founder.the_founder",
   note="Act 3 boss. Five floors, every status floor-selected and mirrored, a Director in a Tier II Boardroom, Tier III Open Plan. Exempt from the budget check by design; it is meant to be richer than the player."))

# ---------------------------------------------------------------- emit
def file(name, key, data, schema):
    return OD([("file", name), ("key", key), ("schema", schema), ("data", data)])

FILES = [
    ("rules.json", rules, "RuleSet"),
    ("economy.json", economy, "Economy"),
    ("floors.json", OD([("floors", floors)]), "FloorFile"),
    ("statuses.json", OD([("statuses", statuses)]), "StatusFile"),
    ("employees.json", OD([("employees", employees)]), "EmployeeFile"),
    ("rooms.json", OD([("rooms", rooms)]), "RoomFile"),
    ("furniture.json", OD([("furniture", furniture)]), "FurnitureFile"),
    ("recipes.json", OD([("recipes", recipes)]), "RecipeFile"),
    ("riders.json", OD([("riders", riders)]), "RiderFile"),
    ("modifiers.json", OD([("modifiers", modifiers)]), "ModifierFile"),
    ("shop.json", shop, "Shop"),
    ("modes.json", OD([("modes", modes)]), "ModeFile"),
    ("map.json", campaign_map, "CampaignMap"),
    ("tutorial.json", tutorial, "Tutorial"),
    ("founders.json", OD([("founders", founders)]), "FounderFile"),
    ("balance.json", balance, "Balance"),
    ("rivals/templates.json", OD([("leaseSchedule", lease_schedule), ("templates", templates)]), "TemplateFile"),
]
for s_ in scripted:
    FILES.append(("rivals/scripted/%s.json" % s_["id"].split(".", 1)[1], s_, "ScriptedRival"))

index = OD([("contentVersion", CONTENT_VERSION), ("schema", "schema/content.schema.json"),
            ("files", [OD([("path", f), ("def", d)]) for f, _, d in FILES])])

def emit():
    for path, data, _ in FILES:
        full = os.path.join(OUT, path)
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "w") as fh:
            json.dump(data, fh, indent=2, ensure_ascii=False); fh.write("\n")
    with open(os.path.join(OUT, "index.json"), "w") as fh:
        json.dump(index, fh, indent=2, ensure_ascii=False); fh.write("\n")
    print("emitted", len(FILES) + 1, "files to", OUT)

# ---------------------------------------------------------------- referential checks
def check():
    errs = []
    ids = {}
    def reg(kind, lst):
        for x in lst:
            i = x["id"]
            if i in ids: errs.append("duplicate id " + i)
            if not i.startswith(kind + "."): errs.append("id prefix mismatch " + i)
            ids[i] = x
    reg("emp", employees); reg("room", rooms); reg("furn", furniture); reg("recipe", recipes)
    reg("rider", riders); reg("mod", modifiers); reg("floor", floors); reg("status", statuses)
    reg("rival", templates); reg("rival", scripted); reg("founder", founders)
    for e in employees:
        ab = [f for f in e["effects"] if f["on"] == "ability"]
        if len(ab) != 1: errs.append("%s must have exactly one ability, has %d" % (e["id"], len(ab)))
        if e["extraplanar"] != e["dept"].startswith("extraplanar") and e["dept"] == "extraplanar": pass
        for f in e["effects"]:
            for k in ("status",):
                if k in f and f[k] not in ids: errs.append("%s unknown %s %s" % (e["id"], k, f[k]))
    for r in rooms:
        for fid in r["floors"]:
            if fid not in ids: errs.append("%s unknown floor %s" % (r["id"], fid))
        for f in r["effects"]:
            if "status" in f and f["status"] not in ids: errs.append("%s unknown status" % r["id"])
    for f_ in furniture:
        for fid in f_.get("floors", []):
            if fid not in ids: errs.append("%s unknown floor %s" % (f_["id"], fid))
    for rc in recipes:
        for i in rc["inputs"]:
            m = i["match"]
            if "defId" in m and m["defId"] not in ids: errs.append("%s unknown input %s" % (rc["id"], m["defId"]))
        if rc["context"] and rc["context"]["room"] not in ids: errs.append("%s unknown context" % rc["id"])
        res = rc["result"]
        if "defId" in res and res["defId"] not in ids: errs.append("%s unknown result %s" % (rc["id"], res["defId"]))
        if res["kind"] in ("roomTier", "roomTenure") and not rc["context"]: errs.append("%s room result needs context" % rc["id"])
    # every ritual-only employee must be produced by a recipe; every shop-less non-ritual too
    produced = {rc["result"].get("defId") for rc in recipes}
    for e in employees:
        if not e["inShop"] and e["id"] not in produced: errs.append("%s not in shop and not produced by any recipe" % e["id"])
    for t in templates:
        for lst in (t["rooms"], t["staff"]):
            for x in lst:
                if x["defId"] not in ids: errs.append("%s unknown %s" % (t["id"], x["defId"]))
        for g in t["gimmickPool"]:
            if g not in ids or not ids[g]["rivalOnly"]: errs.append("%s bad gimmick %s" % (t["id"], g))
        for fo in t["founderPool"]:
            if fo not in ids: errs.append("%s unknown founder %s" % (t["id"], fo))
    for bc in balance["bossCounters"]:
        if bc["boss"] not in ids: errs.append("balance unknown boss " + bc["boss"])
    for pop in balance["populations"].values():
        for t in (pop.get("templates") if isinstance(pop.get("templates"), list) else []):
            if t not in ids: errs.append("balance unknown template " + t)
        if pop.get("start") and pop["start"] not in ids: errs.append("balance unknown start template")
    archs = {t["archetype"] for t in templates}
    for c in balance["counters"]:
        if c["winner"] not in archs or (c["loser"] != "none" and c["loser"] not in archs): errs.append("balance unknown archetype in counters")
    for a in campaign_map["acts"]:
        if a["boss"] not in ids: errs.append("map unknown boss " + a["boss"])
        for s_ in a.get("scriptedFightsFirstRun", []):
            if s_ not in ids: errs.append("map unknown scripted " + s_)
        cols = a["columns"]; nF = cols.count("F") + cols.count("B")
        if nF != a["rounds"][1] - a["rounds"][0] + 1: errs.append("act %d fight count %d != rounds" % (a["act"], nF))
    # snapshots: structural validation per SIMULATION_SPEC §5.1
    for s_ in scripted:
        snap = s_["snapshot"]
        seen_floors = set()
        for f_ in snap["floors"]:
            idx = f_["index"]
            if idx in seen_floors: errs.append("%s dup floor %d" % (s_["id"], idx))
            seen_floors.add(idx)
            w, h = f_["grid"]["w"], f_["grid"]["h"]
            if (w, h) != GRID[idx]: errs.append("%s floor %d grid mismatch" % (s_["id"], idx))
            tiles_in_room = {}
            cells = set()
            for r in f_["rooms"]:
                c, rr, rw, rh = r["rect"]
                rd = ids.get(r["defId"])
                if not rd: errs.append("%s unknown room %s" % (s_["id"], r["defId"])); continue
                fid = [x["id"] for x in floors if x["index"] == idx][0]
                if fid not in rd["floors"]: errs.append("%s room %s illegal on floor %d" % (s_["id"], r["defId"], idx))
                if (rw, rh) != (rd["footprint"]["w"], rd["footprint"]["h"]): errs.append("%s room %s wrong size" % (s_["id"], r["roomId"]))
                if c + rw > w or rr + rh > h: errs.append("%s room %s leaves grid" % (s_["id"], r["roomId"]))
                if c == 0 and not rd["landingLegal"]: errs.append("%s room %s on landing" % (s_["id"], r["roomId"]))
                for x in range(c, c + rw):
                    for y in range(rr, rr + rh):
                        if (x, y) in cells: errs.append("%s rooms overlap at %d,%d" % (s_["id"], x, y))
                        cells.add((x, y)); tiles_in_room[(x, y)] = r
            occ_tiles = set(); occ_count = {}
            for o in f_["occupants"]:
                x, y = o["tile"]
                if x >= w or y >= h: errs.append("%s occupant off grid" % s_["id"])
                if (x, y) in occ_tiles: errs.append("%s two occupants at %d,%d fl %d" % (s_["id"], x, y, idx))
                occ_tiles.add((x, y))
                d = ids.get(o["defId"])
                if not d: errs.append("%s unknown occupant %s" % (s_["id"], o["defId"])); continue
                room_ = tiles_in_room.get((x, y))
                if o["kind"] == "furniture":
                    if not room_: errs.append("%s furniture %s outside a room" % (s_["id"], o["defId"]))
                    fw, fh = d["footprint"]["w"], d["footprint"]["h"]
                    for dx in range(fw):
                        for dy in range(fh):
                            if (x + dx, y + dy) not in tiles_in_room or tiles_in_room[(x + dx, y + dy)] is not room_:
                                errs.append("%s furniture %s footprint leaves room" % (s_["id"], o["defId"]))
                            if (dx, dy) != (0, 0): occ_tiles.add((x + dx, y + dy))
                    if "floors" in d:
                        fid = [xx["id"] for xx in floors if xx["index"] == idx][0]
                        if fid not in d["floors"]: errs.append("%s furniture %s illegal on floor" % (s_["id"], o["defId"]))
                else:
                    if idx == -1 and not d["extraplanar"]: errs.append("%s non-extraplanar on B1" % s_["id"])
                    if d["extraplanar"] and not any(r["instanceId"] == o["instanceId"] for r in snap["globals"]["riders"]) and "ritual" not in d["tags"]:
                        errs.append("%s extraplanar %s without rider" % (s_["id"], o["instanceId"]))
                    if "placement" in d:
                        fid = [xx["id"] for xx in floors if xx["index"] == idx][0]
                        if fid not in d["placement"]["floors"]: errs.append("%s %s placement violated" % (s_["id"], o["defId"]))
                    if room_:
                        occ_count[room_["roomId"]] = occ_count.get(room_["roomId"], 0) + 1
            for r in f_["rooms"]:
                rd = ids[r["defId"]]
                if "maxOccupants" in rd and occ_count.get(r["roomId"], 0) > rd["maxOccupants"]:
                    errs.append("%s room %s over capacity" % (s_["id"], r["roomId"]))
            if idx == -1 and not snap["globals"]["leasedB1"]: errs.append("%s B1 used but not leased" % s_["id"])
        for rd in snap["globals"]["riders"]:
            if rd["riderId"] not in ids: errs.append("%s unknown rider" % s_["id"])
        for m in snap["globals"]["modifiers"]:
            if m not in ids: errs.append("%s unknown modifier" % s_["id"])
        if snap["globals"]["founderId"] not in ids: errs.append("%s unknown founder" % s_["id"])
        if s_["gimmick"] and s_["gimmick"] not in snap["globals"]["modifiers"]: errs.append("%s gimmick not in modifiers" % s_["id"])
    return errs

if __name__ == "__main__":
    errs = check()
    for e in errs: print("ERR", e)
    print("counts: employees %d rooms %d furniture %d recipes %d riders %d modifiers %d templates %d scripted %d" % (
        len(employees), len(rooms), len(furniture), len(recipes), len(riders), len(modifiers), len(templates), len(scripted)))
    if errs: sys.exit(1)
    emit()
