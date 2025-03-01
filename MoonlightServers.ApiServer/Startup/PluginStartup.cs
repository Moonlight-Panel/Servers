using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using MoonCore.Extensions;
using Moonlight.ApiServer.Interfaces.Startup;
using MoonlightServers.ApiServer.Database;
using MoonlightServers.ApiServer.Implementations;

namespace MoonlightServers.ApiServer.Startup;

public class PluginStartup : IPluginStartup
{
    public Task BuildApplication(IHostApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();

        builder.Services.AddDbContext<ServersDataContext>();
        
        // Configure authentication for the remote endpoints
        builder.Services
            .AddAuthentication()
            .AddJwtBearer("serverNodeAuthentication", options =>
            {
                options.TokenValidationParameters = new()
                {
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = false,
                    ValidateActor = false,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true
                };
            });

        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, NodeJwtBearerOptions>();
        
        return Task.CompletedTask;
    }

    public Task ConfigureApplication(IApplicationBuilder app)
     => Task.CompletedTask;

    public Task ConfigureEndpoints(IEndpointRouteBuilder routeBuilder)
        => Task.CompletedTask;
}