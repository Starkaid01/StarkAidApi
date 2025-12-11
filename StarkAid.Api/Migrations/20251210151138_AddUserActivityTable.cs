using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UltimoComandoEsp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoEwelink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoStarkSwitch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoSocial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaRespostaSocial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoIA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaRespostaIA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_UserId",
                table: "UserActivities",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivities");
        }
    }
}
