using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Discord;
using RealTraso.Utility;

namespace RealTraso.Services;

public class MessageUtilities
{
    private readonly DiscordSocketClient _client;

    public MessageUtilities(DiscordSocketClient client)
    {
        _client = client;
    }

    public enum ComponentType
    {
        Button, SelectMenu
    }
    public async Task<IComponentInteraction?> AwaitComponentAsync(ulong messageID, ulong? userId, ComponentType type, int delayInSeconds = 15)
    {
        SocketMessageComponent? response = null;

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        if (type == ComponentType.Button)
            _client.ButtonExecuted += OnComponentReceived;
        else if (type == ComponentType.SelectMenu)
            _client.SelectMenuExecuted += OnComponentReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            if (type == ComponentType.Button)
                _client.ButtonExecuted -= OnComponentReceived;
            else if (type == ComponentType.SelectMenu)
                _client.SelectMenuExecuted -= OnComponentReceived;
            canceler.Dispose();
        }

        return response;

        async Task OnComponentReceived(SocketMessageComponent component)
        {
            if (component.Message.Id == messageID)
            {
                if (userId is null || component.User.Id == userId)
                {
                    response = component;
                    await component.DeferAsync();
                    canceler.Cancel();
                }
                else
                {
                    await component.RespondAsync("No");
                }
            }
        }
    }
    public async Task<Dictionary<ulong, IComponentInteraction?>> AwaitComponentMultipleAsync(ulong messageID, Dictionary<ulong, IComponentInteraction?> users, ComponentType type, int delayInSeconds = 15)
    {
        int usersLeftToRespond = users.Count;

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        if (type == ComponentType.Button)
            _client.ButtonExecuted += OnComponentReceived;
        else if (type == ComponentType.SelectMenu)
            _client.SelectMenuExecuted += OnComponentReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            if (type == ComponentType.Button)
                _client.ButtonExecuted -= OnComponentReceived;
            else if (type == ComponentType.SelectMenu)
                _client.SelectMenuExecuted -= OnComponentReceived;
            canceler.Dispose();
        }

        return users;

        async Task OnComponentReceived(SocketMessageComponent component)
        {
            if (component.Message.Id == messageID)
            {
                if (users.ContainsKey(component.User.Id) && users[component.User.Id] == null)
                {
                    users[component.User.Id] = component;

                    await component.DeferAsync();
                    if (--usersLeftToRespond == 0)
                        canceler.Cancel();
                }
                else
                {
                    await component.RespondAsync("No");
                }
            }
        }
    }
    public async Task<IMessage?> AwaitMessageAsync(ulong channelID, ulong? userId, int delayInSeconds = 15)
    {
        SocketMessage? response = null;

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        _client.MessageReceived += OnMessageReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
            canceler.Dispose();
        }

        return response;

        async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == channelID && (userId is null || message.Author.Id == userId))
            {
                response = message;
                canceler.Cancel();
                await Task.CompletedTask;
            }
        }
    }
    public async Task<Dictionary<ulong, IMessage?>> AwaitMessageMultipleAsync(ulong channelID, Dictionary<ulong, IMessage?> users, int delayInSeconds)
    {
        int usersLeftToRespond = users.Count;

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        _client.MessageReceived += OnMessageReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
            canceler.Dispose();
        }

        return users;

        async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == channelID && users.TryAdd(message.Author.Id, message) && --usersLeftToRespond == 0)
            {
                canceler.Cancel();
                await Task.CompletedTask;
            }
        }
    }
    public async Task<bool?> ActivityConfirmationAsync(IInteractionContext context, IGuildUser challenger, IGuildUser target, string nameOfActivity, bool? botResponse = null)
    {
        var buttonBuilder = new ComponentBuilder()
            .WithButton("Yes", "Yes", ButtonStyle.Success)
            .WithButton("No", "No", ButtonStyle.Danger);

        string questionText = $"{target.Nickname ?? target.Username}, {challenger.Nickname ?? challenger.Username} has challenged you to {nameOfActivity}\n" +
            $"Do you accept?";

        IUserMessage question;
        if (context.Interaction.HasResponded)
        {
            question = await context.Channel.SendMessageAsync(questionText, components: buttonBuilder.Build());
        }
        else
        {
            await context.Interaction.RespondAsync(questionText, components: buttonBuilder.Build());
            question = await context.Interaction.GetOriginalResponseAsync();
        }

        // bot response
        if (botResponse is not null && target.Id == _client.CurrentUser.Id)
        {
            await Task.Delay(500);
            if (botResponse.Value)
            {
                return true;
            }
            else
            {
                await question.ModifyAsync(msg =>
                {
                    msg.Content += $"\n{target.Nickname ?? target.Username} declined";
                    msg.Components = Utilities.DisableAllButtons(buttonBuilder).Build();
                });
                return false;
            }
        }

        // normal user response
        var response = await AwaitComponentAsync(question.Id, target.Id, ComponentType.Button);

        // default values for null (no response), they get changed if there is a response
        string? responseStatusMessage = $"\n{target.Nickname ?? target.Username} didn't respond";
        bool? responseStatus = null;
        if (response is not null)
        {
            if (response.Data.CustomId == "Yes")
            {
                responseStatusMessage = null;
                responseStatus = true;
            }
            else
            {
                responseStatusMessage = $"\n{target.Nickname ?? target.Username} declined";
                responseStatus = false;
            }
        }

        await question.ModifyAsync(msg =>
        {
            if (responseStatusMessage is not null)
                msg.Content += responseStatusMessage;
            msg.Components = Utilities.DisableAllButtons(buttonBuilder).Build();
        });

        return responseStatus;
    }
    public async Task SendMessageAfterDelayAsync(IMessageChannel channel, string message, int delayInSeconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delayInSeconds * 1000, cancellationToken);
            await channel.SendMessageAsync(message);
        }
        catch (TaskCanceledException)
        { /* task cancelled */ }
    }
}