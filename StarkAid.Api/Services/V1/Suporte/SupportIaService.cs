using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;
using System.Text.Json;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace StarkAid.Api.Services.V1.Suporte;

public record IaSupportResult(string Texto, int PromptTokens, int CompletionTokens);

public class SupportIaService : ISupportIaService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SupportIaService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _groApiKey;
    private readonly string _openRouterKey;
    private readonly ITokenUsageService _tokenUsage;
    private readonly PlanoLimitesService _planoLimites;
    private const string PROMPT_SUPORTE = @"Você é o Assistente de Suporte da StarkAid. Seu objetivo é diagnosticar problemas do usuário de forma técnica, clara e curta.

REGRAS DE CONTEXTO:
1. Sempre use o resumo do estado fornecido no campo ""contextoIA"".
2. Utilize somente as últimas mensagens fornecidas no campo ""historico"" para manter coerência.
3. Nunca repita todo o histórico, apenas use-o para raciocinar.

REGRAS DE ESTILO:
- Respostas curtas, objetivas, técnicas.
- Evite perguntas genéricas demais.
- Caso o usuário descreva um problema, proponha imediatamente um diagnóstico inicial.

COMANDOS SOCIAIS - REGRA CRÍTICA:
Quando identificar que o usuário pede alguma ação interna OU quando diagnosticar que uma ação pode resolver o problema, siga SEMPRE este formato EXATO:
1) Responda ao usuário com explicação curta (1-2 frases).
2) Na MESMA resposta, na linha seguinte, inclua OBRIGATORIAMENTE o marcador: [COMANDO:xxx]

Comandos válidos (USE APENAS ESTES):
- limparcache (para problemas de cache/dados antigos)
- atualizardados (para problemas de sincronização/dados não aparecem)
- logout (para problemas de sessão/token)
- limpardados (para limpeza completa de dados temporários)

Nunca invente comandos fora desta lista.

EXEMPLOS CORRETOS DE RESPOSTA:
Exemplo 1:
Vou atualizar os dados para sincronizar com o servidor.
[COMANDO:atualizardados]

Exemplo 2:
Vou limpar o cache agora. Isso pode resolver o problema.
[COMANDO:limparcache]

IMPORTANTE: O marcador [COMANDO:xxx] DEVE estar sempre presente quando você recomendar uma ação. Sem ele, a ação não será executada.

IMPORTANTE:
- Se o usuário relatar problema com ESP/dispositivos ESP não respondendo, sugira atualizar dados ou limpar cache e use [COMANDO:atualizardados] ou [COMANDO:limparcache]
- Se o usuário relatar problema com StarkSwitch não acionando por voz, sugira atualizar dados e use [COMANDO:atualizardados]
- Se o usuário disser algo como ""meus comandos sociais não respondem"", faça diagnóstico:
  - peça a frase dita pelo usuário
  - compare com o comando cadastrado
  - sugira correção
  - só emita [COMANDO:...] se tiver certeza
- SEMPRE que diagnosticar que uma ação pode resolver, inclua o [COMANDO:xxx] na resposta

OBJETIVO FINAL:
Atender o usuário de forma direta, técnica e funcional, sem rodeios, usando o mínimo de tokens necessário e sem perder o contexto.";

    public SupportIaService(
        AppDbContext context, 
        ILogger<SupportIaService> logger, 
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ITokenUsageService tokenUsage,
        PlanoLimitesService planoLimites)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _groApiKey = configuration["IaApiKeys:GroApiKey"] ?? "";
        _openRouterKey = configuration["IaApiKeys:OpenRouterKEY"] ?? "";
        _tokenUsage = tokenUsage;
        _planoLimites = planoLimites;
    }

    public async Task<string> GerarSaudacaoInicial(Guid userId, string nome, string email, string origem, object logs)
    {
        var logsList = logs as System.Collections.IEnumerable;
        var temLogs = logsList != null && logsList.Cast<object>().Any();

        var saudacao = $"Olá {nome}! 👋\n\n";
        saudacao += "Sou o assistente virtual de suporte da StarkAid. Como posso ajudá-lo hoje?\n\n";

        if (temLogs)
        {
            saudacao += "Detectei alguns logs de erro recentes em sua conta. Posso ajudá-lo a resolver esses problemas.\n";
        }
        else
        {
            saudacao += "Se você possui algum código de erro, pode me informar e eu tentarei ajudá-lo.\n";
        }

        return saudacao;
    }

    public async Task<string> ProcessarMensagemComContexto(Guid userId, string mensagem, string origem, Guid conversaId)
    {
        // Gerar resumo da conversa (150-200 tokens)
        var resumo = await GerarResumoConversa(conversaId);
        
        // Obter últimas mensagens (4-6 mensagens)
        var historico = await ObterUltimasMensagens(conversaId);
        
        // Construir mensagens para a IA
        var mensagens = new List<object>
        {
            new { role = "system", content = PROMPT_SUPORTE },
            new { role = "system", content = $"contextoIA: {resumo}" }
        };
        
        // Adicionar histórico
        mensagens.AddRange(historico);
        
        // Adicionar mensagem atual do usuário
        mensagens.Add(new { role = "user", content = mensagem });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return "Usuário não encontrado.";

        var resposta = await ChatCompletion(mensagens);
        if (resposta == null) return "Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente.";

        var tokensUsados = resposta.PromptTokens + resposta.CompletionTokens;
        var consumo = await _tokenUsage.TryConsumeTokensAsync(user, tokensUsados, false); // IA sempre pergunta antes de usar StarkCoins
        if (!consumo.Success)
            throw new TokenInsufficientException(consumo.RequiredCoins);

        return resposta.Texto;
    }

    public async Task<EconomicPayload?> ObterEconomiaAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var limite = _planoLimites.ObterLimiteTokensSemana(user);
        var agMax = _planoLimites.ObterLimiteAgendamentos(user);
        var agAtuais = await _context.Agendamentos.CountAsync(a => a.UserId == userId);
        var agRest = agMax == -1 ? -1 : Math.Max(0, agMax - agAtuais);

        return new EconomicPayload(
            user.PlanType.ToString(),
            user.StarkCoinBalance,
            user.TokensConsumidosSemana,
            limite,
            Math.Max(0, limite - user.TokensConsumidosSemana),
            _planoLimites.ExibeAnuncios(user),
            agMax,
            agRest,
            100);
    }
    
    private async Task<string> GerarResumoConversa(Guid conversaId)
    {
        var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        if (conversa == null) return "Nova conversa iniciada.";
        
        var ultimaAcao = await _context.SuporteAcoes
            .Where(a => a.UserId == conversa.UserId && a.Origem == conversa.Origem)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
        
        var resumo = $"Problema inicial: {conversa.ProblemaInicial ?? "Não especificado"}. ";
        
        if (ultimaAcao != null)
        {
            resumo += $"Última ação executada: {ultimaAcao.Acao}. ";
            if (ultimaAcao.Sucesso)
            {
                resumo += "Resultado: sucesso. ";
            }
            else
            {
                resumo += "Resultado: não resolveu. ";
            }
        }
        
        resumo += $"Total de mensagens: {conversa.ContadorMensagens}.";
        
        return resumo;
    }
    
    private async Task<List<object>> ObterUltimasMensagens(Guid conversaId)
    {
        var conversa = await _context.SuporteConversas.FindAsync(conversaId);
        if (conversa == null || string.IsNullOrEmpty(conversa.Mensagens))
            return new List<object>();
        
        try
        {
            var mensagens = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(conversa.Mensagens) ?? new List<Dictionary<string, object>>();
            
            // Pegar últimas 6 mensagens (mas excluir mensagens de sistema como "digitando...")
            var ultimas = mensagens
                .Where(m => 
                {
                    var msg = m.ContainsKey("message") ? m["message"]?.ToString() : "";
                    return !string.IsNullOrEmpty(msg) && 
                           !msg.Contains("digitando") && 
                           !msg.Contains("⏳") && 
                           !msg.Contains("✅");
                })
                .TakeLast(6)
                .ToList();
            
            var resultado = new List<object>();
            foreach (var msg in ultimas)
            {
                var sender = msg.ContainsKey("sender") ? msg["sender"]?.ToString() : "user";
                var message = msg.ContainsKey("message") ? msg["message"]?.ToString() : "";
                
                // Remover [COMANDO:xxx] do histórico para não confundir a IA
                if (!string.IsNullOrEmpty(message))
                {
                    message = System.Text.RegularExpressions.Regex.Replace(message, @"\[COMANDO:[^\]]+\]", "").Trim();
                }
                
                if (sender == "user" && !string.IsNullOrEmpty(message))
                {
                    resultado.Add(new { role = "user", content = message });
                }
                else if (sender == "ia" && !string.IsNullOrEmpty(message))
                {
                    resultado.Add(new { role = "assistant", content = message });
                }
            }
            
            return resultado;
        }
        catch
        {
            return new List<object>();
        }
    }
    
    private async Task<IaSupportResult?> ChatCompletion(List<object> mensagens)
    {
        try
        {
            // Tentar Groq primeiro
            var resultadoGroq = await ChamarGroq(mensagens.ToArray());
            if (resultadoGroq != null)
                return resultadoGroq;
            
            // Se falhar, tentar OpenRouter
            var resultadoOpenRouter = await ChamarOpenRouter(mensagens.ToArray());
            return resultadoOpenRouter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar IA para suporte");
            return null;
        }
    }
    
    private async Task<IaSupportResult?> ChamarGroq(object[] mensagens)
    {
        if (string.IsNullOrEmpty(_groApiKey)) return null;
        
        try
        {
            var requestBody = new
            {
                model = "llama3-8b-8192",
                messages = mensagens,
                max_tokens = 300,
                temperature = 0.7
            };
            
            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_groApiKey}");
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var texto = root.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            var usage = root.TryGetProperty("usage", out var usageEl)
                ? usageEl
                : default;
            var promptTokens = usage.ValueKind != JsonValueKind.Undefined && usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
            var completionTokens = usage.ValueKind != JsonValueKind.Undefined && usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;

            return new IaSupportResult(texto ?? string.Empty, promptTokens, completionTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar Groq");
            return null;
        }
    }
    
    private async Task<IaSupportResult?> ChamarOpenRouter(object[] mensagens)
    {
        if (string.IsNullOrEmpty(_openRouterKey)) return null;
        
        try
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = mensagens,
                max_tokens = 300,
                temperature = 0.7
            };
            
            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_openRouterKey}");
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var texto = root.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            var usage = root.TryGetProperty("usage", out var usageEl)
                ? usageEl
                : default;
            var promptTokens = usage.ValueKind != JsonValueKind.Undefined && usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
            var completionTokens = usage.ValueKind != JsonValueKind.Undefined && usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;

            return new IaSupportResult(texto ?? string.Empty, promptTokens, completionTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar OpenRouter");
            return null;
        }
    }

    public async Task<string> ProcessarMensagem(Guid userId, string mensagem, string origem, Guid? conversaId = null)
    {
        var mensagemOriginal = mensagem.Trim();
        var mensagemLower = mensagemOriginal.ToLower();

        // Verificar se menciona código de erro
        var codigoErroMatch = System.Text.RegularExpressions.Regex.Match(mensagemLower, @"err[_\s]?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (codigoErroMatch.Success)
        {
            var codigo = codigoErroMatch.Groups[1].Value;
            var codigoCompleto = $"ERR_{codigo.PadLeft(3, '0')}";
            return await ProcessarCodigoErro(userId, codigoCompleto, origem);
        }

        // Detectar problemas específicos do software
        var problemaDetectado = await DetectarProblemaEspecifico(userId, mensagemLower, origem, conversaId);
        if (!string.IsNullOrEmpty(problemaDetectado))
        {
            return problemaDetectado;
        }

        // Verificar se está pedindo ajuda com erro
        if (mensagemLower.Contains("erro") || mensagemLower.Contains("problema") || mensagemLower.Contains("não funciona") || mensagemLower.Contains("nao funciona"))
        {
            return await ProcessarSolicitacaoErro(userId, mensagemLower, origem);
        }

        // Verificar se está pedindo para limpar cache/dados
        if (mensagemLower.Contains("limpar") || mensagemLower.Contains("cache") || mensagemLower.Contains("dados"))
        {
            return await ProcessarLimpeza(userId, mensagemLower, origem);
        }

        // Resposta contextual baseada no histórico
        return await ProcessarMensagemContextual(userId, mensagemLower, origem, conversaId);
    }

    private async Task<string> DetectarProblemaEspecifico(Guid userId, string mensagem, string origem, Guid? conversaId)
    {
        // Problema: Comandos Sociais não aparecem (visualização/listagem)
        var temComandoSocial = (mensagem.Contains("comando") && mensagem.Contains("social")) || 
                               (mensagem.Contains("comandos") && mensagem.Contains("sociais"));
        var temNaoAparece = mensagem.Contains("não aparece") || mensagem.Contains("nao aparece") || 
                           mensagem.Contains("não aparecem") || mensagem.Contains("nao aparecem") ||
                           mensagem.Contains("não aparece na") || mensagem.Contains("nao aparece na") ||
                           mensagem.Contains("não aparece na tela") || mensagem.Contains("nao aparece na tela") ||
                           mensagem.Contains("não aparece na lista") || mensagem.Contains("nao aparece na lista") ||
                           mensagem.Contains("criei") && (mensagem.Contains("não aparece") || mensagem.Contains("nao aparece"));
        
        // Problema: Comandos Sociais não respondem (execução)
        var temNaoResponde = mensagem.Contains("não está respondendo") || mensagem.Contains("nao esta respondendo") ||
                            mensagem.Contains("não responde") || mensagem.Contains("nao responde") ||
                            mensagem.Contains("não recebendo") || mensagem.Contains("nao recebendo") ||
                            mensagem.Contains("não recebo") || mensagem.Contains("nao recebo");
        
        if (temComandoSocial && temNaoAparece)
        {
            // Verificar se já tentou alguma solução
            var ultimaAcao = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcao == null || ultimaAcao.Acao != "atualizardados")
            {
                return "Entendi! Você está com problema na tela de Comandos Sociais - os comandos que você salvou não estão aparecendo na lista.\n\n" +
                       "Isso geralmente acontece quando os dados não foram sincronizados corretamente. Vou atualizar os dados agora para sincronizar com o servidor.\n\n" +
                       "[COMANDO:atualizardados]";
            }
            else if (ultimaAcao.Acao == "atualizardados" && !ultimaAcao.Sucesso)
            {
                return "Já tentamos atualizar os dados. Vou limpar o cache agora, pois pode haver dados antigos em cache impedindo a exibição.\n\n" +
                       "[COMANDO:limparcache]";
            }
            else
            {
                return "Já tentamos atualizar os dados e limpar o cache. Vou verificar algumas possibilidades:\n\n" +
                       "1. Verifique se você está conectado à internet\n" +
                       "2. Confirme se os comandos foram salvos corretamente (você viu a mensagem de sucesso ao salvar?)\n" +
                       "3. Tente fechar e reabrir a tela de Comandos Sociais\n\n" +
                       "Se ainda não aparecer, vou fazer logout para limpar completamente a sessão.\n\n" +
                       "⚠️ Você será desconectado e precisará fazer login novamente.\n\n" +
                       "[COMANDO:logout]";
            }
        }
        
        // Problema: Comandos Sociais não respondem (execução)
        if (temComandoSocial && temNaoResponde)
        {
            var ultimaAcao = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (ultimaAcao == null || ultimaAcao.Acao != "limparcache")
            {
                return "Entendi! Os comandos sociais não estão respondendo quando você os executa, certo?\n\n" +
                       "Isso pode ser problema de cache ou conexão. Vou limpar o cache primeiro.\n\n" +
                       "[COMANDO:limparcache]";
            }
            else if (ultimaAcao.Acao == "limparcache" && !ultimaAcao.Sucesso)
            {
                return "Já tentamos limpar o cache. Vou atualizar os dados agora para sincronizar com o servidor.\n\n" +
                       "[COMANDO:atualizardados]";
            }
            else
            {
                return "Já tentamos limpar o cache e atualizar os dados. Vou verificar algumas possibilidades:\n\n" +
                       "1. Verifique se o WebSocket está conectado (veja o status no Dashboard)\n" +
                       "2. Confirme se você está conectado à internet\n" +
                       "3. Tente executar um comando novamente\n\n" +
                       "Se ainda não funcionar, vou fazer logout para limpar completamente a sessão.\n\n" +
                       "⚠️ Você será desconectado e precisará fazer login novamente.\n\n" +
                       "[COMANDO:logout]";
            }
        }

        // Problema: Dispositivos não aparecem
        if ((mensagem.Contains("dispositivo") || mensagem.Contains("device")) && 
            (mensagem.Contains("não aparece") || mensagem.Contains("nao aparece") || mensagem.Contains("não aparecem") || mensagem.Contains("nao aparecem")))
        {
            return "Entendi! Os dispositivos não estão aparecendo na tela. Isso geralmente é problema de sincronização.\n\n" +
                   "Vou atualizar os dados para sincronizar com o servidor.\n\n" +
                   "[COMANDO:atualizardados]";
        }

        // Problema: Dashboard não carrega
        if (mensagem.Contains("dashboard") && (mensagem.Contains("não carrega") || mensagem.Contains("nao carrega") || mensagem.Contains("não aparece") || mensagem.Contains("nao aparece")))
        {
            return "O Dashboard não está carregando? Vou limpar o cache primeiro, pois pode haver dados antigos.\n\n" +
                   "[COMANDO:limparcache]";
        }

        return null; // Nenhum problema específico detectado
    }

    private async Task<string> ProcessarMensagemContextual(Guid userId, string mensagem, string origem, Guid? conversaId)
    {
        // Se temos conversa, buscar histórico
        if (conversaId.HasValue)
        {
            var conversa = await _context.SuporteConversas.FindAsync(conversaId.Value);
            if (conversa != null && !string.IsNullOrEmpty(conversa.Mensagens))
            {
                try
                {
                    var mensagens = JsonSerializer.Deserialize<List<object>>(conversa.Mensagens) ?? new List<object>();
                    var ultimasMensagens = mensagens.TakeLast(4).ToList();
                    
                    // Verificar se já foi mencionado que não tem código de erro
                    var jaMencionouSemCodigo = mensagens.Any(m => 
                    {
                        var msgJson = JsonSerializer.Serialize(m);
                        var msgDict = JsonSerializer.Deserialize<Dictionary<string, object>>(msgJson);
                        if (msgDict != null && msgDict.ContainsKey("sender") && msgDict["sender"]?.ToString() == "user")
                        {
                            var msgText = msgDict.ContainsKey("message") ? msgDict["message"]?.ToString()?.ToLower() : "";
                            return msgText != null && (msgText.Contains("não tenho") || msgText.Contains("nao tenho") || msgText.Contains("não tem") || msgText.Contains("nao tem"));
                        }
                        return false;
                    });

                    if (jaMencionouSemCodigo)
                    {
                        // Já mencionou que não tem código, focar no problema descrito
                        // Mas não perguntar coisas óbvias - se mencionou "comandos sociais", já sabe que é na tela de Comandos Sociais
                        var temComandoSocial = mensagem.Contains("comando") && mensagem.Contains("social");
                        var temDispositivo = mensagem.Contains("dispositivo") || mensagem.Contains("device");
                        var temDashboard = mensagem.Contains("dashboard");
                        
                        if (temComandoSocial)
                        {
                            // Já sabe que é Comandos Sociais, detectar se é visualização ou execução
                            return await DetectarProblemaEspecifico(userId, mensagem, origem, conversaId) ?? 
                                   "Entendi! Você está com problema nos Comandos Sociais. Me diga: os comandos não aparecem na lista ou não estão respondendo quando você executa?";
                        }
                        else if (temDispositivo)
                        {
                            return "Entendi! Você está com problema nos Dispositivos. Me diga: os dispositivos não aparecem na lista ou não estão respondendo aos comandos?";
                        }
                        else if (temDashboard)
                        {
                            return "Entendi! Você está com problema no Dashboard. O que exatamente não está funcionando?";
                        }
                        else if (mensagem.Contains("não aparece") || mensagem.Contains("nao aparece"))
                        {
                            return "Entendi! Você está com problema de algo não aparecer na tela. Me diga especificamente:\n\n" +
                                   "• É na tela de Comandos Sociais?\n" +
                                   "• É na tela de Dispositivos?\n" +
                                   "• É no Dashboard?\n\n" +
                                   "Com essa informação, vou aplicar a solução correta!";
                        }
                    }
                }
                catch { }
            }
        }

        // Resposta padrão mais útil
        return "Entendi! Para te ajudar melhor, me diga:\n\n" +
               "• Onde está o problema? (Comandos Sociais, Dispositivos, Dashboard, etc.)\n" +
               "• O que exatamente não está funcionando?\n" +
               "• Quando começou a acontecer?\n\n" +
               "Com essas informações, vou aplicar a solução mais adequada! 😊";
    }

    private async Task<string> ProcessarCodigoErro(Guid userId, string codigo, string origem)
    {
        var errorCode = await _context.ErrorCodeDescriptions
            .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && 
                                     (e.Origem == origem || string.IsNullOrEmpty(e.Origem)));

        if (errorCode == null)
        {
            return $"Não encontrei informações sobre o código de erro {codigo}. " +
                   "Por favor, descreva o problema que está enfrentando para que eu possa ajudá-lo melhor.";
        }

        var solucoes = new List<string>();
        if (!string.IsNullOrEmpty(errorCode.Solucoes))
        {
            try
            {
                solucoes = JsonSerializer.Deserialize<List<string>>(errorCode.Solucoes) ?? new List<string>();
            }
            catch
            {
                solucoes = new List<string> { errorCode.Solucoes };
            }
        }

        // Filtrar soluções inviáveis para usuário final
        solucoes = await FiltrarSolucoes(solucoes, origem);

        if (solucoes.Count == 0)
        {
            return $"Encontrei o código de erro {codigo}, mas não há soluções automáticas disponíveis. " +
                   "Vou transferir você para o suporte humano.";
        }

        var resposta = $"Código de Erro: {codigo}\n";
        resposta += $"Descrição: {errorCode.Descricao}\n\n";
        resposta += "Soluções sugeridas:\n";
        for (int i = 0; i < solucoes.Count; i++)
        {
            resposta += $"{i + 1}. {solucoes[i]}\n";
        }

        resposta += "\nVou tentar aplicar algumas soluções automáticas agora...";

        // Tentar aplicar soluções automáticas
        await TentarAplicarSolucoes(userId, solucoes, origem);

        return resposta;
    }

    private async Task<string> ProcessarSolicitacaoErro(Guid userId, string mensagem, string origem)
    {
        // Buscar últimos erros do usuário
        if (origem == "software")
        {
            var ultimosErros = await _context.ErrorLogsSoft
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            if (ultimosErros.Any())
            {
                var ultimoErro = ultimosErros.First();
                if (!string.IsNullOrEmpty(ultimoErro.CodigoDeErro))
                {
                    return await ProcessarCodigoErro(userId, ultimoErro.CodigoDeErro, origem);
                }
            }
        }
        else
        {
            var ultimosErros = await _context.ErrorLogsApp
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            if (ultimosErros.Any())
            {
                var ultimoErro = ultimosErros.First();
                if (!string.IsNullOrEmpty(ultimoErro.CodigoDeErro))
                {
                    return await ProcessarCodigoErro(userId, ultimoErro.CodigoDeErro, origem);
                }
            }
        }

        // Resposta mais conversacional - não pedir código de erro se já foi mencionado que não tem
        if (mensagem.Contains("não tenho") || mensagem.Contains("nao tenho") || mensagem.Contains("não tem") || mensagem.Contains("nao tem"))
        {
            return "Sem problemas! Me conte o que está acontecendo. " +
                   "Por exemplo: algo não está aparecendo? Alguma funcionalidade não está funcionando? " +
                   "Descreva o problema e eu vou te ajudar a resolver.";
        }

        return "Me conte mais sobre o problema. O que exatamente não está funcionando? " +
               "Por exemplo: algo não aparece na tela? Alguma funcionalidade não responde? " +
               "Quanto mais detalhes você me der, melhor posso te ajudar!";
    }

    private async Task<string> ProcessarLimpeza(Guid userId, string mensagem, string origem)
    {
        if (mensagem.Contains("cache"))
        {
            // Aqui você chamaria o endpoint de limpar cache
            return "Vou limpar o cache agora. Isso pode ajudar a resolver alguns problemas. " +
                   "Por favor, aguarde alguns instantes e tente novamente.";
        }

        if (mensagem.Contains("dados"))
        {
            return "Limpar dados remove informações temporárias e logs. " +
                   "Isso não afetará suas configurações principais. Deseja continuar?";
        }

        return "Posso ajudar a limpar cache ou dados. O que você gostaria de limpar?";
    }

    private async Task TentarAplicarSolucoes(Guid userId, List<string> solucoes, string origem)
    {
        foreach (var solucao in solucoes)
        {
            var solucaoLower = solucao.ToLower();

            if (solucaoLower.Contains("limpar cache") || solucaoLower.Contains("limpar o cache"))
            {
                // Chamar endpoint de limpar cache
                _logger.LogInformation("Aplicando solução: limpar cache para usuário {UserId}", userId);
            }

            if (solucaoLower.Contains("reiniciar") || solucaoLower.Contains("recarregar"))
            {
                // Chamar endpoint de reiniciar sessão
                _logger.LogInformation("Aplicando solução: reiniciar sessão para usuário {UserId}", userId);
            }
        }
    }

    public async Task<List<string>> FiltrarSolucoes(List<string> solucoes, string origem)
    {
        var solucoesFiltradas = new List<string>();

        foreach (var solucao in solucoes)
        {
            var solucaoLower = solucao.ToLower();

            // Remover soluções que usuário final não pode executar
            if (solucaoLower.Contains("dll") || 
                solucaoLower.Contains("verificar dependências") ||
                solucaoLower.Contains("compilar") ||
                solucaoLower.Contains("código fonte"))
            {
                continue; // Pular soluções técnicas demais
            }

            // Manter soluções que usuário pode executar
            if (solucaoLower.Contains("reiniciar") ||
                solucaoLower.Contains("limpar cache") ||
                solucaoLower.Contains("verificar conexão") ||
                solucaoLower.Contains("tentar novamente"))
            {
                solucoesFiltradas.Add(solucao);
            }
            else
            {
                // Adicionar outras soluções genéricas
                solucoesFiltradas.Add(solucao);
            }
        }

        return solucoesFiltradas;
    }
}
