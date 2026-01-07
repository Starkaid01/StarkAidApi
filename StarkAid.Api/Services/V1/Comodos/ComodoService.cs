using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Comodos;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1;
using StarkAid.Api.Services.V1.Devices;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.Hubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace StarkAid.Api.Services.V1.Comodos
{
    public class ComodoService : IComodoService
    {
        private readonly AppDbContext _context;
        private readonly IEscopoConversacionalService _escopoService;
        private readonly IMqttClientService _mqttService;
        private readonly IEwelinkService _ewelinkService;
        private readonly IDeviceService _deviceService;
        private readonly IHubContext<DispositivoEspHub> _hubContext;
        private readonly ILogger<ComodoService> _logger;

        public ComodoService(
            AppDbContext context,
            IEscopoConversacionalService escopoService,
            IMqttClientService mqttService,
            IEwelinkService ewelinkService,
            IDeviceService deviceService,
            IHubContext<DispositivoEspHub> hubContext,
            ILogger<ComodoService> logger)
        {
            _context = context;
            _escopoService = escopoService;
            _mqttService = mqttService;
            _ewelinkService = ewelinkService;
            _deviceService = deviceService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // --- CRUD Operations ---

        public async Task<List<ComodoDto>> GetAllAsync(Guid userId)
        {
            var comodos = await _context.Comodos
                .Include(c => c.Dispositivos)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var dtos = new List<ComodoDto>();

            // Fetch names and states from all tables
            var devicesMqtt = await _context.Devices.Where(u => u.UserId == userId).ToListAsync();
            var devicesEsp = await _context.DispositivosEsp.Where(u => u.UserId == userId).ToListAsync();
            var devicesEwelink = await _context.EwelinkDevices.Where(u => u.UserId == userId).ToListAsync();

            var nameMap = new Dictionary<string, string>();
            var stateMap = new Dictionary<string, bool>();

            foreach(var d in devicesMqtt) { nameMap[d.Id.ToString()] = d.Name; stateMap[d.Id.ToString()] = d.IsOn; }
            foreach(var d in devicesEsp) { nameMap[d.Id.ToString()] = d.Nome; stateMap[d.Id.ToString()] = d.LigadoDesligado; }
            foreach(var d in devicesEwelink) 
            { 
                nameMap[d.DeviceId] = d.Name; 
                bool isOn = false;
                if (!string.IsNullOrEmpty(d.Params))
                {
                    try {
                        var j = JObject.Parse(d.Params);
                        isOn = j["switch"]?.ToString() == "on";
                    } catch {}
                }
                stateMap[d.DeviceId] = isOn; 
            }

            foreach (var c in comodos)
            {
                var dto = new ComodoDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Dispositivos = c.Dispositivos.Select(d => new ComodoDispositivoDto
                    {
                        DispositivoId = d.DispositivoId,
                        Tipo = d.Tipo,
                        Papel = d.Papel,
                        NomeDispositivo = nameMap.ContainsKey(d.DispositivoId) ? nameMap[d.DispositivoId] : "Desconhecido",
                        IsOn = stateMap.ContainsKey(d.DispositivoId) ? stateMap[d.DispositivoId] : false
                    }).ToList()
                };
                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<ComodoDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var c = await _context.Comodos
                .Include(x => x.Dispositivos)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (c == null) return null;

            // Simple map (could be improved)
            var names = new Dictionary<string, string>(); 
            // Load names just for these devices? Or Lazy?
            // For now simple return "Carregando..." or fetch.
            // Let's fetch properly.
            var ids = c.Dispositivos.Select(d => d.DispositivoId).ToList();
            
            var devicesMqtt = await _context.Devices.Where(u => u.UserId == userId).ToListAsync();
            var devicesEsp = await _context.DispositivosEsp.Where(u => u.UserId == userId).ToListAsync();
            var devicesEwelink = await _context.EwelinkDevices.Where(u => u.UserId == userId).ToListAsync();

            var nameMap = new Dictionary<string, string>();
            var stateMap = new Dictionary<string, bool>();

            foreach(var d in devicesMqtt) { nameMap[d.Id.ToString()] = d.Name; stateMap[d.Id.ToString()] = d.IsOn; }
            foreach(var d in devicesEsp) { nameMap[d.Id.ToString()] = d.Nome; stateMap[d.Id.ToString()] = d.LigadoDesligado; }
            foreach(var d in devicesEwelink) 
            { 
                nameMap[d.DeviceId] = d.Name; 
                bool isOn = false;
                if (!string.IsNullOrEmpty(d.Params))
                {
                    try {
                        var j = JObject.Parse(d.Params);
                        isOn = j["switch"]?.ToString() == "on";
                    } catch {}
                }
                stateMap[d.DeviceId] = isOn; 
            }

            return new ComodoDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Dispositivos = c.Dispositivos.Select(d => new ComodoDispositivoDto
                {
                    DispositivoId = d.DispositivoId,
                    Tipo = d.Tipo,
                    Papel = d.Papel,
                    NomeDispositivo = nameMap.ContainsKey(d.DispositivoId) ? nameMap[d.DispositivoId] : "Desconhecido",
                    IsOn = stateMap.ContainsKey(d.DispositivoId) ? stateMap[d.DispositivoId] : false
                }).ToList()
            };
        }

        public async Task<bool> ToggleDeviceAsync(Guid userId, string dispositivoId, string tipo)
        {
            // 1. Get Current State
            bool currentState = false;
            
            if (string.Equals(tipo, "Device", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(dispositivoId, out var gId))
                {
                    var d = await _context.Devices.FindAsync(gId);
                    if (d != null) currentState = d.IsOn;
                    else return false;
                }
            }
            else if (string.Equals(tipo, "Ewelink", StringComparison.OrdinalIgnoreCase))
            {
                var status = await _ewelinkService.GetDeviceStatusAsync(userId, dispositivoId);
                if (status != null) currentState = status.IsOn;
                else return false;
            }
            else if (string.Equals(tipo, "Esp", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(dispositivoId, out var gId))
                {
                    var esp = await _context.DispositivosEsp.FindAsync(gId);
                    if (esp != null) currentState = esp.LigadoDesligado;
                    else return false;
                }
            }

            // 2. Execute Toggle
            var candidate = new DeviceCandidate { Id = dispositivoId, Type = tipo };
            await ExecuteDevices(userId, new List<DeviceCandidate> { candidate }, !currentState);
            
            return true;
        }

        public async Task<ComodoDto> CreateAsync(Guid userId, CreateComodoRequest request)
        {
            var comodo = new Comodo
            {
                UserId = userId,
                Nome = request.Nome
            };
            _context.Comodos.Add(comodo);
            await _context.SaveChangesAsync();
            
            return new ComodoDto { Id = comodo.Id, Nome = comodo.Nome };
        }

        public async Task<ComodoDto?> UpdateAsync(Guid id, Guid userId, UpdateComodoRequest request)
        {
            var comodo = await _context.Comodos.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (comodo == null) return null;

            comodo.Nome = request.Nome;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id, userId);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var comodo = await _context.Comodos.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (comodo == null) return false;

            _context.Comodos.Remove(comodo);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Device Association ---

        public async Task<bool> AddDeviceAsync(Guid comodoId, Guid userId, AssociateDeviceRequest request)
        {
            var comodo = await _context.Comodos.FirstOrDefaultAsync(c => c.Id == comodoId && c.UserId == userId);
            if (comodo == null) return false;

            var exists = await _context.ComodoDispositivos
                .AnyAsync(cd => cd.ComodoId == comodoId && cd.DispositivoId == request.DispositivoId);

            if (exists) return true; // Already added

            var cd = new ComodoDispositivo
            {
                ComodoId = comodoId,
                DispositivoId = request.DispositivoId,
                Tipo = request.Tipo,
                Papel = request.Papel
            };
            _context.ComodoDispositivos.Add(cd);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveDeviceAsync(Guid comodoId, string dispositivoId, Guid userId)
        {
            var comodo = await _context.Comodos.FirstOrDefaultAsync(c => c.Id == comodoId && c.UserId == userId);
            if (comodo == null) return false;

            var cd = await _context.ComodoDispositivos
                .FirstOrDefaultAsync(x => x.ComodoId == comodoId && x.DispositivoId == dispositivoId);

            if (cd == null) return false;

            _context.ComodoDispositivos.Remove(cd);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Command Resolution ---

        public async Task<ComandoAmbienteResult> ResolverComandoAmbienteAsync(Guid userId, string tipoDispositivo, string? originalCommand, string? comodoNomeConfirmado = null)
        {
            var tipoLower = tipoDispositivo.ToLower();
            var commandLower = (originalCommand ?? string.Empty).ToLower();
            bool turnOn = !commandLower.Contains("apaga") && !commandLower.Contains("desliga");
            string acaoVoz = turnOn ? "ligar" : "desligar";

            // 1. Identify Candidate Devices
            var candidates = new List<DeviceCandidate>();

            var associatedDevices = await _context.ComodoDispositivos
                .Include(cd => cd.Comodo)
                .Where(cd => cd.Comodo!.UserId == userId)
                .ToListAsync();

            // Filter by Role (Papel) or Name
            var typeCandidates = associatedDevices
                .Where(cd => !string.IsNullOrEmpty(cd.Papel) && 
                      (cd.Papel.ToLower().Contains(tipoLower) || tipoLower.Contains(cd.Papel.ToLower())))
                .ToList();

            candidates.AddRange(typeCandidates.Select(cd => new DeviceCandidate 
            { 
                Id = cd.DispositivoId, 
                Type = cd.Tipo, 
                ComodoId = cd.ComodoId,
                ComodoNome = cd.Comodo!.Nome 
            }));

            // Also check generic devices NOT explicitly associated but containing the type in Name
            var mqttMatches = await _context.Devices
                .Where(d => d.UserId == userId && d.Name.ToLower().Contains(tipoLower))
                .ToListAsync();
            
            foreach (var d in mqttMatches)
            {
                if (!candidates.Any(c => c.Id == d.Id.ToString()))
                {
                    candidates.Add(new DeviceCandidate { Id = d.Id.ToString(), Type = "Device", Name = d.Name });
                }
            }

            var ewelinkMatches = await _context.EwelinkDevices
                .Where(d => d.UserId == userId && d.Name.ToLower().Contains(tipoLower))
                .ToListAsync();

            foreach (var d in ewelinkMatches)
            {
                if (!candidates.Any(c => c.Id == d.DeviceId))
                {
                    candidates.Add(new DeviceCandidate { Id = d.DeviceId, Type = "Ewelink", Name = d.Name });
                }
            }
            
            // 2. Logic Flow
            
            // Case A: User confirmed a room
            if (!string.IsNullOrEmpty(comodoNomeConfirmado))
            {
                var normalizedInput = NormalizeComodoName(comodoNomeConfirmado);
                
                var targetComodo = await _context.Comodos
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                
                var matched = targetComodo.FirstOrDefault(c => 
                    NormalizeComodoName(c.Nome).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                    c.Nome.Equals(comodoNomeConfirmado, StringComparison.OrdinalIgnoreCase));

                if (matched == null)
                {
                    return new ComandoAmbienteResult { Sucesso = false, MensagemVoz = "Não encontrei esse cômodo." };
                }

                var validInRoom = candidates.Where(c => c.ComodoId == matched.Id).ToList();
                if (!validInRoom.Any())
                {
                    return new ComandoAmbienteResult { Sucesso = false, MensagemVoz = $"Não encontrei {tipoDispositivo} em {matched.Nome}." };
                }

                var feedback = await ExecuteDevices(userId, validInRoom, turnOn);
                await _escopoService.CriarOuRenovarEscopoAsync(userId, matched.Id);
                
                return new ComandoAmbienteResult { Sucesso = true, MensagemVoz = feedback, DispositivosAcionados = validInRoom.Select(x => Guid.TryParse(x.Id, out var g) ? g : Guid.Empty).ToList() };
            }

            // Case B: Initial Command
            if (!candidates.Any())
            {
                 return new ComandoAmbienteResult { Sucesso = false, MensagemVoz = $"Não encontrei nenhum dispositivo do tipo {tipoDispositivo} configurado." };
            }

            if (candidates.Count == 1)
            {
                var dev = candidates.First();
                var feedback = await ExecuteDevices(userId, new List<DeviceCandidate> { dev }, turnOn);
                
                if (dev.ComodoId.HasValue)
                     await _escopoService.CriarOuRenovarEscopoAsync(userId, dev.ComodoId.Value);

                return new ComandoAmbienteResult { Sucesso = true, MensagemVoz = feedback, DispositivosAcionados = new List<Guid>() };
            }

            var escopo = await _escopoService.GetEscopoAtivoAsync(userId);
            if (escopo != null)
            {
                var scopedDevs = candidates.Where(c => c.ComodoId == escopo.ComodoId).ToList();
                if (scopedDevs.Any())
                {
                     var feedback = await ExecuteDevices(userId, scopedDevs, turnOn);
                     await _escopoService.CriarOuRenovarEscopoAsync(userId, escopo.ComodoId);
                     return new ComandoAmbienteResult { Sucesso = true, MensagemVoz = feedback };
                }
            }

            return new ComandoAmbienteResult 
            { 
                Sucesso = false, 
                RequerConfirmacao = true, 
                MensagemVoz = $"Em qual cômodo você quer {acaoVoz} o {tipoDispositivo}?" 
            };
        }
        
        private string NormalizeComodoName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var n = name.ToLower().Trim();
            
            string[] prefixes = { "da ", "do ", "no ", "na ", "em ", "o ", "a ", "de " };
            foreach (var p in prefixes)
            {
                if (n.StartsWith(p))
                {
                    n = n.Substring(p.Length).Trim();
                    break;
                }
            }
            return n;
        }

        public async Task<List<DeviceSelectionDto>> GetAvailableDevicesAsync(Guid userId)
        {
             var list = new List<DeviceSelectionDto>();
             
             var mqtt = await _context.Devices.Where(d => d.UserId == userId).ToListAsync();
             list.AddRange(mqtt.Select(x => new DeviceSelectionDto { DispositivoId = x.Id.ToString(), Tipo = "Device", Name = x.Name }));
             
             var esp = await _context.DispositivosEsp.Where(d => d.UserId == userId).ToListAsync();
             list.AddRange(esp.Select(x => new DeviceSelectionDto { DispositivoId = x.Id.ToString(), Tipo = "Esp", Name = x.Nome }));
             
             var ewelink = await _context.EwelinkDevices.Where(d => d.UserId == userId).ToListAsync();
             list.AddRange(ewelink.Select(x => new DeviceSelectionDto { DispositivoId = x.DeviceId, Tipo = "Ewelink", Name = x.Name }));
             
             return list.OrderBy(x => x.Name).ToList();
        }

        private async Task<string> ExecuteDevices(Guid userId, List<DeviceCandidate> devices, bool turnOn = true)
        {
            var results = new List<string>();
            
            foreach (var dev in devices)
            {
                try 
                {
                    if (string.Equals(dev.Type, "Device", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Guid.TryParse(dev.Id, out var gId))
                        {
                            var d = await _context.Devices.FindAsync(gId);
                            if (d != null) 
                            {
                                 if (d.IsOn == turnOn)
                                 {
                                     results.Add($"{d.Name} já estava {(turnOn ? "ligada" : "desligada")}.");
                                 }
                                 else
                                 {
                                     await _mqttService.PublishAsync(d.MqttTopic, !string.IsNullOrEmpty(d.Comando) ? d.Comando : (turnOn ? "ON" : "OFF"));
                                     // Update state in DB optimistically
                                     d.IsOn = turnOn;
                                     await _context.SaveChangesAsync();
                                     results.Add($"{(turnOn ? "Liguei" : "Desliguei")} {d.Name}.");
                                 }
                            }
                        }
                    }
                    else if (string.Equals(dev.Type, "Ewelink", StringComparison.OrdinalIgnoreCase))
                    {
                        var status = await _ewelinkService.GetDeviceStatusAsync(userId, dev.Id);
                        if (status != null)
                        {
                            if (status.IsOn == turnOn)
                            {
                                results.Add($"{status.Name} já estava {(turnOn ? "ligada" : "desligada")}.");
                            }
                            else
                            {
                                var ok = await _ewelinkService.ControlDeviceAsync(userId, dev.Id, turnOn);
                                if (ok) results.Add($"{(turnOn ? "Liguei" : "Desliguei")} {status.Name}.");
                                else results.Add($"Erro ao controlar {status.Name}.");
                            }
                        }
                    }
                    else if (string.Equals(dev.Type, "Esp", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Guid.TryParse(dev.Id, out var gId))
                        {
                            var esp = await _context.DispositivosEsp.FindAsync(gId);
                            if (esp != null)
                            {
                                if (esp.LigadoDesligado == turnOn)
                                {
                                    results.Add($"{esp.Nome} já estava {(turnOn ? "ligado" : "desligado")}.");
                                }
                                else
                                {
                                    var comandoParaEnviar = !string.IsNullOrWhiteSpace(esp.ComandToEsp) 
                                        ? esp.ComandToEsp 
                                        : esp.Comando;

                                    if (!string.IsNullOrWhiteSpace(comandoParaEnviar))
                                    {
                                        var comandoData = new
                                        {
                                            nome = esp.Nome,
                                            ip = esp.Ip,
                                            porta = esp.Porta,
                                            comando = comandoParaEnviar,
                                            comandToEsp = esp.ComandToEsp ?? comandoParaEnviar
                                        };
                                        
                                        await _hubContext.Clients.Group("type_software").SendAsync("ComandoDispositivo", comandoData);
                                        
                                        // Update state optimistically
                                        esp.LigadoDesligado = turnOn;
                                        await _context.SaveChangesAsync();
                                        
                                        results.Add($"{(turnOn ? "Liguei" : "Desliguei")} {esp.Nome}.");
                                    }
                                    else
                                    {
                                        results.Add($"Dispositivo {esp.Nome} não tem comando configurado.");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao controlar dispositivo {Id} do tipo {Type}", dev.Id, dev.Type);
                    results.Add("Ocorreu um erro ao controlar um dos dispositivos.");
                }
            }
            
            if (!results.Any()) return "Pronto.";
            return string.Join(" ", results);
        }
        
        public async Task<string> ControlAllDevicesAsync(Guid userId, bool turnOn)
        {
            _logger.LogInformation("Comando global: {Acao} todos os dispositivos do usuário {UserId}", turnOn ? "Ligar" : "Desligar", userId);

            var candidates = new List<DeviceCandidate>();

            // 1. MQTT Devices
            var mqtt = await _context.Devices.Where(d => d.UserId == userId).ToListAsync();
            candidates.AddRange(mqtt.Select(d => new DeviceCandidate { Id = d.Id.ToString(), Type = "Device", Name = d.Name }));

            // 2. ESP Devices
            var esp = await _context.DispositivosEsp.Where(d => d.UserId == userId).ToListAsync();
            candidates.AddRange(esp.Select(d => new DeviceCandidate { Id = d.Id.ToString(), Type = "Esp", Name = d.Nome }));

            // 3. Ewelink Devices
            var ewelink = await _context.EwelinkDevices.Where(d => d.UserId == userId).ToListAsync();
            candidates.AddRange(ewelink.Select(d => new DeviceCandidate { Id = d.DeviceId, Type = "Ewelink", Name = d.Name }));

            if (!candidates.Any())
            {
                return "Você não tem dispositivos cadastrados.";
            }

            var feedback = await ExecuteDevices(userId, candidates, turnOn);
            return feedback;
        }

        private class DeviceCandidate
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public Guid? ComodoId { get; set; }
            public string? ComodoNome { get; set; }
            public string? Name { get; set; }
        }
    }
}
