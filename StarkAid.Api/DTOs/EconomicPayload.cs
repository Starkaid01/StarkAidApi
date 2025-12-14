namespace StarkAid.Api.DTOs;

public record EconomicPayload(
    string planType,
    int StarkCoinBalance,
    int tokensConsumidosSemana,
    int tokensSemanaMax,
    int tokensRestantes,
    bool adsEnabled,
    int agendamentosMax,
    int agendamentosRestantes,
    int rate = 100);

