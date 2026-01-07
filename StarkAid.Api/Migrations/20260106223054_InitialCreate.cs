using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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

            migrationBuilder.CreateTable(
                name: "ConfiguracoesSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DominioCloudflare = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DominioNlp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DominioAudioResolver = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UltimaAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesStarkNlp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StarkNlpUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesStarkNlp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorCodeDescriptions",
                columns: table => new
                {
                    CodigoDeErro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contexto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CamposRelevantes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Solucoes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorCodeDescriptions", x => x.CodigoDeErro);
                });

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

            migrationBuilder.CreateTable(
                name: "MusicArtistAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Canonical = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicArtistAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReferenciaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Lida = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "UserConversaContexts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextoAtual = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConversaContexts", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StarkCoins = table.Column<int>(type: "int", nullable: false),
                    PlanType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TokensConsumidosSemana = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RemovalAds = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreapprovalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimoPagamentoConfirmadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SpotifyAccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SpotifyRefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SpotifyTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MinutosReconhecidos = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    WhatsAppSessionData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataRecebida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JsonDetalhado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YouTubeMusicCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormalizedQuery = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VideoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    IsLive = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubeMusicCaches", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "Aprendizados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resposta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contexto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    EmQuarentena = table.Column<bool>(type: "bit", nullable: false),
                    QuarentenaDesde = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VariantesDistintasUsadas = table.Column<int>(type: "int", nullable: false),
                    UltimaRessurreicaoAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aprendizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aprendizados_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripePriceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoPlano = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IniciadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CanceladaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PagamentoConfirmadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataCriacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComandosSociais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comando = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resposta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespostasAleatorias = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComandosSociais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComandosSociais_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comodos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comodos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comodos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MqttTopic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comando = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsOn = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispositivosDisparo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MqttTopic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatusTopic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispositivosDisparo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispositivosDisparo_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispositivosEsp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Porta = table.Column<int>(type: "int", nullable: false),
                    Comando = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ComandToEsp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LigadoDesligado = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastPingAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispositivosEsp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispositivosEsp_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateTable(
                name: "ErrorLogsSoft",
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
                    table.PrimaryKey("PK_ErrorLogsSoft", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorLogsSoft_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EwelinkAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AccessTokenExpiry = table.Column<long>(type: "bigint", nullable: false),
                    RefreshTokenExpiry = table.Column<long>(type: "bigint", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EwelinkAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EwelinkAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EwelinkDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Uiid = table.Column<int>(type: "int", nullable: false),
                    Params = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Online = table.Column<bool>(type: "bit", nullable: false),
                    FamilyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RoomId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EwelinkDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EwelinkDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FirebaseTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirebaseTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirebaseTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IaHistoricos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextoUsuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextoIa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IaHistoricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IaHistoricos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxMachines = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StripeSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Licenses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogsFalhasSoft",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoFalha = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ComandoTentado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DispositivoNome = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErroDetalhado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsFalhasSoft", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsFalhasSoft_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagamentosAvulsos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StripeSessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PagamentoConfirmadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentosAvulsos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentosAvulsos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expiration = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expiration = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "Rotinas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    CriadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rotinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rotinas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StarkCoinPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageType = table.Column<int>(type: "int", nullable: false),
                    StarkCoinsAmount = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarkCoinPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarkCoinPurchases_Users_UserId",
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
                name: "Telemetrias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Evento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Telemetrias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Telemetrias_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UltimoComandoEsp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoEwelink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoStarkSwitch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoSocial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaRespostaSocial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimoComandoIA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaRespostaIA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "ComodoDispositivos",
                columns: table => new
                {
                    ComodoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispositivoId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComodoDispositivos", x => new { x.ComodoId, x.DispositivoId });
                    table.ForeignKey(
                        name: "FK_ComodoDispositivos_Comodos_ComodoId",
                        column: x => x.ComodoId,
                        principalTable: "Comodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscoposConversacionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComodoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscoposConversacionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscoposConversacionais_Comodos_ComodoId",
                        column: x => x.ComodoId,
                        principalTable: "Comodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscoposConversacionais_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Disparos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispositivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisparadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confirmado = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disparos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disparos_DispositivosDisparo_DispositivoId",
                        column: x => x.DispositivoId,
                        principalTable: "DispositivosDisparo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Disparos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Agendamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DispositivoEspId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EwelinkDeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoAgendamento = table.Column<int>(type: "int", nullable: false),
                    AgendadoPara = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Comando = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Executado = table.Column<bool>(type: "bit", nullable: false),
                    Recorrencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Agendamentos_DispositivosEsp_DispositivoEspId",
                        column: x => x.DispositivoEspId,
                        principalTable: "DispositivosEsp",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LicenseActivations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseActivations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicenseActivations_Licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "Licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RotinaAcoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RotinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemExecucao = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotinaAcoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotinaAcoes_Rotinas_RotinaId",
                        column: x => x.RotinaId,
                        principalTable: "Rotinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RotinaGatilhos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RotinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Expressao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DiasSemana = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotinaGatilhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotinaGatilhos_Rotinas_RotinaId",
                        column: x => x.RotinaId,
                        principalTable: "Rotinas",
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
                name: "IX_Agendamentos_DeviceId",
                table: "Agendamentos",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_DispositivoEspId",
                table: "Agendamentos",
                column: "DispositivoEspId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_UserId",
                table: "Agendamentos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_UserId1",
                table: "Agendamentos",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_AprendizadoRespostas_AprendizadoId",
                table: "AprendizadoRespostas",
                column: "AprendizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Aprendizados_Tipo",
                table: "Aprendizados",
                column: "Tipo",
                filter: "[Tipo] = 'Global'");

            migrationBuilder.CreateIndex(
                name: "IX_Aprendizados_UserId_Tipo",
                table: "Aprendizados",
                columns: new[] { "UserId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_UserId",
                table: "Assinaturas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComandosSociais_UserId",
                table: "ComandosSociais",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comodos_UserId",
                table: "Comodos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Disparos_DispositivoId",
                table: "Disparos",
                column: "DispositivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Disparos_UserId",
                table: "Disparos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispositivosDisparo_UserId",
                table: "DispositivosDisparo",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispositivosEsp_UserId",
                table: "DispositivosEsp",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogsApp_UserId",
                table: "ErrorLogsApp",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogsSoft_UserId",
                table: "ErrorLogsSoft",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscoposConversacionais_ComodoId",
                table: "EscoposConversacionais",
                column: "ComodoId");

            migrationBuilder.CreateIndex(
                name: "IX_EscoposConversacionais_UserId",
                table: "EscoposConversacionais",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EwelinkAccounts_UserId",
                table: "EwelinkAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EwelinkDevices_UserId",
                table: "EwelinkDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseTokens_UserId",
                table: "FirebaseTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IaHistoricos_UserId",
                table: "IaHistoricos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseActivations_LicenseId",
                table: "LicenseActivations",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_UserId",
                table: "Licenses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsFalhasSoft_UserId",
                table: "LogsFalhasSoft",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosAvulsos_UserId",
                table: "PagamentosAvulsos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitaPassos_ReceitaId",
                table: "ReceitaPassos",
                column: "ReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolvendoSuportes_UserId",
                table: "ResolvendoSuportes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RotinaAcoes_RotinaId",
                table: "RotinaAcoes",
                column: "RotinaId");

            migrationBuilder.CreateIndex(
                name: "IX_RotinaGatilhos_RotinaId",
                table: "RotinaGatilhos",
                column: "RotinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rotinas_UserId",
                table: "Rotinas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StarkCoinPurchases_UserId",
                table: "StarkCoinPurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuporteAcoes_UserId",
                table: "SuporteAcoes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuporteConversas_UserId",
                table: "SuporteConversas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Telemetrias_UserId",
                table: "Telemetrias",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_UserId",
                table: "UserActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFunStates_UserId",
                table: "UserFunStates",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeMusicCaches_NormalizedQuery",
                table: "YouTubeMusicCaches",
                column: "NormalizedQuery");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agendamentos");

            migrationBuilder.DropTable(
                name: "AiInteractionEvents");

            migrationBuilder.DropTable(
                name: "AprendizadoRespostas");

            migrationBuilder.DropTable(
                name: "Assinaturas");

            migrationBuilder.DropTable(
                name: "ComandosSociais");

            migrationBuilder.DropTable(
                name: "ComodoDispositivos");

            migrationBuilder.DropTable(
                name: "ConfiguracoesSistema");

            migrationBuilder.DropTable(
                name: "ConfiguracoesStarkNlp");

            migrationBuilder.DropTable(
                name: "Disparos");

            migrationBuilder.DropTable(
                name: "ErrorCodeDescriptions");

            migrationBuilder.DropTable(
                name: "ErrorLogsApp");

            migrationBuilder.DropTable(
                name: "ErrorLogsSoft");

            migrationBuilder.DropTable(
                name: "EscoposConversacionais");

            migrationBuilder.DropTable(
                name: "EwelinkAccounts");

            migrationBuilder.DropTable(
                name: "EwelinkDevices");

            migrationBuilder.DropTable(
                name: "FirebaseTokens");

            migrationBuilder.DropTable(
                name: "GcExecutionLogs");

            migrationBuilder.DropTable(
                name: "IaHistoricos");

            migrationBuilder.DropTable(
                name: "LicenseActivations");

            migrationBuilder.DropTable(
                name: "LogsFalhasSoft");

            migrationBuilder.DropTable(
                name: "MusicArtistAliases");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PagamentosAvulsos");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "Piadas");

            migrationBuilder.DropTable(
                name: "ReceitaPassos");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ResolvendoSuportes");

            migrationBuilder.DropTable(
                name: "RotinaAcoes");

            migrationBuilder.DropTable(
                name: "RotinaGatilhos");

            migrationBuilder.DropTable(
                name: "StarkCoinPurchases");

            migrationBuilder.DropTable(
                name: "SuporteAcoes");

            migrationBuilder.DropTable(
                name: "SuporteAprendizados");

            migrationBuilder.DropTable(
                name: "SuporteConversas");

            migrationBuilder.DropTable(
                name: "SuportePerguntasFrequentes");

            migrationBuilder.DropTable(
                name: "Telemetrias");

            migrationBuilder.DropTable(
                name: "UserActivities");

            migrationBuilder.DropTable(
                name: "UserConversaContexts");

            migrationBuilder.DropTable(
                name: "UserFunStates");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "WebhookLogs");

            migrationBuilder.DropTable(
                name: "YouTubeMusicCaches");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "DispositivosEsp");

            migrationBuilder.DropTable(
                name: "Aprendizados");

            migrationBuilder.DropTable(
                name: "DispositivosDisparo");

            migrationBuilder.DropTable(
                name: "Comodos");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "Receitas");

            migrationBuilder.DropTable(
                name: "Rotinas");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
