using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MoonlightServers.Daemon.Helpers;

public class TokenAuthScheme : AuthenticationHandler<TokenAuthOptions>
{
    public TokenAuthScheme(IOptionsMonitor<TokenAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    public TokenAuthScheme(IOptionsMonitor<TokenAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(
        options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var authHeaderValue = Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeaderValue))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!authHeaderValue.Contains("Bearer "))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedToken = authHeaderValue
            .Replace("Bearer ", "")
            .Trim();

        if (providedToken != Options.Token)
            return Task.FromResult(AuthenticateResult.NoResult());

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(
                new ClaimsPrincipal(
                    new ClaimsIdentity("token")
                ),
                "token"
            )
        ));
    }
}