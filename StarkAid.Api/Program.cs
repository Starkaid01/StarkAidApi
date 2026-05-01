using Amazon;
using Amazon.TranscribeStreaming;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.SuperIA;
using StarkAid.Api.EntityConfigurations;
using StarkAid.Api.Middlewares;
using StarkAid.Api.Options;
using StarkAid.Api.Services.V1;
using StarkAid.Api.Services.V1.Assinatura;
using StarkAid.Api.Services.V1.Auth;
using StarkAid.Api.Services.V1.Devices;
using StarkAid.Api.Services.V1.SocialCommand;
using StarkAid.Api.Services.V1.SuperIA;
using StarkAid.Api.Services.V1.Email;
using StarkAid.Api.Services.V1.Firebase;
using StarkAid.Api.Services.V1.IA;
using StarkAid.Api.Services.V1.Disparo;
using StarkAid.Api.Services.V1.DispositivoEsp;
using StarkAid.Api.Services.V1.License;
using StarkAid.Api.Services.V1.Weather;
using StarkAid.Api.Services.V1.Suporte;
using StarkAid.Api.Services.V1.Payment.Stripe;
using StarkAid.Api.Hubs;
using StarkAid.Api.Services.Telemetry;
using StarkAid.Api.Services.CommandRouter;
using StarkAid.Api.Services.CommandRouter.Handlers;
using StarkAid.Api.Services.V1.Fun;
using StarkAid.Api.Services.V1.Music;
using StarkAid.Api.Services.V1.Comodos;
using StarkAid.Api.Services.V1.Rotinas;
using Stripe;
using System.Diagnostics;
using System.Threading.RateLimiting;
using System.Text;
using PlanoLimitesService = StarkAid.Api.Services.PlanoLimitesService;
using IStarkCoinConversionService = StarkAid.Api.Services.V1.IStarkCoinConversionService;
using StarkCoinConversionService = StarkAid.Api.Services.StarkCoinConversionService;
using ITokenUsageService = StarkAid.Api.Services.ITokenUsageService;
using TokenUsageService = StarkAid.Api.Services.TokenUsageService;
using RefreshTokenServiceV1 = StarkAid.Api.Services.V1.RefreshTokenService;
using IEwelinkServiceV1 = StarkAid.Api.Services.V1.IEwelinkService;
using EwelinkServiceV1 = StarkAid.Api.Services.V1.EwelinkService;
using TranscribeProxyServiceV1 = StarkAid.Api.Services.V1.TranscribeProxyService;
using WeeklyTokensResetService = StarkAid.Api.Services.WeeklyTokensResetService;
using StarkAid.Api.Services.V1.Lembretes;
using StarkAid.Api.Services.V1.Support.Agents;
using StarkAid.Api.Services.V1.Support.Learning;
using StarkAid.Api.Services.V1.Support.Heuristics;
using StarkAid.Api.Services.V1.Support.SignalR;
using StarkAid.Api.Services.V1.Support.BackgroundServices;

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

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000);
    });

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
    
    // Resolver caminho relativo se necessário
    // Usar ContentRootPath que aponta para o diretório raiz do projeto
    if (!Path.IsPathRooted(firebasePath))
    {
        var contentRoot = builder.Environment.ContentRootPath;
        firebasePath = Path.Combine(contentRoot, firebasePath);
        
        // Se ainda não existir, tentar a partir do diretório atual (para compatibilidade)
        if (!System.IO.File.Exists(firebasePath))
        {
            var currentDirPath = Path.Combine(Directory.GetCurrentDirectory(), firebasePath);
            if (System.IO.File.Exists(currentDirPath))
            {
                firebasePath = currentDirPath;
            }
        }
    }
    
    // Verificar se o arquivo existe
    if (!System.IO.File.Exists(firebasePath))
    {
        throw new FileNotFoundException($"Arquivo Firebase não encontrado: {firebasePath}. Diretório atual: {Directory.GetCurrentDirectory()}, ContentRoot: {builder.Environment.ContentRootPath}");
    }
    
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

            // JWT via query param para WebSocket e via header Authorization para SignalR
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Para WebSockets (query string)
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.WebSockets.IsWebSocketRequest)
                    {
                        context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                    
                    // Para SignalR (header Authorization já é tratado automaticamente pelo JwtBearer)
                    // Mas também podemos aceitar via query string para SignalR
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        var signalRToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(signalRToken))
                        {
                            context.Token = signalRToken;
                        }
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

    // 5. Rate Limiting Híbrido (Por Usuário + Por IP)
    builder.Services.AddRateLimiter(options =>
    {
        // Rate limiting secundário por IP (proteção DDoS/Brute Force) - 200 req/min
        // Aplicado apenas como hedge contra ataques externos
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1)
                }));
        
        // Rate limiting primário por usuário - 100 req/min gerais
        // Aplicado quando o usuário está autenticado
        options.AddPolicy<string>("UserRateLimit", context =>
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var partitionKey = !string.IsNullOrEmpty(userId) 
                ? $"user_{userId}" 
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        });

        // Rate limiting específico para IA - 10 req/min por usuário autenticado
        options.AddPolicy<string>("IaEndpoint", context =>
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var partitionKey = !string.IsNullOrEmpty(userId) 
                ? $"ia_user_{userId}" 
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
        });

        // Rate limiting específico para controle IoT (Commands/publish) - 5 req/min por usuário
        options.AddPolicy<string>("IoTCommandLimit", context =>
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var partitionKey = !string.IsNullOrEmpty(userId) 
                ? $"iot_user_{userId}" 
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 3
                });
        });

        // Rate limiting específico para endpoint de configuração (90 req/min)
        options.AddFixedWindowLimiter("ConfigEndpoint", options =>
        {
            options.PermitLimit = 90;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 10;
        });
    });

    // 6. CORS (melhorado para segurança)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy => 
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("X-RateLimit-Remaining", "X-RateLimit-Reset");
        });
    });


    builder.Services.AddSignalR();

    // Bind Jwt settings from configuration so services can inject IOptions<JwtSettings>
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

    // 7. Policies
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

    builder.Services.AddHttpContextAccessor();
    
    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    builder.Services.AddVersionedApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        });




    // 8. Serviços e Hosted Services
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<RefreshTokenServiceV1>();
    builder.Services.AddScoped<DeviceService>();
    builder.Services.AddScoped<ComandoSocialService>();
    builder.Services.AddScoped<AgendamentoService>();
    builder.Services.AddScoped<StarkAid.Api.Services.V1.Email.IEmailService, StarkAid.Api.Services.V1.Email.EmailService>();
    builder.Services.AddScoped<StarkAid.Api.Services.V1.Notifications.NotificationService>();
    builder.Services.AddScoped<DisparoService>();
    builder.Services.AddScoped<FcmNotificationService>();
    builder.Services.AddScoped<FirebaseTokenService>();

    // 
    builder.Services.AddScoped<StripeWebhookService>();
    builder.Services.AddScoped<DispositivoDisparoService>();
    builder.Services.AddScoped<StripeService>();
    builder.Services.AddScoped<IEwelinkServiceV1, EwelinkServiceV1>();
    builder.Services.AddSingleton<PlanoLimitesService>();
    builder.Services.AddSingleton<IStarkCoinConversionService, StarkCoinConversionService>();
    builder.Services.AddScoped<ITokenUsageService, TokenUsageService>();

    // 9. Command Router e Telemetria
    builder.Services.AddScoped<IEscopoConversacionalService, EscopoConversacionalService>();
    builder.Services.AddScoped<IComodoService, ComodoService>();

    builder.Services.AddScoped<ITelemetryService, TelemetryService>();
    builder.Services.AddScoped<ICommandRouter, CommandRouter>();
    builder.Services.AddScoped<IRotinaService, RotinaService>();
    builder.Services.AddScoped<ICommandHandler, RoutineCommandHandler>();
    
    // Fun Module Services (Prioridade Máxima)
    builder.Services.AddScoped<IIntentDetector, StarkAid.Api.Services.V1.Fun.IntentDetector>();
    builder.Services.AddScoped<IMathService, StarkAid.Api.Services.V1.Fun.MathService>();
    builder.Services.AddScoped<IJokeService, StarkAid.Api.Services.V1.Fun.JokeService>();
    builder.Services.AddScoped<ILocalCommandRouter, StarkAid.Api.Services.V1.Fun.LocalCommandRouter>();
    builder.Services.AddScoped<ICommandHandler, FunCommandHandler>();

    // Music Module Services
    builder.Services.AddScoped<IMusicIntentService, MusicIntentService>();
    builder.Services.AddScoped<IYouTubeMusicService, YouTubeMusicService>();
    builder.Services.AddSingleton<IExternalAudioResolver, OnlineAudioResolver>();

    // Outros Handlers
    builder.Services.AddScoped<ICommandHandler, DeviceCommandHandler>();
    builder.Services.AddScoped<ICommandHandler, HomeCommandHandler>();
    builder.Services.AddScoped<ICommandHandler, SocialCommandHandler>();
    builder.Services.AddScoped<ICommandHandler, SystemCommandHandler>();
    builder.Services.AddScoped<ICommandHandler, LearningCommandHandler>();
    
    // builder.Services.AddScoped<ICommandHandler, IaCommandHandler>();
    builder.Services.AddScoped<IAprendizadoService, AprendizadoService>();

    // Novos Serviços de Suporte Inteligente (Agente Operacional)
    builder.Services.AddScoped<ISupportLearningService, SupportLearningService>();
    builder.Services.AddScoped<ISupportHeuristicService, SupportHeuristicService>();
    builder.Services.AddScoped<ISupportActionExecutor, SupportActionExecutor>();
    builder.Services.AddScoped<ISupportMessageRouter, SupportMessageRouter>();


    // 🔍 Diagnóstico do Banco de Dados
    try
    {
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sys.tables";
                var tables = new List<string>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
                }
                Console.WriteLine($"🔍 Tabelas encontradas: {string.Join(", ", tables)}");

                if (tables.Contains("Users"))
                {
                    cmd.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('Users')";
                    var cols = new List<string>();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) cols.Add(reader.GetString(0));
                    }
                    Console.WriteLine($"🔍 Colunas na tabela 'Users': {string.Join(", ", cols)}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔍 Erro no diagnóstico do banco: {ex.Message}");
    }

    // 🧩 Lê o domínio Cloudflare atual do banco e injeta na configuração
    try
    {
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = db.ConfiguracoesSistema.AsNoTracking().FirstOrDefault();

            if (config != null && !string.IsNullOrWhiteSpace(config.DominioCloudflare))
            {
                builder.Configuration["WppConnectOptions:BaseUrl"] = config.DominioCloudflare;
                Console.WriteLine($"🌍 Domínio Cloudflare carregado do banco: {config.DominioCloudflare}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🌍 Erro ao carregar domínio Cloudflare: {ex.Message}");
    }



    builder.Services.AddHostedService<AgendamentoWorker>();
    builder.Services.AddHostedService<PasswordResetCleanupService>();
    builder.Services.AddHostedService<MqttHostedService>();
    builder.Services.AddHostedService<AssinaturaStatusChecker>();
    builder.Services.AddHostedService<WeeklyTokensResetService>();
    builder.Services.AddHostedService<StarkAid.Api.Services.Background.CognitiveGarbageCollectorService>();
    builder.Services.AddHostedService<StarkAid.Api.Services.Background.LembreteSchedulerService>();
    builder.Services.AddHostedService<RoutineSchedulerService>();
    builder.Services.AddHostedService<SupportCognitiveGarbageCollector>();

    builder.Services.AddSingleton<IMqttClientService, MqttClientService>();

    // ⚙️ WPPConnect - Corrigido para a seção certa
    builder.Services.Configure<WppConnectOptions>(builder.Configuration.GetSection("WppConnectOptions"));

    builder.Services.Configure<NlpConnectOptions>(builder.Configuration.GetSection("NlpConnectOptions"));

    builder.Services.Configure<AiTelemetryOptions>(builder.Configuration.GetSection(AiTelemetryOptions.ConfigSection));


    builder.Services.Configure<IaApiKeys>(builder.Configuration.GetSection("IaApiKeys"));
    builder.Services.AddSingleton<StarkAid.Api.Services.V1.SuperIA.IaService>();


    builder.Services.AddSingleton<AmazonTranscribeStreamingClient>(sp =>
    {
        var accessKey = builder.Configuration["AWS:AccessKey"];
        var secretKey = builder.Configuration["AWS:SecretKey"];
        var region = builder.Configuration["AWS:Region"];

        var config = new AmazonTranscribeStreamingConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        return new AmazonTranscribeStreamingClient(accessKey, secretKey, config);
    });

    builder.Services.AddSingleton<TranscribeProxyServiceV1>(sp =>
    {
        var transcribeClient = sp.GetRequiredService<AmazonTranscribeStreamingClient>();
        var logger = sp.GetRequiredService<ILogger<TranscribeProxyServiceV1>>();
        var tokenUsage = sp.GetRequiredService<ITokenUsageService>();
        return new TranscribeProxyServiceV1(transcribeClient, sp, logger, tokenUsage);
    });

    // Stripe
    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
    builder.Services.AddSingleton(sp =>
        sp.GetRequiredService<IOptions<StripeSettings>>().Value);

    // ✅ StripeClient configurado corretamente
    builder.Services.AddSingleton<StripeClient>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<StripeSettings>>().Value;
        return new StripeClient(options.SecretKey);
    });

    StripeConfiguration.ApiKey = builder.Configuration["StripeSettings:SecretKey"];

    builder.Services.AddMemoryCache(options =>
    {
        options.SizeLimit = 1024; // Limite de 1024 itens no cache
    });

    // 9. Swagger com suporte a versionamento
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opt =>
    {
        var provider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
        
        foreach (var description in provider.ApiVersionDescriptions)
        {
            opt.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "StarkAid API",
                Version = description.ApiVersion.ToString(),
                Description = $"StarkAid API {description.ApiVersion}"
            });
        }

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

    builder.Services.AddScoped<IAgendamentoService, AgendamentoService>();
    builder.Services.AddScoped<IDeviceService, DeviceService>();
    builder.Services.AddScoped<StarkAid.Api.Services.V1.DispositivoEsp.DispositivoEspService>();
    builder.Services.AddScoped<LicenseService>();
    builder.Services.AddHttpClient<WeatherService>();
    builder.Services.AddScoped<IWeatherService, WeatherService>();
    
    // Serviços de Suporte (Novo Sistema Baseado no Router)
    // builder.Services.AddSingleton<StarkAid.Api.Services.V1.Suporte.ConversationStateManager>();
    // builder.Services.AddScoped<StarkAid.Api.Services.V1.Suporte.SupportAgentEngine>();
    
    // Serviços de Suporte (Legacy - Deprecando se não usado, mas mantendo para compilação se necessário)
    builder.Services.AddSingleton<StarkAid.Api.Services.V1.Suporte.ISupportQueueService, StarkAid.Api.Services.V1.Suporte.SupportQueueService>();
    builder.Services.AddScoped<StarkAid.Api.Services.V1.Suporte.ISupportIaService, StarkAid.Api.Services.V1.Suporte.SupportIaService>();
    builder.Services.AddScoped<StarkAid.Api.Services.V1.Suporte.ISuporteChatService, StarkAid.Api.Services.V1.Suporte.SuporteChatService>();
    
    // Lembretes Module
    builder.Services.AddScoped<ILembreteService, LembreteService>();

    var app = builder.Build();

    // 10. Pipeline
    //app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseRateLimiter();
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
    app.MapHub<StarkAid.Api.Hubs.DeviceHub>("/hubs/device");
    app.MapHub<StarkAid.Api.Hubs.DispositivoEspHub>("/hubs/dispositivo-esp");
    // Mapeando para o novo ChatHub mantendo a rota antiga para compatibilidade do cliente ou alterando se necessário
    // User pediu ChatHub.cs. Vou mapear a rota existente para o novo Hub.
    app.MapHub<StarkAid.Api.Hubs.ChatHub>("/hubs/support-chat");
    app.MapHub<StarkAid.Api.Hubs.SupportAgentHub>("/hubs/intelligent-support");
    app.MapHub<StarkAid.Api.Hubs.AvatarHub>("/hubs/avatar");

    // Swagger disponível em Development
    // IMPORTANTE: Para acessar via IP (ex: http://192.168.2.106:5000/swagger),
    // certifique-se de que ASPNETCORE_ENVIRONMENT=Development
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", 
                    $"StarkAid API {description.GroupName.ToUpperInvariant()}");
            }
            // Permitir acesso via IP
            options.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
        });
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

    // Popular códigos de erro na primeira execução
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

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
