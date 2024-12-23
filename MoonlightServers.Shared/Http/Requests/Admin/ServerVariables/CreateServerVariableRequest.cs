using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Admin.ServerVariables;

public class CreateServerVariableRequest
{
    [Required(ErrorMessage = "You need to provide a key for the variable")]
    public string Key { get; set; }
    public string Value { get; set; }
}