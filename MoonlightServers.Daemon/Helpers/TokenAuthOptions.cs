using Microsoft.AspNetCore.Authentication;

namespace MoonlightServers.Daemon.Helpers;

public class TokenAuthOptions : AuthenticationSchemeOptions
{
    public string Token { get; set; }
}