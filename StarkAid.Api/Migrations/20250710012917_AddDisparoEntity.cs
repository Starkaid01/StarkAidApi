using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDisparoEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataHora",
                table: "Disparos",
                newName: "DisparadoEm");

            migrationBuilder.RenameColumn(
                name: "DataConfirmacao",
                table: "Disparos",
                newName: "ConfirmadoEm");

            migrationBuilder.RenameColumn(
                name: "ConfirmadoPeloUsuario",
                table: "Disparos",
                newName: "Confirmado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisparadoEm",
                table: "Disparos",
                newName: "DataHora");

            migrationBuilder.RenameColumn(
                name: "ConfirmadoEm",
                table: "Disparos",
                newName: "DataConfirmacao");

            migrationBuilder.RenameColumn(
                name: "Confirmado",
                table: "Disparos",
                newName: "ConfirmadoPeloUsuario");
        }
    }
}
