using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Database.Entities;

public class Backup
{
    public int Id { get; set; }

    public string Name { get; set; }
    public BackupState State { get; set; }

    public long Size { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;

    public Server Server { get; set; }
}