using MoonlightServers.Shared.Http.Responses.Client.Servers.Allocations;

namespace MoonlightServers.Shared.Http.Responses.Client.Servers;

public class ServerDetailResponse
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int Cpu { get; set; }
    public int Memory { get; set; }
    public int Disk { get; set; }
    
    public string NodeName { get; set; }
    public string StarName { get; set; }

    public AllocationDetailResponse[] Allocations { get; set; }
}