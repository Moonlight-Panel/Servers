namespace MoonlightServers.Shared.Http.Responses.Admin.Nodes.Sys;

public class NodeSystemStatusResponse
{
    public bool RoundtripSuccess { get; set; }
    public bool RoundtripRemoteFailure { get; set; }
    public TimeSpan RoundtripTime { get; set; }
    public string? RoundtripError { get; set; }
    public string Version { get; set; }
}