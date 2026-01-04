using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAprendizadoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aprendizados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resposta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aprendizados", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aprendizados");
        }
    }
}
