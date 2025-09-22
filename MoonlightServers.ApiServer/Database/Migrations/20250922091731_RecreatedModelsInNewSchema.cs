using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoonlightServers.ApiServer.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecreatedModelsInNewSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "servers");

            migrationBuilder.CreateTable(
                name: "Nodes",
                schema: "servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Fqdn = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    TokenId = table.Column<string>(type: "text", nullable: false),
                    HttpPort = table.Column<int>(type: "integer", nullable: false),
                    FtpPort = table.Column<int>(type: "integer", nullable: false),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stars",
                schema: "servers",
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
                    table.PrimaryKey("PK_Stars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                schema: "servers",
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
                    Disk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalSchema: "servers",
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Servers_Stars_StarId",
                        column: x => x.StarId,
                        principalSchema: "servers",
                        principalTable: "Stars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StarDockerImages",
                schema: "servers",
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
                    table.PrimaryKey("PK_StarDockerImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarDockerImages_Stars_StarId",
                        column: x => x.StarId,
                        principalSchema: "servers",
                        principalTable: "Stars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StarVariables",
                schema: "servers",
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
                    table.PrimaryKey("PK_StarVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarVariables_Stars_StarId",
                        column: x => x.StarId,
                        principalSchema: "servers",
                        principalTable: "Stars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Allocations",
                schema: "servers",
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
                    table.PrimaryKey("PK_Allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allocations_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalSchema: "servers",
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Allocations_Servers_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "servers",
                        principalTable: "Servers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServerBackups",
                schema: "servers",
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
                    table.PrimaryKey("PK_ServerBackups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerBackups_Servers_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "servers",
                        principalTable: "Servers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServerShares",
                schema: "servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerShares_Servers_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "servers",
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerVariables",
                schema: "servers",
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
                    table.PrimaryKey("PK_ServerVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerVariables_Servers_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "servers",
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_NodeId",
                schema: "servers",
                table: "Allocations",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_ServerId",
                schema: "servers",
                table: "Allocations",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerBackups_ServerId",
                schema: "servers",
                table: "ServerBackups",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_NodeId",
                schema: "servers",
                table: "Servers",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_StarId",
                schema: "servers",
                table: "Servers",
                column: "StarId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerShares_ServerId",
                schema: "servers",
                table: "ServerShares",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerVariables_ServerId",
                schema: "servers",
                table: "ServerVariables",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_StarDockerImages_StarId",
                schema: "servers",
                table: "StarDockerImages",
                column: "StarId");

            migrationBuilder.CreateIndex(
                name: "IX_StarVariables_StarId",
                schema: "servers",
                table: "StarVariables",
                column: "StarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allocations",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "ServerBackups",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "ServerShares",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "ServerVariables",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "StarDockerImages",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "StarVariables",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "Servers",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "Nodes",
                schema: "servers");

            migrationBuilder.DropTable(
                name: "Stars",
                schema: "servers");
        }
    }
}
