using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using System.Text.Json;

namespace StarkAid.Api.Services.Suporte;

public class SupportIaService : ISupportIaService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SupportIaService> _logger;
    private readonly HttpClient _httpClient;

    public SupportIaService(AppDbContext context, ILogger<SupportIaService> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<string> GerarSaudacaoInicial(Guid userId, string nome, string email, string origem, object logs)
    {
        var logsList = logs as System.Collections.IEnumerable;
        var temLogs = logsList != null && logsList.Cast<object>().Any();

        var saudacao = $"Olá {nome}! 👋\n\n";
        saudacao += "Sou o assistente virtual de suporte da StarkAid. Como posso ajudá-lo hoje?\n\n";

        if (temLogs)
        {
            saudacao += "Detectei alguns logs de erro recentes em sua conta. Posso ajudá-lo a resolver esses problemas.\n";
        }
        else
        {
            saudacao += "Se você possui algum código de erro, pode me informar e eu tentarei ajudá-lo.\n";
        }

        return saudacao;
    }

    public async Task<string> ProcessarMensagem(Guid userId, string mensagem, string origem)
    {
        mensagem = mensagem.Trim().ToLower();

        // Verificar se menciona código de erro
        var codigoErroMatch = System.Text.RegularExpressions.Regex.Match(mensagem, @"err[_\s]?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (codigoErroMatch.Success)
        {
            var codigo = codigoErroMatch.Groups[1].Value;
            var codigoCompleto = $"ERR_{codigo.PadLeft(3, '0')}";
            return await ProcessarCodigoErro(userId, codigoCompleto, origem);
        }

        // Verificar se está pedindo ajuda com erro
        if (mensagem.Contains("erro") || mensagem.Contains("problema") || mensagem.Contains("não funciona"))
        {
            return await ProcessarSolicitacaoErro(userId, mensagem, origem);
        }

        // Verificar se está pedindo para limpar cache/dados
        if (mensagem.Contains("limpar") || mensagem.Contains("cache") || mensagem.Contains("dados"))
        {
            return await ProcessarLimpeza(userId, mensagem, origem);
        }

        // Resposta genérica
        return "Entendi sua solicitação. Vou analisar e tentar ajudá-lo. " +
               "Se você tiver um código de erro específico (ex: ERR_001), pode me informar para uma solução mais direta.";
    }

    private async Task<string> ProcessarCodigoErro(Guid userId, string codigo, string origem)
    {
        var errorCode = await _context.ErrorCodeDescriptions
            .FirstOrDefaultAsync(e => e.CodigoDeErro == codigo && 
                                     (e.Origem == origem || string.IsNullOrEmpty(e.Origem)));

        if (errorCode == null)
        {
            return $"Não encontrei informações sobre o código de erro {codigo}. " +
                   "Por favor, descreva o problema que está enfrentando para que eu possa ajudá-lo melhor.";
        }

        var solucoes = new List<string>();
        if (!string.IsNullOrEmpty(errorCode.Solucoes))
        {
            try
            {
                solucoes = JsonSerializer.Deserialize<List<string>>(errorCode.Solucoes) ?? new List<string>();
            }
            catch
            {
                solucoes = new List<string> { errorCode.Solucoes };
            }
        }

        // Filtrar soluções inviáveis para usuário final
        solucoes = await FiltrarSolucoes(solucoes, origem);

        if (solucoes.Count == 0)
        {
            return $"Encontrei o código de erro {codigo}, mas não há soluções automáticas disponíveis. " +
                   "Vou transferir você para o suporte humano.";
        }

        var resposta = $"Código de Erro: {codigo}\n";
        resposta += $"Descrição: {errorCode.Descricao}\n\n";
        resposta += "Soluções sugeridas:\n";
        for (int i = 0; i < solucoes.Count; i++)
        {
            resposta += $"{i + 1}. {solucoes[i]}\n";
        }

        resposta += "\nVou tentar aplicar algumas soluções automáticas agora...";

        // Tentar aplicar soluções automáticas
        await TentarAplicarSolucoes(userId, solucoes, origem);

        return resposta;
    }

    private async Task<string> ProcessarSolicitacaoErro(Guid userId, string mensagem, string origem)
    {
        // Buscar últimos erros do usuário
        if (origem == "software")
        {
            var ultimosErros = await _context.ErrorLogsSoft
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            if (ultimosErros.Any())
            {
                var ultimoErro = ultimosErros.First();
                if (!string.IsNullOrEmpty(ultimoErro.CodigoDeErro))
                {
                    return await ProcessarCodigoErro(userId, ultimoErro.CodigoDeErro, origem);
                }
            }
        }
        else
        {
            var ultimosErros = await _context.ErrorLogsApp
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            if (ultimosErros.Any())
            {
                var ultimoErro = ultimosErros.First();
                if (!string.IsNullOrEmpty(ultimoErro.CodigoDeErro))
                {
                    return await ProcessarCodigoErro(userId, ultimoErro.CodigoDeErro, origem);
                }
            }
        }

        return "Você possui algum código de erro específico? Se sim, informe-o (ex: ERR_001) para que eu possa ajudá-lo melhor. " +
               "Caso contrário, descreva o problema em detalhes.";
    }

    private async Task<string> ProcessarLimpeza(Guid userId, string mensagem, string origem)
    {
        if (mensagem.Contains("cache"))
        {
            // Aqui você chamaria o endpoint de limpar cache
            return "Vou limpar o cache agora. Isso pode ajudar a resolver alguns problemas. " +
                   "Por favor, aguarde alguns instantes e tente novamente.";
        }

        if (mensagem.Contains("dados"))
        {
            return "Limpar dados remove informações temporárias e logs. " +
                   "Isso não afetará suas configurações principais. Deseja continuar?";
        }

        return "Posso ajudar a limpar cache ou dados. O que você gostaria de limpar?";
    }

    private async Task TentarAplicarSolucoes(Guid userId, List<string> solucoes, string origem)
    {
        foreach (var solucao in solucoes)
        {
            var solucaoLower = solucao.ToLower();

            if (solucaoLower.Contains("limpar cache") || solucaoLower.Contains("limpar o cache"))
            {
                // Chamar endpoint de limpar cache
                _logger.LogInformation("Aplicando solução: limpar cache para usuário {UserId}", userId);
            }

            if (solucaoLower.Contains("reiniciar") || solucaoLower.Contains("recarregar"))
            {
                // Chamar endpoint de reiniciar sessão
                _logger.LogInformation("Aplicando solução: reiniciar sessão para usuário {UserId}", userId);
            }
        }
    }

    public async Task<List<string>> FiltrarSolucoes(List<string> solucoes, string origem)
    {
        var solucoesFiltradas = new List<string>();

        foreach (var solucao in solucoes)
        {
            var solucaoLower = solucao.ToLower();

            // Remover soluções que usuário final não pode executar
            if (solucaoLower.Contains("dll") || 
                solucaoLower.Contains("verificar dependências") ||
                solucaoLower.Contains("compilar") ||
                solucaoLower.Contains("código fonte"))
            {
                continue; // Pular soluções técnicas demais
            }

            // Manter soluções que usuário pode executar
            if (solucaoLower.Contains("reiniciar") ||
                solucaoLower.Contains("limpar cache") ||
                solucaoLower.Contains("verificar conexão") ||
                solucaoLower.Contains("tentar novamente"))
            {
                solucoesFiltradas.Add(solucao);
            }
            else
            {
                // Adicionar outras soluções genéricas
                solucoesFiltradas.Add(solucao);
            }
        }

        return solucoesFiltradas;
    }
}
