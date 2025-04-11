using MoonCore.Extensions;
using Moonlight.ApiServer.Interfaces.Startup;
using MoonlightServers.ApiServer.Database;
using MoonlightServers.ApiServer.Helpers;

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
            .AddScheme<NodeAuthOptions, NodeAuthScheme>("nodeAuthentication", null);
        
        return Task.CompletedTask;
    }

    public Task ConfigureApplication(IApplicationBuilder app)
     => Task.CompletedTask;

    public Task ConfigureEndpoints(IEndpointRouteBuilder routeBuilder)
        => Task.CompletedTask;
}