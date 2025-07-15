using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.StarVariables;
using MoonlightServers.Shared.Http.Responses.Admin.StarVariables;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class StarVariableMapper
{
    public static partial StarVariableDetailResponse ToAdminResponse(StarVariable variable);
    public static partial StarVariable ToStarVariable(CreateStarVariableRequest  request);
    public static partial StarVariable Merge(UpdateStarVariableRequest request, StarVariable variable);
}