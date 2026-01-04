using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiInteractionTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiInteractionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TextoOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextoNormalizado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SimilarityScore = table.Column<double>(type: "float", nullable: true),
                    AprendizadoTipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AprendizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LatenciaMs = table.Column<int>(type: "int", nullable: false),
                    ChamouIaExterna = table.Column<bool>(type: "bit", nullable: false),
                    TokensEstimadosEvitados = table.Column<int>(type: "int", nullable: false),
                    EconomiaUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiInteractionEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiInteractionEvents");
        }
    }
}
