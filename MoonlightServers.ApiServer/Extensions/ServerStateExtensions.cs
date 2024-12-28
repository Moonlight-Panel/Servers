using MoonlightServers.DaemonShared.Enums;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Extensions;

public static class ServerStateExtensions
{
    public static ServerPowerState ToServerPowerState(this ServerState state)
    {
        return state switch
        {
            ServerState.Installing => ServerPowerState.Installing,
            ServerState.Stopping => ServerPowerState.Stopping,
            ServerState.Online => ServerPowerState.Online,
            ServerState.Starting => ServerPowerState.Starting,
            ServerState.Offline => ServerPowerState.Offline,
            _ => ServerPowerState.Offline
        };
    }
}