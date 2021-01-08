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
    public class SteamService : ISteamService
    {
        private const string CachePath = "steamapps.json";
        private const string SteamUri = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";

        private HashSet<SteamApp> _cache = new HashSet<SteamApp>();
        private IMvxLog _log;
        
        public async Task Initialize(IMvxLog log)
        {
            _log = log;
            _log.Info("Updating cache...");
            var updateNeeded = DateTime.Now.Subtract(File.GetLastWriteTimeUtc(CachePath)).TotalDays >= 1;
            string cacheString;
            if (updateNeeded)
            {
                _log.Info("Getting content from API...");
                var client = new HttpClient();
                var httpCall = client.GetAsync(SteamUri);
                var response = await httpCall.ConfigureAwait(false);
                var readAsStringAsync = response.Content.ReadAsStringAsync();
                var responseBody = await readAsStringAsync.ConfigureAwait(false);
                _log.Info("Got content from API successfully. Writing to file...");

                await File.WriteAllTextAsync(CachePath, responseBody, Encoding.UTF8).ConfigureAwait(false);
                cacheString = responseBody;
                _log.Info("Cache written to file successfully.");
            }
            else
            {
                _log.Info("Cache already up to date!");
                cacheString = await File.ReadAllTextAsync(CachePath).ConfigureAwait(false);
            }

            var steamApps = JsonSerializer.Deserialize<SteamApps>(cacheString);
            _cache = new HashSet<SteamApp>(steamApps.AppList.Apps);
            _log.Info("Loaded cache into memory!");
        }

        public IEnumerable<SteamApp> GetListOfAppsByName(string name)
        {
            var listOfAppsByName = _cache.Search(x => x.Name)
                .SetCulture(StringComparison.OrdinalIgnoreCase)
                .ContainingAll(name.Split(' ')).ToHashSet();
            foreach (var steamApp in listOfAppsByName)
            {
                var sa = Task.Run(async () => await AppDetails.GetAsync(steamApp.AppId).ConfigureAwait(false)).Result;
                if (sa.Type != AppType.Game) listOfAppsByName.Remove(steamApp);
            }
            return listOfAppsByName;
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