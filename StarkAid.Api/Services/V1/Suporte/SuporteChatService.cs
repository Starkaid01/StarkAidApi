using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services;
using System.Text.Json;

using StarkAid.Api.Services.V1.Support.Agents;
using StarkAid.Api.DTOs.V1.Support;

namespace StarkAid.Api.Services.V1.Suporte;

public class SuporteChatService : ISuporteChatService
{
    private readonly AppDbContext _context;
    private readonly ISupportIaService _iaService;
    private readonly ISupportMessageRouter _supportRouter;
    private readonly ILogger<SuporteChatService> _logger;
    private const int LIMITE_MENSAGENS_IA = 1000; // Temporariamente aumentado para testes

    public SuporteChatService(
        AppDbContext context,
        ISupportIaService iaService,
        ISupportMessageRouter supportRouter,
        ILogger<SuporteChatService> logger)
    {
        _context = context;
        _iaService = iaService;
        _supportRouter = supportRouter;
        _logger = logger;
    }

    public async Task<string> ProcessarMensagemInicial(Guid userId, string origem, string mensagem)
    {
        // Criar ou obter conversa
        SuporteConversa? conversa = null;
        try
        {
            conversa = await _context.SuporteConversas
                .Where(c => c.UserId == userId && !c.ChatConcluido && c.Origem == origem)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger.LogError(sqlEx, "Tabela SuporteConversas não existe. A migration precisa ser aplicada.");
            // Criar nova conversa sem buscar
            conversa = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar conversa.");
            conversa = null;
        }

        if (conversa == null)
        {
            conversa = new SuporteConversa
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Origem = origem,
                ProblemaInicial = mensagem,
                Mensagens = JsonSerializer.Serialize(new List<object> { new { sender = "user", message = mensagem, timestamp = DateTimeOffset.UtcNow } }),
                ContadorMensagens = 0
            };
            try
            {
                _context.SuporteConversas.Add(conversa);
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                _logger.LogError(sqlEx, "Tabela SuporteConversas não existe. A migration precisa ser aplicada.");
                throw new InvalidOperationException("A migration do banco de dados precisa ser aplicada. Execute: dotnet ef database update --context AppDbContext");
            }
        }
        else
        {
            // Atualizar problema inicial se necessário
            if (string.IsNullOrEmpty(conversa.ProblemaInicial))
            {
                conversa.ProblemaInicial = mensagem;
                await _context.SaveChangesAsync();
            }
        }

        // Verificar se tem Error Logs
        var consultasErro = await ConsultarErrosDoUsuario(userId, origem);

        if (consultasErro.Count > 0)
        {
            // Filtrar soluções viáveis com IA
            var solucoesFiltradas = await FiltrarSolucoesViaveis(consultasErro);

            if (solucoesFiltradas.Count > 0)
            {
                var resposta = "Encontrei alguns códigos de erro em seus logs. Aqui estão as soluções sugeridas:\n\n";
                foreach (var consulta in solucoesFiltradas)
                {
                    resposta += $"Código: {consulta.CodigoErro}\n";
                    resposta += "Soluções:\n";
                    foreach (var solucao in consulta.Solucoes)
                    {
                        resposta += $"• {solucao}\n";
                    }
                    resposta += "\n";
                }
                resposta += "Tente as soluções acima. Se não resolver, me avise aqui.";

                // Salvar mensagem
                await SalvarMensagemConversa(conversa.Id, "ia", resposta);
                return resposta;
            }
        }

        // Se não tem erros ou não encontrou soluções, verificar perguntas frequentes
        var perguntaLower = mensagem.ToLower();
        var perguntaFrequente = await _context.SuportePerguntasFrequentes
            .Where(pf => perguntaLower.Contains(pf.Pergunta.ToLower()) || pf.Pergunta.ToLower().Contains(perguntaLower))
            .OrderByDescending(pf => pf.CreatedAt)
            .FirstOrDefaultAsync();

        if (perguntaFrequente != null)
        {
            var resposta = perguntaFrequente.Resposta;

            // Se requer ação, enviar comando
            if (perguntaFrequente.RequerAcao)
            {
                var comando = origem == "software" ? perguntaFrequente.SuporteToSoft : perguntaFrequente.SuporteToApp;
                if (!string.IsNullOrEmpty(comando))
                {
                    // Comando será enviado via SignalR
                    resposta += $"\n\n[COMANDO:{comando}]";
                }
            }

            await SalvarMensagemConversa(conversa.Id, "ia", resposta);
            return resposta;
        }

        // Se não encontrou nada, usar Roteador Inteligente
        var supportResult = await _supportRouter.ProcessMessageAsync(userId, mensagem, origem);
        var finalResponse = supportResult.Content;

        if (supportResult.Type == SupportMessageType.AgentActionProposal && !string.IsNullOrEmpty(supportResult.ActionProposed))
        {
             var legacyCmd = supportResult.ActionProposed.ToLower() switch {
                 "cleanappcache" => "limparcache",
                 "cleanappdata" => "limpardados",
                 "restartapp" => "restart",
                 "logout" => "logout",
                 "cleanlocaldatabase" => "limpar-data-base",
                 _ => supportResult.ActionProposed.ToLower()
             };
             finalResponse += $"\n\n[COMANDO:{legacyCmd}]";
        }

        await SalvarMensagemConversa(conversa.Id, "ia", finalResponse);
        conversa.ContadorMensagens++;
        await _context.SaveChangesAsync();

        return finalResponse;
    }

    public async Task<string> ProcessarMensagemUsuario(Guid userId, string origem, string mensagem, Guid conversaId)
    {
        var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        if (conversa == null || conversa.ChatConcluido)
        {
            return "Conversa não encontrada ou já concluída.";
        }

        // Verificar limite
        if (await VerificarLimiteMensagens(conversaId))
        {
            conversa.LimiteAtingido = true;
            conversa.ChatConcluido = true;
            await _context.SaveChangesAsync();

            return "Infelizmente meu limite de contexto foi atingido. Deixe uma mensagem explicando detalhes dos erros que está enfrentando para nosso suporte. Assim que um de nossos especialistas ver sua mensagem, você receberá em seu email instruções sobre o que fazer.";
        }

        // Usar o Roteador Inteligente
        var supportResult = await _supportRouter.ProcessMessageAsync(userId, mensagem, origem);
        
        var finalResponse = supportResult.Content;
        
        // Se houver uma ação solicitada (pós-confirmação), anexar o marcador legado para o Hub processar
        if (!string.IsNullOrEmpty(supportResult.ActionToExecute))
        {
             // Mapear nomes de ações do Router para os comandos internos do legado
             var legacyCmd = supportResult.ActionToExecute.ToLower() switch {
                 "cleanappcache" => "limparcache",
                 "cleanappdata" => "limpardados",
                 "restartapp" => "restart",
                 "logout" => "logout",
                 "cleanlocaldatabase" => "limpar-data-base",
                 _ => supportResult.ActionToExecute.ToLower()
             };

             finalResponse += $"\n\n[COMANDO:{legacyCmd}]";
        }

        // Salvar resposta na conversa do banco
        await SalvarMensagemConversa(conversaId, "ia", finalResponse);
        conversa.ContadorMensagens++;
        await _context.SaveChangesAsync();

        return finalResponse;
    }

    public async Task<List<ConsultaErroResponse>> ConsultarErrosDoUsuario(Guid userId, string origem)
    {
        var consultas = new List<ConsultaErroResponse>();

        List<string> codigosErro;
        if (origem == "software")
        {
            var logs = await _context.ErrorLogsSoft
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();
            
            codigosErro = logs
                .Where(l => !string.IsNullOrEmpty(l.CodigoDeErro))
                .Select(l => l.CodigoDeErro!)
                .Distinct()
                .ToList();
        }
        else
        {
            var logs = await _context.ErrorLogsApp
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();
            
            codigosErro = logs
                .Where(l => !string.IsNullOrEmpty(l.CodigoDeErro))
                .Select(l => l.CodigoDeErro!)
                .Distinct()
                .ToList();
        }

        foreach (var codigo in codigosErro)
        {
            var errorCode = await _context.ErrorCodeDescriptions
                .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && 
                                         (e.Origem == origem || string.IsNullOrEmpty(e.Origem)));

            if (errorCode != null && !string.IsNullOrEmpty(errorCode.Solucoes))
            {
                try
                {
                    var solucoes = JsonSerializer.Deserialize<List<string>>(errorCode.Solucoes) ?? new List<string>();
                    consultas.Add(new ConsultaErroResponse
                    {
                        CodigoErro = codigo,
                        Origem = origem,
                        Solucoes = solucoes
                    });
                }
                catch
                {
                    consultas.Add(new ConsultaErroResponse
                    {
                        CodigoErro = codigo,
                        Origem = origem,
                        Solucoes = new List<string> { errorCode.Solucoes }
                    });
                }
            }
        }

        return consultas;
    }

    public async Task<List<ConsultaErroResponse>> FiltrarSolucoesViaveis(List<ConsultaErroResponse> consultas)
    {
        var consultasFiltradas = new List<ConsultaErroResponse>();

        foreach (var consulta in consultas)
        {
            var solucoesFiltradas = await _iaService.FiltrarSolucoes(consulta.Solucoes, consulta.Origem);
            if (solucoesFiltradas.Count > 0)
            {
                consultasFiltradas.Add(new ConsultaErroResponse
                {
                    CodigoErro = consulta.CodigoErro,
                    Origem = consulta.Origem,
                    Solucoes = solucoesFiltradas
                });
            }
        }

        return consultasFiltradas;
    }

    public async Task<bool> VerificarSeResolvido(string mensagem)
    {
        var mensagemLower = mensagem.ToLower();
        
        // Palavras que indicam resolução (mas não quando usadas em contexto negativo)
        var palavrasResolvido = new[] { 
            "resolvido", 
            "funcionou", 
            "consegui resolver", 
            "deu certo", 
            "resolveu", 
            "obrigado", 
            "obrigada", 
            "valeu",
            "funciona agora",
            "está funcionando",
            "já está ok",
            "já está funcionando"
        };
        
        // Palavras que indicam problema (não resolvido)
        var palavrasProblema = new[] {
            "não estou conseguindo",
            "nao estou conseguindo",
            "não consegui",
            "nao consegui",
            "não funciona",
            "nao funciona",
            "não está funcionando",
            "nao esta funcionando",
            "ainda não",
            "ainda nao",
            "problema",
            "erro"
        };
        
        // Se contém palavras de problema, não está resolvido
        if (palavrasProblema.Any(palavra => mensagemLower.Contains(palavra)))
        {
            return false;
        }
        
        // Verificar se contém palavras de resolução (mas não em contexto negativo)
        return palavrasResolvido.Any(palavra => mensagemLower.Contains(palavra));
    }

    public async Task SalvarAprendizado(Guid userId, string origem, string problema, List<string> solucoesQueFuncionaram)
    {
        var aprendizadoExistente = await _context.SuporteAprendizados
            .FirstOrDefaultAsync(a => a.Problema.ToLower() == problema.ToLower() && a.Origem == origem);

        if (aprendizadoExistente != null)
        {
            aprendizadoExistente.ContadorSucesso++;
            aprendizadoExistente.LastUsedAt = DateTimeOffset.UtcNow;
            var solucoesAtuais = JsonSerializer.Deserialize<List<string>>(aprendizadoExistente.Solucoes) ?? new List<string>();
            solucoesAtuais.AddRange(solucoesQueFuncionaram);
            aprendizadoExistente.Solucoes = JsonSerializer.Serialize(solucoesAtuais.Distinct());
        }
        else
        {
            var novoAprendizado = new SuporteAprendizado
            {
                Problema = problema,
                Solucoes = JsonSerializer.Serialize(solucoesQueFuncionaram),
                Origem = origem,
                ContadorSucesso = 1,
                LastUsedAt = DateTimeOffset.UtcNow
            };
            _context.SuporteAprendizados.Add(novoAprendizado);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<SuporteAprendizado>> BuscarAprendizadoSimilar(string problema, string origem)
    {
        var problemaLower = problema.ToLower();
        var palavrasChave = problemaLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var aprendizados = await _context.SuporteAprendizados
            .Where(a => a.Origem == origem)
            .ToListAsync();

        var similares = aprendizados
            .Where(a => palavrasChave.Any(palavra => a.Problema.ToLower().Contains(palavra)))
            .OrderByDescending(a => a.ContadorSucesso)
            .Take(5)
            .ToList();

        return similares;
    }

    public async Task<bool> VerificarLimiteMensagens(Guid conversaId)
    {
        // Temporariamente desabilitado para testes
        return false;
        // var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        // return conversa != null && conversa.ContadorMensagens >= LIMITE_MENSAGENS_IA;
    }

    public async Task MarcarConversaConcluida(Guid conversaId, bool resolvido)
    {
        var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        if (conversa != null)
        {
            conversa.ChatConcluido = true;
            conversa.Resolvido = resolvido;
            conversa.ConcluidoEm = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task SalvarMensagemConversa(Guid conversaId, string sender, string mensagem)
    {
        var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        if (conversa != null)
        {
            var mensagens = new List<object>();
            if (!string.IsNullOrEmpty(conversa.Mensagens))
            {
                try
                {
                    mensagens = JsonSerializer.Deserialize<List<object>>(conversa.Mensagens) ?? new List<object>();
                }
                catch { }
            }

            mensagens.Add(new { sender, message = mensagem, timestamp = DateTimeOffset.UtcNow });
            conversa.Mensagens = JsonSerializer.Serialize(mensagens);
            await _context.SaveChangesAsync();
        }
    }
}
