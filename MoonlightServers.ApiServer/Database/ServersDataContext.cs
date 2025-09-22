using Microsoft.EntityFrameworkCore;
using Moonlight.ApiServer.Configuration;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Models;

namespace MoonlightServers.ApiServer.Database;

public class ServersDataContext : DbContext
{
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<Node> Nodes { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<ServerBackup> ServerBackups { get; set; }
    public DbSet<ServerShare> ServerShares { get; set; }
    public DbSet<ServerVariable> ServerVariables { get; set; }
    public DbSet<Star> Stars { get; set; }
    public DbSet<StarDockerImage> StarDockerImages { get; set; }
    public DbSet<StarVariable> StarVariables { get; set; }

    private readonly AppConfiguration Configuration;
    private readonly string Schema = "servers";

    public ServersDataContext(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if(optionsBuilder.IsConfigured)
            return;
        
        var database = Configuration.Database;
        
        var connectionString = $"Host={database.Host};" +
                               $"Port={database.Port};" +
                               $"Database={database.Database};" +
                               $"Username={database.Username};" +
                               $"Password={database.Password}";

        optionsBuilder.UseNpgsql(connectionString, builder =>
        {
            builder.MigrationsHistoryTable("MigrationsHistory", Schema);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Model.SetDefaultSchema(Schema);
        
        base.OnModelCreating(modelBuilder);

        #region Shares

        modelBuilder.Ignore<ServerShareContent>();
        modelBuilder.Ignore<ServerShareContent.SharePermission>();

        modelBuilder.Entity<ServerShare>(builder =>
        {
            builder.OwnsOne(x => x.Content, navigationBuilder =>
            {
                navigationBuilder.ToJson();
                
                navigationBuilder.OwnsMany(x => x.Permissions);
            });
        });

        #endregion
    }
}