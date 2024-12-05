using MoonCore.Attributes;

namespace MoonlightServers.ApiServer.Services;

[Singleton]
public class ExampleService
{
    private readonly Random Random;
    private readonly ILogger<ExampleService> Logger;

    public ExampleService(ILogger<ExampleService> logger)
    {
        Logger = logger;
        Random = new();
    }

    public async Task<int> GetValue()
    {
        Logger.LogInformation("Generating value");
        return Random.Next(0, 10324);
    }
}