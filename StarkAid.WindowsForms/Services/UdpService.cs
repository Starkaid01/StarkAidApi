using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StarkAid.WindowsForms.Services;

public class UdpService
{
    private UdpClient? _udpClient;
    private IPEndPoint? _localEndPoint;
    private bool _isListening = false;
    private Thread? _listeningThread;

    public event EventHandler<string>? ResponseReceived;

    public void StartListening(int port = 1495)
    {
        if (_isListening)
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] ⚠️ Listener UDP já está ativo na porta {port}");
            return;
        }

        try
        {
            // Fechar qualquer socket anterior se existir
            if (_udpClient != null)
            {
                try
                {
                    _udpClient.Close();
                    _udpClient.Dispose();
                }
                catch { }
                _udpClient = null;
            }

            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
            _udpClient = new UdpClient(_localEndPoint);
            
            // Configurar socket para permitir recebimento de qualquer origem
            // NÃO usar Broadcast aqui pois pode causar problemas
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            _isListening = true;

            // Obter todos os IPs locais para log
            var localIps = GetAllLocalIPs();
            System.Diagnostics.Debug.WriteLine($"[UDP] 🎧 Iniciando listener UDP na porta {port}");
            System.Diagnostics.Debug.WriteLine($"[UDP] 📍 IPs locais detectados: {string.Join(", ", localIps)}");
            System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Listener UDP ativo e aguardando mensagens na porta {port}");
            System.Diagnostics.Debug.WriteLine($"[UDP] 📡 Socket configurado para receber de qualquer origem");

            _listeningThread = new Thread(ListenForResponses)
            {
                IsBackground = true,
                Name = "UDPListenerThread"
            };
            _listeningThread.Start();
            
            System.Diagnostics.Debug.WriteLine($"[UDP] 🧵 Thread de escuta iniciada: {_listeningThread.Name}");
        }
        catch (SocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] ❌ Erro de socket ao iniciar listener UDP: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UDP] SocketErrorCode: {ex.SocketErrorCode}");
            System.Diagnostics.Debug.WriteLine($"[UDP] Stack trace: {ex.StackTrace}");
            _isListening = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] ❌ Erro ao iniciar listener UDP: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UDP] Stack trace: {ex.StackTrace}");
            _isListening = false;
        }
    }

    public void StopListening()
    {
        _isListening = false;
        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;
    }

    private void ListenForResponses()
    {
        System.Diagnostics.Debug.WriteLine($"[UDP] 🎧 Thread de escuta UDP iniciada, aguardando mensagens na porta {_localEndPoint?.Port ?? 1495}...");
        System.Diagnostics.Debug.WriteLine($"[UDP] 📡 Socket UDP pronto para receber: {_udpClient != null}");
        
        var receiveCount = 0;
        
        while (_isListening && _udpClient != null)
        {
            try
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                
                // Log apenas na primeira vez e a cada 100 tentativas para debug
                if (receiveCount == 0 || receiveCount % 100 == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[UDP] ⏳ Aguardando receber dados UDP... (tentativa {receiveCount + 1})");
                }
                
                System.Diagnostics.Debug.WriteLine($"[UDP] 🔍 Chamando Receive()...");
                var data = _udpClient.Receive(ref remoteEndPoint);
                receiveCount++;
                
                System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Receive() retornou! Dados recebidos: {data?.Length ?? 0} bytes");
                
                if (data != null && data.Length > 0)
                {
                    var message = Encoding.UTF8.GetString(data);
                    
                    System.Diagnostics.Debug.WriteLine($"[UDP] 📥✅✅✅ RESPOSTA RECEBIDA! ✅✅✅");
                    System.Diagnostics.Debug.WriteLine($"[UDP] 📥 Origem: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                    System.Diagnostics.Debug.WriteLine($"[UDP] 📥 Mensagem: {message}");
                    System.Diagnostics.Debug.WriteLine($"[UDP] 📦 Tamanho: {data.Length} bytes");
                    
                    // Verificar se há handlers registrados
                    if (ResponseReceived == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UDP] ⚠️⚠️⚠️ ATENÇÃO: Nenhum handler registrado para ResponseReceived! ⚠️⚠️⚠️");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Handler ResponseReceived encontrado, invocando...");
                        ResponseReceived?.Invoke(this, message);
                        System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Handler ResponseReceived invocado com sucesso!");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[UDP] ⚠️ Dados recebidos vazios ou nulos");
                }
            }
            catch (SocketException ex)
            {
                receiveCount++;
                if (_isListening)
                {
                    System.Diagnostics.Debug.WriteLine($"[UDP] ⚠️ SocketException durante recebimento: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[UDP] SocketErrorCode: {ex.SocketErrorCode}");
                    // Não sair do loop, apenas logar e continuar tentando
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    // Socket fechado intencionalmente, sair do loop
                    System.Diagnostics.Debug.WriteLine($"[UDP] 🛑 Socket fechado intencionalmente");
                    break;
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket foi fechado/disposto, sair do loop
                System.Diagnostics.Debug.WriteLine($"[UDP] 🛑 Socket foi fechado/disposto");
                break;
            }
            catch (Exception ex)
            {
                receiveCount++;
                System.Diagnostics.Debug.WriteLine($"[UDP] ❌ Erro ao receber UDP: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UDP] Tipo de exceção: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[UDP] Stack trace: {ex.StackTrace}");
                // Continuar tentando mesmo em caso de erro
                System.Threading.Thread.Sleep(100);
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[UDP] 🛑 Thread de escuta UDP finalizada após {receiveCount} tentativas");
    }

    public void SendCommand(string ip, int porta, string comando)
    {
        UdpClient? client = null;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] 📤 Preparando envio para {ip}:{porta} - Comando: {comando}");
            
            // Validar IP
            if (!IPAddress.TryParse(ip, out var ipAddress))
            {
                System.Diagnostics.Debug.WriteLine($"[UDP] ❌ ERRO: IP inválido: {ip}");
                return;
            }
            
            // Validar porta
            if (porta <= 0 || porta > 65535)
            {
                System.Diagnostics.Debug.WriteLine($"[UDP] ❌ ERRO: Porta inválida: {porta}");
                return;
            }
            
            var endPoint = new IPEndPoint(ipAddress, porta);
            var data = Encoding.UTF8.GetBytes(comando);
            
            System.Diagnostics.Debug.WriteLine($"[UDP] 📦 Dados preparados: {data.Length} bytes");
            System.Diagnostics.Debug.WriteLine($"[UDP] 🎯 Destino: {ipAddress}:{porta}");
            System.Diagnostics.Debug.WriteLine($"[UDP] 📝 Comando em bytes: {BitConverter.ToString(data)}");
            
            // Tentar usar Socket diretamente para ter mais controle
            Socket? socket = null;
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                System.Diagnostics.Debug.WriteLine($"[UDP] 🔌 Socket criado diretamente");
                
                // Enviar usando Socket.SendTo
                System.Diagnostics.Debug.WriteLine($"[UDP] 🚀 Enviando pacote UDP para {ipAddress}:{porta} usando Socket...");
                var bytesSent = socket.SendTo(data, endPoint);
                
                System.Diagnostics.Debug.WriteLine($"[UDP] ✅✅✅ COMANDO ENVIADO COM SUCESSO! ✅✅✅");
                System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Bytes enviados: {bytesSent}/{data.Length}");
                System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Destino: {ip}:{porta}");
                System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Comando: {comando}");
                
                // Pequeno delay para garantir que o pacote seja transmitido
                System.Threading.Thread.Sleep(100);
            }
            finally
            {
                try
                {
                    socket?.Close();
                    socket?.Dispose();
                }
                catch { }
            }
            
            // Também tentar com UdpClient como fallback (comentado por enquanto)
            /*
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            System.Diagnostics.Debug.WriteLine($"[UDP] 🔌 Socket UDP criado, pronto para enviar");
            System.Diagnostics.Debug.WriteLine($"[UDP] 🚀 Enviando pacote UDP para {ipAddress}:{porta}...");
            var bytesSent2 = client.Send(data, data.Length, endPoint);
            System.Diagnostics.Debug.WriteLine($"[UDP] ✅ Bytes enviados (UdpClient): {bytesSent2}/{data.Length}");
            System.Threading.Thread.Sleep(100);
            */
        }
        catch (SocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] ❌ Erro de socket ao enviar comando UDP: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UDP] SocketErrorCode: {ex.SocketErrorCode}");
            System.Diagnostics.Debug.WriteLine($"[UDP] Stack trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] ❌ Erro ao enviar comando UDP: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UDP] Tipo: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[UDP] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Fechar o cliente UDP após enviar
            try
            {
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    System.Diagnostics.Debug.WriteLine($"[UDP] 🔒 Socket de envio fechado");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UDP] ⚠️ Erro ao fechar socket de envio: {ex.Message}");
            }
        }
    }

    public void SendCommandFormatted(string ip, int porta, string comando)
    {
        var formattedCommand = $"{ip}|{porta}|{comando}";
        var parts = formattedCommand.Split('|');
        if (parts.Length == 3)
        {
            if (IPAddress.TryParse(parts[0], out _) && int.TryParse(parts[1], out var port))
            {
                SendCommand(parts[0], port, parts[2]);
            }
        }
    }

    public string? GetLocalIP()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString();
            }
        }
        catch
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
        }
        return null;
    }
    
    private List<string> GetAllLocalIPs()
    {
        var ips = new List<string>();
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] Erro ao obter IPs locais: {ex.Message}");
        }
        
        // Se não encontrou nenhum IP, tentar método alternativo
        if (ips.Count == 0)
        {
            try
            {
                var localIp = GetLocalIP();
                if (!string.IsNullOrEmpty(localIp))
                {
                    ips.Add(localIp);
                }
            }
            catch { }
        }
        
        return ips;
    }

    public int GetPort()
    {
        return _localEndPoint?.Port ?? 1495;
    }

    public bool IsListening => _isListening;
}

