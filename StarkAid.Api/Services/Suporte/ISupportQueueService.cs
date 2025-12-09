namespace StarkAid.Api.Services.Suporte;

public interface ISupportQueueService
{
    Task<int> AdicionarUsuario(Guid userId, string connectionId, string origem);
    Task RemoverUsuario(Guid userId, string connectionId);
    Task<bool> UsuarioEmAtendimento(Guid userId);
    Task MarcarParaTransferencia(Guid userId);
    Task<Guid?> ProximoUsuario();
}
