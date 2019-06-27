using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Xml;
using System.IO;

namespace DampBot
{
    public class OldschoolModule : ModuleBase
    {
        private readonly HttpClient client = new HttpClient();
        private bool bItemsCached = false;
        private Dictionary<string, RunescapeItem> dictItems = new Dictionary<string, RunescapeItem>();

        [Command("hs", RunMode = RunMode.Async), Summary("Looks up the player in the OSRS hiscores")]
        public async Task HiScore([Remainder, Summary("Player name")] string player)
        {
            try
            {
                string responseString = await client.GetStringAsync($"http://services.runescape.com/m=hiscore_oldschool/index_lite.ws?player={player}");

                string[] res = responseString.Split('\n');
                List<string> stats = new List<string>();
                for (int i = 0; i < 24; i++)
                {
                    string stat = res[i].Split(',')[1].Trim();
                    stats.Add(stat);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Player: {player}");
                sb.AppendLine($"Total Level: {stats[0]}, Attack: {stats[1]}, Defence: {stats[2]}, Strength: {stats[3]}, Hitpoints: {stats[4]}, Ranged: {stats[5]}, Prayer: {stats[6]}, Magic: {stats[7]}, Cooking: {stats[8]}, Woodcutting: {stats[9]}, Fletching: {stats[10]}, Fishing: {stats[11]}, Firemaking: {stats[12]}, Crafting: {stats[13]}, Smithing: {stats[14]}, Mining: {stats[15]}, Herblore: {stats[16]}, Agility: {stats[17]}, Thieving: {stats[18]}, Slayer: {stats[19]}, Farming: {stats[20]}, Runecraft: {stats[21]}, Hunter: {stats[22]}, Construction: {stats[23]}");
                await Context.Channel.SendMessageAsync(sb.ToString());
            }
            catch
            {
                await Context.Channel.SendMessageAsync($"Error looking up user in hiscores. Check spelling.");
            }
        }

        [Command("ge", RunMode = RunMode.Async), Summary("Gets the current price of an item on the GE")]
        public async Task Price([Remainder, Summary("Item name")] string item)
        {
            try
            {
                item = item.ToLower();
                if (!bItemsCached)
                {
                    using (StreamReader sr = new StreamReader(@"res\osrsitems.txt"))
                    {
                        string itemsJson = await sr.ReadToEndAsync();
                        string format = itemsJson.Trim(new char[] { '{', '}' });
                        List<string> items = format.Split(new string[] { "}," }, StringSplitOptions.None).ToList<string>();
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i] = items[i].Trim().Replace("\"", String.Empty).Replace("{", String.Empty);
                            string[] itemArray = items[i].Split('\n');
                            string id = itemArray[0].Trim('\"').Split(':').First();
                            string name = itemArray[1].Split(':').Last().Replace(",", String.Empty).Trim();
                            bool trade = Boolean.Parse(itemArray[3].Split(':').Last().Replace(",", String.Empty).Trim());

                            RunescapeItem rsItem = new RunescapeItem(id, name, trade);
                            try
                            {
                                if (trade)
                                    dictItems.Add(rsItem.Name.ToLower(), rsItem);
                            }
                            catch
                            {

                            }
                        }
                    }
                    bItemsCached = true;
                }

				RunescapeItem OsrsItem = dictItems[item];
				if (!OsrsItem.Trade)
				{
					await Context.Channel.SendMessageAsync("Item is not tradeable!");
					return;
				}
				string itemId = OsrsItem.Id;
                string responseString = await client.GetStringAsync($"http://services.runescape.com/m=itemdb_oldschool/api/catalogue/detail.json?item={itemId}");
                XmlDocument docItem = JsonConvert.DeserializeXmlNode(responseString);
                XmlNode ndItem = docItem.DocumentElement;
                string price = ndItem.SelectSingleNode("current/price").InnerText;
				string icon = ndItem.SelectSingleNode("icon_large").InnerText;
				string trend = ndItem.SelectSingleNode("day30/change").InnerText;
				var embed = new Discord.EmbedBuilder();
				embed.WithImageUrl(icon);
                await Context.Channel.SendMessageAsync($"{item}: {price} gp", false, embed.Build());
				await Context.Channel.SendMessageAsync($"Trend: {trend}");
            }
            catch
            {
                await Context.Channel.SendMessageAsync($"Error looking item in GE. Item is either untradeable in the GE or spelled wrong.");
            }
        }
    }
    public class RunescapeItem
    {
        public string Id;
        public string Name;
        public bool Trade;

        public RunescapeItem(string id, string name, bool trade)
        {
            Id = id;
            Name = name;
            Trade = trade;
        }
    }
}
