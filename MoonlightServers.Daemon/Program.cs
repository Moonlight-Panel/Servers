using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonlightServers.Daemon.App.Helpers;

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

var x = new HostHelper();

var data = await x.GetDiskUsage();

logger.LogInformation("Total disk: {value}", Formatter.FormatSize((long)data[0]));
logger.LogInformation("Free disk: {value}", Formatter.FormatSize((long)data[1]));
logger.LogInformation("Total inodes: {value}", data[2]);
logger.LogInformation("Free inodes: {value}", data[3]);

app.Run();