namespace MoonlightServers.Daemon.ServerSystem;

public enum ServerState
{
    Offline = 0,
    Starting = 1,
    Online = 2,
    Stopping = 3,
    Installing = 4
}