using StarkAid.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using StarkAid.Api.DTOs.V1.Music;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.SuperIA;

namespace StarkAid.Api.Services.V1.Music
{
    public class MusicIntentService : IMusicIntentService
    {
        private readonly IYouTubeMusicService _youtubeService;
        private readonly IExternalAudioResolver _audioResolver;
        private readonly ILogger<MusicIntentService> _logger;
        private readonly AppDbContext _context;
        private readonly IaService _iaService;

        public MusicIntentService(IYouTubeMusicService youtubeService, IExternalAudioResolver audioResolver, ILogger<MusicIntentService> logger, AppDbContext context, IaService iaService)
        {
            _youtubeService = youtubeService;
            _audioResolver = audioResolver;
            _logger = logger;
            _context = context;
            _iaService = iaService;
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
            var searchTriggers = new[] { "toca ", "tocar ", "toque ", "musica ", "ouvir ", "coloque ", "solta ", "reproduz ", "reproduzir " };
            if (searchTriggers.Any(trigger => text.StartsWith(trigger)))
            {
                return await HandleMusicIntentAsync(text);
            }

            return new MusicResolveResponse { Type = "none", Tts = "" };
        }

        private async Task<MusicResolveResponse> HandleMusicIntentAsync(string text)
        {
            // Limpeza
            var cleanText = Regex.Replace(text, @"^(toca|tocar|toque|coloque|ouvir|quero ouvir|musica|musica de|solta|reproduz|reproduzir|colocar)\s*", "").Trim();
            
            if (string.IsNullOrEmpty(cleanText))
                return new MusicResolveResponse { Type = "none", Tts = "" };

            // Normalização para comparar com banco (simples)
            var normalizedCheck = MusicQueryNormalizer.Normalize(cleanText);

            MusicKind kind = MusicKind.Song; // Default

            // 1. Verificar se JÁ EXISTE no banco (Cache Exato)
            // Se já temos esse termo salvo, usamos o mesmo Kind que foi salvo antes.
            var existing = await _context.YouTubeMusicCaches
                .Where(x => x.NormalizedQuery == normalizedCheck)
                .OrderByDescending(x => x.LastUsedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                kind = existing.Kind;
                _logger.LogInformation("Cache Hit Local: '{Query}' já existe como {Kind}. Pulando IA.", cleanText, kind);
            }
            else
            {
                // 2. Não existe no banco -> Perguntar para a IA
                _logger.LogInformation("Cache Miss: '{Query}' não encontrado. Solicitando classificação à IA...", cleanText);
                
                var iaClassification = await _iaService.ClassifyMusicIntent(cleanText);
                
                if (iaClassification == "Eartista")
                {
                    kind = MusicKind.Artist;
                }
                else
                {
                    kind = MusicKind.Song;
                }
                
                _logger.LogInformation("IA Classificou como: {Result} -> {Kind}", iaClassification, kind);
            }

            _logger.LogInformation("Processando busca musical: Termo='{Query}', Tipo={Kind}", cleanText, kind);

            // 🎵 RESOLUÇÃO DE ARTISTA CANÔNICO 🎵
            // Se for Artista, tentamos mapear para o nome "oficial" (Canônico) para evitar duplicação de pools.
            if (kind == MusicKind.Artist)
            {
                var canonical = await ResolveCanonicalArtistAsync(cleanText);
                if (canonical != cleanText)
                {
                    _logger.LogInformation("Artista Canônico Detectado: '{Original}' -> '{Canonical}'", cleanText, canonical);
                    cleanText = canonical;
                }
            }

            var searchResults = await _youtubeService.SearchMusicAsync(cleanText, kind);

            if (searchResults != null && searchResults.Count > 0)
            {
                var first = searchResults[0];
                if (kind == MusicKind.Artist && searchResults.Count > 1)
                {
                    // Se for artista novo (acabou de buscar), escolhemos um aleatório dos 10 iniciais
                    // para não tocar SEMPRE a mesma primeira música na primeira vez.
                    var random = new Random();
                    first = searchResults[random.Next(Math.Min(searchResults.Count, 5))];
                }

                return new MusicResolveResponse
                {
                    Type = "radio_two",
                    Source = "online",
                    Tts = kind == MusicKind.Artist ? $"Tocando uma de {cleanText}." : $"Tocando {first.Title}.",
                    YouTubeVideoId = first.VideoId,
                    Title = first.Title,
                    Confidence = 0.9
                };
            }

            return new MusicResolveResponse
            {
                Type = "error",
                Tts = ""
            };
        }

        private async Task<string> ResolveCanonicalArtistAsync(string text)
        {
            var normalized = text.ToLowerInvariant().Trim();

            // 1. Verificar Alias customizados no banco de dados (Prioridade Alta)
            var dbAlias = await _context.MusicArtistAliases
                .FirstOrDefaultAsync(a => a.Alias == normalized);
            
            if (dbAlias != null)
            {
                return dbAlias.Canonical;
            }

            // 2. Seed Hardcoded (Fallback/Legado)
            return text;
        }

        // Método ClassifyIntent antigo removido em favor da IA


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

        private bool IsBias(string input, string canonical, params string[] aliases)
        {
            // Verifica se o input bate com o canonical (normalizado) ou qualquer alias
            if (input == canonical.ToLowerInvariant()) return true;
            foreach (var alias in aliases)
            {
                if (input == alias) return true;
            }
            return false;
        }
    }
}
