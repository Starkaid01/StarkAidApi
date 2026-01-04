using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAtivoToAprendizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Aprendizados",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Aprendizados");
        }
    }
}
