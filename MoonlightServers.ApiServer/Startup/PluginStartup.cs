using MoonCore.Extensions;
using Moonlight.ApiServer.Helpers;
using Moonlight.ApiServer.Interfaces.Startup;
using Moonlight.ApiServer.Services;
using MoonlightServers.ApiServer.Database;

namespace MoonlightServers.ApiServer.Startup;

public class PluginStartup : IPluginStartup
{
    private readonly BundleService BundleService;

    public PluginStartup(BundleService bundleService)
    {
        BundleService = bundleService;
    }

    public Task BuildApplication(IHostApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();
        
        BundleService.BundleCss("css/MoonlightServers.min.css");
        BundleService.BundleCss("css/XtermBlazor.min.css");
        
        return Task.CompletedTask;
    }

    public Task ConfigureApplication(IApplicationBuilder app)
     => Task.CompletedTask;

    public Task ConfigureDatabase(DatabaseContextCollection collection)
    {
        collection.Add<ServersDataContext>();
        
        return Task.CompletedTask;
    }

    public Task ConfigureEndpoints(IEndpointRouteBuilder routeBuilder)
        => Task.CompletedTask;
}