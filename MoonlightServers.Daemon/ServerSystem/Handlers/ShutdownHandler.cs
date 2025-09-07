using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.Handlers;

public class ShutdownHandler : IServerStateHandler
{
    private readonly ServerContext ServerContext;

    public ShutdownHandler(ServerContext serverContext)
    {
        ServerContext = serverContext;
    }

    public async Task ExecuteAsync(StateMachine<ServerState, ServerTrigger>.Transition transition)
    {
        // Filter (we only want to handle exists from the runtime, so we filter out the installing state)
        if (transition is not
            {
                Destination: ServerState.Offline,
                Source: not ServerState.Installing,
                Trigger: ServerTrigger.Exited
            })
            return;

        // Plan:
        // 1. Handle possible crash
        // 2. Remove runtime

        // 1. Handle possible crash
        // TODO: Handle crash here

        // 2. Remove runtime

        await ServerContext.Server.Runtime.DestroyAsync();
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}