using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGcLogAndQuarantineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmQuarentena",
                table: "Aprendizados",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "QuarentenaDesde",
                table: "Aprendizados",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GcExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataExecucao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ItensInativados = table.Column<int>(type: "int", nullable: false),
                    ItensEmQuarentena = table.Column<int>(type: "int", nullable: false),
                    ItensRessuscitados = table.Column<int>(type: "int", nullable: false),
                    DuracaoMs = table.Column<long>(type: "bigint", nullable: false),
                    LogDetalhado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GcExecutionLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GcExecutionLogs");

            migrationBuilder.DropColumn(
                name: "EmQuarentena",
                table: "Aprendizados");

            migrationBuilder.DropColumn(
                name: "QuarentenaDesde",
                table: "Aprendizados");
        }
    }
}
