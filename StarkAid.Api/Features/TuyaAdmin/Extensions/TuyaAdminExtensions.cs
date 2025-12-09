using Microsoft.Extensions.DependencyInjection;
using StarkAid.Api.Features.TuyaAdmin.Services;
using StarkAid.Api.Options;

namespace StarkAid.Api.Features.TuyaAdmin.Extensions;

/// <summary>
/// Registra os serviços da integração Tuya Admin.
/// </summary>
public static class TuyaAdminExtensions
{
    public static IServiceCollection AddTuyaAdmin(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<TuyaConfig>(config.GetSection("Tuya"));

        services.AddHttpClient("TuyaAdmin", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<TuyaTokenProvider>();
        services.AddScoped<ITuyaAdminService, TuyaAdminService>();

        return services;
    }
}
