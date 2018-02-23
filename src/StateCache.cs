using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Timers;

namespace DampBot
{
    public static class StateCache
    {
        internal static Dictionary<ulong, GuildData> Guilds = new Dictionary<ulong, GuildData>();
        internal static DateTime dtStart = DateTime.Now;
    }

    public class GuildData
    {
        //trivia
        internal bool m_bTrivia = false;
        internal string m_bAnswer = string.Empty;
        internal Dictionary<ulong, DateTime> m_TriviaAnswers = new Dictionary<ulong, DateTime>();

        //music
        internal Queue<string> queueSongs = new Queue<string>();
        internal bool bSongPlaying = false;
        internal IAudioClient audioClient;

        //Connect4
        internal string[,] gameboard = new string[7, 6];
        internal bool bGameOver = false;
        internal string connectPlayer1;
        internal string connectPlayer2;
        internal bool connect4inprogress = false;
        internal bool turnPlayer1 = true;

        //timeout
        internal ulong timeoutUser;
        internal int timeoutVotesYes = 0;
        internal int timeoutVotesNo = 0;
        internal int timeoutNumUsers = 0;
        internal Dictionary<IUser, bool> timeoutVoters = new Dictionary<IUser, bool>();
        internal Timer timerTimeout = new Timer(120000);
        internal IMessageChannel channelTimeout;
        internal IVoiceChannel afkChannel;

        public void ClearTimeout()
        {
            timeoutNumUsers = 0;
            timeoutUser = 0;
            timeoutVotesNo = 0;
            timeoutVotesYes = 0;
            timeoutVoters.Clear();
            timerTimeout.Stop();
        }
    }
}
