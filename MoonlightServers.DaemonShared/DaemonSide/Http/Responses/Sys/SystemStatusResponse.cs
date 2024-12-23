namespace MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Sys;

public class SystemStatusResponse
{
    public bool TripSuccess { get; set; }
    public TimeSpan TripTime { get; set; }
    public string? TripError { get; set; }
    public string Version { get; set; }
}