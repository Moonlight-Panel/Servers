namespace MoonlightServers.ApiServer.Database.Entities;

public class StarDockerImage
{
    public int Id { get; set; }

    public string Identifier { get; set; }
    public string DisplayName { get; set; }

    public bool AutoPulling { get; set; } = true;
}