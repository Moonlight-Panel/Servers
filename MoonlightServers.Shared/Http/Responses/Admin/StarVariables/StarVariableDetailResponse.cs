using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Http.Responses.Admin.StarVariables;

public class StarVariableDetailResponse
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    public string Description { get; set; }
    
    public string Key { get; set; }
    public string DefaultValue { get; set; }

    public bool AllowViewing { get; set; }
    public bool AllowEditing { get; set; }

    public StarVariableType Type { get; set; }
    public string? Filter { get; set; }
}