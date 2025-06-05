using System.ComponentModel.DataAnnotations.Schema;

namespace MoonlightServers.ApiServer.Database.Entities;

public class ServerShare
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public Server Server { get; set; }

    [Column(TypeName = "jsonb")]
    public string Permissions { get; set; }

    [Column(TypeName="timestamp with time zone")]
    public DateTime CreatedAt { get; set; }
    
    [Column(TypeName="timestamp with time zone")]
    public DateTime UpdatedAt { get; set; }
}