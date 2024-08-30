using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Database.Entities;

public class StarVariable
{
    public int Id { get; set; }

    public string Key { get; set; }
    public string DefaultValue { get; set; }

    public string DisplayName { get; set; }
    public string Description { get; set; }

    public bool AllowViewing { get; set; } = false;
    public bool AllowEditing { get; set; } = false;

    public string? Filter { get; set; }
    public StarVariableType Type { get; set; } = StarVariableType.Text;
}