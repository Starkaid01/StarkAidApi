using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Entities;
using StarkAid.Api.Hubs;
using StarkAid.Api.Services;
using StarkAid.Api.Services.Devices;
using StarkAid.Api.Services.DispositivoEsp;

namespace StarkAid.Api.Services.Devices;

public class AgendamentoWorker : BackgroundService
{
    private readonly ILogger<AgendamentoWorker> _logger;
    private readonly IServiceProvider _serviceProvider;   // <-- Recebe o provedor raiz
    private const int IntervalMs = 60_000;                // 1 minuto

    public AgendamentoWorker(
        IServiceProvider serviceProvider,                 // <-- injeta apenas o provider
        ILogger<AgendamentoWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgendamentoWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // -------------------------------------------------
                // Cria um **escopo** para usar serviços scoped
                // -------------------------------------------------
                using var scope = _serviceProvider.CreateScope();

                var agendamentoService = scope.ServiceProvider.GetRequiredService<IAgendamentoService>();
                var mqttService = scope.ServiceProvider.GetRequiredService<IMqttClientService>();
                var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
                var dispositivoEspService = scope.ServiceProvider.GetRequiredService<DispositivoEspService>();
                var ewelinkService = scope.ServiceProvider.GetRequiredService<IEwelinkService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DispositivoEspHub>>();

                var pendentes = await agendamentoService.GetPendingAsync();
                _logger.LogInformation("Agendamentos pendentes encontrados: {Count}", pendentes.Count);

                foreach (var ag in pendentes)
                {
                    try
                    {
                        _logger.LogInformation("Processando agendamento {Id}, Tipo: {Tipo}, AgendadoPara: {AgendadoPara}, Executado: {Executado}", 
                            ag.Id, ag.TipoAgendamento, ag.AgendadoPara, ag.Executado);
                        
                        if (ag.TipoAgendamento == TipoAgendamento.ESP)
                        {
                            // Processar agendamento ESP
                            if (ag.DispositivoEspId.HasValue)
                            {
                            var dispositivoEsp = await dispositivoEspService.GetByIdAsync(ag.DispositivoEspId.Value);
                            if (dispositivoEsp == null)
                            {
                                _logger.LogWarning("Dispositivo ESP {Id} não encontrado", ag.DispositivoEspId.Value);
                                ag.Executado = true;
                                continue;
                            }

                            // Usa ComandToEsp se disponível, senão usa Comando como fallback
                            var comandoParaEnviar = !string.IsNullOrWhiteSpace(dispositivoEsp.ComandToEsp) 
                                ? dispositivoEsp.ComandToEsp 
                                : dispositivoEsp.Comando;

                            if (string.IsNullOrWhiteSpace(comandoParaEnviar))
                            {
                                _logger.LogWarning("Dispositivo ESP {Id} não tem comando configurado (ComandToEsp ou Comando)", ag.DispositivoEspId.Value);
                                ag.Executado = true;
                                continue;
                            }

                            // Envia comando via WebSocket para o software Windows Forms
                            var comandoData = new
                            {
                                nome = dispositivoEsp.Nome,
                                ip = dispositivoEsp.Ip,
                                porta = dispositivoEsp.Porta,
                                comando = comandoParaEnviar,
                                comandToEsp = dispositivoEsp.ComandToEsp ?? comandoParaEnviar
                            };

                            _logger.LogInformation("Enviando comando agendado via WebSocket para grupo 'type_software': Nome={Nome}, IP={Ip}, Porta={Porta}, Comando={Comando}, ComandToEsp={ComandToEsp}", 
                                comandoData.nome, comandoData.ip, comandoData.porta, comandoData.comando, comandoData.comandToEsp);

                            await hubContext.Clients.Group("type_software").SendAsync("ComandoDispositivo", comandoData);

                            _logger.LogInformation("Comando agendado '{Comando}' enviado para dispositivo ESP {Nome} ({Ip}:{Porta})", 
                                comandoParaEnviar, dispositivoEsp.Nome, dispositivoEsp.Ip, dispositivoEsp.Porta);
                            }
                        }
                        else if (ag.TipoAgendamento == TipoAgendamento.Starkswitch)
                        {
                            // Processar agendamento Starkswitch
                            if (ag.DeviceId.HasValue)
                            {
                                var device = await deviceService.GetByIdAsync(ag.DeviceId.Value);
                                if (device == null)
                                {
                                    _logger.LogWarning("Dispositivo Starkswitch {Id} não encontrado", ag.DeviceId.Value);
                                    ag.Executado = true;
                                    continue;
                                }

                                // Determina o payload do comando
                                string payload;
                                if (!string.IsNullOrWhiteSpace(ag.Comando))
                                {
                                    payload = ag.Comando.ToLower();
                                }
                                else if (!string.IsNullOrWhiteSpace(device.Comando))
                                {
                                    payload = device.Comando.Trim();
                                }
                                else
                                {
                                    _logger.LogWarning("Nenhum comando disponível para dispositivo Starkswitch {Id}", ag.DeviceId.Value);
                                    ag.Executado = true;
                                    continue;
                                }

                                // Envia comando MQTT
                                if (mqttService.IsConnected)
                                {
                                    await mqttService.PublishAsync(device.MqttTopic, payload);
                                    _logger.LogInformation("Comando agendado '{Payload}' enviado para dispositivo Starkswitch {Name} (tópico: {Topic})", 
                                        payload, device.Name, device.MqttTopic);
                                }
                                else
                                {
                                    _logger.LogWarning("MQTT não está conectado. Comando não enviado para dispositivo {Name}", device.Name);
                                }
                            }
                        }
                        else if (ag.TipoAgendamento == TipoAgendamento.Ewelink)
                        {
                            // Processar agendamento Ewelink
                            if (!string.IsNullOrWhiteSpace(ag.EwelinkDeviceId))
                            {
                                // Determinar ação (ligar ou desligar)
                                bool switchOn = ag.Comando.ToLower() == "ligar";
                                
                                var success = await ewelinkService.ControlDeviceAsync(ag.UserId, ag.EwelinkDeviceId, switchOn);
                                
                                if (success)
                                {
                                    _logger.LogInformation("Comando agendado '{Acao}' enviado para dispositivo Ewelink {DeviceId}", 
                                        ag.Comando, ag.EwelinkDeviceId);
                                }
                                else
                                {
                                    _logger.LogWarning("Falha ao enviar comando agendado para dispositivo Ewelink {DeviceId}", ag.EwelinkDeviceId);
                                    ag.Executado = true;
                                    continue;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Agendamento Ewelink {Id} não tem EwelinkDeviceId", ag.Id);
                                ag.Executado = true;
                                continue;
                            }
                        }

                        // Atualiza o agendamento (marcar como executado ou calcular próximo ciclo)
                        var recorrenciaNormalizada = ag.Recorrencia?.Trim() ?? string.Empty;
                        
                        _logger.LogInformation("Processando agendamento {Id}, Recorrência: '{Recorrencia}', Executado atual: {Executado}", 
                            ag.Id, recorrenciaNormalizada, ag.Executado);
                        
                        if (string.IsNullOrWhiteSpace(recorrenciaNormalizada) || 
                            recorrenciaNormalizada.Equals("NaoRepetir", StringComparison.OrdinalIgnoreCase) || 
                            recorrenciaNormalizada.Equals("Nenhum", StringComparison.OrdinalIgnoreCase))
                        {
                            ag.Executado = true;
                            _logger.LogInformation("Agendamento {Id} marcado como executado (sem recorrência)", ag.Id);
                        }
                        else
                        {
                            var dataAntes = ag.AgendadoPara;
                            ag.AgendadoPara = recorrenciaNormalizada switch
                            {
                                "TodosOsDias" => ag.AgendadoPara.AddDays(1),
                                "TodaSemana" => ag.AgendadoPara.AddDays(7),
                                "TodoMes" => ag.AgendadoPara.AddMonths(1),
                                "TodoAno" => ag.AgendadoPara.AddYears(1),
                                "Diario" => ag.AgendadoPara.AddDays(1), // Compatibilidade com valores antigos
                                "Semanal" => ag.AgendadoPara.AddDays(7),
                                "Mensal" => ag.AgendadoPara.AddMonths(1),
                                "Anual" => ag.AgendadoPara.AddYears(1),
                                _ => throw new InvalidOperationException($"Recorrência inválida: '{recorrenciaNormalizada}'")
                            };
                            ag.Executado = false; // Reset para permitir próxima execução
                            _logger.LogInformation("Agendamento {Id} reagendado de {DataAntes} para {DataDepois} (recorrência: {Recorrencia})", 
                                ag.Id, dataAntes, ag.AgendadoPara, recorrenciaNormalizada);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar agendamento {Id}", ag.Id);
                        // Marca como executado para evitar loop infinito de erros
                        ag.Executado = true;
                    }
                }

                // Salva as alterações de todos os agendamentos modificados
                await agendamentoService.UpdateManyAsync(pendentes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no AgendamentoWorker");
            }

            await Task.Delay(IntervalMs, stoppingToken);
        }

        _logger.LogInformation("AgendamentoWorker finalizado.");
    }
}
