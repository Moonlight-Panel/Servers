using Microsoft.EntityFrameworkCore;
using Moonlight.ApiServer.App.Helpers.Database;
using MoonlightServers.ApiServer.Database.Entities;

namespace MoonlightServers.ApiServer.Database;

public class ServersContext : DatabaseContext
{
    public override string Prefix => "Servers";

    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Backup> Backups { get; set; }
    public DbSet<Network> Networks { get; set; }
    public DbSet<Node> Nodes { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<ServerVariable> ServerVariables { get; set; }
    public DbSet<Star> Stars { get; set; }
    public DbSet<StarDockerImage> StarDockerImages { get; set; }
    public DbSet<StarFolder> StarFolders { get; set; }
    public DbSet<StarVariable> StarVariables { get; set; }
}