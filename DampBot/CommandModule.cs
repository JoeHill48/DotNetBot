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

        [Command("timeout", RunMode = RunMode.Async), Summary("Starts a vote to timeout a user.")]
        public async Task Timeout([Remainder, Summary("Username")] string user)
        {
            try
            {
                var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
                if (StateCache.Guilds[guildId].timeoutUser != 0)
                    throw new Exception($"Timeout vote already in progress!");
                //get all users in voice
                IVoiceChannel channel = (Context.Message.Author as IGuildUser).VoiceChannel;
                if (null == channel)
                    throw new Exception($"{user} is not in a voice channel.");
                var users = channel.GetUsersAsync();
                var collection = await users.FirstOrDefault();
                foreach (IUser u in collection)
                {
                    if (0 == string.Compare(u.Username, user, true))
                    {
                        StateCache.Guilds[guildId].timeoutUser = u.Id;
                        StateCache.Guilds[guildId].timeoutNumUsers = collection.Count();
                        StateCache.Guilds[guildId].channelTimeout = Context.Channel;
                        StateCache.Guilds[guildId].afkChannel = await Context.Guild.GetAFKChannelAsync();

                        await Context.Channel.SendMessageAsync($"Timeout vote for user {u.Username} started for 2 minutes. Type !vote yes/no to vote.", true);
                        StateCache.Guilds[guildId].timerTimeout.Start();
                    }
                    StateCache.Guilds[guildId].timeoutVoters[u] = false;
                }
                if (StateCache.Guilds[guildId].timeoutUser == 0)
                    throw new Exception($"User {user} not found.");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync($"Error starting a timeout vote. {ex.Message}");
            }
        }

        [Command("vote", RunMode = RunMode.Async), Summary("Votes for a timeout of a user.")]
        public async Task Vote([Remainder, Summary("Vote")] string vote)
        {
            try
            {
                var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
                if (StateCache.Guilds[guildId].timeoutUser == 0)
                    throw new Exception("No timeout vote is currently active.");
                if (StateCache.Guilds[guildId].timeoutVoters[Context.User])
                    throw new Exception("Users may only vote once.");
                if (0 == string.Compare(vote, "yes", true))
                {
                    StateCache.Guilds[guildId].timeoutVotesYes++;
                    StateCache.Guilds[guildId].timeoutVoters[Context.User] = true;
                }
                else if (0 == string.Compare(vote, "no", true))
                {
                    StateCache.Guilds[guildId].timeoutVotesNo++;
                    StateCache.Guilds[guildId].timeoutVoters[Context.User] = true;
                }
                else
                    throw new Exception("Vote must be yes or no.");
                await CheckTimeoutVote(guildId);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync($"Could not vote for timeout. {ex.Message}");
            }
        }

        internal static async Task CheckTimeoutVote(ulong guildId)
        {
            try
            {
                double nYesThreshold = Math.Ceiling((double)StateCache.Guilds[guildId].timeoutNumUsers * 2 / 3);
                double nNoThreshold = StateCache.Guilds[guildId].timeoutNumUsers - nYesThreshold + 1;
                if (StateCache.Guilds[guildId].timeoutVotesYes >= nYesThreshold)
                {
                    await StateCache.Guilds[guildId].channelTimeout.SendMessageAsync("Vote passed. User being moved to timeout...");
                    var user = await StateCache.Guilds[guildId].channelTimeout.GetUserAsync((ulong)StateCache.Guilds[guildId].timeoutUser);
                    IGuildUser gUser = user as IGuildUser;
                    await gUser.ModifyAsync(x => x.Channel = new Optional<IVoiceChannel>(StateCache.Guilds[guildId].afkChannel));
                    StateCache.Guilds[guildId].ClearTimeout();
                }
                else if (StateCache.Guilds[guildId].timeoutVotesNo >= nNoThreshold)
                {
                    await StateCache.Guilds[guildId].channelTimeout.SendMessageAsync("Vote failed. User is safe....for now.");
                    StateCache.Guilds[guildId].ClearTimeout();
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
