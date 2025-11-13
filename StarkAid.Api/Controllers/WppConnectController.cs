using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.WPPconnect;
using StarkAid.Api.Entities;
using StarkAid.Api.Options;
using StarkAid.Api.Services.SocialCommand;
using StarkAid.Api.Services.WPPconnect;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StarkAid.Api.Controllers
{
    [Route("api/wpp")]
    [ApiController]
    public class WppConnectController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly WppConnectOptions _options;
        private readonly ComandoSocialService _service;

        public WppConnectController(AppDbContext context, IHttpClientFactory httpFactory, IOptions<WppConnectOptions> options, ComandoSocialService service)
        {
            _context = context;
            _httpClient = httpFactory.CreateClient();
            _options = options.Value;
            _service = service;
        }

        [HttpPost("status-session")]
        public async Task<IActionResult> GetStatusSession([FromBody] SessionRequestDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName && s.IsActive);

            if (session == null)
                return BadRequest("Sessão não encontrada ou inativa.");

            try
            {
                var statusUrl = $"{config.DominioCloudflare}/api/{dto.SessionName}/status-session";

                var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                // 🔍 Log automático do retorno
                var logPath = $"C:\\temp\\wpp_status_session_{dto.SessionName}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                Directory.CreateDirectory("C:\\temp");
                System.IO.File.WriteAllText(logPath, json);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        Error = "Falha ao obter status da sessão",
                        Response = json,
                        Endpoint = statusUrl,
                        Token = session.Token[..Math.Min(15, session.Token.Length)] + "...",
                        LogFile = logPath
                    });
                }

                // ✅ Converte o JSON de retorno e adiciona metadados úteis
                var data = JsonDocument.Parse(json).RootElement;
                string status = data.TryGetProperty("status", out var st) ? st.GetString() : "UNKNOWN";
                string version = data.TryGetProperty("version", out var ver) ? ver.GetString() : null;
                string qrcode = data.TryGetProperty("qrcode", out var qr) ? qr.GetString() : null;

                // ✅ Retorno final padronizado
                return Ok(new
                {
                    Session = dto.SessionName,
                    Status = status,
                    Version = version,
                    QrCode = qrcode,
                    CloudflareEndpoint = config.DominioCloudflare,
                    TokenPrefix = session.Token[..Math.Min(10, session.Token.Length)] + "...",
                    LogFile = logPath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Erro interno ao consultar status da sessão",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }



        // ====================================================
        // 1️⃣ Criar sessão e retornar link da página HTML
        // ====================================================
        [HttpPost("session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var user = await _context.Users.FindAsync(userIdGuid);
            if (user == null)
                return Unauthorized("Usuário não encontrado.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName);

            string token;

            if (session == null)
            {
                // Gera token no WPPConnect
                var tokenResponse = await _httpClient.PostAsync(
                    $"{config.DominioCloudflare}/api/{dto.SessionName}/THISISMYSECURETOKEN/generate-token",
                    null);

                if (!tokenResponse.IsSuccessStatusCode)
                    return StatusCode((int)tokenResponse.StatusCode, await tokenResponse.Content.ReadAsStringAsync());

                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                token = tokenData?["token"] ?? throw new Exception("Token não retornado pelo WPPConnect.");

                session = new UserSession
                {
                    UserId = userIdGuid,
                    SessionName = dto.SessionName,
                    Token = token,
                    IsActive = true
                };
                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();
            }
            else
            {
                token = session.Token;
                session.IsActive = true;
                await _context.SaveChangesAsync();
            }

            // Cria sessão no servidor
            var request = new HttpRequestMessage(HttpMethod.Post, $"{config.DominioCloudflare}/api/{dto.SessionName}/start-session")
            {
                Content = JsonContent.Create(new { webhook = dto.Webhook ?? $"https://starkaid.vbweb.com.br/api/wpp/webhook" })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var startResponse = await _httpClient.SendAsync(request);
            var startResult = await startResponse.Content.ReadAsStringAsync();

            if (!startResponse.IsSuccessStatusCode)
                return StatusCode((int)startResponse.StatusCode, startResult);

            // 🔗 Retorna link para página HTML que atualiza QRCode automaticamente
            var pageUrl = $"https://starkaid.vbweb.com.br/wpp/session-page?session={dto.SessionName}&userid={userIdGuid}";
            return Ok(new { Url = pageUrl });
        }

        // ====================================================
        // 2️⃣ Página HTML de status da sessão
        // ====================================================
        [HttpGet("/wpp/session-page")]
        public IActionResult SessionPage([FromQuery] string session, [FromQuery] string userid)
        {
            var html = $@"
            <!DOCTYPE html>
            <html lang='pt-br'>
            <head>
            <meta charset='utf-8'/>
            <title>WhatsApp - Sessão {session}</title>
            <meta name='viewport' content='width=device-width, initial-scale=1'/>
            <style>
            body {{ font-family: Arial, sans-serif; text-align:center; background:#111; color:#eee; padding:20px; }}
            h2 {{ margin-top:20px; }}
            img {{ margin-top:30px; width:260px; height:260px; border:4px solid #444; border-radius:10px; }}
            .status {{ margin-top:15px; font-size:18px; }}
            </style>
            </head>
            <body>
            <h2>Conectando ao WhatsApp</h2>
            <div id='status' class='status'>Carregando QR Code...</div>
            <img id='qrcode' src='' alt='QR Code' hidden />

            <script>
            async function atualizar() {{
              try {{
                const resp = await fetch('/api/wpp/session-status?session={session}&userid={userid}');
                const data = await resp.json();

                const statusEl = document.getElementById('status');
                const qrImg = document.getElementById('qrcode');

                if (data.status === 'CONNECTED') {{
                  statusEl.textContent = '✅ Sessão conectada!';
                  qrImg.hidden = true;
                }} else if (data.qrcode) {{
                  statusEl.textContent = '📱 Escaneie o QR Code abaixo no WhatsApp';
                  qrImg.src = 'data:image/png;base64,' + data.qrcode;
                  qrImg.hidden = false;
                }} else {{
                  statusEl.textContent = '⌛ Aguardando QR Code...';
                }}
              }} catch (e) {{
                document.getElementById('status').textContent = 'Erro: ' + e.message;
              }}
            }}

            setInterval(atualizar, 60000);
            atualizar();
            </script>
            </body>
            </html>";
            return Content(html, "text/html");
        }

        // ====================================================
        // 3️⃣ Endpoint chamado pelo HTML (status + QR)
        // ====================================================
        [HttpGet("session-status")]
        public async Task<IActionResult> GetSessionStatus([FromQuery] string session, [FromQuery] string userid)
        {
            if (!Guid.TryParse(userid, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == session && s.IsActive);

            if (userSession == null)
                return BadRequest("Sessão inexistente.");

            var request = new HttpRequestMessage(HttpMethod.Get, $"{config.DominioCloudflare}/api/{session}/status-session");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userSession.Token);

            var resp = await _httpClient.SendAsync(request);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, json);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : "UNKNOWN";
            var qr = root.TryGetProperty("qrcode", out var q) ? q.GetString() : null;

            return Ok(new { status, qrcode = qr });
        }

        // ==========================
        // Enviar mensagem
        // ==========================
        [HttpPost("enviar-mensagem")]
        public async Task<IActionResult> EnviarMensagem([FromBody] SendMessageDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");


            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            // Recupera token da sessão
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName && s.IsActive);

            if (session == null)
                return BadRequest("Sessão não encontrada ou inativa. Crie a sessão primeiro.");

            var respMessage = await _service.CriaeMessageWpp(userIdGuid, dto.Message, dto.Estilo);
            var requestBody = new
            {
                phone = dto.PhoneNumber,
                isGroup = dto.IsGroup,
                isNewsletter = dto.IsNewsletter,
                isLid = dto.IsLid,
                message = respMessage
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{config.DominioCloudflare}/api/{dto.SessionName}/send-message")
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            return Content(result, "application/json");
        }

        // ==========================
        // Desconectar sessão
        // ==========================
        [HttpPost("desconectar")]
        public async Task<IActionResult> Desconectar([FromBody] CloseSessionDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName && s.IsActive);

            if (session == null)
                return BadRequest("Sessão não encontrada ou já inativa.");

            // Fechar sessão
            var request = new HttpRequestMessage(HttpMethod.Post, $"{config.DominioCloudflare}/api/{dto.SessionName}/close-session");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            // Marca como inativa
            session.IsActive = false;
            await _context.SaveChangesAsync();

            return Content(result, "application/json");
        }

        // ==========================
        // Obter todas as mensagens não lidas (versão v2+)
        // ==========================
        [HttpPost("unread-messages")]
        public async Task<IActionResult> GetUnreadMessages([FromBody] UnreadMessagesDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName && s.IsActive);

            if (session == null)
                return BadRequest("Sessão não encontrada ou inativa.");

            var url = $"{config.DominioCloudflare}/api/{dto.SessionName}/all-chats-with-messages";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, json);

            var root = JsonDocument.Parse(json).RootElement;

            JsonElement chatsArray;

            // ✅ Adapta automaticamente se o retorno vier como { "response": [...] } ou diretamente como [...]
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("response", out var resp))
                chatsArray = resp;
            else if (root.ValueKind == JsonValueKind.Array)
                chatsArray = root;
            else
                return BadRequest("Formato inesperado retornado pelo WPPConnect.");

            var unreadMessages = new List<object>();

            foreach (var chat in chatsArray.EnumerateArray())
            {
                if (chat.TryGetProperty("messages", out var messages))
                {
                    foreach (var msg in messages.EnumerateArray())
                    {
                        if (msg.TryGetProperty("isNewMsg", out var isNewMsg) && isNewMsg.GetBoolean())
                        {
                            unreadMessages.Add(new
                            {
                                ChatName = chat.TryGetProperty("name", out var name) ? name.GetString() : "(sem nome)",
                                From = msg.GetProperty("from").GetString(),
                                Body = msg.GetProperty("body").GetString(),
                                Timestamp = msg.GetProperty("timestamp").GetInt64(),
                                Type = msg.TryGetProperty("type", out var type) ? type.GetString() : "text"
                            });
                        }
                    }
                }
            }

            return Ok(new
            {
                Session = dto.SessionName,
                UnreadCount = unreadMessages.Count,
                Messages = unreadMessages
            });
        }


        [HttpPost("tem-mensagem-nao-lida-direto")]
        public async Task<IActionResult> TemMensagemNaoLidaDireto([FromBody] UnreadMessagesDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName && s.IsActive);

            if (session == null)
                return BadRequest("Sessão não encontrada ou inativa.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest("Número do contato é obrigatório.");

            // Normaliza número para endpoint WPPConnect
            string NormalizePhone(string phone)
            {
                if (string.IsNullOrEmpty(phone)) return null;
                var digits = new string(phone.Where(char.IsDigit).ToArray());
                return $"{digits}@c.us";
            }

            string targetPhone = NormalizePhone(dto.PhoneNumber);
            string myPhone = "553299861653@c.us"; // Seu número

            try
            {
                // ✅ PRIMEIRO: Verificar status da sessão
                var statusUrl = $"{config.DominioCloudflare}/api/{dto.SessionName}/status-session";
                var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
                statusRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var statusResponse = await _httpClient.SendAsync(statusRequest);
                var statusJson = await statusResponse.Content.ReadAsStringAsync();

                // Log do status
                var tempDir = "C:\\temp";
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                var statusLogPath = $"C:\\temp\\wpp_status_{dto.PhoneNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                System.IO.File.WriteAllText(statusLogPath, statusJson);

                if (!statusResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new
                    {
                        Error = "Sessão não está acessível",
                        StatusResponse = statusJson,
                        StatusCode = statusResponse.StatusCode
                    });
                }

                // ✅ Verificar se a sessão está CONNECTED
                var statusData = JsonDocument.Parse(statusJson).RootElement;
                var sessionStatus = SafeGetString(statusData, "status");

                if (sessionStatus != "CONNECTED")
                {
                    return BadRequest(new
                    {
                        Error = "Sessão não está conectada",
                        CurrentStatus = sessionStatus,
                        Message = "A sessão precisa estar no status 'CONNECTED' para buscar mensagens"
                    });
                }

                // ✅ SEGUNDO: Buscar todas as mensagens usando endpoint que funciona
                var url = $"{config.DominioCloudflare}/api/{dto.SessionName}/all-chats-with-messages";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                // Log completo do JSON
                var logPath = $"C:\\temp\\wpp_debug_{dto.PhoneNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                System.IO.File.WriteAllText(logPath, json);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        Error = "Erro ao buscar mensagens",
                        Response = json,
                        Endpoint = url
                    });
                }

                // Processar resposta
                var root = JsonDocument.Parse(json).RootElement;

                JsonElement chatsArray;

                // ✅ Adapta automaticamente se o retorno vier como { "response": [...] } ou diretamente como [...]
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("response", out var resp) && resp.ValueKind == JsonValueKind.Array)
                    chatsArray = resp;
                else if (root.ValueKind == JsonValueKind.Array)
                    chatsArray = root;
                else
                    return BadRequest("Formato inesperado retornado pelo WPPConnect.");

                bool temNaoLida = false;
                var mensagensNaoLidas = new List<object>();

                // ✅ Função auxiliar para obter valores de forma segura
                string SafeGetString(JsonElement element, string propertyName)
                {
                    try
                    {
                        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                            return null;

                        if (element.TryGetProperty(propertyName, out var prop))
                        {
                            return prop.ValueKind switch
                            {
                                JsonValueKind.String => prop.GetString(),
                                JsonValueKind.Number => prop.GetRawText(),
                                JsonValueKind.True => "true",
                                JsonValueKind.False => "false",
                                JsonValueKind.Null => null,
                                JsonValueKind.Undefined => null,
                                JsonValueKind.Object => "[Object]",
                                JsonValueKind.Array => "[Array]",
                                _ => prop.ToString()
                            };
                        }
                        return null;
                    }
                    catch (Exception ex)
                    {
                        return $"[Error: {ex.Message}]";
                    }
                }

                int SafeGetInt(JsonElement element, string propertyName, int defaultValue = 0)
                {
                    try
                    {
                        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                            return defaultValue;

                        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
                        {
                            return prop.GetInt32();
                        }
                        return defaultValue;
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                long SafeGetLong(JsonElement element, string propertyName, long defaultValue = 0)
                {
                    try
                    {
                        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                            return defaultValue;

                        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
                        {
                            return prop.GetInt64();
                        }
                        return defaultValue;
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                // Buscar mensagens não lidas específicas do número
                foreach (var chat in chatsArray.EnumerateArray())
                {
                    try
                    {
                        // Verificar se é o chat do número desejado
                        var chatId = SafeGetString(chat, "id");

                        // Verificar se é o chat do número alvo
                        var isTargetChat = chatId != null &&
                                          (chatId.Contains(dto.PhoneNumber.Replace("+", "").Replace(" ", "")) ||
                                           chatId == targetPhone);

                        if (isTargetChat)
                        {
                            // ✅ ABORDAGEM PRINCIPAL: Verificar unreadCount diretamente
                            var unreadCount = SafeGetInt(chat, "unreadCount");
                            if (unreadCount > 0)
                            {
                                temNaoLida = true;

                                // Buscar informações adicionais do chat
                                var chatName = SafeGetString(chat, "name");
                                var lastReceivedKey = SafeGetString(chat, "lastReceivedKey");
                                var timestamp = SafeGetLong(chat, "t");

                                mensagensNaoLidas.Add(new
                                {
                                    ChatId = chatId,
                                    ChatName = chatName,
                                    UnreadCount = unreadCount,
                                    LastReceivedKey = lastReceivedKey,
                                    Timestamp = timestamp,
                                    DetectionMethod = "unreadCount"
                                });
                            }
                        }
                    }
                    catch (Exception chatEx)
                    {
                        // Continua mesmo se um chat falhar
                        mensagensNaoLidas.Add(new
                        {
                            Error = $"Erro ao processar chat: {chatEx.Message}",
                            DetectionMethod = "error"
                        });
                    }
                }

                // ✅ TERCEIRA ABORDAGEM: Buscar mensagens específicas do chat usando endpoint direto
                // ✅ LÓGICA CORRIGIDA: Do SEU ponto de vista como destinatário
                try
                {
                    var directMessagesUrl = $"{config.DominioCloudflare}/api/{dto.SessionName}/all-messages-in-chat/{targetPhone}";
                    var directRequest = new HttpRequestMessage(HttpMethod.Get, directMessagesUrl);
                    directRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                    var directResponse = await _httpClient.SendAsync(directRequest);
                    if (directResponse.IsSuccessStatusCode)
                    {
                        var directJson = await directResponse.Content.ReadAsStringAsync();
                        var directRoot = JsonDocument.Parse(directJson).RootElement;

                        if (directRoot.ValueKind == JsonValueKind.Object &&
                            directRoot.TryGetProperty("response", out var directResp) &&
                            directResp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var msg in directResp.EnumerateArray())
                            {
                                try
                                {
                                    // ✅ LÓGICA CORRIGIDA: 
                                    // Do SEU ponto de vista como DESTINATÁRIO:
                                    // - Mensagem foi ENVIADA para mim (to == meu número) 
                                    // - E foi ENVIADA por ela (from == número da sua esposa)
                                    var from = SafeGetString(msg, "from");
                                    var to = SafeGetString(msg, "to");
                                    var ack = SafeGetLong(msg, "ack", -1);

                                    // ✅ CORREÇÃO FINAL: 
                                    // ack == 1: NÃO LIDA (você ainda não leu)
                                    // ack == 0: LIDA (você já leu)
                                    bool isMessageForMe = to == myPhone;
                                    bool isMessageFromTarget = from == targetPhone;

                                    // ✅ APENAS ack == 1 são mensagens NÃO LIDAS
                                    bool isUnread = ack == 1;

                                    if (isMessageForMe && isMessageFromTarget && isUnread)
                                    {
                                        temNaoLida = true;

                                        mensagensNaoLidas.Add(new
                                        {
                                            ChatId = targetPhone,
                                            ChatName = "Direct Endpoint",
                                            From = from,
                                            To = to,
                                            Body = SafeGetString(msg, "body"),
                                            Timestamp = SafeGetLong(msg, "timestamp"),
                                            Type = SafeGetString(msg, "type") ?? "text",
                                            Ack = ack,
                                            AckDescription = GetAckDescription(ack),
                                            Source = "direct-endpoint",
                                            DetectionMethod = "direct-endpoint-filtered",
                                            Perspective = "destinatário"
                                        });
                                    }
                                }
                                catch (Exception msgEx)
                                {
                                    // Continua mesmo se uma mensagem falhar
                                    mensagensNaoLidas.Add(new
                                    {
                                        Error = $"Erro ao processar mensagem direta: {msgEx.Message}",
                                        ChatId = targetPhone,
                                        DetectionMethod = "error"
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Não falha se o endpoint direto não funcionar
                    System.IO.File.WriteAllText($"C:\\temp\\wpp_direct_error_{dto.PhoneNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                        $"Erro no endpoint direto: {ex.Message}");
                }

                return Ok(new
                {
                    Session = dto.SessionName,
                    Phone = dto.PhoneNumber,
                    TargetPhone = targetPhone,
                    MyPhone = myPhone,
                    SessionStatus = sessionStatus,
                    TemNaoLida = temNaoLida,
                    MensagensCount = mensagensNaoLidas.Count,
                    Mensagens = mensagensNaoLidas,
                    TotalChats = chatsArray.GetArrayLength(),
                    LogFile = logPath,
                    StatusLog = statusLogPath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Erro interno",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        // ✅ Função auxiliar para descrever o status ACK
        private string GetAckDescription(long ack)
        {
            return ack switch
            {
                0 => "lida", // ✅ Você já leu esta mensagem
                1 => "não lida", // ✅ Você ainda não leu esta mensagem
                2 => "entregue",
                3 => "ouvida",
                _ => "desconhecido"
            };
        }

        [HttpPost("listar-contatos-salvos")]
        public async Task<IActionResult> ListarContatosDosChats([FromBody] SessionRequestDto dto)
        {
            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
                return BadRequest("Configuração ausente.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.SessionName == dto.SessionName && s.IsActive);

            if (session == null)
                return BadRequest("Sessão não encontrada ou inativa.");

            try
            {
                // ✅ Verificar status da sessão
                var statusUrl = $"{config.DominioCloudflare}/api/{dto.SessionName}/status-session";
                var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
                statusRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var statusResponse = await _httpClient.SendAsync(statusRequest);
                var statusJson = await statusResponse.Content.ReadAsStringAsync();

                if (!statusResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new
                    {
                        Error = "Sessão não está acessível",
                        StatusResponse = statusJson,
                        StatusCode = statusResponse.StatusCode
                    });
                }

                // ✅ Verificar se a sessão está CONNECTED
                var statusData = JsonDocument.Parse(statusJson).RootElement;
                var sessionStatus = SafeGetString(statusData, "status");

                if (sessionStatus != "CONNECTED")
                {
                    return BadRequest(new
                    {
                        Error = "Sessão não está conectada",
                        CurrentStatus = sessionStatus,
                        Message = "A sessão precisa estar no status 'CONNECTED' para listar contatos"
                    });
                }

                // ✅ Buscar todos os chats com mensagens
                var chatsUrl = $"{config.DominioCloudflare}/api/{dto.SessionName}/all-chats-with-messages";
                var chatsRequest = new HttpRequestMessage(HttpMethod.Get, chatsUrl);
                chatsRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

                var chatsResponse = await _httpClient.SendAsync(chatsRequest);
                var chatsJson = await chatsResponse.Content.ReadAsStringAsync();

                var debugPath = $"C:\\temp\\wpp_chats_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
                System.IO.File.WriteAllText(debugPath, chatsJson);

                if (!chatsResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)chatsResponse.StatusCode, new
                    {
                        Error = "Erro ao buscar chats",
                        Response = chatsJson,
                        Endpoint = chatsUrl
                    });
                }

                var root = JsonDocument.Parse(chatsJson).RootElement;
                JsonElement chatsArray;

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("response", out var resp) && resp.ValueKind == JsonValueKind.Array)
                    chatsArray = resp;
                else if (root.ValueKind == JsonValueKind.Array)
                    chatsArray = root;
                else
                    return BadRequest("Formato inesperado retornado pelo WPPConnect para chats.");

                var contatos = new List<object>();

                foreach (var chat in chatsArray.EnumerateArray())
                {
                    try
                    {
                        var contact = chat.TryGetProperty("contact", out var contactEl) ? contactEl : default;
                        if (contact.ValueKind == JsonValueKind.Undefined) continue;

                        var id = SafeGetString(contact, "id") ?? SafeGetString(chat, "id");
                        if (string.IsNullOrEmpty(id) || id.Contains("@g.us")) continue; // pula grupos

                        var numero = ExtrairNumero(id);
                        if (string.IsNullOrEmpty(numero) || !EhNumeroTelefoneValido(numero))
                            continue;

                        var name = SafeGetString(contact, "name");
                        var pushname = SafeGetString(contact, "pushname");
                        var shortName = SafeGetString(contact, "shortName");

                        var nomeExibicao =
                            !string.IsNullOrWhiteSpace(name) && !Regex.IsMatch(name, @"^\d+$") ? name.Trim() :
                            !string.IsNullOrWhiteSpace(pushname) && !Regex.IsMatch(pushname, @"^\d+$") ? pushname.Trim() :
                            !string.IsNullOrWhiteSpace(shortName) && !Regex.IsMatch(shortName, @"^\d+$") ? shortName.Trim() :
                            numero;

                        contatos.Add(new { Nome = nomeExibicao, Numero = numero });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar chat: {ex.Message}");
                    }
                }

                // ✅ Remover duplicatas e ordenar
                var contatosFinal = contatos
                    .GroupBy(c => ((dynamic)c).Numero)
                    .Select(g => g.First())
                    .OrderBy(c => ((dynamic)c).Nome)
                    .Select(c => new Contato
                    {
                        Nome = ((dynamic)c).Nome,
                        Numero = ((dynamic)c).Numero
                    })
                    .ToList();

                contatosFinal = ContatoFilter.FiltrarContatos(contatosFinal);

                return Ok(new
                {
                    Session = dto.SessionName,
                    SessionStatus = sessionStatus,
                    TotalContatosChats = contatosFinal.Count,
                    Contatos = contatosFinal,
                    DebugFile = debugPath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Erro interno",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        // ✅ Nova função para verificar se é um número de telefone válido
        private bool EhNumeroTelefoneValido(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return false;

            // Deve conter apenas dígitos
            if (!numero.All(char.IsDigit))
                return false;

            // Deve ter entre 10 e 15 dígitos (formato internacional)
            if (numero.Length < 10 || numero.Length > 15)
                return false;

            // Filtra números que são provavelmente IDs internos
            var prefixosInvalidos = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            if (prefixosInvalidos.Contains(numero.Substring(0, 1)) && numero.Length > 12)
                return false;

            return true;
        }

        // ✅ Função auxiliar para SafeGetBool
        private bool SafeGetBool(JsonElement element, string propertyName, bool defaultValue = false)
        {
            try
            {
                if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                    return defaultValue;

                if (element.TryGetProperty(propertyName, out var prop))
                {
                    return prop.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => bool.TryParse(prop.GetString(), out bool result) ? result : defaultValue,
                        JsonValueKind.Number => prop.GetInt32() != 0,
                        _ => defaultValue
                    };
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        // ✅ FUNÇÃO SafeGetString
        private string SafeGetString(JsonElement element, string propertyName)
        {
            try
            {
                if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                    return null;

                if (element.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Object)
                    {
                        if (propertyName == "id" && prop.TryGetProperty("_serialized", out var serialized))
                            return serialized.ValueKind == JsonValueKind.String ? serialized.GetString() : null;

                        if (propertyName == "id" && prop.TryGetProperty("user", out var user))
                            return user.ValueKind == JsonValueKind.String ? user.GetString() : null;
                        return null;
                    }

                    return prop.ValueKind switch
                    {
                        JsonValueKind.String => prop.GetString(),
                        JsonValueKind.Number => prop.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => null,
                        JsonValueKind.Undefined => null,
                        _ => null
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em SafeGetString: {ex.Message}");
                return null;
            }
        }

        // ✅ Função auxiliar para extrair número do contactId
        private string ExtrairNumero(string contactId)
        {
            if (string.IsNullOrEmpty(contactId))
                return null;

            if (contactId.EndsWith("@c.us"))
                return contactId.Substring(0, contactId.Length - 5);

            var atIndex = contactId.IndexOf('@');
            if (atIndex > 0)
                return contactId.Substring(0, atIndex);

            return contactId;
        }

        [HttpPost("mudanca-dominio-cloudflare")]
        public async Task<IActionResult> MudancaDominioCloudFlare([FromBody] WppConnectOptions dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId) ||
                string.IsNullOrWhiteSpace(dto.TokenDeAutenticacao) ||
                string.IsNullOrWhiteSpace(dto.NovoDominio))
                return BadRequest("Parâmetros inválidos.");

            if (!Guid.TryParse(dto.UserId, out var userIdGuid))
                return BadRequest("UserId inválido.");

            var user = await _context.Users.FindAsync(userIdGuid);
            if (user == null)
                return Unauthorized("Usuário não encontrado.");

            if (dto.TokenDeAutenticacao != user.ApiKey)
                return Unauthorized("Api-key inválida.");

            var novoDominio = dto.NovoDominio.Trim().TrimEnd('/');

            // Atualiza ou cria o registro de configuração no banco
            var config = await _context.ConfiguracoesSistema.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfiguracaoSistema
                {
                    DominioCloudflare = novoDominio,
                    UltimaAtualizacao = DateTime.UtcNow
                };
                _context.ConfiguracoesSistema.Add(config);
            }
            else
            {
                config.DominioCloudflare = novoDominio;
                config.UltimaAtualizacao = DateTime.UtcNow;
                _context.ConfiguracoesSistema.Update(config);
            }

            await _context.SaveChangesAsync();

            // Atualiza a URL base em memória (sem precisar reiniciar a API)
            _options.BaseUrl = novoDominio;
            _options.NovoDominio = novoDominio;

            return Ok(new { message = "Domínio atualizado com sucesso!", novoDominio });
        }


    }
}
