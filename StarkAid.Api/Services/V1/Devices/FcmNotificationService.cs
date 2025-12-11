using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StarkAid.Api.Services.V1.Devices
{
    public class FcmNotificationService
    {
        private readonly AppDbContext _context;

        public FcmNotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SalvarTokenFcmAsync(Guid userId, string token)
        {
            // Remove todos os tokens antigos do usuário
            var tokensAntigos = _context.FirebaseTokens.Where(t => t.UserId == userId);
            _context.FirebaseTokens.RemoveRange(tokensAntigos);

            // Adiciona o novo token
            var newToken = new FirebaseToken
            {
                UserId = userId,
                Token = token,
                DataCadastro = DateTime.UtcNow
            };

            await _context.FirebaseTokens.AddAsync(newToken);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> ObterTokensPorUsuario(Guid userId)
        {
            return await _context.FirebaseTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .ToListAsync();
        }

        public async Task EnviarNotificacaoAsync(string token, string titulo, string corpo, Guid disparoId)
        {
            var message = new Message()
            {
                Token = token,
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                },
                Data = new Dictionary<string, string>
                {
                    { "titulo", titulo },
                    { "corpo", corpo },
                    { "disparoId", disparoId.ToString() }
                }
            };

            var messaging = FirebaseMessaging.DefaultInstance;
            try
            {
                await messaging.SendAsync(message);
            }
            catch (FirebaseMessagingException ex)
            {
                if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    var tokenEntity = await _context.FirebaseTokens.FirstOrDefaultAsync(t => t.Token == token);
                    if (tokenEntity != null)
                    {
                        _context.FirebaseTokens.Remove(tokenEntity);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task EnviarParaUsuarioAsync(Guid userId, string titulo, string corpo, Guid disparoId)
        {
            var tokens = await ObterTokensPorUsuario(userId);

            foreach (var token in tokens)
            {
                await EnviarNotificacaoAsync(token, titulo, corpo, disparoId);
            }
        }
    }
}
