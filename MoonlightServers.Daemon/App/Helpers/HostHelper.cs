using System.Globalization;
using Mono.Unix.Native;
using MoonCore.Attributes;

namespace MoonlightServers.Daemon.App.Helpers;

[Singleton]
public class HostHelper
{
    public async Task<string> GetCpuModel()
    {
        var lines = await File.ReadAllLinesAsync("/proc/cpuinfo");

        foreach (var line in lines)
        {
            if (line.StartsWith("model name"))
                return line.Split(":")[1].Trim();
        }

        return "Unknown processor";
    }

    public async Task<double[]> GetCpuUsage()
    {
        var linesBefore = await File.ReadAllLinesAsync("/proc/stat");
        await Task.Delay(1000); // Wait for 1 second
        var linesAfter = await File.ReadAllLinesAsync("/proc/stat");

        var cpuDataBefore = linesBefore
            .Where(line => line.StartsWith("cpu"))
            .Select(line => line.Split([" "], StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(long.Parse)
                .ToArray())
            .ToList();

        var cpuDataAfter = linesAfter
            .Where(line => line.StartsWith("cpu"))
            .Select(line => line.Split([" "], StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(long.Parse)
                .ToArray())
            .ToList();

        var numCores = Environment.ProcessorCount;
        var cpuUsagePerCore = new double[numCores];

        for (var i = 0; i < numCores; i++)
        {
            var beforeIdle = cpuDataBefore[i][3];
            var beforeTotal = cpuDataBefore[i].Sum();
            var afterIdle = cpuDataAfter[i][3];
            var afterTotal = cpuDataAfter[i].Sum();

            double idleDelta = afterIdle - beforeIdle;
            double totalDelta = afterTotal - beforeTotal;

            var usage = 100.0 * (1.0 - idleDelta / totalDelta);
            cpuUsagePerCore[i] = usage;
        }

        return cpuUsagePerCore;
    }

    public async Task<TimeSpan> GetUptime()
    {
        var uptimeText = await File.ReadAllTextAsync("/proc/uptime");
        var values = uptimeText.Split(" ");
        var seconds = double.Parse(values[0], CultureInfo.InvariantCulture);

        return TimeSpan.FromSeconds(seconds);
    }

    public async Task<long[]> GetMemoryDetails()
    {
        var result = new long[6];

        var memInfoText = await File.ReadAllLinesAsync("/proc/meminfo");

        foreach (var line in memInfoText)
        {
            if (line.StartsWith("MemTotal:"))
                result[0] = 1024 * long.Parse(line.Replace("MemTotal:", "").Replace("kB", "").Trim());

            if (line.StartsWith("MemFree:"))
                result[1] = 1024 * long.Parse(line.Replace("MemFree:", "").Replace("kB", "").Trim());

            if (line.StartsWith("MemAvailable:"))
                result[2] = 1024 * long.Parse(line.Replace("MemAvailable:", "").Replace("kB", "").Trim());

            if (line.StartsWith("Cached:"))
                result[3] = 1024 * long.Parse(line.Replace("Cached:", "").Replace("kB", "").Trim());

            if (line.StartsWith("SwapTotal:"))
                result[4] = 1024 * long.Parse(line.Replace("SwapTotal:", "").Replace("kB", "").Trim());

            if (line.StartsWith("SwapFree:"))
                result[5] = 1024 * long.Parse(line.Replace("SwapFree:", "").Replace("kB", "").Trim());
        }

        return result;
    }

    public async Task<ulong[]> GetDiskUsage() // 0, Total size - 1, Free size, - 3, Total inodes - 4, free inodes
    {
        var sysCallRes = Syscall.statvfs("/", out var buf);

        if (sysCallRes == -1)
            return [0, 0, 0, 0];

        return [buf.f_blocks * buf.f_frsize, buf.f_bfree * buf.f_frsize, buf.f_files, buf.f_ffree];
    }
}