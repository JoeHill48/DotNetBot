using Discord.Audio;
using Discord.Commands;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DampBot
{
	public class MusicModule : ModuleBase
    {
        private readonly DirectoryInfo directory = new DirectoryInfo(@"C:\tmp");

		// ~say hello -> hello
		[Command("play", RunMode = RunMode.Async), Summary("Plays youtube music")]
		public async Task Play([Remainder, Summary("youtube url")] string strUrl)
		{
			await Context.Channel.SendMessageAsync("Music player is under construction. Check back later for updates!");
			return;
			//var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
			//if (StateCache.Guilds[guildId].bSongPlaying)
			//{
			//	if (!StateCache.Guilds[guildId].queueSongs.Contains(strUrl))
			//	{
			//		StateCache.Guilds[guildId].queueSongs.Enqueue(strUrl);
			//		await Context.Channel.SendMessageAsync("Song added to queue...");
			//		await HandleQueue(guildId);
			//	}
			//	else
			//		await Context.Channel.SendMessageAsync("Song already in queue");
			//	return;
			//}
			//// Get the audio channel
			//IVoiceChannel channel = (Context.Message.Author as IGuildUser).VoiceChannel;
			//if (channel == null)
			//{
			//	await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument.");
			//	return;
			//}

			//if (!directory.Exists)
			//	directory.Create();
			//StateCache.Guilds[guildId].bSongPlaying = true;
			//StateCache.Guilds[guildId].audioClient = await channel.ConnectAsync();
			//YouTube youtube = YouTube.Default;
			//Video vid = youtube.GetVideo(strUrl);
			//await Context.Channel.SendMessageAsync(string.Format("Playing {0}...", vid.Title));
			//File.WriteAllBytes(@"C:\tmp\" + vid.FullName.Replace(" ", ""), vid.GetBytes());
			//string strFilePath = @"C:\tmp\" + vid.FullName.Replace(" ", "") + ".mp3";
			//var inputFile = new MediaFile(@"C:\tmp\" + vid.FullName.Replace(" ", ""));
			//var outputFile = new MediaFile(strFilePath);

			//using (var engine = new Engine())
			//{
			//	engine.GetMetadata(inputFile);

			//	engine.Convert(inputFile, outputFile);
			//}
			//await SendAsync(StateCache.Guilds[guildId].audioClient, strFilePath, guildId);
		}     

        private Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            return Process.Start(ffmpeg);
        }

        private async Task SendAsync(IAudioClient client, string path, ulong guildId)
        {
            if (string.IsNullOrEmpty(path))
                return;
            // Create FFmpeg using the previous example
            var ffmpeg = CreateStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
            var files = directory.EnumerateFiles();
            foreach (FileInfo fi in files)
                fi.Delete();
            StateCache.Guilds[guildId].bSongPlaying = false;
            await HandleQueue(guildId);
        }

        private async Task HandleQueue(ulong guildId)
        {
            if (!StateCache.Guilds[guildId].bSongPlaying)
            {
                if (StateCache.Guilds[guildId].queueSongs.Count > 0)
                    await Play(StateCache.Guilds[guildId].queueSongs.Dequeue());
                if (StateCache.Guilds[guildId].queueSongs.Count == 0)
                    await StateCache.Guilds[guildId].audioClient.StopAsync();
                StateCache.Guilds[guildId].bSongPlaying = false;
            }
        }

        [Command("skip", RunMode = RunMode.Async), Summary("skips to next song in queue")]
        public async Task Skip()
        {
            var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
            if (StateCache.Guilds[guildId].queueSongs.Count <= 0)
            {
                await StateCache.Guilds[guildId].audioClient.StopAsync();
            }
            StateCache.Guilds[guildId].bSongPlaying = false;
            await HandleQueue(guildId);
        }
    }
}
