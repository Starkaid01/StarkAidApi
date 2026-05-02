using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.V1.Support;
using StarkAid.Api.Services.V1.Support.Heuristics;
using StarkAid.Api.Services.V1.Support.Learning;
using StarkAid.Api.Services.V1.SuperIA;
using StarkAid.Api.Services.V1.Support.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace StarkAid.Api.Services.V1.Support.Agents;

public interface ISupportMessageRouter
{
    Task<SupportMessage> ProcessMessageAsync(Guid userId, string text, string origem);
}

public class SupportMessageRouter : ISupportMessageRouter
{
    private static readonly ConcurrentDictionary<Guid, SupportConversation> _conversations = new();
    private readonly ISupportHeuristicService _heuristicService;
    private readonly ISupportLearningService _learningService;
    private readonly IaService _iaService;
    private readonly ILogger<SupportMessageRouter> _logger;

    public SupportMessageRouter(
        ISupportHeuristicService heuristicService,
        ISupportLearningService learningService,
        IaService iaService,
        ILogger<SupportMessageRouter> logger)
    {
        _heuristicService = heuristicService;
        _learningService = learningService;
        _iaService = iaService;
        _logger = logger;
    }

    public async Task<SupportMessage> ProcessMessageAsync(Guid userId, string text, string origem)
    {
        var conv = _conversations.GetOrAdd(userId, id => new SupportConversation { 
            Id = Guid.NewGuid(),
            UserId = id,
            CurrentStage = "Idle",
            AttemptedActions = new List<string>()
        });

        var textClean = text.Trim();
        var textLower = textClean.ToLower();

        // 1. CLASSIFICAÇÃO DE FEEDBACK (REGEX PORTUGUÊS REAL)
        var feedback = ClassifyFeedback(textLower);
        _logger.LogInformation($"[BRAIN] User: {userId} | Stage: {conv.CurrentStage} | Feedback: {feedback} | Input: {textClean}");

        // 2. REGRA DE OURO: SE "NÃO RESOLVEU" -> ESCALAR IMEDIATAMENTE
        if (conv.CurrentStage == "WaitingForFeedback" || conv.CurrentStage == "ExecutingAction")
        {
            if (feedback == SupportFeedback.Negative)
            {
                _logger.LogWarning($"[BRAIN] User {userId} reported failure. Escalating diagnostic ladder.");
                return await EscalateDiagnosticAsync(conv, userId, origem);
            }
            
            if (feedback == SupportFeedback.Positive)
            {
                conv.CurrentStage = "Resolved";
                conv.AttemptedActions.Clear();
                return new SupportMessage 
                { 
                    UserId = userId, 
                    Type = SupportMessageType.AssistantResponse, 
                    Content = "Excelente! Fico muito feliz que funcionou. Se precisar de algo mais, é só me chamar. 😊", 
                    ContextTitle = conv.ContextTitle 
                };
            }
        }

        // 3. TRATAMENTO DE CONFIRMAÇÃO (WaitingForConfirmation)
        if (conv.CurrentStage == "WaitingForConfirmation" && !string.IsNullOrEmpty(conv.PendingAction))
        {
            if (feedback == SupportFeedback.Confirmation || feedback == SupportFeedback.Positive)
            {
                var action = conv.PendingAction;
                conv.AttemptedActions.Add(action);
                conv.PendingAction = null;
                conv.CurrentStage = "WaitingForFeedback";

                return new SupportMessage 
                { 
                    UserId = userId, 
                    Type = SupportMessageType.AgentActionResult, 
                    Content = GetExecutionText(action), 
                    ContextTitle = conv.ContextTitle,
                    ActionToExecute = action 
                };
            }
            else if (feedback == SupportFeedback.Denial)
            {
                conv.PendingAction = null;
                conv.CurrentStage = "Idle";
                return new SupportMessage 
                { 
                    UserId = userId, 
                    Type = SupportMessageType.AssistantResponse, 
                    Content = "Entendido, não executei a ação. Quer tentar descrever o problema de outra forma ou prefere falar com um humano?", 
                    ContextTitle = conv.ContextTitle 
                };
            }
        }

        // 4. NOVO DIAGNÓSTICO / MENSAGEM DO USUÁRIO
        conv.AddMessage(new SupportMessage { UserId = userId, Content = textClean, Type = SupportMessageType.UserInput });

        // 4.1 HEURÍSTICA DE COMANDOS QUE SUMIRAM (ESCALONAMENTO REAIS)
        if (textLower.Contains("comando") && (textLower.Contains("aparece") || textLower.Contains("sumiu") || textLower.Contains("editar") || textLower.Contains("carrega") || textLower.Contains("sumiram")))
        {
            conv.ContextTitle = "Comandos não aparecem";
            // Ladder para este problema específico
            return await ProposeNextActionAsync(conv, userId, origem, new List<string> { "CleanAppCache", "CleanLocalDatabase", "CleanAppData", "RestartApp", "Logout" });
        }

        // 4.2 DEMAIS HEURÍSTICAS
        var heuristic = await _heuristicService.EvaluateAsync(userId, textClean, origem);
        if (heuristic != null)
        {
            if (string.IsNullOrEmpty(heuristic.ActionToPropose) || !conv.AttemptedActions.Contains(heuristic.ActionToPropose))
            {
                if (!string.IsNullOrEmpty(heuristic.ActionToPropose))
                {
                    conv.PendingAction = heuristic.ActionToPropose;
                    conv.CurrentStage = "WaitingForConfirmation";
                }

                return new SupportMessage 
                { 
                    UserId = userId, 
                    Type = !string.IsNullOrEmpty(heuristic.ActionToPropose) ? SupportMessageType.AgentActionProposal : SupportMessageType.AssistantResponse, 
                    Content = heuristic.Message, 
                    ContextTitle = conv.ContextTitle ?? "Diagnóstico Heurístico",
                    ActionProposed = heuristic.ActionToPropose
                };
            }
        }

        // 4.3 APRENDIZADO
        var learned = await _learningService.GetLearnedResponseAsync(textClean, conv.ContextTitle, userId);
        if (learned != null)
        {
            return new SupportMessage { UserId = userId, Type = SupportMessageType.AssistantResponse, Content = learned, ContextTitle = conv.ContextTitle };
        }

        // 4.4 IA (ÚLTIMA INSTÂNCIA)
        var aiDiagnostic = await RunRealDiagnosticAiAsync(conv, textClean);
        
        if (!string.IsNullOrEmpty(aiDiagnostic.ProposedAction) && !conv.AttemptedActions.Contains(aiDiagnostic.ProposedAction))
        {
            conv.PendingAction = aiDiagnostic.ProposedAction;
            conv.CurrentStage = "WaitingForConfirmation";
            conv.ContextTitle = aiDiagnostic.ContextTitle ?? conv.ContextTitle;

            return new SupportMessage 
            { 
                UserId = userId, 
                Type = SupportMessageType.AgentActionProposal, 
                Content = aiDiagnostic.MessageToUser, 
                ContextTitle = conv.ContextTitle,
                ActionProposed = aiDiagnostic.ProposedAction
            };
        }

        return new SupportMessage 
        { 
            UserId = userId, 
            Type = SupportMessageType.AssistantResponse, 
            Content = aiDiagnostic.MessageToUser, 
            ContextTitle = conv.ContextTitle 
        };
    }

    private async Task<SupportMessage> EscalateDiagnosticAsync(SupportConversation conv, Guid userId, string origem)
    {
        // Ladder Geral
        var ladder = new List<string> { "CleanAppCache", "CleanLocalDatabase", "RestartApp", "CleanAppData", "Logout" };
        return await ProposeNextActionAsync(conv, userId, origem, ladder);
    }

    private async Task<SupportMessage> ProposeNextActionAsync(SupportConversation conv, Guid userId, string origem, List<string> ladder)
    {
        string? next = ladder.FirstOrDefault(a => !conv.AttemptedActions.Contains(a));

        if (next == null)
        {
            conv.CurrentStage = "Escalated";
            return new SupportMessage 
            { 
                UserId = userId, 
                Type = SupportMessageType.AssistantResponse, 
                Content = "Sinto muito, já tentei todos os procedimentos automáticos de reparo disponíveis (limpeza de cache, banco de dados e reinicialização) e o problema persiste. Vou encaminhar sua conversa agora para um suporte humano analisar.", 
                ContextTitle = conv.ContextTitle 
            };
        }

        conv.PendingAction = next;
        conv.CurrentStage = "WaitingForConfirmation";

        string explanation = next switch {
            "CleanAppCache" => "Entendi. Vou começar com um procedimento básico: limpar o cache do aplicativo para resolver conflitos de dados temporários. Posso prosseguir?",
            "CleanLocalDatabase" => "Como a limpeza simples não resolveu, o próximo passo é reconstruir o banco de dados local. Isso forçará o app a baixar todos os seus comandos novamente do servidor. Posso fazer isso?",
            "RestartApp" => "Os dados parecem estar corretos, mas o processamento pode ter travado. Recomendo reiniciarmos o aplicativo remotamente agora. O que acha?",
            "CleanAppData" => "O erro é persistente. Vou precisar realizar uma limpeza total dos dados (Hard Reset). Atenção: isso limpará suas configurações locais e exigirá um novo login. Podemos tentar?",
            "Logout" => "Já esgotei as manutenções técnicas. O passo final é forçar o logout da sua conta para invalidar tokens de sessão antigos. Deseja que eu faça isso?",
            _ => "Vou tentar um procedimento de manutenção avançada para resolver este problema. Posso iniciar?"
        };

        return new SupportMessage 
        { 
            UserId = userId, 
            Type = SupportMessageType.AgentActionProposal, 
            Content = explanation, 
            ContextTitle = conv.ContextTitle,
            ActionProposed = next
        };
    }

    private SupportFeedback ClassifyFeedback(string text)
    {
        // REGEX PORTUGUÊS REAL (STRICT)
        if (Regex.IsMatch(text, @"\b(ainda não|ainda nao|não resolveu|nao resolveu|continua igual|mesma coisa|não mudou nada|continua o erro|nada feito|não deu|continua do mesmo jeito)\b")) return SupportFeedback.Negative;
        if (Regex.IsMatch(text, @"\b(sim|pode|claro|com certeza|autorizo|bora|vai|ok|perfeito|aceito|faça|faca)\b")) return SupportFeedback.Confirmation;
        if (Regex.IsMatch(text, @"\b(não|nao|recuso|agora não|não quero|cancelar|para|pare)\b")) return SupportFeedback.Denial;
        if (Regex.IsMatch(text, @"\b(resolveu|funcionou|obrigado|vlw|valeu|arrumou|parou o erro)\b")) return SupportFeedback.Positive;
        
        return SupportFeedback.Neutral;
    }

    private string GetExecutionText(string action)
    {
        return action switch {
            "CleanAppCache" => "Limpando cache... Aguarde um momento enquanto finalizo o processo.",
            "CleanLocalDatabase" => "Sincronizando banco de dados... Seus comandos estão sendo recarregados do servidor.",
            "RestartApp" => "Reiniciando o aplicativo... Você verá a tela de carregamento em breve.",
            "CleanAppData" => "Limpando todos os dados locais... O aplicativo será resetado para o estado original.",
            "Logout" => "Realizando logout... Sua sessão foi encerrada com segurança.",
            _ => "Iniciando manutenção técnica..."
        };
    }

    private async Task<DiagnosticResult> RunRealDiagnosticAiAsync(SupportConversation conv, string userText)
    {
        var messages = conv.Messages.Select(m => new { 
            role = (m.Type == SupportMessageType.UserInput) ? "user" : "assistant",
            content = m.Content
        }).ToList();

        var tried = string.Join(", ", conv.AttemptedActions);
        
        var prompt = $@"Você é o Especialista de Suporte StarkAid.
Sua missão: Diagnóstico técnico humano, empático e resolutivo.

DIRETRIZES:
1. NUNCA diga 'tente ser mais direto'.
2. NUNCA diga 'estou aprendendo'.
3. Se já tentamos [{tried}] e falhou, VOCÊ DEVE PROPOR UMA NOVA AÇÃO da lista abaixo.
4. Explique o PORQUÊ da ação proposta de forma técnica.

LISTA DE AÇÕES:
- CleanAppCache: UI/Sincronização.
- CleanLocalDatabase: Comandos sumiram/erro de edição.
- RestartApp: Travamentos/Lentidão.
- CleanAppData: Erros persistentes.
- Logout: Problemas de autenticação.

FORMATO (JSON APENAS):
{{
  ""MessageToUser"": ""Explicação técnica + Pedido de confirmação"",
  ""ProposedAction"": ""NomeDaAção ou null"",
  ""ContextTitle"": ""Contexto"",
  ""IsFinalResponse"": true
}}";

        var reqMessages = new List<object> { new { role = "system", content = prompt } };
        foreach(var h in messages) reqMessages.Add(h);
        
        var result = await _iaService.ChamarOpenRouter(reqMessages.ToArray());
        
        if (result == null || string.IsNullOrEmpty(result.Texto))
            return new DiagnosticResult { MessageToUser = "Pode me dar mais detalhes do que está ocorrendo para eu buscar a ferramenta certa?" };

        try {
            var jsonStr = result.Texto;
            if (jsonStr.Contains("```json")) jsonStr = jsonStr.Split("```json")[1].Split("```")[0].Trim();
            else if (jsonStr.Contains("```")) jsonStr = jsonStr.Split("```")[1].Split("```")[0].Trim();

            return JsonSerializer.Deserialize<DiagnosticResult>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                   ?? new DiagnosticResult { MessageToUser = result.Texto };
        } catch {
            return new DiagnosticResult { MessageToUser = result.Texto };
        }
    }

    private class DiagnosticResult
    {
        public string MessageToUser { get; set; } = string.Empty;
        public string? ProposedAction { get; set; }
        public string? ContextTitle { get; set; }
        public bool IsFinalResponse { get; set; }
    }
}
