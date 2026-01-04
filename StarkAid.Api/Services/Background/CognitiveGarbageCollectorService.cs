using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.Background
{
    public class CognitiveGarbageCollectorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CognitiveGarbageCollectorService> _logger;

        public CognitiveGarbageCollectorService(IServiceProvider serviceProvider, ILogger<CognitiveGarbageCollectorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cognitive Garbage Collector Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWork(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Cognitive Garbage Collector.");
                }

                // Roda a cada 24 horas
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var sbLog = new System.Text.StringBuilder();
                int totalInativados = 0;
                int totalQuarentena = 0;
                int totalRessuscitados = 0;

                var lastRun = await context.GcExecutionLogs
                    .OrderByDescending(l => l.DataExecucao)
                    .FirstOrDefaultAsync(stoppingToken);
                var lastRunDate = lastRun?.DataExecucao ?? DateTimeOffset.MinValue;

                _logger.LogInformation("Iniciando ciclo avançado de limpeza cognitiva (Batch + Quarentena)...");

                try
                {
                    // === REGRA 1: MOVER PARA QUARENTENA ===
                    // Itens fracos (Confiança < 20) e sem uso recente (> 30 dias) e QUE NÃO ESTÃO em quarentena
                    var thresholdDate = DateTimeOffset.UtcNow.AddDays(-30);
                    var minConfidence = 20;

                    bool hasMore = true;
                    int page = 0;
                    int batchSize = 500;

                    while (hasMore)
                    {
                        var candidatesForQuarantine = await context.Aprendizados
                            .Where(a => a.Ativo
                                   && !a.EmQuarentena
                                   && a.ConfidenceScore < minConfidence
                                   && (a.LastUsedAt == null || a.LastUsedAt < thresholdDate))
                            .Take(batchSize)
                            .ToListAsync(stoppingToken);

                        if (!candidatesForQuarantine.Any())
                        {
                            hasMore = false;
                        }
                        else
                        {
                            foreach (var item in candidatesForQuarantine)
                            {
                                item.EmQuarentena = true;
                                item.QuarentenaDesde = DateTimeOffset.UtcNow;
                            }
                            await context.SaveChangesAsync(stoppingToken);
                            totalQuarentena += candidatesForQuarantine.Count;
                            _logger.LogInformation($"Batch {page}: {candidatesForQuarantine.Count} itens movidos para quarentena.");
                            page++;
                        }
                    }

                    // === REGRA 2: INATIVAR DEPOIS DA QUARENTENA ===
                    // Itens que estão em quarentena há mais de 7 dias e não foram "ressuscitados" (leia-se: acessados)
                    var quarantineThreshold = DateTimeOffset.UtcNow.AddDays(-7);
                    
                    hasMore = true;
                    page = 0;
                    
                    while (hasMore)
                    {
                         var candidatesForInactivation = await context.Aprendizados
                            .Where(a => a.Ativo
                                   && a.EmQuarentena
                                   && a.QuarentenaDesde < quarantineThreshold)
                            .Take(batchSize)
                            .ToListAsync(stoppingToken);

                        if (!candidatesForInactivation.Any())
                        {
                             hasMore = false;
                        }
                        else
                        {
                            foreach (var item in candidatesForInactivation)
                            {
                                item.Ativo = false;
                                item.EmQuarentena = false; // Limpa flag pois já morreu
                                item.QuarentenaDesde = null;
                            }
                            await context.SaveChangesAsync(stoppingToken);
                            totalInativados += candidatesForInactivation.Count;
                            _logger.LogInformation($"Batch {page}: {candidatesForInactivation.Count} itens inativados definitivamente.");
                            page++;
                        }
                    }

                    // === REGRA 3: CONTEXTUAL EXPIRADO (Straight to Inactive) ===
                    // Contextual antigo (> 60 dias) e inútil (hit < 2) pode morrer direto, sem quarentena
                    var contextThreshold = DateTimeOffset.UtcNow.AddDays(-60);
                    
                    var expiredContextuals = await context.Aprendizados
                         .Where(a => a.Ativo 
                                && a.Tipo == "Contextual" 
                                && (a.LastUsedAt == null || a.LastUsedAt < contextThreshold)
                                && a.HitCount < 2)
                         .ToListAsync(stoppingToken);

                    if (expiredContextuals.Any())
                    {
                        foreach (var item in expiredContextuals)
                        {
                            item.Ativo = false;
                        }
                        await context.SaveChangesAsync(stoppingToken);
                        totalInativados += expiredContextuals.Count;
                        sbLog.AppendLine($"Contextuais limpos: {expiredContextuals.Count}");
                    }

                    // === REGRA 4: PROMOÇÃO ASSISTIDA (Usuario -> Global) ===
                    // Itens de usuários com alta performance e sem conteúdo pessoal/ambíguo
                    // Critério Secundário: Deve ter sido usado por pelo menos 3 variantes de perguntas (Prova de generalização)
                    int totalPromovidos = 0;
                    var highPerformanceUsers = await context.Aprendizados
                        .Where(a => a.Ativo 
                               && a.Tipo == "Usuario" 
                               && a.ConfidenceScore >= 80 
                               && a.HitCount >= 5
                               && a.VariantesDistintasUsadas >= 3)
                        .Take(batchSize)
                        .Include(a => a.Respostas)
                        .ToListAsync(stoppingToken);

                    foreach (var item in highPerformanceUsers)
                    {
                        // Validação extra antes de tornar público
                        bool ehPessoal = Helpers.TextHelper.EhConteudoPessoal(item.Texto);
                        bool ehAmbiguo = Helpers.TextHelper.EhAmbiguo(item.Texto);

                        if (!ehPessoal && !ehAmbiguo)
                        {
                            item.Tipo = "Global";
                            // Limpa UserId para desvincular do dono original no cache público (Privacy)
                            item.UserId = null; 
                            // Higieniza resposta (remove gírias)
                            item.Resposta = Helpers.TextHelper.LimparGirias(item.Resposta);
                            
                            // Gerar variações para o cache randômico
                            var iaService = scope.ServiceProvider.GetRequiredService<V1.SuperIA.IaService>();
                            var variacoes = await iaService.GerarVariacoesParaGlobal(item.Texto, item.Resposta);
                            
                            // Limpa variações antigas se houver
                            item.Respostas.Clear();
                            
                            foreach (var v in variacoes)
                            {
                                item.Respostas.Add(new AprendizadoResposta
                                {
                                    Id = Guid.NewGuid(),
                                    Texto = v,
                                    CreatedAt = DateTimeOffset.UtcNow
                                });
                            }
                            // Adiciona a original higienizada
                            item.Respostas.Add(new AprendizadoResposta
                            {
                                Id = Guid.NewGuid(),
                                Texto = item.Resposta,
                                CreatedAt = DateTimeOffset.UtcNow
                            });

                            totalPromovidos++;
                        }
                    }

                    if (totalPromovidos > 0)
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        sbLog.AppendLine($"Promovidos para Global: {totalPromovidos}");
                    }

                    // === REGRA 5: CONTABILIZAR RESSURREIÇÕES ===
                    totalRessuscitados = await context.Aprendizados
                        .CountAsync(a => a.UltimaRessurreicaoAt > lastRunDate, stoppingToken);
                    if (totalRessuscitados > 0) sbLog.AppendLine($"Items recuperados pelo uso: {totalRessuscitados}");

                    // === REGRA 6: LIMPEZA DE LOGS ANTIGOS ===
                    var logThreshold = DateTimeOffset.UtcNow.AddDays(-30);
                    var oldLogs = await context.GcExecutionLogs
                        .Where(l => l.DataExecucao < logThreshold)
                        .ToListAsync(stoppingToken);
                    if (oldLogs.Any())
                    {
                        context.GcExecutionLogs.RemoveRange(oldLogs);
                        await context.SaveChangesAsync(stoppingToken);
                        sbLog.AppendLine($"Logs antigos removidos: {oldLogs.Count}");
                    }
                    
                    stopwatch.Stop();

                    // === LOGAR TELEMETRIA ===
                    sbLog.AppendLine($"Ciclo finalizado em {stopwatch.ElapsedMilliseconds}ms.");
                    sbLog.AppendLine($"Novos em Quarentena: {totalQuarentena}");
                    sbLog.AppendLine($"Inativados Definitivos: {totalInativados}");

                    var gcLog = new GcExecutionLog
                    {
                        Id = Guid.NewGuid(),
                        DataExecucao = DateTimeOffset.UtcNow,
                        DuracaoMs = stopwatch.ElapsedMilliseconds,
                        ItensEmQuarentena = totalQuarentena,
                        ItensInativados = totalInativados,
                        ItensRessuscitados = totalRessuscitados,
                        LogDetalhado = sbLog.ToString()
                    };
                    
                    context.GcExecutionLogs.Add(gcLog);
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation($"GC Finalizado. {totalQuarentena} Quarentena, {totalInativados} Mortos.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro crítico no GC.");
                }
            }
        }
    }
}
