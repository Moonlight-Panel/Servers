using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.NodeAllocations;

public class CreateNodeAllocationRangeRequest
{
    [Required(ErrorMessage = "You need to provide an ip address")]
    [RegularExpression(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$", ErrorMessage = "You need to provide a valid ip address")]
    public string IpAddress { get; set; }
    
    [Required(ErrorMessage = "You need to provide a start port")]
    [Range(1, 65535, ErrorMessage = "You need to provide a valid start port")]
    public int Start { get; set; }
    
    [Required(ErrorMessage = "You need to provide a end port")]
    [Range(1, 65535, ErrorMessage = "You need to provide a valid end port")]
    public int End { get; set; }
}