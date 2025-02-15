using System.Text.RegularExpressions;
using Docker.DotNet.Models;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Extensions;
using Stateless;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    // We are expecting a list of running containers, as we don't wont to inspect every possible container just to check if it exists.
    // If none are provided, we skip the checks. Use this overload if you are creating a new server which didn't exist before
    public async Task Initialize(IList<ContainerListResponse>? runningContainers = null)
    {
        if (runningContainers != null)
        {
            var reAttachSuccessful = await ReAttach(runningContainers);
            
            // If we weren't able to reattach with the current running containers, we initialize the
            // state machine as offline
            if(!reAttachSuccessful)
                await InitializeStateMachine(ServerState.Offline);
        }
        else
            await InitializeStateMachine(ServerState.Offline);

        // And at last we initialize all events, so we can react to certain state changes and outputs.
        // We need to do this regardless if the server was reattached or not, as it hasn't been initialized yet
        await InitializeEvents();
    }

    private Task InitializeStateMachine(ServerState initialState)
    {
        StateMachine = new StateMachine<ServerState, ServerTrigger>(initialState);

        // Setup transitions
        StateMachine.Configure(ServerState.Offline)
            .Permit(ServerTrigger.Start, ServerState.Starting) // Allow to start
            .Permit(ServerTrigger.Reinstall, ServerState.Installing) // Allow to install
            .OnEntryFromAsync(ServerTrigger.NotifyInternalError, InternalError); // Handle unhandled errors

        StateMachine.Configure(ServerState.Starting)
            .Permit(ServerTrigger.Stop, ServerState.Stopping) // Allow stopping
            .Permit(ServerTrigger.NotifyOnline, ServerState.Online) // Allow the server to report as online
            .Permit(ServerTrigger.NotifyRuntimeContainerDied, ServerState.Offline) // Allow server to handle container death
            .Permit(ServerTrigger.NotifyInternalError, ServerState.Offline) // Error handling for the action below
            .OnEntryAsync(InternalStart) // Perform start action
            .OnExitFromAsync(ServerTrigger.NotifyRuntimeContainerDied, InternalCrash); // Define a runtime container death as a crash

        StateMachine.Configure(ServerState.Online)
            .Permit(ServerTrigger.Stop, ServerState.Stopping) // Allow stopping
            .Permit(ServerTrigger.NotifyRuntimeContainerDied, ServerState.Offline) // Allow server to handle container death
            .OnExitFromAsync(ServerTrigger.NotifyRuntimeContainerDied, InternalCrash); // Define a runtime container death as a crash

        StateMachine.Configure(ServerState.Stopping)
            .PermitReentry(ServerTrigger.Kill) // Allow killing, will return to stopping to trigger kill and handle the death correctly
            .Permit(ServerTrigger.NotifyRuntimeContainerDied, ServerState.Offline) // Allow server to handle container death
            .Permit(ServerTrigger.NotifyInternalError, ServerState.Offline) // Error handling for the actions below
            .OnEntryFromAsync(ServerTrigger.Stop, InternalStop) // Perform stop action
            .OnEntryFromAsync(ServerTrigger.Kill, InternalKill) // Perform kill action
            .OnExitFromAsync(ServerTrigger.NotifyRuntimeContainerDied, InternalFinishStop); // Define a runtime container death as a successful stop

        StateMachine.Configure(ServerState.Installing)
            .Permit(ServerTrigger.NotifyInstallationContainerDied, ServerState.Offline) // Allow server to handle container death
            .Permit(ServerTrigger.NotifyInternalError, ServerState.Offline) // Error handling for the action below
            .OnEntryAsync(InternalInstall) // Perform install action
            .OnExitFromAsync(ServerTrigger.NotifyInstallationContainerDied, InternalFinishInstall); // Define the death of the installation container as successful

        return Task.CompletedTask;
    }

    private Task InitializeEvents()
    {
        Console.OnOutput += async content =>
        {
            if (StateMachine.State == ServerState.Starting)
            {
                if (Regex.Matches(content, Configuration.OnlineDetection).Count > 0)
                    await StateMachine.FireAsync(ServerTrigger.NotifyOnline);
            }
        };

        StateMachine.OnTransitioned(transition =>
        {
            Logger.LogInformation(
                "{source} => {destination} ({trigger})",
                transition.Source,
                transition.Destination,
                transition.Trigger
            );
        });

        StateMachine.OnTransitionCompleted(transition =>
        {
            Logger.LogInformation("State: {state}", transition.Destination);
        });

        // Proxy the events so outside subscribes can react to it
        StateMachine.OnTransitionCompletedAsync(async transition =>
        {
            if (OnStateChanged != null)
            {
                await OnStateChanged(transition.Destination);
            }
        });
        
        Console.OnOutput += (async message =>
        {
            if (OnConsoleOutput != null)
            {
                await OnConsoleOutput(message);
            }
        });

        return Task.CompletedTask;
    }

    #region Reattaching & reattach strategies

    private async Task<bool> ReAttach(IList<ContainerListResponse> runningContainers)
    {
        // Docker container names are starting with a / when returned in the docker container list api endpoint,
        // so we trim it from the name when searching

        var existingRuntimeContainer = runningContainers.FirstOrDefault(
            x => x.Names.Any(y => y.TrimStart('/') == RuntimeContainerName)
        );

        if (existingRuntimeContainer != null)
        {
            await ReAttachToRuntime(existingRuntimeContainer);
            return true;
        }

        var existingInstallContainer = runningContainers.FirstOrDefault(
            x => x.Names.Any(y => y.TrimStart('/') == InstallationContainerName)
        );

        if (existingInstallContainer != null)
        {
            await ReAttachToInstallation(existingInstallContainer);
            return true;
        }

        return false;
    }

    private async Task ReAttachToRuntime(ContainerListResponse runtimeContainer)
    {
        if (runtimeContainer.State == "running")
        {
            RuntimeContainerId = runtimeContainer.ID;

            await InitializeStateMachine(ServerState.Online);

            await AttachConsole(runtimeContainer.ID);
        }
        else 
            await InitializeStateMachine(ServerState.Offline);
    }

    private async Task ReAttachToInstallation(ContainerListResponse installationContainer)
    {
        if (installationContainer.State == "running")
        {
            InstallationContainerId = installationContainer.ID;

            await InitializeStateMachine(ServerState.Installing);

            await AttachConsole(installationContainer.ID);
        }
        else
            await InitializeStateMachine(ServerState.Offline);
    }

    #endregion
}