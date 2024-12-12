namespace MoonlightServers.Daemon.Models.UnsafeDocker;

public class UsageDataReport
{
    public UsageData Containers { get; set; }
    public UsageData Images { get; set; }
    public UsageData BuildCache { get; set; }
}