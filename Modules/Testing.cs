using Discord.Commands;

namespace RealTraso.Modules;

public class Testing : ModuleBase
{
    [Command("test")]
    public async Task Test()
    {
        await ReplyAsync("Message Received");
    }
}
