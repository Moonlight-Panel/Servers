using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Variables;

public class UpdateServerVariableRequest
{
    [Required(ErrorMessage = "You need to provide a key")]
    public string Key { get; set; }
    
    [Required(ErrorMessage = "You need to provide a value", AllowEmptyStrings = true)]
    public string Value { get; set; }
}