using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System.Threading.Tasks;

namespace Gengar.Modules
{
    public class GeneralCommands : ModuleBase
    {
        [RequireOwner]
        [Command("echo", RunMode = RunMode.Async)]
        public async Task Echo(ulong guildID, ulong channelID, [Remainder]string msg)
        {
            var guild = await Context.Client.GetGuildAsync(guildID).ConfigureAwait(false);
            var channel = await guild.GetTextChannelAsync(channelID).ConfigureAwait(false);
            await channel.SendMessageAsync(msg);
        }
    }
}
