namespace MoonlightServers.Shared.Http.Responses.Admin.Nodes;

public class StatusNodeResponse
{
    public string CpuModel { get; set; }
    public double[] CpuUsage { get; set; }
    public TimeSpan Uptime { get; set; }

    public ulong MemoryTotal { get; set; }
    public ulong MemoryFree { get; set; }
    public ulong MemoryAvailable { get; set; }
    public ulong MemoryCached { get; set; }

    public ulong SwapTotal { get; set; }
    public ulong SwapFree { get; set; }

    public ulong DiskTotal { get; set; }
    public ulong DiskFree { get; set; }

    public ulong DiskTotalInodes { get; set; }
    public ulong DiskFreeInodes { get; set; }
}