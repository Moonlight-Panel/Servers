namespace MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;

public class NodeAllocationDetailResponse
{
    public int Id { get; set; }
    
    public string IpAddress { get; set; }
    public int Port { get; set; }
}