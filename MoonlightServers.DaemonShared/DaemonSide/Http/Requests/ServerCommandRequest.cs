using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.DaemonShared.DaemonSide.Http.Requests;

public class ServerCommandRequest
{
    [Required(ErrorMessage = "The command is required")]
    public string Command { get; set; }
}