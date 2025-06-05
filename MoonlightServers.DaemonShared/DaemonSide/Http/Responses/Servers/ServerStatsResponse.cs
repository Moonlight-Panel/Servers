namespace MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;

public class ServerStatsResponse
{
    public double CpuUsage { get; set; }
    public ulong MemoryUsage { get; set; }
    public ulong NetworkRead { get; set; }
    public ulong NetworkWrite { get; set; }
    public ulong IoRead { get; set; }
    public ulong IoWrite { get; set; }
}