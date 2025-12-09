using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Services.Weather;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly AppDbContext _context;

    public WeatherController(IWeatherService weatherService, AppDbContext context)
    {
        _weatherService = weatherService;
        _context = context;
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast()
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Usuário não encontrado" });

            if (string.IsNullOrWhiteSpace(user.Cidade))
                return BadRequest(new { message = "Cidade não cadastrada. Por favor, atualize seu perfil com a cidade." });

            var forecast = await _weatherService.GetWeatherForecastAsync(user.Cidade, user.Bairro);
            if (forecast == null)
                return NotFound(new { message = "Não foi possível obter a previsão do tempo para sua localização." });

            return Ok(forecast);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao obter previsão do tempo", error = ex.Message });
        }
    }

    [HttpGet("forecast/coordinates")]
    public async Task<IActionResult> GetForecastByCoordinates([FromQuery] double latitude, [FromQuery] double longitude)
    {
        try
        {
            var forecast = await _weatherService.GetWeatherForecastByCoordinatesAsync(latitude, longitude);
            if (forecast == null)
                return NotFound(new { message = "Não foi possível obter a previsão do tempo para as coordenadas fornecidas." });

            return Ok(forecast);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao obter previsão do tempo", error = ex.Message });
        }
    }
}
