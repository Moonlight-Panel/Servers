namespace MoonlightServers.ApiServer.Database.Entities;

public class Server
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int Cpu { get; set; }
    public int Memory { get; set; }
    public int Disk { get; set; }

    public string? OverrideStartupCommand { get; set; } = null;
    public string? OverrideDockerImage { get; set; } = null;
    
    public bool VirtualDiskEnabled { get; set; } = false;

    public Star Star { get; set; }
    public int DockerImageIndex { get; set; } = 0;

    public Node Node { get; set; }
    public Network? Network { get; set; }
    public List<Allocation> Allocations { get; set; } = new();
    public List<ServerVariable> Variables { get; set; } = new();
    public List<Backup> Backups { get; set; } = new();
}