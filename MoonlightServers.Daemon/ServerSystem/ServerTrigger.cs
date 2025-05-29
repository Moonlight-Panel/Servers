namespace MoonlightServers.Daemon.ServerSystem;

public enum ServerTrigger
{
    Start = 0,
    Stop = 1,
    Kill = 2,
    Install = 3,
    Exited = 4,
    OnlineDetected = 5,
    FailSafe = 6
}