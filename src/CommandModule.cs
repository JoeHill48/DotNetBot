using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DampBot
{
    public class CommandModule : ModuleBase
    {
		private readonly Random _random = new Random();
        [Command("hello?", RunMode = RunMode.Async), Summary("")]
        public async Task Hello()
        {
            await Context.Channel.SendMessageAsync($"Can anybody hear {Context.Message.Author.Username}?");
        }

        [Command("uptime", RunMode = RunMode.Async), Summary("How long the bot has been running.")]
        public async Task Uptime()
        {
            TimeSpan ts = (DateTime.Now - StateCache.dtStart);
            await Context.Channel.SendMessageAsync($"Damp bot has been running for {ts.Days} days {ts.Hours} hrs {ts.Minutes} mins");
        }

        [Command("setgame", RunMode = RunMode.Async), Summary("Sets the bot's status on Discord.")]
        public async Task SetGame([Remainder]string strGame)
        {
            await Program.client.SetGameAsync(strGame);
        }

        [Command("help", RunMode = RunMode.Async), Summary("Provides a list of all commands.")]
        public async Task Help()
        {
            var comms = Program.commands.Commands;
            StringBuilder sb = new StringBuilder();
            foreach (CommandInfo info in comms)
            {
                StringBuilder parameters = new StringBuilder();
                foreach (ParameterInfo parm in info.Parameters)
                {
                    parameters.Append($"{parm.Type.Name} {parm.Name}, ");
                }
                string parms = parameters.ToString().Trim(", ".ToCharArray());
                sb.AppendLine($"!{info.Name}: {info.Summary}");
                if (!string.IsNullOrEmpty(parms))
                {
                    sb.Append($"Parameters: {parms}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            var dmChannel = await Context.Message.Author.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(sb.ToString());
        }

		[Command("roll", RunMode = RunMode.Async), Summary("Rolls a die with specified number of sides (default 20)")]
		public async Task Roll([Remainder]string sides = "20")
		{
			int num = int.TryParse(sides, out num) ? num : 20;
			var res = _random.Next(1, num);
			var user = Context.User.Username;
			await Context.Channel.SendMessageAsync($"{user} rolled a {res}! (1-{num})");
		}
    }
}
