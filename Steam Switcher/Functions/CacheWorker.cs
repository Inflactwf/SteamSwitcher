using Newtonsoft.Json;
using Steam_Switcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Steam_Switcher.Functions
{
    public class CacheWorker
    {
        private const string WebAPIKey = "D6B71DA550D6CFDFDF25303E39426821";
        MainWindow mw = Application.Current.MainWindow as MainWindow;

        public async Task ReloadFullCache(AccountsModel[] accounts, bool forceRefreshImage = false)
        {
            await LoadPublicInformation(accounts, forceRefreshImage);
            await LoadBansInformation(accounts);
            foreach (AccountsModel acc in accounts)
            {
                acc.Level = await GetAccountLevel(acc.SteamID);
                acc.GamesCount = await GetOwnedGames(acc.SteamID);
            }
        }

        public async Task LoadBansInformation(AccountsModel[] accounts)
        {
            List<string> accountIds = new List<string>();
            if (accounts.Length > 0)
            {
                foreach (var item in accounts)
                {
                    accountIds.Add(item.SteamID);
                }
            }
            else
            {
                return;
            }

            var banresult = (await GetAccountsBansInformation(accountIds.ToArray())).ToUTF8();
            var DeserializedBansInformation = JsonConvert.DeserializeObject<GetPlayerBansModel>(banresult.ToString());
            foreach (var player in DeserializedBansInformation.players)
            {
                foreach (var listaccount in mw.AccountsCollection)
                {
                    if (player.SteamId == listaccount.SteamID)
                    {
                        listaccount.CommunityBanned = player.CommunityBanned;
                        listaccount.VACBanned = player.VACBanned;
                        listaccount.EconomyBan = player.EconomyBan;
                    }
                }
            }
        }

        public async Task LoadPublicInformation(AccountsModel[] accounts, bool forceRefreshImage = false)
        {
            List<string> accountIds = new List<string>();

            if (accounts.Length > 0)
            {
                foreach (var item in accounts)
                {
                    accountIds.Add(item.SteamID);
                }
            }
            else
            {
                return;
            }

            var generalresult = (await GetAccountsInformation(accountIds.ToArray())).ToUTF8();
            var DeserializedInformation = JsonConvert.DeserializeObject<GetPlayerSummariesModel>(generalresult.ToString());
            foreach (var player in DeserializedInformation.response.players)
            {
                foreach (var listaccount in mw.AccountsCollection)
                {
                    if (player.steamid == listaccount.SteamID)
                    {
                        listaccount.Nickname = player.personaname;
                        listaccount.PersonaState = player.personastate;
                        listaccount.PrivacyStatus = player.communityvisibilitystate;

                        string file = $"{mw.CacheFolder}{listaccount.SteamID}.jpg";
                        if (!File.Exists(file))
                        {
                            try
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.DownloadFile(new Uri(player.avatarfull), file);
                                }
                                listaccount.AvatarFilePath = file;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не удалось загрузить аватар.\n\nПричина: {ex.Message}");
                            }
                        }
                        else
                        {
                            if (forceRefreshImage)
                            {
                                try
                                {
                                    File.Delete(file);
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(new Uri(player.avatarfull), file);
                                    }
                                    listaccount.AvatarFilePath = file;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Не удалось загрузить аватар принудительно.\n\nПричина: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }

        public async Task<bool> DownloadAvatarDirectly(string steamid)
        {
            string[] acc = new string[] { steamid };

            try
            {
                var generalresult = (await GetAccountsInformation(acc)).ToUTF8();
                var DeserializedInformation = JsonConvert.DeserializeObject<GetPlayerSummariesModel>(generalresult.ToString());
                var url = DeserializedInformation.response.players[0].avatarfull;

                using (WebClient client = new WebClient())
                {
                    string file = $"{mw.CacheFolder}{steamid}.jpg";
                    await client.DownloadFileTaskAsync(new Uri(url), file);
                    return true;
                }
            }
            catch
            {
                return await DownloadAvatarDirectly(steamid);
            }
        }

        public void ClearCache(AccountsModel[] accounts)
        {
            foreach(var accs in accounts)
            {

            }
        }
        
        public async Task<string> GetAccountsBansInformation(string[] steamids)
        {
            string ids = "";
            foreach (string id in steamids)
            {
                ids += $"{id},";
            }

            if (ids.Last().ToString() == ",")
                ids = ids.Remove(ids.Length - 1);

            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.QueryString.Add("key", WebAPIKey);
                    wc.QueryString.Add("steamids", ids);
                    var result = await wc.DownloadStringTaskAsync($"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/");
                    return result;
                }
            }
            catch
            {
                return await GetAccountsBansInformation(steamids);
            }
        }

        public async Task<string> GetAccountsInformation(string[] steamids)
        {
            string ids = "";
            foreach (string id in steamids)
            {
                ids += $"{id},";
            }

            if (ids.Last().ToString() == ",")
                ids = ids.Remove(ids.Length - 1);

            try
            {
                using (WebClient wc = new WebClient())
                {
                    var result = await wc.DownloadStringTaskAsync($"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={WebAPIKey}&steamids={ids}");
                    return result;
                }
            }
            catch
            {
                return await GetAccountsInformation(steamids);
            }
        }

        public async Task<ushort> GetAccountLevel(string steamid)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.QueryString.Add("key", WebAPIKey);
                    wc.QueryString.Add("steamid", steamid);
                    var result = await wc.DownloadStringTaskAsync($"http://api.steampowered.com/IPlayerService/GetSteamLevel/v1/");
                    var DeserializedResult = JsonConvert.DeserializeObject<GetAccountLevelModel>(result);
                    return DeserializedResult.response.player_level;
                }
            }
            catch
            {
                return await GetAccountLevel(steamid);
            }
        }

        public async Task<ushort> GetOwnedGames(string steamid)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.QueryString.Add("key", WebAPIKey);
                    wc.QueryString.Add("steamid", steamid);
                    var result = await wc.DownloadStringTaskAsync($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v1/");
                    var DeserializedResult = JsonConvert.DeserializeObject<GetOwnedGamesModel>(result);
                    return DeserializedResult.response.game_count;
                }
            }
            catch
            {
                return await GetOwnedGames(steamid);
            }
        }
    }
}
