using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSystem.Interfaces;

namespace MoonlightServers.Daemon.ServerSystem.Models;

public class ServerContext
{
    public ServerConfiguration Configuration { get; set; }
    public int Identifier { get; set; }
    public AsyncServiceScope ServiceScope { get; set; }
    public Server Server { get; set; }
    public ILogger Logger { get; set; }
}