using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracaoStarkNlpController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ConfiguracaoStarkNlpController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/configuracaostarknlp
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var config = await _context.ConfiguracoesStarkNlp.FirstOrDefaultAsync();
            if (config == null)
                return NotFound("Nenhuma configuração encontrada.");

            return Ok(config);
        }

        // PUT: api/configuracaostarknlp
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] string novaUrl)
        {
            if (string.IsNullOrWhiteSpace(novaUrl))
                return BadRequest("A URL não pode ser vazia.");

            var config = await _context.ConfiguracoesStarkNlp.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfiguracaoStarkNlp
                {
                    StarkNlpUrl = novaUrl,
                    DataAtualizacao = DateTime.UtcNow
                };
                _context.ConfiguracoesStarkNlp.Add(config);
            }
            else
            {
                config.StarkNlpUrl = novaUrl;
                config.DataAtualizacao = DateTime.UtcNow;
                _context.ConfiguracoesStarkNlp.Update(config);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Mensagem = "URL atualizada com sucesso.", config.StarkNlpUrl });
        }
    }
}
