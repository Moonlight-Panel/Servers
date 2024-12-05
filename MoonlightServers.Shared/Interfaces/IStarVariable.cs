using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Interfaces;

public interface IStarVariable // For a common abstraction between create and update model to use in a shared form component
{
    public string Name { get; set; }

    public string Description { get; set; }
    
    public string Key { get; set; }

    public string DefaultValue { get; set; }

    public bool AllowViewing { get; set; }
    public bool AllowEditing { get; set; }

    public StarVariableType Type { get; set; }
    public string? Filter { get; set; }
}