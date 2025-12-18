using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Devices;
using StarkAid.Api.DTOs.V1.Admin;
using System.Security.Claims;

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

        public AdminController(AppDbContext context, IMqttClientService mqttService)
        {
            _context = context;
            _mqttService = mqttService;
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
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
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

            // Buscar última sessão ativa para obter último form/activity
            var ultimaSessao = await _context.UserSessions
                .Where(s => s.UserId == id && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();

            // Combinar dados de soft e app (priorizar o mais recente baseado em LastUpdatedAt)
            var ultimoComandoEsp = (activitySoft?.LastUpdatedAt ?? DateTimeOffset.MinValue) >= (activityApp?.LastUpdatedAt ?? DateTimeOffset.MinValue)
                ? activitySoft?.UltimoComandoEsp ?? activityApp?.UltimoComandoEsp
                : activityApp?.UltimoComandoEsp ?? activitySoft?.UltimoComandoEsp;
            
            var ultimoComandoEwelink = (activitySoft?.LastUpdatedAt ?? DateTimeOffset.MinValue) >= (activityApp?.LastUpdatedAt ?? DateTimeOffset.MinValue)
                ? activitySoft?.UltimoComandoEwelink ?? activityApp?.UltimoComandoEwelink
                : activityApp?.UltimoComandoEwelink ?? activitySoft?.UltimoComandoEwelink;
            
            var ultimoComandoStarkSwitch = (activitySoft?.LastUpdatedAt ?? DateTimeOffset.MinValue) >= (activityApp?.LastUpdatedAt ?? DateTimeOffset.MinValue)
                ? activitySoft?.UltimoComandoStarkSwitch ?? activityApp?.UltimoComandoStarkSwitch
                : activityApp?.UltimoComandoStarkSwitch ?? activitySoft?.UltimoComandoStarkSwitch;
            
            // Para social e IA, usar do histórico se não tiver na activity
            var ultimoComandoSocial = activitySoft?.UltimoComandoSocial ?? activityApp?.UltimoComandoSocial ?? ultimoComandoSocialHistorico?.Comando;
            var ultimaRespostaSocial = activitySoft?.UltimaRespostaSocial ?? activityApp?.UltimaRespostaSocial ?? ultimoComandoSocialHistorico?.Resposta;
            var ultimoComandoIA = activitySoft?.UltimoComandoIA ?? activityApp?.UltimoComandoIA ?? ultimoComandoIAHistorico?.TextoUsuario;
            var ultimaRespostaIA = activitySoft?.UltimaRespostaIA ?? activityApp?.UltimaRespostaIA ?? ultimoComandoIAHistorico?.TextoIa;

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
                UltimoFormAcessado = ultimaSessao?.SessionName ?? "Nenhum form acessado",
                UltimaActivityAcessada = ultimaSessao?.LastActivityAt
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
    }

    public class UpdateUserRequest
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
}
