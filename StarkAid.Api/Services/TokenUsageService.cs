using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services;

public record TokenUsageResult(bool Success, bool InsufficientBalance, int TokensCharged, int StarkCoinsCharged, int RequiredCoins);

public class TokenInsufficientException : Exception
{
    public int RequiredCoins { get; }
    public TokenInsufficientException(int requiredCoins) : base("Saldo insuficiente para processar a IA.")
    {
        RequiredCoins = requiredCoins;
    }
}

public interface ITokenUsageService
{
    Task<TokenUsageResult> TryConsumeTokensAsync(User user, int tokensSolicitados, CancellationToken cancellationToken = default);
    Task<TokenUsageResult> TryConsumeTokensAsync(User user, int tokensSolicitados, bool allowAutoStarkCoinsUsage, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aplica as regras de limite semanal e consumo de StarkCoins para excedentes.
/// </summary>
public class TokenUsageService : ITokenUsageService
{
    private readonly PlanoLimitesService _planoLimites;
    private readonly IStarkCoinConversionService _conversionService;
    private readonly AppDbContext _db;

    public TokenUsageService(
        PlanoLimitesService planoLimites,
        IStarkCoinConversionService conversionService,
        AppDbContext db)
    {
        _planoLimites = planoLimites;
        _conversionService = conversionService;
        _db = db;
    }

    public async Task<TokenUsageResult> TryConsumeTokensAsync(User user, int tokensSolicitados, CancellationToken cancellationToken = default)
    {
        // Para compatibilidade com outros serviços, permite uso automático de StarkCoins
        return await TryConsumeTokensAsync(user, tokensSolicitados, true, cancellationToken);
    }

    public async Task<TokenUsageResult> TryConsumeTokensAsync(User user, int tokensSolicitados, bool allowAutoStarkCoinsUsage, CancellationToken cancellationToken = default)
    {
        if (tokensSolicitados <= 0)
            return new TokenUsageResult(true, false, 0, 0, 0);

        var trackedUser = _db.Users.Local.FirstOrDefault(u => u.Id == user.Id) ?? user;
        var limite = _planoLimites.ObterLimiteTokensSemana(trackedUser);
        var tokensUsados = trackedUser.TokensConsumidosSemana;
        var tokensRestantes = Math.Max(0, limite - tokensUsados);

        if (tokensSolicitados <= tokensRestantes)
        {
            trackedUser.TokensConsumidosSemana += tokensSolicitados;
            await _db.SaveChangesAsync(cancellationToken);
            return new TokenUsageResult(true, false, tokensSolicitados, 0, 0);
        }

        // Se não permite uso automático de StarkCoins, sempre retorna erro
        if (!allowAutoStarkCoinsUsage)
        {
            var excedente = tokensSolicitados - tokensRestantes;
            var coinsNecessarias = _conversionService.CalcularStarkCoinsNecessarias(excedente);
            return new TokenUsageResult(false, true, 0, 0, coinsNecessarias);
        }

        // Uso automático de StarkCoins (para outros serviços)
        var excedenteAuto = tokensSolicitados - tokensRestantes;
        var coinsNecessariasAuto = _conversionService.CalcularStarkCoinsNecessarias(excedenteAuto);

        if (trackedUser.StarkCoinBalance >= coinsNecessariasAuto)
        {
            trackedUser.StarkCoinBalance -= coinsNecessariasAuto;

            trackedUser.TokensConsumidosSemana += tokensSolicitados;
            await _db.SaveChangesAsync(cancellationToken);
            return new TokenUsageResult(true, false, tokensSolicitados, coinsNecessariasAuto, 0);
        }

        return new TokenUsageResult(false, true, 0, 0, coinsNecessariasAuto);
    }
}

