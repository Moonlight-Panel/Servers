using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.Servers;

public class UpdateServerRequest
{
    [Required(ErrorMessage = "You need to provide a name for the server")]
    public string Name { get; set; }
    public int OwnerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "You need to provide a valid cpu percent amount")]
    public int Cpu { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "You need to provide a valid memory amount")]
    public int Memory { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "You need to provide a valid disk amount")]
    public int Disk { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "You need to provide a valid bandwidth amount")]
    public int Bandwidth { get; set; }

    public string? StartupOverride { get; set; }
    
    public int DockerImageIndex { get; set; }
    
    // TODO: Add star change
    //public int StarId { get; set; }
    
    // TODO: Add transfer
    //public int NodeId { get; set; }

    public int[] AllocationIds { get; set; } = [];
}