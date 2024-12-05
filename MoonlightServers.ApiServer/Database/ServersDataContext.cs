using Microsoft.EntityFrameworkCore;
using Moonlight.ApiServer.Configuration;
using Moonlight.ApiServer.Helpers;
using MoonlightServers.ApiServer.Database.Entities;

namespace MoonlightServers.ApiServer.Database;

public class ServersDataContext : DatabaseContext
{
    public override string Prefix { get; } = "Servers";

    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Node> Nodes { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<ServerBackup> ServerBackups { get; set; }
    public DbSet<ServerVariable> ServerVariables { get; set; }
    public DbSet<Star> Stars { get; set; }
    public DbSet<StarDockerImage> StarDockerImages { get; set; }
    public DbSet<StarVariable> StarVariables { get; set; }

    public ServersDataContext(AppConfiguration configuration) : base(configuration)
    {
    }
}