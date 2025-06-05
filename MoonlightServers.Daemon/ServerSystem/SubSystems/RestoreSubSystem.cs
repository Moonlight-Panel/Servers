using Docker.DotNet;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class RestoreSubSystem : ServerSubSystem
{
    private readonly DockerClient DockerClient;
    
    public RestoreSubSystem(Server server, ILogger logger, DockerClient dockerClient) : base(server, logger)
    {
        DockerClient = dockerClient;
    }

    public override async Task Initialize()
    {
        Logger.LogDebug("Searching for restorable container");
        
        // Handle possible runtime container
        
        var runtimeContainerName = $"moonlight-runtime-{Configuration.Id}";

        try
        {
            var runtimeContainer = await DockerClient.Containers.InspectContainerAsync(runtimeContainerName);

            if (runtimeContainer.State.Running)
            {
                var provisionSubSystem = Server.GetRequiredSubSystem<ProvisionSubSystem>();
                
                // Override values
                provisionSubSystem.CurrentContainerId = runtimeContainer.ID;
                Server.OverrideState(ServerState.Online);

                // Update and attach console
                
                var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

                var logStream = await DockerClient.Containers.GetContainerLogsAsync(runtimeContainerName, true, new ()
                {
                    Follow = false,
                    ShowStderr = true,
                    ShowStdout = true
                });

                var (standardOutput, standardError) = await logStream.ReadOutputToEndAsync(CancellationToken.None);

                // We split up the read output data into their lines to prevent overloading
                // the console by one large string
                
                foreach (var line in standardOutput.Split("\n"))
                    await consoleSubSystem.WriteOutput(line + "\n");
                
                foreach (var line in standardError.Split("\n"))
                    await consoleSubSystem.WriteOutput(line + "\n");

                await consoleSubSystem.Attach(provisionSubSystem.CurrentContainerId);
                
                // Attach stats
                var statsSubSystem = Server.GetRequiredSubSystem<StatsSubSystem>();
                await statsSubSystem.Attach(provisionSubSystem.CurrentContainerId);
                
                // Done :>
                Logger.LogInformation("Restored runtime container successfully");
                return;
            }
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }
        
        // Handle possible installation container
        
        var installContainerName = $"moonlight-install-{Configuration.Id}";

        try
        {
            var installContainer = await DockerClient.Containers.InspectContainerAsync(installContainerName);

            if (installContainer.State.Running)
            {
                var installationSubSystem = Server.GetRequiredSubSystem<InstallationSubSystem>();
                
                // Override values
                installationSubSystem.CurrentContainerId = installContainer.ID;
                Server.OverrideState(ServerState.Installing);

                var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

                var logStream = await DockerClient.Containers.GetContainerLogsAsync(installContainerName, true, new ()
                {
                    Follow = false,
                    ShowStderr = true,
                    ShowStdout = true
                });

                var (standardOutput, standardError) = await logStream.ReadOutputToEndAsync(CancellationToken.None);

                // We split up the read output data into their lines to prevent overloading
                // the console by one large string
                
                foreach (var line in standardOutput.Split("\n"))
                    await consoleSubSystem.WriteOutput(line + "\n");
                
                foreach (var line in standardError.Split("\n"))
                    await consoleSubSystem.WriteOutput(line + "\n");

                await consoleSubSystem.Attach(installationSubSystem.CurrentContainerId);
                return;
            }
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }
    }
}