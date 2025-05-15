using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.SingleDb;
using Moonlight.ApiServer.Configuration;
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

    public ServersDataContext(AppConfiguration configuration)
    {
        Options = new()
        {
            Host = configuration.Database.Host,
            Port = configuration.Database.Port,
            Username = configuration.Database.Username,
            Password = configuration.Database.Password,
            Database = configuration.Database.Database
        };
    }
}