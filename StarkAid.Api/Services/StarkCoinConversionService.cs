namespace StarkAid.Api.Services;

public class StarkCoinConversionService : IStarkCoinConversionService
{
    public decimal TokensPorStarkcoin => 100m;

    public int CalcularStarkCoinsNecessarias(int tokens)
    {
        var tokensPositivos = Math.Max(0, tokens);
        if (tokensPositivos == 0) return 0;
        return (int)Math.Ceiling(tokensPositivos / TokensPorStarkcoin);
    }
}

