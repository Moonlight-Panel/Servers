using MoonlightServers.Shared.Http.Responses.Admin.StarDockerImages;
using MoonlightServers.Shared.Http.Responses.Admin.StarVariables;

namespace MoonlightServers.Shared.Http.Responses.Admin.Stars;

public class StarDetailResponse
{
    public int Id { get; set; }
    
    // References
    public StarVariableDetailResponse[] Variables { get; set; }
    public StarDockerImageDetailResponse[] DockerImages { get; set; }

    // Meta
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string? UpdateUrl { get; set; }
    public string? DonateUrl { get; set; }

    // Start and stop
    public string StartupCommand { get; set; }
    public string StopCommand { get; set; }
    public string OnlineDetection { get; set; }

    // Install
    public string InstallShell { get; set; }
    public string InstallDockerImage { get; set; }
    public string InstallScript { get; set; }

    // Misc
    public int RequiredAllocations { get; set; }
    public bool AllowDockerImageChange { get; set; }
    public string ParseConfiguration { get; set; }
}