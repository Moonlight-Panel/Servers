using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.Handlers;

public class OnlineDetectionHandler : IServerStateHandler
{
    private readonly ServerContext Context;
    private IOnlineDetector OnlineDetector => Context.Server.OnlineDetector;
    private ILogger Logger => Context.Logger;

    private IAsyncDisposable? ConsoleSubscription;
    private bool IsActive = false;

    public OnlineDetectionHandler(ServerContext context)
    {
        Context = context;
    }

    public async Task ExecuteAsync(StateMachine<ServerState, ServerTrigger>.Transition transition)
    {
        if (
            transition is
            { Source: ServerState.Offline, Destination: ServerState.Starting, Trigger: ServerTrigger.Start } && !IsActive
        )
        {
            await StartAsync();
        }
        else if (transition is { Source: not ServerState.Installing, Destination: ServerState.Offline } && IsActive)
        {
            await StopAsync();
        }
    }

    private async Task StartAsync()
    {
        IsActive = true;

        await OnlineDetector.CreateAsync();

        ConsoleSubscription = await Context.Server.Console.SubscribeStdOutAsync(OnHandleOutput);
        
        Logger.LogTrace("Created online detector. Created console subscription");
    }

    private async ValueTask OnHandleOutput(string line)
    {
        if(!IsActive)
            return;
        
        if(!await OnlineDetector.HandleOutputAsync(line))
            return;
        
        if(!Context.Server.StateMachine.CanFire(ServerTrigger.DetectOnline))
            return;

        Logger.LogTrace("Detected server as online. Destroying online detector");
        
        await Context.Server.StateMachine.FireAsync(ServerTrigger.DetectOnline);
        await StopAsync();
    }

    private async Task StopAsync()
    {
        IsActive = false;

        if (ConsoleSubscription != null)
        {
            await ConsoleSubscription.DisposeAsync();
            ConsoleSubscription = null;
        }

        await OnlineDetector.DestroyAsync();
        
        Logger.LogTrace("Destroyed online detector. Revoked console subscription");
    }

    public async ValueTask DisposeAsync()
    {
        if (ConsoleSubscription != null)
            await ConsoleSubscription.DisposeAsync();
    }
}