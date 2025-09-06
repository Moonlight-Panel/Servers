namespace MoonlightServers.Shared.Http.Responses.Admin.StarDockerImages;

public class StarDockerImageResponse
{
    public int Id { get; set; }

    public string DisplayName { get; set; }
    public string Identifier { get; set; }
    public bool AutoPulling { get; set; }
}