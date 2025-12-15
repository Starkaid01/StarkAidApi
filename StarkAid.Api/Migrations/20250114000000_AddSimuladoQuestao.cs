using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSimuladoQuestao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimuladoQuestao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimuladoId = table.Column<int>(type: "int", nullable: false),
                    QuestaoId = table.Column<int>(type: "int", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimuladoQuestao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimuladoQuestao_Questao_QuestaoId",
                        column: x => x.QuestaoId,
                        principalTable: "Questao",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SimuladoQuestao_Simulado_SimuladoId",
                        column: x => x.SimuladoId,
                        principalTable: "Simulado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimuladoQuestao_QuestaoId",
                table: "SimuladoQuestao",
                column: "QuestaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SimuladoQuestao_SimuladoId",
                table: "SimuladoQuestao",
                column: "SimuladoId");

            migrationBuilder.CreateIndex(
                name: "IX_SimuladoQuestao_SimuladoId_Ordem",
                table: "SimuladoQuestao",
                columns: new[] { "SimuladoId", "Ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimuladoQuestao");
        }
    }
}
