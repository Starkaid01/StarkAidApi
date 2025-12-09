using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using StarkAid.WindowsForms.Models;

namespace StarkAid.WindowsForms.Services;

public class WebSocketService
{
    private HubConnection? _connection;
    private readonly string _baseUrl = "https://starkaid.runasp.net";
    private string? _token;
    private readonly HashSet<string> _processedMessages = new HashSet<string>();
    private readonly object _processLock = new object();

    public event EventHandler<(string nome, string ip, int porta, string comando)>? ComandoDispositivoReceived;
    public event EventHandler<string>? RespostaDispositivoReceived;
    public event EventHandler<string>? ToSoftMessageReceived;
    public event EventHandler<string>? SuporteComandoReceived;

    public async Task ConnectAsync(string token)
    {
        _token = token;
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== WebSocketService.ConnectAsync iniciado ===");
            System.Diagnostics.Debug.WriteLine($"URL: {_baseUrl}/hubs/dispositivo-esp?type=software");
            System.Diagnostics.Debug.WriteLine($"Token presente: {!string.IsNullOrEmpty(token)}");
            
            _connection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/hubs/dispositivo-esp?type=software", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();
            
            System.Diagnostics.Debug.WriteLine("HubConnection criado, iniciando conexão...");

            // Registrar eventos de conexão
            _connection.Closed += async (error) =>
            {
                System.Diagnostics.Debug.WriteLine($"WebSocket desconectado: {error?.Message ?? "Sem erro"}");
                // Tentar reconectar após 5 segundos
                await Task.Delay(5000);
                if (!string.IsNullOrEmpty(_token))
                {
                    try
                    {
                        await ConnectAsync(_token);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao reconectar WebSocket: {ex.Message}");
                    }
                }
            };

            _connection.Reconnecting += (error) =>
            {
                System.Diagnostics.Debug.WriteLine($"WebSocket reconectando: {error?.Message ?? "Sem erro"}");
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                System.Diagnostics.Debug.WriteLine($"WebSocket reconectado: {connectionId}");
                return Task.CompletedTask;
            };

            // Registrar handlers - usando object para tratar diferentes formatos
            _connection.On<object>("ComandoDispositivo", (data) =>
            {
                try
                {
                    ComandoDispositivoDto? comando = null;
                    string jsonString = "";
                    
                    // Tentar deserializar diretamente se for o tipo esperado
                    if (data is ComandoDispositivoDto dto)
                    {
                        comando = dto;
                    }
                    // Tratar JsonElement do System.Text.Json
                    else if (data is JsonElement jsonElement)
                    {
                        jsonString = jsonElement.GetRawText();
                        System.Diagnostics.Debug.WriteLine($"ComandoDispositivo recebido via WebSocket (JsonElement): {jsonString}");
                        comando = JsonConvert.DeserializeObject<ComandoDispositivoDto>(jsonString);
                    }
                    else
                    {
                        // Tentar deserializar de JSON usando Newtonsoft
                        jsonString = JsonConvert.SerializeObject(data);
                        System.Diagnostics.Debug.WriteLine($"ComandoDispositivo recebido via WebSocket (JSON): {jsonString}");
                        comando = JsonConvert.DeserializeObject<ComandoDispositivoDto>(jsonString);
                    }
                    
                    if (comando != null && !string.IsNullOrWhiteSpace(comando.Ip) && comando.Porta > 0)
                    {
                        // Usa ComandToEsp se disponível, senão usa Comando
                        var comandoParaEnviar = !string.IsNullOrWhiteSpace(comando.ComandToEsp) 
                            ? comando.ComandToEsp 
                            : comando.Comando ?? "";
                        
                        System.Diagnostics.Debug.WriteLine($"Comando recebido via WebSocket: IP={comando.Ip}, Porta={comando.Porta}, Comando={comando.Comando}, ComandToEsp={comando.ComandToEsp ?? "não fornecido"}, Usando={comandoParaEnviar}");
                        System.Diagnostics.Debug.WriteLine($"Processando comando: {comando.Nome} - {comando.Ip}:{comando.Porta} - {comandoParaEnviar}");
                        ComandoDispositivoReceived?.Invoke(this, (comando.Nome ?? "", comando.Ip ?? "", comando.Porta, comandoParaEnviar));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"AVISO: Dados do comando inválidos ou incompletos! IP={comando?.Ip}, Porta={comando?.Porta}, Comando={comando?.Comando}");
                        System.Diagnostics.Debug.WriteLine($"JSON recebido: {jsonString}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao processar comando dispositivo: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"Tipo do objeto recebido: {data?.GetType().FullName}");
                }
            });

            _connection.On<object>("RespostaDispositivo", (data) =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(data);
                    var obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null)
                    {
                        var resposta = obj.resposta?.ToString() ?? "";
                        
                        // Se contém "toSoft:", processar apenas via handler ToSoft (evitar duplicação)
                        // Não processar aqui para evitar que seja falado duas vezes
                        if (!resposta.Contains("toSoft:", StringComparison.OrdinalIgnoreCase))
                        {
                            RespostaDispositivoReceived?.Invoke(this, resposta);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao processar resposta dispositivo: {ex.Message}");
                }
            });

            // Handler para comandos de suporte
            _connection.On<string>("SuporteComando", (comando) =>
            {
                System.Diagnostics.Debug.WriteLine($"Comando de suporte recebido: {comando}");
                string acao;
                
                // Se começar com "suporteToSoft:", remover o prefixo
                if (comando.StartsWith("suporteToSoft:"))
                {
                    acao = comando.Replace("suporteToSoft:", "");
                }
                else
                {
                    // Comando direto (ex: "limparcache", "logout")
                    acao = comando;
                }
                
                SuporteComandoReceived?.Invoke(this, acao);
            });

            _connection.On<object>("ToAppResposta", (data) =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(data);
                    var obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null)
                    {
                        var resposta = obj.resposta?.ToString() ?? obj.ToString() ?? "";
                        
                        // Se contém "toSoft:", processar apenas via handler ToSoft (evitar duplicação)
                        // Não processar aqui para evitar que seja falado duas vezes
                        if (!resposta.Contains("toSoft:", StringComparison.OrdinalIgnoreCase))
                        {
                            RespostaDispositivoReceived?.Invoke(this, resposta);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao processar ToAppResposta: {ex.Message}");
                }
            });

            // Handler genérico para qualquer mensagem que possa conter "toSoft:"
            _connection.On<string>("ToSoft", (message) =>
            {
                try
                {
                    ProcessToSoftMessage(message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao processar ToSoft: {ex.Message}");
                }
            });

            await _connection.StartAsync();
            System.Diagnostics.Debug.WriteLine($"✅ WebSocket conectado com sucesso! ConnectionId: {_connection.ConnectionId}");
            System.Diagnostics.Debug.WriteLine($"Estado da conexão: {_connection.State}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erro ao conectar WebSocket: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async Task SendRespostaAsync(string nome, string ip, int porta, string resposta)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                var resp = $"toApp:{resposta}";
                await _connection.SendAsync("ReceberRespostaDoSoftware", nome, ip, porta, resp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao enviar resposta: {ex.Message}");
            }
        }
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Processa mensagens que contêm o prefixo "toSoft:" removendo o prefixo e disparando evento
    /// Evita processamento duplicado usando um HashSet para rastrear mensagens já processadas
    /// </summary>
    private void ProcessToSoftMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (message.Contains("toSoft:", StringComparison.OrdinalIgnoreCase))
        {
            // Remover o prefixo "toSoft:" (case-insensitive) apenas uma vez
            var index = message.IndexOf("toSoft:", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var mensagemSemPrefixo = message.Substring(index + "toSoft:".Length).Trim();
                
                // Evitar processamento duplicado - verificar se já processamos esta mensagem nos últimos 3 segundos
                lock (_processLock)
                {
                    var messageKey = mensagemSemPrefixo.ToLowerInvariant();
                    
                    // Limpar mensagens antigas (mais de 3 segundos) - simplificado
                    // Na prática, vamos apenas verificar se a mensagem já está no HashSet
                    // e limpar periodicamente (a cada 10 mensagens processadas)
                    if (_processedMessages.Count > 100)
                    {
                        _processedMessages.Clear();
                    }
                    
                    // Se já processamos esta mensagem recentemente, ignorar
                    if (_processedMessages.Contains(messageKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"[WebSocket] Mensagem toSoft duplicada ignorada: {mensagemSemPrefixo}");
                        return;
                    }
                    
                    // Adicionar mensagem atual
                    _processedMessages.Add(messageKey);
                    
                    // Remover após 3 segundos (usando Task.Delay em background)
                    _ = Task.Delay(3000).ContinueWith(_ =>
                    {
                        lock (_processLock)
                        {
                            _processedMessages.Remove(messageKey);
                        }
                    });
                }
                
                System.Diagnostics.Debug.WriteLine($"[WebSocket] Mensagem toSoft detectada: {message}");
                System.Diagnostics.Debug.WriteLine($"[WebSocket] Mensagem após remover prefixo: {mensagemSemPrefixo}");
                
                ToSoftMessageReceived?.Invoke(this, mensagemSemPrefixo);
            }
        }
    }
}

