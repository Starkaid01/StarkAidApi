using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Devices;
using StarkAid.Api.DTOs.V1.Admin;
using System;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using StarkAid.Api.Helpers;
using StarkAid.Api.Options;
using StarkAid.Api.Services.V1.SuperIA;
using Microsoft.Extensions.Options;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Authorize(Policy = "AdministradorOnly")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMqttClientService _mqttService;
        private readonly Options.AiTelemetryOptions _telemetryOptions;
        private readonly Services.V1.SuperIA.IaService _iaService;

        public AdminController(AppDbContext context, IMqttClientService mqttService, IOptions<AiTelemetryOptions> telemetryOptions, Services.V1.SuperIA.IaService iaService)
        {
            _context = context;
            _mqttService = mqttService;
            _telemetryOptions = telemetryOptions.Value;
            _iaService = iaService;
        }
        
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok("Acesso exclusivo para administradores.");
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var apiStatus = "OK";
            var mqttStatus = _mqttService.IsConnected ? "Conectado" : "Desconectado";

            return Ok(new
            {
                totalUsers,
                activeUsers,
                inactiveUsers = totalUsers - activeUsers,
                apiStatus,
                mqttStatus,
                mqttConnected = _mqttService.IsConnected
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastUpdatedAt,
                    u.StarkCoins,
                    u.RemovalAds
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastUpdatedAt,
                    u.StarkCoins,
                    u.RemovalAds,
                    u.ApiKey
                })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email;

            if (!string.IsNullOrWhiteSpace(request.Role))
                user.Role = request.Role;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (request.StarkCoinBalance.HasValue)
                user.StarkCoins = request.StarkCoinBalance.Value;

            if (!string.IsNullOrWhiteSpace(request.RemovalAds))
                user.RemovalAds = request.RemovalAds;

            if (request.Estado != null)
                user.Estado = request.Estado;

            if (request.Cidade != null)
                user.Cidade = request.Cidade;

            if (request.Bairro != null)
                user.Bairro = request.Bairro;

            user.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuário atualizado com sucesso." });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuário deletado com sucesso." });
        }

        [HttpGet("users/{id}/details")]
        public async Task<IActionResult> GetUserDetails(Guid id)
        {
            var user = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.StarkCoins,
                    u.ApiKey,
                    u.Cidade,
                    u.Bairro
                })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            // Buscar dispositivos
            var devices = await _context.Devices
                .Where(d => d.UserId == id)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Comando,
                    d.MqttTopic,
                    d.ApiKey,
                    AgendamentosCount = _context.Agendamentos.Count(a => a.DeviceId == d.Id)
                })
                .ToListAsync();

            // Buscar comandos sociais
            var comandosSociais = await _context.ComandosSociais
                .Where(c => c.UserId == id)
                .Select(c => new
                {
                    c.Id,
                    c.Comando,
                    c.Resposta,
                    c.RespostasAleatorias
                })
                .ToListAsync();

            // Buscar agendamentos
            var agendamentos = await _context.Agendamentos
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.AgendadoPara)
                .Select(a => new
                {
                    a.Id,
                    a.DeviceId,
                    DeviceName = a.Device.Name,
                    a.Comando,
                    a.AgendadoPara,
                    a.Executado,
                    a.Recorrencia
                })
                .ToListAsync();

            // Buscar último comando do histórico de IA
            var ultimoComando = await _context.IaHistoricos
                .Where(h => h.UserId == id)
                .OrderByDescending(h => h.CriadoEm)
                .Select(h => new
                {
                    h.TextoUsuario,
                    h.TextoIa,
                    h.CriadoEm
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                user,
                devices,
                comandosSociais,
                agendamentos,
                ultimoComando,
                totalDevices = devices.Count,
                totalComandosSociais = comandosSociais.Count,
                totalAgendamentos = agendamentos.Count
            });
        }

        [HttpGet("users/{id}/dashboard")]
        public async Task<IActionResult> GetUserDashboard(Guid id)
        {
            var user = await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    StarkCoinBalance = u.StarkCoins,
                    ApiKey = u.ApiKey,
                    Cidade = u.Cidade,
                    Bairro = u.Bairro,
                    Estado = u.Estado
                })
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return NotFound();

            // Contadores
            var quantidadeDispositivosEsp = await _context.DispositivosEsp
                .CountAsync(d => d.UserId == id);
            
            var quantidadeDispositivosEwelink = await _context.EwelinkDevices
                .CountAsync(d => d.UserId == id);
            
            var quantidadeDispositivosStarkSwitch = await _context.Devices
                .CountAsync(d => d.UserId == id);
            
            var totalComandosSociais = await _context.ComandosSociais
                .CountAsync(c => c.UserId == id);

            // Buscar atividades (soft e app)
            var activitySoft = await _context.UserActivities
                .FirstOrDefaultAsync(a => a.UserId == id && a.Origem == "soft");
            
            var activityApp = await _context.UserActivities
                .FirstOrDefaultAsync(a => a.UserId == id && a.Origem == "app");

            // Buscar último comando social do histórico
            var ultimoComandoSocialHistorico = await _context.ComandosSociais
                .Where(c => c.UserId == id)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            // Buscar último comando IA do histórico
            var ultimoComandoIAHistorico = await _context.IaHistoricos
                .Where(h => h.UserId == id)
                .OrderByDescending(h => h.CriadoEm)
                .FirstOrDefaultAsync();

            // Verificar se usuário está online
            var usuarioOnline = await _context.UserSessions
                .AnyAsync(s => s.UserId == id && s.IsActive && 
                    s.LastActivityAt.HasValue && 
                    s.LastActivityAt.Value > DateTime.UtcNow.AddMinutes(-5));

            // Buscar última sessão ativa para obter último form/activity por origem
            var ultimaSessaoSoft = await _context.UserSessions
                .Where(s => s.UserId == id && s.Origem == "soft")
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();

            var ultimaSessaoApp = await _context.UserSessions
                .Where(s => s.UserId == id && s.Origem == "app")
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();
            
            // Helper local function to map
            UserActivityDto MapActivity(UserActivity? act, UserSession? session) {
                if(act == null && session == null) return null;
                return new UserActivityDto {
                    UltimoComandoEsp = act?.UltimoComandoEsp,
                    UltimoComandoEwelink = act?.UltimoComandoEwelink,
                    UltimoComandoStarkSwitch = act?.UltimoComandoStarkSwitch,
                    UltimoComandoSocial = act?.UltimoComandoSocial,
                    UltimaRespostaSocial = act?.UltimaRespostaSocial,
                    UltimoComandoIA = act?.UltimoComandoIA,
                    UltimaRespostaIA = act?.UltimaRespostaIA,
                    LastUpdatedAt = act?.LastUpdatedAt ?? session?.LastActivityAt,
                    UltimaUiAcessada = session?.SessionName,
                    // Infer device logic: prioritizing latest populated field or just leaving null if not explicit
                    UltimoDispositivoAcionado = !string.IsNullOrEmpty(act?.UltimoComandoEsp) ? "Esp Device" : 
                                                !string.IsNullOrEmpty(act?.UltimoComandoEwelink) ? "EweLink Device" :
                                                !string.IsNullOrEmpty(act?.UltimoComandoStarkSwitch) ? "StarkSwitch" : null 
                };
            }

            var combinedLastActivity = (ultimaSessaoSoft?.LastActivityAt > ultimaSessaoApp?.LastActivityAt) 
                                        ? ultimaSessaoSoft?.LastActivityAt 
                                        : ultimaSessaoApp?.LastActivityAt;

            // Para social e IA, usar do histórico se não tiver na activity (mantendo lógica anterior para campos combinados)
            var ultimoComandoSocial = activitySoft?.UltimoComandoSocial ?? activityApp?.UltimoComandoSocial ?? ultimoComandoSocialHistorico?.Comando;
            var ultimaRespostaSocial = activitySoft?.UltimaRespostaSocial ?? activityApp?.UltimaRespostaSocial ?? ultimoComandoSocialHistorico?.Resposta;
            var ultimoComandoIA = activitySoft?.UltimoComandoIA ?? activityApp?.UltimoComandoIA ?? ultimoComandoIAHistorico?.TextoUsuario;
            var ultimaRespostaIA = activitySoft?.UltimaRespostaIA ?? activityApp?.UltimaRespostaIA ?? ultimoComandoIAHistorico?.TextoIa;

            // COMBINED logic (legacy/top level)
            var ultimoComandoEsp = (activitySoft?.LastUpdatedAt ?? DateTimeOffset.MinValue) >= (activityApp?.LastUpdatedAt ?? DateTimeOffset.MinValue)
                ? activitySoft?.UltimoComandoEsp ?? activityApp?.UltimoComandoEsp
                : activityApp?.UltimoComandoEsp ?? activitySoft?.UltimoComandoEsp;
            
            var ultimoComandoEwelink = (activitySoft?.LastUpdatedAt ?? DateTimeOffset.MinValue) >= (activityApp?.LastUpdatedAt ?? DateTimeOffset.MinValue)
                ? activitySoft?.UltimoComandoEwelink ?? activityApp?.UltimoComandoEwelink
                : activityApp?.UltimoComandoEwelink ?? activitySoft?.UltimoComandoEwelink;
            
            var ultimoComandoStarkSwitch = (activitySoft?.LastUpdatedAt ?? DateTimeOffset.MinValue) >= (activityApp?.LastUpdatedAt ?? DateTimeOffset.MinValue)
                ? activitySoft?.UltimoComandoStarkSwitch ?? activityApp?.UltimoComandoStarkSwitch
                : activityApp?.UltimoComandoStarkSwitch ?? activitySoft?.UltimoComandoStarkSwitch;


            var dashboard = new UserDashboardResponse
            {
                User = user,
                QuantidadeDispositivosEsp = quantidadeDispositivosEsp,
                QuantidadeDispositivosEwelink = quantidadeDispositivosEwelink,
                QuantidadeDispositivosStarkSwitch = quantidadeDispositivosStarkSwitch,
                TotalComandosSociais = totalComandosSociais,
                UltimoComandoEsp = ultimoComandoEsp ?? "Nenhum comando",
                UltimoComandoEwelink = ultimoComandoEwelink ?? "Nenhum comando",
                UltimoComandoStarkSwitch = ultimoComandoStarkSwitch ?? "Nenhum comando",
                UltimoComandoSocial = ultimoComandoSocial ?? "Nenhum comando",
                UltimaRespostaSocial = ultimaRespostaSocial ?? "Nenhuma resposta",
                UltimoComandoIA = ultimoComandoIA ?? "Nenhum comando",
                UltimaRespostaIA = ultimaRespostaIA ?? "Nenhuma resposta",
                UsuarioOnline = usuarioOnline,
                UltimoFormAcessado = (ultimaSessaoSoft?.LastActivityAt > ultimaSessaoApp?.LastActivityAt) ? ultimaSessaoSoft?.SessionName : ultimaSessaoApp?.SessionName ?? "Nenhum form acessado",
                UltimaActivityAcessada = combinedLastActivity,
                
                ActivitySoft = MapActivity(activitySoft, ultimaSessaoSoft),
                ActivityApp = MapActivity(activityApp, ultimaSessaoApp)
            };

            return Ok(dashboard);
        }

        // ========== ADMIN DEVICE MANAGEMENT ==========
        [HttpPut("devices/{deviceId}")]
        public async Task<IActionResult> UpdateDevice(Guid deviceId, [FromBody] AdminUpdateDeviceRequest request)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
                device.Name = request.Name;

            if (request.Comando != null)
                device.Comando = request.Comando;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dispositivo atualizado com sucesso." });
        }

        [HttpDelete("devices/{deviceId}")]
        public async Task<IActionResult> DeleteDevice(Guid deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
                return NotFound();

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Dispositivo deletado com sucesso." });
        }

        // ========== ADMIN SOCIAL COMMAND MANAGEMENT ==========
        [HttpPost("comandos-sociais")]
        public async Task<IActionResult> CreateComandoSocial([FromBody] AdminCreateComandoSocialRequest request)
        {
            var comando = new ComandoSocial
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Comando = request.Comando,
                Resposta = request.Resposta,
                RespostasAleatorias = request.RespostasAleatorias
            };

            _context.ComandosSociais.Add(comando);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Comando social criado com sucesso.", id = comando.Id });
        }

        [HttpPut("comandos-sociais/{comandoId}")]
        public async Task<IActionResult> UpdateComandoSocial(Guid comandoId, [FromBody] AdminUpdateComandoSocialRequest request)
        {
            var comando = await _context.ComandosSociais.FindAsync(comandoId);
            if (comando == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Comando))
                comando.Comando = request.Comando;

            if (!string.IsNullOrWhiteSpace(request.Resposta))
                comando.Resposta = request.Resposta;

            if (request.RespostasAleatorias != null)
                comando.RespostasAleatorias = request.RespostasAleatorias;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Comando social atualizado com sucesso." });
        }

        [HttpDelete("comandos-sociais/{comandoId}")]
        public async Task<IActionResult> DeleteComandoSocial(Guid comandoId)
        {
            var comando = await _context.ComandosSociais.FindAsync(comandoId);
            if (comando == null)
                return NotFound();

            _context.ComandosSociais.Remove(comando);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Comando social deletado com sucesso." });
        }

        // ========== ADMIN AGENDAMENTO MANAGEMENT ==========
        [HttpPut("agendamentos/{agendamentoId}")]
        public async Task<IActionResult> UpdateAgendamento(Guid agendamentoId, [FromBody] AdminUpdateAgendamentoRequest request)
        {
            var agendamento = await _context.Agendamentos.FindAsync(agendamentoId);
            if (agendamento == null)
                return NotFound();

            if (request.AgendadoPara.HasValue)
                agendamento.AgendadoPara = request.AgendadoPara.Value;

            if (!string.IsNullOrWhiteSpace(request.Comando))
                agendamento.Comando = request.Comando;

            if (request.Recorrencia != null)
                agendamento.Recorrencia = request.Recorrencia;

            if (request.Executado.HasValue)
                agendamento.Executado = request.Executado.Value;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Agendamento atualizado com sucesso." });
        }

        [HttpDelete("agendamentos/{agendamentoId}")]
        public async Task<IActionResult> DeleteAgendamento(Guid agendamentoId)
        {
            var agendamento = await _context.Agendamentos.FindAsync(agendamentoId);
            if (agendamento == null)
                return NotFound();

            _context.Agendamentos.Remove(agendamento);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Agendamento deletado com sucesso." });
        }

        [HttpGet("users-with-plans")]
        public async Task<IActionResult> GetUsersWithActivePlans()
        {
            var now = DateTimeOffset.UtcNow;
            
            // Buscar todas as assinaturas ativas com seus usuários
            var assinaturasAtivas = await _context.Assinaturas
                .Include(a => a.User)
                .Where(a => (a.Status == "ativa" || a.Status == "Ativa" || a.Status == "ATIVA") &&
                           (!a.ExpiraEm.HasValue || a.ExpiraEm.Value > now))
                .OrderByDescending(a => a.DataCriacao)
                .ToListAsync();

            // Criar uma entrada para cada assinatura ativa (um usuário pode ter múltiplas assinaturas)
            var result = assinaturasAtivas.Select(assinatura =>
            {
                var user = assinatura.User;

                // Determinar o tipo de plano baseado no valor
                string tipoPlano = assinatura.Valor switch
                {
                    10m => "Remove Ads",
                    5m => "StarkCoins Básico",
                    15m => "StarkCoins Intermediário",
                    25m => "StarkCoins Avançado",
                    50m => "StarkCoins Premium",
                    100m => "StarkCoins VIP",
                    _ => assinatura.TipoPlano ?? "Desconhecido"
                };

                return new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role,
                    starkCoinBalance = user.StarkCoins,
                    plano = tipoPlano,
                    valor = assinatura.Valor,
                    status = assinatura.Status,
                    expiraEm = assinatura.ExpiraEm,
                    assinaturaId = assinatura.Id,
                    dataCriacao = assinatura.DataCriacao
                };
            })
            .OrderByDescending(u => u.expiraEm ?? DateTimeOffset.MaxValue)
            .ThenByDescending(u => u.dataCriacao)
            .ToList();

            return Ok(result);
        }

        [HttpGet("starkcoins-vendas")]
        public async Task<IActionResult> GetStarkcoinsVendas()
        {
            // Buscar as últimas 30 vendas de StarkCoins (PagamentoAvulso com status "Pago")
            var vendas = await _context.PagamentosAvulsos
                .Include(p => p.User)
                .Where(p => p.Status == "Pago" || p.Status == "pago")
                .OrderByDescending(p => p.PagamentoConfirmadoEm ?? p.DataCriacao)
                .Take(30)
                .Select(p => new
                {
                    id = p.Id,
                    data = p.PagamentoConfirmadoEm ?? p.DataCriacao,
                    valor = p.Valor,
                    usuarioNome = p.User.Name,
                    usuarioEmail = p.User.Email,
                    status = p.Status
                })
                .ToListAsync();

            var result = vendas.Select(v => new
            {
                id = v.id,
                data = v.data,
                valor = v.valor,
                usuarioNome = v.usuarioNome,
                usuarioEmail = v.usuarioEmail,
                status = v.status
            }).ToList();

            // Calcular total apenas para vendas com status "Pago" (Concluído)
            var total = vendas
                .Where(v => v.status == "Pago" || v.status == "pago")
                .Sum(v => v.valor);

            return Ok(new
            {
                vendas = result,
                total = total
            });
        }

        [HttpDelete("starkcoins-vendas/{id}")]
        public async Task<IActionResult> DeleteStarkcoinsVenda(Guid id)
        {
            var venda = await _context.PagamentosAvulsos.FindAsync(id);
            if (venda == null)
                return NotFound("Venda não encontrada.");

            _context.PagamentosAvulsos.Remove(venda);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registro de venda deletado com sucesso." });
        }

        [HttpGet("pagamentos-falhas")]
        public async Task<IActionResult> GetPagamentosFalhas()
        {
            // Buscar pagamentos com status diferente de "Pago" (falhas)
            var pagamentosFalhas = await _context.PagamentosAvulsos
                .Include(p => p.User)
                .Where(p => p.Status != "Pago" && p.Status != "pago")
                .OrderByDescending(p => p.DataCriacao)
                .Select(p => new
                {
                    id = p.Id,
                    data = p.DataCriacao,
                    valor = p.Valor,
                    usuarioNome = p.User.Name,
                    usuarioEmail = p.User.Email,
                    status = p.Status,
                    codigoErro = p.StripeSessionId,
                    detalheErro = p.Status == "pendente" ? "Pagamento pendente - não foi concluído" :
                                  p.Status == "cancelado" || p.Status == "Cancelado" ? "Pagamento cancelado pelo usuário" :
                                  p.Status == "falhou" || p.Status == "Falhou" ? "Falha no processamento do pagamento" :
                                  $"Status: {p.Status}"
                })
                .ToListAsync();

            return Ok(pagamentosFalhas);
        }

        [HttpDelete("pagamentos-falhas/{id}")]
        public async Task<IActionResult> DeletePagamentoFalha(Guid id)
        {
            var pagamento = await _context.PagamentosAvulsos.FindAsync(id);
            if (pagamento == null)
                return NotFound("Pagamento não encontrado.");

            _context.PagamentosAvulsos.Remove(pagamento);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registro de pagamento deletado com sucesso." });
        }

        [HttpGet("error-logs/users")]
        public async Task<IActionResult> GetUsersWithErrorLogs()
        {
            // Buscar usuários que têm logs de erro (Soft ou App)
            var usersWithLogsSoft = await _context.ErrorLogsSoft
                .Include(e => e.User)
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    userName = g.First().User.Name,
                    userEmail = g.First().User.Email,
                    totalLogsSoft = g.Count(),
                    totalLogsApp = 0
                })
                .ToListAsync();

            var usersWithLogsApp = await _context.ErrorLogsApp
                .Include(e => e.User)
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    userName = g.First().User.Name,
                    userEmail = g.First().User.Email,
                    totalLogsSoft = 0,
                    totalLogsApp = g.Count()
                })
                .ToListAsync();

            // Combinar resultados
            var combined = usersWithLogsSoft
                .Concat(usersWithLogsApp)
                .GroupBy(u => u.userId)
                .Select(g => new
                {
                    userId = g.Key,
                    userName = g.First().userName,
                    userEmail = g.First().userEmail,
                    totalLogsSoft = g.Sum(u => u.totalLogsSoft),
                    totalLogsApp = g.Sum(u => u.totalLogsApp)
                })
                .Where(u => u.totalLogsSoft > 0 || u.totalLogsApp > 0)
                .ToList();

            return Ok(combined);
        }

        [HttpGet("error-logs/soft/{userId}")]
        public async Task<IActionResult> GetErrorLogsSoft(Guid userId)
        {
            var logs = await _context.ErrorLogsSoft
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.DataErro)
                .ThenByDescending(e => e.HoraErro)
                .Select(e => new
                {
                    id = e.Id,
                    ultimoComando = e.UltimoComando,
                    ultimaResposta = e.UltimaResposta,
                    ultimoDispositivoAcionado = e.UltimoDispositivoAcionado,
                    erroCompleto = e.ErroCompleto,
                    codigoDeErro = e.CodigoDeErro,
                    dataErro = e.DataErro,
                    horaErro = e.HoraErro,
                    acaoErro = e.AcaoErro,
                    createdAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("error-logs/app/{userId}")]
        public async Task<IActionResult> GetErrorLogsApp(Guid userId)
        {
            var logs = await _context.ErrorLogsApp
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.DataErro)
                .ThenByDescending(e => e.HoraErro)
                .Select(e => new
                {
                    id = e.Id,
                    ultimoComando = e.UltimoComando,
                    ultimaResposta = e.UltimaResposta,
                    ultimoDispositivoAcionado = e.UltimoDispositivoAcionado,
                    erroCompleto = e.ErroCompleto,
                    codigoDeErro = e.CodigoDeErro,
                    dataErro = e.DataErro,
                    horaErro = e.HoraErro,
                    acaoErro = e.AcaoErro,
                    createdAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpDelete("error-logs/soft/{id}")]
        public async Task<IActionResult> DeleteErrorLogSoft(int id)
        {
            var log = await _context.ErrorLogsSoft.FindAsync(id);
            if (log == null)
                return NotFound("Log não encontrado.");

            _context.ErrorLogsSoft.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Log deletado com sucesso." });
        }

        [HttpDelete("error-logs/app/{id}")]
        public async Task<IActionResult> DeleteErrorLogApp(int id)
        {
            var log = await _context.ErrorLogsApp.FindAsync(id);
            if (log == null)
                return NotFound("Log não encontrado.");

            _context.ErrorLogsApp.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Log deletado com sucesso." });
        }

        // ========== ADMIN APRENDIZADO IA MANAGEMENT ==========
        [HttpGet("aprendizados")]
        public async Task<IActionResult> GetAprendizados()
        {
            var aprendizados = await _context.Aprendizados
                .Include(a => a.Respostas)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return Ok(aprendizados);
        }

        [HttpGet("aprendizados/stats")]
        public async Task<IActionResult> GetAprendizadoStats()
        {
            var totalHitCount = await _context.Aprendizados.SumAsync(a => a.HitCount);
            
            // Estimativa de economia: cada HitCount economiza em média o que foi configurado
            var tokensEconomizados = totalHitCount * _telemetryOptions.DefaultTokensPerInteraction; 

            var stats = new
            {
                TotalItens = await _context.Aprendizados.CountAsync(),
                TotalGlobal = await _context.Aprendizados.CountAsync(a => a.Tipo == "Global"),
                TotalUsuario = await _context.Aprendizados.CountAsync(a => a.Tipo == "Usuario"),
                TotalContextual = await _context.Aprendizados.CountAsync(a => a.Tipo == "Contextual"),
                TotalQuarentena = await _context.Aprendizados.CountAsync(a => a.EmQuarentena),
                TotalInativos = await _context.Aprendizados.CountAsync(a => !a.Ativo),
                TotalHits = totalHitCount,
                TokensEconomizados = tokensEconomizados,
                EconomiaEstimadaDolar = (decimal)(tokensEconomizados / 1000.0) * _telemetryOptions.CostPer1KTokens
            };

            return Ok(stats);
        }

        [HttpGet("ia/telemetry/overview")]
        public async Task<IActionResult> GetTelemetryOverview()
        {
            var totalInteracoes = await _context.AiInteractionEvents.CountAsync();
            var totalHits = totalInteracoes > 0 
                ? await _context.AiInteractionEvents.CountAsync(x => x.Resultado != "CacheMiss")
                : 0;

            var stats = new
            {
                TotalInteracoes = totalInteracoes,
                CacheHitRate = totalInteracoes > 0 ? (double)totalHits / totalInteracoes * 100 : 0,
                CacheMissRate = totalInteracoes > 0 ? (double)(totalInteracoes - totalHits) / totalInteracoes * 100 : 0,
                TokensEconomizados = totalInteracoes > 0 ? await _context.AiInteractionEvents.SumAsync(x => x.TokensEstimadosEvitados) : 0,
                EconomiaUSD = totalInteracoes > 0 ? await _context.AiInteractionEvents.SumAsync(x => x.EconomiaUSD) : 0,
                LatenciaMediaMs = totalInteracoes > 0 ? await _context.AiInteractionEvents.AverageAsync(x => x.LatenciaMs) : 0
            };

            return Ok(stats);
        }

        [HttpGet("ia/telemetry/quality")]
        public async Task<IActionResult> GetTelemetryQuality()
        {
            var distribution = await _context.AiInteractionEvents
                .GroupBy(x => x.Resultado)
                .Select(g => new { Resultado = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(distribution);
        }

        [HttpGet("ia/telemetry/top-misses")]
        public async Task<IActionResult> GetTopMisses([FromQuery] int limit = 10)
        {
            var misses = await _context.AiInteractionEvents
                .Where(x => x.Resultado == "CacheMiss")
                .GroupBy(x => x.TextoNormalizado)
                .Select(g => new { Texto = g.Key, Ocorrencias = g.Count() })
                .OrderByDescending(x => x.Ocorrencias)
                .Take(limit)
                .ToListAsync();

            return Ok(misses);
        }

        [HttpGet("ia/telemetry/fuzzy-analytics")]
        public async Task<IActionResult> GetFuzzyAnalytics()
        {
            var stats = await _context.AiInteractionEvents
                .Where(x => x.SimilarityScore.HasValue)
                .GroupBy(x => x.Resultado)
                .Select(g => new { 
                    Resultado = g.Key, 
                    AverageScore = g.Average(x => x.SimilarityScore),
                    MinScore = g.Min(x => x.SimilarityScore),
                    MaxScore = g.Max(x => x.SimilarityScore)
                })
                .ToListAsync();

            return Ok(stats);
        }

        [HttpGet("ia/telemetry/roi-history")]
        public async Task<IActionResult> GetRoiHistory([FromQuery] int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);

            var roi = await _context.AiInteractionEvents
                .Where(x => x.CreatedAt >= startDate)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new { 
                    Date = g.Key, 
                    Economia = g.Sum(x => x.EconomiaUSD),
                    Hits = g.Count(x => x.Resultado != "CacheMiss")
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var byOrigin = await _context.AiInteractionEvents
                .Where(x => x.CreatedAt >= startDate)
                .GroupBy(x => x.Origem)
                .Select(g => new { 
                    Origem = g.Key, 
                    Economia = g.Sum(x => x.EconomiaUSD),
                    Interactions = g.Count()
                })
                .ToListAsync();

            return Ok(new { roi, byOrigin });
        }

        [HttpPost("aprendizados/{id}/promover")]
        public async Task<IActionResult> PromoverAprendizado(Guid id)
        {
            var item = await _context.Aprendizados.FindAsync(id);
            if (item == null) return NotFound();

            item.Tipo = "Global";
            item.UserId = null; // Desvincula para ser público
            item.Resposta = TextHelper.LimparGirias(item.Resposta);
            item.ConfidenceScore = Math.Max(item.ConfidenceScore, 80);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Aprendizado promovido a Global com sucesso." });
        }

        [HttpPost("aprendizados/{id}/rebaixar")]
        public async Task<IActionResult> RebaixarAprendizado(Guid id)
        {
            var item = await _context.Aprendizados.FindAsync(id);
            if (item == null) return NotFound();

            item.Tipo = "Usuario";
            // Se o admin rebaixa, ele assume a "paternidade" se não houver dono, ou apenas muda o tipo
            if (item.UserId == null)
            {
                item.UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Aprendizado rebaixado a Privado (Usuário)." });
        }

        [HttpPost("aprendizados/{id}/quarentena")]
        public async Task<IActionResult> ToggleQuarentena(Guid id)
        {
            var item = await _context.Aprendizados.FindAsync(id);
            if (item == null) return NotFound();

            item.EmQuarentena = !item.EmQuarentena;
            if (item.EmQuarentena)
            {
                item.QuarentenaDesde = DateTimeOffset.UtcNow;
                item.ConfidenceScore = Math.Min(item.ConfidenceScore, 20);
            }
            else
            {
                item.QuarentenaDesde = null;
                item.ConfidenceScore = Math.Max(item.ConfidenceScore, 50);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = item.EmQuarentena ? "Item movido para quarentena." : "Item removido da quarentena.", emQuarentena = item.EmQuarentena });
        }

        [HttpPost("aprendizados")]
        public async Task<IActionResult> CreateAprendizado([FromBody] AdminUpdateAprendizadoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Texto) || string.IsNullOrWhiteSpace(request.Resposta))
                return BadRequest("Texto e Resposta são obrigatórios.");

            var textoNormalizado = TextHelper.NormalizarTexto(request.Texto);

            // Evitar duplicatas manuais
            if (await _context.Aprendizados.AnyAsync(a => a.Texto == textoNormalizado))
                return BadRequest("Este comando já existe na base de aprendizado.");

            var tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "Global" : request.Tipo;

            var novo = new Aprendizado
            {
                Id = Guid.NewGuid(),
                Texto = textoNormalizado,
                Resposta = request.Resposta,
                Tipo = tipo,
                Contexto = request.Contexto,
                UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Se for Global, gerar variações
            if (tipo == "Global")
            {
                var variacoes = await _iaService.GerarVariacoesParaGlobal(request.Texto, request.Resposta);
                foreach (var v in variacoes)
                {
                    novo.Respostas.Add(new AprendizadoResposta
                    {
                        Id = Guid.NewGuid(),
                        Texto = v,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
                
                // Adiciona também a original como uma das variações para entrar no sorteio/rodízio
                novo.Respostas.Add(new AprendizadoResposta
                {
                    Id = Guid.NewGuid(),
                    Texto = request.Resposta,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            _context.Aprendizados.Add(novo);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Aprendizado criado com sucesso.", id = novo.Id, variacoesGeradas = novo.Respostas.Count });
        }

        [HttpPut("aprendizados/{id}")]
        public async Task<IActionResult> UpdateAprendizado(Guid id, [FromBody] AdminUpdateAprendizadoRequest request)
        {
            var aprendizado = await _context.Aprendizados.FindAsync(id);
            if (aprendizado == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Texto))
                aprendizado.Texto = TextHelper.NormalizarTexto(request.Texto);

            if (!string.IsNullOrWhiteSpace(request.Resposta))
                aprendizado.Resposta = request.Resposta;

            if (!string.IsNullOrWhiteSpace(request.Tipo))
                aprendizado.Tipo = request.Tipo;

            if (request.Ativo.HasValue)
                aprendizado.Ativo = request.Ativo.Value;
            
            if (request.EmQuarentena.HasValue)
            {
                aprendizado.EmQuarentena = request.EmQuarentena.Value;
                if (aprendizado.EmQuarentena && aprendizado.QuarentenaDesde == null)
                    aprendizado.QuarentenaDesde = DateTimeOffset.UtcNow;
                else if (!aprendizado.EmQuarentena)
                    aprendizado.QuarentenaDesde = null;
            }

            aprendizado.Contexto = request.Contexto;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Aprendizado atualizado com sucesso." });
        }

        [HttpDelete("aprendizados/{id}")]
        public async Task<IActionResult> DeleteAprendizado(Guid id)
        {
            var aprendizado = await _context.Aprendizados.FindAsync(id);
            if (aprendizado == null)
                return NotFound();

            _context.Aprendizados.Remove(aprendizado);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Aprendizado deletado com sucesso." });
        }

        [HttpPost("aprendizados/{id}/respostas")]
        public async Task<IActionResult> AddResposta(Guid id, [FromBody] AdminUpdateAprendizadoRespostaRequest request)
        {
            var aprendizado = await _context.Aprendizados.FindAsync(id);
            if (aprendizado == null) return NotFound();

            if (string.IsNullOrWhiteSpace(request.Texto))
                return BadRequest("O texto da variação é obrigatório.");

            var novaResposta = new AprendizadoResposta
            {
                Id = Guid.NewGuid(),
                AprendizadoId = id,
                Texto = request.Texto,
                UsoCount = 0, // Reset usage for new variation
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.AprendizadoRespostas.Add(novaResposta);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Variação adicionada com sucesso.", id = novaResposta.Id });
        }

        [HttpPut("aprendizados/respostas/{respostaId}")]
        public async Task<IActionResult> UpdateResposta(Guid respostaId, [FromBody] AdminUpdateAprendizadoRespostaRequest request)
        {
            var resposta = await _context.AprendizadoRespostas.FindAsync(respostaId);
            if (resposta == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Texto))
                resposta.Texto = request.Texto;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Variação atualizada com sucesso." });
        }

        [HttpDelete("aprendizados/respostas/{respostaId}")]
        public async Task<IActionResult> DeleteResposta(Guid respostaId)
        {
            var resposta = await _context.AprendizadoRespostas.FindAsync(respostaId);
            if (resposta == null) return NotFound();

            // Garantir que não deletamos a única resposta se for Global (opcional, mas boa prática)
            // Varificar se pertencente a um aprendizado Global que deve ter pelo menos 1? 
            // Para simplificar, permitimos deletar, mas o sistema pode ficar sem variações se deletar todas.
            
            _context.AprendizadoRespostas.Remove(resposta);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Variação deletada com sucesso." });
        }
    }

    public class AdminUpdateAprendizadoRequest
    {
        public string? Texto { get; set; }
        public string? Resposta { get; set; }
        public string? Tipo { get; set; }
        public string? Contexto { get; set; }
        public bool? Ativo { get; set; }
        public bool? EmQuarentena { get; set; }
    }

    public class AdminCreateComandoSocialRequest
    {
        public Guid UserId { get; set; }
        public string Comando { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public string? RespostasAleatorias { get; set; }
    }

    public class AdminUpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int? StarkCoinBalance { get; set; }
        public string? RemovalAds { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
        public string? Bairro { get; set; }
    }

    public class AdminUpdateDeviceRequest
    {
        public string? Name { get; set; }
        public string? Comando { get; set; }
    }

    public class AdminUpdateComandoSocialRequest
    {
        public string? Comando { get; set; }
        public string? Resposta { get; set; }
        public string? RespostasAleatorias { get; set; }
    }

    public class AdminUpdateAgendamentoRequest
    {
        public DateTimeOffset? AgendadoPara { get; set; }
        public string? Comando { get; set; }
        public string? Recorrencia { get; set; }
        public bool? Executado { get; set; }
    }

    public class AdminUpdateAprendizadoRespostaRequest
    {
        public string Texto { get; set; } = string.Empty;
    }
}
