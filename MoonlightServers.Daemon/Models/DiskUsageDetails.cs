namespace MoonlightServers.Daemon.Models;

public class DiskUsageDetails
{
    public string Device { get; set; }
    public string MountPath { get; set; }
    public ulong DiskTotal { get; set; }
    public ulong DiskFree { get; set; }
    public ulong InodesTotal { get; set; }
    public ulong InodesFree { get; set; }
}