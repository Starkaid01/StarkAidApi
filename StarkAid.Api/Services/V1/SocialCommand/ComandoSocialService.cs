using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;
using StarkAid.Api.DTOs.V1.SocialCommand;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.SuperIA;
using StarkAid.Api.Services;
using System.Text.Json;

namespace StarkAid.Api.Services.V1.SocialCommand;

public class ComandoSocialService
{
    private readonly AppDbContext _context;
    private readonly IaService _iaService;
    private readonly ITokenUsageService _tokenUsage;
    private readonly PlanoLimitesService _planoLimites;

    public ComandoSocialService(AppDbContext context, IaService iaService, ITokenUsageService tokenUsage, PlanoLimitesService planoLimites)
    {
        _context = context;
        _iaService = iaService;
        _tokenUsage = tokenUsage;
        _planoLimites = planoLimites;
    }

    public async Task<List<ComandoSocial>> GetAllAsync()
    {
        return await _context.ComandosSociais.ToListAsync();
    }

    public async Task<ComandoSocial> AddAsync(string comando, string resposta)
    {
        var novo = new ComandoSocial
        {
            Id = Guid.NewGuid(),
            Comando = comando.ToLower(),
            Resposta = resposta
        };

        _context.ComandosSociais.Add(novo);
        await _context.SaveChangesAsync();

        return novo;
    }

    public async Task<List<ComandoSocial>> GetByUserIdAsync(Guid userId)
    {
        return await _context.ComandosSociais
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<ComandoSocial?> AddAsync(Guid userId, string comando, string resposta, string estilo)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        // 🔹 Se o usuário não tiver saldo de coins, salva sem variações
        if (user.StarkCoins <= 0)
        {
            var novo = new ComandoSocial
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Comando = comando,
                Resposta = resposta,
                RespostasAleatorias = ""
            };

            _context.ComandosSociais.Add(novo);
            await _context.SaveChangesAsync();
            return novo;
        }
        else
        {
            // 🔹 Gera variações com IA
            var resultado = await _iaService.ChamarStarkNlp(resposta);
            if (string.IsNullOrWhiteSpace(resultado.Texto))
                return null;

            // ChamarStarkNlp não retorna contagem de tokens; não debita StarkCoins adicionais aqui.

            // 🔹 Tenta validar o JSON
            string jsonValido;
            try
            {
                using var doc = JsonDocument.Parse(resultado.Texto);
                jsonValido = resultado.Texto;
            }
            catch
            {
                // fallback: converte texto plano em JSON válido
                var partes = resultado.Texto.Split("||", StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToArray();
                jsonValido = System.Text.Json.JsonSerializer.Serialize(new { alternativas = partes });
            }

            var novo = new ComandoSocial
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Comando = comando,
                Resposta = resposta,
                RespostasAleatorias = jsonValido
            };

            _context.ComandosSociais.Add(novo);
            await _context.SaveChangesAsync();

            return novo;
        }
    }

    public async Task<List<string>?> RespsrandomAnswers(Guid userId, string resposta)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        if (user.StarkCoins <= 0)
            return new List<string> { resposta };

        var mensagens = new[]
        {
            new { role = "system", content = "Você é uma IA que reescreve frases. Crie exatamente 4 variações diferentes que transmitam o mesmo significado da frase original. Não use frases formais, seja simples e direto. Responda SOMENTE em JSON no formato: { \"alternativas\": [\"...\",\"...\",\"...\",\"...\"] }" },
            new { role = "user", content = resposta }
        };

        var resultado = await _iaService.ProcessarMensagemJson(mensagens);

        if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            return null;

        var tokensUsados = Math.Max(0, resultado.PromptTokens) + Math.Max(0, resultado.CompletionTokens);
        var consumo = await _tokenUsage.TryConsumeTokensAsync(user, tokensUsados); // Comandos sociais não consomem tokens/StarkCoins
        if (!consumo.Success)
            throw new TokenInsufficientException(consumo.RequiredCoins);

        // Desserializa retorno JSON
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = System.Text.Json.JsonSerializer.Deserialize<AlternativasDto>(resultado.Texto, options);
            if (dto?.alternativas != null && dto.alternativas.Count > 0)
            {
                return dto.alternativas;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // fallthrough para fallback
        }

        return new List<string> { resposta };
    }

    public async Task<string> CriaeMessageWpp(Guid userId, string message, string estilo)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        var resultado = await _iaService.ProcessarMensagemWpp("", "", message, estilo);
        if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            return null;

        var mensagemLimpa = LimparRespostaIA(resultado.Texto);

        var tokensUsados = Math.Max(0, resultado.PromptTokens) + Math.Max(0, resultado.CompletionTokens);
        var consumo = await _tokenUsage.TryConsumeTokensAsync(user, tokensUsados); // Comandos sociais não consomem tokens/StarkCoins
        if (!consumo.Success)
            throw new TokenInsufficientException(consumo.RequiredCoins);

        return mensagemLimpa;
    }

    private string LimparRespostaIA(string resposta)
    {
        if (string.IsNullOrWhiteSpace(resposta))
            return resposta;

        var prefixos = new[]
        {
            "Você pode dizer:",
            "Diga:",
            "ok diga:",
            "Pode dizer:",
            "Sugiro:",
            "Mensagem:"
        };

        foreach (var prefixo in prefixos)
        {
            if (resposta.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            {
                resposta = resposta.Substring(prefixo.Length).Trim();
            }
        }

        if (resposta.StartsWith("\"") && resposta.EndsWith("\""))
        {
            resposta = resposta.Substring(1, resposta.Length - 2);
        }

        resposta = resposta.Replace("[Seu nome]", "")
            .Replace("[seu nome]", "")
            .Replace("[nome]", "")
            .Trim();

        return resposta;
    }

    public async Task<bool> EditAsync(Guid id, Guid userId, string comando, string resposta, string estilo)
    {
        var comandoSocial = await _context.ComandosSociais
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (comandoSocial == null) return false;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        if (user.StarkCoins > 0)
        {
            var resultado = await _iaService.ChamarStarkNlp(resposta);
            if (string.IsNullOrWhiteSpace(resultado.Texto))
                return false;

            // ChamarStarkNlp não retorna contagem de tokens; mantemos sem débito adicional aqui.

            string jsonValido;
            try
            {
                using var doc = JsonDocument.Parse(resultado.Texto);
                jsonValido = resultado.Texto;
            }
            catch
            {
                var partes = resultado.Texto.Split("||", StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToArray();
                jsonValido = System.Text.Json.JsonSerializer.Serialize(new { alternativas = partes });
            }

            comandoSocial.Comando = comando;
            comandoSocial.Resposta = resposta;
            comandoSocial.RespostasAleatorias = jsonValido;
        }

        if (user.StarkCoins <= 0)
        {
            comandoSocial.Comando = comando;
            comandoSocial.Resposta = resposta;
            comandoSocial.RespostasAleatorias = "";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var comandoSocial = await _context.ComandosSociais
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (comandoSocial == null) return false;

        _context.ComandosSociais.Remove(comandoSocial);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EconomicPayload?> ObterEconomiaAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var limite = _planoLimites.ObterLimiteTokensSemana(user);
        var agMax = _planoLimites.ObterLimiteAgendamentos(user);
        var agAtuais = await _context.Agendamentos.CountAsync(a => a.UserId == userId);
        var agRest = agMax == -1 ? -1 : Math.Max(0, agMax - agAtuais);

        return new EconomicPayload(
            user.PlanType.ToString(),
            user.StarkCoins,
            user.TokensConsumidosSemana,
            limite,
            Math.Max(0, limite - user.TokensConsumidosSemana),
            _planoLimites.ExibeAnuncios(user),
            agMax,
            agRest,
            100);
    }

    public async Task<string?> ProcessSocialAsync(Guid userId, string text)
    {
        var normalizedText = text.ToLower().Trim();
        
        // Busca direta
        var social = await _context.ComandosSociais
            .Where(c => c.UserId == userId && c.Comando.ToLower() == normalizedText)
            .FirstOrDefaultAsync();

        if (social == null)
        {
             // Busca por "contém" ou similar simples se não achou exato
             social = await _context.ComandosSociais
                .Where(c => c.UserId == userId && normalizedText.Contains(c.Comando.ToLower()))
                .OrderByDescending(c => c.Comando.Length) // Pega o mais longo/específico
                .FirstOrDefaultAsync();
        }

        if (social != null)
        {
             // Se houver respostas aleatórias (JSON), escolhe uma
             if (!string.IsNullOrEmpty(social.RespostasAleatorias))
             {
                 try {
                     var doc = JsonDocument.Parse(social.RespostasAleatorias);
                     if (doc.RootElement.TryGetProperty("alternativas", out var alts) && alts.GetArrayLength() > 0)
                     {
                         var index = new Random().Next(alts.GetArrayLength());
                         return alts[index].GetString();
                     }
                 } catch {}
             }
             return social.Resposta;
        }

        return null;
    }
}
