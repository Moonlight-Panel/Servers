namespace MoonlightServers.Shared.Http.Responses.Admin.Stars;

public class StarResponse
{
    public int Id { get; set; }

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
    public int DefaultDockerImage { get; set; }
    public string ParseConfiguration { get; set; }
}