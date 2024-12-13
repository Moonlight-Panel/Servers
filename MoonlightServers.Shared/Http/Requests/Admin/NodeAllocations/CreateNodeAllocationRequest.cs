using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.NodeAllocations;

public class CreateNodeAllocationRequest
{
    [Required(ErrorMessage = "You need to provide an ip address")]
    [RegularExpression(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$", ErrorMessage = "You need to provide a valid ip address")]
    public string IpAddress { get; set; }
    
    [Required(ErrorMessage = "You need to provide a port")]
    [Range(1, 65535, ErrorMessage = "You need to provide a valid port")]
    public int Port { get; set; }
}