using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Suporte
{
    public enum SupportIntent
    {
        Question_Economy, 
        UsageHelp_Ewelink, 
        UsageHelp_AssistantName, 
        MaintenanceIssue,
        AccountData,
        Greeting,
        MaintenanceConfirmation,
        MaintenanceDenial,
        Issue_SocialCmd,
        Issue_Device,
        Unknown
    }

    public class AgentDecision
    {
        public SupportIntent Intent { get; set; }
        public string ResponseToUser { get; set; }
        public bool ShouldRequestMaintenance { get; set; }
        public bool ShouldExecuteMaintenance { get; set; }
        public string MaintenanceAction { get; set; } // "ClearCache", "ClearData", "Restart", "DropDB"
    }

    public class SupportAgentEngine
    {
        private readonly ILogger<SupportAgentEngine> _logger;
        private readonly AppDbContext _context;

        // Sequence of actions to try for general issues
        private readonly List<string> _escalationPath = new() { "ClearCache", "Restart", "ClearData" };

        public SupportAgentEngine(ILogger<SupportAgentEngine> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<AgentDecision> DecideNextStepAsync(ConversationContext context, string userMessage)
        {
            var intent = ClassifyIntent(userMessage);
            _logger.LogInformation($"[SupportAgent] User: '{userMessage}' -> Intent: {intent} | Stage: {context.CurrentStage}");

            // 1. Handling Active Diagnostics (User is inside a troubleshooting flow)
            if (context.CurrentStage == SupportStage.WaitingForActionConfirmation)
            {
                if (intent == SupportIntent.MaintenanceConfirmation)
                {
                    // User said YES. Execute the pending action.
                    var action = context.PendingAction;
                    
                    // Mark as attempted
                    context.AttemptedActions.Add(action);
                    
                    // Move to feedback stage
                    context.CurrentStage = SupportStage.WaitingForActionFeedback;
                    context.MaintenanceConsentRequested = false;

                    return new AgentDecision {
                        Intent = intent,
                        ShouldExecuteMaintenance = true,
                        MaintenanceAction = action,
                        ResponseToUser = GetExecutionMessage(action)
                    };
                }
                else if (intent == SupportIntent.MaintenanceDenial)
                {
                    // User said NO. Skip to next step or give up.
                    return await EscalateAsync(context, "Entendido. Se preferir não fazer isso agora, tem mais algo que eu possa ajudar?");
                }
            }

            if (context.CurrentStage == SupportStage.WaitingForActionFeedback)
            {
                // User is reporting back after we did something (like Clear Cache)
                if (intent == SupportIntent.MaintenanceDenial || userMessage.ToLower().Contains("não") || userMessage.ToLower().Contains("igual") || userMessage.ToLower().Contains("mesma coisa") || userMessage.ToLower().Contains("nada"))
                {
                    // It didn't work. Escalate!
                    return await EscalateAsync(context, "Entendi que o problema persiste.");
                }
                else if (intent == SupportIntent.MaintenanceConfirmation || userMessage.ToLower().Contains("resolveu") || userMessage.ToLower().Contains("funcionou") || userMessage.ToLower().Contains("obrigado"))
                {
                    // Success!
                    context.CurrentStage = SupportStage.Idle;
                    context.AttemptedActions.Clear();
                    return new AgentDecision {
                        ResponseToUser = "Fico feliz em ter ajudado! Se precisar de mais alguma coisa, estou por aqui."
                    };
                }
            }

            // 2. Start New Flows
            if (intent == SupportIntent.Issue_SocialCmd)
            {
                return await DiagnoseSocialCommand(context);
            }
            
            if (intent == SupportIntent.Issue_Device)
            {
                return await InitializeMaintenanceFlow(context, "ClearCache", "Problemas com dispositivos geralmente são resolvidos atualizando o cache.");
            }

            if (intent == SupportIntent.MaintenanceIssue)
            {
                return await EscalateAsync(context, "Parece um problema técnico.");
            }

            // 3. Normal Q&A
            return await BuildResponseAsync(context, intent);
        }

        private async Task<AgentDecision> EscalateAsync(ConversationContext context, string reasonPrefix)
        {
            // Find next action in escalation path that hasn't been tried
            var nextAction = _escalationPath.FirstOrDefault(a => !context.AttemptedActions.Contains(a));

            if (nextAction == null)
            {
                // End of line -> Human Support
                context.CurrentStage = SupportStage.Escalated;
                return new AgentDecision {
                    ResponseToUser = $"{reasonPrefix} Já tentamos as soluções automáticas (Cache, Dados, etc) e não resolveu. Vou transferir seu caso para nossos especialistas técnicos. Aguarde um momento..."
                };
            }

            // Propose next action
            context.CurrentStage = SupportStage.WaitingForActionConfirmation;
            context.PendingAction = nextAction;
            context.MaintenanceConsentRequested = true;

            string proposal = "";
            switch (nextAction)
            {
                case "ClearCache": 
                    proposal = "Vou começar limpando o cache temporário. Isso resolve a maioria das falhas de sincronização. Posso prosseguir?"; 
                    break;
                case "Restart": 
                    proposal = "O próximo passo é reiniciar o aplicativo completamente para recarregar as configurações. O app irá fechar e abrir novamente. Autoriza?"; 
                    break;
                case "ClearData": 
                    proposal = "Como as opções anteriores não funcionaram, a solução recomendada é limpar os dados locais do app. IMPORTANTE: Você precisará fazer login novamente. Posso realizar esse procedimento?"; 
                    break;
            }

            return new AgentDecision {
                ResponseToUser = $"{reasonPrefix} {proposal}",
                ShouldRequestMaintenance = true
            };
        }

        private async Task<AgentDecision> InitializeMaintenanceFlow(ConversationContext context, string action, string reason)
        {
            if (context.AttemptedActions.Contains(action)) return await EscalateAsync(context, "Isso não funcionou antes.");

            context.CurrentStage = SupportStage.WaitingForActionConfirmation;
            context.PendingAction = action;
            context.MaintenanceConsentRequested = true;
            
            return new AgentDecision {
                ResponseToUser = $"{reason} Posso executar essa ação agora?",
                ShouldRequestMaintenance = true
            };
        }

        private async Task<AgentDecision> DiagnoseSocialCommand(ConversationContext context)
        {
            Guid userId;
            if (!Guid.TryParse(context.UserId, out userId)) return new AgentDecision { ResponseToUser = "Erro interno de identificação." };

            var commands = await _context.ComandosSociais.Where(c => c.UserId == userId).ToListAsync();
            
            // 1. Check for bad data
            var corrupt = commands.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Comando) || string.IsNullOrWhiteSpace(c.Resposta));
            if (corrupt != null)
            {
                _context.ComandosSociais.Remove(corrupt);
                await _context.SaveChangesAsync();
                return new AgentDecision { ResponseToUser = "Identifiquei um comando salvo incorretamente (em branco) e o removi. Por favor, tente criar novamente." };
            }

            // 2. Propose Cache Clear
            if (commands.Any() && !context.AttemptedActions.Contains("ClearCache"))
            {
                 context.CurrentStage = SupportStage.WaitingForActionConfirmation;
                 context.PendingAction = "ClearCache";
                 context.MaintenanceConsentRequested = true;

                 var lastCmd = commands.LastOrDefault(); 
                 var cmdName = lastCmd?.Comando ?? "comando";

                 return new AgentDecision {
                     ResponseToUser = $"Localizei o comando '{cmdName}' no banco de dados. Se ele não aparece para você, é conflito de cache. Posso limpar o cache para atualizar sua lista?",
                     ShouldRequestMaintenance = true
                 };
            }
            
            if (!commands.Any())
            {
                return new AgentDecision { ResponseToUser = "Não encontrei nenhum comando ativo na sua conta. Tem certeza que ele foi salvo? Tente criar novamente." };
            }

            // If we are here, we have commands but Cache didn't fix it (or was already tried)
            return await EscalateAsync(context, "Se o comando existe mas ainda não aparece...");
        }

        private string GetExecutionMessage(string action)
        {
            switch (action)
            {
                case "ClearCache": return "Limpando cache... Um momento.";
                case "Restart": return "Reiniciando aplicação...";
                case "ClearData": return "Limpando dados e reiniciando...";
                case "DropDB": return "Resetando banco local...";
                default: return "Executando...";
            }
        }

        public SupportIntent ClassifyIntent(string message)
        {
            var lowerMsg = message.ToLower().Trim();

            // Broad Consent Matching
            if (Regex.IsMatch(lowerMsg, @"\b(sim|pode|claro|com certeza|autorizo|vai|manda ver|faça|ok|tá bom)\b"))
                return SupportIntent.MaintenanceConfirmation;
            
            if (Regex.IsMatch(lowerMsg, @"\b(não|nao|cancelar|para|jamais|nem pensar|esquece)\b"))
                return SupportIntent.MaintenanceDenial;

            // Broad Greetings
            if (Regex.IsMatch(lowerMsg, @"\b(olá|ola|oi|bom dia|boa tarde|boa noite|e ai|e aí)\b"))
                return SupportIntent.Greeting;
            
            // Topics
            if (lowerMsg.Contains("comando") || lowerMsg.Contains("social") || lowerMsg.Contains("frase") || lowerMsg.Contains("editar")) return SupportIntent.Issue_SocialCmd;
            if (lowerMsg.Contains("dispositivo") || lowerMsg.Contains("luz") || lowerMsg.Contains("tv")) return SupportIntent.Issue_Device; 
            
            if (lowerMsg.Contains("senha") || lowerMsg.Contains("resetar")) return SupportIntent.AccountData;
            if (lowerMsg.Contains("ewelink")) return SupportIntent.UsageHelp_Ewelink;
            if (lowerMsg.Contains("starkcoins") || lowerMsg.Contains("saldo")) return SupportIntent.Question_Economy;

            if (lowerMsg.Contains("travou") || lowerMsg.Contains("bug") || lowerMsg.Contains("problema") || lowerMsg.Contains("erro") || lowerMsg.Contains("aparece") || lowerMsg.Contains("sumiu") || lowerMsg.Contains("não consigo"))
                return SupportIntent.MaintenanceIssue;

            return SupportIntent.Unknown;
        }

        private async Task<AgentDecision> BuildResponseAsync(ConversationContext context, SupportIntent intent)
        {
             var decision = new AgentDecision { Intent = intent };
             switch (intent)
             {
                 case SupportIntent.Greeting:
                     decision.ResponseToUser = "Olá! Sou seu suporte inteligente. Posso ajudar com dúvidas, problemas técnicos ou configurações.";
                     break;
                 case SupportIntent.AccountData:
                     decision.ResponseToUser = "Alterações de conta/senha devem ser feitas na tela de login (opção 'Esqueci senha') ou no menu Perfil.";
                     break;
                 case SupportIntent.UsageHelp_Ewelink:
                     decision.ResponseToUser = "Para gerenciar o eWeLink, acesse Configurações > Dispositivos. Lá você faz o login e sincroniza seus itens.";
                     break;
                 case SupportIntent.Question_Economy:
                     if(Guid.TryParse(context.UserId, out var uid)) {
                         var u = await _context.Users.FindAsync(uid);
                         decision.ResponseToUser = u != null ? $"Seu saldo atual é de {u.StarkCoins} StarkCoins." : "Erro ao ler saldo.";
                     }
                     break;
                 case SupportIntent.Issue_SocialCmd: // Fallback
                     return await DiagnoseSocialCommand(context);
                 case SupportIntent.Issue_Device:
                 case SupportIntent.MaintenanceIssue:
                     // Should have been caught by decide logic, but fallback:
                     return await EscalateAsync(context, "Vamos tentar resolver isso.");
                 default:
                     decision.ResponseToUser = "Desculpe, ainda estou aprendendo. Tente ser mais direto, como 'meu comando sumiu' ou 'limpar cache'.";
                     break;
             }
             return decision;
        }
    }
}
