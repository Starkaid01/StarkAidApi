using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSuporteEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResolvendoSuportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResolvendoSuportes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResolvendoSuportes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuporteAcoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resposta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sucesso = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuporteAcoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuporteAcoes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuporteAprendizados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Problema = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Solucoes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContadorSucesso = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuporteAprendizados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuporteConversas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProblemaInicial = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Mensagens = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContadorMensagens = table.Column<int>(type: "int", nullable: false),
                    ChatConcluido = table.Column<bool>(type: "bit", nullable: false),
                    Resolvido = table.Column<bool>(type: "bit", nullable: false),
                    LimiteAtingido = table.Column<bool>(type: "bit", nullable: false),
                    TransferidoParaHumano = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuporteConversas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuporteConversas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuportePerguntasFrequentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pergunta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Resposta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuporteToSoft = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SuporteToApp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequerAcao = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuportePerguntasFrequentes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResolvendoSuportes_UserId",
                table: "ResolvendoSuportes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuporteAcoes_UserId",
                table: "SuporteAcoes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuporteConversas_UserId",
                table: "SuporteConversas",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResolvendoSuportes");

            migrationBuilder.DropTable(
                name: "SuporteAcoes");

            migrationBuilder.DropTable(
                name: "SuporteAprendizados");

            migrationBuilder.DropTable(
                name: "SuporteConversas");

            migrationBuilder.DropTable(
                name: "SuportePerguntasFrequentes");
        }
    }
}
