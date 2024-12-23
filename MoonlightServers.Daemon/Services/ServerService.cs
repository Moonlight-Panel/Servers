using MoonCore.Attributes;
using MoonCore.Models;
using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerService
{
    private readonly List<ServerData> Servers = new();
    private readonly RemoteService RemoteService;
    private readonly ILogger<ServerService> Logger;

    public ServerService(RemoteService remoteService, ILogger<ServerService> logger)
    {
        RemoteService = remoteService;
        Logger = logger;
    }

    public async Task Initialize() //TODO: Add initialize call from panel
    {
        //TODO: Handle block creating servers while initializing
        Logger.LogInformation("Loading servers from panel");

        var apiClient = await RemoteService.CreateHttpClient();

        var servers = await PagedData<ServerDataResponse>.All(async (page, pageSize) =>
            await apiClient.GetJson<PagedData<ServerDataResponse>>(
                $"api/servers/remote/servers?page={page}&pageSize={pageSize}"
            )
        );
        
        Logger.LogInformation("Initializing {count} servers", servers.Length);
    }

    public Task ImportServer(ServerData serverData)
    {
        lock (Servers)
            Servers.Add(serverData);

        return Task.CompletedTask;
    }
}