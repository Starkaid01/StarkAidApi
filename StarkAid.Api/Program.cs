using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StarkAid.Api.Converters;
using StarkAid.Api.Data;
using StarkAid.Api.Middlewares;
using StarkAid.Api.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

var firebaseConfigPath = builder.Configuration["Firebase:CredentialsPath"];
if (string.IsNullOrEmpty(firebaseConfigPath))
    throw new InvalidOperationException("Caminho para credenciais Firebase não configurado.");

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(firebaseConfigPath)
});

// 🔍 Carregar configs na ordem correta
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 🔐 Validação da chave JWT obrigatória
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("Chave JWT não configurada.");

var keyBytes = Encoding.ASCII.GetBytes(jwtKey);
var isProduction = true;

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserNivel1Only", policy =>
        policy.RequireRole("UserNivel1", "UserNivel2", "UserNivel3", "Administrador"));

    options.AddPolicy("UserNivel2Only", policy =>
        policy.RequireRole("UserNivel2", "UserNivel3", "Administrador"));

    options.AddPolicy("UserNivel3Only", policy =>
        policy.RequireRole("UserNivel3", "Administrador"));

    options.AddPolicy("AdministradorOnly", policy =>
        policy.RequireRole("Administrador"));

    options.AddPolicy("PagAtrasadoOnly", policy =>
        policy.RequireRole("PagAtrasado"));
});

// 📦 Configura banco PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 📑 Serviços
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddTransient<SeedService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<ComandoSocialService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<DisparoService>();
builder.Services.AddScoped<FcmNotificationService>();
builder.Services.AddScoped<FirebaseTokenService>();
builder.Services.AddHostedService<AgendamentoWorker>();
builder.Services.AddHostedService<PasswordResetCleanupService>();
builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
builder.Services.AddSingleton<MercadoPagoService>();
builder.Services.AddScoped<DispositivoDisparoService>();
builder.Services.AddMemoryCache();

// 🔐 JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = isProduction;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = isProduction,
        ValidateAudience = isProduction,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"]
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection string carregada: {connectionString}");
// 📝 Swagger com JWT e API Key
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite 'Bearer ' + token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Chave de API do usuário",
        Name = "Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            new string[] {}
        }
    });
});

// 🎛 Controllers e Serialização
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});



// 🔈 URL para ouvir conexões externas
builder.WebHost.UseUrls("http://0.0.0.0:5238");


var app = builder.Build();

// 🚀 Inicia MQTT Service
var mqttService = app.Services.GetRequiredService<IMqttClientService>();
await (mqttService as MqttClientService)!.StartAsync();

// 🌱 Seed inicial de admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seedService = services.GetRequiredService<SeedService>();
    seedService.SeedAdminUser();
}

// 📑 Middlewares padrão
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<DeviceApiKeyMiddleware>();
app.UseMiddleware<CommandRateLimiterMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
