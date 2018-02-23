using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DampBot
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        public static CommandService commands;
        private IServiceProvider services;
        public async Task MainAsync()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection().BuildServiceProvider();
            await InstallCommands();

            string token = "MzMwNDAzMjM3NzEyNzU2NzM2.DTfXqg.KBgnLqQwH8iZYL9zOANvlyCuTc8";
            await client.LoginAsync(TokenType.Bot, token);
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
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        //public async Task JoinedGuild(SocketGuild guild)
        //{
        //    client.
        //    ShardedCommandContext context = new ShardedCommandContext(client, )
        //}

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            var context = new CommandContext(client, message);
            var guildId = (null == context.Guild ? context.Channel.Id : context.Guild.Id);
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (message.HasCharPrefix('!', ref argPos))
            {
                // Create a Command Context
                if (!StateCache.Guilds.ContainsKey(guildId))
                    StateCache.Guilds[guildId] = new GuildData();
                    // Execute the command. (result does not indicate a return value, 
                    // rather an object stating if the command executed successfully)
                    var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess)
                {
                    if (0 == string.Compare(result.ErrorReason, "Unknown command."))
                        return;
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
                return;
            }
            try
            {
                if (StateCache.Guilds[guildId].m_bTrivia)
                {
                    if (0 == string.Compare(message.Author.Username, "dampbot", true))
                        return;
                    if (0 == string.Compare(message.Content, StateCache.Guilds[guildId].m_bAnswer, true))
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
                        StateCache.Guilds[guildId].m_TriviaAnswers[message.Author.Id] = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return;
        }

        //private async Task CalculateScore(string strId)
        //{
        //    using (StreamReader sr = new StreamReader(@"C:\tmp\trivia.txt"))
        //    {
        //        using (StreamWriter sw = new StreamWriter(@"C:\tmp\trivia.txt"))
        //        {
        //            string line = await sr.ReadLineAsync();
        //            if ()
        //        }
        //    }
        //}
    }
}
