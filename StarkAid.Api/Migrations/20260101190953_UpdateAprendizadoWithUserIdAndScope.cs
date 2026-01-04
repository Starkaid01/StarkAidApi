using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAprendizadoWithUserIdAndScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Aprendizados",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aprendizados_UserId",
                table: "Aprendizados",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aprendizados_Users_UserId",
                table: "Aprendizados",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aprendizados_Users_UserId",
                table: "Aprendizados");

            migrationBuilder.DropIndex(
                name: "IX_Aprendizados_UserId",
                table: "Aprendizados");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Aprendizados");
        }
    }
}
