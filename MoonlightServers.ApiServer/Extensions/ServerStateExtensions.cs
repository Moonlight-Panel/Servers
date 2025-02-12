using MoonlightServers.DaemonShared.Enums;
using MoonlightServers.Shared.Enums;
using ServerState = MoonlightServers.Shared.Enums.ServerState;

namespace MoonlightServers.ApiServer.Extensions;

public static class ServerStateExtensions
{
    public static ServerState ToServerPowerState(this DaemonShared.Enums.ServerState state)
    {
        return state switch
        {
            DaemonShared.Enums.ServerState.Installing => ServerState.Installing,
            DaemonShared.Enums.ServerState.Stopping => ServerState.Stopping,
            DaemonShared.Enums.ServerState.Online => ServerState.Online,
            DaemonShared.Enums.ServerState.Starting => ServerState.Starting,
            DaemonShared.Enums.ServerState.Offline => ServerState.Offline,
            _ => ServerState.Offline
        };
    }
}