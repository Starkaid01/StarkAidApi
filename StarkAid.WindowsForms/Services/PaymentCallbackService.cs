using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace StarkAid.WindowsForms.Services;

public class PaymentCallbackService : IDisposable
{
    private HttpListener? _listener;
    private bool _isRunning = false;
    private Thread? _listenerThread;
    private const int Port = 8765;

    public event EventHandler<PaymentCallbackEventArgs>? PaymentSuccess;
    public event EventHandler? PaymentCanceled;

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _isRunning = true;

            _listenerThread = new Thread(Listen)
            {
                IsBackground = true
            };
            _listenerThread.Start();

            System.Diagnostics.Debug.WriteLine($"✅ PaymentCallbackService iniciado na porta {Port}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erro ao iniciar PaymentCallbackService: {ex.Message}");
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _listener?.Stop();
        _listener?.Close();
        _listener = null;

        System.Diagnostics.Debug.WriteLine("🛑 PaymentCallbackService parado");
    }

    private void Listen()
    {
        while (_isRunning && _listener != null)
        {
            try
            {
                var context = _listener.GetContext();
                _ = Task.Run(() => ProcessRequest(context));
            }
            catch (HttpListenerException)
            {
                // Listener foi fechado
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar requisição: {ex.Message}");
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var url = request.Url?.AbsolutePath ?? "";
            var query = request.Url?.Query ?? "";

            System.Diagnostics.Debug.WriteLine($"📥 Requisição recebida: {url}?{query}");

            // Processar callback de pagamento
            if (url == "/payment")
            {
                var queryParams = ParseQueryString(query);
                
                // Verificar se é sucesso ou cancelamento
                if (queryParams.ContainsKey("funds"))
                {
                    if (queryParams["funds"] == "success")
                    {
                        PaymentSuccess?.Invoke(this, new PaymentCallbackEventArgs { Type = "funds", Success = true });
                        SendResponse(response, 200, "Pagamento de fundos processado com sucesso! Você pode fechar esta janela.");
                    }
                    else if (queryParams["funds"] == "cancel")
                    {
                        PaymentCanceled?.Invoke(this, EventArgs.Empty);
                        SendResponse(response, 200, "Pagamento de fundos cancelado. Você pode fechar esta janela.");
                    }
                }
                else if (queryParams.ContainsKey("plano"))
                {
                    if (queryParams["plano"] == "success")
                    {
                        var nivel = queryParams.ContainsKey("nivel") ? queryParams["nivel"] : "";
                        PaymentSuccess?.Invoke(this, new PaymentCallbackEventArgs { Type = "plano", Success = true, Nivel = nivel });
                        SendResponse(response, 200, "Plano contratado com sucesso! Você pode fechar esta janela.");
                    }
                    else if (queryParams["plano"] == "cancel")
                    {
                        PaymentCanceled?.Invoke(this, EventArgs.Empty);
                        SendResponse(response, 200, "Contratação de plano cancelada. Você pode fechar esta janela.");
                    }
                }
                else
                {
                    SendResponse(response, 400, "Parâmetros inválidos.");
                }
            }
            else
            {
                SendResponse(response, 404, "Página não encontrada.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar callback: {ex.Message}");
            SendResponse(response, 500, $"Erro interno: {ex.Message}");
        }
        finally
        {
            response.Close();
        }
    }

    private Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return result;

        // Remover o '?' inicial se existir
        if (query.StartsWith("?"))
            query = query.Substring(1);

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }

        return result;
    }

    private void SendResponse(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        response.ContentType = "text/html; charset=utf-8";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>StarkAid - Pagamento</title>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            color: #fff;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}
        .container {{
            text-align: center;
            padding: 40px;
            background: rgba(25, 25, 35, 0.9);
            border-radius: 10px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.5);
        }}
        h1 {{
            color: #00ffff;
            margin-bottom: 20px;
        }}
        p {{
            font-size: 18px;
            line-height: 1.6;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>StarkAid</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        Stop();
    }
}

public class PaymentCallbackEventArgs : EventArgs
{
    public string Type { get; set; } = string.Empty; // "funds" ou "plano"
    public bool Success { get; set; }
    public string Nivel { get; set; } = string.Empty;
}

