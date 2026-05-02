using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Services.V1.Support.Agents;
using StarkAid.Api.DTOs.V1.Support;

namespace StarkAid.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ISupportMessageRouter _router;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            ISupportMessageRouter router,
            ILogger<ChatHub> logger)
        {
            _router = router;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("ChatConnected");
            await base.OnConnectedAsync();
        }

        public async Task StartChatSession()
        {
            var welcome = "Olá! Sou o assistente de suporte da StarkAid. Como posso te ajudar hoje?";
            await Clients.Caller.SendAsync("ChatSessionStarted");
            await Clients.Caller.SendAsync("ReceiveMessage", new { sender = "ia", message = welcome });
        }

        public async Task SendMessage(string message)
        {
            var userIdString = Context.UserIdentifier;
            if (!Guid.TryParse(userIdString, out var userId))
            {
                await Clients.Caller.SendAsync("ChatError", "Usuário não identificado.");
                return;
            }

            try
            {
                // 1. Processar com o Cérebro Único (SupportMessageRouter)
                var result = await _router.ProcessMessageAsync(userId, message, "app");

                // 2. Enviar Resposta (Mantendo formato legado do ChatHub se necessário)
                await Clients.Caller.SendAsync("ReceiveMessage", new { sender = "ia", message = result.Content });

                // 3. Se houver ação a executar (pós-confirmação)
                if (!string.IsNullOrEmpty(result.ActionToExecute))
                {
                    // No ChatHub original, as ações eram enviadas via "ExecuteAction"
                    // Mapeamos para os comandos que o App Android entende
                    var legacyCmd = result.ActionToExecute.ToLower() switch {
                        "cleanappcache" => "clean-cache",
                        "cleanappdata" => "clean-dados",
                        "restartapp" => "restart",
                        "logout" => "logout",
                        "cleanlocaldatabase" => "clean-data-base",
                        _ => result.ActionToExecute.ToLower()
                    };
                    
                    await Clients.Caller.SendAsync("ExecuteAction", legacyCmd);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ChatHub ao processar mensagem");
                await Clients.Caller.SendAsync("ReceiveMessage", new { sender = "ia", message = "Desculpe, tive um problema técnico agora. Pode repetir?" });
            }
        }
    }
}
