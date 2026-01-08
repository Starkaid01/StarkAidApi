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
using System.Linq;
using System.Collections.Concurrent;
using System.Net;

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

        private async Task<IaResultado?> ChamarGroq(object[] mensagens, int maxTokens = 300)
        {
            const string modelo = "llama-3.3-70b-versatile";
            if (ModeloEsgotado(modelo)) return null;

            var requestBody = new
            {
                model = modelo,
                messages = mensagens,
                max_tokens = maxTokens,
                temperature = 0.5
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_groApiKey}");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro de rede ao chamar Groq");
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                MarcarModeloComoEsgotado(modelo, response);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Groq retornou erro {0}: {1}", response.StatusCode, erro);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var texto = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            var usage = doc.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

            // Fallback Groq (embora Groq costume cobrar, melhor prevenir)
            if (promptTokens == 0) promptTokens = (requestJson.Length / 4);
            if (completionTokens == 0) completionTokens = ((texto ?? "").Length / 4);

            return new IaResultado
            {
                Texto = texto ?? "",
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                Modelo = modelo
            };
        }

        private static readonly string[] ModelosFree = new[]
        {
            "google/gemini-2.0-flash-exp:free",
            "meta-llama/llama-3.3-70b-instruct:free",
            "google/gemma-3-27b-it:free",
            "meta-llama/llama-3.2-3b-instruct:free"
        };

        private class ModeloEstado
        {
            public bool Esgotado { get; set; }
            public DateTimeOffset? RetryAfter { get; set; }
        }

        private static readonly ConcurrentDictionary<string, ModeloEstado> EstadoModelos = new();

        private bool ModeloEsgotado(string modelo)
        {
            if (!EstadoModelos.TryGetValue(modelo, out var estado))
                return false;

            if (estado.RetryAfter == null)
                return estado.Esgotado;

            if (DateTimeOffset.UtcNow >= estado.RetryAfter)
            {
                EstadoModelos.TryRemove(modelo, out _);
                return false;
            }

            return true;
        }

        private void MarcarModeloComoEsgotado(string modelo, HttpResponseMessage response)
        {
            DateTimeOffset? retryAfter = null;

            // 1. Tentar ler Retry-After (segundos ou data)
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var val = values.First();
                if (int.TryParse(val, out var seconds))
                    retryAfter = DateTimeOffset.UtcNow.AddSeconds(seconds);
                else if (DateTimeOffset.TryParse(val, out var dto))
                    retryAfter = dto;
            }

            // 2. Tentar ler x-ratelimit-reset (Unix timestamp) - Comum na OpenRouter/Groq
            if (retryAfter == null && response.Headers.TryGetValues("x-ratelimit-reset", out var resetValues))
            {
                if (long.TryParse(resetValues.First(), out var unixTimestamp))
                {
                    // Se for um timestamp muito grande, é data. Se pequeno, segundos.
                    if (unixTimestamp > 1000000000)
                        retryAfter = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                    else
                        retryAfter = DateTimeOffset.UtcNow.AddSeconds(unixTimestamp);
                }
            }

            EstadoModelos[modelo] = new ModeloEstado
            {
                Esgotado = true,
                RetryAfter = retryAfter ?? DateTimeOffset.UtcNow.AddMinutes(10) // Fallback 10 min
            };
            
            _logger.LogWarning("⚠️ Modelo {0} em cooldown. Retorno previsto: {1}", modelo, EstadoModelos[modelo].RetryAfter);
        }

        public async Task<IaResultado?> ChamarOpenRouter(object[] mensagens, int maxTokens = 300)
        {
            foreach (var modelo in ModelosFree)
            {
                if (ModeloEsgotado(modelo)) 
                {
                    _logger.LogInformation("⏭️ Pulando modelo esgotado: {0}", modelo);
                    continue;
                }

                var resultado = await TentarModelo(modelo, mensagens, maxTokens);
                if (resultado != null)
                    return resultado;
            }

            // Nenhum modelo respondeu ou todos esgotados
            return null;
        }

        private async Task<IaResultado?> TentarModelo(string modelo, object[] mensagens, int maxTokens)
        {
            var requestBody = new
            {
                model = modelo,
                messages = mensagens,
                max_tokens = maxTokens,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro de rede ao chamar modelo {0}", modelo);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                MarcarModeloComoEsgotado(modelo, response);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Modelo {0} retornou erro {1}: {2}", modelo, response.StatusCode, erro);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                _logger.LogWarning("Modelo {0} retornou resposta sem choices.", modelo);
                return null;
            }

            var texto = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var usage = doc.RootElement.GetProperty("usage");

            // Fallback de Tokens se a API retornar 0 (comum em modelos free/beta)
            int pTokens = usage.GetProperty("prompt_tokens").GetInt32();
            int cTokens = usage.GetProperty("completion_tokens").GetInt32();

            if (pTokens == 0) pTokens = (requestJson.Length / 4);
            if (cTokens == 0) cTokens = ((texto ?? "").Length / 4);

            return new IaResultado
            {
                Texto = texto ?? "",
                PromptTokens = pTokens,
                CompletionTokens = cTokens,
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

            var resultado = await ChamarOpenRouter(mensagens, maxTokens: 400) ?? await ChamarGroq(mensagens, maxTokens: 400);
            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
                return null;

            return resultado.Texto;
        }

        public async Task<List<string>> GerarVariacoesParaGlobal(string pergunta, string respostaBase)
        {
            var mensagens = new[]
            {
                new { role = "system", content = "Você é um gerador de variações de resposta para uma base de conhecimento factual. " +
                                                 "Gere 3 respostas semanticamente equivalentes à resposta base enviada, mas com formulações diferentes. " +
                                                 "As respostas devem ser neutras, objetivas e profissionais. Não use gírias nem invente fatos novos. " +
                                                 "Mantenha o mesmo conteúdo factual. Responda SOMENTE um JSON no formato: { \"variacoes\": [\"...\",\"...\",\"...\"] }" },
                new { role = "user", content = $"Pergunta: {pergunta}\nResposta base: {respostaBase}" }
            };

            var resultado = await ChamarOpenRouter(mensagens, maxTokens: 500) ?? await ChamarGroq(mensagens, maxTokens: 500);
            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            {
                _logger.LogWarning("IA retornou texto vazio ou nulo ao gerar variações.");
                return new List<string>();
            }

            try
            {
                _logger.LogInformation("GerarVariacoesParaGlobal - Resposta Bruta da IA: {0}", resultado.Texto);

                // Limpeza básica se a IA retornar markdown
                var text = resultado.Texto.Replace("```json", "").Replace("```", "").Trim();

                // Tentar extrair apenas o objeto JSON se houver texto ao redor
                int startIndex = text.IndexOf('{');
                int endIndex = text.LastIndexOf('}');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    text = text.Substring(startIndex, endIndex - startIndex + 1);
                }

                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("variacoes", out var variacoes))
                {
                    var lista = variacoes.EnumerateArray()
                        .Select(v => v.GetString() ?? "")
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToList();
                        
                    _logger.LogInformation("GerarVariacoesParaGlobal - {0} variações extraídas com sucesso.", lista.Count);
                    return lista;
                }
                else
                {
                     _logger.LogWarning("JSON válido mas propriedade 'variacoes' não encontrada. JSON: {0}", text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao parsear variações da IA. Conteúdo original: {0}", resultado.Texto);
            }

            return new List<string>();
        }
        public async Task<string> ClassifyMusicIntent(string text)
        {
            var mensagens = new[]
            {
                new { role = "system", content = "Você é um classificador de intenção musical. O usuário dirá algo relacionado a música. Se ele estiver pedindo para tocar uma música específica, responda APENAS 'Emusica'. Se ele estiver pedindo para tocar um artista, banda ou cantor (para tocar várias músicas deles), responda APENAS 'Eartista'. Responda APENAS a flag." },
                new { role = "user", content = text }
            };

            var resultado = await ChamarOpenRouter(mensagens, maxTokens: 10) ?? await ChamarGroq(mensagens, maxTokens: 10);
            
            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
                return "Emusica"; // Fallback seguro
                
            var content = resultado.Texto.Trim().Replace("\"", "").Replace("'", "");
            
            if (content.Contains("Eartista", StringComparison.OrdinalIgnoreCase)) return "Eartista";
            return "Emusica";
        }
    }
}
