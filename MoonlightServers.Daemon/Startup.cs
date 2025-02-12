using System.Text.Json;
using Docker.DotNet;
using MoonCore.Configuration;
using MoonCore.Extended.Extensions;
using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonCore.Services;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon;

public class Startup
{
    private string[] Args;

    // Configuration
    private AppConfiguration Configuration;
    private ConfigurationService ConfigurationService;
    private ConfigurationOptions ConfigurationOptions;

    // Logging
    private ILoggerProvider[] LoggerProviders;
    private ILoggerFactory LoggerFactory;
    private ILogger<Startup> Logger;

    // WebApplication Stuff
    private WebApplication WebApplication;
    private WebApplicationBuilder WebApplicationBuilder;

    public async Task Run(string[] args)
    {
        Args = args;

        await SetupStorage();
        await SetupAppConfiguration();
        await SetupLogging();

        await CreateWebApplicationBuilder();

        await RegisterAppConfiguration();
        await RegisterLogging();
        await RegisterBase();
        await RegisterDocker();
        await RegisterServers();
        await RegisterSignalR();
        await RegisterCors();

        await BuildWebApplication();

        await UseBase();
        await UseCors();
        await UseBaseMiddleware();

        await MapBase();
        await MapHubs();

        await WebApplication.RunAsync();
    }

    private Task SetupStorage()
    {
        Directory.CreateDirectory("storage");
        Directory.CreateDirectory(PathBuilder.Dir("storage", "logs"));
        Directory.CreateDirectory(PathBuilder.Dir("storage", "plugins"));

        return Task.CompletedTask;
    }

    #region Base

    private Task RegisterBase()
    {
        WebApplicationBuilder.Services.AutoAddServices<Startup>();
        WebApplicationBuilder.Services.AddControllers();

        WebApplicationBuilder.Services.AddApiExceptionHandler();

        return Task.CompletedTask;
    }

    private Task UseBase()
    {
        WebApplication.UseRouting();
        WebApplication.UseApiExceptionHandler();

        return Task.CompletedTask;
    }

    private Task UseBaseMiddleware()
    {
        return Task.CompletedTask;
    }

    private Task MapBase()
    {
        WebApplication.MapControllers();

        return Task.CompletedTask;
    }

    #endregion

    #region Docker

    private Task RegisterDocker()
    {
        var dockerClient = new DockerClientConfiguration(
            new Uri(Configuration.Docker.Uri)
        ).CreateClient();

        WebApplicationBuilder.Services.AddSingleton(dockerClient);

        return Task.CompletedTask;
    }

    #endregion

    #region Configurations

    private Task SetupAppConfiguration()
    {
        ConfigurationService = new ConfigurationService();

        // Setup options
        ConfigurationOptions = new ConfigurationOptions();

        ConfigurationOptions.AddConfiguration<AppConfiguration>("app");
        ConfigurationOptions.Path = PathBuilder.Dir("storage");
        ConfigurationOptions.EnvironmentPrefix = "WebAppTemplate".ToUpper();

        // Create minimal logger
        var loggerFactory = new LoggerFactory();

        loggerFactory.AddMoonCore(configuration =>
        {
            configuration.Console.Enable = true;
            configuration.Console.EnableAnsiMode = true;
            configuration.FileLogging.Enable = false;
        });

        var logger = loggerFactory.CreateLogger<ConfigurationService>();

        // Retrieve configuration
        Configuration = ConfigurationService.GetConfiguration<AppConfiguration>(
            ConfigurationOptions,
            logger
        );

        return Task.CompletedTask;
    }

    private Task RegisterAppConfiguration()
    {
        ConfigurationService.RegisterInDi(ConfigurationOptions, WebApplicationBuilder.Services);
        WebApplicationBuilder.Services.AddSingleton(ConfigurationService);

        return Task.CompletedTask;
    }

    #endregion

    #region Web Application

    private Task CreateWebApplicationBuilder()
    {
        WebApplicationBuilder = WebApplication.CreateBuilder(Args);
        return Task.CompletedTask;
    }

    private Task BuildWebApplication()
    {
        WebApplication = WebApplicationBuilder.Build();
        return Task.CompletedTask;
    }

    #endregion

    #region Logging

    private Task SetupLogging()
    {
        LoggerProviders = LoggerBuildHelper.BuildFromConfiguration(configuration =>
        {
            configuration.Console.Enable = true;
            configuration.Console.EnableAnsiMode = true;

            configuration.FileLogging.Enable = true;
            configuration.FileLogging.EnableLogRotation = true;
            configuration.FileLogging.Path = PathBuilder.File("storage", "logs", "WebAppTemplate.log");
            configuration.FileLogging.RotateLogNameTemplate =
                PathBuilder.File("storage", "logs", "WebAppTemplate.log.{0}");
        });

        LoggerFactory = new LoggerFactory();
        LoggerFactory.AddProviders(LoggerProviders);

        Logger = LoggerFactory.CreateLogger<Startup>();

        return Task.CompletedTask;
    }

    private async Task RegisterLogging()
    {
        // Configure application logging
        WebApplicationBuilder.Logging.ClearProviders();
        WebApplicationBuilder.Logging.AddProviders(LoggerProviders);

        // Logging levels
        var logConfigPath = PathBuilder.File("storage", "logConfig.json");

        // Ensure logging config, add a default one is missing
        if (!File.Exists(logConfigPath))
        {
            var logLevels = new Dictionary<string, string>
            {
                { "Default", "Information" },
                { "Microsoft.AspNetCore", "Warning" },
                { "System.Net.Http.HttpClient", "Warning" }
            };

            var logLevelsJson = JsonSerializer.Serialize(logLevels);
            var logConfig = "{\"LogLevel\":" + logLevelsJson + "}";
            await File.WriteAllTextAsync(logConfigPath, logConfig);
        }

        // Configure logging configuration
        WebApplicationBuilder.Logging.AddConfiguration(
            await File.ReadAllTextAsync(logConfigPath)
        );

        // Mute exception handler middleware
        // https://github.com/dotnet/aspnetcore/issues/19740
        WebApplicationBuilder.Logging.AddFilter(
            "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
            LogLevel.Critical
        );

        WebApplicationBuilder.Logging.AddFilter(
            "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware",
            LogLevel.Critical
        );
    }

    #endregion

    #region Servers

    private Task RegisterServers()
    {
        WebApplicationBuilder.Services.AddHostedService(
            sp => sp.GetRequiredService<ServerService>()
        );

        return Task.CompletedTask;
    }

    private Task UseServers()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Hubs

    private Task RegisterSignalR()
    {
        WebApplicationBuilder.Services.AddSignalR();
        return Task.CompletedTask;
    }

    private Task MapHubs()
    {
        WebApplication.MapHub<ServerWebSocketHub>("api/servers/ws");

        return Task.CompletedTask;
    }

    #endregion

    #region Cors

    private Task RegisterCors()
    {
        //TODO: IMPORTANT: CHANGE !!!
        WebApplicationBuilder.Services.AddCors(x =>
            x.AddDefaultPolicy(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod().Build()
            )
        );

        return Task.CompletedTask;
    }

    private Task UseCors()
    {
        WebApplication.UseCors();
        return Task.CompletedTask;
    }

    #endregion
}