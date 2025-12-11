using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System.Text.Json;

namespace StarkAid.Api.Services.V1.Suporte;

public class SuporteChatService : ISuporteChatService
{
    private readonly AppDbContext _context;
    private readonly ISupportIaService _iaService;
    private readonly ILogger<SuporteChatService> _logger;
    private const int LIMITE_MENSAGENS_IA = 1000; // Temporariamente aumentado para testes

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

        // Se não encontrou nada, usar IA para responder com contexto adequado
        var respostaIa = await _iaService.ProcessarMensagemComContexto(userId, mensagem, origem, conversa.Id);
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

        // Verificar se resolveu (apenas se já houve ações executadas anteriormente)
        var ultimaAcaoResolvido = await _context.SuporteAcoes
            .Where(a => a.UserId == userId && a.Origem == origem)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
        
        // Só verificar se resolveu se já houve alguma ação executada
        if (ultimaAcaoResolvido != null)
        {
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
        }

        // Tentar ações diretas se usuário diz que não resolveu (ANTES de verificar aprendizado)
        var mensagemLower = mensagem.ToLower();
        if (mensagemLower.Contains("não resolveu") || mensagemLower.Contains("nao resolveu") || mensagemLower.Contains("ainda não") || mensagemLower.Contains("ainda nao"))
        {
            // Verificar última ação executada
            var ultimaAcaoNaoResolveu = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcaoNaoResolveu?.Acao == "limparcache")
            {
                // Tentar atualizar dados
                var resposta = "Vou atualizar os dados agora. Isso pode resolver o problema.\n\n" +
                              "[COMANDO:atualizardados]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
            else if (ultimaAcaoNaoResolveu?.Acao == "atualizardados")
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

                var resposta = "Vou fazer logout para limpar completamente a sessão. Isso pode resolver problemas com token expirado.\n\n" +
                              "⚠️ Você será desconectado e precisará fazer login novamente.\n\n" +
                              "[COMANDO:logout]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
            else if (ultimaAcaoNaoResolveu?.Acao == "logout")
            {
                // Se já tentou tudo, dar sugestões específicas
                var resposta = "Já tentamos limpar cache, atualizar dados e fazer logout. Vou verificar outras possibilidades:\n\n" +
                              "1. Verifique se o WebSocket está conectado (veja o status no Dashboard)\n" +
                              "2. Confirme se você está conectado à internet\n" +
                              "3. Tente reiniciar o aplicativo completamente\n" +
                              "4. Verifique se os dispositivos StarkSwitch estão online\n\n" +
                              "Se ainda não funcionar, vou transferir você para o suporte humano para investigação mais profunda.";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
        }

        // Verificar se usuário está relatando problema com dispositivos starkswitch e comandos de voz
        if ((mensagemLower.Contains("starkswitch") || mensagemLower.Contains("stark switch")) && 
            (mensagemLower.Contains("voz") || mensagemLower.Contains("comando") || mensagemLower.Contains("não aciona") || mensagemLower.Contains("nao aciona")))
        {
            var ultimaAcaoStark = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcaoStark == null || ultimaAcaoStark.Acao != "atualizardados")
            {
                var resposta = "Entendi! Você está com problema para acionar dispositivos StarkSwitch por comandos de voz.\n\n" +
                              "Isso geralmente é problema de sincronização ou conexão. Vou atualizar os dados para sincronizar com o servidor.\n\n" +
                              "[COMANDO:atualizardados]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
            else if (ultimaAcaoStark.Acao == "atualizardados")
            {
                var resposta = "Já tentamos atualizar os dados. Vou limpar o cache agora, pois pode haver dados antigos em cache.\n\n" +
                              "[COMANDO:limparcache]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
        }

        // Verificar se usuário está relatando problema com ESP/dispositivos ESP
        if ((mensagemLower.Contains("esp") || mensagemLower.Contains("dispositivo esp")) && 
            (mensagemLower.Contains("não funciona") || mensagemLower.Contains("nao funciona") || 
             mensagemLower.Contains("não está funcionando") || mensagemLower.Contains("nao esta funcionando") ||
             mensagemLower.Contains("não tenho") || mensagemLower.Contains("nao tenho") ||
             mensagemLower.Contains("não recebo") || mensagemLower.Contains("nao recebo")))
        {
            var ultimaAcaoEsp = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcaoEsp == null || ultimaAcaoEsp.Acao != "atualizardados")
            {
                var resposta = "Entendi! Você está com problema para enviar comandos para dispositivos ESP e não está recebendo resposta.\n\n" +
                              "Isso geralmente é problema de sincronização ou conexão UDP. Vou atualizar os dados para sincronizar com o servidor.\n\n" +
                              "[COMANDO:atualizardados]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
            else if (ultimaAcaoEsp.Acao == "atualizardados")
            {
                var resposta = "Já tentamos atualizar os dados. Vou limpar o cache agora, pois pode haver dados antigos em cache.\n\n" +
                              "[COMANDO:limparcache]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }
        }

        // Se chegou aqui, nenhum problema específico foi detectado
        // Processar com IA usando contexto antes de mostrar aprendizado genérico
        var respostaIaContexto = await _iaService.ProcessarMensagemComContexto(userId, mensagem, origem, conversaId);
        
        // Verificar se a resposta da IA contém um comando - se sim, usar ela
        if (!string.IsNullOrEmpty(respostaIaContexto) && respostaIaContexto.Contains("[COMANDO:"))
        {
            await SalvarMensagemConversa(conversaId, "ia", respostaIaContexto);
            conversa.ContadorMensagens++;
            await _context.SaveChangesAsync();
            return respostaIaContexto;
        }
        
        // Se a IA não gerou comando, verificar aprendizado (apenas se não for "não resolveu")
        var aprendizadoSimilar = await BuscarAprendizadoSimilar(conversa.ProblemaInicial ?? mensagem, origem);
        if (aprendizadoSimilar.Count > 0 && 
            !mensagemLower.Contains("não resolveu") && 
            !mensagemLower.Contains("nao resolveu") &&
            conversa.ContadorMensagens > 2) // Só mostrar aprendizado após algumas mensagens
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
        
        // Se não há aprendizado, usar resposta da IA mesmo sem comando
        if (!string.IsNullOrEmpty(respostaIaContexto))
        {
            await SalvarMensagemConversa(conversaId, "ia", respostaIaContexto);
            conversa.ContadorMensagens++;
            await _context.SaveChangesAsync();
            return respostaIaContexto;
        }
        
        // Fallback: retornar mensagem padrão se nada foi processado
        var respostaFallback = "Desculpe, não consegui processar sua mensagem. Por favor, tente descrever o problema de forma mais detalhada.";
        await SalvarMensagemConversa(conversaId, "ia", respostaFallback);
        conversa.ContadorMensagens++;
        await _context.SaveChangesAsync();
        return respostaFallback;

        // Verificar se usuário está relatando problema com listar comandos/dispositivos
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
                var resposta = "Entendi! Vou limpar o cache agora. Isso pode resolver o problema de dados não aparecerem.\n\n" +
                              "[COMANDO:limparcache]";
                await SalvarMensagemConversa(conversaId, "ia", resposta);
                conversa.ContadorMensagens++;
                await _context.SaveChangesAsync();
                return resposta;
            }

            // Etapa 2: Se cache não resolveu, tentar atualizar dados
            if (ultimaAcao.Acao == "limparcache")
            {
                var acaoAtualizar = await _context.SuporteAcoes
                    .Where(a => a.UserId == userId && a.Origem == origem && a.Acao == "atualizardados")
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (acaoAtualizar == null)
                {
                    var resposta = "Vou atualizar os dados agora. Isso pode resolver o problema.\n\n" +
                                  "[COMANDO:atualizardados]";
                    await SalvarMensagemConversa(conversaId, "ia", resposta);
                    conversa.ContadorMensagens++;
                    await _context.SaveChangesAsync();
                    return resposta;
                }
            }

            // Etapa 3: Se nada funcionou, tentar logout
            if (ultimaAcao.Acao == "atualizardados")
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

                    var resposta = "Vou fazer logout para limpar completamente a sessão. Isso pode resolver problemas com token expirado ou dados em cache.\n\n" +
                                  "⚠️ Você será desconectado e precisará fazer login novamente.\n\n" +
                                  "[COMANDO:logout]";
                    await SalvarMensagemConversa(conversaId, "ia", resposta);
                    conversa.ContadorMensagens++;
                    await _context.SaveChangesAsync();
                    return resposta;
                }
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
