using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using MvvmCross.Logging;

namespace GoldbergGUI.Core.Services
{
    // downloads and updates goldberg emu
    // sets up config files
    // does file copy stuff
    public interface IGoldbergService
    {
        public Task<GoldbergGlobalConfiguration> Initialize(IMvxLog log);
        public Task<GoldbergConfiguration> Read(string path);
        public Task Save(string path, GoldbergConfiguration configuration);
        public Task<GoldbergGlobalConfiguration> GetGlobalSettings();
        public Task SetGlobalSettings(GoldbergGlobalConfiguration configuration);

        public bool GoldbergApplied(string path);

        // public Task<bool> Download();
        // public Task Extract(string archivePath);
        public Task GenerateInterfacesFile(string filePath);
        public List<string> Languages();
    }

    // ReSharper disable once UnusedType.Global
    public class GoldbergService : IGoldbergService
    {
        private IMvxLog _log;
        private const string DefaultAccountName = "Mr_Goldberg";
        private const long DefaultSteamId = 76561197960287930;
        private const string DefaultLanguage = "english";
        private const string GoldbergUrl = "https://mr_goldberg.gitlab.io/goldberg_emulator/";
        private readonly string _goldbergZipPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg.zip");
        private readonly string _goldbergPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg");

        private static readonly string GlobalSettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Goldberg SteamEmu Saves");

        private readonly string _accountNamePath = Path.Combine(GlobalSettingsPath, "settings/account_name.txt");
        private readonly string _userSteamIdPath = Path.Combine(GlobalSettingsPath, "settings/user_steam_id.txt");
        private readonly string _languagePath = Path.Combine(GlobalSettingsPath, "settings/language.txt");

        private readonly string _customBroadcastIpsPath =
            Path.Combine(GlobalSettingsPath, "settings/custom_broadcasts.txt");

        // ReSharper disable StringLiteralTypo
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
        public async Task<GoldbergGlobalConfiguration> Initialize(IMvxLog log)
        {
            _log = log;

            var download = await Download().ConfigureAwait(false);
            if (download)
            {
                await Extract(_goldbergZipPath).ConfigureAwait(false);
            }

            return await GetGlobalSettings().ConfigureAwait(false);
        }

        public async Task<GoldbergGlobalConfiguration> GetGlobalSettings()
        {
            _log.Info("Getting global settings...");
            var accountName = DefaultAccountName;
            var steamId = DefaultSteamId;
            var language = DefaultLanguage;
            var customBroadcastIps = new List<string>();
            if (!File.Exists(GlobalSettingsPath)) Directory.CreateDirectory(Path.Join(GlobalSettingsPath, "settings"));
            await Task.Run(() =>
            {
                if (File.Exists(_accountNamePath)) accountName = File.ReadLines(_accountNamePath).First().Trim();
                if (File.Exists(_userSteamIdPath) &&
                    !long.TryParse(File.ReadLines(_userSteamIdPath).First().Trim(), out steamId) &&
                    steamId < 76561197960265729 && steamId > 76561202255233023)
                {
                    _log.Error("Invalid User Steam ID! Using default Steam ID...");
                    steamId = DefaultSteamId;
                }

                if (File.Exists(_languagePath)) language = File.ReadLines(_languagePath).First().Trim();
                if (File.Exists(_customBroadcastIpsPath))
                    customBroadcastIps.AddRange(
                        File.ReadLines(_customBroadcastIpsPath).Select(line => line.Trim()));
            }).ConfigureAwait(false);
            _log.Info("Got global settings.");
            return new GoldbergGlobalConfiguration
            {
                AccountName = accountName,
                UserSteamId = steamId,
                Language = language,
                CustomBroadcastIps = customBroadcastIps
            };
        }

        public async Task SetGlobalSettings(GoldbergGlobalConfiguration c)
        {
            var accountName = c.AccountName;
            var userSteamId = c.UserSteamId;
            var language = c.Language;
            var customBroadcastIps = c.CustomBroadcastIps;
            _log.Info("Setting global settings...");
            // Account Name
            if (!string.IsNullOrEmpty(accountName))
            {
                _log.Info("Setting account name...");
                if (!File.Exists(_accountNamePath))
                    await File.Create(_accountNamePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_accountNamePath, accountName).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Invalid account name! Skipping...");
                if (!File.Exists(_accountNamePath))
                    await File.Create(_accountNamePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_accountNamePath, DefaultAccountName).ConfigureAwait(false);
            }

            // User SteamID
            if (userSteamId >= 76561197960265729 && userSteamId <= 76561202255233023)
            {
                _log.Info("Setting user Steam ID...");
                if (!File.Exists(_userSteamIdPath))
                    await File.Create(_userSteamIdPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_userSteamIdPath, userSteamId.ToString()).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Invalid user Steam ID! Skipping...");
                if (!File.Exists(_userSteamIdPath))
                    await File.Create(_userSteamIdPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_userSteamIdPath, DefaultSteamId.ToString()).ConfigureAwait(false);
            }

            // Language
            if (!string.IsNullOrEmpty(language))
            {
                _log.Info("Setting language...");
                if (!File.Exists(_languagePath))
                    await File.Create(_languagePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_languagePath, language).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Invalid language! Skipping...");
                if (!File.Exists(_languagePath))
                    await File.Create(_languagePath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_languagePath, DefaultLanguage).ConfigureAwait(false);
            }

            // Custom Broadcast IPs
            if (customBroadcastIps != null && customBroadcastIps.Count > 0)
            {
                _log.Info("Setting custom broadcast IPs...");
                var result =
                    customBroadcastIps.Aggregate("", (current, address) => $"{current}{address}\n");
                if (!File.Exists(_customBroadcastIpsPath))
                    await File.Create(_customBroadcastIpsPath).DisposeAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(_customBroadcastIpsPath, result).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Empty list of custom broadcast IPs! Skipping...");
                await Task.Run(() => File.Delete(_customBroadcastIpsPath)).ConfigureAwait(false);
            }
            _log.Info("Setting global configuration finished.");
        }

        // If first time, call GenerateInterfaces
        // else try to read config
        public async Task<GoldbergConfiguration> Read(string path)
        {
            _log.Info("Reading configuration...");
            var appId = -1;
            var dlcList = new List<SteamApp>();
            var steamAppidTxt = Path.Combine(path, "steam_appid.txt");
            if (File.Exists(steamAppidTxt))
            {
                _log.Info("Getting AppID...");
                await Task.Run(() => int.TryParse(File.ReadLines(steamAppidTxt).First().Trim(), out appId))
                    .ConfigureAwait(false);
            }
            else
            {
                _log.Info(@"""steam_appid.txt"" missing! Skipping...");
            }

            var dlcTxt = Path.Combine(path, "steam_settings", "DLC.txt");
            if (File.Exists(dlcTxt))
            {
                _log.Info("Getting DLCs...");
                var readAllLinesAsync = await File.ReadAllLinesAsync(dlcTxt).ConfigureAwait(false);
                var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
                // ReSharper disable once LoopCanBeConvertedToQuery
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
            else
            {
                _log.Info(@"""steam_settings/DLC.txt"" missing! Skipping...");
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
            _log.Info("Saving configuration...");
            // DLL setup
            _log.Info("Running DLL setup...");
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
            _log.Info("Saving settings...");
            if (!Directory.Exists(Path.Combine(path, "steam_settings")))
            {
                Directory.CreateDirectory(Path.Combine(path, "steam_settings"));
            }

            // create steam_appid.txt
            await File.WriteAllTextAsync(Path.Combine(path, "steam_appid.txt"), c.AppId.ToString())
                .ConfigureAwait(false);

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
            _log.Debug($"Goldberg applied? {(steamSettingsDirExists && steamAppIdTxtExists).ToString()}");
            return steamSettingsDirExists && steamAppIdTxtExists;
        }

        private async Task<bool> Download()
        {
            // Get webpage
            // Get job id, compare with local if exists, save it if false or missing
            // Get latest archive if mismatch, call Extract
            _log.Info("Initializing download...");
            if (!Directory.Exists(_goldbergPath)) Directory.CreateDirectory(_goldbergPath);
            var client = new HttpClient();
            var response = await client.GetAsync(GoldbergUrl).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var regex = new Regex(
                @"https:\/\/gitlab\.com\/Mr_Goldberg\/goldberg_emulator\/-\/jobs\/(?<jobid>.*)\/artifacts\/download");
            var jobIdPath = Path.Combine(_goldbergPath, "job_id");
            var match = regex.Match(body);
            if (File.Exists(jobIdPath))
            {
                try
                {
                    _log.Info("Check if update is needed...");
                    var jobIdLocal = Convert.ToInt32(File.ReadLines(jobIdPath).First().Trim());
                    var jobIdRemote = Convert.ToInt32(match.Groups["jobid"].Value);
                    _log.Debug($"job_id: local {jobIdLocal}; remote {jobIdRemote}");
                    if (jobIdLocal.Equals(jobIdRemote))
                    {
                        _log.Info("Latest Goldberg emulator is already available! Skipping...");
                        return false;
                    }
                }
                catch (Exception)
                {
                    _log.Error("An error occured, local Goldberg setup might be broken!");
                }
            }

            _log.Info("Starting download...");
            await StartDownload(match.Value).ConfigureAwait(false);
            return true;
        }

        private async Task StartDownload(string downloadUrl)
        {
            try
            {
                var client = new HttpClient();
                _log.Debug(downloadUrl);
                await using var fileStream = File.OpenWrite(_goldbergZipPath);
                //client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead)
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                var headResponse = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                var contentLength = headResponse.Content.Headers.ContentLength;
                await client.GetFileAsync(downloadUrl, fileStream).ContinueWith(async t =>
                {
                    await fileStream.DisposeAsync().ConfigureAwait(false);
                    var fileLength = new FileInfo(_goldbergZipPath).Length;
                    // Environment.Exit(128);
                    if (contentLength == fileLength)
                    {
                        _log.Info("Download finished!");
                    }
                    else
                    {
                        throw new Exception("File size does not match!");
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ShowErrorMessage();
                _log.Error(e.ToString);
                Environment.Exit(1);
            }
        }

        // Empty subfolder ./goldberg/
        // Extract all from archive to subfolder ./goldberg/
        private async Task Extract(string archivePath)
        {
            var errorOccured = false;
            _log.Debug("Start extraction...");
            Directory.Delete(_goldbergPath, true);
            Directory.CreateDirectory(_goldbergPath);
            using (var archive = await Task.Run(() => ZipFile.OpenRead(archivePath)).ConfigureAwait(false))
            {
                foreach (var entry in archive.Entries)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            var fullPath = Path.Combine(_goldbergPath, entry.FullName);
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            else
                            {
                                entry.ExtractToFile(fullPath, true);
                            }
                        }
                        catch (Exception e)
                        {
                            errorOccured = true;
                            _log.Error($"Error while trying to extract {entry.FullName}");
                            _log.Error(e.ToString);
                        }
                    }).ConfigureAwait(false);
                }
            }

            if (errorOccured)
            {
                ShowErrorMessage();
                _log.Warn("Error occured while extraction! Please setup Goldberg manually");
            }
            _log.Info("Extraction was successful!");
        }

        private void ShowErrorMessage()
        {
            if (Directory.Exists(_goldbergPath))
            {
                Directory.Delete(_goldbergPath, true);
            }

            Directory.CreateDirectory(_goldbergPath);
            MessageBox.Show("Could not setup Goldberg Emulator!\n" +
                            "Please download it manually and extract its content into the \"goldberg\" subfolder!");
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