using System.ComponentModel.DataAnnotations;
using MoonlightServers.Shared.Http.Requests.Admin.StarVariables;

namespace MoonlightServers.Shared.Http.Requests.Admin.Stars;

public class CreateStarRequest
{
    [Required(ErrorMessage = "You need to specify a name")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "You need to specify a author")]
    public string Author { get; set; }
}