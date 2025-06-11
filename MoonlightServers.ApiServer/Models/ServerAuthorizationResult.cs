using MoonlightServers.ApiServer.Database.Entities;

namespace MoonlightServers.ApiServer.Models;

public record ServerAuthorizationResult
{
    public bool Succeeded { get; set; }
    public ServerShare? Share { get; set; }
    public string? Message { get; set; }

    public static ServerAuthorizationResult Success(ServerShare? share = null)
    {
        return new()
        {
            Succeeded = true,
            Share = share
        };
    }

    public static ServerAuthorizationResult Failed(string? message = null)
    {
        return new()
        {
            Succeeded = false,
            Message = message
        };
    }
}