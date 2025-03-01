using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;

namespace MoonlightServers.ApiServer.Implementations;

public class NodeJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IServiceProvider ServiceProvider;

    public NodeJwtBearerOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void Configure(JwtBearerOptions options)
    {
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        // Dont configure any other scheme
        if (name != "serverNodeAuthentication")
            return;

        options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, kid, _) =>
        {
            if (string.IsNullOrEmpty(kid))
                return [];

            if (kid.Length != 6)
                return [];

            using var scope = ServiceProvider.CreateScope();

            var nodeRepo = scope.ServiceProvider.GetRequiredService<DatabaseRepository<Node>>();

            var node = nodeRepo
                .Get()
                .FirstOrDefault(x => x.TokenId == kid);

            if (node == null)
                return [];

            return
            [
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(node.Token)
                )
            ];
        };
    }
}