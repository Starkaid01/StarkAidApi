using Amazon.TranscribeStreaming;
using Amazon.TranscribeStreaming.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;
using StarkAid.Api.Services;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace StarkAid.Api.Services.V1
{
    public class ClientSession
    {
        public string SessionId { get; }
        public WebSocket WebSocket { get; }
        public Guid UserId { get; }
        public Channel<byte[]> AudioChannel { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public bool StopRequested { get; set; }
        public bool IsRestarting { get; set; }

        // NOVO: tempo do último áudio recebido
        public DateTime LastAudioTime { get; set; }

        public int MaxBufferChunks { get; }

        public ClientSession(string sessionId, WebSocket ws, Guid userId, int maxBufferChunks, CancellationTokenSource cts)
        {
            SessionId = sessionId;
            WebSocket = ws;
            UserId = userId;
            MaxBufferChunks = maxBufferChunks;
            CancellationTokenSource = cts;
            AudioChannel = Channel.CreateUnbounded<byte[]>();
            LastAudioTime = DateTime.UtcNow;
        }
    }

    public class TranscribeProxyService
    {
        private readonly AmazonTranscribeStreamingClient _client;
        private readonly IServiceProvider _provider;
        private readonly ILogger<TranscribeProxyService> _logger;
        private readonly ITokenUsageService _tokenUsage;

        private readonly ConcurrentDictionary<string, ClientSession> _clients = new();
        private readonly ConcurrentQueue<WebSocket> _waitingQueue = new();

        private readonly int _maxActiveClients = 18;
        private readonly int _maxBufferChunks = 50;

        private readonly object _startLock = new();

        public TranscribeProxyService(
            AmazonTranscribeStreamingClient client,
            IServiceProvider provider,
            ILogger<TranscribeProxyService> logger,
            ITokenUsageService tokenUsage)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenUsage = tokenUsage ?? throw new ArgumentNullException(nameof(tokenUsage));
        }

        public async Task StartTranscriptionAsync(WebSocket webSocket, string languageCode, Guid userId)
        {
            if (webSocket is null) throw new ArgumentNullException(nameof(webSocket));

            if (_clients.Count >= _maxActiveClients)
            {
                if (webSocket.State == WebSocketState.Open)
                    await SendStatusAsync(webSocket, "WAITING_IN_QUEUE", null, null, null, userId);

                _waitingQueue.Enqueue(webSocket);
                return;
            }

            await StartClientSession(webSocket, languageCode, userId);
        }

        private async Task StartClientSession(WebSocket webSocket, string languageCode, Guid userId)
        {
            var sessionId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            var session = new ClientSession(sessionId, webSocket, userId, _maxBufferChunks, cts);
            _clients[sessionId] = session;

            var monitorTask = MonitorInactivity(session, cts.Token);

            try
            {
                await RunTranscriptionLoop(session, languageCode, cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                cts.Cancel();
                _logger.LogInformation($"[Transcribe][{sessionId}] CTS cancelado.");
                try { if (monitorTask != null && !monitorTask.IsCompleted) await monitorTask; } catch { }
                await EndSessionAsync(session);
            }
        }

        private async Task RunTranscriptionLoop(ClientSession session, string languageCode, CancellationToken cancellationToken)
        {
            var buffer = new byte[8 * 1024];
            StartStreamTranscriptionResponse? response = null;
            DateTime? awsSessionStart = null;
            var awsLock = new object();
            var isStartingAws = false;

            using var billingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            while (session.WebSocket.State == WebSocketState.Open && !billingCts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await session.WebSocket.ReceiveAsync(buffer, billingCts.Token);
                }
                catch { break; }

                if (result.MessageType == WebSocketMessageType.Close) break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
                    if (msg.ToUpperInvariant() == "BYE" || msg.ToUpperInvariant() == "STOP")
                    {
                        billingCts.Cancel();
                        await EndSessionAsync(session, response);
                        return;
                    }
                    continue;
                }

                if (result.Count > 1)
                {
                    var chunk = new byte[result.Count];
                    Array.Copy(buffer, chunk, result.Count);

                    if (!IsSilent(chunk))
                    {
                        await session.AudioChannel.Writer.WriteAsync(chunk, billingCts.Token);
                        session.LastAudioTime = DateTime.UtcNow;

                        if (response == null && !isStartingAws)
                        {
                            lock (awsLock)
                            {
                                if (response == null && !isStartingAws)
                                {
                                    isStartingAws = true;
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            awsSessionStart = DateTime.UtcNow;
                                            response = await StartAwsTranscribeSession(session, languageCode, billingCts.Token);
                                        }
                                        finally
                                        {
                                            isStartingAws = false;
                                        }
                                    });
                                }
                            }
                        }
                    }
                }

                // Fecha AWS após 30s de silêncio
                if (response != null && (DateTime.UtcNow - session.LastAudioTime).TotalSeconds > 30)
                {
                    await CloseAwsSession(response, billingCts);
                    response = null;

                    if (awsSessionStart.HasValue)
                    {
                        var minutes = Math.Max(0, (DateTime.UtcNow - awsSessionStart.Value).TotalMinutes);
                        await DeductUserTokens(session.UserId, minutes);
                        awsSessionStart = null;
                    }
                }
            }

            if (response != null)
            {
                await CloseAwsSession(response, billingCts);
                if (awsSessionStart.HasValue)
                {
                    var minutes = Math.Max(0, (DateTime.UtcNow - awsSessionStart.Value).TotalMinutes);
                    await DeductUserTokens(session.UserId, minutes);
                }
            }
        }

        private async Task<StartStreamTranscriptionResponse> StartAwsTranscribeSession(ClientSession session, string languageCode, CancellationToken token)
        {
            var request = new StartStreamTranscriptionRequest
            {
                LanguageCode = languageCode,
                MediaEncoding = MediaEncoding.Pcm,
                MediaSampleRateHertz = 16000,
                AudioStreamPublisher = async () =>
                {
                    if (token.IsCancellationRequested) return null;
                    if (await session.AudioChannel.Reader.WaitToReadAsync(token))
                    {
                        var chunk = await session.AudioChannel.Reader.ReadAsync(token);
                        return new AudioEvent { AudioChunk = new MemoryStream(chunk, writable: false) };
                    }
                    return null;
                }
            };

            var response = await _client.StartStreamTranscriptionAsync(request, token);
            _ = Task.Run(async () =>
            {
                await foreach (var evt in response.TranscriptResultStream.WithCancellation(token))
                {
                    if (evt is TranscriptEvent transcriptEvent)
                    {
                        foreach (var result in transcriptEvent.Transcript.Results)
                        {
                            foreach (var alt in result.Alternatives)
                            {
                                var text = alt.Transcript;
                                if (!string.IsNullOrWhiteSpace(text) && session.WebSocket.State == WebSocketState.Open)
                                {
                                    var prefix = result.IsPartial.HasValue && result.IsPartial.Value ? "[PARCIAL]" : "[FINAL]";
                                    await SendStatusAsync(session.WebSocket, prefix, null, result.IsPartial, text, session.UserId, token);
                                }
                            }
                        }
                    }
                }
            }, token);

            return response;
        }

        private async Task CloseAwsSession(StartStreamTranscriptionResponse response, CancellationTokenSource cts)
        {
            // CS1998: Added await Task.CompletedTask to make the method properly asynchronous.
            // CA1822: Removed the unused parameter 'response' and marked the method as static.
            // IDE0060: Removed the unused parameter 'response'.

            try
            {
                cts.Cancel();
            }
            catch { }

            await Task.CompletedTask; // Ensures the method remains asynchronous.
        }

        private async Task DeductUserTokens(Guid userId, double minutesUsed)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            var tokens = (int)Math.Ceiling(minutesUsed * 20); // 0.2 coin/min = 20 tokens/min
            if (tokens <= 0) return;

            var consumo = await _tokenUsage.TryConsumeTokensAsync(user, tokens);
            if (!consumo.Success)
            {
                await SendAndClose(_clients.Values.FirstOrDefault(c => c.UserId == userId)?.WebSocket, "INSUFFICIENT_BALANCE", "Saldo insuficiente para transcrição.", userId);
            }
        }

        private async Task SendAndClose(WebSocket? ws, string message, string reason, Guid? userId = null)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                object? economy = null;
                if (userId.HasValue)
                {
                    economy = await BuildEconomyAsync(userId.Value);
                }

                var payload = new
                {
                    message,
                    reason,
                    economy
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Delay(50);
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None); } catch { }
            }
        }

        private async Task<EconomicPayload?> BuildEconomyAsync(Guid userId)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var plano = scope.ServiceProvider.GetRequiredService<PlanoLimitesService>();

            var user = await db.Users.FindAsync(userId);
            if (user == null) return null;

            var limite = plano.ObterLimiteTokensSemana(user);
            var agMax = plano.ObterLimiteAgendamentos(user);
            var agAtuais = await db.Agendamentos.CountAsync(a => a.UserId == userId);
            var agRest = agMax == -1 ? -1 : Math.Max(0, agMax - agAtuais);

            return new EconomicPayload(
                user.PlanType.ToString(),
                user.StarkCoinBalance,
                user.TokensConsumidosSemana,
                limite,
                Math.Max(0, limite - user.TokensConsumidosSemana),
                plano.ExibeAnuncios(user),
                agMax,
                agRest,
                100);
        }

        private async Task SendStatusAsync(WebSocket ws, string message, string? reason, bool? isPartial, string? transcript, Guid userId, CancellationToken token = default)
        {
            if (ws.State != WebSocketState.Open) return;

            var economy = await BuildEconomyAsync(userId);
            var payload = new
            {
                message,
                reason,
                isPartial,
                transcript,
                economy
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, token);
        }

        public async Task SendAuthOkAsync(WebSocket ws, Guid userId)
        {
            await SendStatusAsync(ws, "AUTH_OK", null, null, null, userId);
        }

        private async Task MonitorInactivity(ClientSession session, CancellationToken token)
        {
            var checkInterval = TimeSpan.FromSeconds(5);
            var inactivityTimeout = TimeSpan.FromSeconds(30); // ⬅️ Mais razoável

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var inactivity = DateTime.UtcNow - session.LastAudioTime;
                    if (inactivity >= inactivityTimeout) break;

                    try
                    {
                        if (session.WebSocket.State == WebSocketState.Open)
                        {
                            await SendStatusAsync(session.WebSocket, "PING", null, null, null, session.UserId, token);
                        }
                        else break;
                    }
                    catch { break; }

                    await Task.Delay(checkInterval, token);
                }
            }
            finally
            {
                await EndSessionAsync(session);
            }
        }

        private async Task EndSessionAsync(ClientSession session, StartStreamTranscriptionResponse? response = null)
        {
            try
            {
                _logger.LogInformation($"[Transcribe][{session.SessionId}] Finalizando sessão...");

                session.CancellationTokenSource?.Cancel();
                session.AudioChannel?.Writer.TryComplete();

                _clients.TryRemove(session.SessionId, out _);
                _logger.LogInformation($"[Transcribe][{session.SessionId}] Sessão finalizada. Sessões ativas: {_clients.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Transcribe][{session.SessionId}] Erro ao finalizar sessão");
            }

            // Added await Task.CompletedTask to ensure the method is properly asynchronous.
            await Task.CompletedTask;
        }

        private bool IsSilent(byte[] chunk)
        {
            // Aqui você pode implementar detecção de silêncio simples
            long sum = 0;
            foreach (var b in chunk) sum += Math.Abs(b);
            return sum < 500; // Threshold
        }
    }
}
