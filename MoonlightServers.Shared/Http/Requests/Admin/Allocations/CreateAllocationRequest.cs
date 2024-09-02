using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.Allocations;

public class CreateAllocationRequest
{
    [Required(ErrorMessage = "You need to provide an ip address")]
    [RegularExpression("^(?:[0-9]{1,3}\\.){3}[0-9]{1,3}$", ErrorMessage = "You need tp provide a valid ip address")]
    public string IpAddress { get; set; } = "0.0.0.0";
    
    [Required(ErrorMessage = "You need to provgide a port")]
    [Range(1, 65535 , ErrorMessage = "You need to provide a valid port")]
    public int Port { get; set; }
}