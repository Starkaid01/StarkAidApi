using StarkAid.Api.Entities;

namespace StarkAid.Api.Services;

/// <summary>
/// Centraliza as regras de planos Free/Premium para limites e cobranças.
/// </summary>
public class PlanoLimitesService
{
    private const int LimiteTokensFree = 1200;
    private const int LimiteTokensPremium = 4500;
    private const int LimiteAgendamentosFree = 2;

    public int ObterLimiteTokensSemana(User user) =>
        (user.PlanType == UserPlanType.Premium || user.Plano == PlanoStarkAid.Premium)
            ? LimiteTokensPremium
            : LimiteTokensFree;

    public int ObterLimiteAgendamentos(User user) =>
        (user.PlanType == UserPlanType.Premium || user.Plano == PlanoStarkAid.Premium)
            ? -1
            : LimiteAgendamentosFree;

    public bool ExibeAnuncios(User user) =>
        !(user.PlanType == UserPlanType.Premium || user.Plano == PlanoStarkAid.Premium);

    public bool DeveExibirAds(User user) => ExibeAnuncios(user);

    public bool PodeCriarAgendamento(User user) => PodeCriarAgendamento(user, user.Agendamentos?.Count ?? 0);

    public bool PodeCriarAgendamento(User user, int agendamentosAtuais)
    {
        var limite = ObterLimiteAgendamentos(user);
        if (limite < 0) return true;
        return agendamentosAtuais < limite;
    }

    public int CalcularAgendamentosRestantes(User user, int agendamentosAtuais)
    {
        var limite = ObterLimiteAgendamentos(user);
        if (limite < 0) return -1;
        return Math.Max(0, limite - agendamentosAtuais);
    }

    public int ConverterTokensParaStarkCoins(int promptTokens, int completionTokens)
    {
        var totalTokens = Math.Max(0, promptTokens) + Math.Max(0, completionTokens);
        if (totalTokens == 0) return 0;
        return (int)Math.Ceiling(totalTokens / 100m);
    }

    /// <summary>
    /// Calcula quantos tokens dessa operação devem ser cobrados em StarkCoins,
    /// considerando o consumo acumulado do usuário.
    /// </summary>
    public int CalcularTokensParaCobrar(User user, int tokensUsados)
    {
        var limite = ObterLimiteTokensSemana(user);
        var consumoAtual = user.TokensConsumidosSemana;
        var novoTotal = consumoAtual + tokensUsados;

        var excedenteAntes = Math.Max(0, consumoAtual - limite);
        var excedenteDepois = Math.Max(0, novoTotal - limite);

        return Math.Max(0, excedenteDepois - excedenteAntes);
    }
}

