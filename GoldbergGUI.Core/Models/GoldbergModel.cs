using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoldbergGUI.Core.Models
{
    public class GoldbergGlobalConfiguration
    {
        /// <summary>
        /// Name of the user
        /// </summary>
        public string AccountName { get; set; }
        /// <summary>
        /// Steam64ID of the user
        /// </summary>
        public long UserSteamId { get; set; }
        /// <summary>
        /// language to be used
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// Custom broadcast addresses (IPv4 or domain addresses)
        /// </summary>
        public List<string> CustomBroadcastIps { get; set; }
    }
    public class GoldbergConfiguration
    {
        /// <summary>
        /// App ID of the game
        /// </summary>
        public int AppId { get; set; }
        /// <summary>
        /// List of DLC
        /// </summary>
        public List<SteamApp> DlcList { get; set; }
        
        public List<Depot> Depots { get; set; }
        
        public List<Group> SubscribedGroups { get; set; }
        
        public List<AppPath> AppPaths { get; set; }
        
        public List<Achievement> Achievements { get; set; }
        
        public List<Item> Items { get; set; }
        
        public List<Leaderboard> Leaderboards { get; set; }
        
        public List<Stat> Stats { get; set; }
        
        // Add controller setting here!
        /// <summary>
        /// Set offline mode.
        /// </summary>
        public bool Offline { get; set; }
        /// <summary>
        /// Disable networking (game is set to online, however all outgoing network connectivity will be disabled).
        /// </summary>
        public bool DisableNetworking { get; set; }
        /// <summary>
        /// Disable overlay (experimental only).
        /// </summary>
        public bool DisableOverlay { get; set; }
        
        public GoldbergGlobalConfiguration OverwrittenGlobalConfiguration { get; set; }
    }

    public class Depot
    {
        /// <summary>
        /// ID of Depot.
        /// </summary>
        public int DepotId { get; set; }
        /// <summary>
        /// Name of Depot.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Associated DLC App ID, can be null (e.g. if Depot is for base game).
        /// </summary>
        public int DlcAppId { get; set; }
    }

    public class Group
    {
        /// <summary>
        /// ID of group (https://steamcommunity.com/gid/103582791433980119/memberslistxml/?xml=1).
        /// </summary>
        public int GroupId { get; set; }
        /// <summary>
        /// Name of group.
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// App ID of game associated with group (https://steamcommunity.com/games/218620/memberslistxml/?xml=1).
        /// </summary>
        public int AppId { get; set; }
    }

    public class AppPath
    {
        public int AppId { get; set; }
        public string Path { get; set; }
    }

    public class Achievement
    {
        /// <summary>
        /// Achievement description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } 

        /// <summary>
        /// Human readable name, as shown on webpage, game libary, overlay, etc.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } 

        /// <summary>
        /// Is achievement hidden? 0 = false, else true.
        /// </summary>
        [JsonPropertyName("hidden")]
        public int Hidden { get; set; } 
        
        /// <summary>
        /// Path to icon when unlocked (colored).
        /// </summary>
        [JsonPropertyName("icon")]
        public string Icon { get; set; } 

        /// <summary>
        /// Path to icon when locked (grayed out).
        /// </summary>
        [JsonPropertyName("icongray")]
        public string IconGray { get; set; } 

        /// <summary>
        /// Internal name.
        /// </summary>
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