using System.Collections.Generic;

namespace GoldbergGUI.Core.Models
{
    public class GoldbergConfiguration
    {
        public int AppId { get; set; }
        public List<SteamApp> DlcList { get; set; }
        public bool Offline { get; set; }
        public bool DisableNetworking { get; set; }
        public bool DisableOverlay { get; set; }
    }
}