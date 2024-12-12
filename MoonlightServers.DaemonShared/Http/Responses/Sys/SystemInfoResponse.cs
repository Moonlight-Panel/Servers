namespace MoonlightServers.DaemonShared.Http.Responses.Sys;

public class SystemInfoResponse
{
    public string DockerVersion { get; set; }
    public string Version { get; set; }
    public string OsName { get; set; }
    public long MemoryUsage { get; set; }
    public TimeSpan Uptime { get; set; }
    public int CpuUsage { get; set; }
}