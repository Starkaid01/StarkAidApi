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
        if (_isListening) return;

        try
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
            _udpClient = new UdpClient(_localEndPoint);
            _isListening = true;

            _listeningThread = new Thread(ListenForResponses)
            {
                IsBackground = true
            };
            _listeningThread.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao iniciar listener UDP: {ex.Message}");
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
        while (_isListening && _udpClient != null)
        {
            try
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = _udpClient.Receive(ref remoteEndPoint);
                var message = Encoding.UTF8.GetString(data);
                ResponseReceived?.Invoke(this, message);
            }
            catch (SocketException)
            {
                // Socket fechado, sair do loop
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao receber UDP: {ex.Message}");
            }
        }
    }

    public void SendCommand(string ip, int porta, string comando)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Enviando comando UDP para {ip}:{porta} - Comando: {comando}");
            using var client = new UdpClient();
            var endPoint = new IPEndPoint(IPAddress.Parse(ip), porta);
            var data = Encoding.UTF8.GetBytes(comando);
            var bytesSent = client.Send(data, data.Length, endPoint);
            System.Diagnostics.Debug.WriteLine($"Comando UDP enviado com sucesso! {bytesSent} bytes enviados para {ip}:{porta}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar comando UDP: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

    public int GetPort()
    {
        return _localEndPoint?.Port ?? 1495;
    }

    public bool IsListening => _isListening;
}

