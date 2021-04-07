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

    class SteamCache
    {
        public string Filename { get; }
        public string SteamUri { get; }
        public Type ApiVersion { get; }
        public AppType SteamAppType { get; }
        public HashSet<SteamApp> Cache { get; set; } = new HashSet<SteamApp>();

        public SteamCache(string filename, string uri, Type apiVersion, AppType steamAppType)
        {
            Filename = filename;
            SteamUri = uri;
            ApiVersion = apiVersion;
            SteamAppType = steamAppType;
        }
    }

    // ReSharper disable once UnusedType.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SteamService : ISteamService
    {
        // ReSharper disable StringLiteralTypo
        private readonly Dictionary<AppType, SteamCache> _caches =
            new Dictionary<AppType, SteamCache>
            {
                {
                    AppType.Game,
                    new SteamCache(
                        "steamapps_games.json",
                        "https://api.steampowered.com/IStoreService/GetAppList/v1/" +
                        "?max_results=50000" +
                        "&include_games=1" +
                        "&key=" + Secrets.SteamWebApiKey(),
                        typeof(SteamAppsV1),
                        AppType.Game
                    )
                },
                {
                    AppType.DLC,
                    new SteamCache(
                        "steamapps_dlc.json",
                        "https://api.steampowered.com/IStoreService/GetAppList/v1/" +
                        "?max_results=50000" +
                        "&include_games=0" +
                        "&include_dlc=1" +
                        "&key=" + Secrets.SteamWebApiKey(),
                        typeof(SteamAppsV1),
                        AppType.DLC
                    )
                }
            };

        private static readonly Secrets Secrets = new Secrets();

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";

        private IMvxLog _log;

        public async Task Initialize(IMvxLog log)
        {
            //var (path, uri, jsonType, appType) = _caches[0];
            static SteamApps DeserializeSteamApps(Type type, string cacheString)
            {
                if (type == typeof(SteamAppsV1))
                    return JsonSerializer.Deserialize<SteamAppsV1>(cacheString);
                else if (type == typeof(SteamAppsV2))
                    return JsonSerializer.Deserialize<SteamAppsV2>(cacheString);
                return null;
            }

            foreach (var (k, c) in _caches)
            {
                _log = log;
                _log.Info($"Updating cache ({k.Value})...");
                var updateNeeded =
                    DateTime.Now.Subtract(File.GetLastWriteTimeUtc(c.Filename)).TotalDays >= 1;
                SteamApps steamApps;
                try
                {
                    var temp = await GetCache(updateNeeded, c.SteamUri, c.Filename)
                        .ConfigureAwait(false);
                    steamApps = DeserializeSteamApps(c.ApiVersion, temp);
                }
                catch (JsonException)
                {
                    _log.Error("Local cache broken, forcing update...");
                    var temp = await GetCache(true, c.SteamUri, c.Filename).ConfigureAwait(false);
                    steamApps = DeserializeSteamApps(c.ApiVersion, temp);
                }

                try
                {
                    var cacheRaw = new HashSet<SteamApp>(steamApps.AppList.Apps);
                    var cache = new HashSet<SteamApp>();
                    foreach (var steamApp in cacheRaw)
                    {
                        steamApp.type = c.SteamAppType;
                        cache.Add(steamApp);
                    }

                    c.Cache = cache;

                    _log.Info("Loaded cache into memory!");
                }
                catch (NullReferenceException e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
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
            var listOfAppsByName = _caches[AppType.Game].Cache.Search(x => x.Name)
                .SetCulture(StringComparison.OrdinalIgnoreCase)
                .ContainingAll(name.Split(' '));
            return listOfAppsByName;
        }

        public SteamApp GetAppByName(string name)
        {
            _log.Info($"Trying to get app {name}");
            var comparableName = Regex.Replace(name, Misc.AlphaNumOnlyRegex, "").ToLower();
            var app = _caches[AppType.Game].Cache.FirstOrDefault(x => x.CompareName(comparableName));
            if (app != null) _log.Info($"Successfully got app {app}");
            return app;
        }

        public SteamApp GetAppById(int appid)
        {
            _log.Info($"Trying to get app with ID {appid}");
            var app = _caches[AppType.Game].Cache.FirstOrDefault(x => x.AppId.Equals(appid));
            if (app != null) _log.Info($"Successfully got app {app}");
            return app;
        }

        public async Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb)
        {
            var dlcList = new List<SteamApp>();
            if (steamApp != null)
            {
                _log.Info($"Get DLC for App {steamApp}");
                var task = AppDetails.GetAsync(steamApp.AppId);
                var steamAppDetails = await task.ConfigureAwait(true);
                if (steamAppDetails.Type == AppType.Game.Value)
                {
                    steamAppDetails.DLC.ForEach(x =>
                    {
                        var result = _caches[AppType.DLC].Cache.FirstOrDefault(y => y.AppId.Equals(x))
                                     ?? new SteamApp {AppId = x, Name = $"Unknown DLC {x}"};
                        dlcList.Add(result);
                    });

                    dlcList.ForEach(x => _log.Debug($"{x.AppId}={x.Name}"));
                    _log.Info("Got DLC successfully...");

                    // Get DLC from SteamDB
                    // Get Cloudflare cookie
                    // Scrape and parse HTML page
                    // Add missing to DLC list

                    // Return current list if we don't intend to use SteamDB
                    if (!useSteamDb) return dlcList;
                    
                    try
                    {
                        var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

                        var client = new HttpClient();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                        _log.Info($"Get SteamDB App {steamApp}");
                        var httpCall = client.GetAsync(steamDbUri);
                        var response = await httpCall.ConfigureAwait(false);
                        _log.Debug(httpCall.Status.ToString());
                        _log.Debug(response.EnsureSuccessStatusCode().ToString());

                        var readAsStringAsync = response.Content.ReadAsStringAsync();
                        var responseBody = await readAsStringAsync.ConfigureAwait(false);
                        _log.Debug(readAsStringAsync.Status.ToString());

                        var parser = new HtmlParser();
                        var doc = parser.ParseDocument(responseBody);

                        var query1 = doc.QuerySelector("#dlc");
                        if (query1 != null)
                        {
                            _log.Info("Got list of DLC from SteamDB.");
                            var query2 = query1.QuerySelectorAll(".app");
                            foreach (var element in query2)
                            {
                                var dlcId = element.GetAttribute("data-appid");
                                var query3 = element.QuerySelectorAll("td");
                                var dlcName = query3 != null
                                    ? query3[1].Text().Replace("\n", "").Trim()
                                    : $"Unknown DLC {dlcId}";
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
                    catch (Exception e)
                    {
                        _log.Error("Could not get DLC from SteamDB! Skipping...");
                        _log.Error(e.ToString);
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