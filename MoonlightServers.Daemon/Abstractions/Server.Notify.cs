using MoonlightServers.Daemon.Enums;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task NotifyRuntimeContainerDied() => await StateMachine.FireAsync(ServerTrigger.NotifyRuntimeContainerDied);
    public async Task NotifyInstallationContainerDied() => await StateMachine.FireAsync(ServerTrigger.NotifyInstallationContainerDied);
}