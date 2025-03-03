using MoonCore.Helpers;

namespace MoonlightServers.Daemon.Configuration;

public class AppConfiguration
{
    public DockerData Docker { get; set; } = new();
    public StorageData Storage { get; set; } = new();
    public SecurityData Security { get; set; } = new();
    public RemoteData Remote { get; set; } = new();
    public FilesData Files { get; set; } = new();
    
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
        public string TokenId { get; set; }
    }
    
    public class StorageData
    {
        public string Volumes { get; set; } = PathBuilder.Dir("volumes");
        public string VirtualDisks { get; set; } = PathBuilder.Dir("virtualDisks");
        public string Backups { get; set; } = PathBuilder.Dir("backups");
        public string Install { get; set; } = PathBuilder.Dir("install");
    }
    
    public class FilesData
    {
        public int UploadLimit { get; set; } = 500;
    }
}