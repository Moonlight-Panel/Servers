using MoonCore.Helpers;

namespace MoonlightServers.Daemon.Configuration;

public class AppConfiguration
{
    public DockerData Docker { get; set; } = new();
    public StorageData Storage { get; set; } = new();
    public SecurityData Security { get; set; } = new();
    public RemoteData Remote { get; set; } = new();
    public FilesData Files { get; set; } = new();
    public ServerData Server { get; set; } = new();
    public KestrelData Kestrel { get; set; } = new();
    
    public class KestrelData
    {
        public int RequestBodySizeLimit { get; set; } = 25;
    }
    
    public class RemoteData
    {
        public string Url { get; set; }
    }
    
    public class DockerData
    {
        public string Uri { get; set; } = "unix:///var/run/docker.sock";
        public DockerCredentialData[] Credentials { get; set; } = [];
        
        public class DockerCredentialData
        {
            public string Domain { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
        }
    }
    
    public class SecurityData
    {
        public string Token { get; set; }
        public string TokenId { get; set; }
    }
    
    public class StorageData
    {
        public string Volumes { get; set; } = Path.Combine("storage", "volumes");
        public string VirtualDisks { get; set; } = Path.Combine("storage", "virtualDisks");
        public string Backups { get; set; } = Path.Combine("storage", "backups");
        public string Install { get; set; } =Path.Combine("storage", "install");

        public VirtualDiskData VirtualDiskOptions { get; set; } = new();
    }

    public record VirtualDiskData
    {
        public string FileSystemType { get; set; } = "ext4";
        public string E2FsckParameters { get; set; } = "-pf";
    }
    
    public class FilesData
    {
        public int UploadSizeLimit { get; set; } = 1024 * 2;
        public int UploadChunkSize { get; set; } = 20;
    }
    
    public class ServerData
    {
        public int WaitBeforeKillSeconds { get; set; } = 30;
        public int TmpFsSize { get; set; } = 100;
        public float MemoryOverheadMultiplier { get; set; } = 0.05f;
        public int ConsoleMessageCacheLimit { get; set; } = 250;
    }
}