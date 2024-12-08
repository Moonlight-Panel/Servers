namespace MoonlightServers.ApiServer.Models.Stars;

public class LegacyImageParseConfigModel
{
    public string Type { get; set; } = "";
    public string File { get; set; } = "";
    public Dictionary<string, string> Configuration { get; set; } = new();
}