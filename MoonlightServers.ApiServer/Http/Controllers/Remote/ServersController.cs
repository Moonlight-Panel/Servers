using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.ApiServer.Http.Controllers.Remote;

[ApiController]
[Route("api/remote/servers")]
[Authorize(AuthenticationSchemes = "serverNodeAuthentication")]
public class ServersController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<Node> NodeRepository;
    private readonly ILogger<ServersController> Logger;

    public ServersController(
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<Node> nodeRepository,
        ILogger<ServersController> logger)
    {
        ServerRepository = serverRepository;
        NodeRepository = nodeRepository;
        Logger = logger;
    }

    [HttpGet]
    public async Task<PagedData<ServerDataResponse>> Get([FromQuery] int page, [FromQuery] int pageSize)
    {
        // Load the node via the token id
        var tokenId = User.Claims.First(x => x.Type == "iss").Value;

        var node = await NodeRepository
            .Get()
            .FirstAsync(x => x.TokenId == tokenId);

        var total = await ServerRepository
            .Get()
            .Where(x => x.Node.Id == node.Id)
            .CountAsync();

        var servers = await ServerRepository
            .Get()
            .Where(x => x.Node.Id == node.Id)
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
            var convertedData = ConvertToServerData(server);

            if (convertedData == null)
                continue;

            serverData.Add(convertedData);
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

    [HttpGet("{id:int}")]
    public async Task<ServerDataResponse> Get([FromRoute] int id)
    {
        // Load the node via the token id
        var tokenId = User.Claims.First(x => x.Type == "iss").Value;

        var node = await NodeRepository
            .Get()
            .FirstAsync(x => x.TokenId == tokenId);

        // Load the server with the star data attached. We filter by the node to ensure the node can only access
        // servers linked to it
        var server = await ServerRepository
            .Get()
            .Where(x => x.Node.Id == node.Id)
            .Include(x => x.Star)
            .ThenInclude(x => x.DockerImages)
            .Include(x => x.Variables)
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var convertedData = ConvertToServerData(server);

        if (convertedData == null)
            throw new HttpApiException("An error occured while creating the server data model", 500);

        return convertedData;
    }

    [HttpGet("{id:int}/install")]
    public async Task<ServerInstallDataResponse> GetInstall([FromRoute] int id)
    {
        // Load the node via the token id
        var tokenId = User.Claims.First(x => x.Type == "iss").Value;

        var node = await NodeRepository
            .Get()
            .FirstAsync(x => x.TokenId == tokenId);

        // Load the server with the star data attached. We filter by the node to ensure the node can only access
        // servers linked to it
        var server = await ServerRepository
            .Get()
            .Where(x => x.Node.Id == node.Id)
            .Include(x => x.Star)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        return new ServerInstallDataResponse()
        {
            Script = server.Star.InstallScript,
            DockerImage = server.Star.InstallDockerImage,
            Shell = server.Star.InstallShell
        };
    }

    private ServerDataResponse? ConvertToServerData(Server server)
    {
        // Find the docker image to use for this server
        StarDockerImage? dockerImage = null;

        // Handle server set image if specified
        if (server.DockerImageIndex != -1)
        {
            dockerImage = server.Star.DockerImages
                .Skip(server.DockerImageIndex)
                .FirstOrDefault();
        }

        // Handle star default image if set 
        if (dockerImage == null && server.Star.DefaultDockerImage != -1)
        {
            dockerImage = server.Star.DockerImages
                .Skip(server.Star.DefaultDockerImage)
                .FirstOrDefault();
        }

        if (dockerImage == null)
            dockerImage = server.Star.DockerImages.LastOrDefault();

        if (dockerImage == null)
        {
            Logger.LogWarning("Unable to map server data for server {id}: No docker image available", server.Id);
            return null;
        }

        // Convert model
        return new ServerDataResponse()
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
        };
    }
}