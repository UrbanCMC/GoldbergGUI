using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GoldbergGUI.Core.Utils;
using SQLite;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
namespace GoldbergGUI.Core.Models
{
    [Table("steamapp")]
    public class SteamApp
    {
        private string _name;

        [JsonPropertyName("appid")]
        [Column("appid")]
        [PrimaryKey]
        public int AppId { get; set; }

        /// <summary>
        /// Name of Steam app
        /// </summary>
        [JsonPropertyName("name")]
        [Column("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                ComparableName = Regex.Replace(value, Misc.AlphaNumOnlyRegex, "").ToLower();
            }
        }

        [Column("comparable_name")]
        public string ComparableName { get; private set; }

        /// <summary>
        /// App type (Game, DLC, ...)
        /// </summary>
        [Column("type")]
        public string type { get; set; }

        public override string ToString()
        {
            return $"{AppId}={Name}";
        }

        [JsonPropertyName("last_modified")]
        [Ignore]
        public long LastModified { get; set; }

        [JsonPropertyName("price_change_number")]
        [Ignore]
        public long PriceChangeNumber { get; set; }
    }

    public class AppList
    {
        [JsonPropertyName("apps")] public List<SteamApp> Apps { get; set; }

        [JsonPropertyName("have_more_results")]
        public bool HaveMoreResults { get; set; }

        [JsonPropertyName("last_appid")] public long LastAppid { get; set; }
    }

    public class SteamApps
    {
        public virtual AppList AppList { get; set; }
    }

    public class SteamAppsV2 : SteamApps
    {
        [JsonPropertyName("applist")] public override AppList AppList { get; set; }
    }

    public class SteamAppsV1 : SteamApps
    {
        [JsonPropertyName("response")] public override AppList AppList { get; set; }
    }

    /*[Table("apptype")]
    public class AppType
    {
        private AppType(string value)
        {
            var db = new SQLiteConnection("steamapps.db");
            db.CreateTable<AppType>();
            Value = value;
        }

        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; }

        [Column("value")]
        public string Value { get; }

        [Ignore] public static AppType Game { get; } = new AppType("game");
        [Ignore] public static AppType DLC { get; } = new AppType("dlc");
        [Ignore] public static AppType Music { get; } = new AppType("music");
        [Ignore] public static AppType Demo { get; } = new AppType("demo");
        [Ignore] public static AppType Ad { get; } = new AppType("advertising");
        [Ignore] public static AppType Mod { get; } = new AppType("mod");
        [Ignore] public static AppType Video { get; } = new AppType("video");
    }*/
}