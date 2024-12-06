using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.StarDockerImages;

public class UpdateStarDockerImageRequest
{
    [Required(ErrorMessage = "You need to provide a display name")]
    public string DisplayName { get; set; }
    
    [Required(ErrorMessage = "You need to specify a docker image identifier")]
    [RegularExpression("^(?:(?=[^:\\/]{1,253})(?!-)[a-zA-Z0-9-]{1,63}(?<!-)(?:\\.(?!-)[a-zA-Z0-9-]{1,63}(?<!-))*(?::[0-9]{1,5})?\\/)?((?![._-])(?:[a-z0-9._-]*)(?<![._-])(?:\\/(?![._-])[a-z0-9._-]*(?<![._-]))*)(?::(?![.-])[a-zA-Z0-9_.-]{1,128})?$", ErrorMessage = "You need to specify a valid docker image identifier")]
    public string Identifier { get; set; }
    
    public bool AutoPulling { get; set; }
}