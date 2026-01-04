using System.Threading.Tasks;
using StarkAid.Api.DTOs.Telemetry;

namespace StarkAid.Api.Services.Telemetry
{
    public interface ITelemetryService
    {
        Task RegistrarAsync(TelemetryEventDto dto);
        Task RegistrarInteracaoIaAsync(StarkAid.Api.Entities.AiInteractionEvent evento);
    }
}
