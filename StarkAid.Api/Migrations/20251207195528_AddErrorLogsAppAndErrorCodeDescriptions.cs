using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLogsAppAndErrorCodeDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorCodeDescriptions",
                columns: table => new
                {
                    CodigoDeErro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contexto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CamposRelevantes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorCodeDescriptions", x => x.CodigoDeErro);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogsApp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UltimoComando = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaResposta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoDispositivoAcionado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErroCompleto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoDeErro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataErro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HoraErro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AcaoErro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogsApp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorLogsApp_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogsApp_UserId",
                table: "ErrorLogsApp",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorCodeDescriptions");

            migrationBuilder.DropTable(
                name: "ErrorLogsApp");
        }
    }
}
