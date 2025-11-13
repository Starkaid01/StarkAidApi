using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Nlp;
using StarkAid.Api.Entities;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace StarkAid.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NlpServerController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _http;
        private readonly AppDbContext _context;
        public NlpServerController(AppDbContext db, IHttpClientFactory httpClientFactory, AppDbContext context)
        {
            _db = db;
            _http = httpClientFactory.CreateClient();
            _context = context;
        }

        // 🧩 POST /api/nlpserver/add-name
        [HttpPost("add-name")]
        public async Task<IActionResult> AddName([FromBody] AddNameRequest request)
        {
            var dominio = await _db.ConfiguracoesSistema.AsNoTracking().FirstOrDefaultAsync();

            if (dominio == null || string.IsNullOrWhiteSpace(dominio.DominioNlp))
                return BadRequest("Nenhum domínio NLP configurado no banco.");

            var url = $"{dominio.DominioNlp.TrimEnd('/')}/add_name";

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                return Content(responseBody, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Falha ao conectar ao NLP Server", error = ex.Message });
            }
        }

        // 🧠 POST /api/nlpserver/extract-entities
        [Authorize]
        [HttpPost("extract-entities")]
        public async Task<IActionResult> ExtractEntities(Guid id, [FromBody] ExtractEntitiesRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdFromToken != user.Id.ToString())
                return Forbid();

            var dominio = await _db.ConfiguracoesSistema.AsNoTracking().FirstOrDefaultAsync();

            if (dominio == null || string.IsNullOrWhiteSpace(dominio.DominioNlp))
                return BadRequest("Nenhum domínio NLP configurado no banco.");

            var url = $"{dominio.DominioNlp.TrimEnd('/')}/extract_entities";

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                user.StarkCoins -= 0.01m;
                await _context.SaveChangesAsync();

                return Content(responseBody, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao conectar ao NLP Server", error = ex.Message });
            }
        }

        // 🔧 PUT /api/nlpserver/url-base
        [HttpPut("url-base")]
        public async Task<IActionResult> AtualizarUrlBase([FromBody] UpdateNlpUrlRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.NovaUrl))
                return BadRequest("URL não pode estar vazia.");

            var config = await _db.ConfiguracoesSistema.FirstOrDefaultAsync();

            if (config == null)
            {
                config = new Entities.ConfiguracaoSistema
                {
                    DominioNlp = req.NovaUrl.Trim(),
                    UltimaAtualizacao = DateTime.UtcNow
                };
                _db.ConfiguracoesSistema.Add(config);
            }
            else
            {
                config.DominioNlp = req.NovaUrl.Trim();
                config.UltimaAtualizacao = DateTime.UtcNow;
                _db.ConfiguracoesSistema.Update(config);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "URL base do NLP atualizada com sucesso!",
                novaUrl = config.DominioNlp
            });
        }
    }
}
