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
	public class TriviaModule : ModuleBase
	{
		private readonly string strApi = "https://opentdb.com/api.php?amount=1&token=";
		private bool bToken = false;
		private string strToken;

		private void GetToken()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12;
			WebClient client = new WebClient();
			var json = client.DownloadString("https://opentdb.com/api_token.php?command=request");
			JObject o = JObject.Parse(json);
			strToken = (string)o["token"];
			bToken = true;
		}

		private void ResetToken()
		{
			WebClient client = new WebClient();
			var json = client.DownloadString("https://opentdb.com/api_token.php?command=reset&token=" + strToken);
			JObject o = JObject.Parse(json);
			strToken = (string)o["token"];
			bToken = true;
		}

		[Command("trivia", RunMode = RunMode.Async), Summary("Starts a game of trivia.")]
		public async Task Trivia()
		{
			try
			{
				var guildId = (null == Context.Guild ? Context.Channel.Id : Context.Guild.Id);
				if (StateCache.Guilds[guildId].m_bTrivia)
				{
					throw new Exception("Trivia session already in progress!");
				}
				if (!bToken)
					GetToken();
				WebClient client = new WebClient();
				var json = client.DownloadString(strApi + strToken);
				JObject o = JObject.Parse(json);
				string strCode = (string)o["response_code"];
				switch (strCode)
				{
					case "3":
						{
							ResetToken();
							await Context.Channel.SendMessageAsync("Resetting session token. Please try again.");
							return;
						}
					case "4":
						{
							GetToken();
							await Context.Channel.SendMessageAsync("Resetting session token. Please try again.");
							return;
						}
					case "0":
						{
							//trivia
							dynamic res = JsonConvert.DeserializeObject<ExpandoObject>(json);
							dynamic dynResults = res.results;
							dynamic results = dynResults[0];
							string strCategory = results.category;
							string strDifficulty = results.difficulty;
							string strQuestion = WebUtility.HtmlDecode(results.question);
							string strAnswer = WebUtility.HtmlDecode(results.correct_answer);
							List<object> lstIncorrect = results.incorrect_answers;
							List<string> lstChoices = new List<string>();
							foreach (Object obj in lstIncorrect)
							{
								string str = obj as string;
								str = WebUtility.HtmlDecode(str);
								lstChoices.Add(str);
							}
							lstChoices.Add(strAnswer);
							lstChoices = Shuffle(lstChoices);
							await Context.Channel.SendMessageAsync($"Category: {strCategory}\r\nDifficulty: {strDifficulty}\r\n{strQuestion}\r\n");
							StringBuilder sb = new StringBuilder();
							for (int i = 0; i < lstChoices.Count; i++)
							{
								Char charLetter = (Char)(97 + i);
								string strAns = lstChoices[i];
								sb.AppendLine($"{charLetter}. {strAns}");
								if (string.Equals(strAns, strAnswer, StringComparison.OrdinalIgnoreCase))
									StateCache.Guilds[guildId].m_bAnswer = charLetter.ToString();
							}
							await Context.Channel.SendMessageAsync(sb.ToString());
							StateCache.Guilds[guildId].m_bTrivia = true;
							return;
						}
					default:
						throw new Exception("Trivia not working. Try again later.");
				}
			}
			catch (Exception ex)
			{
				await Context.Channel.SendMessageAsync(ex.Message);
			}
		}

        private List<T> Shuffle<T>(List<T> list)
        {
            try
			{
                Random rng = new Random();

                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
                return list;
            }
            catch
            {
                return list;
            }
        }
    }
}
