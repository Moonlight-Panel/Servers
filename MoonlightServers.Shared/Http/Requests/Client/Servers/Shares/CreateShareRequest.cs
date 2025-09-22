using System.ComponentModel.DataAnnotations;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Shares;

public record CreateShareRequest
{
    [Required(ErrorMessage = "You need to provide a username")]
    public string Username { get; set; }
    
    public Dictionary<string, ServerPermissionLevel> Permissions { get; set; } = [];
}