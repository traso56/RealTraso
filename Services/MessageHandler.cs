using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealTraso.Utility;
using System.Reflection;

namespace RealTraso.Services;

internal class MessageHandler : DiscordClientService
{
    private readonly IServiceProvider _provider;
    private readonly CommandService _commandService;
    private readonly BotOptions _options;
    private readonly ExceptionReporter _exceptionReporter;

    public MessageHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider,
        CommandService commandService, IOptions<BotOptions> options, ExceptionReporter exceptionReporter)
        : base(client, logger)
    {
        _provider = provider;
        _commandService = commandService;
        _options = options.Value;
        _exceptionReporter = exceptionReporter;

        commandService.CommandExecuted += OnCommandExecuted;

        Client.MessageReceived += OnMessageReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

    private Task OnCommandExecuted(Optional<CommandInfo> info, ICommandContext context, IResult result)
    {
        Task.Run(async () =>
        {
            if (result.IsSuccess)
                return;
            if (result.Error == CommandError.Exception)
            {
                if (result is ExecuteResult executeResult)
                {
                    var exceptionContext = new ExceptionContext(context.Message);
                    await _exceptionReporter.NotifyExceptionAsync(executeResult.Exception, exceptionContext, "Exception while executing a text command", true);
                }
            }
            else
            {
                if (result.ErrorReason == "Unknown command.")
                    return;
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(context.Message);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing command executed event", true);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
    private Task OnMessageReceived(SocketMessage arg)
    {
        Task.Run(async () =>
        {
            if (arg is not SocketUserMessage message) return;
            if (message.Source != MessageSource.User) return;

            var context = new SocketCommandContext(Client, message);
            if (context.Channel.GetChannelType() != ChannelType.DM)
            {
#if DEBUG
                if (context.Channel.Id != GlobalIds.IpeeServerBotChannel) // ignore all channels except the designated during debug
                    return;
#endif
                int argPos = 0;
                // command handling
                if (message.HasStringPrefix(_options.Prefix, ref argPos))
                {
                    await _commandService.ExecuteAsync(context, argPos, _provider);
                }
                // bot pinged
                else if (message.Content.Contains(Client.CurrentUser.Mention))
                {
                    await context.Channel.SendMessageAsync("Why do you ping me? I'm a dumb bot");
                }
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(arg);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing message received event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
}
