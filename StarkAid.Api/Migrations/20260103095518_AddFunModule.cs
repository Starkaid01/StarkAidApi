using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFunModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Piadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Piadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Receitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ingredientes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receitas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFunStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PiadasContadasIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceitaAtualId = table.Column<int>(type: "int", nullable: true),
                    PassoAtual = table.Column<int>(type: "int", nullable: false),
                    IniciouPassoAPasso = table.Column<bool>(type: "bit", nullable: false),
                    ReceitasVistasIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFunStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFunStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceitaPassos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceitaId = table.Column<int>(type: "int", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitaPassos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceitaPassos_Receitas_ReceitaId",
                        column: x => x.ReceitaId,
                        principalTable: "Receitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Piadas",
                columns: new[] { "Id", "Ativa", "Categoria", "Texto" },
                values: new object[,]
                {
                    { 1, true, "Tecnologia", "Por que o computador foi ao médico? Porque estava com vírus." },
                    { 2, true, "Geral", "O que o zero disse para o oito? Que cinto bonito!" },
                    { 3, true, "Escola", "Por que o livro de matemática se suicidou? Porque tinha muitos problemas." },
                    { 4, true, "Geral", "Qual é o cúmulo da força? Dobrar a esquina." },
                    { 5, true, "Tecnologia", "O que uma impressora disse para a outra? Essa folha é sua ou é impressão minha?" },
                    { 6, true, "Natureza", "Por que a plantinha não foi ao médico? Porque só tinha médico de plantão." },
                    { 7, true, "Animais", "O que o pato disse para a pata? Vem Quá!" },
                    { 8, true, "Geral", "Qual o pé que é mais rápido? O pé-ligeiro." },
                    { 9, true, "Natureza", "Por que o pinheiro não se perde na floresta? Porque ele tem uma pinha." },
                    { 10, true, "Comida", "O que o tomate foi fazer no banco? Tirar extrato." },
                    { 11, true, "Tecnologia", "Qual é a tecla preferida do astronauta? A barra de espaço." },
                    { 12, true, "Animais", "Por que o jacaré tirou o filho da escola? Porque ele réptil de ano." },
                    { 13, true, "Comida", "Qual é o rei dos queijos? O Requeijão." },
                    { 14, true, "Geral", "O que é um ponto verde na antártida? Um ping-green." },
                    { 15, true, "Profissões", "Por que o bombeiro não gosta de andar? Porque ele socorre." },
                    { 16, true, "Animais", "Qual é o animal que não vale mais nada? O javali." },
                    { 17, true, "Geral", "O que o pagodeiro foi fazer na igreja? Cantar pá god." },
                    { 18, true, "Geral", "Por que a velhinha não usa relógio? Porque ela é sem hora." },
                    { 19, true, "Herois", "Como o Batman faz para entrar na Bat-caverna? Ele bat-palma." },
                    { 20, true, "Ciencia", "Qual o doce preferido do átomo? Pé-de-moleculas." },
                    { 21, true, "Espaço", "O que a Lua disse ao Sol? Nossa, você é tão grande e não te deixam sair à noite!" },
                    { 22, true, "Ciencia", "Por que as estrelas não fazem miau? Porque Astronomia." },
                    { 23, true, "Comida", "O que a banana suicida falou? Macacos me mordam!" },
                    { 24, true, "Geografia", "Qual o estado que quer ser carro? Sergipe." },
                    { 25, true, "Charada", "O que é, o que é: cai em pé e corre deitado? A chuva." },
                    { 26, true, "Geral", "Em qual cidade o Thor mora? Valhalla? Não, Pousada." },
                    { 27, true, "Ciencia", "Por que o elétron não foi à festa? Porque precisa ser positivo." },
                    { 28, true, "Animais", "O que o advogado do frango foi fazer? Foi soltar a franga." },
                    { 29, true, "Animais", "Qual a diferença entre o gato e a coca-cola? O gato faz miau e a coca-cola faz tshhh." },
                    { 30, true, "Ferramentas", "O que o martelo foi fazer no culto? Pregador." }
                });

            migrationBuilder.InsertData(
                table: "Receitas",
                columns: new[] { "Id", "Categoria", "Ingredientes", "Nome" },
                values: new object[,]
                {
                    { 1, "Doces", "3 cenouras, 4 ovos, 1 xícara de óleo, 2 xícaras de açúcar, 2 xícaras de farinha, 1 colher de fermento.", "Bolo de Cenoura" },
                    { 2, "Salgados", "2 ovos, sal a gosto, queijo, presunto, orégano.", "Omelete Simples" },
                    { 3, "Acompanhamentos", "1 xícara de arroz, 2 xícaras de água, alho, sal, óleo.", "Arroz Branco" },
                    { 4, "Doces", "1 lata de leite condensado, 4 colheres de chocolate em pó, 1 colher de manteiga, granulado.", "Brigadeiro" },
                    { 5, "Bebidas", "3 limões, 1 litro de água, açúcar ou adoçante a gosto, gelo.", "Suco de Limão" }
                });

            migrationBuilder.InsertData(
                table: "ReceitaPassos",
                columns: new[] { "Id", "Descricao", "Ordem", "ReceitaId" },
                values: new object[,]
                {
                    { 1, "Descasque e corte as cenouras em rodelas.", 1, 1 },
                    { 2, "No liquidificador, bata as cenouras, os ovos e o óleo.", 2, 1 },
                    { 3, "Em uma tigela, misture o açúcar, a farinha e o fermento.", 3, 1 },
                    { 4, "Despeje a mistura do liquidificador na tigela e mexa bem.", 4, 1 },
                    { 5, "Unte uma forma e despeje a massa.", 5, 1 },
                    { 6, "Asse em forno pré-aquecido a 180 graus por 40 minutos.", 6, 1 },
                    { 7, "Quebre os ovos em um prato fundo.", 1, 2 },
                    { 8, "Bata os ovos ligeiramente com um garfo.", 2, 2 },
                    { 9, "Tempere com sal e orégano.", 3, 2 },
                    { 10, "Aqueça uma frigideira com um pouco de óleo.", 4, 2 },
                    { 11, "Despeje os ovos e adicione o queijo e presunto.", 5, 2 },
                    { 12, "Dobre ao meio e deixe dourar dos dois lados.", 6, 2 },
                    { 13, "Lave o arroz se desejar.", 1, 3 },
                    { 14, "Aqueça o óleo e refogue o alho picado.", 2, 3 },
                    { 15, "Adicione o arroz e refogue por um minuto.", 3, 3 },
                    { 16, "Adicione a água fervente e o sal.", 4, 3 },
                    { 17, "Cozinhe em fogo baixo com a panela semi-tampada.", 5, 3 },
                    { 18, "Quando a água secar, desligue e deixe descansar.", 6, 3 },
                    { 19, "Em uma panela, coloque o leite condensado.", 1, 4 },
                    { 20, "Adicione o chocolate em pó e a manteiga.", 2, 4 },
                    { 21, "Leve ao fogo baixo, mexendo sempre.", 3, 4 },
                    { 22, "Mexa até desgrudar do fundo da panela.", 4, 4 },
                    { 23, "Despeje em um prato untado e deixe esfriar.", 5, 4 },
                    { 24, "Enrole as bolinhas e passe no granulado.", 6, 4 },
                    { 25, "Lave bem os limões.", 1, 5 },
                    { 26, "Corte os limões ao meio.", 2, 5 },
                    { 27, "Esprema o suco dos limões em uma jarra.", 3, 5 },
                    { 28, "Adicione a água e misture.", 4, 5 },
                    { 29, "Adoce a gosto e mexa bem até dissolver.", 5, 5 },
                    { 30, "Adicione gelo e sirva imediatamente.", 6, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceitaPassos_ReceitaId",
                table: "ReceitaPassos",
                column: "ReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFunStates_UserId",
                table: "UserFunStates",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Piadas");

            migrationBuilder.DropTable(
                name: "ReceitaPassos");

            migrationBuilder.DropTable(
                name: "UserFunStates");

            migrationBuilder.DropTable(
                name: "Receitas");
        }
    }
}
