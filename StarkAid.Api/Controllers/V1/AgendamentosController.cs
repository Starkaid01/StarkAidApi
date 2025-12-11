using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.DTOs.V1.Devices;
using StarkAid.Api.Hubs;
using StarkAid.Api.Services.V1;
using StarkAid.Api.Services.V1.Devices;
using StarkAid.Api.Services.V1.DispositivoEsp;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AgendamentosController : ControllerBase
    {
        private readonly AgendamentoService _service;
        private readonly DispositivoEspService _dispositivoEspService;
        private readonly IEwelinkService _ewelinkService;
        private readonly IHubContext<DispositivoEspHub> _hubContext;
        private readonly ILogger<AgendamentosController> _logger;

        public AgendamentosController(
            AgendamentoService service,
            DispositivoEspService dispositivoEspService,
            IEwelinkService ewelinkService,
            IHubContext<DispositivoEspHub> hubContext,
            ILogger<AgendamentosController> logger)
        {
            _service = service;
            _dispositivoEspService = dispositivoEspService;
            _ewelinkService = ewelinkService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Usuário não autenticado.");
                }

                var userId = Guid.Parse(userIdClaim);
                var agendamentos = await _service.BuscarPorUsuarioAsync(userId);
                return Ok(agendamentos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar agendamentos: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = "Erro ao listar agendamentos", message = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarAgendamentoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var agendamento = await _service.CriarAsync(
                userId,
                request.DeviceId,
                request.AgendadoPara,
                request.Comando,
                request.Recorrencia);
            return Created("", agendamento);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(Guid id, [FromBody] EditarAgendamentoRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var atualizado = await _service.EditarAsync(
                id,
                userId,
                request.AgendadoPara,
                request.Comando,
                request.Recorrencia);
            if (!atualizado)
                return NotFound("Agendamento não encontrado ou não pertence a você.");

            return Ok("Agendamento atualizado.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var removido = await _service.ExcluirAsync(id, userId);
            if (!removido)
                return NotFound("Agendamento não encontrado ou não pertence a você.");

            return Ok("Agendamento removido.");
        }

        [HttpPost("esp")]
        public async Task<IActionResult> CriarAgendamentoEsp([FromBody] CriarAgendamentoEspRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request inválido.");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Usuário não autenticado.");
                }

                var userId = Guid.Parse(userIdClaim);
                
                // Verificar se o dispositivo ESP existe e pertence ao usuário
                var dispositivo = await _dispositivoEspService.GetByIdAsync(request.DispositivoEspId);
                if (dispositivo == null)
                    return NotFound("Dispositivo ESP não encontrado.");
                
                if (dispositivo.UserId.HasValue && dispositivo.UserId != userId)
                    return Forbid("Dispositivo ESP não pertence a você.");

                // Validar recorrência
                var recorrenciasValidas = new[] { "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno" };
                if (!recorrenciasValidas.Contains(request.Recorrencia))
                    return BadRequest("Recorrência inválida. Use: NaoRepetir, TodosOsDias, TodaSemana, TodoMes ou TodoAno");

                var agendamento = await _service.CriarAgendamentoEspAsync(
                    userId,
                    request.DispositivoEspId,
                    request.Data,
                    request.Hora,
                    request.Minuto,
                    request.Recorrencia);

                return Created("", agendamento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar agendamento ESP: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = "Erro ao criar agendamento", message = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("starkswitch")]
        public async Task<IActionResult> CriarAgendamentoStarkswitch([FromBody] CriarAgendamentoStarkswitchRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Verificar se o dispositivo existe e pertence ao usuário
            var deviceService = HttpContext.RequestServices.GetRequiredService<IDeviceService>();
            var device = await deviceService.GetByIdAsync(request.DeviceId);
            if (device == null)
                return NotFound("Dispositivo Starkswitch não encontrado.");
            
            if (device.UserId != userId)
                return Forbid("Dispositivo Starkswitch não pertence a você.");

            // Validar ação
            if (request.Acao.ToLower() != "ligar" && request.Acao.ToLower() != "desligar")
                return BadRequest("Ação inválida. Use: ligar ou desligar");

            // Validar recorrência
            var recorrenciasValidas = new[] { "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno" };
            if (!recorrenciasValidas.Contains(request.Recorrencia))
                return BadRequest("Recorrência inválida. Use: NaoRepetir, TodosOsDias, TodaSemana, TodoMes ou TodoAno");

            var agendamento = await _service.CriarAgendamentoStarkswitchAsync(
                userId,
                request.DeviceId,
                request.Acao,
                request.Data,
                request.Hora,
                request.Minuto,
                request.Recorrencia);

            return Created("", agendamento);
        }

        [HttpPost("ewelink")]
        public async Task<IActionResult> CriarAgendamentoEwelink([FromBody] CriarAgendamentoEwelinkRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request inválido.");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Usuário não autenticado.");
                }

                var userId = Guid.Parse(userIdClaim);
                
                // Verificar se o dispositivo Ewelink existe e pertence ao usuário
                var device = await _ewelinkService.GetDeviceStatusAsync(userId, request.EwelinkDeviceId);
                if (device == null)
                    return NotFound("Dispositivo Ewelink não encontrado.");
                
                // Validar ação
                if (request.Acao.ToLower() != "ligar" && request.Acao.ToLower() != "desligar")
                    return BadRequest("Ação inválida. Use: ligar ou desligar");

                // Validar recorrência
                var recorrenciasValidas = new[] { "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno" };
                if (!recorrenciasValidas.Contains(request.Recorrencia))
                    return BadRequest("Recorrência inválida. Use: NaoRepetir, TodosOsDias, TodaSemana, TodoMes ou TodoAno");

                var agendamento = await _service.CriarAgendamentoEwelinkAsync(
                    userId,
                    request.EwelinkDeviceId,
                    request.Acao,
                    request.Data,
                    request.Hora,
                    request.Minuto,
                    request.Recorrencia);

                return Created("", agendamento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar agendamento Ewelink: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = "Erro ao criar agendamento", message = ex.Message, details = ex.ToString() });
            }
        }
    }
}
