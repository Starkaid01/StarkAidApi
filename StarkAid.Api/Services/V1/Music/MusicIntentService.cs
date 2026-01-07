using StarkAid.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using StarkAid.Api.DTOs.V1.Music;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Music
{
    public class MusicIntentService : IMusicIntentService
    {
        private readonly IYouTubeMusicService _youtubeService;
        private readonly IExternalAudioResolver _audioResolver;
        private readonly ILogger<MusicIntentService> _logger;
        private readonly AppDbContext _context;

        public MusicIntentService(IYouTubeMusicService youtubeService, IExternalAudioResolver audioResolver, ILogger<MusicIntentService> logger, AppDbContext context)
        {
            _youtubeService = youtubeService;
            _audioResolver = audioResolver;
            _logger = logger;
            _context = context;
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

            // Classificação: Artista vs Música
            var kind = ClassifyIntent(cleanText);
            _logger.LogInformation("Intenção Musical Classificada: {Kind} para '{Query}'", kind, cleanText);

            // Sempre usar YouTube (disfarçado de radio_two para o app)
            _logger.LogInformation("Pesquisando YouTube para '{Query}'.", cleanText);

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
                Tts = "Desculpe, não consegui encontrar essa música no YouTube agora."
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
            // Seed inicial - Artistas Brasileiros
            if (IsBias(normalized, "charlie brown jr", "charlie brown", "cbjr", "charlie brown junior", "charlie jr")) return "charlie brown jr";
            if (IsBias(normalized, "legião urbana", "legiao urbana", "legiao", "legião")) return "legião urbana";
            if (IsBias(normalized, "engenheiros do hawaii", "engenheiros", "engenheiros do havai", "engenheiros do hawai")) return "engenheiros do hawaii";
            if (IsBias(normalized, "skank", "skank banda")) return "skank";
            if (IsBias(normalized, "capital inicial", "capital")) return "capital inicial";
            if (IsBias(normalized, "os paralamas do sucesso", "paralamas", "paralamas do sucesso", "os paralamas")) return "os paralamas do sucesso";
            if (IsBias(normalized, "jorge & mateus", "jorge e mateus", "jorge mateus", "j&m")) return "jorge & mateus";
            if (IsBias(normalized, "zezé di camargo & luciano", "zeze di camargo e luciano", "zeze di camargo & luciano", "zeze e luciano", "zezé e luciano", "zeze de camargo e luciano")) return "zezé di camargo & luciano";
            if (IsBias(normalized, "henrique & juliano", "henrique e juliano", "henrique juliano")) return "henrique & juliano";

            // Seed inicial - Artistas Internacionais
            if (IsBias(normalized, "queen", "queen banda", "freddie mercury")) return "queen";
            if (IsBias(normalized, "the beatles", "beatles")) return "the beatles";
            if (IsBias(normalized, "linkin park", "linkin", "link park")) return "linkin park";
            if (IsBias(normalized, "metallica", "metalica")) return "metallica";
            if (IsBias(normalized, "nirvana", "nirvana banda")) return "nirvana";
            if (IsBias(normalized, "michael jackson", "mj", "michael", "rei do pop")) return "michael jackson";
            if (IsBias(normalized, "eminem", "slim shady")) return "eminem";
            if (IsBias(normalized, "rihanna", "rihanna cantora")) return "rihanna";
            if (IsBias(normalized, "taylor swift", "taylor")) return "taylor swift";

            return text;
        }

        private MusicKind ClassifyIntent(string text)
        {
            var lower = text.ToLowerInvariant();
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 🧠 Regra de Ouro (Hierarquia de Decisão)

            // 1. Palavras-chave de música (indicam versão específica ou formato)
            if (Regex.IsMatch(text, @"\b(remix|live|ao vivo|acustico|acústico|unplugged|cover|versao|versão|karaoke|letra|lyric|oficial|video|clipe|audio)\b", RegexOptions.IgnoreCase))
                return MusicKind.Song;

            // 2. Conectores explícitos (indicam relação Música DE Artista)
            if (Regex.IsMatch(text, @"\s(de|do|da|by|from|feat|ft\.|with)\s", RegexOptions.IgnoreCase) || text.Contains(" - "))
                return MusicKind.Song;

            // 3. Whitelist de Artistas (A única exceção à regra padrão)
            // Só classificamos como ARTISTA se for um match claro em query curta (<= 3 palavras).
            // Isso evita que "céu azul charlie brown jr" vire artista.
            var knownArtists = new[] { 
                "charlie brown jr", "charlie brown", "legiao urbana", "legião urbana", "engenheiros do hawaii",
                "queen", "the beatles", "beatles", "djavan", "adele", "madonna", "coldplay", 
                "u2", "metallica", "nirvana", "iron maiden", "pink floyd", "guns n roses", "ac/dc",
                "linkin park", "red hot chili peppers", "foo fighters"
            };

            if (tokens.Length <= 3 && knownArtists.Any(a => lower == a || (a.Contains(" ") && lower == a))) 
            {
               return MusicKind.Artist;
            }
            
            // Refinamento: Se contém parte de um nome composto muito forte, mas a query é curta
            if (tokens.Length <= 3 && (lower.Contains("charlie brown") || lower.Contains("legiao urbana")))
                return MusicKind.Artist;

            // 4. Padrão Absoluto: Música (Song)
            // "Céu Azul", "Evidências", "Anna Júlia", "Tempo Perdido" caem aqui.
            return MusicKind.Song;
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
