using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Helpers;

public class ServerConsoleMonitor
{
    private readonly Server Server;
    private readonly IHubClients Clients;

    public ServerConsoleMonitor(Server server, IHubClients clients)
    {
        Server = server;
        Clients = clients;
    }

    public void Initialize()
    {
        Server.StateMachine.OnTransitioned += OnPowerStateChanged;
        Server.OnTaskAdded += OnTaskNotify;
    }

    public void Destroy()
    {
        Server.StateMachine.OnTransitioned -= OnPowerStateChanged;
    }
    
    private async Task OnTaskNotify(string task)
    {
        await Clients.Group($"server-{Server.Configuration.Id}").SendAsync(
            "TaskNotify",
            task
        );
    }

    private async Task OnPowerStateChanged(ServerState serverState)
    {
        await Clients.Group($"server-{Server.Configuration.Id}").SendAsync(
            "PowerStateChanged",
            serverState.ToString()
        );
    }
}