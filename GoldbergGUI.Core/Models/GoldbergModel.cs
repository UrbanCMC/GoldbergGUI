using System.Collections.Generic;

namespace GoldbergGUI.Core.Models
{
    public class GoldbergGlobalConfiguration
    {
        public string AccountName { get; set; }
        public long UserSteamId { get; set; }
        public string Language { get; set; }
    }
    public class GoldbergConfiguration
    {
        public int AppId { get; set; }
        public List<SteamApp> DlcList { get; set; }
        public bool Offline { get; set; }
        public bool DisableNetworking { get; set; }
        public bool DisableOverlay { get; set; }
    }
}