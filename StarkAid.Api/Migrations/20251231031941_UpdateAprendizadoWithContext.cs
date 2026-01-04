using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAprendizadoWithContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contexto",
                table: "Aprendizados",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Aprendizados",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserConversaContexts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextoAtual = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConversaContexts", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConversaContexts");

            migrationBuilder.DropColumn(
                name: "Contexto",
                table: "Aprendizados");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Aprendizados");
        }
    }
}
