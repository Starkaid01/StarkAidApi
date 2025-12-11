using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLogsFalhasSoftTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogsFalhasSoft",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoFalha = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ComandoTentado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DispositivoNome = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErroDetalhado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsFalhasSoft", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsFalhasSoft_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsFalhasSoft_UserId",
                table: "LogsFalhasSoft",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsFalhasSoft");
        }
    }
}
