using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealTraso.Services;
using RealTraso.Utility;
using Serilog;

namespace RealTraso;

internal static class Program
{
    static async Task<int> Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
#if !DEBUG
            .WriteTo.File(
            path: Path.Combine(AppContext.BaseDirectory, "Files", "SystemLogs", "RealTraso.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60)
#endif
            .CreateLogger();

        Log.Information("Program starting");

        try
        {
            using var host = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    x.AddConfiguration(configuration);
                })
                .ConfigureDiscordHost((context, config) =>
                {
                    config.SocketConfig = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Info,
                        MessageCacheSize = 100,
                        AlwaysDownloadUsers = true,
                        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.GuildPresences
                    };
                    config.Token = context.Configuration.GetSection("BotSettings").Get<BotOptions>()?.DiscordToken ?? throw new ArgumentException("Discord token is null");
                })
                .UseCommandService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = Discord.Commands.RunMode.Async;
                    config.CaseSensitiveCommands = true;
                })
                .UseInteractionService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = Discord.Interactions.RunMode.Async;
                    config.UseCompiledLambda = true;
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        // config
                        .Configure<BotOptions>(configuration.GetSection("BotSettings"))
                        .AddHttpClient()
                        // managers
                        .AddHostedService<MessageHandler>()
                        .AddHostedService<InteractionHandler>()
                        // singletons
                        .AddSingleton<ExceptionReporter>()
                        .AddSingleton<MessageUtilities>();
                })
                .UseSerilog()
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
            return 0;
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.Information("Program ending");
            Log.CloseAndFlush();
        }
    }
}