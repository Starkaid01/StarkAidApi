using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using StarkAid.Api.Data;
using StarkAid.Api.EntityConfigurations;
using StarkAid.Api.Middlewares;
using StarkAid.Api.Services;
using Stripe;
using System.Diagnostics;
using System.Text;

try
{
    // 1. Configuração de logs
    var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    Directory.CreateDirectory(logPath);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logPath, "log-.json"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
        .CreateLogger();

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    Log.Information("Iniciando StarkAid.Api...");

    // 2. Configuração do AppSettings e connection string
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection não configurada.");

    // 3. Firebase
    var firebasePath = builder.Configuration["Firebase:CredentialsPath"]
        ?? throw new InvalidOperationException("Firebase credentials path não configurado.");
    FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(firebasePath) });

    // 4. JWT
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT Key não configurada.");
    var keyBytes = Encoding.ASCII.GetBytes(jwtKey);
    var isProd = builder.Environment.IsProduction();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = isProd;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = isProd,
                ValidateAudience = isProd,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"]
            };

            // JWT via query param para WebSocket
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.WebSockets.IsWebSocketRequest)
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

    // 5. CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });


    builder.Services.AddSignalR();

    // 6. Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("UserNivel1Only", policy => policy.RequireRole("UserNivel1", "UserNivel2", "UserNivel3", "Administrador")); 
        options.AddPolicy("UserNivel2Only", policy => policy.RequireRole("UserNivel2", "UserNivel3", "Administrador")); 
        options.AddPolicy("UserNivel3Only", policy => policy.RequireRole("UserNivel3", "Administrador")); 
        options.AddPolicy("AdministradorOnly", policy => policy.RequireRole("Administrador")); 
        options.AddPolicy("PagAtrasadoOnly", policy => policy.RequireRole("PagAtrasado"));
    });

    // 7. DbContext
    builder.Services.AddDbContext<AppDbContext>(opt =>
    {
        opt.UseSqlServer(connectionString, o =>
        {
            o.EnableRetryOnFailure();
            o.CommandTimeout(15);
        });
    });

    builder.Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        });

    // 8. Serviços e Hosted Services
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<RefreshTokenService>();
    builder.Services.AddScoped<DeviceService>();
    builder.Services.AddScoped<ComandoSocialService>();
    builder.Services.AddScoped<AgendamentoService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<DisparoService>();
    builder.Services.AddScoped<FcmNotificationService>();
    builder.Services.AddScoped<FirebaseTokenService>();
    builder.Services.AddScoped<StripeWebhookService>();
    builder.Services.AddScoped<DispositivoDisparoService>();
    builder.Services.AddScoped<StripeService>();

    builder.Services.AddHostedService<AgendamentoWorker>();
    builder.Services.AddHostedService<PasswordResetCleanupService>();
    builder.Services.AddHostedService<MqttHostedService>();
    builder.Services.AddHostedService<AssinaturaStatusChecker>();
    builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
    builder.Services.AddMemoryCache();

    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
    builder.Services.AddSingleton<StripeClient>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<StripeSettings>>().Value;
        return new StripeClient(options.SecretKey);
    });

    // 9. Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opt =>
    {
        opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Digite 'Bearer {token}'"
        });
        // Novo esquema para API Key
        opt.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Name = "Api-Key", // Nome do header usado pela API Key
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Insira a API Key"
        });
        // Requerimentos de segurança
        opt.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            },
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // 10. Pipeline
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    // WebSockets
    app.UseWebSockets(new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(30),
        ReceiveBufferSize = 4 * 1024
    });

    // Middlewares custom
    app.UseMiddleware<DeviceApiKeyMiddleware>();
    app.UseMiddleware<CommandRateLimiterMiddleware>();

    app.MapControllers();
    app.MapHub<StarkAid.Api.Hubs.DeviceHub>("/hubs/device"); // endpoint do hub

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Dispose MQTT on shutdown
    app.Lifetime.ApplicationStopping.Register(() =>
    {
        Task.Run(async () =>
        {
            using var scope = app.Services.CreateScope();
            var mqttService = scope.ServiceProvider.GetService<IMqttClientService>();
            if (mqttService is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
        }).Wait();
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao iniciar a aplicação");
}
finally
{
    Log.CloseAndFlush();
}