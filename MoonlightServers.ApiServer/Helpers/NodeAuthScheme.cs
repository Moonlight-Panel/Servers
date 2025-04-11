using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;

namespace MoonlightServers.ApiServer.Helpers;

public class NodeAuthScheme : AuthenticationHandler<NodeAuthOptions>
{
    public NodeAuthScheme(IOptionsMonitor<NodeAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    public NodeAuthScheme(IOptionsMonitor<NodeAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(
        options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.NoResult();

        var authHeaderValue = Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeaderValue))
            return AuthenticateResult.NoResult();

        if (!authHeaderValue.Contains("Bearer "))
            return AuthenticateResult.NoResult();

        var tokenParts = authHeaderValue
            .Replace("Bearer ", "")
            .Trim()
            .Split('.');

        if (tokenParts.Length != 2)
            return AuthenticateResult.NoResult();

        var tokenId = tokenParts[0];
        var token = tokenParts[1];

        if (tokenId.Length != 6)
            return AuthenticateResult.NoResult();

        var nodeRepo = Context.RequestServices.GetRequiredService<DatabaseRepository<Node>>();

        var node = await nodeRepo
            .Get()
            .FirstOrDefaultAsync(x => x.TokenId == tokenId);

        if (node == null)
            return AuthenticateResult.NoResult();

        if (node.Token != token)
            return AuthenticateResult.NoResult();

        return AuthenticateResult.Success(
            new AuthenticationTicket(
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [
                            new Claim("nodeId", node.Id.ToString())
                        ],
                        "nodeAuthentication"
                    )
                ),
                "nodeAuthentication"
            )
        );
    }
}