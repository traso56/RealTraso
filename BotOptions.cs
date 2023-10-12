namespace RealTraso;

internal class BotOptions
{
    public const string BotSettings = "BotSettings";
    // discord
    public required string Prefix { get; set; }
    public required string DiscordToken { get; set; }
}
