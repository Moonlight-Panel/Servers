using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoonCore.Extended.Helpers;
using Moonlight.ApiServer.App.PluginApi;
using MoonlightServers.ApiServer.Database;

namespace MoonlightServers.ApiServer;

public class MoonlightServersPlugin : MoonlightPlugin
{
    public MoonlightServersPlugin(ILogger logger, PluginService pluginService) : base(logger, pluginService)
    {
    }

    public override Task OnLoaded()
    {
        return Task.CompletedTask;
    }

    public override Task OnAppBuilding(WebApplicationBuilder builder, DatabaseHelper databaseHelper)
    {
        // Register database
        builder.Services.AddDbContext<ServersContext>();
        databaseHelper.AddDbContext<ServersContext>();
        
        return Task.CompletedTask;
    }

    public override Task OnAppConfiguring(WebApplication app)
    {
        return Task.CompletedTask;
    }
}