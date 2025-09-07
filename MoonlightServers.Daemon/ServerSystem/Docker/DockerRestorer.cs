using Docker.DotNet;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Docker;

public class DockerRestorer : IRestorer
{
    private readonly DockerClient DockerClient;
    private readonly ServerContext Context;

    public DockerRestorer(DockerClient dockerClient, ServerContext context)
    {
        DockerClient = dockerClient;
        Context = context;
    }

    public Task InitializeAsync()
    => Task.CompletedTask;

    public async Task<bool> HandleRuntimeAsync()
    {
        var containerName = string.Format(DockerConstants.RuntimeNameTemplate, Context.Configuration.Id);

        try
        {
            var container = await DockerClient.Containers.InspectContainerAsync(
                containerName
            );

            return container.State.Running;
        }
        catch (DockerContainerNotFoundException)
        {
            return false;
        }
    }

    public async Task<bool> HandleInstallationAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, Context.Configuration.Id);

        try
        {
            var container = await DockerClient.Containers.InspectContainerAsync(
                containerName
            );

            return container.State.Running;
        }
        catch (DockerContainerNotFoundException)
        {
            return false;
        }
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}