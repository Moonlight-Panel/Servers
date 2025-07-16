using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Nodes;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class NodeMapper
{
    public static partial NodeResponse ToAdminNodeResponse(Node node);
    public static partial Node ToNode(CreateNodeRequest request);
    public static partial void Merge(UpdateNodeRequest request, Node node);
}