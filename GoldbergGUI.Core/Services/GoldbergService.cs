using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public Task<(string accountName, long userSteamId)> Initialize(IMvxLog log);
        public Task<(int appId, List<SteamApp> dlcList)> Read(string path);
        public Task Save(string path, int appId, List<SteamApp> dlcList);
        public bool GoldbergApplied(string path);
        public void Download();
        public void Extract(string archivePath);
        public Task GenerateInterfacesFile(string filePath);
    }
    
    // ReSharper disable once UnusedType.Global
    public class GoldbergService : IGoldbergService
    {
        private IMvxLog _log;
        //private const string GoldbergUrl = "https://mr_goldberg.gitlab.io/goldberg_emulator/";
        private readonly string _goldbergPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg");
        private static readonly string GlobalSettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Goldberg SteamEmu Saves");
        private readonly string _accountNamePath = Path.Combine(GlobalSettingsPath, "settings/account_name.txt");
        private readonly string _userSteamIdPath = Path.Combine(GlobalSettingsPath, "settings/user_steam_id.txt");
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
        public async Task<(string accountName, long userSteamId)> Initialize(IMvxLog log)
        {
            _log = log;
            var accountName = "Account name...";
            long steamId = -1;
            await Task.Run(() =>
            {
                if (File.Exists(_accountNamePath)) accountName = File.ReadLines(_accountNamePath).First().Trim();
                if (File.Exists(_userSteamIdPath) &&
                    !long.TryParse(File.ReadLines(_userSteamIdPath).First().Trim(), out steamId))
                    _log.Error("Invalid User Steam ID!");
            }).ConfigureAwait(false);

            return (accountName, steamId);
        }

        // If first time, call GenerateInterfaces
        // else try to read config
        public async Task<(int appId, List<SteamApp> dlcList)> Read(string path)
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
                        dlcList.Add(new SteamApp {AppId = Convert.ToInt32(match.Groups["id"].Value),
                            Name = match.Groups["name"].Value});
                }
            }
            return (appId, dlcList);
        }

        // If first time, rename original SteamAPI DLL to steam_api(64)_o.dll
        // If not, rename current SteamAPI DLL to steam_api(64).dll.backup
        // Copy Goldberg DLL to path
        // Save configuration files
        public async Task Save(string path, int appId, List<SteamApp> dlcList)
        {
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

            if (!Directory.Exists(Path.Combine(path, "steam_settings")))
            {
                Directory.CreateDirectory(Path.Combine(path, "steam_settings"));
            }
            
            await File.WriteAllTextAsync(Path.Combine(path, "steam_appid.txt"), appId.ToString()).ConfigureAwait(false);
            var dlcString = "";
            dlcList.ForEach(x => dlcString += $"{x}\n");
            await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "DLC.txt"), dlcString).ConfigureAwait(false);
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
            //throw new NotImplementedException();
            return true;
        }
        
        // Get webpage
        // Get commit, compare with local if exists, save it if false or missing
        // Get latest archive if mismatch, call Extract
        public void Download()
        {
            //throw new NotImplementedException();
        }

        // Empty subfolder ./goldberg/
        // Extract all from archive to subfolder ./goldberg/
        public void Extract(string archivePath)
        {
            //throw new NotImplementedException();
        }

        // https://gitlab.com/Mr_Goldberg/goldberg_emulator/-/blob/master/generate_interfaces_file.cpp
        // (maybe) check DLL date first
        public async Task GenerateInterfacesFile(string filePath)
        {
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