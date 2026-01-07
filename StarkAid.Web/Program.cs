using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
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
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://starkaid.runasp.net/";
    return new HttpClient
    {
        BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/")
    };
});

// Serviços da API
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IEwelinkService, EwelinkService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConfirmService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
