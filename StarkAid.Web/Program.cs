using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StarkAid.Web;
using StarkAid.Web.Services;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    return new HttpClient
    {
        BaseAddress = new Uri("https://starkaid.runasp.net/")  // Confirme se é 5000 ou 5001; logs mostram 5000
    };
});

// Serviços da API
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IEwelinkService, EwelinkService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();

await builder.Build().RunAsync();