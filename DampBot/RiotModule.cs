using Discord.Commands;
using System;
using System.Threading.Tasks;
using RiotApi.Net.RestClient;
using RiotApi.Net.RestClient.Configuration;

namespace DampBot
{
    public class RiotModule : ModuleBase
    {
        public RiotClient client = new RiotClient("RGAPI-85bc6203-e08c-406a-9746-e01ca1cdc8b7");

        [Command("rank", RunMode = RunMode.Async), Summary("Gets the list of free champs")]
        public async Task Rank([Remainder, Summary("")] string summonerName)
        {
            try
            {
                var summoner = client.Summoner.GetSummonersByName(RiotApiConfig.Regions.NA, summonerName);
                var stats = client.Stats.GetRankedStatsBySummonerId(RiotApiConfig.Regions.NA, summoner["IAmDamp"].Id);
                await Context.Channel.SendMessageAsync(stats.ToString());
                return;
            }
            catch (Exception ex)
            { }
        }
    }
}
