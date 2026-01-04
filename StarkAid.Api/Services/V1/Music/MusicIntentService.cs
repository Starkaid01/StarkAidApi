using System.Text.RegularExpressions;
using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Services.V1.Music
{
    public class MusicIntentService : IMusicIntentService
    {
        private readonly IRadioBrowserService _radioService;
        private readonly IYouTubeMusicService _youtubeService;
        private readonly ILogger<MusicIntentService> _logger;

        public MusicIntentService(IRadioBrowserService radioService, IYouTubeMusicService youtubeService, ILogger<MusicIntentService> logger)
        {
            _radioService = radioService;
            _youtubeService = youtubeService;
            _logger = logger;
        }

        public async Task<MusicResolveResponse> ResolveIntentAsync(string text)
        {
            text = text.ToLower().Trim();

            // 1. Controles básicos
            if (IsStopCommand(text)) return CreateControlResponse("stop", "Parando a música.");
            if (IsPauseCommand(text)) return CreateControlResponse("pause", "Pausando.");
            if (IsResumeCommand(text)) return CreateControlResponse("resume", "Continuando a reprodução.");
            if (IsNextCommand(text)) return new MusicResolveResponse { Type = "next", Tts = "Mudando de estação." };
            if (IsVolumeUpCommand(text)) return CreateControlResponse("volume_up", "Aumentando o volume.");
            if (IsVolumeDownCommand(text)) return CreateControlResponse("volume_down", "Baixando o volume.");
            if (IsStatusCommand(text)) return CreateControlResponse("status", "Verificando o status.");

            // 2. Classificação e Resolução
            return await HandleMusicIntentAsync(text);
        }

        private async Task<MusicResolveResponse> HandleMusicIntentAsync(string text)
        {
            // Limpeza
            var cleanText = Regex.Replace(text, @"^(toca|tocar|coloque|ouvir|quero ouvir)\s*", "").Trim();
            
            if (string.IsNullOrEmpty(cleanText))
                cleanText = "sucessos";

            // Detecção de Tipo (Música específica vs Artista/Gênero)
            bool isSpecificSong = cleanText.Contains(" a música ") || cleanText.Count(f => f == ' ') >= 3;

            // PASSO 1: Tentar Rádio (sempre primeiro)
            var station = await _radioService.ResolveBestRadioAsync(cleanText);
            
            if (station != null)
            {
                _logger.LogInformation("Resolvido via Rádio: {Name}", station.Name);
                return new MusicResolveResponse
                {
                    Type = "radio",
                    Source = "radio",
                    Tts = $"Tocando a rádio {station.Name}.",
                    Station = station,
                    Confidence = 0.9
                };
            }

            // PASSO 2: Fallback YouTube (Somente se rádio falhar)
            _logger.LogInformation("Rádio falhou para '{Query}'. Tentando YouTube Fallback.", cleanText);
            var (videoId, title) = await _youtubeService.SearchMusicAsync(cleanText);

            if (videoId != null)
            {
                return new MusicResolveResponse
                {
                    Type = "youtube",
                    Source = "youtube",
                    Tts = $"Tocando {title} do YouTube.",
                    YouTubeVideoId = videoId,
                    Title = title,
                    Confidence = 0.8
                };
            }

            return new MusicResolveResponse
            {
                Type = "error",
                Tts = "Desculpe, não consegui encontrar essa música em rádios ou no YouTube agora."
            };
        }

        private MusicResolveResponse CreateControlResponse(string type, string tts)
        {
            return new MusicResolveResponse { Type = type, Tts = tts };
        }

        private bool IsStopCommand(string t) => t.Contains("para o som") || t.Contains("para a música") || t.Contains("desliga o som") || t.Contains("stop");
        private bool IsPauseCommand(string t) => t.Contains("pausa") || t.Contains("pausar");
        private bool IsResumeCommand(string t) => t.Contains("continua") || t.Contains("resume") || t.Contains("retoma");
        private bool IsVolumeUpCommand(string t) => t.Contains("aumenta o volume") || t.Contains("mais alto");
        private bool IsVolumeDownCommand(string t) => t.Contains("abaixa o volume") || t.Contains("mais baixo");
        private bool IsNextCommand(string t) => t.Contains("próxima") || t.Contains("pula") || t.Contains("troca");
        private bool IsStatusCommand(string t) => t.Contains("que música") || t.Contains("quem está cantando") || t.Contains("qual rádio");
    }
}
