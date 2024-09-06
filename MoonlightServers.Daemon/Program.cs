using MoonCore.Extensions;
using MoonCore.Helpers;

var loggerFactory = new LoggerFactory();

var loggerProviders = LoggerBuildHelper.BuildFromConfiguration(configuration =>
{
    configuration.Console.Enable = true;
    configuration.Console.EnableAnsiMode = true;

    configuration.FileLogging.Enable = false;
});

loggerFactory.AddProviders(loggerProviders);

var logger = loggerFactory.CreateLogger("Startup");

logger.LogInformation("Starting MoonlightServers Daemon v2.1 Galaxy"); //TODO: Versions

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProviders(loggerProviders);

builder.Services.AutoAddServices<Program>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();