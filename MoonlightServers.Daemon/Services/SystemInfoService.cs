using System.Diagnostics;
using System.Runtime.InteropServices;
using Docker.DotNet;
using MoonCore.Attributes;
using MoonlightServers.Daemon.Helpers;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class SystemInfoService
{
    private readonly ILogger<SystemInfoService> Logger;
    private readonly IHost Host;

    public SystemInfoService(
        ILogger<SystemInfoService> logger,
        IHost host
    )
    {
        Logger = logger;
        Host = host;
    }

    public Task<string> GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows platform detected
            var osVersion = Environment.OSVersion.Version;
            return Task.FromResult($"Windows {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var releaseRaw = File
                .ReadAllLines("/etc/os-release")
                .FirstOrDefault(x => x.StartsWith("PRETTY_NAME="));

            if (string.IsNullOrEmpty(releaseRaw))
                return Task.FromResult("Linux (unknown release)");

            var release = releaseRaw
                .Replace("PRETTY_NAME=", "")
                .Replace("\"", "");

            if (string.IsNullOrEmpty(release))
                return Task.FromResult("Linux (unknown release)");

            return Task.FromResult(release);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS platform detected
            var osVersion = Environment.OSVersion.Version;
            return Task.FromResult($"macOS {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}");
        }

        // Unknown platform
        return Task.FromResult("N/A");
    }

    public Task<long> GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var bytes = process.PrivateMemorySize64;
        return Task.FromResult(bytes);
    }

    public Task<TimeSpan> GetUptime()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;
        return Task.FromResult(uptime);
    }

    public Task<int> GetCpuUsage()
    {
        var process = Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime;
        var wallClockTime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

        var cpuUsage = (int)(100.0 * cpuTime.TotalMilliseconds / wallClockTime.TotalMilliseconds /
                             Environment.ProcessorCount);

        return Task.FromResult(cpuUsage);
    }

    public Task Shutdown()
    {
        Logger.LogCritical("Restart of daemon has been requested");

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await Host.StopAsync(CancellationToken.None);
        });

        return Task.CompletedTask;
    }
}