using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.Stars;

public class CreateStarRequest
{
    [Required(ErrorMessage = "You need to specify a name")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "You need to specify a author")]
    public string Author { get; set; }
}