using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Implementations;

public class ConsoleSignalRComponent : IServerComponent
{
    private readonly IHubContext<ServerWebSocketHub> Hub;
    private readonly ServerContext Context;

    private IAsyncDisposable? StdOutSubscription;
    private string HubGroup;

    public ConsoleSignalRComponent(IHubContext<ServerWebSocketHub> hub, ServerContext context)
    {
        Hub = hub;
        Context = context;
    }

    public async Task InitializeAsync()
    {
        HubGroup = Context.Configuration.Id.ToString();
        
        StdOutSubscription = await Context.Server.Console.SubscribeStdOutAsync(OnStdOut);
    }

    private async ValueTask OnStdOut(string output)
    {
        await Hub.Clients.Group(HubGroup).SendAsync("ConsoleOutput", output);
    }

    public async ValueTask DisposeAsync()
    {
        if (StdOutSubscription != null)
            await StdOutSubscription.DisposeAsync();
    }
}