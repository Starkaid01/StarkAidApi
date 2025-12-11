using System.Net.WebSockets;
using System.Threading.Channels;

namespace StarkAid.Api.DTOs.V1.WPPconnect
{
    public class ClientSession
    {
        public string SessionId { get; }
        public WebSocket WebSocket { get; }
        public Guid UserId { get; }
        public int MaxBufferChunks { get; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        // NOVO
        public bool StopRequested { get; set; } = false;
        public Channel<byte[]> AudioChannel { get; set; } = Channel.CreateUnbounded<byte[]>();

        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        // 🔧 Propriedade nova para evitar START concorrente
        public bool IsRestarting { get; set; } = false;

        // ⬅️ NOVO
        public DateTime LastAudioTime { get; set; }

        public ClientSession(string sessionId, WebSocket ws, Guid userId, int maxBufferChunks, CancellationTokenSource cts)
        {
            SessionId = sessionId;
            WebSocket = ws;
            UserId = userId;
            MaxBufferChunks = maxBufferChunks;
            CancellationTokenSource = cts;
            LastAudioTime = DateTime.UtcNow; // inicializa
        }
    }
}
