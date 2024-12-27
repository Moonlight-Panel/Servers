namespace MoonlightServers.DaemonShared.Enums;

public enum ServerTask
{
    None = 0,
    CreatingStorage = 1,
    PullingDockerImage = 2,
    RemovingContainer = 3,
    CreatingContainer = 4,
    StartingContainer = 5,
    StoppingContainer = 6
}