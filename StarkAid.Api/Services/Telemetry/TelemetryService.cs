using System.Threading.Tasks;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Telemetry;
using StarkAid.Api.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace StarkAid.Api.Services.Telemetry
{
    public sealed class TelemetryService : ITelemetryService
    {
        private readonly IServiceProvider _serviceProvider;

        public TelemetryService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task RegistrarAsync(TelemetryEventDto dto)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var telemetria = new Telemetria(dto);
            db.Telemetrias.Add(telemetria);
            await db.SaveChangesAsync();
        }

        public async Task RegistrarInteracaoIaAsync(AiInteractionEvent evento)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (evento.Id == Guid.Empty) evento.Id = Guid.NewGuid();
            
            // Re-attach context/entities if they were tracked by another context?
            // AiInteractionEvent is a DTO/Entity. If it references other entities (User), we might need to handle them.
            // But usually it's just IDs.
            // However, EF might complain if 'evento' is already tracked by another context (the Controller's context).
            // But 'evento' is created as new object in Controller. It's NOT added to Controller's context. 
            // So it is Detached. Adding to new context is fine.
            
            db.AiInteractionEvents.Add(evento);
            await db.SaveChangesAsync();
        }
    }
}
