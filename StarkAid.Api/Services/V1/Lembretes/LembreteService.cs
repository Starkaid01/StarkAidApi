using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.Hubs;

namespace StarkAid.Api.Services.V1.Lembretes
{
    public interface ILembreteService
    {
        Task<Lembrete> CriarLembreteAsync(Guid userId, string texto, DateTimeOffset dispararEm);
        Task<Lembrete> CriarLembreteSemDataAsync(Guid userId, string texto);
        Task<Lembrete> ProcessarTextoLembreteAsync(Guid userId, string texto);
        Task MarcarComoFaladoAsync(Guid lembreteId);
        Task MarcarComoDisparadoAsync(Guid lembreteId);
        Task ProcessarLembretesPendentesAsync();
        Task<Lembrete?> ObterPorIdAsync(Guid id);
        Task<List<Lembrete>> ObterDoUsuarioAsync(Guid userId);
        Task RemoverAsync(Guid id);
    }

    public class LembreteService : ILembreteService
    {
        private readonly AppDbContext _context;
        private readonly FcmNotificationService _fcmService;
        private readonly ILogger<LembreteService> _logger;
        private readonly IHubContext<DeviceHub> _hubContext;

        public LembreteService(AppDbContext context, FcmNotificationService fcmService, ILogger<LembreteService> logger, IHubContext<DeviceHub> hubContext)
        {
            _context = context;
            _fcmService = fcmService;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<Lembrete> ProcessarTextoLembreteAsync(Guid userId, string texto)
        {
            // Lógica simples de interpretação de tempo
            // Retorna um Lembrete criado (pode ser com data futura ou null se precisar de confirmação)
            // OBS: O requisito diz que o APP pergunta. Então se falta horário, talvez devêssemos retornar algo específico?
            // Mas aqui vamos tentar resolver o máximo.

            var dataDisparo = InterpretarData(texto);
            
            if (dataDisparo.HasValue)
            {
                return await CriarLembreteAsync(userId, texto, dataDisparo.Value);
            }
            
            // Se não conseguiu detectar data/hora, cria sem data (ou pode ser tratado como rascunho)
            // O requisito diz "Detectar ausência de horário -> O app pergunta".
            // Vamos assumir que aqui retornamos null ou um status especial se não achar data.
            // Para simplificar: Retornamos null para "Data não encontrada" e o Controller avisa o App.
            
            // Mas o metodo CriarLembreteSemDataAsync serve para "AguardandoResposta".
            // Vamos retornar null e deixar o Controller decidir ou o App lida com a resposta.
            // Porem, se o usuario diz "Me lembra amanha", detectamos "Amanhã" mas sem hora.
            
            // Melhor: Tenta achar a data. Se achar "Amanhã" sem hora, define data como null mas contexto "Amanhã".
            // Como simplificação, vamos assumir que se não tem hora, retorna null Date.
            
            return null;
        }

        private string NormalizarTextoParaSalvar(string texto, DateTimeOffset? dataDisparo)
        {
            var textoNorm = texto.Trim();
            
            // 1. Remove trigger phrases (insensitive) e remove optional "que" at the end
            // "me lembra que...", "lembrar de...", "agendar..."
            textoNorm = Regex.Replace(textoNorm, @"^\s*(me\s+lembr(a|e)(?:\s+de)?|lembr(a|e)(?:\s+de)?|crie\s+um\s+lembrete|agendar|lembrar|me\s+lembre)(?:\s+que)?\s*", "", RegexOptions.IgnoreCase);

            // Remove leading "que" if it stayed (e.g. "me lembra. que eu tenho...")
            textoNorm = Regex.Replace(textoNorm, @"^\s*que\s+", "", RegexOptions.IgnoreCase);

            // 2. Replacements (Sequence matters: longest first)
            
            // "que eu tenho que" / "eu tenho que" / "que tenho que" -> "você tem que"
            textoNorm = Regex.Replace(textoNorm, @"\b(que\s+)?(eu\s+)?tenho\s+que\b", "você tem que", RegexOptions.IgnoreCase);
            
            // "que eu preciso" / "eu preciso" -> "você precisa"
            textoNorm = Regex.Replace(textoNorm, @"\b(que\s+)?(eu\s+)?preciso\b", "você precisa", RegexOptions.IgnoreCase);

            // "que tenho" -> "você tem que" (generic fallback for 'tenho')
            textoNorm = Regex.Replace(textoNorm, @"\bque\s+tenho\b", "você tem que", RegexOptions.IgnoreCase);
            
            // Pronouns (careful to not replace inside words, \b is crucial)
            // \beu\b -> você
            textoNorm = Regex.Replace(textoNorm, @"\beu\b", "você", RegexOptions.IgnoreCase);
            // \bmeu\b -> seu
            textoNorm = Regex.Replace(textoNorm, @"\bmeu\b", "seu", RegexOptions.IgnoreCase);
            // \bminha\b -> sua
            textoNorm = Regex.Replace(textoNorm, @"\bminha\b", "sua", RegexOptions.IgnoreCase);

            // 3. Time-specific logic
            // "daqui a" split logic: "lembretetouser = lembretetouser[0] + " agora""
            var split = Regex.Split(textoNorm, @"\s+daqui(\s+a)?\s+", RegexOptions.IgnoreCase);
            if (split.Length > 1)
            {
                // Take the first part, ignore "daqui X time"
                textoNorm = split[0].Trim() + " agora";
            }

            // 4. "Somente para lembrete no dia: ' amanha ' = ' hoje '"
            textoNorm = Regex.Replace(textoNorm, @"\bamanhã\b", "hoje", RegexOptions.IgnoreCase);
            textoNorm = Regex.Replace(textoNorm, @"\bno dia\b", "hoje", RegexOptions.IgnoreCase); 

            return textoNorm.Trim();
        }

        private DateTimeOffset? InterpretarData(string texto)
        {
            var now = DateTimeOffset.UtcNow.ToLocalTime(); // Ideal seria pegar fuso do user, assumindo -3 para BR por enquanto ou UTC
            // Ajuste para fuso fixo -3 (BRT) já que o prompt é em PT-BR e estamos no Brasil
             var brTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

            texto = texto.ToLower();

            // 1. "Daqui X minutos/horas"
            var regexDaqui = new Regex(@"daqui\s+(?:a\s+)?(\d+)\s*(min|minuto|minutos|hora|horas)"); // Added optional 'a'
            var matchDaqui = regexDaqui.Match(texto);
            if (matchDaqui.Success)
            {
                int qtd = int.Parse(matchDaqui.Groups[1].Value);
                string unidade = matchDaqui.Groups[2].Value;

                if (unidade.StartsWith("min"))
                    return DateTimeOffset.UtcNow.AddMinutes(qtd);
                else if (unidade.StartsWith("hora"))
                    return DateTimeOffset.UtcNow.AddHours(qtd);
            }

            // 2. "Dia X de Mes Y"
            // Ex: "dia 22 de outubro"
            // Precisamos tratar ano. Assumimos ano atual ou próximo.
            // E hora? "as 14 horas"
            
            // Esta lógica é complexa para um regex simples. 
            // Vamos fazer um básico ("amanhã as X").
            
            if (texto.Contains("amanhã"))
            {
                var amanha = brTime.AddDays(1).Date;
                var hora = ExtrairHora(texto);
                if (hora.HasValue)
                    return new DateTimeOffset(amanha.Add(hora.Value), brTime.Offset).ToUniversalTime();
                 
                 // Se tem "amanhã" mas não tem hora, retornamos NULL para disparar a pergunta do App
                 return null; 
            }

            // 2. "Dia X de Mes Y" ou só "Dia X"
            // Ex: "dia 22 de outubro" ou "dia 22"
            var regexDia = new Regex(@"dia\s+(\d{1,2})(\s+de\s+([a-záéíóúç]+))?");
            var matchDia = regexDia.Match(texto);
            if (matchDia.Success)
            {
                 int dia = int.Parse(matchDia.Groups[1].Value);
                 int mes = brTime.Month;
                 int ano = brTime.Year;
                 
                 if (matchDia.Groups[3].Success)
                 {
                     mes = ObterMes(matchDia.Groups[3].Value);
                 }
                 
                 // Se mês/dia já passou este ano, assume próximo ano
                 // Mas se mês não foi especificado, assume próximo mês se dia < hoje?
                 // Simplificação: Se mês foi especificado e data < hoje, ano + 1.
                 // Se mês não foi especificado e dia < hoje, mes + 1.
                 
                 if (!matchDia.Groups[3].Success && dia < brTime.Day)
                 {
                     mes++;
                     if (mes > 12) { mes=1; ano++; }
                 }
                 
                 try 
                 {
                     var data = new DateTime(ano, mes, dia);
                     if (matchDia.Groups[3].Success && data < brTime.Date)
                     {
                         data = data.AddYears(1);
                     }
                     
                     var hora = ExtrairHora(texto);
                     if (hora.HasValue)
                     {
                         return new DateTimeOffset(data.Add(hora.Value), brTime.Offset).ToUniversalTime();
                     }
                     // Se disse o dia mas não a hora, definimos uma hora padrão? Ou pedimos a hora?
                     // Prompt: "Me lembra dia 22..." -> Pode pedir a hora.
                     // Vamos retornar null para pedir hora se não tiver hora explícita.
                     return null;
                 }
                 catch {}
            }
            
            return null;
        }

        private int ObterMes(string nome)
        {
            nome = nome.ToLower();
            if (nome.StartsWith("jan")) return 1;
            if (nome.StartsWith("fev")) return 2;
            if (nome.StartsWith("mar")) return 3;
            if (nome.StartsWith("abr")) return 4;
            if (nome.StartsWith("mai")) return 5;
            if (nome.StartsWith("jun")) return 6;
            if (nome.StartsWith("jul")) return 7;
            if (nome.StartsWith("ago")) return 8;
            if (nome.StartsWith("set")) return 9;
            if (nome.StartsWith("out")) return 10;
            if (nome.StartsWith("nov")) return 11;
            if (nome.StartsWith("dez")) return 12;
            return DateTime.Now.Month;
        }


        private TimeSpan? ExtrairHora(string texto)
        {
             var regexHora = new Regex(@"às\s+(\d{1,2})(:(\d{2}))?");
             var match = regexHora.Match(texto);
             if (match.Success)
             {
                 int h = int.Parse(match.Groups[1].Value);
                 int m = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                 return new TimeSpan(h, m, 0);
             }
             return null;
        }

        public async Task<Lembrete> CriarLembreteAsync(Guid userId, string texto, DateTimeOffset dispararEm)
        {
            var lembrete = new Lembrete
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Texto = NormalizarTextoParaSalvar(texto, dispararEm), // Assuming already normalized if called via Processar, or raw if called directly. Should I normalize here?
                // If I normalize here, I might double normalize or affect logic.
                // Let's assume the caller normalizes if needed. But Processar calls this with normalized.
                // If the Controller calls this directly (e.g. conversational flow), the text comes from the user response/full text.
                // In conversational flow, the text is "lembrar de X" + output of step 2.
                // It might need normalization.
                // Let's check if it needs normalization.
                DispararEm = dispararEm,
                Status = LembreteStatus.Pendente,
                DataCriacao = DateTimeOffset.UtcNow
            };

            // Safety: Normalize inside Criar if not doing it outside? 
            // ProcessarTextoLembreteAsync calls CriarLembreteAsync.
            // If I normalize in Processar, I pass normalized text.
            // If I normalize in Criar, I receive normalized text and normalize it again? Valid since idempotency.
            // But wait, "daqui a" logic adds "agora". If I do it twice?
            // "X agora" -> "daqui" not found -> ok.
            // "X amanhã" -> "X hoje" -> "X hoje". Ok.
            
            // However, better to make Processar use a separate Normalizer or just rely on Criar.
            // Processar needs original text so it parses time.
            // Then it passes Processed Text to Criar.
            // If I move normalization to Criar, I'd have to ensure Processar passes ORIGINAL to Criar?? No, Processar passes text.
            
            // Let's stick to Processar normalizing it.
            // And if Controller calls Criar directly, it should normalize too?
            // Yes. Let's make CriarLembreteAsync call Normalizar if it hasn't been done? Hard to detect.
            // I'll make a public Normalizar method or ensure usage.
            // Simpler: I will add `texto = NormalizarTextoParaSalvar(texto, dispararEm);` inside CriarLembreteAsync.
            // And remove the call in Processar? No, Processar already passes it.
            // Actually, if Processar calls Criar, and Criar cleans it, that's fine.
            // Does Processar pass original or cleaned?
            // In my proposed code above:
            // Processar extracts time from `texto`.
            // Then `textoNormalizado = Normalizar(texto)`.
            // Calls `Criar(..., textoNormalizado, ...)`
            // If `Criar` ALSO normalizes:
            // `Normalizar(textoNormalizado)` -> Clean.
            // This is safe.
            
            // So: Update CriarLembreteAsync to normalize.
            // Update Processar to NOT normalize (pass original), and let Criar do it.
            // Wait, Processar needs to remove "daqui X" from the text contextually?
            // Normalizar does that.
            // So if I pass raw text "me lembra daqui 5 min" to Criar:
            // Criar -> Normalizar -> "agora" (replacing daqui).
            // Saves "agora".
            // Processar parses time -> +5 min.
            // Passes raw "me lembra..." to Criar.
            // Criar saves "agora".
            // Result: Lembrete "agora" at +5 min. Perfect.

            _context.Lembretes.Add(lembrete);
            await _context.SaveChangesAsync();
 
            _logger.LogInformation("Lembrete criado para user {UserId} em {Data}", userId, dispararEm);
            return lembrete;
        }

        public async Task<Lembrete> CriarLembreteSemDataAsync(Guid userId, string texto)
        {
             // Criação temporária ou apenas retorna null para o App gerenciar estado?
             // Prompt diz: "Criar flag aguardandoRespostaHoraLembrete = true" no App.
             // Então talvez não persista nada ainda.
             return null;
        }

        public async Task MarcarComoFaladoAsync(Guid lembreteId)
        {
            var lembrete = await _context.Lembretes.FindAsync(lembreteId);
            if (lembrete != null)
            {
                lembrete.Falado = true;
                lembrete.Status = LembreteStatus.Concluido;
                await _context.SaveChangesAsync();
                 _logger.LogInformation("Lembrete {Id} marcado como falado.", lembreteId);
            }
        }
        
        public async Task MarcarComoDisparadoAsync(Guid lembreteId)
        {
            var lembrete = await _context.Lembretes.FindAsync(lembreteId);
            if (lembrete != null)
            {
                lembrete.Status = LembreteStatus.Disparado;
                lembrete.PushEnviado = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Lembrete {Id} marcado como disparado/push enviado.", lembreteId);
            }
        }

        public async Task ProcessarLembretesPendentesAsync()
        {
            var agora = DateTimeOffset.UtcNow;
            
            var pendentes = await _context.Lembretes
                .Where(l => l.Status == LembreteStatus.Pendente && l.DispararEm <= agora)
                .ToListAsync();

            foreach (var l in pendentes)
            {
                try 
                {
                    // Enviar SignalR (fala se estiver aberto)
                    await _hubContext.Clients.Group(l.UserId.ToString()).SendAsync("SpeakLembrete", l.Texto, l.Id);
                    
                    // Enviar Push
                    // Título genérico ou "Lembrete"
                    await _fcmService.EnviarParaUsuarioAsync(l.UserId, "Lembrete StarkAid", l.Texto, tipo: "lembrete", disparoId: l.Id);
                    
                    // Marcar como disparado (Push enviado)
                    // O Status muda para Disparado. Só muda para Concluido se for falado/lido.
                    await MarcarComoDisparadoAsync(l.Id);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar lembrete {Id}", l.Id);
                }
            }
        }

        public async Task<Lembrete?> ObterPorIdAsync(Guid id)
        {
            return await _context.Lembretes.FindAsync(id);
        }

        public async Task<List<Lembrete>> ObterDoUsuarioAsync(Guid userId)
        {
            return await _context.Lembretes
                .Where(l => l.UserId == userId && l.Status != LembreteStatus.Concluido)
                .OrderBy(l => l.DispararEm)
                .ToListAsync();
        }

        public async Task RemoverAsync(Guid id)
        {
             var l = await _context.Lembretes.FindAsync(id);
             if (l != null)
             {
                 _context.Lembretes.Remove(l);
                 await _context.SaveChangesAsync();
             }
        }
    }
}
