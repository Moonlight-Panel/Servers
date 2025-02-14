using Docker.DotNet.Models;
using Mono.Unix.Native;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Models.Cache;

namespace MoonlightServers.Daemon.Extensions;

public static class ServerConfigurationExtensions
{
    public static CreateContainerParameters ToRuntimeCreateParameters(this ServerConfiguration configuration,
        string hostPath, string containerName)
    {
        var parameters = configuration.ToSharedCreateParameters();

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

        parameters.Image = configuration.DockerImage;

        #endregion

        #region Environment

        parameters.Env = configuration.ToEnvironmentVariables()
            .Select(x => $"{x.Key}={x.Value}")
            .ToList();

        #endregion

        #region Working Dir

        parameters.WorkingDir = "/home/container";

        #endregion

        #region User

        var userId = Syscall.getuid(); // TODO: Extract to external service?

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
        }

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

            foreach (var allocation in configuration.Allocations)
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

        return parameters;
    }

    public static CreateContainerParameters ToInstallationCreateParameters(
        this ServerConfiguration configuration,
        string runtimeHostPath,
        string installationHostPath,
        string containerName,
        string installDockerImage,
        string installShell
    )
    {
        var parameters = configuration.ToSharedCreateParameters();

        // - Name
        parameters.Name = containerName;
        parameters.Hostname = containerName;
        
        // - Image
        parameters.Image = installDockerImage;
        
        // - Env
        parameters.Env = configuration
            .ToEnvironmentVariables()
            .Select(x => $"{x.Key}={x.Value}")
            .ToList();
        
        // -- Working directory
        parameters.WorkingDir = "/mnt/server";
        
        // - User
        // Note: Some images might not work if we set a user here
        parameters.User = "0:0";

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

        parameters.Cmd = [installShell, "/mnt/install/install.sh"];

        return parameters;
    }

    private static CreateContainerParameters ToSharedCreateParameters(this ServerConfiguration configuration)
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

        parameters.HostConfig.CPUQuota = configuration.Cpu * 1000;
        parameters.HostConfig.CPUPeriod = 100000;
        parameters.HostConfig.CPUShares = 1024;

        #endregion

        #region Memory & Swap

        var memoryLimit = configuration.Memory;

        // The overhead multiplier gives the container a little bit more memory to prevent crashes
        var memoryOverhead = memoryLimit + (memoryLimit * 0.05f); // TODO: Config

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
            { "/tmp", $"rw,exec,nosuid,size=100M" } // TODO: Config
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
        parameters.Labels.Add("ServerId", configuration.Id.ToString());

        #endregion

        return parameters;
    }

    public static Dictionary<string, string> ToEnvironmentVariables(this ServerConfiguration configuration)
    {
        var result = new Dictionary<string, string>
        {
            //TODO: Add timezone, add server ip
            { "STARTUP", configuration.StartupCommand },
            { "SERVER_MEMORY", configuration.Memory.ToString() }
        };

        if (configuration.Allocations.Length > 0)
        {
            var mainAllocation = configuration.Allocations.First();

            result.Add("SERVER_IP", mainAllocation.IpAddress);
            result.Add("SERVER_PORT", mainAllocation.Port.ToString());
        }

        // Handle allocation variables
        var i = 1;
        foreach (var allocation in configuration.Allocations)
        {
            result.Add($"ML_PORT_{i}", allocation.Port.ToString());
            i++;
        }

        // Copy variables as env vars
        foreach (var variable in configuration.Variables)
            result.Add(variable.Key, variable.Value);

        return result;
    }
}