using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using MvvmCross.Logging;
using NinjaNye.SearchExtensions;
using SteamStorefrontAPI;

namespace GoldbergGUI.Core.Services
{
    // gets info from steam api
    public interface ISteamService
    {
        public Task Initialize(IMvxLog log);
        public IEnumerable<SteamApp> GetListOfAppsByName(string name);
        public SteamApp GetAppByName(string name);
        public SteamApp GetAppById(int appid);
        public Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb);
    }

    // ReSharper disable once UnusedType.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SteamService : ISteamService
    {
        // ReSharper disable StringLiteralTypo
        private readonly List<(string filename, string uri, string type)> _caches = new List<(string, string, string)>
        {
            (
                "steamapps.json",
                "https://api.steampowered.com/ISteamApps/GetAppList/v2/",
                null
            ),
            (
                "steamapps_games.json",
                "https://api.steampowered.com/IStoreService/GetAppList/v1/?include_games=1&key=" +
                Secrets.SteamWebApiKey(),
                AppType.Game
            ),
            (
                "steamapps_dlc.json",
                "https://api.steampowered.com/IStoreService/GetAppList/v1/?include_games=0&include_dlc=1&key=" +
                Secrets.SteamWebApiKey(),
                AppType.DLC
            )
        };
        // ReSharper enable StringLiteralTypo

        private static readonly Secrets Secrets = new Secrets();

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";

        private HashSet<SteamApp> _cache = new HashSet<SteamApp>();
        private IMvxLog _log;

        public async Task Initialize(IMvxLog log)
        {
            var path = _caches.First().filename;
            var uri = _caches.First().uri;
            _log = log;
            _log.Info("Updating cache...");
            var updateNeeded = DateTime.Now.Subtract(File.GetLastWriteTimeUtc(path)).TotalDays >= 1;
            var cacheString = await GetCache(updateNeeded, uri, path).ConfigureAwait(false);
            SteamApps steamApps;
            try
            {
                steamApps = JsonSerializer.Deserialize<SteamApps>(cacheString);
            }
            catch (JsonException)
            {
                cacheString = await GetCache(true, uri, path).ConfigureAwait(false);
                steamApps = JsonSerializer.Deserialize<SteamApps>(cacheString);
            }

            _cache = new HashSet<SteamApp>(steamApps.AppList.Apps);
            _log.Info("Loaded cache into memory!");
        }

        private async Task<string> GetCache(bool updateNeeded, string steamUri, string cachePath)
        {
            string cacheString;
            if (updateNeeded)
            {
                _log.Info("Getting content from API...");
                var client = new HttpClient();
                var response = await client.GetAsync(steamUri).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                _log.Info("Got content from API successfully. Writing to file...");
                await File.WriteAllTextAsync(cachePath, responseBody, Encoding.UTF8).ConfigureAwait(false);

                _log.Info("Cache written to file successfully.");
                cacheString = responseBody;
            }
            else
            {
                _log.Info("Cache already up to date!");
                cacheString = await File.ReadAllTextAsync(cachePath).ConfigureAwait(false);
            }

            return cacheString;
        }

        public IEnumerable<SteamApp> GetListOfAppsByName(string name)
        {
            var listOfAppsByName = _cache.Search(x => x.Name)
                .SetCulture(StringComparison.OrdinalIgnoreCase)
                .ContainingAll(name.Split(' '));
            return listOfAppsByName;
            /*var filteredList = new HashSet<SteamApp>();
            foreach (var steamApp in listOfAppsByName)
            {
                try
                {
                    var task = AppDetails.GetAsync(steamApp.AppId);
                    var details = await task.ConfigureAwait(false);
                    if (details?.Type != null && details.Type == AppType.Game) filteredList.Add(steamApp);
                }
                catch (Exception e)
                {
                    _log.Debug($"{e.GetType()}: {steamApp}");
                }
            }
            return filteredList;*/
        }

        public SteamApp GetAppByName(string name)
        {
            _log.Info($"Trying to get app {name}");
            var comparableName = Regex.Replace(name, Misc.SpecialCharsRegex, "").ToLower();
            var app = _cache.FirstOrDefault(x => x.CompareName(comparableName));
            if (app != null) _log.Info($"Successfully got app {app}");
            return app;
        }

        public SteamApp GetAppById(int appid)
        {
            _log.Info($"Trying to get app with ID {appid}");
            var app = _cache.FirstOrDefault(x => x.AppId.Equals(appid));
            if (app != null) _log.Info($"Successfully got app {app}");
            return app;
        }

        public async Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb)
        {
            _log.Info("Get DLC");
            var dlcList = new List<SteamApp>();
            if (steamApp != null)
            {
                var task = AppDetails.GetAsync(steamApp.AppId);
                var steamAppDetails = await task.ConfigureAwait(true);
                if (steamAppDetails.Type == AppType.Game)
                {
                    steamAppDetails.DLC.ForEach(x =>
                    {
                        var result = _cache.FirstOrDefault(y => y.AppId.Equals(x)) ??
                                     new SteamApp {AppId = x, Name = $"Unknown DLC {x}"};
                        dlcList.Add(result);
                    });

                    dlcList.ForEach(x => _log.Debug($"{x.AppId}={x.Name}"));
                    _log.Info("Got DLC successfully...");

                    // Get DLC from SteamDB
                    // Get Cloudflare cookie
                    // Scrape and parse HTML page
                    // Add missing to DLC list
                    if (useSteamDb)
                    {
                        var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

                        /* var handler = new ClearanceHandler();
                
                        var client = new HttpClient(handler);
        
                        var content = client.GetStringAsync(steamDbUri).Result;
                        _log.Debug(content); */

                        var client = new HttpClient();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                        _log.Info("Get SteamDB App");
                        var httpCall = client.GetAsync(steamDbUri);
                        var response = await httpCall.ConfigureAwait(false);
                        _log.Debug(httpCall.Status.ToString());
                        _log.Debug(response.EnsureSuccessStatusCode().ToString());

                        var readAsStringAsync = response.Content.ReadAsStringAsync();
                        var responseBody = await readAsStringAsync.ConfigureAwait(false);
                        _log.Debug(readAsStringAsync.Status.ToString());

                        var parser = new HtmlParser();
                        var doc = parser.ParseDocument(responseBody);
                        // Console.WriteLine(doc.DocumentElement.OuterHtml);

                        var query1 = doc.QuerySelector("#dlc");
                        if (query1 != null)
                        {
                            var query2 = query1.QuerySelectorAll(".app");
                            foreach (var element in query2)
                            {
                                var dlcId = element.GetAttribute("data-appid");
                                var dlcName = $"Unknown DLC {dlcId}";
                                var query3 = element.QuerySelectorAll("td");
                                if (query3 != null) dlcName = query3[1].Text().Replace("\n", "").Trim();

                                var dlcApp = new SteamApp {AppId = Convert.ToInt32(dlcId), Name = dlcName};
                                var i = dlcList.FindIndex(x => x.AppId.Equals(dlcApp.AppId));
                                if (i > -1)
                                {
                                    if (dlcList[i].Name.Contains("Unknown DLC")) dlcList[i] = dlcApp;
                                }
                                else
                                {
                                    dlcList.Add(dlcApp);
                                }
                            }

                            dlcList.ForEach(x => _log.Debug($"{x.AppId}={x.Name}"));
                            _log.Info("Got DLC from SteamDB successfully...");
                        }
                        else
                        {
                            _log.Error("Could not get DLC from SteamDB!");
                        }
                    }
                }
                else
                {
                    _log.Error("Could not get DLC: Steam App is not of type \"game\"");
                }
            }
            else
            {
                _log.Error("Could not get DLC: Invalid Steam App");
            }

            return dlcList;
        }
    }
}