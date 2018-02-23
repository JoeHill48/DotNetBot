using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DampBot
{
    public class ConnectFourModule : ModuleBase
    {

        [Command("endgame", RunMode = RunMode.Async), Summary("Ends a game of Connect 4")]
        public async Task EndGame()
        {
            try
            {
                var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
                ClearGameBoard(guildId);
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync($"Error ending game. {ex.Message}");
            }
        }

        [Command("connect4", RunMode = RunMode.Async), Summary("Plays a game of Connect 4")]
        public async Task Connect4([Remainder, Summary("opponent")] string opponent)
        {
            try
            {
                var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
                if (StateCache.Guilds[guildId].connect4inprogress)
                    throw new Exception("Game already in progress!");

                StateCache.Guilds[guildId].connectPlayer1 = Context.Message.Author.Username.ToLower();
                StateCache.Guilds[guildId].connectPlayer2 = opponent.ToLower();

                //Erase board
                for (int x = 0; x < 7; x++)
                {
                    for (int y = 0; y < 6; y++)
                        StateCache.Guilds[guildId].gameboard[x, y] = string.Empty;                     
                }
                StateCache.Guilds[guildId].connect4inprogress = true;
                await Context.Message.Channel.SendMessageAsync($"Connect 4 game between {StateCache.Guilds[guildId].connectPlayer1} and {StateCache.Guilds[guildId].connectPlayer2} started!");
                await Context.Message.Channel.SendMessageAsync($"{StateCache.Guilds[guildId].connectPlayer1}'s turn!");
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync($"Error starting a game of connect4. {ex.Message}");
            }
        }

        [Command("place", RunMode = RunMode.Async), Summary("Places a token in the Connect 4 gameboard")]
        public async Task Place([Remainder, Summary("Location")] string location)
        {
            try
            {
                var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
                if (!StateCache.Guilds[guildId].connect4inprogress)
                    throw new Exception("No game is in progress!");
                string token = string.Empty;
                if (StateCache.Guilds[guildId].turnPlayer1)
                {
                    token = "x";
                    if (0 != string.Compare(Context.Message.Author.Username, StateCache.Guilds[guildId].connectPlayer1, true))                   
                        throw new Exception("It is not your turn!");                   
                }
                else if (!StateCache.Guilds[guildId].turnPlayer1)
                {
                    token = "o";
                    if (0 != string.Compare(Context.Message.Author.Username, StateCache.Guilds[guildId].connectPlayer2, true))
                        throw new Exception("It is not your turn!");
                }
                if (!Int32.TryParse(location, out int column))
                    throw new Exception("Please specify a column 1-7");

                if ((column < 1) || column > 7)
                    throw new Exception("Please specify a column 1-7");
                column -= 1;
                for (int i = 5; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(StateCache.Guilds[guildId].gameboard[column, i]))
                    {
                        StateCache.Guilds[guildId].gameboard[column, i] = token;
                        break;
                    }
                }

                StateCache.Guilds[guildId].turnPlayer1 = !StateCache.Guilds[guildId].turnPlayer1;
                await CheckBoard(guildId);
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync($"Error placing token on gameboard. {ex.Message}");
            }
        }

        private async Task CheckBoard(ulong guildId)
        {
            try
            {
                await Context.Message.Channel.SendMessageAsync(PrintBoard(guildId));
                if (CheckTie(guildId))
                {
                    await Context.Message.Channel.SendMessageAsync($"It's a tie!");
                    ClearGameBoard(guildId);
                }
                string winner = string.Empty;
                List<string> stringCollection;
                for (int x = 0; x < 7; x++)
                {
                    for (int y = 5; y >= 0; y--)
                    {
                        try
                        {
                            //Check going right
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 1, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 2, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 3, y]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going left
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 1, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 2, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 3, y]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going up
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y - 1]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y - 2]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y - 3]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going down
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y + 1]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y + 2]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y + 3]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going diagonal up-right
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 1 , y -1]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 2, y - 2]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 3, y - 3]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going diagonal down-right
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 1, y + 1]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 2, y + 2]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x + 3, y + 3]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going diagonal up-left
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 1, y - 1]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 2, y - 2]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 3, y - 3]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }

                        try
                        {
                            //Check going diagonal down-left
                            stringCollection = new List<string>();
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x, y]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 1, y + 1]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 2, y + 2]);
                            stringCollection.Add(StateCache.Guilds[guildId].gameboard[x - 3, y + 3]);

                            if (CheckWinner(stringCollection, guildId, out winner))
                            {
                                await Context.Message.Channel.SendMessageAsync($"{winner} wins!");
                                ClearGameBoard(guildId);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync($"Error checking board status. {ex.Message}");
            }
        }

        private bool CheckWinner(List<string> stringCollection, ulong guildId, out string winner)
        {
            winner = string.Empty;
            try
            {
                if (stringCollection.All(s => s == stringCollection.First()))
                {
                    if (0 == string.Compare(stringCollection.First(), "x"))
                        winner = StateCache.Guilds[guildId].connectPlayer1;
                    else if (0 == string.Compare(stringCollection.First(), "o"))
                        winner = StateCache.Guilds[guildId].connectPlayer2;
                    else
                        winner = string.Empty;
                    if (string.IsNullOrEmpty(winner))
                        return false;
                    else
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string PrintBoard(ulong guildId)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int rowLength = StateCache.Guilds[guildId].gameboard.GetLength(0);
                int colLength = StateCache.Guilds[guildId].gameboard.GetLength(1);

                for (int y = 0; y < colLength; y++)
                {
                    for (int x = 0; x < rowLength; x++)
                    {
                        if (string.IsNullOrEmpty(StateCache.Guilds[guildId].gameboard[x, y]))
                            sb.Append("|  ");
                        else
                            sb.Append($"|{StateCache.Guilds[guildId].gameboard[x, y]}");
                    }
                    sb.Append("|");
                    sb.AppendLine();
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool CheckTie(ulong guildId)
        {
            foreach (string s in StateCache.Guilds[guildId].gameboard)
            {
                if (string.IsNullOrEmpty(s))
                    return false;
            }
            return true;
        }

        private void ClearGameBoard(ulong guildId)
        {
            StateCache.Guilds[guildId].turnPlayer1 = true;
            StateCache.Guilds[guildId].gameboard = new string[7, 6];
            StateCache.Guilds[guildId].connect4inprogress = false;
            StateCache.Guilds[guildId].connectPlayer1 = string.Empty;
            StateCache.Guilds[guildId].connectPlayer2 = string.Empty;
        }
    }
}
