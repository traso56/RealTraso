using Discord.Interactions;

namespace RealTraso.Interactive;

[RequireContext(ContextType.Guild)]
public class Testing : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("test", "test")]
    public async Task Test()
    {
        await RespondAsync("response");
    }
}
