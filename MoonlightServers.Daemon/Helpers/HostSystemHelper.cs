using System.Runtime.InteropServices;
using Mono.Unix.Native;
using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Models;

namespace MoonlightServers.Daemon.Helpers;

[Singleton]
public class HostSystemHelper
{
    private readonly ILogger<HostSystemHelper> Logger;

    public HostSystemHelper(ILogger<HostSystemHelper> logger)
    {
        Logger = logger;
    }

    public string GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows platform detected
            var osVersion = Environment.OSVersion.Version;
            return $"Windows {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var releaseRaw = File
                .ReadAllLines("/etc/os-release")
                .FirstOrDefault(x => x.StartsWith("PRETTY_NAME="));

            if (string.IsNullOrEmpty(releaseRaw))
                return "Linux (unknown release)";

            var release = releaseRaw
                .Replace("PRETTY_NAME=", "")
                .Replace("\"", "");

            if (string.IsNullOrEmpty(release))
                return "Linux (unknown release)";

            return release;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS platform detected
            var osVersion = Environment.OSVersion.Version;
            return $"Shitty macOS {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}";
        }

        // Unknown platform
        return "Unknown";
    }

    #region CPU Usage

    public async Task<CpuUsageDetails> GetCpuUsage()
    {
        var result = new CpuUsageDetails();
        var perCoreUsages = new List<double>();

        // Initial read
        var (cpuLastStats, cpuLastSums) = await ReadAllCpuStats();

        await Task.Delay(1000);

        // Second read
        var (cpuNowStats, cpuNowSums) = await ReadAllCpuStats();

        for (var i = 0; i < cpuNowStats.Length; i++)
        {
            var cpuDelta = cpuNowSums[i] - cpuLastSums[i];
            var cpuIdle = cpuNowStats[i][3] - cpuLastStats[i][3];
            var cpuUsed = cpuDelta - cpuIdle;

            var usage = 100.0 * cpuUsed / cpuDelta;

            if (i == 0)
                result.OverallUsage = usage;
            else
                perCoreUsages.Add(usage);
        }

        result.PerCoreUsage = perCoreUsages.ToArray();
        
        // Get model name
        var cpuInfoLines = await File.ReadAllLinesAsync("/proc/cpuinfo");
        var modelLine = cpuInfoLines.FirstOrDefault(x => x.StartsWith("model name"));
        result.Model = modelLine?.Split(":")[1].Trim() ?? "N/A";

        return result;
    }

    private async Task<(long[][] cpuStatsList, long[] cpuSums)> ReadAllCpuStats()
    {
        var lines = await File.ReadAllLinesAsync("/proc/stat");

        lines = lines.Where(line => line.StartsWith("cpu"))
            .TakeWhile(line => line.StartsWith("cpu")) // Ensures only CPU lines are read
            .ToArray();

        var statsList = new List<long[]>();
        var sumList = new List<long>();

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1) // Skip the "cpu" label
                .ToArray();

            var cpuTimes = parts
                .Select(long.Parse)
                .ToArray();

            var sum = cpuTimes.Sum();

            statsList.Add(cpuTimes);
            sumList.Add(sum);
        }

        return (statsList.ToArray(), sumList.ToArray());
    }

    #endregion

    #region Memory

    public async Task ClearCachedMemory()
    {
        await File.WriteAllTextAsync("/proc/sys/vm/drop_caches", "3");
    }

    public async Task<MemoryUsageDetails> GetMemoryUsage()
    {
        var details = new MemoryUsageDetails();

        var lines = await File.ReadAllLinesAsync("/proc/meminfo");

        foreach (var line in lines)
        {
            // We want to ignore all non kilobyte values
            if (!line.Contains("kB"))
                continue;

            // Split the line up so we can extract the id and the value
            // to map it to the model field
            var parts = line.Split(":");

            var id = parts[0];
            var value = parts[1]
                .Replace("kB", "")
                .Trim();

            if (!long.TryParse(value, out var longValue))
                continue;

            var bytes = ByteConverter.FromKiloBytes(longValue).Bytes;

            switch (id)
            {
                case "MemTotal":
                    details.Total = bytes;
                    break;

                case "MemFree":
                    details.Free = bytes;
                    break;

                case "MemAvailable":
                    details.Available = bytes;
                    break;

                case "Cached":
                    details.Cached = bytes;
                    break;

                case "SwapTotal":
                    details.SwapTotal = bytes;
                    break;

                case "SwapFree":
                    details.SwapFree = bytes;
                    break;
            }
        }

        return details;
    }

    #endregion

    #region Disks

    public async Task<DiskUsageDetails[]> GetDiskUsages()
    {
        var details = new List<DiskUsageDetails>();

        // First we need to check which mounts actually exist
        var diskDevices = new Dictionary<string, string>();
        string[] ignoredMounts = ["/boot/efi", "/boot"];

        var mountLines = await File.ReadAllLinesAsync("/proc/mounts");

        foreach (var mountLine in mountLines)
        {
            var parts = mountLine.Split(" ");

            var device = parts[0];
            var mountedAt = parts[1];

            // We only want to handle mounted physical devices
            if (!device.StartsWith("/dev/"))
                continue;

            // Ignore certain mounts which we dont want to show
            if (ignoredMounts.Contains(mountedAt))
                continue;

            diskDevices.Add(device, mountedAt);
        }

        foreach (var diskMount in diskDevices)
        {
            var device = diskMount.Key;
            var mount = diskMount.Value;
            
            var statusCode = Syscall.statvfs(mount, out var statvfs);

            if (statusCode != 0)
            {
                var error = Stdlib.GetLastError();

                Logger.LogError(
                    "An error occured while checking disk stats for mount {mount}: {error}",
                    mount,
                    error
                );
                
                continue;
            }

            // Source: https://man7.org/linux/man-pages/man3/statvfs.3.html
            var detail = new DiskUsageDetails()
            {
                Device = device,
                MountPath = mount,
                DiskTotal = statvfs.f_blocks * statvfs.f_frsize,
                DiskFree = statvfs.f_bfree * statvfs.f_frsize,
                InodesTotal = statvfs.f_files,
                InodesFree = statvfs.f_ffree
            };
            
            details.Add(detail);
        }

        return details.ToArray();
    }

    #endregion
}