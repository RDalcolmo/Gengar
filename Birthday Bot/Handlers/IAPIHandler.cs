using System.Threading.Tasks;
using Birthday_Bot.Models;

namespace Birthday_Bot.Handlers
{
	public interface IAPIHandler
	{
		Task CreateMessage(MessageModel message, long channelID);
		Task<bool> IsInGuild(long guildId, long userId);
	}
}