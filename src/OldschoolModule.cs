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
        private HttpClient client = new HttpClient();
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
				sb.AppendLine($"Attack: {stats[1]}, Hitpoints: {stats[4]}, Mining: {stats[15]}");
				sb.AppendLine($"Strength: {stats[3]}, Agility: {stats[17]}, Smithing: {stats[14]}");
				sb.AppendLine($"Defence: {stats[2]}, Herblore: {stats[16]}, Fishing: {stats[11]}");
				sb.AppendLine($"Ranged: {stats[5]}, Thieving: {stats[18]}, Cooking: {stats[8]}");
				sb.AppendLine($"Prayer: {stats[6]}, Crafting: {stats[13]}, Firemaking: {stats[12]}");
				sb.AppendLine($"Magic: {stats[7]}, Fletching: {stats[10]}, Woodcutting: {stats[9]}");
				sb.AppendLine($"Runecraft: {stats[21]}, Slayer: {stats[19]}, Farming: {stats[20]}");
				sb.AppendLine($"Construction: {stats[23]}, Hunter: {stats[22]}");
                await Context.Channel.SendMessageAsync(sb.ToString());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync($"Error looking up user in hiscores. Check your spelling, or the service may be unavailable.");
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
                await Context.Channel.SendMessageAsync($"Error looking item in GE. Item is either untradeable in the GE, spelled wrong, or the service is currently unavailable.");
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
