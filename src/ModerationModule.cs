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
                    if (string.Equals(u.Username, user, StringComparison.OrdinalIgnoreCase))
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
                if (string.Equals(vote, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    StateCache.Guilds[guildId].timeoutVotesYes++;
                    StateCache.Guilds[guildId].timeoutVoters[Context.User] = true;
                }
                else if (string.Equals(vote, "no", StringComparison.OrdinalIgnoreCase))
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

        [Command("purge", RunMode = RunMode.Async), Summary("Purges a number of messages from a user in a channel.")]
        public async Task Purge([Remainder] string user = "")
        {
			try
			{
				var guser = Context.User as IGuildUser;
				if (!guser.GuildPermissions.Has(GuildPermission.ManageMessages))
				{
					await Context.User.SendMessageAsync($"You do not have permissions to purge messages in {Context.Guild.Name}. Contact an admin if you wish to have messages removed!");
					return;
				}
				var channel = Context.Channel;
				IUser user2purge = null;
				//Get user
				if (!string.Equals(user, string.Empty, StringComparison.OrdinalIgnoreCase))
				{
					var users = channel.GetUsersAsync().FirstOrDefault().Result;
					user2purge = users.First(x => (string.Equals(x.Username, user, StringComparison.OrdinalIgnoreCase)));
					foreach (IUser u in users)
					{
						if (string.Equals(u.Username.ToLower(), user, StringComparison.OrdinalIgnoreCase))
						{
							user2purge = u;
							break;
						}
					}
				}

				//Get messages;
				var msgs = channel.GetMessagesAsync(1000).Flatten();
				var usermsgs = (null == user2purge) ? msgs : msgs.Where(x => x.Author == user2purge);
				usermsgs = usermsgs.Where(msg => !msg.IsPinned);
				var enumerator = usermsgs.GetEnumerator();
				while (await enumerator.MoveNext())
					await channel.DeleteMessageAsync(enumerator.Current);
			}
			catch (TimeoutException) { }
			catch (Exception ex)
			{
				await Context.Channel.SendMessageAsync($"Error purging channel. {ex.Message}");
			}
        }
	}
}
