using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ConversationAnalysis : ControllerBase
    {
        private readonly string _baseSecurePath;

        public ConversationAnalysis()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var securePathFromEnv = System.Environment.GetEnvironmentVariable("STARKAID_SECURE_PATH");
            if (!string.IsNullOrWhiteSpace(securePathFromEnv) && Directory.Exists(securePathFromEnv))
            {
                _baseSecurePath = securePathFromEnv;
                return;
            }

            var seemsToBeBaseFolder =
                Directory.Exists(Path.Combine(currentDirectory, "relatorios")) ||
                Directory.Exists(Path.Combine(currentDirectory, "transcricoes-audios-id"));

            _baseSecurePath = seemsToBeBaseFolder
                ? currentDirectory
                : Path.Combine(currentDirectory, "confidencial-acesso-restrito");
        }


        [HttpGet("transcription/{id}")]
        public async Task<IActionResult> GetTranscription(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID is empty");

            string folder = Path.Combine(_baseSecurePath, "transcricoes-audios-id");
            string filePath = Path.Combine(folder, $"{id}.txt");

            // DEBUG: Log path
            // Console.WriteLine($"Try Access: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"DEBUG: Arquivo de transcrição não encontrado. Caminho buscado: {filePath}");
            }

            string content = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(content);
        }

        [HttpGet("audio/{id}")]
        public IActionResult GetAudio(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID is empty");

            string folder = Path.Combine(_baseSecurePath, "audios-organizados-id");
            string[] extensions = { "ogg", "mp4", "m4a" };

            // Debug info collector
            var triedPaths = new System.Collections.Generic.List<string>();

            foreach (var ext in extensions)
            {
                string filePath = Path.Combine(folder, $"{id}.{ext}");
                triedPaths.Add(filePath);

                if (System.IO.File.Exists(filePath))
                {
                    string mimeType = ext == "ogg" ? "audio/ogg" : "audio/mp4";
                    var stream = System.IO.File.OpenRead(filePath);
                    return File(stream, mimeType, enableRangeProcessing: true);
                }
            }

            return NotFound($"DEBUG: Áudio não encontrado. Caminhos buscados: {string.Join(", ", triedPaths)}");
        }

        [HttpPost("transcription/{id}")]
        public async Task<IActionResult> UpdateTranscription(string id, [FromBody] TranscriptionUpdateModel model)
        {
            if (string.IsNullOrWhiteSpace(id) || model == null || string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest("ID e conteúdo são obrigatórios.");
            }

            string folder = Path.Combine(_baseSecurePath, "transcricoes-audios-id");
            string filePath = Path.Combine(folder, $"{id}.txt");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"DEBUG: Impossível salvar. Arquivo original não existe: {filePath}");
            }

            try
            {
                await System.IO.File.WriteAllTextAsync(filePath, model.Content);
                var replacements = await UpdateChatReportTranscriptionBlock(id, model.Content);
                return Ok(new { message = "Transcrição salva com sucesso.", chatReportBlocksUpdated = replacements });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Erro interno ao salvar arquivo: {ex.Message}");
            }
        }

        [HttpGet("report/{type}")]
        public async Task<IActionResult> GetReport(string type)
        {
            string filename = GetReportFilename(type);
            if (filename == null) return BadRequest("Tipo de relatório inválido.");

            string folder = Path.Combine(_baseSecurePath, "relatorios");
            string filePath = Path.Combine(folder, filename);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"DEBUG: Relatório não encontrado. Buscado em: {filePath}");
            }

            string content = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(content);
        }

        [HttpPost("report/{type}")]
        public async Task<IActionResult> UpdateReport(string type, [FromBody] TranscriptionUpdateModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Content))
                return BadRequest("Conteúdo obrigatório.");

            string filename = GetReportFilename(type);
            if (filename == null) return BadRequest("Tipo de relatório inválido.");

            string folder = Path.Combine(_baseSecurePath, "relatorios");
            string filePath = Path.Combine(folder, filename);

            // Se não existir, avisa (ou cria, dependendo da regra, mas melhor avisar se for erro de path)
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"DEBUG: Arquivo original não existe: {filePath}");
            }

            try
            {
                await System.IO.File.WriteAllTextAsync(filePath, model.Content);
                return Ok(new { message = "Relatório salvo com sucesso." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Erro ao salvar relatório: {ex.Message}");
            }
        }

        private string GetReportFilename(string type)
        {
            return type.ToLower() switch
            {
                "analitico" => "relatorio-analitico-defesa.txt",
                "tecnico" => "relatorio-tecnico-metodologia.txt",
                "chat" => "relatorio-chat-reconstruido.txt",
                _ => null
            };
        }

        private async Task<int> UpdateChatReportTranscriptionBlock(string id, string transcriptionContent)
        {
            var reportFolder = Path.Combine(_baseSecurePath, "relatorios");
            var reportPath = Path.Combine(reportFolder, "relatorio-chat-reconstruido.txt");

            if (!System.IO.File.Exists(reportPath))
            {
                throw new System.IO.FileNotFoundException($"Relatório de chat não encontrado: {reportPath}");
            }

            var reportContent = await System.IO.File.ReadAllTextAsync(reportPath);

            var newline = reportContent.Contains("\r\n", System.StringComparison.Ordinal) ? "\r\n" : "\n";
            var normalizedBlockContent = NormalizeTranscriptionForReport(transcriptionContent, newline);

            var updated = ReplaceTranscriptionBlocksInChatReport(reportContent, id, normalizedBlockContent, newline, out var replacements);

            if (updated == reportContent)
            {
                return 0;
            }

            await System.IO.File.WriteAllTextAsync(reportPath, updated);
            return replacements;
        }

        private static string ReplaceTranscriptionBlocksInChatReport(
            string reportContent,
            string id,
            string normalizedBlockContent,
            string newline,
            out int replacements)
        {
            replacements = 0;
            if (string.IsNullOrEmpty(reportContent)) return reportContent;
            if (string.IsNullOrWhiteSpace(id)) return reportContent;
            if (newline == null) newline = System.Environment.NewLine;

            var escapedId = Regex.Escape(id);
            var markerRegex = new Regex($@"\[[^\]]*Associado[^\]]*{escapedId}[^\]]*\]", RegexOptions.IgnoreCase);
            var closingBraceRegex = new Regex(@"(?m)^[ \t]*\}", RegexOptions.IgnoreCase);

            var sb = new StringBuilder(reportContent.Length);
            var currentIndex = 0;
            var searchIndex = 0;

            while (searchIndex < reportContent.Length)
            {
                var match = markerRegex.Match(reportContent, searchIndex);
                if (!match.Success)
                {
                    sb.Append(reportContent, currentIndex, reportContent.Length - currentIndex);
                    break;
                }

                var afterMarkerIndex = match.Index + match.Length;
                var openBraceIndex = reportContent.IndexOf('{', afterMarkerIndex);
                if (openBraceIndex < 0)
                {
                    searchIndex = afterMarkerIndex;
                    continue;
                }

                var contentStartIndex = IndexAfterLineBreak(reportContent, openBraceIndex);
                if (contentStartIndex <= openBraceIndex || contentStartIndex >= reportContent.Length)
                {
                    searchIndex = openBraceIndex + 1;
                    continue;
                }

                var closingBraceMatch = closingBraceRegex.Match(reportContent, contentStartIndex);
                if (!closingBraceMatch.Success)
                {
                    searchIndex = openBraceIndex + 1;
                    continue;
                }

                sb.Append(reportContent, currentIndex, contentStartIndex - currentIndex);

                var replacement = normalizedBlockContent ?? string.Empty;
                if (!replacement.EndsWith(newline, System.StringComparison.Ordinal))
                {
                    replacement += newline;
                }
                sb.Append(replacement);

                currentIndex = closingBraceMatch.Index;
                searchIndex = closingBraceMatch.Index + closingBraceMatch.Length;
                replacements++;
            }

            return sb.ToString();
        }

        private static int IndexAfterLineBreak(string text, int startIndex)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= text.Length) return text.Length;

            var newlineIndex = text.IndexOf('\n', startIndex);
            return newlineIndex < 0 ? text.Length : newlineIndex + 1;
        }

        private static string NormalizeTranscriptionForReport(string transcriptionContent, string newline)
        {
            if (transcriptionContent == null) return string.Empty;
            if (newline == null) newline = System.Environment.NewLine;

            var lines = Regex.Split(transcriptionContent, "\r\n|\n|\r");
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                line = Regex.Replace(line, @"^\s*\d{3}\.\s*", "");
                line = line.Trim();
                lines[i] = line.Length == 0 ? string.Empty : $"    {line}";
            }

            return string.Join(newline, lines);
        }
    }

    public class TranscriptionUpdateModel
    {
        public string Content { get; set; }
    }
}
