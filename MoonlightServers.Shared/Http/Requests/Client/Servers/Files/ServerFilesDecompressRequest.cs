using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Files;

public class ServerFilesDecompressRequest
{
    [Required(ErrorMessage = "You need to specify a type")]
    public string Type { get; set; }

    [Required(ErrorMessage = "You need to specify a path")]
    public string Path { get; set; }
    
    [Required(ErrorMessage = "You need to specify a destination")]
    public string Destination { get; set; }
}