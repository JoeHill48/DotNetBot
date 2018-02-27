using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DampBot
{
    public class ModerationModule : ModuleBase
    {
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

                        await Context.Channel.SendMessageAsync($"Timeout vote for user {u.Username} started for 2 minutes. Type !vote yes/no to vote.");
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

        internal async Task CheckTimeoutVote(ulong guildId)
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

        [Command("purge", RunMode = RunMode.Async), Summary("Purges a number of messages from a user in a channel.")]
        public async Task Purge([Remainder] string purgecommands)
        {
            try
            {
                var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
                var channel = Context.Channel;
                string user, count, purgeChannel;
                purgeChannel = count = user = string.Empty;
                //Parse arg string
                var commands = purgecommands.Split(' ');
                if (commands.Count() < 1 || commands.Count() > 3)
                {
                    await channel.SendMessageAsync("Usage: !purge user number [channel]");
                    return;
                }
                if (commands.Count() >= 2)
                {
                    user = commands[0].ToLower();
                    count = commands[1].ToLower();
                }
                if (commands.Count() == 3)
                {
                    purgeChannel = commands[2].ToLower();
                }

                //Get channel
                IMessageChannel channel2purge = channel;
                if (!string.IsNullOrEmpty(purgeChannel))
                {
                    var textchannels = Context.Guild.GetTextChannelsAsync().Result;
                    channel2purge = textchannels.First(x => x.Name.ToLower().Contains(purgeChannel));
                }

                //Get user
                IUser user2purge = null;
                var users = channel2purge.GetUsersAsync().FirstOrDefault().Result;
                user2purge = users.First(x => (0 == string.Compare(x.Username, user, true)));
                foreach (IUser u in users)
                {
                    if (0 == string.Compare(u.Username.ToLower(), user))
                    {
                        user2purge = u;
                        break;
                    }
                }

                //Get count
                int number2purge = Int32.Parse(count);
                //Get messages;
                var msgs = channel2purge.GetMessagesAsync(1000).Flatten().Result;
                var usermsgs = msgs.Where(x => x.Author == user2purge);
                var msgs2purge = usermsgs.Take(number2purge);
                await channel2purge.DeleteMessagesAsync(msgs2purge);
            }
            catch { }
        }
    }
}
