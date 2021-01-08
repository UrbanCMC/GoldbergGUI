﻿using System.Collections.Generic;
 using System.Collections.ObjectModel;
 using System.IO;
 using System.Linq;
 using System.Threading.Tasks;
 using GoldbergGUI.Core.Models;
 using GoldbergGUI.Core.Services;
 using Microsoft.Win32;
 using MvvmCross.Commands;
 using MvvmCross.Logging;
 using MvvmCross.ViewModels;

 namespace GoldbergGUI.Core.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MainViewModel : MvxViewModel
    {
        private string _dllPath;
        private string _gameName;

        private int _appId;

        //private SteamApp _currentGame;
        private ObservableCollection<SteamApp> _dlcs;
        private string _accountName;
        private long _steamId;
        private bool _offline;
        private bool _disableNetworking;
        private bool _disableOverlay;

        private readonly ISteamService _steam;
        private readonly IGoldbergService _goldberg;
        private readonly IMvxLog _log;
        private bool _mainWindowEnabled;

        public MainViewModel(ISteamService steam, IGoldbergService goldberg, IMvxLog log)
        {
            _steam = steam;
            _goldberg = goldberg;
            _log = log;
        }

        public override void Prepare()
        {
            base.Prepare();
            Task.Run(async () =>
            {
                MainWindowEnabled = false;
                ResetForm();
                await _steam.Initialize(_log).ConfigureAwait(false);
                var (accountName, userSteamId) = await _goldberg.Initialize(_log).ConfigureAwait(false);
                AccountName = accountName;
                SteamId = userSteamId;
                MainWindowEnabled = true;
            });
        }

        public override async Task Initialize()
        {
            _log.Info("Init");
            await base.Initialize().ConfigureAwait(false);
        }

        // PROPERTIES //
        
        public string DllPath
        {
            get => _dllPath;
            private set
            {
                _dllPath = value;
                RaisePropertyChanged(() => DllPath);
            }
        }

        public string GameName
        {
            get => _gameName;
            set
            {
                _gameName = value;
                RaisePropertyChanged(() => GameName);
            }
        }

        public int AppId
        {
            get => _appId;
            set
            {
                _appId = value;
                RaisePropertyChanged(() => AppId);
                Task.Run(async () => await GetNameById().ConfigureAwait(false));
            }
        }

        // ReSharper disable once InconsistentNaming
        public ObservableCollection<SteamApp> DLCs
        {
            get => _dlcs;
            private set
            {
                _dlcs = value;
                RaisePropertyChanged(() => DLCs);
            }
        }

        public string AccountName
        {
            get => _accountName;
            set
            {
                _accountName = value;
                RaisePropertyChanged(() => AccountName);
            }
        }

        public long SteamId
        {
            get => _steamId;
            set
            {
                _steamId = value;
                RaisePropertyChanged(() => SteamId);
            }
        }

        public bool Offline
        {
            get => _offline;
            set
            {
                _offline = value;
                RaisePropertyChanged(() => Offline);
            }
        }

        public bool DisableNetworking
        {
            get => _disableNetworking;
            set
            {
                _disableNetworking = value;
                RaisePropertyChanged(() => DisableNetworking);
            }
        }

        public bool DisableOverlay
        {
            get => _disableOverlay;
            set
            {
                _disableOverlay = value;
                RaisePropertyChanged(() => DisableOverlay);
            }
        }

        public bool MainWindowEnabled
        {
            get => _mainWindowEnabled;
            set
            {
                _mainWindowEnabled = value;
                RaisePropertyChanged(() => MainWindowEnabled);
            }
        }

        // COMMANDS //
        
        public IMvxCommand OpenFileCommand => new MvxAsyncCommand(OpenFile);

        private async Task OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SteamAPI DLL|steam_api.dll;steam_api64.dll|" +
                         "All files (*.*)|*.*",
                Multiselect = false,
                Title = "Select SteamAPI DLL..."
            };
            if (dialog.ShowDialog() != true)
            {
                _log.Warn("File selection canceled.");
                return;
            }
            DllPath = dialog.FileName;
            //_goldberg.GenerateInterfacesFile(filePath);
            await ReadConfig().ConfigureAwait(false);
        }

        public IMvxCommand FindIdCommand => new MvxCommand(FindId);

        private void FindId()
        {
            if (GameName.Contains("Game name..."))
            {
                _log.Error("No game name entered!");
                return;
            }
            var appByName = _steam.GetAppByName(_gameName);
            if (appByName != null)
            {
                GameName = appByName.Name;
                AppId = appByName.AppId;
            }
            else
            {
                _log.Warn("Steam app could not be found!");
            }
        }

        //public IMvxCommand GetNameByIdCommand => new MvxAsyncCommand(GetNameById);

        private async Task GetNameById()
        {
            if (AppId <= 0)
            {
                _log.Error("Invalid Steam App!");
                return;
            }
            var steamApp = await Task.Run(() => _steam.GetAppById(AppId)).ConfigureAwait(false);
            if (steamApp != null) GameName = steamApp.Name;
        }

        public IMvxCommand GetListOfDlcCommand => new MvxAsyncCommand(GetListOfDlc);

        private async Task GetListOfDlc()
        {
            if (AppId <= 0)
            {
                _log.Error("Invalid Steam App!");
                return;
            }
            MainWindowEnabled = false;
            var listOfDlc = await _steam.GetListOfDlc(new SteamApp {AppId = AppId, Name = GameName}, true).ConfigureAwait(false);
            DLCs = new MvxObservableCollection<SteamApp>(listOfDlc);
            MainWindowEnabled = true;
        }

        public IMvxCommand SaveConfigCommand => new MvxAsyncCommand(SaveConfig);

        private async Task SaveConfig()
        {
            if (DllPath.Contains("Path to game's steam_api(64).dll"))
            {
                _log.Error("No DLL selected!");
                return;
            }
            _log.Info("Saving...");
            if (!GetDllPathDir(out var dirPath)) return;
            MainWindowEnabled = false;
            await _goldberg.Save(dirPath, AppId, DLCs.ToList()).ConfigureAwait(false);
            MainWindowEnabled = true;
        }

        public IMvxCommand ResetConfigCommand => new MvxAsyncCommand(ResetConfig);

        private async Task ResetConfig()
        {
            if (DllPath.Contains("Path to game's steam_api(64).dll"))
            {
                _log.Error("No DLL selected!");
                return;
            }
            _log.Info("Reset form...");
            MainWindowEnabled = false;
            await ReadConfig().ConfigureAwait(false);
            MainWindowEnabled = true;
        }
        
        public IMvxCommand GenerateSteamInterfacesCommand => new MvxAsyncCommand(GenerateSteamInterfaces);

        private async Task GenerateSteamInterfaces()
        {
            if (DllPath.Contains("Path to game's steam_api(64).dll"))
            {
                _log.Error("No DLL selected!");
                return;
            }

            _log.Info("Generate steam_interfaces.txt...");
            MainWindowEnabled = false;
            await _goldberg.GenerateInterfacesFile(DllPath).ConfigureAwait(false);
            MainWindowEnabled = true;
        }
        
        // OTHER METHODS //
        
        private void ResetForm()
        {
            DllPath = "Path to game's steam_api(64).dll...";
            GameName = "Game name...";
            AppId = -1;
            DLCs = new ObservableCollection<SteamApp>();
            AccountName = "Account name...";
            SteamId = -1;
            Offline = false;
            DisableNetworking = false;
            DisableOverlay = false;
        }

        private async Task ReadConfig()
        {
            if (!GetDllPathDir(out var dirPath)) return;
            MainWindowEnabled = false;
            List<SteamApp> dlcList;
            (AppId, dlcList) = await _goldberg.Read(dirPath).ConfigureAwait(false);
            DLCs = new ObservableCollection<SteamApp>(dlcList);
            MainWindowEnabled = true;
        }

        private bool GetDllPathDir(out string dirPath)
        {
            if (DllPath.Contains("Path to game's steam_api(64).dll"))
            {
                _log.Error("No DLL selected!");
                dirPath = null;
                return false;
            }

            dirPath = Path.GetDirectoryName(DllPath);
            if (dirPath != null) return true;
            
            _log.Error($"Invalid directory for {DllPath}.");
            return false;
        }
    }
}