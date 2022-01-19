using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System.Threading.Tasks;

namespace Gengar.Modules
{
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        [RequireOwner]
        [Command("echo", RunMode = RunMode.Async)]
        public async Task Echo(string guildID, string channelID, [Remainder]string msg)
        {
            var guild = Context.Client.GetGuild(ulong.Parse(guildID));
            var channel = guild.GetTextChannel(ulong.Parse(channelID));
            await channel.SendMessageAsync(msg);
        }
    }
}
