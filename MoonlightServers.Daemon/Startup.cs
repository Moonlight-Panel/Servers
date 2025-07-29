using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using Docker.DotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.IdentityModel.Tokens;
using MoonCore.EnvConfiguration;
using MoonCore.Extended.Extensions;
using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonCore.Logging;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSys;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.ServerSys.Implementations;
using MoonlightServers.Daemon.ServerSystem;
using MoonlightServers.Daemon.Services;
using Server = MoonlightServers.Daemon.ServerSystem.Server;

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
        await UseCors();
        await UseAuth();
        await UseBaseMiddleware();

        await MapBase();
        await MapHubs();

        Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("Press enter to create server instance");
                Console.ReadLine();

                var config = new ServerConfiguration()
                {
                    Allocations = [
                        new ServerConfiguration.AllocationConfiguration()
                        {
                            IpAddress = "0.0.0.0",
                            Port = 25565
                        }
                    ],
                    Cpu = 400,
                    Disk = 10240,
                    DockerImage = "ghcr.io/parkervcp/yolks:java_21",
                    Id = 69,
                    Memory = 4096,
                    OnlineDetection = "\\)! For help, type ",
                    StartupCommand = "java -Xms128M -Xmx{{SERVER_MEMORY}}M -Dterminal.jline=false -Dterminal.ansi=true -jar {{SERVER_JARFILE}}",
                    StopCommand = "stop",
                    Variables = new()
                    {
                        {
                            "SERVER_JARFILE",
                            "server.jar"
                        }
                    }
                };
                
                var factory = WebApplication.Services.GetRequiredService<ServerFactory>();
                var server = factory.CreateServer(config);

                await using var consoleSub = await server.Console.OnOutput
                    .SubscribeAsync(Console.Write);

                await using var stateSub = await server.OnState.SubscribeAsync(state =>
                {
                    Console.WriteLine($"State: {state}");
                    return ValueTask.CompletedTask;
                });
                
                await server.Initialize();

                Console.ReadLine();

                if(server.StateMachine.State == ServerState.Offline)
                    await server.StateMachine.FireAsync(ServerTrigger.Start);
                else
                    await server.StateMachine.FireAsync(ServerTrigger.Stop);
                
                Console.ReadLine();

                await server.StateMachine.FireAsync(ServerTrigger.Install);
                
                Console.ReadLine();
                
                await server.Context.ServiceScope.DisposeAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        });

        await WebApplication.RunAsync();
    }

    private Task SetupStorage()
    {
        Directory.CreateDirectory("storage");
        Directory.CreateDirectory(Path.Combine("storage", "logs"));

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
            options.Limits.MaxRequestBodySize =
                ByteConverter.FromMegaBytes(Configuration.Kestrel.RequestBodySizeLimit).Bytes;
        });

        return Task.CompletedTask;
    }

    private Task UseBase()
    {
        WebApplication.UseRouting();
        WebApplication.UseExceptionHandler();

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
        var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "storage", "app.json");

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
        LoggerFactory = new LoggerFactory();
        LoggerFactory.AddAnsiConsole();

        Logger = LoggerFactory.CreateLogger<Startup>();

        return Task.CompletedTask;
    }

    private async Task RegisterLogging()
    {
        // Configure application logging
        WebApplicationBuilder.Logging.ClearProviders();
        WebApplicationBuilder.Logging.AddAnsiConsole();
        WebApplicationBuilder.Logging.AddFile(
            Path.Combine(Directory.GetCurrentDirectory(), "storage", "logs", "MoonlightServer.Daemon.log")
        );

        // Logging levels
        var logConfigPath = Path.Combine("storage", "logConfig.json");

        // Ensure logging config, add a default one is missing
        if (!File.Exists(logConfigPath))
        {
            var defaultLogLevels = new Dictionary<string, string>
            {
                { "Default", "Information" },
                { "Microsoft.AspNetCore", "Warning" },
                { "System.Net.Http.HttpClient", "Warning" }
            };

            var json = JsonSerializer.Serialize(defaultLogLevels);
            await File.WriteAllTextAsync(logConfigPath, json);
        }

        var logLevels = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await File.ReadAllTextAsync(logConfigPath)
        )!;

        // Configure logging configuration
        foreach (var logLevel in logLevels)
            WebApplicationBuilder.Logging.AddFilter(logLevel.Key, Enum.Parse<LogLevel>(logLevel.Value));
        
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
        WebApplicationBuilder.Services.AddHostedService(sp => sp.GetRequiredService<ServerService>()
        );

        WebApplicationBuilder.Services.AddSingleton<DockerEventService>();
        WebApplicationBuilder.Services.AddHostedService(sp => sp.GetRequiredService<DockerEventService>());

        WebApplicationBuilder.Services.AddSingleton<ServerConfigurationMapper>();

        WebApplicationBuilder.Services.AddSingleton<ServerFactory>();

        // Server scoped stuff
        WebApplicationBuilder.Services.AddScoped<IConsole, DockerConsole>();
        WebApplicationBuilder.Services.AddScoped<IFileSystem, RawFileSystem>();
        WebApplicationBuilder.Services.AddScoped<IRestorer, DefaultRestorer>();
        WebApplicationBuilder.Services.AddScoped<IInstaller, DockerInstaller>();
        WebApplicationBuilder.Services.AddScoped<IProvisioner, DockerProvisioner>();
        WebApplicationBuilder.Services.AddScoped<IStatistics, DockerStatistics>();
        WebApplicationBuilder.Services.AddScoped<ServerContext>();
        
        WebApplicationBuilder.Services.AddScoped<ServerSys.Abstractions.Server>();

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
        WebApplication.MapHub<ServerWebSocketHub>("api/servers/ws", options =>
        {
            options.AllowStatefulReconnects = false;
            options.CloseOnAuthenticationExpiration = true;
        });

        return Task.CompletedTask;
    }

    #endregion

    #region Cors

    private Task RegisterCors()
    {
        //TODO: IMPORTANT: CHANGE !!!
        WebApplicationBuilder.Services.AddCors(x =>
            x.AddDefaultPolicy(builder => builder
                .SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
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
            .AddAuthentication("token")
            .AddScheme<TokenAuthOptions, TokenAuthScheme>("token",
                options => { options.Token = Configuration.Security.Token; })
            .AddJwtBearer("accessToken", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ClockSkew = TimeSpan.Zero,
                    ValidateAudience = true,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateActor = false,
                    IssuerSigningKeys =
                    [
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(Configuration.Security.Token)
                        )
                    ],
                    AudienceValidator = (audiences, _, _)
                        => audiences.Contains(Configuration.Security.TokenId)
                };

                // Some requests like the signal r websockets or the upload and download endpoints require the user to provide
                // the access token via the query instead of the header field due to client limitations. To reuse the authentication
                // on all these requests as we do for all other endpoints which require an access token, we are defining
                // the custom behaviour to load the access token from the query for these specific endpoints below
                options.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        if (
                            !context.HttpContext.Request.Path.StartsWithSegments("/api/servers/ws") &&
                            !context.HttpContext.Request.Path.StartsWithSegments("/api/servers/upload") &&
                            !context.HttpContext.Request.Path.StartsWithSegments("/api/servers/download")
                        )
                        {
                            return Task.CompletedTask;
                        }

                        var accessToken = context.Request.Query["access_token"];

                        if (string.IsNullOrEmpty(accessToken))
                            return Task.CompletedTask;

                        context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        WebApplicationBuilder.Services.AddAuthorization(options =>
        {
            // We are defining the access token policies here. Because the same jwt secret is used by the panel
            // to generate jwt access tokens for all sorts of daemon related stuff we need to separate
            // the type of access token using the type parameter provided in the claims.

            options.AddPolicy("serverWebsocket", builder => { builder.RequireClaim("type", "websocket"); });

            options.AddPolicy("serverUpload", builder => { builder.RequireClaim("type", "upload"); });

            options.AddPolicy("serverDownload", builder => { builder.RequireClaim("type", "download"); });
        });

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