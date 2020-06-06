using Birthday_Bot.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Birthday_Bot.Handlers
{
	public class APIHandler : IAPIHandler
	{
		private readonly IConfiguration _config;

		public APIHandler(IConfiguration config)
		{
			_config = config;
		}

		public async Task CreateMessage(MessageModel message, long channelID)
		{
			var authUrl = _config["BaseUrl"] + $"/channels/{channelID}/messages";
			using (var httpClient = new HttpClient())
			{
				MessageModel _message = new MessageModel()
				{
					content = message.content,
					tts = message.tts,
				};

				var postBody = JsonConvert.SerializeObject(_message);
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _config["BotToken"]);
				var response = await httpClient.PostAsync(authUrl, new StringContent(postBody, Encoding.UTF8, "application/json"));
			}
		}

		public async Task<bool> IsInGuild(long guildId, long userId)
		{
			var authUrl = _config["BaseUrl"] + $"/guilds/{guildId}/members/{userId}";
			using (var httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _config["BotToken"]);
				var response = await httpClient.GetAsync(authUrl);

				if (response.IsSuccessStatusCode)
					return true;
				else
					return false;

			}
		}
	}
}
