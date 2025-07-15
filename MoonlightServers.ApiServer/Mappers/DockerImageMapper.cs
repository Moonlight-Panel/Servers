using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.StarDockerImages;
using MoonlightServers.Shared.Http.Responses.Admin.StarDockerImages;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class DockerImageMapper
{
    public static partial StarDockerImageDetailResponse ToAdminResponse(StarDockerImage dockerImage);
    public static partial StarDockerImage ToDockerImage(CreateStarDockerImageRequest request);
    public static partial StarDockerImage Merge(UpdateStarDockerImageRequest request, StarDockerImage variable);
}