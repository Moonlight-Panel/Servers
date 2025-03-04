using System.ComponentModel.DataAnnotations;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Variables;

public class UpdateServerVariableRangeRequest
{
    [Required(ErrorMessage = "You need to provide variables")]
    public UpdateServerVariableRequest[] Variables { get; set; }
}