namespace MoonlightServers.Daemon.Enums;

public enum ServerTrigger
{
    Start = 0,
    Stop = 1,
    Restart = 2,
    Kill = 3,
    Reinstall = 4,
    NotifyOnline = 5,
    NotifyRuntimeContainerDied = 6,
    NotifyInstallationContainerDied = 7,
    NotifyInternalError = 8
}