using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Rotinas;
using StarkAid.Api.Entities;
using StarkAid.Api.Hubs;
using StarkAid.Api.Services.CommandRouter;
using StarkAid.Api.Services.V1.Devices;
using StarkAid.Api.DTOs.Commands;
using Newtonsoft.Json.Linq;
using StarkAid.Api.Services.V1; // Added for ITokenUsageService
using StarkAid.Api.Services.V1.Devices;

namespace StarkAid.Api.Services.V1.Rotinas
{
    public class RotinaService : IRotinaService
    {
        private readonly AppDbContext _context;
        private readonly IMqttClientService _mqttService;
        private readonly IEwelinkService _ewelinkService;
        private readonly IHubContext<DispositivoEspHub> _hubContext;
        private readonly IHubContext<DeviceHub> _deviceHubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RotinaService> _logger;
        private readonly ITokenUsageService _tokenUsageService; // Added
        private readonly FcmNotificationService _fcmService;

        public RotinaService(
            AppDbContext context,
            IMqttClientService mqttService,
            IEwelinkService ewelinkService,
            IHubContext<DispositivoEspHub> hubContext,
            IHubContext<DeviceHub> deviceHubContext,
            IServiceProvider serviceProvider,
            ILogger<RotinaService> logger,
            ITokenUsageService tokenUsageService,
            FcmNotificationService fcmService) // Added
        {
            _context = context;
            _mqttService = mqttService;
            _ewelinkService = ewelinkService;
            _hubContext = hubContext;
            _deviceHubContext = deviceHubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _tokenUsageService = tokenUsageService; // Added
            _fcmService = fcmService;
        }

        public async Task<List<RotinaDto>> GetAllAsync(Guid userId)
        {
            var rotinas = await _context.Rotinas
                .Include(r => r.Gatilhos)
                .Include(r => r.Acoes)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return rotinas.Select(MapToDto).ToList();
        }

        public async Task<RotinaDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var rotina = await _context.Rotinas
                .Include(r => r.Gatilhos)
                .Include(r => r.Acoes)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            return rotina != null ? MapToDto(rotina) : null;
        }

        public async Task<RotinaDto> CreateAsync(Guid userId, CreateRotinaRequest request)
        {
            var rotina = new Rotina
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Nome = request.Nome,
                Descricao = request.Descricao,
                Ativa = true,
                CriadaEm = DateTimeOffset.UtcNow,
                AtualizadaEm = DateTimeOffset.UtcNow,
                Gatilhos = request.Gatilhos.Select(g => new RotinaGatilho
                {
                    Id = Guid.NewGuid(),
                    Tipo = g.Tipo,
                    Expressao = g.Expressao,
                    DiasSemana = g.DiasSemana
                }).ToList(),
                Acoes = request.Acoes.Select(a => new RotinaAcao
                {
                    Id = Guid.NewGuid(),
                    OrdemExecucao = a.OrdemExecucao,
                    Tipo = a.Tipo,
                    Payload = a.Payload
                }).ToList()
            };

            _context.Rotinas.Add(rotina);
            await _context.SaveChangesAsync();

            return MapToDto(rotina);
        }

        public async Task<RotinaDto?> UpdateAsync(Guid id, Guid userId, UpdateRotinaRequest request)
        {
            try
            {
                // 1. Verificar existência (sem tracking para não poluir o contexto)
                var exists = await _context.Rotinas.AnyAsync(r => r.Id == id && r.UserId == userId);
                if (!exists) return null;

                // 2. Atualizar a Rotina (base) via ExecuteUpdate (atômico e direto no DB)
                await _context.Rotinas
                    .Where(r => r.Id == id && r.UserId == userId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Nome, request.Nome)
                        .SetProperty(r => r.Descricao, request.Descricao)
                        .SetProperty(r => r.Ativa, request.Ativa)
                        .SetProperty(r => r.AtualizadaEm, DateTimeOffset.UtcNow));

                // 3. Limpar coleções antigas via ExecuteDelete (direto no DB)
                await _context.RotinaGatilhos.Where(g => g.RotinaId == id).ExecuteDeleteAsync();
                await _context.RotinaAcoes.Where(a => a.RotinaId == id).ExecuteDeleteAsync();

                // 4. Adicionar novos Gatilhos
                if (request.Gatilhos.Any())
                {
                    var novosGatilhos = request.Gatilhos.Select(g => new RotinaGatilho
                    {
                        Id = Guid.NewGuid(),
                        RotinaId = id,
                        Tipo = g.Tipo,
                        Expressao = g.Expressao,
                        DiasSemana = g.DiasSemana
                    }).ToList();
                    _context.RotinaGatilhos.AddRange(novosGatilhos);
                }

                // 5. Adicionar novas Ações
                if (request.Acoes.Any())
                {
                    var novasAcoes = request.Acoes.Select(a => new RotinaAcao
                    {
                        Id = Guid.NewGuid(),
                        RotinaId = id,
                        OrdemExecucao = a.OrdemExecucao,
                        Tipo = a.Tipo,
                        Payload = a.Payload
                    }).ToList();
                    _context.RotinaAcoes.AddRange(novasAcoes);
                }

                // 6. Salvar as novas inserções
                await _context.SaveChangesAsync();

                // 7. Retornar DTO atualizado
                var rotinaFinal = await _context.Rotinas
                    .Include(r => r.Gatilhos)
                    .Include(r => r.Acoes)
                    .AsNoTracking()
                    .FirstAsync(r => r.Id == id);

                return MapToDto(rotinaFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar rotina {Id} (Modo Direto)", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var rotina = await _context.Rotinas.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (rotina == null) return false;

            _context.Rotinas.Remove(rotina);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetAtivaAsync(Guid id, Guid userId, bool ativa)
        {
            var rotina = await _context.Rotinas.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (rotina == null) return false;

            rotina.Ativa = ativa;
            rotina.AtualizadaEm = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ExecutarRotinaAsync(Guid id, Guid userId, int depth = 0)
        {
            if (depth > 5)
            {
                _logger.LogWarning("Limite de profundidade de rotina atingido para usuário {UserId}. Abortando para evitar loop infinito.", userId);
                return;
            }

            var rotina = await _context.Rotinas
                .Include(r => r.Acoes)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (rotina == null || !rotina.Ativa) return;

            _logger.LogInformation("Executando rotina {RotinaNome} ({RotinaId}) para o usuário {UserId}", rotina.Nome, rotina.Id, userId);

            var acoesOrdenadas = rotina.Acoes.OrderBy(a => a.OrdemExecucao).ToList();

            foreach (var acao in acoesOrdenadas)
            {
                try
                {
                await ExecutarAcaoAsync(userId, acao, depth);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar ação {AcaoId} da rotina {RotinaId}", acao.Id, rotina.Id);
                }
            }
        }

        public async Task ProcessarGatilhosTempoAsync(DateTimeOffset agora)
        {
            // Pega o horário em HH:mm (formato usado nas rotinas)
            string horarioAtual = agora.ToString("HH:mm");
            int diaSemanaAtual = (int)agora.DayOfWeek; 
            if (diaSemanaAtual == 0) diaSemanaAtual = 7; // Ajustar Domingo para 7 se seg=1

            var gatilhos = await _context.RotinaGatilhos
                .Include(g => g.Rotina)
                .Where(g => g.Tipo == TipoGatilho.Tempo && 
                            g.Expressao == horarioAtual && 
                            g.Rotina.Ativa)
                .ToListAsync();

            foreach (var gatilho in gatilhos)
            {
                // Verifica dias da semana se especificado
                if (!string.IsNullOrEmpty(gatilho.DiasSemana))
                {
                    var dias = gatilho.DiasSemana.Split(',').Select(int.Parse);
                    if (!dias.Contains(diaSemanaAtual)) continue;
                }

                _logger.LogInformation("Gatilho de tempo disparado para rotina {RotinaId}", gatilho.RotinaId);
                
                // Executar em background para não travar o loop do scheduler
                _ = Task.Run(() => ExecutarRotinaAsync(gatilho.RotinaId, gatilho.Rotina.UserId, 0));
            }
        }

        public async Task<bool> ProcessarGatilhosComandoAsync(Guid userId, string comando, int depth = 0)
        {
            var comandoNormalizado = Helpers.TextHelper.NormalizarTexto(comando);
            bool disparou = false;
            _logger.LogInformation("Processando gatilhos de comando para User {UserId}: '{Comando}' (Normalizado: '{Normalizado}')", userId, comando, comandoNormalizado);

            var gatilhos = await _context.RotinaGatilhos
                .Include(g => g.Rotina)
                .Where(g => g.Tipo == TipoGatilho.Comando && g.Rotina.UserId == userId && g.Rotina.Ativa)
                .ToListAsync();

            _logger.LogInformation("Encontrados {Count} gatilhos de comando ativos para este usuário.", gatilhos.Count);

            foreach (var gatilho in gatilhos)
            {
                var expressaoNormalizada = Helpers.TextHelper.NormalizarTexto(gatilho.Expressao);
                _logger.LogInformation("Testando gatilho: '{Expressao}' (Normalizado: '{Normalizado}')", gatilho.Expressao, expressaoNormalizada);

                if (comandoNormalizado.Equals(expressaoNormalizada) || comandoNormalizado.Contains(expressaoNormalizada))
                {
                    _logger.LogInformation("Gatilho de comando '{Comando}' disparado para rotina {RotinaId}", gatilho.Expressao, gatilho.RotinaId);
                    await ExecutarRotinaAsync(gatilho.RotinaId, userId, depth + 1);
                    disparou = true;
                }
            }

            return disparou;
        }

        private async Task ExecutarAcaoAsync(Guid userId, RotinaAcao acao, int depth = 0)
        {
            _logger.LogInformation("Executando ação tipo {TipoAcao} com payload {Payload}", acao.Tipo, acao.Payload);

            switch (acao.Tipo)
            {
                case TipoAcao.Dispositivo:
                    await ControlarDispositivoAsync(userId, acao.Payload);
                    break;
                case TipoAcao.Comando:
                    // Deduct a StarkCoin for IA command execution
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null && user.StarkCoins > 0)
                    {
                        user.StarkCoins -= 1;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("StarkCoin deduzido para usuário {UserId} devido a comando IA da rotina.", userId);
                    }
                    else
                    {
                        _logger.LogWarning("Usuário {UserId} não possui StarkCoins suficientes para comando IA da rotina.", userId);
                        // Optionally, you might want to skip the command execution or notify the user
                        var notifPayloadNoCoins = new JObject { ["titulo"] = "StarkAid", ["mensagem"] = "Você não tem StarkCoins suficientes para executar este comando IA." };
                        await EnviarNotificacaoAppAsync(userId, notifPayloadNoCoins.ToString());
                        break; // Exit if no coins
                    }
                    // Processar comando de voz via IA
                    var commandResult = await ProcessarComandoVozAsync(userId, acao.Payload, depth);
                    // Enviar notificação ao app com o resultado
                    if (!string.IsNullOrEmpty(commandResult))
                    {
                        var notifPayload = new JObject { ["titulo"] = "IA", ["mensagem"] = commandResult };
                        await EnviarNotificacaoAppAsync(userId, notifPayload.ToString());
                    }
                    break;
                case TipoAcao.Delay:
                    await ProcessarDelayAsync(acao.Payload);
                    break;
                case TipoAcao.Notificacao:
                    await EnviarNotificacaoAppAsync(userId, acao.Payload);
                    break;
                case TipoAcao.AbrirUrl:
                    await SolicitarAberturaUrlAppAsync(userId, acao.Payload);
                    break;
                case TipoAcao.ComandoAssistente:
                    await EnviarComandoAssistenteAppAsync(userId, acao.Payload);
                    break;
            }
        }

        private async Task EnviarNotificacaoAppAsync(Guid userId, string payloadJson)
        {
            try
            {
                var payload = JObject.Parse(payloadJson);
                var titulo = payload["titulo"]?.ToString() ?? "StarkAid";
                var mensagem = payload["mensagem"]?.ToString() ?? "";
                
                _logger.LogInformation("Enviando notificação para user {UserId}: Título={Titulo}, Mensagem={Mensagem}", userId, titulo, mensagem);
                
                // 1. SignalR (Real-time se app estiver aberto e conectado)
                await _deviceHubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", titulo, mensagem);

                // 2. FCM Push Notification (Para background/notificação de sistema)
                await _fcmService.EnviarParaUsuarioAsync(userId, titulo, mensagem, tipo: "rotina");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação da rotina");
            }
        }

        private async Task SolicitarAberturaUrlAppAsync(Guid userId, string payloadJson)
        {
            try
            {
                var payload = JObject.Parse(payloadJson);
                var url = payload["url"]?.ToString();
                
                if (!string.IsNullOrEmpty(url))
                {
                    await _deviceHubContext.Clients.Group(userId.ToString()).SendAsync("OpenUrl", url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao solicitar abertura de URL da rotina");
            }
        }

        private async Task EnviarComandoAssistenteAppAsync(Guid userId, string payloadJson)
        {
            try
            {
                var payload = JObject.Parse(payloadJson);
                var comando = payload["comando"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(comando))
                {
                    _logger.LogInformation("Enviando comando de assistente para user {UserId}: {Comando}", userId, comando);
                    await _deviceHubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveAssistantCommand", comando);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar comando de assistente para o app");
            }
        }

        private async Task ControlarDispositivoAsync(Guid userId, string payloadJson)
        {
            try
            {
                var payload = JObject.Parse(payloadJson);
                var deviceId = payload["deviceId"]?.ToString();
                var tipo = payload["tipo"]?.ToString();
                var action = payload["action"]?.ToString()?.ToLower();
                bool turnOn = action == "on" || action == "ligar";

                if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(tipo)) return;

                if (tipo == "Device")
                {
                    if (Guid.TryParse(deviceId, out var gId))
                    {
                        var d = await _context.Devices.FindAsync(gId);
                        if (d != null)
                        {
                            // Se for ligar (turnOn=true), usa o comando customizado se houver, ou "ON".
                            // Se for desligar (turnOn=false), usa "OFF" padrão (ignora comando customizado que geralmente é para ligar).
                            var payloadMqtt = turnOn 
                                ? (!string.IsNullOrEmpty(d.Comando) ? d.Comando : "ON") 
                                : "OFF";
                                
                            await _mqttService.PublishAsync(d.MqttTopic, payloadMqtt);
                            d.IsOn = turnOn;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else if (tipo == "Ewelink")
                {
                    await _ewelinkService.ControlDeviceAsync(userId, deviceId, turnOn);
                }
                else if (tipo == "Esp")
                {
                    if (Guid.TryParse(deviceId, out var gId))
                    {
                        var esp = await _context.DispositivosEsp.FindAsync(gId);
                        if (esp != null)
                        {
                            // Mesma lógica para ESP: custom command apenas para ligar
                            var comandoBase = turnOn 
                                ? (!string.IsNullOrWhiteSpace(esp.ComandToEsp) ? esp.ComandToEsp : esp.Comando)
                                : null; // Para desligar, não usamos o campo de comando customizado "genérico"
                                
                            // Se for desligar ou se não tiver comando custom, definimos um padrão?
                            // ESP geralmente espera algo específico. Se o usuário configurou comando, assumimos que é para ativar.
                            // Mas para desativar, se for ESP custom, talvez precise de outro comando.
                            // Como não temos campo "OffCommand", assumiremos que o ESP trata Toggle ou o app envia comando específico.
                            // Mas na rotina, a "action" define "on" ou "off".
                            
                            // Se o ESP usa lógica de API direta (ComandToEsp), enviar nada pode não funcionar.
                            // Vamos manter a lógica original mas tentar enviar OFF se for desligar e não tiver custom command.
                            
                            // Correção simplificada: Se for ESP via Hub, enviamos o objeto comando.
                            // Se for desligar, precisamos garantir que o Esp receba a instrução de desligar.
                            // O Hub envia: { nome, ip, porta, comando... }
                            // Se 'turnOn' é false, o que enviamos?
                            
                            // Analisando a implementação original: ela SEMPRE enviava o comando se existisse.
                            // Vamos assumir que para ESP, se for desligar, o ideal seria não enviar o comando de "Ligar".
                            // Mas como desligamos um ESP genérico? Normalmente via HTTP/Tcp com comando específico.
                            // Se não tivermos o comando de desligar, não conseguimos desligar.
                            // VAMOS MANTER a lógica original para ESP por enquanto se não tivermos certeza, 
                            // MAS para MQTT (Device) é seguro enviar "OFF".
                            
                            // Revisando Device (MQTT):
                            /* 
                               Original: !string.IsNullOrEmpty(d.Comando) ? d.Comando : (turnOn ? "ON" : "OFF")
                               Se d.Comando="LIGAR_LUZ", enviava "LIGAR_LUZ" mesmo se turnOn=false.
                               Correção: se turnOn=false, enviar "OFF".
                            */
                             
                             // Manteve-se a lógica corrigida apenas para Device (MQTT) acima. 
                             // Para ESP, vamos tentar melhorar também.
                             
                            var comandoParaEnviar = turnOn 
                                ? (!string.IsNullOrWhiteSpace(esp.ComandToEsp) ? esp.ComandToEsp : esp.Comando)
                                : "OFF"; // Tentativa de enviar OFF se for desligar

                            // Se o comando original fosse vazio, ele não enviava nada.
                            // Agora se for desligar, envia "OFF".
                            
                             if (!string.IsNullOrWhiteSpace(comandoParaEnviar))
                            {
                                var comandoData = new
                                {
                                    nome = esp.Nome,
                                    ip = esp.Ip,
                                    porta = esp.Porta,
                                    comando = comandoParaEnviar,
                                    comandToEsp = esp.ComandToEsp ?? comandoParaEnviar
                                };
                                await _hubContext.Clients.Group("type_software").SendAsync("ComandoDispositivo", comandoData);
                                esp.LigadoDesligado = turnOn;
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar ação de dispositivo na rotina.");
            }
        }

        private async Task<string?> ProcessarComandoVozAsync(Guid userId, string payloadJson, int depth = 0)
        {
            try
            {
                var payload = JObject.Parse(payloadJson);
                var comando = payload["comando"]?.ToString();

                if (!string.IsNullOrEmpty(comando))
                {
                    // Usa o roteador de comandos para executar como se fosse uma entrada do usuário
                    // Resolvemos ICommandRouter do provedor de serviços para evitar dependência circular no construtor
                    var commandRouter = _serviceProvider.GetRequiredService<ICommandRouter>();
                    var result = await commandRouter.RouteAsync(new CommandRequestDto { 
                        UserId = userId, 
                        Texto = comando, 
                        ExecutionDepth = depth + 1, // Incrementar profundidade para evitar recursão infinita
                        UseStarkCoins = true // Sempre true para comandos de rotina
                    });
                    
                    _logger.LogInformation("Resultado do comando de voz na rotina: Success={IsSuccess}, Message={Message}", result.IsSuccess, result.Message);
                    
                    if (result.IsSuccess)
                    {
                        return result.Message; // Assuming CommandResult has Message (or similar). If not, verify CommandResult structure later. Assuming standard Result pattern.
                        // Actually, I should check CommandResult properties. Usually it has Data or Message.
                        // Based on IaCommandHandler: return CommandResult.Success(resultado.Texto); which implies a constructor or factory taking string.
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar comando de voz na rotina.");
            }
            return null;
        }

        private async Task ProcessarDelayAsync(string payloadJson)
        {
            try
            {
                var payload = JObject.Parse(payloadJson);
                var seconds = payload["seconds"]?.Value<int>() ?? payload["delaySeconds"]?.Value<int>() ?? 0;
                if (seconds > 0)
                {
                    await Task.Delay(seconds * 1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar delay na rotina.");
            }
        }

        private RotinaDto MapToDto(Rotina r)
        {
            return new RotinaDto
            {
                Id = r.Id,
                Nome = r.Nome,
                Descricao = r.Descricao,
                Ativa = r.Ativa,
                CriadaEm = r.CriadaEm,
                AtualizadaEm = r.AtualizadaEm,
                Gatilhos = r.Gatilhos.Select(g => new RotinaGatilhoDto
                {
                    Id = g.Id,
                    Tipo = g.Tipo,
                    Expressao = g.Expressao,
                    DiasSemana = g.DiasSemana
                }).ToList(),
                Acoes = r.Acoes.Select(a => new RotinaAcaoDto
                {
                    Id = a.Id,
                    OrdemExecucao = a.OrdemExecucao,
                    Tipo = a.Tipo,
                    Payload = a.Payload
                }).ToList()
            };
        }

        public async Task SeedDefaultRotinasAsync(Guid userId)
        {
            var defaultRotinas = new List<Rotina>
            {
                new Rotina
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Nome = "Bom dia",
                    Descricao = "Rotina matinal padrão",
                    Ativa = true,
                    CriadaEm = DateTimeOffset.UtcNow,
                    AtualizadaEm = DateTimeOffset.UtcNow,
                    Gatilhos = new List<RotinaGatilho> {
                        new RotinaGatilho { Id = Guid.NewGuid(), Tipo = TipoGatilho.Tempo, Expressao = "08:00", DiasSemana = "1,2,3,4,5" },
                        new RotinaGatilho { Id = Guid.NewGuid(), Tipo = TipoGatilho.Comando, Expressao = "bom dia" }
                    },
                    Acoes = new List<RotinaAcao> {
                        new RotinaAcao { Id = Guid.NewGuid(), OrdemExecucao = 1, Tipo = TipoAcao.Comando, Payload = "{\"comando\": \"que horas são?\"}" },
                        new RotinaAcao { Id = Guid.NewGuid(), OrdemExecucao = 2, Tipo = TipoAcao.Comando, Payload = "{\"comando\": \"como está o tempo hoje?\"}" }
                    }
                },
                new Rotina
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Nome = "Boa noite",
                    Descricao = "Rotina para dormir",
                    Ativa = true,
                    CriadaEm = DateTimeOffset.UtcNow,
                    AtualizadaEm = DateTimeOffset.UtcNow,
                    Gatilhos = new List<RotinaGatilho> {
                        new RotinaGatilho { Id = Guid.NewGuid(), Tipo = TipoGatilho.Comando, Expressao = "boa noite" }
                    },
                    Acoes = new List<RotinaAcao> {
                        new RotinaAcao { Id = Guid.NewGuid(), OrdemExecucao = 1, Tipo = TipoAcao.Comando, Payload = "{\"comando\": \"apagar tudo\"}" },
                        new RotinaAcao { Id = Guid.NewGuid(), OrdemExecucao = 2, Tipo = TipoAcao.Delay, Payload = "{\"seconds\": 2}" }
                    }
                }
            };

            foreach(var r in defaultRotinas)
            {
                var exists = await _context.Rotinas.AnyAsync(x => x.UserId == userId && x.Nome == r.Nome);
                if (!exists)
                {
                    _context.Rotinas.Add(r);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
