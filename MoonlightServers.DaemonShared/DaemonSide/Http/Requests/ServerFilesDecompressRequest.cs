using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.DaemonShared.DaemonSide.Http.Requests;

public class ServerFilesDecompressRequest
{
    public string Destination { get; set; }
    public string Path { get; set; }
    public CompressType Type { get; set; }
}