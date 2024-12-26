namespace MoonlightServers.Daemon.Models.Cache;

public class ServerConfiguration
{
    public int Id { get; set; }

    // Limits
    public int Cpu { get; set; }
    public int Memory { get; set; }
    public int Disk { get; set; }
    public int Bandwidth { get; set; }
    public bool UseVirtualDisk { get; set; }

    // Start, Stop & Status
    public string StartupCommand { get; set; }
    public string StopCommand { get; set; }
    public string OnlineDetection { get; set; }
    
    // Container
    public string DockerImage { get; set; }
    public AllocationConfiguration[] Allocations { get; set; }

    public Dictionary<string, string> Variables { get; set; }
    
    public struct AllocationConfiguration
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
    }
}