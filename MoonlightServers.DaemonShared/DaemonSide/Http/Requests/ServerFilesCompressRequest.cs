using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.DaemonShared.DaemonSide.Http.Requests;

public class ServerFilesCompressRequest
{
    public string Destination { get; set; }
    public string[] Items { get; set; }
    public CompressType Type { get; set; }
}