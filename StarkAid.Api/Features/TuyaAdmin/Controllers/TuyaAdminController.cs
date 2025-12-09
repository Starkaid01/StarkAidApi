using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Features.TuyaAdmin.Models;
using StarkAid.Api.Features.TuyaAdmin.Services;

namespace StarkAid.Api.Features.TuyaAdmin.Controllers
{
    [ApiController]
    [Route("admin/tuya")]
    [Authorize(Roles = "Admin")]
    public class TuyaAdminController : ControllerBase
    {
        private readonly ITuyaAdminService _service;
        private readonly ILogger<TuyaAdminController> _logger;

        public TuyaAdminController(ITuyaAdminService service, ILogger<TuyaAdminController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Buscar usuário Tuya por email (consulta no Tuya Cloud)
        /// GET /admin/tuya/list-users?email=...
        /// </summary>
        [HttpGet("list-users")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "email é obrigatório" });

            var user = await _service.GetUserByEmailAsync(email, ct);
            if (user == null)
                return NotFound(new { message = "Usuário não encontrado" });

            return Ok(user);
        }

        /// <summary>
        /// Deletar usuário Tuya pelo UID
        /// DELETE /admin/tuya/delete-user/{uid}
        /// </summary>
        [HttpDelete("delete-user/{uid}")]
        public async Task<IActionResult> DeleteUser([FromRoute] string uid, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return BadRequest(new { message = "uid é obrigatório" });

            var ok = await _service.DeleteUserByUidAsync(uid, ct);
            return ok ? NoContent() : StatusCode(500, new { message = "Falha ao deletar usuário" });
        }

        /// <summary>
        /// Deletar vários usuários passando lista de emails.
        /// DELETE /admin/tuya/clean-duplicates
        /// Body: { "emails": ["a@b.com", "x@y.com"] }
        /// </summary>
        [HttpDelete("clean-duplicates")]
        public async Task<IActionResult> CleanDuplicates([FromBody] CleanDuplicatesRequestDto request, CancellationToken ct)
        {
            if (request?.Emails == null || request.Emails.Length == 0)
                return BadRequest(new { message = "emails é obrigatório no body" });

            var results = await _service.CleanDuplicatesAsync(request.Emails, ct);
            return Ok(results.Select(r => new { email = r.email, deleted = r.deleted, message = r.message }));
        }

        // Adicione no TuyaAdminController
        [HttpPost("migrate-user")]
        public async Task<IActionResult> MigrateUser([FromBody] MigrateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email e senha são obrigatórios" });

            try
            {
                // 1. Verificar se usuário já existe no Cloud Project
                var existingUser = await _service.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Ok(new
                    {
                        message = "Usuário já existe no Cloud Project",
                        uid = existingUser.Uid,
                        migrated = false
                    });
                }

                // 2. Criar usuário no Cloud Project
                var migrationResult = await _service.CreateUserInCloudProjectAsync(request.Email, request.Password);

                if (migrationResult != null)
                {
                    return Ok(new
                    {
                        message = "Usuário migrado com sucesso",
                        uid = migrationResult.Uid,
                        migrated = true
                    });
                }
                else
                {
                    return StatusCode(500, new { message = "Falha ao criar usuário no Cloud Project" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao migrar usuário {Email}", request.Email);
                return StatusCode(500, new { message = $"Erro na migração: {ex.Message}" });
            }
        }

        public record MigrateUserRequest(string Email, string Password);


    }


}
