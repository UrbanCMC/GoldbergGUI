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

        [JsonPropertyName("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _comparableName = Regex.Replace(value, Misc.SpecialCharsRegex, "").ToLower();
            }
        }

        public bool CompareName(string value)
        {
            return _comparableName.Equals(value);
        }

        public override string ToString()
        {
            return $"{AppId}={Name}";
        }
    }

    public class AppList
    {
        [JsonPropertyName("apps")] public List<SteamApp> Apps { get; set; }
    }

    public class SteamApps
    {
        [JsonPropertyName("applist")] public AppList AppList { get; set; }
    }
    public static class AppType
    {
        public const string Game = "game";
        public const string DLC = "dlc";
        public const string Music = "music";
        public const string Demo = "demo";
        public const string Ad = "advertising";
        public const string Mod = "mod";
        public const string Video = "video";
    }
}