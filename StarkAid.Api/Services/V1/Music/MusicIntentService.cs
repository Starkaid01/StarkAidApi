using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Services.V1.Music
{
    public class MusicIntentService : IMusicIntentService
    {
        private readonly IYouTubeMusicService _youtubeService;
        private readonly IExternalAudioResolver _audioResolver;
        private readonly ILogger<MusicIntentService> _logger;

        public MusicIntentService(IYouTubeMusicService youtubeService, IExternalAudioResolver audioResolver, ILogger<MusicIntentService> logger)
        {
            _youtubeService = youtubeService;
            _audioResolver = audioResolver;
            _logger = logger;
        }

        public async Task<MusicResolveResponse> ResolveIntentAsync(string text)
        {
            text = text.ToLower().Trim();

            // 1. Controles básicos
            if (IsStopCommand(text)) return CreateControlResponse("stop", "Parando a música.");
            if (IsPauseCommand(text)) return CreateControlResponse("pause", "Pausando.");
            if (IsResumeCommand(text)) return CreateControlResponse("resume", "Continuando a reprodução.");
            if (IsVolumeUpCommand(text)) return CreateControlResponse("volume_up", "Aumentando o volume.");
            if (IsVolumeDownCommand(text)) return CreateControlResponse("volume_down", "Baixando o volume.");
            if (IsStatusCommand(text)) return CreateControlResponse("status", "Verificando o status.");

            // 2. Classificação e Resolução
            // SÓ pesquisamos se houver gatilho explícito
            if (text.StartsWith("toca ") || text.StartsWith("tocar ") || text.StartsWith("toque "))
            {
                return await HandleMusicIntentAsync(text);
            }

            return new MusicResolveResponse { Type = "none", Tts = "" };
        }

        private async Task<MusicResolveResponse> HandleMusicIntentAsync(string text)
        {
            // Limpeza
            var cleanText = Regex.Replace(text, @"^(toca|tocar|toque|coloque|ouvir|quero ouvir)\s*", "").Trim();
            
            if (string.IsNullOrEmpty(cleanText))
                return new MusicResolveResponse { Type = "none", Tts = "" };

            // Sempre usar YouTube (disfarçado de radio_two para o app)
            _logger.LogInformation("Pesquisando YouTube para '{Query}'.", cleanText);
            var searchResults = await _youtubeService.SearchMusicAsync(cleanText);

            if (searchResults != null && searchResults.Count > 0)
            {
                var first = searchResults[0];
                return new MusicResolveResponse
                {
                    Type = "radio_two",
                    Source = "online",
                    Tts = $"Tocando {first.Title}.",
                    YouTubeVideoId = first.VideoId,
                    Title = first.Title,
                    Confidence = 0.9
                };
            }

            return new MusicResolveResponse
            {
                Type = "error",
                Tts = "Desculpe, não consegui encontrar essa música no YouTube agora."
            };
        }

        private MusicResolveResponse CreateControlResponse(string type, string tts)
        {
            return new MusicResolveResponse { Type = type, Tts = tts };
        }

        private bool IsStopCommand(string t) => 
        t == "parar" || t == "para" || t == "pare" || t == "stop" || t == "desliga" ||
        t.Contains("para o som") 
        || t.Contains("para a música") 
        || t.Contains("para música") 
        || t.Contains("parar música") 
        
        || t.Contains("para a musica") 
        || t.Contains("para musica") 
        || t.Contains("parar musica") 
        || t.Contains("parar o som") 
        || t.Contains("para o som") 
        || t.Contains("pare o som") 

        || t.Contains("pare a musica") 
        || t.Contains("pare musica") 
        
        || t.Contains("desligar o som") 
        || t.Contains("desliga o som") 
        || t.Contains("desligue o som") 
        || t.Contains("chega de música") 
        || t.Contains("parar reprodução")
        || t.Contains("stop");
        private bool IsPauseCommand(string t) => 
        t.Contains("pausa") 
        || t.Contains("pausar")
        || t.Contains("pause");
        private bool IsResumeCommand(string t) => 
        t.Contains("continua") 
        || t.Contains("continuar") 
        || t.Contains("continue") 
        || t.Contains("retoma") 
        || t.Contains("resume") 
        || t.Contains("retomar");
        private bool IsVolumeUpCommand(string t) => 
        t.Contains("aumenta o volume") 
        || t.Contains("aumentar volume") 
        || t.Contains("aumenta mais") 
        || t.Contains("aumentar mais") 
        || t.Contains("aumente o volume") 

        || t.Contains("mais alto") 
        || t.Contains("sobe o volume");
        private bool IsVolumeDownCommand(string t) => 
        t.Contains("abaixa o volume") 
        || t.Contains("abaixar o volume") 
        || t.Contains("baixa o volume") 
        || t.Contains("baixar o volume")         
        || t.Contains("mais baixo") 
        || t.Contains("diminui o volume")
        || t.Contains("baixa mais")
        || t.Contains("abaixa mais")
        || t.Contains("baixar mais")
        || t.Contains("abaixar mais")
        || t.Contains("diminui mais")
        
        || t.Contains("abaixe o volume") 
        || t.Contains("baixe o volume") 

        || t.Contains("diminua o volume") 
        || t.Contains("diminuir o volume") 
        || t.Contains("diminue o volume")         
        || t.Contains("diminui mais")

        || t.Contains("abaixar volume") 
        || t.Contains("baixa volume") 
        || t.Contains("baixar volume")         
        || t.Contains("mais baixo") 
        || t.Contains("diminui volume")
        
        || t.Contains("abaixe volume") 
        || t.Contains("baixe volume") 

        || t.Contains("diminua volume") 
        || t.Contains("diminuir volume") 
        || t.Contains("diminue volume");
        
        
        private bool IsNextCommand(string t) => false;
        private bool IsStatusCommand(string t) => 
        t.Contains("que música") 
        || t.Contains("quem está cantando") 
        
        ||t.Contains("que musica") 
        ||t.Contains("qual musica") 
        ||t.Contains("quem esta cantando");
    }
}
