using System.Diagnostics;
using System.IO;
using Westwind.Utilities.Configuration;

namespace GoldbergGUI.Core
{
    public class Settings : AppConfiguration
    {
        public Settings() => InitializeDefaults();

        public static Settings Instance { get; } = new Settings();

        public bool AutomaticallyDownloadLatestGoldbergBuild { get; set; }

        public bool SaveSteamAppIdToSteamSettingsFolder { get; set; }

        protected override IConfigurationProvider OnCreateDefaultProvider(string sectionName, object configData)
        {
            var provider = new JsonFileConfigurationProvider<Settings>
            {
                JsonConfigurationFile = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!, "settings.json")
            };

            Provider = provider;
            return provider;
        }

        private void InitializeDefaults()
        {
            AutomaticallyDownloadLatestGoldbergBuild = true;
            SaveSteamAppIdToSteamSettingsFolder = false;
        }
    }
}