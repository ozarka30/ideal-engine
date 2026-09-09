using CompanyWars.Content;
using CompanyWars.Sim;

namespace CompanyWars.Build;

/// <summary>The shop's bag draw model (GAME_DESIGN.md §5.1, D-54): per tab, per round, eligible cards shuffled and drawn without replacement.</summary>
public static class Shop
{
    public const string StaffTab = "staff";
    public const string RoomsTab = "rooms";
    public const string FurnitureTab = "furniture";

    public static ShopTier TierFor(ContentDb db, long round)
    {
        foreach (ShopTier t in db.Shop.Tiers)
        {
            if (round >= t.Rounds[0] && round <= t.Rounds[1]) return t;
        }
        return db.Shop.Tiers[^1];
    }

    /// <summary>A fresh shop for a round: bags rebuilt, every tab drawn once.</summary>
    public static ShopState Open(ContentDb db, TowerSnapshot tower, long round, uint rng)
    {
        var state = new ShopState(new Dictionary<string, BagState>(StringComparer.Ordinal), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), rng);
        state = Draw(db, tower, round, state, StaffTab);
        state = Draw(db, tower, round, state, RoomsTab);
        state = Draw(db, tower, round, state, FurnitureTab);
        return state;
    }

    /// <summary>Replaces a tab's cards from its bags. Rerolling is this with a fee paid by the reducer.</summary>
    public static ShopState Draw(ContentDb db, TowerSnapshot tower, long round, ShopState shop, string tab)
    {
        ShopTier tier = TierFor(db, round);
        var bags = new Dictionary<string, BagState>(shop.Bags, StringComparer.Ordinal);
        uint rng = shop.Rng;
        string[] cards;
        switch (tab)
        {
            case StaffTab:
                {
                    var list = new List<string>();
                    foreach (KeyValuePair<string, long> kv in tier.Staff.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        long count = kv.Value;
                        if (count <= 0) continue;
                        string key = "staff." + kv.Key;
                        for (long i = 0; i < count; i++)
                        {
                            list.Add(Take(bags, key, () => EligibleStaff(db, kv.Key), ref rng));
                        }
                    }
                    cards = list.ToArray();
                    break;
                }
            case RoomsTab:
                cards = TakeMany(bags, "rooms", () => EligibleRooms(db, tower, tier), db.Shop.CardsPerTab, ref rng);
                break;
            case FurnitureTab:
                cards = TakeMany(bags, "furniture", () => EligibleFurniture(db, tier), db.Shop.CardsPerTab, ref rng);
                break;
            default:
                throw new BuildException($"no shop tab named {tab}");
        }
        return tab switch
        {
            StaffTab => shop with { Bags = bags, StaffCards = cards, Rng = rng },
            RoomsTab => shop with { Bags = bags, RoomCards = cards, Rng = rng },
            _ => shop with { Bags = bags, FurnitureCards = cards, Rng = rng },
        };
    }

    private static string[] TakeMany(Dictionary<string, BagState> bags, string key, Func<List<string>> eligible, long count, ref uint rng)
    {
        var list = new List<string>();
        for (long i = 0; i < count; i++)
        {
            string? card = TryTake(bags, key, eligible, ref rng);
            if (card == null) break;
            list.Add(card);
        }
        return list.ToArray();
    }

    private static string Take(Dictionary<string, BagState> bags, string key, Func<List<string>> eligible, ref uint rng)
    {
        return TryTake(bags, key, eligible, ref rng) ?? throw new BuildException($"nothing eligible for {key}");
    }

    private static string? TryTake(Dictionary<string, BagState> bags, string key, Func<List<string>> eligible, ref uint rng)
    {
        if (!bags.TryGetValue(key, out BagState? bag) || bag.Remaining.Length == 0)
        {
            List<string> pool = eligible();
            if (pool.Count == 0) return null;
            bag = new BagState(Shuffle(pool, ref rng));
        }
        string card = bag.Remaining[0];
        bags[key] = new BagState(bag.Remaining[1..]);
        return card;
    }

    /// <summary>Fisher–Yates with the build-phase generator; the generator state is threaded through so a save reproduces the shop.</summary>
    public static string[] Shuffle(List<string> pool, ref uint rng)
    {
        var arr = pool.ToArray();
        var gen = new Mulberry32(rng);
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = (int)gen.Draw((uint)(i + 1));
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        rng = gen.State;
        return arr;
    }

    public static List<string> EligibleStaff(ContentDb db, string tier)
    {
        var list = new List<string>();
        foreach (EmployeeDef e in db.Employees)
        {
            if (e.InShop && !e.Extraplanar && e.Tier.ToString() == tier) list.Add(e.Id);
        }
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    public static List<string> EligibleRooms(ContentDb db, TowerSnapshot tower, ShopTier tier)
    {
        var owned = new HashSet<string>(StringComparer.Ordinal);
        foreach (SnapshotFloor f in tower.Floors) owned.Add(db.FloorByIndex(f.Index).Id);
        var list = new List<string>();
        foreach (RoomDef r in db.Rooms)
        {
            if (r.Fixed) continue;
            if (Array.IndexOf(tier.RoomTiles, r.Footprint.W * r.Footprint.H) < 0) continue;
            if (db.Shop.RoomsOnlyForOwnedFloors && !r.Floors.Any(owned.Contains)) continue;
            list.Add(r.Id);
        }
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    public static List<string> EligibleFurniture(ContentDb db, ShopTier tier)
    {
        var list = new List<string>();
        foreach (FurnitureDef f in db.Furniture)
        {
            if (Array.IndexOf(tier.FurnitureRarity, f.Rarity) >= 0) list.Add(f.Id);
        }
        list.Sort(StringComparer.Ordinal);
        return list;
    }
}
