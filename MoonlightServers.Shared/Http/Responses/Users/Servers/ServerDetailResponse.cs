using MoonlightServers.Shared.Http.Responses.User.Allocations;

namespace MoonlightServers.Shared.Http.Responses.Users.Servers;

public class ServerDetailResponse
{
    public int Id { get; set; }

    public string Name { get; set; }
    
    public string NodeName { get; set; }
    public string StarName { get; set; }

    public AllocationDetailResponse[] Allocations { get; set; }
}