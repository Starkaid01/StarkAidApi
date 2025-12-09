using Microsoft.AspNetCore.SignalR.Client;
using StarkAid.WindowsForms.Services;
using System.Text;

namespace StarkAid.WindowsForms.Forms;

public partial class ChatSuporteForm : Form
{
    private readonly ApiService _apiService;
    private HubConnection? _hubConnection;
    private TextBox? _txtMensagem;
    private RichTextBox? _rtbChat;
    private Button? _btnEnviar;
    private Button? _btnConectar;
    private Button? _btnDesconectar;
    private Label? _lblStatus;
    private Panel? _pnlQueueStatus;
    private Label? _lblQueueStatus;

    public ChatSuporteForm(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Chat de Suporte";
        this.Size = new Size(800, 600);
        this.MinimumSize = new Size(600, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);
        this.FormBorderStyle = FormBorderStyle.Sizable;

        // Painel de status da fila
        _pnlQueueStatus = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(30, 30, 40),
            Padding = new Padding(10),
            Visible = false
        };

        _lblQueueStatus = new Label
        {
            Dock = DockStyle.Fill,
            Text = "",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _pnlQueueStatus.Controls.Add(_lblQueueStatus);

        // Status da conexão
        _lblStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Text = "Desconectado",
            ForeColor = Color.Orange,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };

        // Área de chat
        _rtbChat = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 15, 25),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        // Painel de controles
        var pnlControls = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            BackColor = Color.FromArgb(30, 30, 40)
        };

        // Botões de conexão
        var pnlConnectionButtons = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(10, 5, 10, 5)
        };

        _btnConectar = new Button
        {
            Text = "Conectar",
            Size = new Size(100, 30),
            Location = new Point(10, 5),
            BackColor = Color.FromArgb(0, 150, 150),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9)
        };
        _btnConectar.FlatAppearance.BorderSize = 0;
        _btnConectar.Click += BtnConectar_Click;

        _btnDesconectar = new Button
        {
            Text = "Desconectar",
            Size = new Size(100, 30),
            Location = new Point(120, 5),
            BackColor = Color.FromArgb(150, 0, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Enabled = false
        };
        _btnDesconectar.FlatAppearance.BorderSize = 0;
        _btnDesconectar.Click += BtnDesconectar_Click;

        pnlConnectionButtons.Controls.Add(_btnConectar);
        pnlConnectionButtons.Controls.Add(_btnDesconectar);

        // Área de mensagem
        var pnlMessage = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _txtMensagem = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 50),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = true,
            Height = 40
        };
        _txtMensagem.KeyDown += TxtMensagem_KeyDown;

        _btnEnviar = new Button
        {
            Text = "Enviar",
            Dock = DockStyle.Right,
            Width = 100,
            BackColor = Color.FromArgb(0, 150, 150),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Enabled = false
        };
        _btnEnviar.FlatAppearance.BorderSize = 0;
        _btnEnviar.Click += BtnEnviar_Click;

        pnlMessage.Controls.Add(_btnEnviar);
        pnlMessage.Controls.Add(_txtMensagem);

        pnlControls.Controls.Add(pnlMessage);
        pnlControls.Controls.Add(pnlConnectionButtons);

        // Adicionar controles ao formulário
        this.Controls.Add(pnlControls);
        this.Controls.Add(_rtbChat);
        this.Controls.Add(_pnlQueueStatus);
        this.Controls.Add(_lblStatus);
    }

    private async void BtnConectar_Click(object? sender, EventArgs e)
    {
        try
        {
            var token = _apiService.GetAuthToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Você precisa estar logado para usar o chat de suporte.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var baseUrl = _apiService.GetBaseUrl();
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/hubs/support-chat?origem=software", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            // Event handlers
            _hubConnection.On<object>("QueuePosition", (data) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() => AtualizarStatusFila(data)));
                }
                else if (IsHandleCreated)
                {
                    AtualizarStatusFila(data);
                }
            });

            _hubConnection.On<object>("NextInQueue", (data) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() => AtualizarStatusFila(data)));
                }
                else if (IsHandleCreated)
                {
                    AtualizarStatusFila(data);
                }
            });

            _hubConnection.On<object>("ReceiveMessage", (data) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var json = System.Text.Json.JsonSerializer.Serialize(data);
                            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                            var message = dict?.ContainsKey("message") == true ? dict["message"]?.ToString() : json;
                            var sender = dict?.ContainsKey("sender") == true ? dict["sender"]?.ToString() ?? "ia" : "ia";
                            AdicionarMensagem(message ?? "Mensagem vazia", sender);
                        }
                        catch
                        {
                            // Se falhar, tentar como string direta
                            AdicionarMensagem(data?.ToString() ?? "Mensagem vazia", "ia");
                        }
                    }));
                }
                else if (IsHandleCreated)
                {
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(data);
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        var message = dict?.ContainsKey("message") == true ? dict["message"]?.ToString() : json;
                        var sender = dict?.ContainsKey("sender") == true ? dict["sender"]?.ToString() ?? "ia" : "ia";
                        AdicionarMensagem(message ?? "Mensagem vazia", sender);
                    }
                    catch
                    {
                        // Se falhar, tentar como string direta
                        AdicionarMensagem(data?.ToString() ?? "Mensagem vazia", "ia");
                    }
                }
            });

            _hubConnection.On<string>("Error", (error) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() => MessageBox.Show($"Erro no chat: {error}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
                else if (IsHandleCreated)
                {
                    MessageBox.Show($"Erro no chat: {error}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            _hubConnection.Reconnecting += (error) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() => _lblStatus.Text = "Reconectando..."));
                }
                else if (IsHandleCreated)
                {
                    _lblStatus.Text = "Reconectando...";
                }
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += (connectionId) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() => _lblStatus.Text = "Conectado"));
                }
                else if (IsHandleCreated)
                {
                    _lblStatus.Text = "Conectado";
                }
                return Task.CompletedTask;
            };

            _hubConnection.On<string>("LimiteAtingido", (message) =>
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _txtMensagem.Enabled = false;
                        _txtMensagem.Text = "Limite de contexto atingido. Preencha o formulário abaixo.";
                        _btnEnviar.Enabled = false;
                        AdicionarMensagem("Limite de contexto atingido. Por favor, preencha o formulário de suporte.", "system");
                    }));
                }
                else if (IsHandleCreated)
                {
                    _txtMensagem.Enabled = false;
                    _txtMensagem.Text = "Limite de contexto atingido. Preencha o formulário abaixo.";
                    _btnEnviar.Enabled = false;
                    AdicionarMensagem("Limite de contexto atingido. Por favor, preencha o formulário de suporte.", "system");
                }
            });

            await _hubConnection.StartAsync();

            _lblStatus.Text = "Conectado";
            _lblStatus.ForeColor = Color.Lime;
            _btnConectar.Enabled = false;
            _btnDesconectar.Enabled = true;
            _btnEnviar.Enabled = true;
            _txtMensagem?.Focus();

            AdicionarMensagem("Conectado ao chat de suporte. Aguarde sua vez na fila...", "system");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao conectar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblStatus.Text = "Erro ao conectar";
            _lblStatus.ForeColor = Color.Red;
        }
    }

    private async void BtnDesconectar_Click(object? sender, EventArgs e)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _lblStatus.Text = "Desconectado";
        _lblStatus.ForeColor = Color.Orange;
        _btnConectar.Enabled = true;
        _btnDesconectar.Enabled = false;
        _btnEnviar.Enabled = false;
        _pnlQueueStatus.Visible = false;
        AdicionarMensagem("Desconectado do chat de suporte.", "system");
    }

    private async void BtnEnviar_Click(object? sender, EventArgs e)
    {
        await EnviarMensagem();
    }

    private async void TxtMensagem_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            await EnviarMensagem();
        }
    }

    private async Task EnviarMensagem()
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            MessageBox.Show("Você precisa estar conectado para enviar mensagens.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_txtMensagem == null || string.IsNullOrWhiteSpace(_txtMensagem.Text))
        {
            return;
        }

        var mensagem = _txtMensagem.Text.Trim();
        _txtMensagem.Clear();

        try
        {
            await _hubConnection.InvokeAsync("SendMessage", mensagem);
            AdicionarMensagem(mensagem, "user");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao enviar mensagem: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AdicionarMensagem(string mensagem, string sender)
    {
        if (_rtbChat == null) return;

        var sb = new StringBuilder();
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        
        switch (sender)
        {
            case "user":
                sb.AppendLine($"[{timestamp}] Você: {mensagem}");
                _rtbChat.SelectionColor = Color.LightBlue;
                break;
            case "ia":
                sb.AppendLine($"[{timestamp}] Assistente: {mensagem}");
                _rtbChat.SelectionColor = Color.LightGreen;
                break;
            case "support":
                sb.AppendLine($"[{timestamp}] Suporte: {mensagem}");
                _rtbChat.SelectionColor = Color.Yellow;
                break;
            default:
                sb.AppendLine($"[{timestamp}] {mensagem}");
                _rtbChat.SelectionColor = Color.Gray;
                break;
        }

        _rtbChat.AppendText(sb.ToString());
        _rtbChat.SelectionStart = _rtbChat.Text.Length;
        _rtbChat.ScrollToCaret();
    }

    private void AtualizarStatusFila(object data)
    {
        if (_lblQueueStatus == null || _pnlQueueStatus == null) return;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (dict != null && dict.ContainsKey("message"))
            {
                _lblQueueStatus.Text = dict["message"].ToString() ?? "";
                _pnlQueueStatus.Visible = true;
            }
            else if (dict != null && dict.ContainsKey("posicao"))
            {
                var posicao = dict["posicao"].ToString();
                _lblQueueStatus.Text = $"Aguarde, você está na fila. Posição: {posicao}";
                _pnlQueueStatus.Visible = true;
            }
        }
        catch
        {
            _lblQueueStatus.Text = "Aguardando na fila...";
            _pnlQueueStatus.Visible = true;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_hubConnection != null)
        {
            _hubConnection.StopAsync().Wait();
            _hubConnection.DisposeAsync().AsTask().Wait();
        }
        base.OnFormClosing(e);
    }
}
