using MoonCore.Extensions;
using Moonlight.ApiServer.Interfaces.Startup;
using MoonlightServers.ApiServer.Database;

namespace MoonlightServers.ApiServer.Startup;

public class PluginStartup : IPluginStartup
{
    public Task BuildApplication(IHostApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();

        builder.Services.AddDbContext<ServersDataContext>();
        
        return Task.CompletedTask;
    }

    public Task ConfigureApplication(IApplicationBuilder app)
     => Task.CompletedTask;

    public Task ConfigureEndpoints(IEndpointRouteBuilder routeBuilder)
        => Task.CompletedTask;
}