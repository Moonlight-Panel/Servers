namespace MoonlightServers.Shared.Http.Responses.Admin.Servers;

public class ServerResponse
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    public int OwnerId { get; set; }
    public int Cpu { get; set; }
    public int Memory { get; set; }
    public int Disk { get; set; }
    public bool UseVirtualDisk { get; set; }
    public int Bandwidth { get; set; }

    public string? StartupOverride { get; set; }
    
    public int DockerImageIndex { get; set; }
    
    public int StarId { get; set; }
    
    public int NodeId { get; set; }
    
    public int[] AllocationIds { get; set; } = [];
}