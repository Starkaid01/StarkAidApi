using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace StarkAid.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerssistirFormController : ControllerBase
    {
        private readonly string _jsonPath;

        public PerssistirFormController(IWebHostEnvironment env)
        {
            // Usar App_Data que geralmente tem permissões de escrita
            _jsonPath = Path.Combine(env.ContentRootPath, "App_Data", "anamnese.json");

            // Garante que a pasta exista
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_jsonPath)!);
            }
            catch (Exception)
            {
                // Fallback: usar temporary path se App_Data não funcionar
                var tempPath = Path.GetTempPath();
                _jsonPath = Path.Combine(tempPath, "StarkAid", "anamnese.json");
                Directory.CreateDirectory(Path.GetDirectoryName(_jsonPath)!);
            }
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                if (!System.IO.File.Exists(_jsonPath))
                    return Ok(new { message = "Nenhum dado salvo ainda." });

                var json = System.IO.File.ReadAllText(_jsonPath);
                var data = JsonSerializer.Deserialize<object>(json);
                return Ok(data);
            }
            catch (Exception)
            {
                return Ok(new { message = "Nenhum dado salvo ainda." });
            }
        }

        [HttpPost]
        public IActionResult Save([FromBody] JsonElement novoConteudo)
        {
            try
            {
                Dictionary<string, object> dadosAtuais = new Dictionary<string, object>();

                // Carrega o arquivo existente, se houver
                if (System.IO.File.Exists(_jsonPath))
                {
                    var jsonExistente = System.IO.File.ReadAllText(_jsonPath);
                    dadosAtuais = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonExistente) ?? new Dictionary<string, object>();
                }

                // Converte o novo conteúdo para dicionário
                var novosDados = JsonSerializer.Deserialize<Dictionary<string, object>>(novoConteudo.GetRawText()) ?? new Dictionary<string, object>();

                // Atualiza apenas as seções enviadas
                foreach (var kv in novosDados)
                {
                    dadosAtuais[kv.Key] = kv.Value;
                }

                // Salva o JSON atualizado
                var jsonFinal = JsonSerializer.Serialize(dadosAtuais, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_jsonPath, jsonFinal);

                return Ok(new { message = "Formulário salvo com sucesso!", data = dadosAtuais });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Erro ao salvar o formulário.",
                    detail = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}