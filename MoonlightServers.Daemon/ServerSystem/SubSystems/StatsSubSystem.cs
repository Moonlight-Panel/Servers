using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.DaemonShared.DaemonSide.Models;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class StatsSubSystem : ServerSubSystem
{
    private readonly DockerClient DockerClient;
    private readonly IHubContext<ServerWebSocketHub> HubContext;

    public StatsSubSystem(
        Server server,
        ILogger logger,
        DockerClient dockerClient,
        IHubContext<ServerWebSocketHub> hubContext
    ) : base(server, logger)
    {
        DockerClient = dockerClient;
        HubContext = hubContext;
    }

    public Task Attach(string containerId)
    {
        Logger.LogDebug("Attaching to stats stream");
        
        Task.Run(async () =>
        {
            while (!Server.TaskCancellation.IsCancellationRequested)
            {
                try
                {
                    await DockerClient.Containers.GetContainerStatsAsync(
                        containerId,
                        new()
                        {
                            Stream = true
                        },
                        new Progress<ContainerStatsResponse>(async response =>
                        {
                            try
                            {
                                var stats = ConvertToStats(response);

                                await HubContext.Clients
                                    .Group(Configuration.Id.ToString())
                                    .SendAsync("StatsUpdated", stats);
                            }
                            catch (Exception e)
                            {
                                Logger.LogError("An error occured handling stats update: {e}", e);
                            }
                        }),
                        Server.TaskCancellation
                    );
                }
                catch (TaskCanceledException)
                {
                    // Ignored
                }
                catch (Exception e)
                {
                    Logger.LogError("An error occured while loading container stats: {e}", e);
                }
            }

            Logger.LogDebug("Stopped fetching container stats");
        });

        return Task.CompletedTask;
    }

    private ServerStats ConvertToStats(ContainerStatsResponse response)
    {
        var result = new ServerStats();
        
        // When killed this field will be null so we just return
        if (response.CPUStats.CPUUsage == null)
            return result;
        
        #region CPU

        if(response.CPUStats is { CPUUsage.PercpuUsage: not null }) // Sometimes some values are just null >:/
        {
            var cpuDelta = (float)response.CPUStats.CPUUsage.TotalUsage - response.PreCPUStats.CPUUsage.TotalUsage;
            var cpuSystemDelta = (float)response.CPUStats.SystemUsage - response.PreCPUStats.SystemUsage;

            var cpuCoreCount = (int)response.CPUStats.OnlineCPUs;

            if (cpuCoreCount == 0)
                cpuCoreCount = response.CPUStats.CPUUsage.PercpuUsage.Count;

            var cpuPercent = 0f;
        
            if (cpuSystemDelta > 0.0f && cpuDelta > 0.0f)
            {
                cpuPercent = (cpuDelta / cpuSystemDelta) * 100;

                if (cpuCoreCount > 0)
                    cpuPercent *= cpuCoreCount;
            }

            result.CpuUsage = Math.Round(cpuPercent * 1000) / 1000;
        }
        
        #endregion

        #region Memory

        result.MemoryUsage = response.MemoryStats.Usage;

        #endregion

        #region Network

        foreach (var network in response.Networks)
        {
            result.NetworkRead += network.Value.RxBytes;
            result.NetworkWrite += network.Value.TxBytes;
        }

        #endregion

        #region IO

        if (response.BlkioStats.IoServiceBytesRecursive != null)
        {
            result.IoRead = response.BlkioStats.IoServiceBytesRecursive
                .FirstOrDefault(x => x.Op == "read")?.Value ?? 0;
        
            result.IoWrite = response.BlkioStats.IoServiceBytesRecursive
                .FirstOrDefault(x => x.Op == "write")?.Value ?? 0;
        }

        #endregion

        return result;
    }
}