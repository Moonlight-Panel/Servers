using System.Diagnostics;
using MoonCore.Attributes;

namespace MoonlightServers.Daemon.Helpers;

[Singleton]
public class OwnProcessHelper
{
    public long GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var bytes = process.PrivateMemorySize64;

        return bytes;
    }
    
    public TimeSpan GetUptime()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;

        return uptime;
    }

    public int CpuUsage()
    {
        var process = Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime;
        var wallClockTime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

        var cpuUsage = (int)(100.0 * cpuTime.TotalMilliseconds / wallClockTime.TotalMilliseconds /
                             Environment.ProcessorCount);

        return cpuUsage;
    }
}