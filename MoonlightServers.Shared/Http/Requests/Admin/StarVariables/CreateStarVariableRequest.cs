using System.ComponentModel.DataAnnotations;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Interfaces;

namespace MoonlightServers.Shared.Http.Requests.Admin.StarVariables;

public class CreateStarVariableRequest : IStarVariable
{
    [Required(ErrorMessage = "You need to specify a variable name")]
    public string Name { get; set; }

    [Required(ErrorMessage = "You need to specify a variable description", AllowEmptyStrings = true)]
    public string Description { get; set; } = "";
    
    [Required(ErrorMessage = "You need to specify a variable key")]
    public string Key { get; set; }

    [Required(ErrorMessage = "You need to specify a variable default value", AllowEmptyStrings = true)]
    public string DefaultValue { get; set; } = "";

    public bool AllowViewing { get; set; }
    public bool AllowEditing { get; set; }

    public StarVariableType Type { get; set; } = StarVariableType.Text;
    public string? Filter { get; set; } = null;
}