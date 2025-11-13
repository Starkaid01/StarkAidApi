using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.SocialCommand;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.SuperIA;
using System.Text.Json;

namespace StarkAid.Api.Services.SocialCommand;

public class ComandoSocialService
{
    private readonly AppDbContext _context;
    private readonly IaService _iaService;
    public ComandoSocialService(AppDbContext context, IaService iaService)
    {
        _context = context;
        _iaService = iaService;
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
            Comando = comando.ToLower(),  // pra evitar case sensitive
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

        // 🔹 Se o usuário não tiver saldo, salva sem variações
        if (user.StarkCoins < 0.04m)
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
            // 🔹 Prompt para gerar variações no formato JSON esperado
            var mensagens = new[]
            {
                new { role = "system", content = "Você é uma IA que reescreve frases. Crie exatamente 4 variações diferentes e curtas que tenham o mesmo sentido da frase original. Nao use formalidades. Responda SOMENTE em JSON no formato: { \"alternativas\": [\"...\",\"...\",\"...\",\"...\"] }" },
                new { role = "user", content = resposta }
            };

            // 🔹 Chama a IA diretamente com esse prompt
            var resultado = await _iaService.ChamarStarkNlp(resposta);
            if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.Texto))
                return null;

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

            // 🔹 Calcula custo e debita saldo
            var custoSC = 0.04m;

            if (user.StarkCoins < custoSC)
                throw new InvalidOperationException("Saldo insuficiente para adicionar comando.");

            user.StarkCoins -= custoSC;

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


    private async Task<string?> GerarVariaçõesAsync(string resposta)
    {
        var mensagens = new[]
        {
            new { role = "system", content = "Você é uma IA que reescreve frases. Crie exatamente 4 variações diferentes e curtas que tenham o mesmo sentido da frase original. Nao use formalidades. Responda SOMENTE em JSON no formato: { \"alternativas\": [\"...\",\"...\",\"...\",\"...\"] }" },
            new { role = "user", content = resposta }
        };

        var resultado = await _iaService.ChamarOpenRouter(mensagens);
        if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(resultado.Texto);
            return resultado.Texto;
        }
        catch
        {
            var partes = resultado.Texto.Split("||", StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            return System.Text.Json.JsonSerializer.Serialize(new { alternativas = partes });
        }
    }


    public async Task<List<string>?> RespsrandomAnswers(Guid userId, string resposta)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        if (user.StarkCoins < 0.1m)
            return new List<string> { resposta };

        var mensagens = new[]
        {
            new { role = "system", content = "Você é uma IA que reescreve frases. Crie exatamente 4 variações diferentes que transmitam o mesmo significado da frase original. Não use frases formais, seja simples e direto. Responda SOMENTE em JSON no formato: { \"alternativas\": [\"...\",\"...\",\"...\",\"...\"] }" },
            new { role = "user", content = resposta }
        };

        // Usa o novo método do IaService
        var resultado = await _iaService.ProcessarMensagemJson(mensagens);

        if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            return null;

        // Cálculo de custo
        var custoUsd = _iaService.CalcularCustoUSD(resultado);
        var custoSC = custoUsd / 0.03m;
        if (user.StarkCoins < custoSC)
            throw new InvalidOperationException("Saldo insuficiente para gerar variações.");

        // Debita saldo e salva
        user.StarkCoins -= custoSC;
        await _context.SaveChangesAsync();

        // Desserializa retorno JSON usando System.Text.Json (case-insensitive)
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = System.Text.Json.JsonSerializer.Deserialize<AlternativasDto>(resultado.Texto, options);
            if (dto?.alternativas != null && dto.alternativas.Count > 0)
            {
                // Se IA retornou mais ou menos que 4, você pode normalizar aqui:
                // por enquanto, retorna o que veio
                return dto.alternativas;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // fallthrough para fallback
        }

        // fallback: retorna a resposta original (ou você pode tentar extrair linhas do texto)
        return new List<string> { resposta };
    }

    public async Task<string> CriaeMessageWpp(Guid userId, string message, string estilo)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        // Agora basta enviar diretamente o texto:
        var resultado = await _iaService.ProcessarMensagemWpp("", "", message, estilo);
        if (resultado == null || string.IsNullOrWhiteSpace(resultado.Texto))
            return null;

        // Limpeza pós-processamento
        var mensagemLimpa = LimparRespostaIA(resultado.Texto);

        var custoUsd = _iaService.CalcularCustoUSD(resultado);
        var custoSC = custoUsd / 0.02m;

        if (user.StarkCoins < custoSC)
            throw new InvalidOperationException("Saldo insuficiente para adicionar comando.");

        user.StarkCoins -= custoSC;
        await _context.SaveChangesAsync();

        return mensagemLimpa;
    }

    // 🔹 Método para limpar a resposta da IA
    private string LimparRespostaIA(string resposta)
    {
        if (string.IsNullOrWhiteSpace(resposta))
            return resposta;

        // Remove prefixos comuns
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

        // Remove aspas se a mensagem estiver entre aspas
        if (resposta.StartsWith("\"") && resposta.EndsWith("\""))
        {
            resposta = resposta.Substring(1, resposta.Length - 2);
        }

        // Remove placeholders
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

        if (user.StarkCoins > 0.04m)
        {
            // 🔹 Gera novas variações (usando a mesma lógica do Add)

            // 🔹 Chama a IA diretamente com esse prompt
            var resultado = await _iaService.ChamarStarkNlp(resposta);
            if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.Texto))
                return false; // Alterado de 'null' para 'false' para corrigir CS0037

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

            // 🔹 Calcula custo e debita saldo
            var custoSC = 0.04m;

            if (user.StarkCoins < custoSC)
                throw new InvalidOperationException("Saldo insuficiente para adicionar comando.");

            user.StarkCoins -= custoSC;

            // 🔹 Atualiza comando mantendo estrutura padronizada
            comandoSocial.Comando = comando;
            comandoSocial.Resposta = resposta;
            comandoSocial.RespostasAleatorias = jsonValido;
        }

        if (user.StarkCoins < 0.04m)
        {
            // 🔹 Atualiza comando mantendo estrutura padronizada
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
}
