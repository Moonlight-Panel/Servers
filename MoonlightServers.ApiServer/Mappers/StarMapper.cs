using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.StarDockerImages;
using MoonlightServers.Shared.Http.Requests.Admin.Stars;
using MoonlightServers.Shared.Http.Responses.Admin.Stars;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class StarMapper
{
    public static partial StarDetailResponse ToAdminResponse(Star star);
    public static partial Star ToStar(CreateStarRequest request);
    public static partial void Merge(UpdateStarRequest request, Star star);
}