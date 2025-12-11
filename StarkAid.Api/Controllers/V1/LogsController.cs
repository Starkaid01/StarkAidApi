using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Authorize]
    [Route("api/v{version:apiVersion}/logs")]
    public class LogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("error-code-soft/{codigo}")]
        public async Task<IActionResult> GetErrorCodeDescriptionSoft(string codigo)
        {
            var errorCode = await _context.ErrorCodeDescriptions
                .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && 
                                         (e.Origem == "soft" || string.IsNullOrEmpty(e.Origem)));

            if (errorCode == null)
            {
                // Retornar descrição padrão se não encontrar
                return Ok(new
                {
                    codigoDeErro = codigo,
                    descricao = "Código de erro não encontrado na base de dados.",
                    contexto = "Desconhecido",
                    camposRelevantes = "N/A"
                });
            }

            return Ok(new
            {
                codigoDeErro = errorCode.CodigoDeErro,
                descricao = errorCode.Descricao,
                contexto = errorCode.Contexto,
                camposRelevantes = errorCode.CamposRelevantes
            });
        }

        [HttpGet("error-code-app/{codigo}")]
        public async Task<IActionResult> GetErrorCodeDescriptionApp(string codigo)
        {
            var errorCode = await _context.ErrorCodeDescriptions
                .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && e.Origem == "app");

            if (errorCode == null)
            {
                // Retornar descrição padrão se não encontrar
                return Ok(new
                {
                    codigoDeErro = codigo,
                    descricao = "Código de erro não encontrado na base de dados.",
                    contexto = "Desconhecido",
                    camposRelevantes = "N/A"
                });
            }

            return Ok(new
            {
                codigoDeErro = errorCode.CodigoDeErro,
                descricao = errorCode.Descricao,
                contexto = errorCode.Contexto,
                camposRelevantes = errorCode.CamposRelevantes
            });
        }

        [HttpGet("error-solutions-soft/{codigo}")]
        public async Task<IActionResult> GetErrorSolutionsSoft(string codigo)
        {
            var errorCode = await _context.ErrorCodeDescriptions
                .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && 
                                         (e.Origem == "soft" || string.IsNullOrEmpty(e.Origem)));

            if (errorCode == null || string.IsNullOrEmpty(errorCode.Solucoes))
            {
                return Ok(new
                {
                    codigoDeErro = codigo,
                    solucoes = new List<string> { "Código de erro não encontrado ou sem soluções cadastradas." }
                });
            }

            // Parse JSON array de soluções
            try
            {
                var solucoes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(errorCode.Solucoes) 
                    ?? new List<string>();
                
                return Ok(new
                {
                    codigoDeErro = errorCode.CodigoDeErro,
                    solucoes = solucoes
                });
            }
            catch
            {
                // Se não conseguir fazer parse, retornar como string única
                return Ok(new
                {
                    codigoDeErro = errorCode.CodigoDeErro,
                    solucoes = new List<string> { errorCode.Solucoes }
                });
            }
        }

        [HttpGet("error-solutions-app/{codigo}")]
        public async Task<IActionResult> GetErrorSolutionsApp(string codigo)
        {
            var errorCode = await _context.ErrorCodeDescriptions
                .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && e.Origem == "app");

            if (errorCode == null || string.IsNullOrEmpty(errorCode.Solucoes))
            {
                return Ok(new
                {
                    codigoDeErro = codigo,
                    solucoes = new List<string> { "Código de erro não encontrado ou sem soluções cadastradas." }
                });
            }

            // Parse JSON array de soluções
            try
            {
                var solucoes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(errorCode.Solucoes) 
                    ?? new List<string>();
                
                return Ok(new
                {
                    codigoDeErro = errorCode.CodigoDeErro,
                    solucoes = solucoes
                });
            }
            catch
            {
                // Se não conseguir fazer parse, retornar como string única
                return Ok(new
                {
                    codigoDeErro = errorCode.CodigoDeErro,
                    solucoes = new List<string> { errorCode.Solucoes }
                });
            }
        }
    }
}

