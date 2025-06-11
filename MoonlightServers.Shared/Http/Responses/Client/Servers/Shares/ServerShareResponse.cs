using MoonlightServers.Shared.Models;

namespace MoonlightServers.Shared.Http.Responses.Client.Servers.Shares;

public class ServerShareResponse
{
    public int Id { get; set; }
    public string Username { get; set; }
    public ServerSharePermission[] Permissions { get; set; }
}