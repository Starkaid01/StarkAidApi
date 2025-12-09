using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System.Text.Json;

namespace StarkAid.Api.Services.Suporte;

public class SuporteChatService : ISuporteChatService
{
    private readonly AppDbContext _context;
    private readonly ISupportIaService _iaService;
    private readonly ILogger<SuporteChatService> _logger;
    private const int LIMITE_MENSAGENS_IA = 12;

    public SuporteChatService(
        AppDbContext context,
        ISupportIaService iaService,
        ILogger<SuporteChatService> logger)
    {
        _context = context;
        _iaService = iaService;
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

        // Se não encontrou nada, usar IA para responder
        var respostaIa = await _iaService.ProcessarMensagem(userId, mensagem, origem);
        await SalvarMensagemConversa(conversa.Id, "ia", respostaIa);
        conversa.ContadorMensagens++;
        await _context.SaveChangesAsync();

        return respostaIa;
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

        // Salvar mensagem do usuário
        await SalvarMensagemConversa(conversaId, "user", mensagem);

        // Verificar se resolveu
        var resolvido = await VerificarSeResolvido(mensagem);
        if (resolvido)
        {
            // Salvar aprendizado
            if (!string.IsNullOrEmpty(conversa.ProblemaInicial))
            {
                // Obter ações que foram executadas e funcionaram
                var acoesSucesso = await _context.SuporteAcoes
                    .Where(a => a.UserId == userId && a.Origem == origem && a.Sucesso)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                if (acoesSucesso.Any())
                {
                    var solucoes = acoesSucesso.Select(a => $"Executar ação: {a.Acao}").ToList();
                    await SalvarAprendizado(userId, origem, conversa.ProblemaInicial, solucoes);
                }
            }

            await MarcarConversaConcluida(conversaId, true);
            
            // Remover flag de resolução de suporte
            var resolvendoSuporte = await _context.ResolvendoSuportes
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Origem == origem && r.Ativo);
            if (resolvendoSuporte != null)
            {
                resolvendoSuporte.Ativo = false;
                resolvendoSuporte.ResolvidoEm = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            return "Que ótimo que conseguimos resolver seu problema! Se precisar de mais alguma coisa, é só chamar. Tenha um ótimo dia! 😊";
        }

        // Verificar aprendizado
        var aprendizadoSimilar = await BuscarAprendizadoSimilar(conversa.ProblemaInicial ?? mensagem, origem);
        if (aprendizadoSimilar.Count > 0)
        {
            var melhorAprendizado = aprendizadoSimilar.OrderByDescending(a => a.ContadorSucesso).First();
            var solucoes = JsonSerializer.Deserialize<List<string>>(melhorAprendizado.Solucoes) ?? new List<string>();

            var resposta = "Encontrei uma solução similar que funcionou para outros usuários:\n\n";
            foreach (var solucao in solucoes)
            {
                resposta += $"• {solucao}\n";
            }
            resposta += "\nTente as soluções acima. Se não resolver, me avise aqui.";

            await SalvarMensagemConversa(conversaId, "ia", resposta);
            conversa.ContadorMensagens++;
            await _context.SaveChangesAsync();
            return resposta;
        }

        // Verificar se usuário está relatando problema com listar comandos/dispositivos
        var mensagemLower = mensagem.ToLower();
        var problemaListagem = mensagemLower.Contains("não lista") || mensagemLower.Contains("nao lista") ||
                              mensagemLower.Contains("não carrega") || mensagemLower.Contains("nao carrega") ||
                              mensagemLower.Contains("dispositivo") || mensagemLower.Contains("comando");

        if (problemaListagem)
        {
            // Etapa 1: Tentar limpar cache
            var ultimaAcao = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcao == null || ultimaAcao.Acao != "limparcache")
            {
                var resposta = "Vou limpar o cache. Pode ser conflito de dados armazenados em cache.\n\n[COMANDO:limparcache]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }

            // Etapa 2: Se cache não resolveu, tentar atualizar dados
            if (ultimaAcao.Acao == "limparcache" && !ultimaAcao.Sucesso)
            {
                var acaoAtualizar = await _context.SuporteAcoes
                    .Where(a => a.UserId == userId && a.Origem == origem && a.Acao == "atualizardados")
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (acaoAtualizar == null)
                {
                    var resposta = "Vou atualizar os dados. Isso pode resolver o problema.\n\n[COMANDO:atualizardados]";
                    await SalvarMensagemConversa(conversaId, "ia", resposta);
                    conversa.ContadorMensagens++;
                    await _context.SaveChangesAsync();
                    return resposta;
                }
            }

            // Etapa 3: Se nada funcionou, tentar logout
            if (ultimaAcao.Acao == "atualizardados" && !ultimaAcao.Sucesso)
            {
                var acaoLogout = await _context.SuporteAcoes
                    .Where(a => a.UserId == userId && a.Origem == origem && a.Acao == "logout")
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (acaoLogout == null)
                {
                    // Salvar flag de resolução de suporte
                    var resolvendoSuporte = await _context.ResolvendoSuportes
                        .FirstOrDefaultAsync(r => r.UserId == userId && r.Origem == origem && r.Ativo);

                    if (resolvendoSuporte == null)
                    {
                        resolvendoSuporte = new ResolvendoSuporte
                        {
                            UserId = userId,
                            Origem = origem,
                            Ativo = true,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        _context.ResolvendoSuportes.Add(resolvendoSuporte);
                    }

                    var resposta = "Vou fazer logout para limpar a sessão. Você precisará fazer login novamente.\n\n[COMANDO:logout]";
                    await SalvarMensagemConversa(conversaId, "ia", resposta);
                    conversa.ContadorMensagens++;
                    await _context.SaveChangesAsync();
                    return resposta;
                }
            }
        }

        // Tentar ações diretas se usuário diz que não resolveu
        if (mensagemLower.Contains("não resolveu") || mensagemLower.Contains("nao resolveu") || mensagemLower.Contains("ainda não"))
        {
            // Verificar última ação executada
            var ultimaAcao = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcao?.Acao == "limparcache")
            {
                // Tentar atualizar dados
                var resposta = "Vou atualizar os dados agora.\n\n[COMANDO:atualizardados]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
            else if (ultimaAcao?.Acao == "atualizardados")
            {
                // Tentar logout
                var resolvendoSuporte = await _context.ResolvendoSuportes
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.Origem == origem && r.Ativo);

                if (resolvendoSuporte == null)
                {
                    resolvendoSuporte = new ResolvendoSuporte
                    {
                        UserId = userId,
                        Origem = origem,
                        Ativo = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _context.ResolvendoSuportes.Add(resolvendoSuporte);
                }

                var resposta = "Vou fazer logout para limpar a sessão completamente.\n\n[COMANDO:logout]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
        }

        // Verificar limite de mensagens antes de usar IA
        if (await VerificarLimiteMensagens(conversaId))
        {
            conversa.LimiteAtingido = true;
            conversa.ChatConcluido = true;
            conversa.TransferidoParaHumano = true;
            await _context.SaveChangesAsync();

            return "Infelizmente meu limite de contexto foi atingido. Deixe uma mensagem explicando detalhes dos erros que está enfrentando para nosso suporte. Assim que um de nossos especialistas ver sua mensagem, você receberá em seu email instruções sobre o que fazer.";
        }

        // Usar IA para conversar abertamente
        var respostaIa = await _iaService.ProcessarMensagem(userId, mensagem, origem);
        await SalvarMensagemConversa(conversaId, "ia", respostaIa);
        conversa.ContadorMensagens++;
        await _context.SaveChangesAsync();

        return respostaIa;
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
        var palavrasResolvido = new[] { "resolvido", "funcionou", "consegui", "deu certo", "resolveu", "obrigado", "obrigada", "valeu" };

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
        var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        return conversa != null && conversa.ContadorMensagens >= LIMITE_MENSAGENS_IA;
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
