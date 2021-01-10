using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GoldbergGUI.Core.Models;
using MvvmCross.Logging;

namespace GoldbergGUI.Core.Services
{
    // downloads and updates goldberg emu
    // sets up config files
    // does file copy stuff
    public interface IGoldbergService
    {
        public Task<(string accountName, long userSteamId, string language)> Initialize(IMvxLog log);
        public Task<GoldbergConfiguration> Read(string path);
        public Task Save(string path, GoldbergConfiguration configuration);
        public Task<(string accountName, long steamId, string language)> GetGlobalSettings();
        public Task SetGlobalSettings(string accountName, long userSteamId, string language);
        public bool GoldbergApplied(string path);
        public Task<bool> Download();
        public Task Extract(string archivePath);
        public Task GenerateInterfacesFile(string filePath);
        public List<string> Languages();
    }

    // ReSharper disable once UnusedType.Global
    public class GoldbergService : IGoldbergService
    {
        private IMvxLog _log;
        private const string GoldbergUrl = "https://mr_goldberg.gitlab.io/goldberg_emulator/";
        private const string DefaultLanguage = "english";
        private readonly string _goldbergZipPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg.zip");
        private readonly string _goldbergPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg");

        private static readonly string GlobalSettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Goldberg SteamEmu Saves");

        private readonly string _accountNamePath = Path.Combine(GlobalSettingsPath, "settings/account_name.txt");
        private readonly string _userSteamIdPath = Path.Combine(GlobalSettingsPath, "settings/user_steam_id.txt");
        private readonly string _languagePath = Path.Combine(GlobalSettingsPath, "settings/language.txt");

        private readonly List<string> _interfaceNames = new List<string>
        {
            "SteamClient",
            "SteamGameServer",
            "SteamGameServerStats",
            "SteamUser",
            "SteamFriends",
            "SteamUtils",
            "SteamMatchMaking",
            "SteamMatchMakingServers",
            "STEAMUSERSTATS_INTERFACE_VERSION",
            "STEAMAPPS_INTERFACE_VERSION",
            "SteamNetworking",
            "STEAMREMOTESTORAGE_INTERFACE_VERSION",
            "STEAMSCREENSHOTS_INTERFACE_VERSION",
            "STEAMHTTP_INTERFACE_VERSION",
            "STEAMUNIFIEDMESSAGES_INTERFACE_VERSION",
            "STEAMUGC_INTERFACE_VERSION",
            "STEAMAPPLIST_INTERFACE_VERSION",
            "STEAMMUSIC_INTERFACE_VERSION",
            "STEAMMUSICREMOTE_INTERFACE_VERSION",
            "STEAMHTMLSURFACE_INTERFACE_VERSION_",
            "STEAMINVENTORY_INTERFACE_V",
            "SteamController",
            "SteamMasterServerUpdater",
            "STEAMVIDEO_INTERFACE_V"
        };

        // Call Download
        // Get global settings
        public async Task<(string accountName, long userSteamId, string language)> Initialize(IMvxLog log)
        {
            _log = log;

            var download = await Download().ConfigureAwait(false);
            if (download) await Extract(_goldbergZipPath).ConfigureAwait(false);
            return await GetGlobalSettings().ConfigureAwait(false);
        }

        public async Task<(string accountName, long steamId, string language)> GetGlobalSettings()
        {
            var accountName = "Account name...";
            long steamId = -1;
            var language = DefaultLanguage;
            await Task.Run(() =>
            {
                if (File.Exists(_accountNamePath)) accountName = File.ReadLines(_accountNamePath).First().Trim();
                if (File.Exists(_userSteamIdPath) &&
                    !long.TryParse(File.ReadLines(_userSteamIdPath).First().Trim(), out steamId) &&
                    steamId < 76561197960265729 && steamId > 76561202255233023)
                    _log.Error("Invalid User Steam ID!");
                if (File.Exists(_languagePath)) language = File.ReadLines(_languagePath).First().Trim();
            }).ConfigureAwait(false);
            return (accountName, steamId, language);
        }

        public async Task SetGlobalSettings(string accountName, long userSteamId, string language)
        {
            if (accountName != null && accountName != "Account name...")
                await File.WriteAllTextAsync(_accountNamePath, accountName).ConfigureAwait(false);
            else
                await File.WriteAllTextAsync(_accountNamePath, "Goldberg").ConfigureAwait(false);
            if (userSteamId >= 76561197960265729 && userSteamId <= 76561202255233023)
                await File.WriteAllTextAsync(_userSteamIdPath, userSteamId.ToString()).ConfigureAwait(false);
            else
                await Task.Run(() => File.Delete(_userSteamIdPath)).ConfigureAwait(false);
            if (language != null)
                await File.WriteAllTextAsync(_languagePath, language).ConfigureAwait(false);
            else
                await File.WriteAllTextAsync(_languagePath, DefaultLanguage).ConfigureAwait(false);
        }

        // If first time, call GenerateInterfaces
        // else try to read config
        public async Task<GoldbergConfiguration> Read(string path)
        {
            var appId = -1;
            var dlcList = new List<SteamApp>();
            var steamAppidTxt = Path.Combine(path, "steam_appid.txt");
            if (File.Exists(steamAppidTxt))
            {
                await Task.Run(() => int.TryParse(File.ReadLines(steamAppidTxt).First().Trim(), out appId))
                    .ConfigureAwait(false);
            }

            var dlcTxt = Path.Combine(path, "steam_settings", "DLC.txt");
            if (File.Exists(dlcTxt))
            {
                var readAllLinesAsync = await File.ReadAllLinesAsync(dlcTxt).ConfigureAwait(false);
                var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
                foreach (var line in readAllLinesAsync)
                {
                    var match = expression.Match(line);
                    if (match.Success)
                        dlcList.Add(new SteamApp
                        {
                            AppId = Convert.ToInt32(match.Groups["id"].Value),
                            Name = match.Groups["name"].Value
                        });
                }
            }

            return new GoldbergConfiguration
            {
                AppId = appId,
                DlcList = dlcList,
                Offline = File.Exists(Path.Combine(path, "steam_settings", "offline.txt")),
                DisableNetworking = File.Exists(Path.Combine(path, "steam_settings", "disable_networking.txt")),
                DisableOverlay = File.Exists(Path.Combine(path, "steam_settings", "disable_overlay.txt"))
            };
        }

        // If first time, rename original SteamAPI DLL to steam_api(64)_o.dll
        // If not, rename current SteamAPI DLL to steam_api(64).dll.backup
        // Copy Goldberg DLL to path
        // Save configuration files
        public async Task Save(string path, GoldbergConfiguration c)
        {
            // DLL setup
            const string x86Name = "steam_api";
            const string x64Name = "steam_api64";
            if (File.Exists(Path.Combine(path, $"{x86Name}.dll")))
            {
                CopyDllFiles(path, x86Name);
            }

            if (File.Exists(Path.Combine(path, $"{x64Name}.dll")))
            {
                CopyDllFiles(path, x64Name);
            }

            // Create steam_settings folder if missing
            if (!Directory.Exists(Path.Combine(path, "steam_settings")))
            {
                Directory.CreateDirectory(Path.Combine(path, "steam_settings"));
            }

            // create steam_appid.txt
            await File.WriteAllTextAsync(Path.Combine(path, "steam_appid.txt"), c.AppId.ToString()).ConfigureAwait(false);

            // DLC
            if (c.DlcList.Count > 0)
            {
                var dlcString = "";
                c.DlcList.ForEach(x => dlcString += $"{x}\n");
                await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "DLC.txt"), dlcString)
                    .ConfigureAwait(false);
            }
            else
            {
                if (File.Exists(Path.Combine(path, "steam_settings", "DLC.txt")))
                    File.Delete(Path.Combine(path, "steam_settings", "DLC.txt"));
            }

            // Offline
            if (c.Offline)
            {
                await File.Create(Path.Combine(path, "steam_settings", "offline.txt")).DisposeAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                File.Delete(Path.Combine(path, "steam_settings", "offline.txt"));
            }

            // Disable Networking
            if (c.DisableNetworking)
            {
                await File.Create(Path.Combine(path, "steam_settings", "disable_networking.txt")).DisposeAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                File.Delete(Path.Combine(path, "steam_settings", "disable_networking.txt"));
            }

            // Disable Overlay
            if (c.DisableOverlay)
            {
                await File.Create(Path.Combine(path, "steam_settings", "disable_overlay.txt")).DisposeAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                File.Delete(Path.Combine(path, "steam_settings", "disable_overlay.txt"));
            }
        }

        private void CopyDllFiles(string path, string name)
        {
            var steamApiDll = Path.Combine(path, $"{name}.dll");
            var originalDll = Path.Combine(path, $"{name}_o.dll");
            var guiBackup = Path.Combine(path, $"{name}.dll.GOLDBERGGUIBACKUP");
            var goldbergDll = Path.Combine(_goldbergPath, $"{name}.dll");

            if (!File.Exists(originalDll))
                File.Move(steamApiDll, originalDll);
            else
            {
                File.Move(steamApiDll, guiBackup, true);
                File.SetAttributes(guiBackup, FileAttributes.Hidden);
            }

            File.Copy(goldbergDll, steamApiDll);
        }

        public bool GoldbergApplied(string path)
        {
            var steamSettingsDirExists = Directory.Exists(Path.Combine(path, "steam_settings"));
            var steamAppIdTxtExists = File.Exists(Path.Combine(path, "steam_appid.txt"));
            return steamSettingsDirExists && steamAppIdTxtExists;
        }

        // Get webpage
        // Get job id, compare with local if exists, save it if false or missing
        // Get latest archive if mismatch, call Extract
        public async Task<bool> Download()
        {
            var value = false;
            _log.Debug("Download");
            if (!Directory.Exists(_goldbergPath)) Directory.CreateDirectory(_goldbergPath);
            var client = new HttpClient();
            var response = await client.GetAsync(GoldbergUrl).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var regex = new Regex(
                @"https:\/\/gitlab\.com\/Mr_Goldberg\/goldberg_emulator\/-\/jobs\/(?<jobid>.*)\/artifacts\/download");
            var jobIdPath = Path.Combine(_goldbergPath, "job_id");
            var match = regex.Match(body);
            var downloadUrl = match.Value;
            if (File.Exists(jobIdPath))
            {
                var jobIdLocal = Convert.ToInt32(File.ReadLines(jobIdPath).First().Trim());
                var jobIdRemote = Convert.ToInt32(match.Groups["jobid"].Value);
                _log.Debug($"job_id: local {jobIdLocal}; remote {jobIdRemote}");
                if (!jobIdLocal.Equals(jobIdRemote))
                {
                    await StartDownload(client, downloadUrl).ConfigureAwait(false);
                    value = true;
                }
            }
            else
            {
                await StartDownload(client, downloadUrl).ConfigureAwait(false);
                value = true;
            }

            return value;
        }

        private async Task StartDownload(HttpClient client, string downloadUrl)
        {
            _log.Debug(downloadUrl);
            await using var fileStream = File.OpenWrite(_goldbergZipPath);
            //client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead)
            var task = GetFileAsync(client, downloadUrl, fileStream).ConfigureAwait(false);
            await task;
            if (task.GetAwaiter().IsCompleted)
            {
                _log.Info("Download finished!");
            }
        }

        private static async Task GetFileAsync(HttpClient client, string requestUri, Stream destination,
            CancellationToken cancelToken = default)
        {
            var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancelToken)
                .ConfigureAwait(false);
            await using var download = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await download.CopyToAsync(destination, cancelToken).ConfigureAwait(false);
            if (destination.CanSeek) destination.Position = 0;
        }

        // Empty subfolder ./goldberg/
        // Extract all from archive to subfolder ./goldberg/
        public async Task Extract(string archivePath)
        {
            _log.Debug("Extract");
            await Task.Run(() =>
            {
                Directory.Delete(_goldbergPath, true);
                ZipFile.ExtractToDirectory(archivePath, _goldbergPath);
            }).ConfigureAwait(false);
            _log.Debug("Extract done!");
        }

        // https://gitlab.com/Mr_Goldberg/goldberg_emulator/-/blob/master/generate_interfaces_file.cpp
        // (maybe) check DLL date first
        public async Task GenerateInterfacesFile(string filePath)
        {
            _log.Debug($"GenerateInterfacesFile {filePath}");
            //throw new NotImplementedException();
            // Get DLL content
            var result = new HashSet<string>();
            var dllContent = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            // find interfaces
            foreach (var name in _interfaceNames)
            {
                FindInterfaces(ref result, dllContent, new Regex($"{name}\\d{{3}}"));
                if (!FindInterfaces(ref result, dllContent, new Regex(@"STEAMCONTROLLER_INTERFACE_VERSION\d{3}")))
                {
                    FindInterfaces(ref result, dllContent, new Regex("STEAMCONTROLLER_INTERFACE_VERSION"));
                }
            }

            var dirPath = Path.GetDirectoryName(filePath);
            if (dirPath == null) return;
            await using var destination = File.CreateText(dirPath + "/steam_interfaces.txt");
            foreach (var s in result)
            {
                await destination.WriteLineAsync(s).ConfigureAwait(false);
            }
        }

        public List<string> Languages() => new List<string>
        {
            DefaultLanguage,
            "arabic",
            "bulgarian",
            "schinese",
            "tchinese",
            "czech",
            "danish",
            "dutch",
            "finnish",
            "french",
            "german",
            "greek",
            "hungarian",
            "italian",
            "japanese",
            "koreana",
            "norwegian",
            "polish",
            "portuguese",
            "brazilian",
            "romanian",
            "russian",
            "spanish",
            "swedish",
            "thai",
            "turkish",
            "ukrainian"
        };

        private static bool FindInterfaces(ref HashSet<string> result, string dllContent, Regex regex)
        {
            var success = false;
            var matches = regex.Matches(dllContent);
            foreach (Match match in matches)
            {
                success = true;
                //result += $@"{match.Value}\n";
                result.Add(match.Value);
            }

            return success;
        }
    }
}