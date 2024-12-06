using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.Stars;

public class UpdateStarRequest
{
    [Required(ErrorMessage = "You need to specify a name")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "You need to specify a version")]
    public string Version { get; set; }
    
    [Required(ErrorMessage = "You need to specify a author")]
    public string Author { get; set; }
    
    public string? UpdateUrl { get; set; }
    public string? DonateUrl { get; set; }

    [Required(ErrorMessage = "You need to specify a startup command")]
    public string StartupCommand { get; set; }

    [Required(ErrorMessage = "You need to specify a stop command")]
    public string StopCommand { get; set; }
    
    [Required(ErrorMessage = "You need to specify a online detection string")]
    public string OnlineDetection { get; set; }



    [Required(ErrorMessage = "You need to specify an install shell")]
    public string InstallShell { get; set; }

    [Required(ErrorMessage = "You need to specify an install docker image")]
    [RegularExpression("^(?:(?=[^:\\/]{1,253})(?!-)[a-zA-Z0-9-]{1,63}(?<!-)(?:\\.(?!-)[a-zA-Z0-9-]{1,63}(?<!-))*(?::[0-9]{1,5})?\\/)?((?![._-])(?:[a-z0-9._-]*)(?<![._-])(?:\\/(?![._-])[a-z0-9._-]*(?<![._-]))*)(?::(?![.-])[a-zA-Z0-9_.-]{1,128})?$", ErrorMessage = "You need to specify a valid docker image")]
    public string InstallDockerImage { get; set; }

    [Required(ErrorMessage = "You need to specify an install script")]
    public string InstallScript { get; set; }


    [Range(0, 20, ErrorMessage = "You need to provide a valid amount of allocations")]
    public int RequiredAllocations { get; set; }

    public bool AllowDockerImageChange { get; set; }

    [Required(ErrorMessage = "You need to provide parse configuration")]
    public string ParseConfiguration { get; set; }
}