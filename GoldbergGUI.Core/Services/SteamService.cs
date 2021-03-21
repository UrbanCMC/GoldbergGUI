using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using MvvmCross.Logging;
using NinjaNye.SearchExtensions;
using SQLite;
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
        public string SteamUri { get; }
        public Type ApiVersion { get; }
        public string SteamAppType { get; }

        public SteamCache(string uri, Type apiVersion, string steamAppType)
        {
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
        private readonly Dictionary<string, SteamCache> _caches =
            new Dictionary<string, SteamCache>
            {
                {
                    AppTypeGame,
                    new SteamCache(
                        "https://api.steampowered.com/IStoreService/GetAppList/v1/" +
                        "?max_results=50000" +
                        "&include_games=1" +
                        "&key=" + Secrets.SteamWebApiKey(),
                        typeof(SteamAppsV1),
                        AppTypeGame
                    )
                },
                {
                    AppTypeDlc,
                    new SteamCache(
                        "https://api.steampowered.com/IStoreService/GetAppList/v1/" +
                        "?max_results=50000" +
                        "&include_games=0" +
                        "&include_dlc=1" +
                        "&key=" + Secrets.SteamWebApiKey(),
                        typeof(SteamAppsV1),
                        AppTypeDlc
                    )
                }
            };

        private static readonly Secrets Secrets = new Secrets();

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";

        private const string AppTypeGame = "game";
        private const string AppTypeDlc = "dlc";
        private const string Database = "steamapps.db";

        private IMvxLog _log;

        private SQLiteConnection _db;

        public async Task Initialize(IMvxLog log)
        {
            //var (path, uri, jsonType, appType) = _caches[0];
            static SteamApps DeserializeSteamApps(Type type, string cacheString)
            {
                return type == typeof(SteamAppsV2)
                    ? (SteamApps) JsonSerializer.Deserialize<SteamAppsV2>(cacheString)
                    : JsonSerializer.Deserialize<SteamAppsV1>(cacheString);
            }

            _log = log;
            _db = new SQLiteConnection(Database);
            _db.CreateTable<SteamApp>();

           if (DateTime.Now.Subtract(File.GetLastWriteTimeUtc(Database)).TotalDays >= 1 || !_db.Table<SteamApp>().Any())
            {
                foreach (var (appType, steamCache) in _caches)
                {
                    _log.Info($"Updating cache ({appType})...");
                    bool haveMoreResults;
                    long lastAppId = 0;
                    var client = new HttpClient();
                    var cacheRaw = new HashSet<SteamApp>();
                    do
                    {
                        var response = lastAppId > 0
                            ? await client.GetAsync($"{steamCache.SteamUri}&last_appid={lastAppId}")
                                .ConfigureAwait(false)
                            : await client.GetAsync(steamCache.SteamUri).ConfigureAwait(false);
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var steamApps = DeserializeSteamApps(steamCache.ApiVersion, responseBody);
                        foreach (var appListApp in steamApps.AppList.Apps) cacheRaw.Add(appListApp);
                        haveMoreResults = steamApps.AppList.HaveMoreResults;
                        lastAppId = steamApps.AppList.LastAppid;
                    } while (haveMoreResults);

                    var cache = new HashSet<SteamApp>();
                    foreach (var steamApp in cacheRaw)
                    {
                        steamApp.type = steamCache.SteamAppType;
                        steamApp.ComparableName = Regex.Replace(steamApp.Name, Misc.AlphaNumOnlyRegex, "").ToLower();
                        cache.Add(steamApp);
                    }

                    _db.InsertAll(cache);
                }
            }
        }

        public IEnumerable<SteamApp> GetListOfAppsByName(string name)
        {
            var listOfAppsByName = _db.Table<SteamApp>()
                .Where(x => x.type == AppTypeGame).Search(x => x.Name)
                .SetCulture(StringComparison.OrdinalIgnoreCase)
                .ContainingAll(name.Split(' '));
            return listOfAppsByName;
        }

        public SteamApp GetAppByName(string name)
        {
            _log.Info($"Trying to get app {name}");
            var comparableName = Regex.Replace(name, Misc.AlphaNumOnlyRegex, "").ToLower();
            var app = _db.Table<SteamApp>()
                .FirstOrDefault(x => x.type == AppTypeGame && x.ComparableName.Equals(comparableName));
            if (app != null) _log.Info($"Successfully got app {app}");
            return app;
        }

        public SteamApp GetAppById(int appid)
        {
            _log.Info($"Trying to get app with ID {appid}");
            var app = _db.Table<SteamApp>().Where(x => x.type == AppTypeGame)
                .FirstOrDefault(x => x.AppId.Equals(appid));
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
                if (steamAppDetails.Type == AppTypeGame)
                {
                    steamAppDetails.DLC.ForEach(x =>
                    {
                        var result = _db.Table<SteamApp>().Where(z => z.type == AppTypeDlc)
                                         .FirstOrDefault(y => y.AppId.Equals(x))
                                     ?? new SteamApp {AppId = x, Name = $"Unknown DLC {x}"};
                        dlcList.Add(result);
                    });

                    dlcList.ForEach(x => _log.Debug($"{x.AppId}={x.Name}"));
                    _log.Info("Got DLC successfully...");

                    // Get DLC from SteamDB
                    // Get Cloudflare cookie
                    // Scrape and parse HTML page
                    // Add missing to DLC list

                    // ReSharper disable once InvertIf
                    if (useSteamDb)
                    {
                        var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

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

                        var query1 = doc.QuerySelector("#dlc");
                        if (query1 != null)
                        {
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