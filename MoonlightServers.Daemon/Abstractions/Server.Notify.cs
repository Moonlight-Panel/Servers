using MoonlightServers.Daemon.Enums;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task NotifyContainerDied() => await StateMachine.FireAsync(ServerTrigger.NotifyContainerDied);
}