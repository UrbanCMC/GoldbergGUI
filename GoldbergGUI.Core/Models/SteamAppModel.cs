using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GoldbergGUI.Core.Utils;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
namespace GoldbergGUI.Core.Models
{
    public class SteamApp
    {
        private string _name;
        private string _comparableName;
        [JsonPropertyName("appid")] public int AppId { get; set; }

        /// <summary>
        /// Name of Steam app
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _comparableName = Regex.Replace(value, Misc.AlphaNumOnlyRegex, "").ToLower();
            }
        }
        
        /// <summary>
        /// Trimmed and cleaned name of Steam app, used for comparisons.
        /// </summary>
        public bool CompareName(string value) => _comparableName.Equals(value);

        /// <summary>
        /// App type (Game, DLC, ...)
        /// </summary>
        public AppType type { get; set; }

        public override string ToString()
        {
            return $"{AppId}={Name}";
        }

        [JsonPropertyName("last_modified")] public long LastModified { get; set; }

        [JsonPropertyName("price_change_number")]
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

    public class AppType
    {
        private AppType(string value) => Value = value;

        public string Value { get; }

        public static AppType Game { get; } = new AppType("game");
        public static AppType DLC { get; } = new AppType("dlc");
        public static AppType Music { get; } = new AppType("music");
        public static AppType Demo { get; } = new AppType("demo");
        public static AppType Ad { get; } = new AppType("advertising");
        public static AppType Mod { get; } = new AppType("mod");
        public static AppType Video { get; } = new AppType("video");
    }
}