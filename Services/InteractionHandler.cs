using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using RealTraso.Utility;
using System.Reflection;

namespace RealTraso.Services;

public class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _provider;
    private readonly InteractionService _interactionService;
    private readonly ExceptionReporter _exceptionReporter;

    public InteractionHandler(DiscordSocketClient client, ILogger<InteractionHandler> logger, IServiceProvider provider,
        InteractionService interactionService, ExceptionReporter exceptionReporter)
        : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
        _exceptionReporter = exceptionReporter;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

        _interactionService.InteractionExecuted += OnInteractionExecuted;
        Client.InteractionCreated += OnInteractionCreated;
    }

    private Task OnInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
    {
        Task.Run(async () =>
        {
            if (result.IsSuccess || command is null)
                return;

            if (result.Error == InteractionCommandError.Exception)
            {
                if (result is ExecuteResult executeResult)
                {
                    bool pendingDefer = !context.Interaction.HasResponded;
                    bool isSlashCommand = context.Interaction.Type == InteractionType.ApplicationCommand;
                    if (pendingDefer)
                    {
                        string errorMessage = $"There was an internal error, please check the logs, pinging <@{GlobalIds.TrasoId}>";
                        if (executeResult.Exception is OverflowException)
                            errorMessage = executeResult.Exception.Message + $", pinging <@{GlobalIds.TrasoId}>";
                        await context.Interaction.RespondAsync(errorMessage, ephemeral: !isSlashCommand);
                    }

                    var exceptionContext = new ExceptionContext(context.Channel);
                    await _exceptionReporter.NotifyExceptionAsync(executeResult.Exception, exceptionContext, "Exception while executing an interaction", !pendingDefer && isSlashCommand);
                }
            }
            else
            {
                if (context.Interaction.HasResponded)
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                else
                    await context.Interaction.RespondAsync(result.ErrorReason);
            }
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(context.Channel);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing interaction executed event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    private Task OnInteractionCreated(SocketInteraction interaction)
    {
        Task.Run(async () =>
        {
            SocketInteractionContext context = new SocketInteractionContext(Client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _provider);
        }).ContinueWith(async t =>
        {
            var exceptionContext = new ExceptionContext(interaction.Channel);
            await _exceptionReporter.NotifyExceptionAsync(t.Exception!.InnerException!, exceptionContext, "Exception while executing interaction created event", false);
        }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }
}