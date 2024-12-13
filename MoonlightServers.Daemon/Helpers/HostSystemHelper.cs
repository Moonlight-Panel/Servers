using System.Runtime.InteropServices;
using MoonCore.Attributes;

namespace MoonlightServers.Daemon.Helpers;

[Singleton]
public class HostSystemHelper
{
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
    
    
}