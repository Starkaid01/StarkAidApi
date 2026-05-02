using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.Services.V1.Support.Agents;
using StarkAid.Api.DTOs.V1.Support;
using StarkAid.Api.Services.V1.Support.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace StarkAid.Api.Hubs;

[Authorize]
public class SupportAgentHub : Hub
{
    private readonly ISupportMessageRouter _router;
    private readonly ISupportActionExecutor _executor;

    public SupportAgentHub(ISupportMessageRouter router, ISupportActionExecutor executor)
    {
        _router = router;
        _executor = executor;
    }

    public async Task SendMessage(string message, string origem)
    {
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out var userId)) return;

        try
        {
            var result = await _router.ProcessMessageAsync(userId, message, origem);
            
            // Enviar resposta ao usuário
            await Clients.Caller.SendAsync("ReceiveMessage", result);

            // Se o router decidiu que uma ação deve ser executada (ex: após confirmação do usuário)
            if (!string.IsNullOrEmpty(result.ActionToExecute))
            {
                await _executor.ExecuteActionAsync(userId, result.ActionToExecute, origem);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", new SupportMessage 
            { 
                Type = SupportMessageType.Error, 
                Content = "Desculpe, ocorreu um erro interno ao processar seu pedido: " + ex.Message 
            });
        }
    }
}
