namespace MoonlightServers.Daemon.Configuration;

public class AppConfiguration
{
    public DockerData Docker { get; set; } = new();
    public StorageData Storage { get; set; } = new();
    public SecurityData Security { get; set; } = new();
    public RemoteData Remote { get; set; } = new();
    
    public class RemoteData
    {
        public string Url { get; set; }
    }
    
    public class DockerData
    {
        public string Uri { get; set; } = "unix:///var/run/docker.sock";
    }
    
    public class SecurityData
    {
        public string Token { get; set; }
    }
    
    public class StorageData
    {
        public string Volumes { get; set; }
        public string VirtualDisks { get; set; }
        public string Backups { get; set; }
        public string Install { get; set; }
    }
}