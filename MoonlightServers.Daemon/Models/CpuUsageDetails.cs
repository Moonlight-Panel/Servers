namespace MoonlightServers.Daemon.Models;

public class CpuUsageDetails
{
    public string Model { get; set; }
    public double OverallUsage { get; set; }
    public double[] PerCoreUsage { get; set; }
}