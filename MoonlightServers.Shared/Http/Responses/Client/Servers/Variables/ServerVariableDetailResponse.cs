using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Http.Responses.Client.Servers.Variables;

public class ServerVariableDetailResponse
{
    public string Key { get; set; }
    public string Value { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public StarVariableType Type { get; set; }
    public string? Filter { get; set; }
}