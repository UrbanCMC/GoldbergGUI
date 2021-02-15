using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoldbergGUI.Core.Models
{
    public class GoldbergGlobalConfiguration
    {
        public string AccountName { get; set; }
        public long UserSteamId { get; set; }
        public string Language { get; set; }
        public List<string> CustomBroadcastIps { get; set; }
    }
    public class GoldbergConfiguration
    {
        public int AppId { get; set; }
        public List<SteamApp> DlcList { get; set; }
        
        public List<int> Depots { get; set; }
        
        public List<int> SubscribedGroups { get; set; }
        
        public Dictionary<int, string> AppPaths { get; set; }
        
        public List<Achievement> Achievements { get; set; }
        
        public List<Item> Items { get; set; }
        
        public List<Leaderboard> Leaderboards { get; set; }
        
        public List<Stat> Stats { get; set; }
        
        // Add controller setting here!
        public bool Offline { get; set; }
        public bool DisableNetworking { get; set; }
        public bool DisableOverlay { get; set; }
        
        public GoldbergGlobalConfiguration OverwrittenGlobalConfiguration { get; set; }
    }

    public class Achievement
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } 

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } 

        [JsonPropertyName("hidden")]
        public string Hidden { get; set; } 

        [JsonPropertyName("icon")]
        public string Icon { get; set; } 

        [JsonPropertyName("icongray")]
        public string IconGray { get; set; } 

        [JsonPropertyName("name")]
        public string Name { get; set; } 
    }
    
    public class Item
    {
        [JsonPropertyName("Timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("modified")]
        public string Modified { get; set; }

        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("display_type")]
        public string DisplayType { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("bundle")]
        public string Bundle { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("icon_url")]
        public Uri IconUrl { get; set; }

        [JsonPropertyName("icon_url_large")]
        public Uri IconUrlLarge { get; set; }

        [JsonPropertyName("name_color")]
        public string NameColor { get; set; }

        [JsonPropertyName("tradable")]
        // [JsonConverter(typeof(PurpleParseStringConverter))]
        public bool Tradable { get; set; }

        [JsonPropertyName("marketable")]
        // [JsonConverter(typeof(PurpleParseStringConverter))]
        public bool Marketable { get; set; }

        [JsonPropertyName("commodity")]
        // [JsonConverter(typeof(PurpleParseStringConverter))]
        public bool Commodity { get; set; }

        [JsonPropertyName("drop_interval")]
        // [JsonConverter(typeof(FluffyParseStringConverter))]
        public long DropInterval { get; set; }

        [JsonPropertyName("drop_max_per_window")]
        // [JsonConverter(typeof(FluffyParseStringConverter))]
        public long DropMaxPerWindow { get; set; }

        [JsonPropertyName("workshopid")]
        // [JsonConverter(typeof(FluffyParseStringConverter))]
        public long Workshopid { get; set; }

        [JsonPropertyName("tw_unique_to_own")]
        // [JsonConverter(typeof(PurpleParseStringConverter))]
        public bool TwUniqueToOwn { get; set; }

        [JsonPropertyName("item_quality")]
        // [JsonConverter(typeof(FluffyParseStringConverter))]
        public long ItemQuality { get; set; }

        [JsonPropertyName("tw_price")]
        public string TwPrice { get; set; }

        [JsonPropertyName("tw_type")]
        public string TwType { get; set; }

        [JsonPropertyName("tw_client_visible")]
        // [JsonConverter(typeof(FluffyParseStringConverter))]
        public long TwClientVisible { get; set; }

        [JsonPropertyName("tw_icon_small")]
        public string TwIconSmall { get; set; }

        [JsonPropertyName("tw_icon_large")]
        public string TwIconLarge { get; set; }

        [JsonPropertyName("tw_description")]
        public string TwDescription { get; set; }

        [JsonPropertyName("tw_client_name")]
        public string TwClientName { get; set; }

        [JsonPropertyName("tw_client_type")]
        public string TwClientType { get; set; }

        [JsonPropertyName("tw_rarity")]
        public string TwRarity { get; set; }
    }

    public class Leaderboard
    {
        public string Name { get; set; }
        public SortMethod SortMethodSetting { get; set; }
        public DisplayType DisplayTypeSetting { get; set; }

        public enum SortMethod
        {
            None,
            Ascending,
            Descending
        }
        public enum DisplayType
        {
            None,
            Numeric,
            TimeSeconds,
            TimeMilliseconds
        }
    }

    public class Stat
    {
        public string Name { get; set; }
        public StatType StatTypeSetting { get; set; }
        public string Value { get; set; }

        public enum StatType
        {
            Int,
            Float,
            AvgRate
        }
    }
}