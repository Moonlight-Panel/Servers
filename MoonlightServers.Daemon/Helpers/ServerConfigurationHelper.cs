using Docker.DotNet.Models;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Models.Cache;

namespace MoonlightServers.Daemon.Helpers;

public static class ServerConfigurationHelper
{
    public static void ApplyRuntimeOptions(CreateContainerParameters parameters, ServerConfiguration configuration, AppConfiguration appConfiguration)
    {
        ApplySharedOptions(parameters, configuration);
        
        // -- Cap drops
        parameters.HostConfig.CapDrop = new List<string>()
        {
            "setpcap", "mknod", "audit_write", "net_raw", "dac_override",
            "fowner", "fsetid", "net_bind_service", "sys_chroot", "setfcap"
        };

        // -- More security options
        parameters.HostConfig.ReadonlyRootfs = true;
        parameters.HostConfig.SecurityOpt = new List<string>()
        {
            "no-new-privileges"
        };

        // - Name
        var name = $"moonlight-runtime-{configuration.Id}";
        parameters.Name = name;
        parameters.Hostname = name;
        
        // - Image
        parameters.Image = configuration.DockerImage;
        
        // - Env
        parameters.Env = ConstructEnv(configuration)
            .Select(x => $"{x.Key}={x.Value}")
            .ToList();
        
        // -- Working directory
        parameters.WorkingDir = "/home/container";
        
        // - User
        //TODO: use config
        parameters.User = $"998:998";

        // -- Mounts
        parameters.HostConfig.Mounts = new List<Mount>();
        
        parameters.HostConfig.Mounts.Add(new()
        {
            Source = GetRuntimeVolume(configuration, appConfiguration),
            Target = "/home/container",
            ReadOnly = false,
            Type = "bind"
        });
        
        // -- Ports
        //var config = configService.Get();

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
    }

    public static void ApplySharedOptions(CreateContainerParameters parameters, ServerConfiguration configuration)
    {
        // - Input, output & error streams and tty
        parameters.Tty = true;
        parameters.AttachStderr = true;
        parameters.AttachStdin = true;
        parameters.AttachStdout = true;
        parameters.OpenStdin = true;
        
        // - Host config
        parameters.HostConfig = new HostConfig();

        // -- CPU limits
        parameters.HostConfig.CPUQuota = configuration.Cpu * 1000;
        parameters.HostConfig.CPUPeriod = 100000;
        parameters.HostConfig.CPUShares = 1024;

        // -- Memory and swap limits
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
        parameters.HostConfig.MemorySwap = swapLimit == -1 ? swapLimit : ByteConverter.FromMegaBytes(swapLimit, 1000).Bytes;

        // -- Other limits
        parameters.HostConfig.BlkioWeight = 100;
        //container.HostConfig.PidsLimit = configuration.Limits.PidsLimit;
        parameters.HostConfig.OomKillDisable = true; //!configuration.Limits.EnableOomKill;
        
        // -- DNS
        parameters.HostConfig.DNS = /*config.Docker.DnsServers.Any() ? config.Docker.DnsServers :*/ new List<string>()
        {
            "1.1.1.1",
            "9.9.9.9"
        };

        // -- Tmpfs
        parameters.HostConfig.Tmpfs = new Dictionary<string, string>()
        {
            { "/tmp", $"rw,exec,nosuid,size=100M" } // TODO: Config
        };
        
        // -- Logging
        parameters.HostConfig.LogConfig = new()
        {
            Type = "json-file", // We need to use this provider, as the GetLogs endpoint needs it
            Config = new Dictionary<string, string>()
        };
        
        // - Labels
        parameters.Labels = new Dictionary<string, string>();
        
        parameters.Labels.Add("Software", "Moonlight-Panel");
        parameters.Labels.Add("ServerId", configuration.Id.ToString());
    }

    public static Dictionary<string, string> ConstructEnv(ServerConfiguration configuration)
    {
        var result = new Dictionary<string, string>();

        // Default environment variables
        //TODO: Add timezone, add server ip
        result.Add("STARTUP", configuration.StartupCommand);
        result.Add("SERVER_MEMORY", configuration.Memory.ToString());

        if (configuration.Allocations.Length > 0)
        {
            var mainAllocation = configuration.Allocations.First();
            
            result.Add("SERVER_IP", mainAllocation.IpAddress);
            result.Add("SERVER_PORT", mainAllocation.Port.ToString());
        }
        
        // Handle additional allocation variables
        var i = 1;
        foreach (var additionalAllocation in configuration.Allocations)
        {
            result.Add($"ML_PORT_{i}", additionalAllocation.Port.ToString());
            i++;
        }

        // Copy variables as env vars
        foreach (var variable in configuration.Variables)
            result.Add(variable.Key, variable.Value);

        return result;
    }

    public static string GetRuntimeVolume(ServerConfiguration configuration, AppConfiguration appConfiguration)
    {
        var localPath = PathBuilder.Dir(appConfiguration.Storage.Volumes, configuration.Id.ToString());
        var absolutePath = Path.GetFullPath(localPath);

        return absolutePath;
    }
}