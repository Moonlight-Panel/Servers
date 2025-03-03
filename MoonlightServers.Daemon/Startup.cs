using System.Text;
using System.Text.Json;
using Docker.DotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Configuration;
using MoonCore.EnvConfiguration;
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

        await ConfigureKestrel();
        await RegisterAppConfiguration();
        await RegisterLogging();
        await RegisterBase();
        await RegisterAuth();
        await RegisterDocker();
        await RegisterServers();
        await RegisterSignalR();
        await RegisterCors();

        await BuildWebApplication();

        await UseBase();
        await UseAuth();
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

    private Task ConfigureKestrel()
    {
        WebApplicationBuilder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = ByteConverter.FromMegaBytes(Configuration.Files.UploadLimit).Bytes;
        });
        
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

    private async Task SetupAppConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        
        // Ensure configuration file exists
        var jsonFilePath = PathBuilder.File(Directory.GetCurrentDirectory(), "storage", "app.json");

        if (!File.Exists(jsonFilePath))
            await File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(new AppConfiguration()));

        configurationBuilder.AddJsonFile(
            jsonFilePath
        );
        
        configurationBuilder.AddEnvironmentVariables(prefix: "MOONLIGHT_", separator: "_");

        var configurationRoot = configurationBuilder.Build();

        // Retrieve configuration
        Configuration = configurationRoot.Get<AppConfiguration>()!;
    }

    private Task RegisterAppConfiguration()
    {
        WebApplicationBuilder.Services.AddSingleton(Configuration);

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

    #region Authentication

    private Task RegisterAuth()
    {
        WebApplicationBuilder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Configuration.Security.Token
                    )),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ClockSkew = TimeSpan.Zero
                };
            });
        
        WebApplicationBuilder.Services.AddAuthorization();
        
        return Task.CompletedTask;
    }
    
    private Task UseAuth()
    {
        WebApplication.UseAuthentication();
        WebApplication.UseAuthorization();
        
        return Task.CompletedTask;
    }

    #endregion
}