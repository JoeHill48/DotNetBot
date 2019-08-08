using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Text;

namespace DampBot
{
	public class FactModule : ModuleBase
	{
		private WebClient _client = new WebClient();

		[Command("fact", RunMode = RunMode.Async), Summary("Gets a random fact")]
		public async Task Fact()
		{
			try
			{
				var json = _client.DownloadString(@"https://catfact.ninja/fact");

				//Fact
				dynamic res = JsonConvert.DeserializeObject<ExpandoObject>(json);
				dynamic fact = res.fact;
				await Context.Channel.SendMessageAsync(fact);
			}
			catch (Exception ex)
			{
				await Context.Channel.SendMessageAsync(ex.Message);
			}
		}
	}
}
