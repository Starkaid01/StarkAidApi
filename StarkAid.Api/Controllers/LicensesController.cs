using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.License;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.Assinatura;
using StarkAid.Api.Services.License;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/licenses")]
[Authorize]
public class LicensesController : ControllerBase
{
    private readonly LicenseService _licenseService;
    private readonly StripeService _stripeService;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(
        LicenseService licenseService,
        StripeService stripeService,
        ILogger<LicensesController> logger)
    {
        _licenseService = licenseService;
        _stripeService = stripeService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuário não autenticado");
        }
        return userId;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateLicenseRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            // Validar maxMachines
            if (request.MaxMachines != 2 && request.MaxMachines != 4)
            {
                return BadRequest(new { message = "MaxMachines deve ser 2 ou 4" });
            }

            // Definir preço baseado no número de máquinas
            decimal price = request.MaxMachines == 2 ? 250.00m : 454.00m;

            // Buscar usuário
            var dbContext = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Criar sessão de checkout no Stripe
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/api/licenses/checkout/success?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/api/licenses/checkout/cancel";

            var (session, customer) = await _stripeService.CreateOneTimePaymentSessionAsync(
                user,
                price,
                successUrl,
                cancelUrl
            );

            // Criar licença no banco (ainda inativa)
            var license = await _licenseService.CreateLicenseAsync(userId, request.MaxMachines, price, session.Id);

            _logger.LogInformation("Checkout criado para licença {LicenseId}, sessão Stripe: {SessionId}", license.Id, session.Id);

            return Ok(new CheckoutLicenseResponse
            {
                CheckoutUrl = session.Url,
                SessionId = session.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar checkout de licença");
            return StatusCode(500, new { message = "Erro ao criar checkout", error = ex.Message });
        }
    }

    [HttpGet("checkout/success")]
    public async Task<IActionResult> CheckoutSuccess([FromQuery] string session_id)
    {
        try
        {
            var session = await _stripeService.GetSessionAsync(session_id);
            
            if (session == null || session.PaymentStatus != "paid")
            {
                return BadRequest(new { message = "Pagamento não confirmado" });
            }

            // Confirmar pagamento e ativar licença
            var confirmed = await _licenseService.ConfirmPaymentAsync(session_id, session.PaymentIntentId);

            if (!confirmed)
            {
                return BadRequest(new { message = "Erro ao confirmar pagamento" });
            }

            return Redirect($"{Request.Scheme}://{Request.Host}/licenses?status=success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar sucesso do checkout");
            return StatusCode(500, new { message = "Erro ao processar pagamento", error = ex.Message });
        }
    }

    [HttpGet("checkout/cancel")]
    public IActionResult CheckoutCancel()
    {
        return Redirect($"{Request.Scheme}://{Request.Host}/licenses?status=canceled");
    }

    [HttpGet]
    public async Task<IActionResult> GetUserLicenses()
    {
        try
        {
            var userId = GetUserId();
            var licenses = await _licenseService.GetUserLicensesAsync(userId);

            var licensesDto = licenses.Select(l => new LicenseDto
            {
                Id = l.Id,
                LicenseKey = l.LicenseKey,
                MaxMachines = l.MaxMachines,
                Price = l.Price,
                CreatedAt = l.CreatedAt,
                ExpiresAt = l.ExpiresAt,
                IsActive = l.IsActive,
                PaymentConfirmedAt = l.PaymentConfirmedAt,
                ActiveActivations = l.Activations.Count(a => a.IsActive),
                Activations = l.Activations.Select(a => new LicenseActivationDto
                {
                    Id = a.Id,
                    MachineId = a.MachineId,
                    MachineName = a.MachineName,
                    ActivatedAt = a.ActivatedAt,
                    DeactivatedAt = a.DeactivatedAt,
                    IsActive = a.IsActive,
                    IpAddress = a.IpAddress
                }).ToList()
            }).ToList();

            return Ok(licensesDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar licenças do usuário");
            return StatusCode(500, new { message = "Erro ao buscar licenças", error = ex.Message });
        }
    }

    [HttpPost("activate")]
    public async Task<IActionResult> ActivateLicense([FromBody] ActivateLicenseRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            // Normalizar a chave da licença
            var normalizedLicenseKey = request.LicenseKey?.Trim().ToUpperInvariant() ?? string.Empty;
            
            _logger.LogInformation("Tentativa de ativação - Usuário: {UserId}, Licença: {LicenseKey}", userId, normalizedLicenseKey);
            
            // Verificar se a licença existe e pertence ao usuário
            var license = await _licenseService.GetLicenseByKeyAsync(normalizedLicenseKey);
            if (license == null)
            {
                _logger.LogWarning("Licença não encontrada: {LicenseKey} para usuário {UserId}", normalizedLicenseKey, userId);
                return BadRequest(new { message = "Licença não encontrada. Verifique se a chave está correta." });
            }
            
            if (license.UserId != userId)
            {
                _logger.LogWarning("Usuário {UserId} tentou ativar licença de outro usuário: {LicenseId}", userId, license.Id);
                return BadRequest(new { message = "Esta licença não pertence ao seu usuário." });
            }
            
            // Obter MachineId (identificador único da máquina)
            var machineId = GetMachineId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            _logger.LogInformation("MachineId obtido: {MachineId}", machineId);

            var activation = await _licenseService.ActivateLicenseAsync(
                normalizedLicenseKey,
                machineId,
                request.MachineName,
                ipAddress
            );

            if (activation == null)
            {
                _logger.LogWarning("Falha na ativação - Licença: {LicenseKey}, MachineId: {MachineId}, Usuário: {UserId}", 
                    normalizedLicenseKey, machineId, userId);
                return BadRequest(new { message = "Não foi possível ativar a licença. Verifique se a licença está ativa e se não excedeu o limite de máquinas." });
            }

            _logger.LogInformation("Licença ativada com sucesso - Licença: {LicenseKey}, MachineId: {MachineId}", 
                normalizedLicenseKey, machineId);

            return Ok(new LicenseActivationDto
            {
                Id = activation.Id,
                MachineId = activation.MachineId,
                MachineName = activation.MachineName,
                ActivatedAt = activation.ActivatedAt,
                IsActive = activation.IsActive,
                IpAddress = activation.IpAddress
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Não autorizado ao ativar licença: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar licença");
            return StatusCode(500, new { message = "Erro ao ativar licença", error = ex.Message });
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyLicense([FromBody] VerifyLicenseRequest request)
    {
        try
        {
            var machineId = GetMachineId();
            var isValid = await _licenseService.VerifyLicenseAsync(request.LicenseKey, machineId);

            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar licença");
            return StatusCode(500, new { message = "Erro ao verificar licença", error = ex.Message });
        }
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> DeactivateLicense([FromBody] VerifyLicenseRequest request)
    {
        try
        {
            var machineId = GetMachineId();
            var deactivated = await _licenseService.DeactivateLicenseAsync(request.LicenseKey, machineId);

            if (!deactivated)
            {
                return BadRequest(new { message = "Não foi possível desativar a licença" });
            }

            return Ok(new { message = "Licença desativada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar licença");
            return StatusCode(500, new { message = "Erro ao desativar licença", error = ex.Message });
        }
    }

    [HttpDelete("{licenseId}")]
    public async Task<IActionResult> DeleteLicense(Guid licenseId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _licenseService.DeleteInactiveLicenseAsync(licenseId, userId);

            if (!deleted)
            {
                return BadRequest(new { message = "Não foi possível deletar a licença. Verifique se ela está inativa e pertence a você." });
            }

            return Ok(new { message = "Licença deletada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar licença");
            return StatusCode(500, new { message = "Erro ao deletar licença", error = ex.Message });
        }
    }

    [HttpPost("{licenseId}/activate-test")]
    public async Task<IActionResult> ActivateLicenseForTest(Guid licenseId)
    {
        try
        {
            var userId = GetUserId();
            var dbContext = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            
            var license = await dbContext.Licenses
                .FirstOrDefaultAsync(l => l.Id == licenseId && l.UserId == userId);

            if (license == null)
            {
                return NotFound(new { message = "Licença não encontrada ou não pertence a você" });
            }

            // Ativar a licença manualmente para testes
            license.IsActive = true;
            license.PaymentConfirmedAt = DateTimeOffset.UtcNow;
            license.StripePaymentIntentId = "TEST_MANUAL_ACTIVATION";

            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Licença {LicenseId} ativada manualmente para testes pelo usuário {UserId}", licenseId, userId);

            return Ok(new { message = "Licença ativada com sucesso para testes", licenseId = license.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar licença para testes");
            return StatusCode(500, new { message = "Erro ao ativar licença", error = ex.Message });
        }
    }

    private string GetMachineId()
    {
        // Tentar obter do header customizado
        var machineIdHeader = Request.Headers["X-Machine-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(machineIdHeader))
        {
            return machineIdHeader;
        }

        // Fallback: usar IP + User-Agent como identificador
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();
        var combined = $"{ip}-{userAgent}";
        
        // Gerar hash MD5 do identificador combinado
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}

