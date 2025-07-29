using System.Reactive.Linq;
using System.Reactive.Subjects;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.ServerSystem;
using Stateless;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public class Server : IAsyncDisposable
{
    public IConsole Console { get; private set; }
    public IFileSystem FileSystem { get; private set; }
    public IInstaller Installer { get; private set; }
    public IProvisioner Provisioner { get; private set; }
    public IRestorer Restorer { get; private set; }
    public IStatistics Statistics { get; private set; }
    public StateMachine<ServerState, ServerTrigger> StateMachine { get; private set; }
    public ServerContext Context { get; private set; }
    public IAsyncObservable<ServerState> OnState => OnStateSubject.ToAsyncObservable();

    private readonly Subject<ServerState> OnStateSubject = new();
    private readonly ILogger<Server> Logger;
    
    private IAsyncDisposable? ProvisionExitSubscription;
    private IAsyncDisposable? InstallerExitSubscription;

    public Server(
        ILogger<Server> logger,
        IConsole console,
        IFileSystem fileSystem,
        IInstaller installer,
        IProvisioner provisioner,
        IRestorer restorer,
        IStatistics statistics,
        ServerContext context
    )
    {
        Logger = logger;
        Console = console;
        FileSystem = fileSystem;
        Installer = installer;
        Provisioner = provisioner;
        Restorer = restorer;
        Statistics = statistics;
        Context = context;
    }

    public async Task Initialize()
    {
        Logger.LogDebug("Initializing server components");

        IServerComponent[] components = [Console, Restorer, FileSystem, Installer, Provisioner, Statistics];

        foreach (var serverComponent in components)
        {
            try
            {
                await serverComponent.Initialize();
            }
            catch (Exception e)
            {
                Logger.LogError(
                    e,
                    "Error initializing server component: {type}",
                    serverComponent.GetType().Name.GetType().FullName
                );

                throw;
            }
        }

        Logger.LogDebug("Restoring server");
        var restoredState = await Restorer.Restore();

        if (restoredState == ServerState.Offline)
            Logger.LogDebug("Restorer didnt find anything to restore. State is offline");
        else
            Logger.LogDebug("Restored server to state: {state}", restoredState);

        CreateStateMachine(restoredState);

        // Setup event handling
        ProvisionExitSubscription = await Provisioner.OnExited.SubscribeAsync(async o =>
        {
            await StateMachine.FireAsync(ServerTrigger.Exited);
        });

        InstallerExitSubscription = await Installer.OnExited.SubscribeAsync(async o =>
        {
            await StateMachine.FireAsync(ServerTrigger.Exited);
        });
    }

    private void CreateStateMachine(ServerState initialState)
    {
        StateMachine = new StateMachine<ServerState, ServerTrigger>(initialState, FiringMode.Queued);
        
        StateMachine.OnTransitioned(transition => OnStateSubject.OnNext(transition.Destination));

        // Configure basic state machine flow

        StateMachine.Configure(ServerState.Offline)
            .Permit(ServerTrigger.Start, ServerState.Starting)
            .Permit(ServerTrigger.Install, ServerState.Installing)
            .PermitReentry(ServerTrigger.FailSafe);

        StateMachine.Configure(ServerState.Starting)
            .Permit(ServerTrigger.OnlineDetected, ServerState.Online)
            .Permit(ServerTrigger.FailSafe, ServerState.Offline)
            .Permit(ServerTrigger.Exited, ServerState.Offline)
            .Permit(ServerTrigger.Stop, ServerState.Stopping)
            .Permit(ServerTrigger.Kill, ServerState.Stopping);

        StateMachine.Configure(ServerState.Online)
            .Permit(ServerTrigger.Stop, ServerState.Stopping)
            .Permit(ServerTrigger.Kill, ServerState.Stopping)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        StateMachine.Configure(ServerState.Stopping)
            .PermitReentry(ServerTrigger.FailSafe)
            .PermitReentry(ServerTrigger.Kill)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        StateMachine.Configure(ServerState.Installing)
            .Permit(ServerTrigger.FailSafe, ServerState.Offline) // TODO: Add kill
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        // Handle transitions

        StateMachine.Configure(ServerState.Starting)
            .OnEntryAsync(HandleStart)
            .OnExitFromAsync(ServerTrigger.Exited, HandleRuntimeExit);

        StateMachine.Configure(ServerState.Installing)
            .OnEntryAsync(HandleInstall)
            .OnExitFromAsync(ServerTrigger.Exited, HandleInstallExit);
        
        StateMachine.Configure(ServerState.Online)
            .OnExitFromAsync(ServerTrigger.Exited, HandleRuntimeExit);

        StateMachine.Configure(ServerState.Stopping)
            .OnEntryFromAsync(ServerTrigger.Stop, HandleStop)
            .OnExitFromAsync(ServerTrigger.Exited, HandleRuntimeExit);
    }

    #region State machine handlers

    private async Task HandleStart()
    {
        try
        {
            // Plan for starting the server:
            // 1. Fetch latest configuration from panel (maybe: and perform sync)
            // 2. Ensure that the file system exists
            // 3. Mount the file system
            // 4. Provision the container
            // 5. Attach console to container
            // 6. Start the container

            // 1. Fetch latest configuration from panel
            // TODO: Implement

            // 2. Ensure that the file system exists
            if (!FileSystem.Exists)
            {
                await Console.WriteToMoonlight("Creating storage");
                await FileSystem.Create();
            }

            // 3. Mount the file system
            if (!FileSystem.IsMounted)
            {
                await Console.WriteToMoonlight("Mounting storage");
                await FileSystem.Mount();
            }

            // 4. Provision the container
            await Console.WriteToMoonlight("Provisioning runtime");
            await Provisioner.Provision();

            // 5. Attach console to container
            await Console.AttachToRuntime();

            // 6. Start the container
            await Provisioner.Start();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured while starting the server");
        }
    }
    
    private async Task HandleStop()
    {
        await Provisioner.Stop();
    }

    private async Task HandleRuntimeExit()
    {
        Logger.LogDebug("Deprovisioning");
        await Console.WriteToMoonlight("Deprovisioning");
        
        await Provisioner.Deprovision();
    }

    private async Task HandleInstall()
    {
        Logger.LogDebug("Installing");
        
        Logger.LogDebug("Setting up");
        await Console.WriteToMoonlight("Setting up installation");
        
        // TODO: Extract to service

        Context.InstallConfiguration = new()
        {
            Shell = "/bin/ash",
            DockerImage = "ghcr.io/parkervcp/installers:alpine",
            Script =
                "#!/bin/ash\n# Paper Installation Script\n#\n# Server Files: /mnt/server\nPROJECT=paper\n\nif [ -n \"${DL_PATH}\" ]; then\n\techo -e \"Using supplied download url: ${DL_PATH}\"\n\tDOWNLOAD_URL=`eval echo $(echo ${DL_PATH} | sed -e 's/{{/${/g' -e 's/}}/}/g')`\nelse\n\tVER_EXISTS=`curl -s https://api.papermc.io/v2/projects/${PROJECT} | jq -r --arg VERSION $MINECRAFT_VERSION '.versions[] | contains($VERSION)' | grep -m1 true`\n\tLATEST_VERSION=`curl -s https://api.papermc.io/v2/projects/${PROJECT} | jq -r '.versions' | jq -r '.[-1]'`\n\n\tif [ \"${VER_EXISTS}\" == \"true\" ]; then\n\t\techo -e \"Version is valid. Using version ${MINECRAFT_VERSION}\"\n\telse\n\t\techo -e \"Specified version not found. Defaulting to the latest ${PROJECT} version\"\n\t\tMINECRAFT_VERSION=${LATEST_VERSION}\n\tfi\n\n\tBUILD_EXISTS=`curl -s https://api.papermc.io/v2/projects/${PROJECT}/versions/${MINECRAFT_VERSION} | jq -r --arg BUILD ${BUILD_NUMBER} '.builds[] | tostring | contains($BUILD)' | grep -m1 true`\n\tLATEST_BUILD=`curl -s https://api.papermc.io/v2/projects/${PROJECT}/versions/${MINECRAFT_VERSION} | jq -r '.builds' | jq -r '.[-1]'`\n\n\tif [ \"${BUILD_EXISTS}\" == \"true\" ]; then\n\t\techo -e \"Build is valid for version ${MINECRAFT_VERSION}. Using build ${BUILD_NUMBER}\"\n\telse\n\t\techo -e \"Using the latest ${PROJECT} build for version ${MINECRAFT_VERSION}\"\n\t\tBUILD_NUMBER=${LATEST_BUILD}\n\tfi\n\n\tJAR_NAME=${PROJECT}-${MINECRAFT_VERSION}-${BUILD_NUMBER}.jar\n\n\techo \"Version being downloaded\"\n\techo -e \"MC Version: ${MINECRAFT_VERSION}\"\n\techo -e \"Build: ${BUILD_NUMBER}\"\n\techo -e \"JAR Name of Build: ${JAR_NAME}\"\n\tDOWNLOAD_URL=https://api.papermc.io/v2/projects/${PROJECT}/versions/${MINECRAFT_VERSION}/builds/${BUILD_NUMBER}/downloads/${JAR_NAME}\nfi\n\ncd /mnt/server\n\necho -e \"Running curl -o ${SERVER_JARFILE} ${DOWNLOAD_URL}\"\n\nif [ -f ${SERVER_JARFILE} ]; then\n\tmv ${SERVER_JARFILE} ${SERVER_JARFILE}.old\nfi\n\ncurl -o ${SERVER_JARFILE} ${DOWNLOAD_URL}\n\nif [ ! -f server.properties ]; then\n    echo -e \"Downloading MC server.properties\"\n    curl -o server.properties https://raw.githubusercontent.com/parkervcp/eggs/master/minecraft/java/server.properties\nfi"
        };
        
        await Installer.Setup();

        await Console.AttachToInstallation();

        await Installer.Start();
    }
    
    private async Task HandleInstallExit()
    {
        Logger.LogDebug("Installation done");
        await Console.WriteToMoonlight("Cleaning up");
        
        await Installer.Cleanup();
        
        await Console.WriteToMoonlight("Installation completed");
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (ProvisionExitSubscription != null)
            await ProvisionExitSubscription.DisposeAsync();

        if (InstallerExitSubscription != null)
            await InstallerExitSubscription.DisposeAsync();

        await Console.DisposeAsync();
        await FileSystem.DisposeAsync();
        await Installer.DisposeAsync();
        await Provisioner.DisposeAsync();
        await Restorer.DisposeAsync();
        await Statistics.DisposeAsync();
    }
}