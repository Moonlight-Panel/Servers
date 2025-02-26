using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoonlightServers.ApiServer.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecreatedMigrationsForPostgresql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servers_Nodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Fqdn = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    HttpPort = table.Column<int>(type: "integer", nullable: false),
                    FtpPort = table.Column<int>(type: "integer", nullable: false),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    EnableTransparentMode = table.Column<bool>(type: "boolean", nullable: false),
                    EnableDynamicFirewall = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_Nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers_Stars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    UpdateUrl = table.Column<string>(type: "text", nullable: true),
                    DonateUrl = table.Column<string>(type: "text", nullable: true),
                    StartupCommand = table.Column<string>(type: "text", nullable: false),
                    StopCommand = table.Column<string>(type: "text", nullable: false),
                    OnlineDetection = table.Column<string>(type: "text", nullable: false),
                    InstallShell = table.Column<string>(type: "text", nullable: false),
                    InstallDockerImage = table.Column<string>(type: "text", nullable: false),
                    InstallScript = table.Column<string>(type: "text", nullable: false),
                    RequiredAllocations = table.Column<int>(type: "integer", nullable: false),
                    AllowDockerImageChange = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultDockerImage = table.Column<int>(type: "integer", nullable: false),
                    ParseConfiguration = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_Stars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers_Servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StarId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    StartupOverride = table.Column<string>(type: "text", nullable: true),
                    DockerImageIndex = table.Column<int>(type: "integer", nullable: false),
                    Cpu = table.Column<int>(type: "integer", nullable: false),
                    Memory = table.Column<int>(type: "integer", nullable: false),
                    Disk = table.Column<int>(type: "integer", nullable: false),
                    UseVirtualDisk = table.Column<bool>(type: "boolean", nullable: false),
                    Bandwidth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_Servers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_Servers_Servers_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Servers_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Servers_Servers_Servers_Stars_StarId",
                        column: x => x.StarId,
                        principalTable: "Servers_Stars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servers_StarDockerImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StarId = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    AutoPulling = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_StarDockerImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_StarDockerImages_Servers_Stars_StarId",
                        column: x => x.StarId,
                        principalTable: "Servers_Stars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servers_StarVariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StarId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    DefaultValue = table.Column<string>(type: "text", nullable: false),
                    AllowViewing = table.Column<bool>(type: "boolean", nullable: false),
                    AllowEditing = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Filter = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_StarVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_StarVariables_Servers_Stars_StarId",
                        column: x => x.StarId,
                        principalTable: "Servers_Stars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servers_Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    ServerId = table.Column<int>(type: "integer", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_Allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_Allocations_Servers_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Servers_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Servers_Allocations_Servers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers_Servers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Servers_ServerBackups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Successful = table.Column<bool>(type: "boolean", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    ServerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_ServerBackups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_ServerBackups_Servers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers_Servers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Servers_ServerVariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers_ServerVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_ServerVariables_Servers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers_Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Servers_Allocations_NodeId",
                table: "Servers_Allocations",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_Allocations_ServerId",
                table: "Servers_Allocations",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_ServerBackups_ServerId",
                table: "Servers_ServerBackups",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_Servers_NodeId",
                table: "Servers_Servers",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_Servers_StarId",
                table: "Servers_Servers",
                column: "StarId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_ServerVariables_ServerId",
                table: "Servers_ServerVariables",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_StarDockerImages_StarId",
                table: "Servers_StarDockerImages",
                column: "StarId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_StarVariables_StarId",
                table: "Servers_StarVariables",
                column: "StarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Servers_Allocations");

            migrationBuilder.DropTable(
                name: "Servers_ServerBackups");

            migrationBuilder.DropTable(
                name: "Servers_ServerVariables");

            migrationBuilder.DropTable(
                name: "Servers_StarDockerImages");

            migrationBuilder.DropTable(
                name: "Servers_StarVariables");

            migrationBuilder.DropTable(
                name: "Servers_Servers");

            migrationBuilder.DropTable(
                name: "Servers_Nodes");

            migrationBuilder.DropTable(
                name: "Servers_Stars");
        }
    }
}
