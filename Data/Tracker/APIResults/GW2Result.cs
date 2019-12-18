using System.Collections.Generic;
using System;

namespace MopsBot.Data.Tracker.APIResults.GW2
{

    public class Tier
    {
        public int count { get; set; }
        public int points { get; set; }
    }

    public class Reward
    {
        public string type { get; set; }
        public int id { get; set; }
        public int count { get; set; }
        public string region { get; set; }
    }

    public class Bit
    {
        public string type { get; set; }
        public int id { get; set; }
    }

    public class AchievementInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string requirement { get; set; }
        public string locked_text { get; set; }
        public string type { get; set; }
        public List<string> flags { get; set; }
        public List<Tier> tiers { get; set; }
        public List<Reward> rewards { get; set; }
        public List<Bit> bits { get; set; }
    }


    public class Achievement
    {
        public int id { get; set; }
        public int current { get; set; }
        public int max { get; set; }
        public bool done { get; set; }
        public List<object> bits { get; set; }
        public bool? unlocked { get; set; }
        public int? repeated { get; set; }
    }

    public class Crafting
    {
        public string discipline { get; set; }
        public int rating { get; set; }
        public bool active { get; set; }
    }

    public class Pve
    {
        public int id { get; set; }
        public List<int> traits { get; set; }
    }

    public class Pvp
    {
        public int id { get; set; }
        public List<int> traits { get; set; }
    }

    public class Specializations
    {
        public List<Pve> pve { get; set; }
        public List<Pvp> pvp { get; set; }
        public List<object> wvw { get; set; }
    }

    public class Pve2
    {
        public int heal { get; set; }
        public List<int> utilities { get; set; }
        public int elite { get; set; }
    }

    public class Pvp2
    {
        public int heal { get; set; }
        public List<object> utilities { get; set; }
        public object elite { get; set; }
    }

    public class Wvw
    {
        public int heal { get; set; }
        public List<int> utilities { get; set; }
        public int elite { get; set; }
    }

    public class Skills
    {
        public Pve2 pve { get; set; }
        public Pvp2 pvp { get; set; }
        public Wvw wvw { get; set; }
    }

    public class Attributes
    {
        public int Healing { get; set; }
    }

    public class Stats
    {
        public int id { get; set; }
        public Attributes attributes { get; set; }
    }

    public class Equipment
    {
        public int id { get; set; }
        public string slot { get; set; }
        public List<int> upgrades { get; set; }
        public string binding { get; set; }
        public string bound_to { get; set; }
        public Stats stats { get; set; }
        public List<int?> dyes { get; set; }
    }

    public class EquipmentPvp
    {
        public int amulet { get; set; }
        public int rune { get; set; }
        public List<int?> sigils { get; set; }
    }

    public class Training
    {
        public int id { get; set; }
        public int spent { get; set; }
        public bool done { get; set; }
    }

    public class Inventory
    {
        public int id { get; set; }
        public int count { get; set; }
        public string binding { get; set; }
        public string bound_to { get; set; }
        public List<int?> upgrades { get; set; }
        public int? charges { get; set; }
    }

    public class Bag
    {
        public int id { get; set; }
        public int size { get; set; }
        public List<Inventory> inventory { get; set; }
    }

    public class Character
    {
        public string name { get; set; }
        public string race { get; set; }
        public string gender { get; set; }
        public List<object> flags { get; set; }
        public string profession { get; set; }
        public int level { get; set; }
        public int masteryLevel { get; set; }
        public string guild { get; set; }
        public int age { get; set; }
        public DateTime created { get; set; }
        public int deaths { get; set; }
        public List<Crafting> crafting { get; set; }
        public List<string> backstory { get; set; }
        public List<object> wvw_abilities { get; set; }
        public Specializations specializations { get; set; }
        public Skills skills { get; set; }
        public List<Equipment> equipment { get; set; }
        public List<int> recipes { get; set; }
        public EquipmentPvp equipment_pvp { get; set; }
        public List<Training> training { get; set; }
        public List<Bag> bags { get; set; }
    }

    public class Level
    {
        public string name { get; set; }
        public string description { get; set; }
        public string instruction { get; set; }
        public string icon { get; set; }
        public int point_cost { get; set; }
        public int exp_cost { get; set; }
    }

    public class MasteryInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string requirement { get; set; }
        public int order { get; set; }
        public string background { get; set; }
        public string region { get; set; }
        public List<Level> levels { get; set; }
    }

    public class TPTransaction
    {
        public object id { get; set; }
        public int item_id { get; set; }
        public int price { get; set; }
        public int quantity { get; set; }
        public DateTime created { get; set; }
        public DateTime purchased { get; set; }
    }

    public class Item
    {
        public int id { get; set; }
        public int count { get; set; }
    }

    public class TPInbox
    {
        public long coins { get; set; }
        public List<Item> items { get; set; }
    }

    public class Masteries
    {
        public int id { get; set; }
        public int level { get; set; }
    }

    public class Attribute
    {
        public string attribute { get; set; }
        public int modifier { get; set; }
    }

    public class InfixUpgrade
    {
        public List<Attribute> attributes { get; set; }
    }

    public class Details
    {
        public string type { get; set; }
        public string damage_type { get; set; }
        public int min_power { get; set; }
        public int max_power { get; set; }
        public int defense { get; set; }
        public List<object> infusion_slots { get; set; }
        public InfixUpgrade infix_upgrade { get; set; }
        public int suffix_item_id { get; set; }
        public string secondary_suffix_item_id { get; set; }
    }

    public class ItemInfo
    {
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public int level { get; set; }
        public string rarity { get; set; }
        public int vendor_value { get; set; }
        public string default_skin { get; set; }
        public List<string> game_types { get; set; }
        public List<string> flags { get; set; }
        public List<object> restrictions { get; set; }
        public int id { get; set; }
        public string chat_link { get; set; }
        public string icon { get; set; }
        public Details details { get; set; }
    }

    public class Wallet
    {
        public int id { get; set; }
        public int value { get; set; }
    }
}