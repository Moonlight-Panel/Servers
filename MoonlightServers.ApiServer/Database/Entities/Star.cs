namespace MoonlightServers.ApiServer.Database.Entities;

public class Star
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string Author { get; set; }

    public string? DonationUrl { get; set; }
    public string? UpdateUrl { get; set; }

    public string StartupCommand { get; set; }
    public string StopCommand { get; set; }
    public string OnlineDetection { get; set; }

    public string InstallShell { get; set; }
    public string InstallDockerImage { get; set; }
    public string InstallScript { get; set; }
    public int RequiredAllocations { get; set; }

    public string ParseConfiguration { get; set; }

    public int DefaultDockerImageIndex { get; set; }
    public bool AllowDockerImageChanging { get; set; }

    public List<StarVariable> Variables { get; set; } = new();
    public List<StarDockerImage> DockerImages { get; set; } = new();

    public StarFolder? Folder { get; set; }
}