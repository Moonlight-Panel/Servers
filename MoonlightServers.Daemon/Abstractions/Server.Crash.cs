using Docker.DotNet;
using Docker.DotNet.Models;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task InternalCrash()
    {
        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

        ContainerInspectResponse? container;
        
        try
        {
            container = await dockerClient.Containers.InspectContainerAsync(RuntimeContainerId);
        }
        catch (DockerContainerNotFoundException)
        {
            container = null;
        }
        
        if(container == null)
            return;

        var exitCode = container.State.ExitCode;
        
        // TODO: Report to panel
        
        await LogToConsole($"Server crashed. Exit code: {exitCode}");
        
        await Destroy();
    }

    public async Task InternalError()
    {
        await LogToConsole("An unhandled error occured performing action");
        // TODO:
        Logger.LogInformation("Reporting or smth");
    }
}