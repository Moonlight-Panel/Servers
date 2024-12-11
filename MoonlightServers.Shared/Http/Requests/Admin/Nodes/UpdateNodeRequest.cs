using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.Nodes;

public class UpdateNodeRequest
{
    [Required(ErrorMessage = "You need to provide a name")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "You need to provide a fqdn")]
    public string Fqdn { get; set; }
    
    [Required(ErrorMessage = "You need to provide a http port")]
    [Range(1, 65535, ErrorMessage = "You need to provide a valid http port")]
    public int HttpPort { get; set; }
    
    [Required(ErrorMessage = "You need to provide a ftp port")]
    [Range(1, 65535, ErrorMessage = "You need to provide a valid ftp port")]
    public int FtpPort { get; set; }
    
    public bool EnableTransparentMode { get; set; }
    public bool EnableDynamicFirewall { get; set; }
}