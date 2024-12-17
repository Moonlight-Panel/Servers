namespace MoonlightServers.ApiServer.Database.Entities;

public class Server
{
    public int Id { get; set; }
    
    // Relations
    public Star Star { get; set; }
    public Node Node { get; set; }
    public List<Allocation> Allocations { get; set; } = new();
    public List<ServerVariable> Variables { get; set; } = new();
    public List<ServerBackup> Backups { get; set; } = new();

    // Meta
    public string Name { get; set; }
    public int OwnerId { get; set; }
    
    // Star specific stuff
    public string? StartupOverride { get; set; }
    public int DockerImageIndex { get; set; }
    
    // Resources and limits
    public int Cpu { get; set; }
    public int Memory { get; set; }
    public int Disk { get; set; }
    public bool UseVirtualDisk { get; set; }
    public int Bandwidth { get; set; }
}