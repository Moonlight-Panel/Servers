namespace MoonlightServers.Shared.Http.Responses.Admin.Allocations;

public class DetailAllocationResponse
{
    public int Id { get; set; }
    public string IpAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; }
}