namespace MoonlightServers.DaemonShared.Http.Responses.Statistics;

public class StatisticsApplicationResponse
{
    public int CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public TimeSpan Uptime { get; set; }
}