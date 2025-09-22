using Docker.DotNet.Models;
using Mono.Unix.Native;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Mappers;

public class ServerConfigurationMapper
{
    private readonly AppConfiguration AppConfiguration;

    public ServerConfigurationMapper(AppConfiguration appConfiguration)
    {
        AppConfiguration = appConfiguration;
    }
    
    public ServerConfiguration FromServerDataResponse(ServerDataResponse response)
    {
        return new ServerConfiguration()
        {
            Id = response.Id,
            StartupCommand = response.StartupCommand,
            Allocations = response.Allocations.Select(y => new ServerConfiguration.AllocationConfiguration()
            {
                IpAddress = y.IpAddress,
                Port = y.Port
            }).ToArray(),
            Variables = response.Variables,
            OnlineDetection = response.OnlineDetection,
            DockerImage = response.DockerImage,
            Cpu = response.Cpu,
            Disk = response.Disk,
            Memory = response.Memory,
            StopCommand = response.StopCommand,
        };
    }

    public CreateContainerParameters ToRuntimeParameters(
        ServerConfiguration serverConfiguration,
        string hostPath,
        string containerName
    )
    {
        var parameters = ToSharedParameters(serverConfiguration);

        #region Security

        parameters.HostConfig.CapDrop = new List<string>()
        {
            "setpcap", "mknod", "audit_write", "net_raw", "dac_override",
            "fowner", "fsetid", "net_bind_service", "sys_chroot", "setfcap"
        };

        parameters.HostConfig.ReadonlyRootfs = true;
        parameters.HostConfig.SecurityOpt = new List<string>()
        {
            "no-new-privileges"
        };

        #endregion

        #region Name

        parameters.Name = containerName;
        parameters.Hostname = containerName;

        #endregion

        #region Docker Image

        parameters.Image = serverConfiguration.DockerImage;

        #endregion

        #region Working Dir

        parameters.WorkingDir = "/home/container";

        #endregion

        #region User

        // TODO: Extract this to an external service with config options and return a userspace user id and a install user id
        // in order to know which permissions are required in order to run the container with the correct permissions

        var userId = Syscall.getuid();

        if (userId == 0)
            userId = 998;

        parameters.User = $"{userId}:{userId}";

        /*
        if (userId == 0)
        {
            // We are running as root, so we need to run the container as another user and chown the files when we make changes
            parameters.User = $"998:998";
        }
        else
        {
            // We are not running as root, so we start the container as the same user,
            // as we are not able to chown the container content to a different user
            parameters.User = $"{userId}:{userId}";
        }*/

        #endregion

        #region Mounts

        parameters.HostConfig.Mounts = new List<Mount>();

        parameters.HostConfig.Mounts.Add(new()
        {
            Source = hostPath,
            Target = "/home/container",
            ReadOnly = false,
            Type = "bind"
        });

        #endregion

        #region Port Bindings

        if (true) // TODO: Add network toggle
        {
            parameters.ExposedPorts = new Dictionary<string, EmptyStruct>();
            parameters.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();

            foreach (var allocation in serverConfiguration.Allocations)
            {
                parameters.ExposedPorts.Add($"{allocation.Port}/tcp", new());
                parameters.ExposedPorts.Add($"{allocation.Port}/udp", new());

                parameters.HostConfig.PortBindings.Add($"{allocation.Port}/tcp", new List<PortBinding>
                {
                    new()
                    {
                        HostPort = allocation.Port.ToString(),
                        HostIP = allocation.IpAddress
                    }
                });

                parameters.HostConfig.PortBindings.Add($"{allocation.Port}/udp", new List<PortBinding>
                {
                    new()
                    {
                        HostPort = allocation.Port.ToString(),
                        HostIP = allocation.IpAddress
                    }
                });
            }
        }

        #endregion

        // TODO: Implement a way to directly startup a server without using the entrypoint.sh and parsing the startup command here
        // in the daemon instead of letting it the entrypoint do. iirc pelican wants to do that as well so we need to do that
        // sooner or later in order to stay compatible to pelican
        // Possible flag name: LegacyEntrypointMode

        return parameters;
    }

    public CreateContainerParameters ToInstallParameters(
        ServerConfiguration serverConfiguration,
        ServerInstallDataResponse installData,
        string runtimeHostPath,
        string installationHostPath,
        string containerName
    )
    {
        var parameters = ToSharedParameters(serverConfiguration);

        // - Name
        parameters.Name = containerName;
        parameters.Hostname = containerName;

        // - Image
        parameters.Image = installData.DockerImage;

        // -- Working directory
        parameters.WorkingDir = "/mnt/server";

        // - User
        // Note: Some images might not work if we set a user here

        var userId = Syscall.getuid();

        // If we are root, we are able to change owner permissions after the installation
        // so we run the installation as root, otherwise we need to run it as our current user,
        // so we are able to access the files created by the installer
        if (userId == 0)
            parameters.User = "0:0";
        else
            parameters.User = $"{userId}:{userId}";

        // -- Mounts
        parameters.HostConfig.Mounts = new List<Mount>();

        parameters.HostConfig.Mounts.Add(new()
        {
            Source = runtimeHostPath,
            Target = "/mnt/server",
            ReadOnly = false,
            Type = "bind"
        });

        parameters.HostConfig.Mounts.Add(new()
        {
            Source = installationHostPath,
            Target = "/mnt/install",
            ReadOnly = false,
            Type = "bind"
        });

        parameters.Cmd = [installData.Shell, "/mnt/install/install.sh"];

        return parameters;
    }

    public CreateContainerParameters ToSharedParameters(ServerConfiguration serverConfiguration)
    {
        var parameters = new CreateContainerParameters()
        {
            HostConfig = new()
        };

        #region Input, output & error streams and tty

        parameters.Tty = true;
        parameters.AttachStderr = true;
        parameters.AttachStdin = true;
        parameters.AttachStdout = true;
        parameters.OpenStdin = true;

        #endregion

        #region CPU

        parameters.HostConfig.CPUQuota = serverConfiguration.Cpu * 1000;
        parameters.HostConfig.CPUPeriod = 100000;
        parameters.HostConfig.CPUShares = 1024;

        #endregion

        #region Memory & Swap

        var memoryLimit = serverConfiguration.Memory;

        // The overhead multiplier gives the container a little bit more memory to prevent crashes
        var memoryOverhead = memoryLimit + (memoryLimit * AppConfiguration.Server.MemoryOverheadMultiplier);

        long swapLimit = -1;

        /*

        // If swap is enabled globally and not disabled on this server, set swap
        if (!configuration.Limits.DisableSwap && config.Server.EnableSwap)
            swapLimit = (long)(memoryOverhead + memoryOverhead * config.Server.SwapMultiplier);
            co
            */

        // Finalize limits by converting and updating the host config
        parameters.HostConfig.Memory = ByteConverter.FromMegaBytes((long)memoryOverhead, 1000).Bytes;
        parameters.HostConfig.MemoryReservation = ByteConverter.FromMegaBytes(memoryLimit, 1000).Bytes;
        parameters.HostConfig.MemorySwap =
            swapLimit == -1 ? swapLimit : ByteConverter.FromMegaBytes(swapLimit, 1000).Bytes;

        #endregion

        #region Misc Limits

        // -- Other limits
        parameters.HostConfig.BlkioWeight = 100;
        //container.HostConfig.PidsLimit = configuration.Limits.PidsLimit;
        parameters.HostConfig.OomKillDisable = true; //!configuration.Limits.EnableOomKill;

        #endregion

        #region DNS

        // TODO: Read hosts dns settings?

        parameters.HostConfig.DNS = /*config.Docker.DnsServers.Any() ? config.Docker.DnsServers :*/ new List<string>()
        {
            "1.1.1.1",
            "9.9.9.9"
        };

        #endregion

        #region Tmpfs

        parameters.HostConfig.Tmpfs = new Dictionary<string, string>()
        {
            { "/tmp", $"rw,exec,nosuid,size={AppConfiguration.Server.TmpFsSize}M" }
        };

        #endregion

        #region Logging

        parameters.HostConfig.LogConfig = new()
        {
            Type = "json-file", // We need to use this provider, as the GetLogs endpoint needs it
            Config = new Dictionary<string, string>()
        };

        #endregion

        #region Labels

        parameters.Labels = new Dictionary<string, string>();

        parameters.Labels.Add("Software", "Moonlight-Panel");
        parameters.Labels.Add("ServerId", serverConfiguration.Id.ToString());

        #endregion

        #region Environment

        parameters.Env = CreateEnvironmentVariables(serverConfiguration);

        #endregion

        return parameters;
    }

    private List<string> CreateEnvironmentVariables(ServerConfiguration serverConfiguration)
    {
        var result = new Dictionary<string, string>
        {
            //TODO: Add timezone, add server ip
            { "STARTUP", serverConfiguration.StartupCommand },
            { "SERVER_MEMORY", serverConfiguration.Memory.ToString() }
        };

        if (serverConfiguration.Allocations.Length > 0)
        {
            for (var i = 0; i < serverConfiguration.Allocations.Length; i++)
            {
                var allocation = serverConfiguration.Allocations[i];

                result.Add($"ML_PORT_{i}", allocation.Port.ToString());

                if (i == 0) // TODO: Implement a way to set the default/main allocation
                {
                    result.Add("SERVER_IP", allocation.IpAddress);
                    result.Add("SERVER_PORT", allocation.Port.ToString());
                }
            }
        }

        // Copy variables as env vars
        foreach (var variable in serverConfiguration.Variables)
            result.Add(variable.Key, variable.Value);

        // Convert to the format of the docker library
        return result.Select(variable => $"{variable.Key}={variable.Value}").ToList();
    }
}