using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAprendizadoRespostas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aprendizados_Users_UserId",
                table: "Aprendizados");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Aprendizados",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "AprendizadoRespostas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AprendizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsoCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AprendizadoRespostas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AprendizadoRespostas_Aprendizados_AprendizadoId",
                        column: x => x.AprendizadoId,
                        principalTable: "Aprendizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AprendizadoRespostas_AprendizadoId",
                table: "AprendizadoRespostas",
                column: "AprendizadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aprendizados_Users_UserId",
                table: "Aprendizados",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aprendizados_Users_UserId",
                table: "Aprendizados");

            migrationBuilder.DropTable(
                name: "AprendizadoRespostas");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Aprendizados",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Aprendizados_Users_UserId",
                table: "Aprendizados",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
