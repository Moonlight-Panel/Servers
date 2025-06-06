using System.ComponentModel.DataAnnotations.Schema;
using MoonlightServers.ApiServer.Models;

namespace MoonlightServers.ApiServer.Database.Entities;

public class ServerShare
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public Server Server { get; set; }

    public ServerShareContent Content { get; set; } = new();

    [Column(TypeName="timestamp with time zone")]
    public DateTime CreatedAt { get; set; }
    
    [Column(TypeName="timestamp with time zone")]
    public DateTime UpdatedAt { get; set; }
}