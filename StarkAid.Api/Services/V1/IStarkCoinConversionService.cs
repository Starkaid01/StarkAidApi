namespace StarkAid.Api.Services.V1;

public interface IStarkCoinConversionService
{
    decimal TokensPorStarkcoin { get; }

    int CalcularStarkCoinsNecessarias(int tokens);
}

