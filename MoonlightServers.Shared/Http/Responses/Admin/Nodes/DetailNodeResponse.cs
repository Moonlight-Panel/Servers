namespace MoonlightServers.Shared.Http.Responses.Admin.Nodes;

public class DetailNodeResponse
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string Fqdn { get; set; }
    public int ApiPort { get; set; }
    public bool SslEnabled { get; set; }
}