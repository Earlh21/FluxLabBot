using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FluxLab;
using Microsoft.Extensions.DependencyInjection;

#if DEBUG
string? botToken = GetEnvironmentVariable("FLUX_BOT_TOKEN_DEBUG");
#else
string? botToken = GetEnvironmentVariable("FLUX_BOT_TOKEN");
#endif

if (botToken == null)
{
    Console.WriteLine("FLUX_BOT_TOKEN/FLUX_BOT_TOKEN_DEBUG environment variable not set.");
    return;
}

var fluxToken = GetEnvironmentVariable("FLUX_API_KEY");

if (fluxToken == null)
{
    Console.WriteLine("FLUX_API_TOKEN environment variable not set.");
    return;
}

var client = new DiscordSocketClient();

client.Log += LogAsync;
client.Ready += ReadyAsync;

await client.LoginAsync(TokenType.Bot, botToken);
await client.StartAsync();
await Task.Delay(-1);

Task LogAsync(LogMessage log)
{
    Console.WriteLine(log.ToString());
    return Task.CompletedTask;
}

async Task ReadyAsync()
{
    Console.WriteLine("Bot is connected.");

    var serviceProvider = new ServiceCollection()
        .AddTransient<FluxClient>(_ => new FluxClient(fluxToken))
        .BuildServiceProvider();
    var interactionService = new InteractionService(client);
    await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

    await interactionService.RegisterCommandsGloballyAsync();
    await Task.Delay(1000);

    client.InteractionCreated += async interaction =>
    {
        var scope = serviceProvider.CreateScope();
        var ctx = new SocketInteractionContext(client, interaction);
        await interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
    };
}

string? GetEnvironmentVariable(string name)
{
    string? process = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    if (process != null) return process;

    string? user = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
    if (user != null) return user;

    string? machine = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
    return machine;
}