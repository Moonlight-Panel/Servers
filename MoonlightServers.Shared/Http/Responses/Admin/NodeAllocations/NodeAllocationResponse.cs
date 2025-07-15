namespace MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;

public class NodeAllocationResponse
{
    public int Id { get; set; }
    
    public string IpAddress { get; set; }
    public int Port { get; set; }
}