using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.SuperIA;
using StarkAid.Api.Entities;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Web;

namespace StarkAid.Api.Services.V1.SuperIA
{
    public class IaService
    {
        private readonly string _groApiKey;
        private readonly string _openRouterKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<IaService> _logger;
        private readonly IServiceProvider _provider;

        public IaService(IServiceProvider provider, IConfiguration configuration, HttpClient httpClient, ILogger<IaService> logger)
        {
            _groApiKey = configuration["IaApiKeys:GroApiKey"]
                ?? throw new InvalidOperationException("GroApiKey não configurada.");
            _openRouterKey = configuration["IaApiKeys:OpenRouterKEY"]
                ?? throw new InvalidOperationException("OpenRouterKey não configurada.");
            _httpClient = httpClient;
            _logger = logger;
            _provider = provider;
        }

        public async Task<IaResultado?> ProcessarMensagem(string contextoUser, string contextoIA, string texto, string estilo)
        {
            var mensagens = new[]
            {
                new { role = "system", content = "Você é o Assistente StarkAid, desenvolvido pela StarkAid, para ser um assistente de automacao por voz inteligente. Responda de forma curta e direta." },
                new { role = "user", content = contextoUser },
                new { role = "assistant", content = contextoIA },
                new { role = "user", content = texto }
            };

            if (!string.IsNullOrEmpty(estilo))
            {
                mensagens = new[]
                {
                    new { role = "system", content =
                        $"Você é o Assistente StarkAid, " +
                        $"desenvolvido pela StarkAid, para ser um assistente de automacao por voz inteligente. " +
                        $"Responda de forma curta e direta. se receber mensagens pedindo acender apagar algo, " +
                        $"ligar desligar algo diga que nao encontrou este dispositivo " +
                        $"se for comandos de software tipo para abrir fechar algo como camera ou algum app " +
                        $"diga que ainda nao implementamos esta função. se for comando ativar inteligencia de respostas aleatorias dizendo que ativou inteligencia." +
                        $" sua personalidade deve ser {estilo}" },
                    new { role = "user", content = contextoUser },
                    new { role = "assistant", content = contextoIA },
                    new { role = "user", content = texto }
                };
            }

            var respostaGroq = await ChamarGroq(mensagens);
            if (respostaGroq != null) return respostaGroq;

            var respostaOpenRouter = await ChamarOpenRouter(mensagens);
            return respostaOpenRouter;
        }

        public async Task<(bool Sucesso, string Texto)> ChamarStarkNlp(string fraseOriginal)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var config = await _context.ConfiguracoesStarkNlp.FirstOrDefaultAsync();
                if (config == null || string.IsNullOrWhiteSpace(config.StarkNlpUrl))
                    return (false, "URL do StarkNLP não configurada.");

                var encoded = HttpUtility.UrlEncode(fraseOriginal);

                var url = $"{config.StarkNlpUrl.TrimEnd('/')}/random-answers?resposta={encoded}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var response = await _httpClient.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro StarkNLP: {0}", body);
                    return (false, $"Erro do servidor NLP: {body}");
                }

                try
                {
                    using var doc = JsonDocument.Parse(body);

                    var alternativas = doc.RootElement
                        .GetProperty("alternativas")
                        .EnumerateArray()
                        .Select(x => x.GetString() ?? "")
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                    if (alternativas.Length == 0)
                        return (false, "Nenhuma alternativa retornada pela IA.");

                    var jsonFinal = JsonSerializer.Serialize(new { alternativas });

                    return (true, jsonFinal);
                }
                catch (Exception exJson)
                {
                    _logger.LogError(exJson, "JSON veio inválido. Conteúdo: {0}", body);

                    var partes = body.Split('\n')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                    var jsonFinal = JsonSerializer.Serialize(new { alternativas = partes });

                    return (true, jsonFinal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao chamar StarkNLP");
                return (false, $"Erro local: {ex.Message}");
            }
        }

        public async Task<IaResultado?> ProcessarMensagemWpp(string contextoUser, string contextoIA, string texto, string estilo)
        {
            var mensagens = new[]
            {
                new {role = "system", content = @"INSTRUÇÕES CRÍTICAS:
1. Você deve retornar APENAS o texto da mensagem pronta para enviar
2. NUNCA comece com 'Você pode dizer:', 'Diga:', ou qualquer prefixo
3. NUNCA use placeholders como [seu nome], [nome], etc.
4. A mensagem deve estar 100% completa e direta
5. Se o usuário pedir para 'diga que...', remova o 'diga que' e escreva diretamente

EXEMPLOS:
Input: 'diga que não posso ir hoje na festa'
Output: 'Desculpe, não poderei ir à festa hoje.'

Input: 'avise que não vou à reunião'
Output: 'Não poderei comparecer à reunião.'"
                },
                new { role = "user", content = contextoUser },
                new { role = "assistant", content = contextoIA },
                new { role = "user", content = texto }
            };

            if (!string.IsNullOrEmpty(estilo))
            {
                mensagens = new[]
                {
                    new {role = "system", content = @$"INSTRUÇÕES CRÍTICAS:
1. Você deve retornar APENAS o texto da mensagem pronta para enviar
2. NUNCA comece com 'Você pode dizer:', 'Diga:', ou qualquer prefixo
3. NUNCA use placeholders como [seu nome], [nome], etc.
4. A mensagem deve estar 100% completa e direta
5. Se o usuário pedir para 'diga que...', remova o 'diga que' e escreva diretamente
6. Voce deve expressar a personalidade {estilo} nas respostas

EXEMPLOS:
Input: 'diga que não posso ir hoje na festa'
Output: 'Descilpe, não poderei ir à festa hoje.'

Input: 'avise que não vou à reunião'
Output: 'Não poderei comparecer à reunião.'"
                    },
                    new { role = "user", content = contextoUser },
                    new { role = "assistant", content = contextoIA },
                    new { role = "user", content = texto }
                };
            }

            var respostaGroq = await ChamarGroq(mensagens);
            if (respostaGroq != null) return respostaGroq;

            var respostaOpenRouter = await ChamarOpenRouter(mensagens);
            return respostaOpenRouter;
        }

        public async Task<IaResultado?> ProcessarMensagemJson(object[] mensagens)
        {
            var resultadoOpen = await ChamarOpenRouter(mensagens);
            if (resultadoOpen != null)
                return resultadoOpen;

            var resultadoGroq = await ChamarGroq(mensagens);
            return resultadoGroq;
        }

        private async Task<IaResultado?> ChamarGroq(object[] mensagens)
        {
            var requestBody = new
            {
                model = "llama3-8b-8192",
                messages = mensagens,
                max_tokens = 150,
                temperature = 0.5
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_groApiKey}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var texto = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            var usage = doc.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

            return new IaResultado
            {
                Texto = texto ?? "",
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                Modelo = "groq-llama3-8b-8192"
            };
        }

        private static readonly string[] ModelosFree = new[]
        {
            "google/gemini-2.0-flash-exp:free",
            "meta-llama/llama-3.3-70b-instruct:free",
            "google/gemma-3-27b-it:free",
            "nousresearch/hermes-3-405b:free",
            "meta-llama/llama-3.2-3b-instruct:free"
        };

        public async Task<IaResultado?> ChamarOpenRouter(object[] mensagens)
        {
            foreach (var modelo in ModelosFree)
            {
                var resultado = await TentarModelo(modelo, mensagens);
                if (resultado != null)
                    return resultado;
            }

            // Nenhum modelo respondeu
            return null;
        }
        private async Task<IaResultado?> TentarModelo(string modelo, object[] mensagens)
        {
            var requestBody = new
            {
                model = modelo,
                messages = mensagens,
                max_tokens = 150,
                temperature = 0.6
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://openrouter.ai/api/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _openRouterKey);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Log útil para debug
                var erro = await response.Content.ReadAsStringAsync();
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var texto = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var usage = doc.RootElement.GetProperty("usage");

            return new IaResultado
            {
                Texto = texto ?? "",
                PromptTokens = usage.GetProperty("prompt_tokens").GetInt32(),
                CompletionTokens = usage.GetProperty("completion_tokens").GetInt32(),
                Modelo = modelo
            };
        }
        //public async Task<IaResultado?> ChamarOpenRouter(object[] mensagens)
        //{
        //    var requestBody = new
        //    {
        //        model = "gpt-4o-mini",
        //        messages = mensagens,
        //        max_tokens = 150,
        //        temperature = 0.6
        //    };

        //    var requestJson = JsonSerializer.Serialize(requestBody);
        //    var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
        //    {
        //        Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        //    };
        //    request.Headers.Add("Authorization", $"Bearer {_openRouterKey}");

        //    var response = await _httpClient.SendAsync(request);
        //    if (!response.IsSuccessStatusCode) return null;

        //    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        //    var texto = doc.RootElement.GetProperty("choices")[0]
        //        .GetProperty("message")
        //        .GetProperty("content")
        //        .GetString()?.Trim();

        //    var usage = doc.RootElement.GetProperty("usage");
        //    var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
        //    var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

        //    return new IaResultado
        //    {
        //        Texto = texto ?? "",
        //        PromptTokens = promptTokens,
        //        CompletionTokens = completionTokens,
        //        Modelo = "openrouter-gpt-4o-mini"
        //    };
        //}



        public async Task<string> ResumirTexto(string texto, string estilo)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;

            var mensagens = new[]
            {
                new { role = "system", content = "Você é um assistente que resume textos para economizar tokens. Faça resumo curto, mantendo apenas informações importantes." },
                new { role = "user", content = texto }
            };

            if (!string.IsNullOrEmpty(estilo))
            {
                mensagens = new[]
                {
                    new { role = "system", content = $"Você é um assistente que resume textos para economizar tokens. Faça resumo curto, mantendo apenas informações importantes. Voce deve expressar a personalidade {estilo} em suas respostas" },
                    new { role = "user", content = texto }
                };
            }

            var resultado = await ChamarOpenRouter(mensagens);
            return resultado?.Texto ?? texto;
        }

        public async Task<string?> GerarRespostasAlternativas(string respostaOriginal, string estilo)
        {
            var mensagens = new[]
            {
                new { role = "system", content = "Você é uma IA que reescreve frases. Crie exatamente 4 variações diferentes que transmitam o mesmo significado, da frase original. Nao use frases muito formais seja simples e direto. Responda SOMENTE em JSON no formato: { \"alternativas\": [\"...\",\"...\",\"...\",\"...\"] }" },
                new { role = "user", content = respostaOriginal }
            };

            if (!string.IsNullOrEmpty(estilo))
            {
                mensagens = new[]
                {
                    new { role = "system", content = "Você é uma IA que reescreve frases. Crie exatamente 4 variações diferentes que transmitam o mesmo significado, da frase original. Nao use frases muito formais seja simples e direto. Responda SOMENTE em JSON no formato: { \"alternativas\": [\"...\",\"...\",\"...\",\"...\"] }. " + $"Voce deve expressar a personalidade {estilo} em suas respostas" },
                    new { role = "user", content = respostaOriginal }
                };
            }

            var resultado = await ChamarOpenRouter(mensagens) ?? await ChamarGroq(mensagens);
            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
                return null;

            return resultado.Texto;
        }
    }
}
