using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Attributes;
using MoonCore.Extended.Helpers;
using MoonlightServers.Daemon.Configuration;

namespace MoonlightServers.Daemon.Helpers;

[Singleton]
public class AccessTokenHelper
{
    private readonly AppConfiguration Configuration;

    public AccessTokenHelper(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

// TODO: Improve
    public bool Process(string accessToken, out Claim[] claims)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var data = jwtSecurityTokenHandler.ValidateToken(accessToken, new()
            {
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateActor = false,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Configuration.Security.Token)
                )
            }, out var _);

            claims = data.Claims.ToArray();

            return true;
        }
        catch (Exception e)
        {
            claims = [];
            return false;
        }
    }
}