using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Files;

public class ServerFilesCompressRequest
{
    [Required(ErrorMessage = "You need to specify a type")]
    public string Type { get; set; }

    public string[] Items { get; set; } = [];
    
    [Required(ErrorMessage = "You need to specify a destination")]
    public string Destination { get; set; }
}