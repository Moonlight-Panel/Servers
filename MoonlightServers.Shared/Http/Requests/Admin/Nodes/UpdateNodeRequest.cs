using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.Nodes;

public class UpdateNodeRequest
{
    [Required(ErrorMessage = "You need to provide a name")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "You need to provide a fqdn")]
    public string Fqdn { get; set; }

    [Range(1, 65535, ErrorMessage = "You need to provide a valid port")]
    public int ApiPort { get; set; } = 8080;

    public bool SslEnabled { get; set; } = false;
}