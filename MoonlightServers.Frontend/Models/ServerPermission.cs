namespace MoonlightServers.Frontend.Models;

public record ServerPermission
{
    public string Identifier { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
}