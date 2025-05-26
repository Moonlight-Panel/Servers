namespace MoonlightServers.Daemon.Models;

public class MemoryUsageDetails
{
    public long Total { get; set; }
    public long Available { get; set; }
    public long Free { get; set; }
    public long Cached { get; set; }
    public long SwapTotal { get; set; }
    public long SwapFree { get; set; }
}