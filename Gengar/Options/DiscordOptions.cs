namespace Gengar.Options;
public class DiscordOptions
{
    public string BotToken { get; set; } = null!;
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}
