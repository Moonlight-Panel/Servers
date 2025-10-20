using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoonCore.Extensions;
using Moonlight.ApiServer.Configuration;
using Moonlight.ApiServer.Models;
using Moonlight.ApiServer.Plugins;
using MoonlightServers.ApiServer.Database;
using MoonlightServers.ApiServer.Helpers;
using MoonlightServers.ApiServer.Implementations.ServerAuthFilters;
using MoonlightServers.ApiServer.Interfaces;

namespace MoonlightServers.ApiServer;

public class PluginStartup : IPluginStartup
{
    public void AddPlugin(WebApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();

        builder.Services.AddDbContext<ServersDataContext>();

        // Configure authentication for the remote endpoints
        builder.Services
            .AddAuthentication()
            .AddScheme<NodeAuthOptions, NodeAuthScheme>("nodeAuthentication", null);

        var configuration = AppConfiguration.CreateEmpty();
        builder.Configuration.Bind(configuration);

        if (configuration.Frontend.EnableHosting)
        {
            builder.Services.AddSingleton(new FrontendConfigurationOption()
            {
                Scripts =
                [
                    "/_content/MoonlightServers.Frontend/js/XtermBlazor.min.js",
                    "/_content/MoonlightServers.Frontend/js/addon-fit.js",
                    "/_content/MoonlightServers.Frontend/js/moonlightServers.js"
                ],
                Styles = ["/_content/MoonlightServers.Frontend/css/XtermBlazor.min.css"]
            });
        }

        //  Add server auth filters
        builder.Services.AddSingleton<IServerAuthorizationFilter, OwnerAuthFilter>();
        builder.Services.AddScoped<IServerAuthorizationFilter, AdminAuthFilter>();
        builder.Services.AddScoped<IServerAuthorizationFilter, ShareAuthFilter>();
    }

    public void UsePlugin(WebApplication app)
    {
    }

    public void MapPlugin(WebApplication app)
    {
    }
}