using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers;

public class ServerCommandRequest
{
    [Required(ErrorMessage = "The command is required")]
    public string Command { get; set; }
}