using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.ApiServer.Http.Controllers.Remote.Servers;

[ApiController]
[Route("api/servers/remote/servers")]
public class RemoteServersController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly ILogger<RemoteServersController> Logger;

    public RemoteServersController(
        DatabaseRepository<Server> serverRepository,
        ILogger<RemoteServersController> logger
    )
    {
        ServerRepository = serverRepository;
        Logger = logger;
    }

    [HttpGet]
    public async Task<PagedData<ServerDataResponse>> Get([FromQuery] int page, [FromQuery] int pageSize)
    {
        var total = await ServerRepository
            .Get()
            .Where(x => x.Node.Id == 1)
            .CountAsync();

        var servers = await ServerRepository
            .Get()
            .Where(x => x.Node.Id == 1)
            .Include(x => x.Star)
            .ThenInclude(x => x.DockerImages)
            .Include(x => x.Variables)
            .Include(x => x.Allocations)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        var serverData = new List<ServerDataResponse>();

        foreach (var server in servers)
        {
            var dockerImage = server.Star.DockerImages
                .FirstOrDefault(x => x.Id == server.DockerImageIndex);

            if (dockerImage == null)
            {
                dockerImage = server.Star.DockerImages
                    .FirstOrDefault(x => x.Id == server.Star.DefaultDockerImage);
            }

            if (dockerImage == null)
                dockerImage = server.Star.DockerImages.LastOrDefault();

            if (dockerImage == null)
            {
                Logger.LogWarning("Unable to map server data for server {id}: No docker image available", server.Id);
                continue;
            }

            serverData.Add(new ServerDataResponse()
            {
                Id = server.Id,
                StartupCommand = server.StartupOverride ?? server.Star.StartupCommand,
                Allocations = server.Allocations.Select(x => new AllocationDataResponse()
                {
                    IpAddress = x.IpAddress,
                    Port = x.Port
                }).ToArray(),
                Variables = server.Variables.ToDictionary(x => x.Key, x => x.Value),
                Bandwidth = server.Bandwidth,
                Cpu = server.Cpu,
                Disk = server.Disk,
                Memory = server.Memory,
                OnlineDetection = server.Star.OnlineDetection,
                DockerImage = dockerImage.Identifier,
                PullDockerImage = dockerImage.AutoPulling,
                ParseConiguration = server.Star.ParseConfiguration,
                StopCommand = server.Star.StopCommand,
                UseVirtualDisk = server.UseVirtualDisk
            });
        }

        return new PagedData<ServerDataResponse>()
        {
            Items = serverData.ToArray(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = total == 0 ? 0 : total / pageSize
        };
    }
}