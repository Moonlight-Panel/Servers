using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Responses.Admin.ServerVariables;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class ServerVariableMapper
{
    public static partial ServerVariableResponse ToAdminResponse(ServerVariable serverVariable);
    
    // EF Projections

    public static partial IQueryable<ServerVariableResponse> ProjectToAdminResponse(this IQueryable<ServerVariable> variables);
}