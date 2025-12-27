using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Ewelink;
using StarkAid.Api.Services.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class EwelinkController : ControllerBase
    {
        private readonly IEwelinkService _ewelinkService;
        private readonly AppDbContext _context;

        public EwelinkController(IEwelinkService ewelinkService, AppDbContext context)
        {
            _ewelinkService = ewelinkService;
            _context = context;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Usuário não autenticado.");
            return Guid.Parse(userIdClaim);
        }

        // Endpoint público para callback OAuth (sem autenticação)
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Code não enviado.");

            var token = await _ewelinkService.TrocarCodePorTokenAsync(code);
            return Ok(new { message = "Code recebido e token gerado.", token });
        }

        // Endpoint autenticado para fazer login e salvar tokens (OAuth)
        [Authorize]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EwelinkLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                    return BadRequest(new { message = "Code não enviado." });

                var userId = GetUserId();
                // Usar região do request se disponível, senão padrão "as"
                var region = request.Region ?? "as";
                var tokenResult = await _ewelinkService.TrocarCodePorTokenAsync(request.Code, region);
                
                if (tokenResult == null)
                    return BadRequest(new { message = "Erro ao obter token. Resposta vazia do Ewelink." });

                var tokenObj = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(tokenResult));
                
                // Verificar se há erro na resposta
                if (tokenObj?.error != null && tokenObj.error != 0)
                {
                    var errorMsg = tokenObj.msg?.ToString() ?? "Erro desconhecido do Ewelink";
                    return BadRequest(new { message = $"Erro do Ewelink: {errorMsg}", error = tokenObj.error });
                }

                // A resposta pode ter os tokens diretamente ou em 'data'
                var data = tokenObj?.data ?? tokenObj;
                
                // Ewelink retorna accessToken/refreshToken (não at/rt)
                var accessToken = data?.accessToken ?? data?.at;
                var refreshToken = data?.refreshToken ?? data?.rt;
                
                if (accessToken == null || refreshToken == null)
                {
                    // Log para debug
                    var responseStr = JsonConvert.SerializeObject(tokenObj);
                    return BadRequest(new { message = "Dados de token inválidos na resposta do Ewelink.", details = responseStr });
                }

                // Calcular expiração se não estiver presente
                // Ewelink retorna atExpiredTime/rtExpiredTime (não atExpiredAt/rtExpiredAt)
                long atExpiredAt = 0;
                long rtExpiredAt = 0;
                if (data.atExpiredTime != null)
                    atExpiredAt = (long)data.atExpiredTime;
                else if (data.atExpiredAt != null)
                    atExpiredAt = (long)data.atExpiredAt;
                else
                    atExpiredAt = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds();

                if (data.rtExpiredTime != null)
                    rtExpiredAt = (long)data.rtExpiredTime;
                else if (data.rtExpiredAt != null)
                    rtExpiredAt = (long)data.rtExpiredAt;
                else
                    rtExpiredAt = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeMilliseconds();

                // Salvar tokens no banco
                var account = await _ewelinkService.SaveOrUpdateAccountAsync(
                    userId,
                    accessToken?.ToString() ?? "",
                    refreshToken?.ToString() ?? "",
                    atExpiredAt,
                    rtExpiredAt,
                    region // Usar a região que foi passada no request
                );

                // Buscar e salvar dispositivos
                await SyncDevicesAsync(userId, accessToken?.ToString() ?? "", region);

                return Ok(new { message = "Login realizado com sucesso.", redirectTo = "/ewelink" });

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao processar login: {ex.Message}", details = ex.ToString() });
            }
        }

        // Endpoint autenticado para fazer login direto com email e senha
        [Authorize]
        [HttpPost("login-direto")]
        public async Task<IActionResult> LoginDireto([FromBody] EwelinkDirectLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                    return BadRequest(new { message = "Email e senha são obrigatórios." });

                var userId = GetUserId();
                var loginResult = await _ewelinkService.LoginDiretoAsync(
                    request.Email, 
                    request.Password, 
                    request.AreaCode ?? "+55"
                );
                
                if (loginResult == null)
                    return BadRequest(new { message = "Erro ao fazer login. Resposta vazia do Ewelink." });

                var loginObj = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(loginResult));
                
                // Verificar se há erro na resposta
                if (loginObj?.error != null && loginObj.error != 0)
                {
                    var errorMsg = loginObj.msg?.ToString() ?? "Erro desconhecido do Ewelink";
                    return BadRequest(new { message = $"Erro do Ewelink: {errorMsg}", error = loginObj.error });
                }

                // A resposta do login direto pode ter os tokens em 'data' ou diretamente no objeto
                var data = loginObj?.data ?? loginObj;
                if (data?.at == null || data?.rt == null)
                {
                    // Log para debug
                    var responseStr = JsonConvert.SerializeObject(loginObj);
                    return BadRequest(new { message = "Dados de token inválidos na resposta do Ewelink.", details = responseStr });
                }

                // Calcular expiração (30 dias para access token, padrão do Ewelink)
                long atExpiredAt = 0;
                long rtExpiredAt = 0;
                if (data.atExpiredAt != null)
                    atExpiredAt = (long)data.atExpiredAt;
                else
                    atExpiredAt = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds();

                if (data.rtExpiredAt != null)
                    rtExpiredAt = (long)data.rtExpiredAt;
                else
                    rtExpiredAt = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeMilliseconds();

                // Salvar tokens no banco
                var account = await _ewelinkService.SaveOrUpdateAccountAsync(
                    userId,
                    data.at?.ToString() ?? "",
                    data.rt?.ToString() ?? "",
                    atExpiredAt,
                    rtExpiredAt,
                    data.region?.ToString()
                );

                // Buscar e salvar dispositivos (login direto não retorna região, usar padrão "as")
                await SyncDevicesAsync(userId, data.at?.ToString() ?? "", "as");

                return Ok(new { message = "Login realizado com sucesso.", account });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao processar login: {ex.Message}", details = ex.ToString() });
            }
        }

        // Endpoint para verificar se está logado
        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var userId = GetUserId();
                
                var account = await _ewelinkService.GetAccountByUserIdAsync(userId);
                
                if (account == null)
                    return Ok(new { isLoggedIn = false });

                // Tentar refresh do token apenas se a conta existir
                await _ewelinkService.RefreshAccountTokenIfNeededAsync(userId);
                
                // Buscar conta atualizada após refresh
                account = await _ewelinkService.GetAccountByUserIdAsync(userId);

                return Ok(new { isLoggedIn = true, account });
            }
            catch (Exception ex)
            {
                // Log do erro e retornar status não logado
                Console.WriteLine($"Erro ao verificar status Ewelink: {ex.Message}");
                return Ok(new { isLoggedIn = false, error = ex.Message });
            }
        }

        // Endpoint para fazer logout (desconectar conta Ewelink)
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            var account = await _ewelinkService.GetAccountByUserIdAsync(userId);
            
            if (account == null)
                return Ok(new { message = "Usuário não está logado no Ewelink." });

            // Desativar a conta (soft delete)
            account.IsActive = false;
            account.LastUpdatedAt = DateTimeOffset.UtcNow;
            
            // Opcional: remover dispositivos também
            // var devices = await _context.EwelinkDevices.Where(d => d.UserId == userId).ToListAsync();
            // _context.EwelinkDevices.RemoveRange(devices);
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Logout realizado com sucesso." });
        }

        // Endpoint para listar dispositivos do usuário
        [Authorize]
        [HttpGet("dispositivos")]
        public async Task<IActionResult> GetDispositivos()
        {
            var userId = GetUserId();
            
            // O refresh é feito automaticamente dentro de GetUserDevicesAsync
            var devices = await _ewelinkService.GetUserDevicesAsync(userId);
            return Ok(devices);
        }

        // Endpoint para obter status de um dispositivo específico
        [Authorize]
        [HttpGet("dispositivos/{deviceId}/status")]
        public async Task<IActionResult> GetDeviceStatus(string deviceId)
        {
            var userId = GetUserId();
            
            // Refresh automático do token antes de obter status
            await _ewelinkService.RefreshAccountTokenIfNeededAsync(userId);
            
            var device = await _ewelinkService.GetDeviceStatusAsync(userId, deviceId);
            
            if (device == null)
                return NotFound("Dispositivo não encontrado.");

            return Ok(device);
        }

        // Endpoint para controlar dispositivo (ligar/desligar)
        [Authorize]
        [HttpPost("dispositivos/{deviceId}/controlar")]
        public async Task<IActionResult> ControlarDispositivo(string deviceId, [FromBody] EwelinkControlDeviceRequest request)
        {
            try
            {
                Console.WriteLine($"[CONTROLAR DISPOSITIVO] Iniciando - DeviceId: {deviceId}");
                
                if (string.IsNullOrEmpty(deviceId))
                {
                    Console.WriteLine("[CONTROLAR DISPOSITIVO] DeviceId vazio");
                    return BadRequest(new { message = "DeviceId não enviado." });
                }

                // Se request for null, tentar ler do body manualmente
                if (request == null)
                {
                    Console.WriteLine("[CONTROLAR DISPOSITIVO] Request body é null, tentando ler manualmente...");
                    var body = await new StreamReader(Request.Body).ReadToEndAsync();
                    Console.WriteLine($"[CONTROLAR DISPOSITIVO] Body raw: {body}");
                    request = JsonConvert.DeserializeObject<EwelinkControlDeviceRequest>(body);
                    
                    if (request == null)
                    {
                        Console.WriteLine("[CONTROLAR DISPOSITIVO] Não foi possível deserializar o request body");
                        return BadRequest(new { message = "Request body inválido." });
                    }
                }

                Console.WriteLine($"[CONTROLAR DISPOSITIVO] DeviceId: {deviceId}, Switch: {request.Switch}");

                var userId = GetUserId();
                Console.WriteLine($"[CONTROLAR DISPOSITIVO] UserId: {userId}");
                
                // Refresh automático do token antes de controlar
                await _ewelinkService.RefreshAccountTokenIfNeededAsync(userId);
                
                var success = await _ewelinkService.ControlDeviceAsync(userId, deviceId, request.Switch);

                if (!success)
                {
                    Console.WriteLine($"[CONTROLAR DISPOSITIVO] Falha ao controlar dispositivo {deviceId}");
                    return BadRequest(new { message = "Erro ao controlar dispositivo." });
                }

                Console.WriteLine($"[CONTROLAR DISPOSITIVO] Sucesso ao controlar dispositivo {deviceId}");

                // Retornar status atualizado
                var device = await _ewelinkService.GetDeviceStatusAsync(userId, deviceId);
                return Ok(device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONTROLAR DISPOSITIVO] Erro: {ex.Message}");
                Console.WriteLine($"[CONTROLAR DISPOSITIVO] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[CONTROLAR DISPOSITIVO] Inner exception: {ex.InnerException.Message}");
                }
                return BadRequest(new { message = $"Erro ao controlar dispositivo: {ex.Message}" });
            }
        }

        // Endpoint para sincronizar dispositivos
        [Authorize]
        [HttpPost("sincronizar")]
        public async Task<IActionResult> SincronizarDispositivos()
        {
            var userId = GetUserId();
            var account = await _ewelinkService.GetAccountByUserIdAsync(userId);
            
            if (account == null)
                return BadRequest("Usuário não está logado no Ewelink.");

            await _ewelinkService.RefreshAccountTokenIfNeededAsync(userId);
            account = await _ewelinkService.GetAccountByUserIdAsync(userId);
            if (account == null)
                return BadRequest("Erro ao atualizar token.");

            // Buscar região da conta ou usar padrão "as"
            var accountRegion = account.Region ?? "as";
            await SyncDevicesAsync(userId, account.AccessToken, accountRegion);
            var devices = await _ewelinkService.GetUserDevicesAsync(userId);
            
            return Ok(new { message = "Dispositivos sincronizados com sucesso.", devices });
        }

        // Método auxiliar para sincronizar dispositivos
        private async Task SyncDevicesAsync(Guid userId, string accessToken, string region = "as")
        {
            try
            {
                Console.WriteLine($"[SYNC DEVICES] Iniciando sincronização para usuário {userId}, região: {region}");
                
                var familias = await _ewelinkService.ListarFamiliasAsync(accessToken, region);
                if (familias == null)
                {
                    Console.WriteLine("[SYNC DEVICES] Nenhuma família retornada da API");
                    return;
                }

                var familiasJson = JsonConvert.SerializeObject(familias);
                Console.WriteLine($"[SYNC DEVICES] Resposta de famílias: {familiasJson}");
                
                var familiasObj = JsonConvert.DeserializeObject<dynamic>(familiasJson);
                
                // A resposta pode ter os dados diretamente ou em 'data'
                var familiasList = familiasObj?.data?.familyList ?? familiasObj?.familyList;
                
                if (familiasList == null)
                {
                    Console.WriteLine("[SYNC DEVICES] familyList é null ou vazio");
                    Console.WriteLine($"[SYNC DEVICES] Estrutura recebida: {JsonConvert.SerializeObject(familiasObj)}");
                    return;
                }

                var devicesToSave = new List<StarkAid.Api.Entities.EwelinkDevice>();
                int familiaCount = 0;
                int deviceCount = 0;

                foreach (var familia in familiasList)
                {
                    familiaCount++;
                    var familyId = familia.id?.ToString();
                    Console.WriteLine($"[SYNC DEVICES] Processando família {familiaCount}: {familyId}");
                    
                    var dispositivos = await _ewelinkService.ListarDispositivosAsync(accessToken, familyId, region);
                    if (dispositivos == null)
                    {
                        Console.WriteLine($"[SYNC DEVICES] Nenhum dispositivo retornado para família {familyId}");
                        continue;
                    }

                    var dispositivosJson = JsonConvert.SerializeObject(dispositivos);
                    Console.WriteLine($"[SYNC DEVICES] Resposta de dispositivos para família {familyId}: {dispositivosJson}");
                    
                    var dispositivosObj = JsonConvert.DeserializeObject<dynamic>(dispositivosJson);
                    
                    // A resposta pode ter os dados diretamente ou em 'data'
                    var dispositivosList = dispositivosObj?.data?.thingList ?? dispositivosObj?.thingList;
                    
                    if (dispositivosList == null)
                    {
                        Console.WriteLine($"[SYNC DEVICES] thingList é null ou vazio para família {familyId}");
                        Console.WriteLine($"[SYNC DEVICES] Estrutura recebida: {JsonConvert.SerializeObject(dispositivosObj)}");
                        continue;
                    }

                    foreach (var dev in dispositivosList)
                    {
                        var itemData = dev.itemData;
                        if (itemData?.deviceid == null)
                        {
                            Console.WriteLine("[SYNC DEVICES] Dispositivo sem deviceid, pulando...");
                            continue;
                        }

                        var device = new StarkAid.Api.Entities.EwelinkDevice
                        {
                            UserId = userId,
                            DeviceId = itemData.deviceid.ToString(),
                            Name = itemData.name?.ToString() ?? "Dispositivo sem nome",
                            Type = (int)(itemData.type ?? 0),
                            Uiid = (int)(itemData.uiid ?? 0),
                            Params = JsonConvert.SerializeObject(itemData.@params),
                            Online = itemData.online == true,
                            FamilyId = familyId,
                            RoomId = itemData.roomid?.ToString()
                        };

                        Console.WriteLine($"[SYNC DEVICES] Adicionando dispositivo: {device.Name} (ID: {device.DeviceId})");
                        devicesToSave.Add(device);
                        deviceCount++;
                    }
                }

                Console.WriteLine($"[SYNC DEVICES] Total de {deviceCount} dispositivos encontrados em {familiaCount} famílias");

                if (devicesToSave.Any())
                {
                    Console.WriteLine($"[SYNC DEVICES] Salvando {devicesToSave.Count} dispositivos no banco...");
                    await _ewelinkService.SaveOrUpdateDevicesAsync(userId, devicesToSave);
                    Console.WriteLine($"[SYNC DEVICES] Dispositivos salvos com sucesso!");
                }
                else
                {
                    Console.WriteLine("[SYNC DEVICES] Nenhum dispositivo para salvar");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request
                Console.WriteLine($"[SYNC DEVICES] Erro ao sincronizar dispositivos: {ex.Message}");
                Console.WriteLine($"[SYNC DEVICES] Stack trace: {ex.StackTrace}");
            }
        }

        // Endpoints legados (mantidos para compatibilidade)
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] EwelinkRefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest("Refresh token não enviado.");

            var result = await _ewelinkService.RefreshTokenAsync(request.RefreshToken);
            return Ok(result);
        }

        [HttpGet("familias")]
        public async Task<IActionResult> GetFamilias([FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return BadRequest("Token de acesso não enviado.");

            var token = authorization.Replace("Bearer ", "");
            var result = await _ewelinkService.ListarFamiliasAsync(token);
            return Ok(result);
        }

        [HttpGet("dispositivos-legacy")]
        public async Task<IActionResult> GetDispositivosLegacy([FromHeader] string authorization, [FromQuery] string familyId)
        {
            if (string.IsNullOrEmpty(authorization) || string.IsNullOrEmpty(familyId))
                return BadRequest("Token de acesso ou familyId não enviado.");

            var token = authorization.Replace("Bearer ", "");
            var result = await _ewelinkService.ListarDispositivosAsync(token, familyId);
            return Ok(result);
        }

        [HttpPost("dispositivos/controlar-legacy")]
        public async Task<IActionResult> ControlarDispositivoLegacy(
            [FromHeader] string authorization,
            [FromBody] EwelinkDeviceControlRequest request)
        {
            if (string.IsNullOrEmpty(authorization) || string.IsNullOrEmpty(request.DeviceId))
                return BadRequest("Token de acesso ou deviceId não enviado.");

            var token = authorization.Replace("Bearer ", "");
            var result = await _ewelinkService.ControlarDispositivoAsync(token, request.DeviceId, request.Parameters);
            return Ok(result);
        }
    }
}