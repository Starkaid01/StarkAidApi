using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace StarkAid.Api.Services.Suporte;

public class SupportQueueService : ISupportQueueService
{
    private readonly ConcurrentQueue<QueueItem> _fila = new();
    private readonly ConcurrentDictionary<Guid, QueueItem> _usuariosEmFila = new();
    private readonly ConcurrentDictionary<Guid, bool> _usuariosEmAtendimento = new();
    private readonly ConcurrentDictionary<Guid, bool> _usuariosParaTransferencia = new();
    private readonly ILogger<SupportQueueService> _logger;

    public SupportQueueService(ILogger<SupportQueueService> logger)
    {
        _logger = logger;
    }

    public Task<int> AdicionarUsuario(Guid userId, string connectionId, string origem)
    {
        // Se já está na fila, não adicionar novamente
        if (_usuariosEmFila.ContainsKey(userId))
        {
            var item = _usuariosEmFila[userId];
            return Task.FromResult(ObterPosicao(userId));
        }

        var queueItem = new QueueItem
        {
            UserId = userId,
            ConnectionId = connectionId,
            Origem = origem,
            EntradaNaFila = DateTime.UtcNow
        };

        _fila.Enqueue(queueItem);
        _usuariosEmFila[userId] = queueItem;

        var posicao = ObterPosicao(userId);
        _logger.LogInformation("Usuário {UserId} adicionado à fila. Posição: {Posicao}", userId, posicao);

        // Se não há ninguém em atendimento, marcar como próximo
        if (_usuariosEmAtendimento.IsEmpty)
        {
            _usuariosEmAtendimento[userId] = true;
            _logger.LogInformation("Usuário {UserId} iniciou atendimento imediatamente", userId);
        }

        return Task.FromResult(posicao);
    }

    public Task RemoverUsuario(Guid userId, string connectionId)
    {
        if (_usuariosEmFila.TryRemove(userId, out _))
        {
            _usuariosEmAtendimento.TryRemove(userId, out _);
            _usuariosParaTransferencia.TryRemove(userId, out _);
            _logger.LogInformation("Usuário {UserId} removido da fila", userId);

            // Processar próximo da fila
            ProcessarProximo();
        }

        return Task.CompletedTask;
    }

    public Task<bool> UsuarioEmAtendimento(Guid userId)
    {
        return Task.FromResult(_usuariosEmAtendimento.ContainsKey(userId) && _usuariosEmAtendimento[userId]);
    }

    public Task MarcarParaTransferencia(Guid userId)
    {
        _usuariosParaTransferencia[userId] = true;
        _logger.LogInformation("Usuário {UserId} marcado para transferência", userId);
        return Task.CompletedTask;
    }

    public Task<Guid?> ProximoUsuario()
    {
        if (_fila.TryDequeue(out var item))
        {
            _usuariosEmFila.TryRemove(item.UserId, out _);
            _usuariosEmAtendimento[item.UserId] = true;
            _logger.LogInformation("Próximo usuário da fila: {UserId}", item.UserId);
            return Task.FromResult<Guid?>(item.UserId);
        }

        return Task.FromResult<Guid?>(null);
    }

    private int ObterPosicao(Guid userId)
    {
        // Se está em atendimento, retornar 0 (próximo)
        if (_usuariosEmAtendimento.ContainsKey(userId) && _usuariosEmAtendimento[userId])
        {
            return 0;
        }

        var items = _fila.ToArray();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].UserId == userId)
            {
                return i + 1;
            }
        }
        return 0;
    }

    private void ProcessarProximo()
    {
        if (_fila.TryPeek(out var proximo))
        {
            if (!_usuariosEmAtendimento.ContainsKey(proximo.UserId))
            {
                _usuariosEmAtendimento[proximo.UserId] = true;
                _logger.LogInformation("Próximo usuário {UserId} iniciou atendimento", proximo.UserId);
            }
        }
    }

    private class QueueItem
    {
        public Guid UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public DateTime EntradaNaFila { get; set; }
    }
}
