using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;

namespace DampBot
{
	class Program
	{
		static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

		public static DiscordSocketClient client;
		public static CommandService commands;
		private IServiceProvider services;

		private readonly string[] _TriviaAnswers = new string[8] { "a", "b", "c", "d", "A", "B", "C", "D" };

		public async Task MainAsync()
		{
			client = new DiscordSocketClient();
			commands = new CommandService();
			services = new ServiceCollection().BuildServiceProvider();
			await InstallCommands();

			//Read in token from res/token.txt
			using (StreamReader sr = new StreamReader(@"res\token.txt"))
				await client.LoginAsync(TokenType.Bot, sr.ReadToEnd());
			await client.StartAsync();
			await client.SetStatusAsync(UserStatus.Online);


			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		public async Task InstallCommands()
		{
			// Hook the MessageReceived Event into our Command Handler
			client.MessageReceived += HandleCommand;
			// Discover all of the commands in this assembly and load them.
			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services: null);
		}

		public async Task HandleCommand(SocketMessage messageParam)
		{
			// Don't process the command if it was a System Message
			if (!(messageParam is SocketUserMessage message)) return;
			// Create a number to track where the prefix ends and the command begins
			int argPos = 0;
			var context = new CommandContext(client, message);
			var guildId = (null == context.Guild ? context.Channel.Id : context.Guild.Id);
			if (message.Author.IsBot)
				return;
			// Determine if the message is a command, based on if it starts with 'd!' or a mention prefix
			if (message.HasStringPrefix("d!", ref argPos, StringComparison.OrdinalIgnoreCase) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
			{
				// Create a Command Context
				if (!StateCache.Guilds.ContainsKey(guildId))
					StateCache.Guilds[guildId] = new GuildData(guildId);
				// Execute the command. (result does not indicate a return value, 
				// rather an object stating if the command executed successfully)
				var result = await commands.ExecuteAsync(context, argPos, services);
				if (!result.IsSuccess)
				{
					if (string.Equals(result.ErrorReason, "Unknown command.", StringComparison.OrdinalIgnoreCase))
						return;
					await context.Channel.SendMessageAsync(result.ErrorReason);
				}
				return;
			}
			//Miscellaneous
			else if (message.Content.Equals("damp", StringComparison.OrdinalIgnoreCase))
				await message.Channel.SendMessageAsync("||tiny pp||");
			else if (message.Content.Equals("sinna", StringComparison.OrdinalIgnoreCase))
				await message.Channel.SendMessageAsync("sinna time! :sunglasses:");
			else if (message.Content.Equals("arty", StringComparison.OrdinalIgnoreCase))
				await message.Channel.SendMessageAsync("my tits hurt");

			//Trivia answers
			else if (StateCache.Guilds[guildId].m_bTrivia)
			{
				if (string.Equals(message.Content, StateCache.Guilds[guildId].m_bAnswer, StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						double elapsed = (DateTime.Now - StateCache.Guilds[guildId].m_TriviaAnswers[message.Author.Id]).TotalSeconds;
						if (elapsed > 5)
							throw new Exception();
						else return;
					}
					catch
					{
						await message.Channel.SendMessageAsync($"{message.Author.Username} got the answer first!");
						StateCache.Guilds[guildId].m_TriviaAnswers.Clear();
					}

					StateCache.Guilds[guildId].m_bAnswer = string.Empty;
					StateCache.Guilds[guildId].m_bTrivia = false;
				}
				else
				{
					if (_TriviaAnswers.Contains(message.Content))
						StateCache.Guilds[guildId].m_TriviaAnswers[message.Author.Id] = DateTime.Now;
				}
			}
		}
	}
}
