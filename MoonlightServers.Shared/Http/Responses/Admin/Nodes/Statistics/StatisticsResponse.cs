namespace MoonlightServers.Shared.Http.Responses.Admin.Nodes.Statistics;

public class StatisticsResponse
{
    public CpuData Cpu { get; set; }
    public MemoryData Memory { get; set; }

    public DiskData[] Disks { get; set; }

    public record DiskData
    {
        public string Device { get; set; }
        public string MountPath { get; set; }
        public ulong DiskTotal { get; set; }
        public ulong DiskFree { get; set; }
        public ulong InodesTotal { get; set; }
        public ulong InodesFree { get; set; }
    }

    public record MemoryData
    {
        public long Total { get; set; }
        public long Available { get; set; }
        public long Free { get; set; }
        public long Cached { get; set; }
        public long SwapTotal { get; set; }
        public long SwapFree { get; set; }
    }

    public record CpuData
    {
        public string Model { get; set; }
        public double Usage { get; set; }
        public double[] UsagePerCore { get; set; }
    }
}