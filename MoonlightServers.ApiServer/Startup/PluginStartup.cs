using MoonCore.Extensions;
using Moonlight.ApiServer.Interfaces.Startup;
using Moonlight.ApiServer.Services;

namespace MoonlightServers.ApiServer.Startup;

public class PluginStartup : IAppStartup
{
    private readonly ILogger<PluginStartup> Logger;
    private readonly BundleService BundleService;

    public PluginStartup(ILogger<PluginStartup> logger, BundleService bundleService)
    {
        Logger = logger;
        BundleService = bundleService;
    }

    public Task BuildApp(IHostApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();
        
        BundleService.BundleCss("css/MoonlightServers.min.css");

        return Task.CompletedTask;
    }

    public Task ConfigureApp(IApplicationBuilder app)
    {
        return Task.CompletedTask;
    }
}