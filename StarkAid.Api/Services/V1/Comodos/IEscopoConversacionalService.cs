using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Comodos
{
    public interface IEscopoConversacionalService
    {
        Task<EscopoConversacional?> GetEscopoAtivoAsync(Guid userId);
        Task CriarOuRenovarEscopoAsync(Guid userId, Guid comodoId);
        Task LimparEscopoAsync(Guid userId);
    }
}
