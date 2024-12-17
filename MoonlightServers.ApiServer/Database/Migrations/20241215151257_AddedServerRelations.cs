using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoonlightServers.ApiServer.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedServerRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServerId",
                schema: "Servers",
                table: "ServerBackups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerBackups_ServerId",
                schema: "Servers",
                table: "ServerBackups",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServerBackups_Servers_ServerId",
                schema: "Servers",
                table: "ServerBackups",
                column: "ServerId",
                principalSchema: "Servers",
                principalTable: "Servers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerBackups_Servers_ServerId",
                schema: "Servers",
                table: "ServerBackups");

            migrationBuilder.DropIndex(
                name: "IX_ServerBackups_ServerId",
                schema: "Servers",
                table: "ServerBackups");

            migrationBuilder.DropColumn(
                name: "ServerId",
                schema: "Servers",
                table: "ServerBackups");
        }
    }
}
