namespace StarkAid.Api.Services;

public interface IStarkCoinConversionService
{
    decimal TokensPorStarkcoin { get; }

    int CalcularStarkCoinsNecessarias(int tokens);
}

