using System;
using System.Collections.Generic;
using System.Linq;

namespace CompanyWars.Game.Proto;

// ponytail: a throwaway prototype of docs/BUSINESS_TYPES.md + BUSINESS_FIRST.md, to be played and then kept or
// binned. Its catalogue lives here rather than in content/ on purpose: nothing in it is a rule yet. It is pure
// C# (no Godot, integer arithmetic) so it could move into CompanyWars.Sim if it survives.

public enum Role { Worker, Sales, Manager }

public enum Act { Idle, Walking, Working, Resting, BurntOut }

public sealed class RoomKind
{
    public required string Id;
    public required string Look;      // the content room whose scene draws it
    public required string Name;
    public required int W;
    public required int H;
    public required int Cost;
    public int Desks;                 // at fit-out 0
    public int DesksPerFit;
    public int MaxFit;
    public bool SalesDesks;
    public int RestCap;               // people it can rest at once
    public int Recover;               // stamina milli per tick while resting here
    public int MoralePerTick;         // while resting here
    public bool Recruiting;
    public bool Reception;
    public bool Meeting;
    public bool Server;
    public int[] Floors = { 0, 1, 2 };
    public required string Blurb;

    public int DeskCount(int fit) => Desks + DesksPerFit * fit;
}

public sealed class Room
{
    public required RoomKind Kind;
    public int Floor, Col, Row, Fit;
    public readonly List<Person> Seated = new();
    public int RestingNow;
    public bool Covers(int f, int c, int r) => f == Floor && c >= Col && c < Col + Kind.W && r >= Row && r < Row + Kind.H;
    public bool Touches(Room o) => o.Floor == Floor && Col < o.Col + o.Kind.W + 1 && o.Col < Col + Kind.W + 1 && Row < o.Row + o.Kind.H + 1 && o.Row < Row + Kind.H + 1 && o != this;
    /// <summary>Directly above or below: the floors are adjacent and the footprints overlap in plan.</summary>
    public bool Stacked(Room o) => Math.Abs(o.Floor - Floor) == 1 && Col < o.Col + o.Kind.W && o.Col < Col + Kind.W && Row < o.Row + o.Kind.H && o.Row < Row + Kind.H;
    public bool HasDesks => Kind.Desks > 0;
    public (int C, int R) SeatTile(int i) => (Col + i % Kind.W, Row + Math.Min(Kind.H - 1, i / Kind.W));
}

/// <summary>What a room is doing this quarter once its neighbours are counted: permille multipliers and the synergies that made them.</summary>
public sealed class RoomStats
{
    public int Bill = 1000, Drain = 1000, Recover = 1000, MoraleDrain = 1000, RestCapBonus;
    public readonly List<Synergy> Active = new();
}

/// <summary>A room synergy. Printed on the card, or hidden until the quarter it first fires (the discovery loop).</summary>
public sealed class Synergy
{
    public required string Id;
    public required string Name;
    public required string Blurb;
    public bool Hidden;
    public required Func<Firm, Room, bool> Applies;
    public required Action<Firm, Room, RoomStats> Apply;
}

public sealed class Person
{
    public required string Name;
    public required Role Role;
    public int Skill = 250;           // milli-yen per working tick at ×1
    public int Stamina = 1_000_000;   // milli
    public int Morale = 800_000;      // milli
    public int Tenure;
    public Act Act = Act.Idle;
    public int Floor, Col, Row;
    public int ToFloor, ToCol, ToRow, WalkLeft, WalkTotal;
    public Act After;
    public Room? Desk;
    public Room? RestRoom;
    public int RestedTicks;
    public bool Slumped => Act == Act.BurntOut;
}

public sealed class Firm
{
    public required string Name;
    public readonly List<Room> Rooms = new();
    public readonly List<Person> People = new();
    public readonly bool[] Leased = { true, false, false };
    public int Loyalty = 1000;
    public int Budget = 120;
    public int Overtime;              // 0 off, 1 on, 2 crunch
    public bool Party;
    public long RevenueMilli, MonthOutput, MonthSales;
    public int Wins, Burnouts, Quits, Poached;
    public readonly Dictionary<Room, RoomStats> Stats = new();
    public readonly HashSet<string> Discovered = new();
    public int BidMult = 1000, PoachPerMonth = 1;

    public RoomStats StatsOf(Room r) => Stats.TryGetValue(r, out RoomStats? s) ? s : new RoomStats();

    /// <summary>Counts every room's neighbours and applies the synergies. Run after seating: some depend on who sits where.</summary>
    public List<Synergy> Compute()
    {
        Stats.Clear();
        BidMult = 1000; PoachPerMonth = 1;
        var found = new List<Synergy>();
        foreach (Room room in Rooms)
        {
            var s = new RoomStats();
            foreach (Synergy syn in ProtoSim.Synergies)
            {
                if (!syn.Applies(this, room)) continue;
                syn.Apply(this, room, s);
                s.Active.Add(syn);
                if (syn.Hidden && Discovered.Add(syn.Id)) found.Add(syn);
            }
            Stats[room] = s;
        }
        if (Has(k => k.Reception)) BidMult = BidMult * 1100 / 1000;
        if (Has(k => k.Meeting)) BidMult = BidMult * 1150 / 1000;
        return found;
    }

    /// <summary>Hidden synergies that would fire on this room but have not been seen yet: the "something hums here" count.</summary>
    public int Undiscovered(Room room) => ProtoSim.Synergies.Count(s => s.Hidden && !Discovered.Contains(s.Id) && s.Applies(this, room));

    public int Desks(bool sales) => Rooms.Where(r => r.Kind.SalesDesks == sales).Sum(r => r.Kind.DeskCount(r.Fit));
    public int DesksUsed => People.Count(p => p.Desk != null);
    public int Wages => People.Count * 2;
    public bool Has(Func<RoomKind, bool> f) => Rooms.Any(r => f(r.Kind));
    public long Revenue => RevenueMilli / 1000;

    public Room? RoomAt(int f, int c, int r) => Rooms.FirstOrDefault(x => x.Covers(f, c, r));

    public string? Place(RoomKind k, int floor, int col, int row)
    {
        if (!Leased[floor]) return "that floor is not leased";
        if (Array.IndexOf(k.Floors, floor) < 0) return $"{k.Name} does not go on {ProtoSim.FloorName(floor)}";
        if (col + k.W > 5 || row + k.H > 3) return "it does not fit there";
        for (int c = col; c < col + k.W; c++) for (int r = row; r < row + k.H; r++) if (RoomAt(floor, c, r) != null) return "something is already there";
        if (Budget < k.Cost) return $"¥{k.Cost} needed";
        Budget -= k.Cost;
        Rooms.Add(new Room { Kind = k, Floor = floor, Col = col, Row = row });
        return null;
    }

    /// <summary>Everyone starts at reception and walks to a seat: sales to sales desks, the rest to any desk, in hiring order.</summary>
    public void Assign()
    {
        foreach (Room r in Rooms) { r.Seated.Clear(); r.RestingNow = 0; }
        foreach (Person p in People.OrderBy(p => p.Role == Role.Sales ? 0 : p.Role == Role.Manager ? 1 : 2))
        {
            p.Desk = null; p.RestRoom = null;
            p.Floor = 0; p.Col = 1; p.Row = 1;
            if (p.Act == Act.BurntOut) { p.Act = Act.Idle; }
            IEnumerable<Room> pool = Rooms.Where(r => r.Kind.DeskCount(r.Fit) > r.Seated.Count && (p.Role == Role.Sales ? r.Kind.SalesDesks : !r.Kind.SalesDesks || p.Role == Role.Manager));
            Room? seat = pool.OrderBy(r => r.Kind.SalesDesks == (p.Role == Role.Sales) ? 0 : 1).ThenBy(r => r.Floor).FirstOrDefault();
            if (seat == null) { p.Act = Act.Idle; continue; }
            seat.Seated.Add(p);
            p.Desk = seat;
            (int c, int r2) = seat.SeatTile(seat.Seated.Count - 1);
            ProtoSim.StartWalk(p, seat.Floor, c, r2, Act.Working);
        }
    }

    /// <summary>The quarter is over: tired people stay tired, burnt-out ones come back at half, the miserable quit.</summary>
    public List<string> EndRound(bool won, int round)
    {
        var lines = new List<string>();
        Budget += 50 + 5 * round + (won ? 25 : 0) - Wages;
        if (won) Wins++;
        Loyalty = Math.Min(1000, Loyalty + 50);
        foreach (Person p in People.ToList())
        {
            p.Tenure++;
            p.Skill = Math.Min(160, p.Skill + 5);
            p.Stamina = p.Act == Act.BurntOut ? 400_000 : Math.Min(1_000_000, p.Stamina + 500_000);
            p.Morale = Math.Min(1_000_000, p.Morale + 100_000);
            if (p.Morale <= 0) { People.Remove(p); Quits++; lines.Add($"{p.Name} quit"); }
        }
        return lines;
    }
}

public sealed class Quarter
{
    public const int Ticks = 1200, TicksPerSecond = 20, TicksPerTile = 5;
    public const long PoolBase = 300_000;   // milli-yen in the month-1 market; months 2 and 3 scale by MonthMult

    /// <summary>What the lead bar shows: banked revenue plus this month's billing so far.</summary>
    public static long Live(Firm f) => f.RevenueMilli + f.MonthOutput;
    public static readonly int[] MonthStart = { 0, 400, 800 };
    public static readonly int[] MonthMult = { 1000, 1500, 2500 };
    public readonly Firm A, B;
    public int Tick;
    public readonly List<(int Tick, string Side, string Text, string Kind)> Ledger = new();
    public bool Finished => Tick >= Ticks;
    public string? Winner => !Finished ? null : A.RevenueMilli > B.RevenueMilli ? "A" : B.RevenueMilli > A.RevenueMilli ? "B" : "draw";

    public Quarter(Firm a, Firm b)
    {
        A = a; B = b;
        foreach (Firm f in new[] { A, B })
        {
            f.RevenueMilli = 0; f.MonthOutput = 0; f.MonthSales = 0;
            if (f.Party) { f.Budget -= 10; foreach (Person p in f.People) { p.Morale = Math.Min(1_000_000, p.Morale + 200_000); p.Stamina = Math.Max(100_000, p.Stamina - 150_000); } }
            f.Assign();
            foreach (Synergy syn in f.Compute()) Log(f, $"DISCOVERED {syn.Name}: {syn.Blurb}", "pr");
            int idle = f.People.Count(p => p.Desk == null);
            if (idle > 0) Log(f, $"{idle} with no desk, idling at reception", "status");
        }
    }

    private void Log(Firm f, string text, string kind) => Ledger.Add((Tick, f == A ? "A" : "B", text, kind));

    public static int Month(int tick) => tick >= MonthStart[2] ? 2 : tick >= MonthStart[1] ? 1 : 0;

    public void Step()
    {
        if (Finished) return;
        foreach (Firm f in new[] { A, B }) foreach (Person p in f.People.ToList()) StepPerson(f, p);
        Tick++;
        if (Tick == MonthStart[1] || Tick == MonthStart[2] || Tick == Ticks) Settle(Month(Tick - 1));
    }

    private static int RestFloor(Firm f) => f.Overtime switch { 0 => 300_000, 1 => 150_000, _ => 0 };

    private void StepPerson(Firm f, Person p)
    {
        int mult = MonthMult[Month(Tick)];
        switch (p.Act)
        {
            case Act.Walking:
                p.WalkLeft--;
                if (p.WalkLeft <= 0) { p.Floor = p.ToFloor; p.Col = p.ToCol; p.Row = p.ToRow; p.Act = p.After; p.RestedTicks = 0; }
                break;
            case Act.Working:
            {
                Room desk = p.Desk!;
                RoomStats st = f.StatsOf(desk);
                int roomMult = st.Bill;
                bool managed = desk.Seated.Any(m => m.Role == Role.Manager && m.Act == Act.Working);
                if (managed && p.Role != Role.Manager) roomMult = roomMult * 1200 / 1000;
                int otMult = f.Overtime switch { 0 => 1000, 1 => 1250, _ => 1500 };
                long earn = (long)p.Skill * roomMult / 1000 * otMult / 1000 * mult / 1000;
                if (p.Role == Role.Manager) earn /= 2;
                f.MonthOutput += earn;
                if (p.Role == Role.Sales) f.MonthSales += earn;
                int crowd = 1000 + 250 * desk.Fit + (managed ? 200 : 0);
                int drain = 2000 * (f.Overtime switch { 0 => 1000, 1 => 1500, _ => 2200 }) / 1000 * crowd / 1000 * st.Drain / 1000;
                p.Stamina -= drain;
                p.Morale -= (f.Overtime switch { 0 => 0, 1 => 40, _ => 120 }) * st.MoraleDrain / 1000;
                if (p.Stamina <= 0)
                {
                    p.Stamina = 0; p.Act = Act.BurntOut; p.Morale -= 300_000; f.Burnouts++;
                    Log(f, $"{ProtoSim.FloorName(p.Floor)} {p.Name} burnt out", "scandal");
                    break;
                }
                if (p.Stamina < RestFloor(f))
                {
                    Room? rest = f.Rooms.Where(r => r.Kind.RestCap + f.StatsOf(r).RestCapBonus > r.RestingNow).OrderBy(r => ProtoSim.Distance(p.Floor, p.Col, p.Row, r.Floor, r.Col, r.Row)).FirstOrDefault();
                    if (rest != null)
                    {
                        rest.RestingNow++;
                        p.RestRoom = rest;
                        (int c, int r2) = rest.SeatTile(rest.RestingNow - 1);
                        ProtoSim.StartWalk(p, rest.Floor, c, r2, Act.Resting);
                        Log(f, $"{ProtoSim.FloorName(p.Floor)} {p.Name} → {rest.Kind.Name}", "status");
                    }
                    else
                    {
                        p.RestRoom = null; p.Act = Act.Resting; p.RestedTicks = 0;
                        Log(f, $"{ProtoSim.FloorName(p.Floor)} {p.Name} dozing at the desk", "status");
                    }
                }
                break;
            }
            case Act.Resting:
            {
                p.RestedTicks++;
                p.Stamina = Math.Min(1_000_000, p.Stamina + (p.RestRoom != null ? p.RestRoom.Kind.Recover * f.StatsOf(p.RestRoom).Recover / 1000 : 1500));
                p.Morale = Math.Min(1_000_000, p.Morale + (p.RestRoom?.Kind.MoralePerTick ?? 0));
                int enough = p.RestRoom != null ? 800_000 : 600_000;
                if (p.Stamina >= enough && p.RestedTicks >= 40)
                {
                    if (p.RestRoom != null) { p.RestRoom.RestingNow--; p.RestRoom = null; }
                    if (p.Desk != null)
                    {
                        (int c, int r2) = p.Desk.SeatTile(p.Desk.Seated.IndexOf(p));
                        ProtoSim.StartWalk(p, p.Desk.Floor, c, r2, Act.Working);
                    }
                    else p.Act = Act.Idle;
                }
                break;
            }
            case Act.Idle:
                p.Morale -= 20;
                break;
            case Act.BurntOut:
                break;
        }
    }

    /// <summary>The month's bell: billable output is banked, the shared market is split by output weighted by Loyalty, burnouts cost Loyalty, recruiters poach.</summary>
    private void Settle(int month)
    {
        long pool = PoolBase * MonthMult[month] / 1000;
        long bidA = Bid(A), bidB = Bid(B);
        long shareA = bidA + bidB == 0 ? pool / 2 : pool * bidA / (bidA + bidB);
        long shareB = pool - shareA;
        A.RevenueMilli += A.MonthOutput + shareA;
        B.RevenueMilli += B.MonthOutput + shareB;
        Ledger.Add((Tick, "A", $"MONTH {month + 1}: billed ¥{A.MonthOutput / 1000:N0}, won ¥{shareA / 1000:N0} of the ¥{pool / 1000:N0} market", "sales"));
        Ledger.Add((Tick, "B", $"MONTH {month + 1}: billed ¥{B.MonthOutput / 1000:N0}, won ¥{shareB / 1000:N0} of the ¥{pool / 1000:N0} market", "sales"));
        foreach (Firm f in new[] { A, B })
        {
            int burnt = f.People.Count(p => p.Act == Act.BurntOut);
            if (burnt > 0) { f.Loyalty = Math.Max(300, f.Loyalty - 40 * burnt); Ledger.Add((Tick, f == A ? "A" : "B", $"{burnt} burnt out on the floor: Loyalty −{40 * burnt}", "scandal")); }
            f.MonthOutput = 0; f.MonthSales = 0;
        }
        if (Tick >= Ticks) return;
        Poach(A, B);
        Poach(B, A);
    }

    private static long Bid(Firm f)
    {
        return (f.MonthOutput + f.MonthSales) * f.Loyalty / 1000 * f.BidMult / 1000;
    }

    private void Poach(Firm to, Firm from)
    {
        if (!to.Has(k => k.Recruiting)) return;
        for (int n = 0; n < to.PoachPerMonth; n++) PoachOne(to, from);
    }

    private void PoachOne(Firm to, Firm from)
    {
        Person? mark = from.People.Where(p => p.Act != Act.BurntOut && p.Morale < 500_000).OrderBy(p => p.Morale).FirstOrDefault();
        if (mark == null) return;
        from.People.Remove(mark);
        if (mark.Desk != null) { mark.Desk.Seated.Remove(mark); mark.Desk = null; }
        if (mark.RestRoom != null) { mark.RestRoom.RestingNow--; mark.RestRoom = null; }
        to.People.Add(mark);
        to.Poached++;
        mark.Morale = 700_000;
        Room? seat = to.Rooms.Where(r => r.Kind.DeskCount(r.Fit) > r.Seated.Count && (mark.Role == Role.Sales) == r.Kind.SalesDesks).FirstOrDefault();
        mark.Floor = 0; mark.Col = 1; mark.Row = 1;
        if (seat != null) { seat.Seated.Add(mark); mark.Desk = seat; (int c, int r) = seat.SeatTile(seat.Seated.Count - 1); ProtoSim.StartWalk(mark, seat.Floor, c, r, Act.Working); }
        else mark.Act = Act.Idle;
        Ledger.Add((Tick, to == A ? "A" : "B", $"headhunted {mark.Name} from {from.Name}", "poach"));
        Ledger.Add((Tick, from == A ? "A" : "B", $"{mark.Name} walked out for {to.Name}", "poach"));
    }
}

public static class ProtoSim
{
    public static readonly RoomKind[] Catalogue =
    {
        new() { Id = "open_plan", Look = "room.open_plan", Name = "Open Plan", W = 2, H = 3, Cost = 30, Desks = 6, DesksPerFit = 2, MaxFit = 2, Floors = new[] { 1, 2 }, Blurb = "6 desks; fit-out adds 2 more each. Cheap per desk, and the crowding burns people faster." },
        new() { Id = "office", Look = "room.training_room", Name = "Office", W = 2, H = 2, Cost = 22, Desks = 3, DesksPerFit = 1, MaxFit = 2, Floors = new[] { 1, 2 }, Blurb = "3 desks; fit-out adds 1 each. Roomier than the open plan." },
        new() { Id = "sales", Look = "room.sales_floor", Name = "Sales Floor", W = 2, H = 2, Cost = 25, Desks = 3, DesksPerFit = 1, MaxFit = 2, SalesDesks = true, Floors = new[] { 1, 2 }, Blurb = "3 sales desks. What sales bill counts double toward the market." },
        new() { Id = "server", Look = "room.server_room", Name = "Server Room", W = 2, H = 2, Cost = 18, Server = true, Floors = new[] { 1, 2 }, Blurb = "Desks in rooms touching it earn ×1.25." },
        new() { Id = "meeting", Look = "room.meeting_room", Name = "Meeting Room", W = 2, H = 2, Cost = 20, Meeting = true, Floors = new[] { 1, 2 }, Blurb = "The firm's market bid ×1.15. One is enough." },
        new() { Id = "break", Look = "room.break_room", Name = "Break Room", W = 1, H = 2, Cost = 12, RestCap = 2, Recover = 8000, MoralePerTick = 30, Blurb = "Rests 2 at a time, fast. People walk here when they are tired, if it is close." },
        new() { Id = "kitchen", Look = "room.kitchenette", Name = "Kitchenette", W = 1, H = 2, Cost = 8, RestCap = 1, Recover = 6000, MoralePerTick = 60, Blurb = "Rests 1 at a time and cheers them up." },
        new() { Id = "recruiting", Look = "room.legal_dept", Name = "Recruiting Office", W = 2, H = 2, Cost = 35, Recruiting = true, Floors = new[] { 1, 2 }, Blurb = "Each month, headhunts the rival's unhappiest person. They walk out of their building and into yours." },
    };

    // Synergies. Multiplicative, so they stack; each of the strong ones carries a cost, so breaking the system is a
    // trade rather than a free lunch. The printed ones teach the grammar; the hidden ones are the reason to try things.
    public static readonly Synergy[] Synergies =
    {
        new() { Id = "server", Name = "Wired In", Blurb = "Desks in a room touching a Server Room bill ×1.25, once per server touching it.",
            Applies = (f, r) => r.HasDesks && f.Rooms.Any(o => o.Kind.Server && o.Touches(r)),
            Apply = (f, r, s) => { foreach (Room o in f.Rooms.Where(o => o.Kind.Server && o.Touches(r))) s.Bill = s.Bill * 1250 / 1000; } },
        new() { Id = "pitch", Name = "Pitch Room", Blurb = "A Sales Floor touching a Meeting Room bills ×1.3.",
            Applies = (f, r) => r.Kind.SalesDesks && f.Rooms.Any(o => o.Kind.Meeting && o.Touches(r)),
            Apply = (f, r, s) => s.Bill = s.Bill * 1300 / 1000 },
        new() { Id = "canteen", Name = "Canteen", Blurb = "A Break Room touching a Kitchenette: both rest one more person and recover ×1.5.",
            Applies = (f, r) => r.Kind.RestCap > 0 && f.Rooms.Any(o => o.Kind.RestCap > 0 && o.Kind != r.Kind && o.Touches(r)),
            Apply = (f, r, s) => { s.Recover = s.Recover * 1500 / 1000; s.RestCapBonus += 1; } },
        new() { Id = "farm", Name = "Server Farm", Hidden = true, Blurb = "Two Server Rooms touching each other: desks touching either bill ×1.5 more, and the heat tires them ×1.5.",
            Applies = (f, r) => r.HasDesks && f.Rooms.Any(o => o.Kind.Server && o.Touches(r) && f.Rooms.Any(o2 => o2.Kind.Server && o2.Touches(o))),
            Apply = (f, r, s) => { s.Bill = s.Bill * 1500 / 1000; s.Drain = s.Drain * 1500 / 1000; } },
        new() { Id = "sweatshop", Name = "Sweatshop", Hidden = true, Blurb = "An Open Plan at full fit-out under Crunch bills ×1.5 again, and morale drains ×3.",
            Applies = (f, r) => r.Kind.Id == "open_plan" && r.Fit >= r.Kind.MaxFit && f.Overtime == 2,
            Apply = (f, r, s) => { s.Bill = s.Bill * 1500 / 1000; s.MoraleDrain = s.MoraleDrain * 3; } },
        new() { Id = "coffee", Name = "Coffee Run", Hidden = true, Blurb = "An Open Plan touching a Kitchenette tires ×0.8.",
            Applies = (f, r) => r.Kind.Id == "open_plan" && f.Rooms.Any(o => o.Kind.Id == "kitchen" && o.Touches(r)),
            Apply = (f, r, s) => s.Drain = s.Drain * 800 / 1000 },
        new() { Id = "boiler", Name = "Boiler Room", Hidden = true, Blurb = "A Sales Floor touching a Recruiting Office bills ×1.2, and the recruiters headhunt two a month.",
            Applies = (f, r) => r.Kind.SalesDesks && f.Rooms.Any(o => o.Kind.Recruiting && o.Touches(r)),
            Apply = (f, r, s) => { s.Bill = s.Bill * 1200 / 1000; f.PoachPerMonth = 2; } },
        new() { Id = "department", Name = "Department", Hidden = true, Blurb = "The same kind of room directly above or below: each bills ×1.15 per stacked floor.",
            Applies = (f, r) => r.HasDesks && f.Rooms.Any(o => o.Kind == r.Kind && o.Stacked(r)),
            Apply = (f, r, s) => { foreach (Room o in f.Rooms.Where(o => o.Kind == r.Kind && o.Stacked(r))) s.Bill = s.Bill * 1150 / 1000; } },
        new() { Id = "management", Name = "Management Floor", Hidden = true, Blurb = "An Office with a Manager in it touching a Meeting Room: every desk on that floor bills ×1.15.",
            Applies = (f, r) => r.HasDesks && f.Rooms.Any(o => o.Kind.Id == "office" && o.Floor == r.Floor && o.Seated.Any(p => p.Role == Role.Manager) && f.Rooms.Any(m => m.Kind.Meeting && m.Touches(o))),
            Apply = (f, r, s) => s.Bill = s.Bill * 1150 / 1000 },
        new() { Id = "gossip", Name = "Gossip", Hidden = true, Blurb = "A Sales Floor touching a Break Room: nobody there loses morale, but they bill ×0.9.",
            Applies = (f, r) => r.Kind.SalesDesks && f.Rooms.Any(o => o.Kind.Id == "break" && o.Touches(r)),
            Apply = (f, r, s) => { s.Bill = s.Bill * 900 / 1000; s.MoraleDrain = 0; } },
    };

    public static readonly RoomKind ReceptionKind =new() { Id = "reception", Look = "room.reception", Name = "Reception", W = 2, H = 2, Cost = 0, Reception = true, Floors = new[] { 0 }, Blurb = "Where everyone arrives. Market bid ×1.1." };

    public static readonly int[] LeaseCost = { 0, 40, 60 };
    public static int HireCost(Role r) => r switch { Role.Worker => 12, Role.Sales => 16, _ => 25 };
    public const int FitCost = 12, Rounds = 12, Strikes = 3;

    public static string FloorName(int f) => f switch { 0 => "G", 1 => "1F", _ => "2F" };

    public static int Distance(int f1, int c1, int r1, int f2, int c2, int r2)
    {
        if (f1 == f2) return Math.Abs(c1 - c2) + Math.Abs(r1 - r2);
        return c1 + Math.Abs(r1 - r2) + 4 * Math.Abs(f1 - f2) + c2;
    }

    public static void StartWalk(Person p, int f, int c, int r, Act after)
    {
        p.ToFloor = f; p.ToCol = c; p.ToRow = r; p.After = after;
        p.WalkTotal = Math.Max(1, Distance(p.Floor, p.Col, p.Row, f, c, r) * Quarter.TicksPerTile);
        p.WalkLeft = p.WalkTotal;
        p.Act = Act.Walking;
    }

    private static readonly string[] Surnames = { "Tanaka", "Suzuki", "Sato", "Takahashi", "Watanabe", "Ito", "Yamamoto", "Nakamura", "Kobayashi", "Kato", "Yoshida", "Yamada", "Sasaki", "Yamaguchi", "Matsumoto", "Inoue", "Kimura", "Hayashi", "Shimizu", "Saito", "Mori", "Abe", "Ikeda", "Hashimoto", "Ishikawa", "Ogawa", "Fujita", "Okada", "Goto", "Hasegawa", "Murakami", "Kondo", "Ishii", "Sakamoto", "Endo", "Aoki", "Fujii", "Nishimura", "Fukuda", "Ota" };
    private static uint _seed = 7;
    private static int _named;

    public static Person NewPerson(Role role)
    {
        _seed = unchecked(_seed * 1664525u + 1013904223u);
        string name = Surnames[_named++ % Surnames.Length];
        var p = new Person { Name = name, Role = role, Skill = role == Role.Sales ? 220 : 250 };
        p.Stamina = 900_000 + (int)(_seed % 100_000);
        return p;
    }

    public static Firm NewPlayer(string name)
    {
        var f = new Firm { Name = name };
        f.Rooms.Add(new Room { Kind = ReceptionKind, Floor = 0, Col = 0, Row = 0 });
        f.People.Add(NewPerson(Role.Worker));
        f.People.Add(NewPerson(Role.Worker));
        return f;
    }

    /// <summary>The rival for a round: two personalities alternate, both grow with the round.</summary>
    public static Firm Rival(int round)
    {
        bool churn = round % 2 == 1;
        var f = new Firm { Name = churn ? "Kurokawa Corp" : "Hoshino Kaisha", Budget = 0 };
        f.Rooms.Add(new Room { Kind = ReceptionKind, Floor = 0, Col = 0, Row = 0 });
        f.Leased[1] = true;
        f.Leased[2] = round >= 4;
        RoomKind K(string id) => Catalogue.First(k => k.Id == id);
        void Put(string id, int fl, int c, int r, int fit = 0) => f.Rooms.Add(new Room { Kind = K(id), Floor = fl, Col = c, Row = r, Fit = fit });
        Put("open_plan", 1, 0, 0, Math.Min(2, round / 3));
        if (round >= 2) Put("sales", 1, 2, 0);
        if (churn)
        {
            f.Overtime = round >= 3 ? 2 : 1;
            if (round >= 4) Put("open_plan", 2, 0, 0, Math.Min(2, round / 4));
            if (round >= 6) Put("recruiting", 2, 2, 0);
            else if (round >= 4) Put("server", 2, 2, 0);
        }
        else
        {
            f.Overtime = 0;
            f.Party = round % 3 == 0;
            Put("break", 0, 2, 0);
            if (round >= 3) Put("break", 1, 4, 0);
            if (round >= 4) { Put("office", 2, 0, 0, 1); Put(round >= 7 ? "meeting" : "server", 2, 2, 0); Put("kitchen", 2, 4, 0); }
        }
        int workers = Math.Min(f.Desks(false) - (round >= 5 ? 1 : 0), 2 + round);
        for (int i = 0; i < workers; i++) f.People.Add(NewPerson(Role.Worker));
        for (int i = 0; i < Math.Min(f.Desks(true), round - 1); i++) f.People.Add(NewPerson(Role.Sales));
        if (round >= 5) f.People.Add(NewPerson(Role.Manager));
        foreach (Person p in f.People) { p.Tenure = churn ? 0 : Math.Min(round, 4); p.Skill += 5 * p.Tenure; }
        return f;
    }
}
