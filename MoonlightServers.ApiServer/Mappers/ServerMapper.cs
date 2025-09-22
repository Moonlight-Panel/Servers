using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Servers;
using MoonlightServers.Shared.Http.Responses.Admin.Servers;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class ServerMapper
{
    [UserMapping(Default = true)]
    public static ServerResponse ToAdminServerResponse(Server server)
    {
        var response = ToAdminServerResponse_Internal(server);

        response.AllocationIds = server.Allocations.Select(x => x.Id).ToArray();

        return response;
    }

    private static partial ServerResponse ToAdminServerResponse_Internal(Server server);
    
    [MapperIgnoreSource(nameof(CreateServerRequest.Variables))]
    public static partial Server ToServer(CreateServerRequest request);
    public static partial void Merge(UpdateServerRequest request, Server server);
    
    // EF Projections

    public static partial IQueryable<ServerResponse> ProjectToAdminResponse(this IQueryable<Server> servers);
}