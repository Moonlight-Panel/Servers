namespace MoonlightServers.DaemonShared.PanelSide.Http.Responses;

public class ServerDataResponse
{
    public int Id { get; set; }
    public string StartupCommand { get; set; }
    public string OnlineDetection { get; set; }
    public string StopCommand { get; set; }
    public string DockerImage { get; set; }
    public bool PullDockerImage { get; set; }
    public string ParseConiguration { get; set; }

    public int Cpu { get; set; }
    public int Memory { get; set; }
    public int Disk { get; set; }

    public bool UseVirtualDisk { get; set; }
    public int Bandwidth { get; set; }

    public AllocationDataResponse[] Allocations { get; set; }
    public Dictionary<string, string> Variables { get; set; }
}