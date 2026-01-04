using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantesToAprendizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariantesDistintasUsadas",
                table: "Aprendizados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Telemetrias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Evento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Telemetrias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Telemetrias_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Telemetrias_UserId",
                table: "Telemetrias",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Telemetrias");

            migrationBuilder.DropColumn(
                name: "VariantesDistintasUsadas",
                table: "Aprendizados");
        }
    }
}
