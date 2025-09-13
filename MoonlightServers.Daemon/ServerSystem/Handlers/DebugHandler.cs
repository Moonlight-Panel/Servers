using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.Handlers;

public class DebugHandler : IServerStateHandler
{
    private readonly ServerContext Context;
    private IAsyncDisposable? StdOutSubscription;

    public DebugHandler(ServerContext context)
    {
        Context = context;
    }

    public async Task ExecuteAsync(StateMachine<ServerState, ServerTrigger>.Transition transition)
    {
        if(StdOutSubscription != null)
            return;

        StdOutSubscription = await Context.Server.Console.SubscribeStdOutAsync(line =>
        {
            Console.WriteLine($"STD OUT: {line}");
            return ValueTask.CompletedTask;
        });
    }
    
    public async ValueTask DisposeAsync()
    {
        if (StdOutSubscription != null)
            await StdOutSubscription.DisposeAsync();
    }
}