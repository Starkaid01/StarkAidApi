using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using StarkAid.WindowsForms.Config;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace StarkAid.WindowsForms.Forms;

public partial class DispositivosEwelinkForm : Form
{
    private readonly ApiService _apiService;
    private readonly WebSocketService? _webSocketService;
    private readonly SpeechService? _speechService;
    private DataGridView? _dgvDispositivos;
    private Button? _btnAtualizar;
    private Button? _btnLigar;
    private Button? _btnDesligar;
    private Button? _btnAtualizarStatus;
    private Label? _lblStatus;
    private List<EwelinkDevice> _dispositivos = new();
    private bool _isLoggedIn = false;

    public DispositivosEwelinkForm(ApiService apiService, WebSocketService? webSocketService = null, SpeechService? speechService = null)
    {
        _apiService = apiService;
        _webSocketService = webSocketService;
        _speechService = speechService;
        InitializeComponent();
        this.Load += DispositivosEwelinkForm_Load;
        _ = CheckEwelinkStatus(); // Fire and forget no construtor
    }

    private async void DispositivosEwelinkForm_Load(object? sender, EventArgs e)
    {
        // Atualizar sessão com nome do form
        _ = _apiService.SetUserOnlineAsync("DispositivosEwelinkForm");
    }

    private void InitializeComponent()
    {
        this.Text = "Dispositivos Ewelink";
        this.Size = new Size(1000, 680);
        this.MinimumSize = new Size(1000, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        // Painel superior com status e botão atualizar
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(25, 25, 35),
            Padding = new Padding(20, 10, 20, 10)
        };

        _lblStatus = new Label
        {
            Text = "Verificando status...",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Yellow,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _btnAtualizarStatus = new Button
        {
            Text = "🔄 ATUALIZAR STATUS",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 35),
            Location = new Point(250, 10),
            Cursor = Cursors.Hand
        };
        _btnAtualizarStatus.FlatAppearance.BorderSize = 0;
        _btnAtualizarStatus.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnAtualizarStatus.MouseLeave += (s, e) => SoundPlayer.StopMouseMove();
        _btnAtualizarStatus.Click += BtnAtualizarStatus_Click;

        pnlTop.Controls.Add(_lblStatus);
        pnlTop.Controls.Add(_btnAtualizarStatus);

        _dgvDispositivos = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            GridColor = Color.FromArgb(50, 50, 60),
            EnableHeadersVisualStyles = false
        };
        
        // Estilo do cabeçalho
        _dgvDispositivos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvDispositivos.ColumnHeadersHeight = 35;
        _dgvDispositivos.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvDispositivos.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvDispositivos.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvDispositivos.Columns.Add("DeviceId", "ID");
        _dgvDispositivos.Columns.Add("Name", "Nome");
        _dgvDispositivos.Columns.Add("Online", "Online");
        _dgvDispositivos.Columns.Add("IsOn", "Estado");
        _dgvDispositivos.Columns[0].Visible = false;

        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(25, 25, 35)
        };

        _btnAtualizar = new Button
        {
            Text = "🔄 ATUALIZAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(140, 40),
            Location = new Point(20, 10),
            Cursor = Cursors.Hand
        };
        _btnAtualizar.FlatAppearance.BorderSize = 0;
        _btnAtualizar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnAtualizar.MouseLeave += (s, e) => SoundPlayer.StopMouseMove();
        _btnAtualizar.Click += BtnAtualizar_Click;

        _btnLigar = new Button
        {
            Text = "🔛 LIGAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(170, 10),
            Cursor = Cursors.Hand
        };
        _btnLigar.FlatAppearance.BorderSize = 0;
        _btnLigar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnLigar.MouseLeave += (s, e) => SoundPlayer.StopMouseMove();
        _btnLigar.Click += BtnLigar_Click;

        _btnDesligar = new Button
        {
            Text = "🔴 DESLIGAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(300, 10),
            Cursor = Cursors.Hand
        };
        _btnDesligar.FlatAppearance.BorderSize = 0;
        _btnDesligar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnDesligar.MouseLeave += (s, e) => SoundPlayer.StopMouseMove();
        _btnDesligar.Click += BtnDesligar_Click;

        pnlButtons.Controls.Add(_btnAtualizar);
        pnlButtons.Controls.Add(_btnLigar);
        pnlButtons.Controls.Add(_btnDesligar);

        this.Controls.Add(_dgvDispositivos);
        this.Controls.Add(pnlTop);
        this.Controls.Add(pnlButtons);
    }

    private async Task CheckEwelinkStatus()
    {
        try
        {
            var status = await _apiService.GetEwelinkStatusAsync();
            _isLoggedIn = status?.IsLoggedIn ?? false;

            if (_isLoggedIn)
            {
                _lblStatus!.Text = "✓ Conectado ao Ewelink";
                _lblStatus.ForeColor = Color.Green;
                await LoadDispositivos();
            }
            else
            {
                _lblStatus!.Text = "⚠ Não conectado ao Ewelink";
                _lblStatus.ForeColor = Color.Orange;
                ShowNotConnectedMessage();
            }
        }
        catch (Exception ex)
        {
            _lblStatus!.Text = "❌ Erro ao verificar status";
            _lblStatus.ForeColor = Color.Red;
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar status Ewelink: {ex.Message}");
            ShowNotConnectedMessage();
        }
    }

    private void ShowNotConnectedMessage()
    {
        _dgvDispositivos!.Rows.Clear();
        
        // Remover todas as linhas do DataGridView
        _dgvDispositivos.Rows.Clear();
        
        // Criar painel de mensagem
        var pnlMessage = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 40),
            Padding = new Padding(40)
        };

        var lblMessage = new Label
        {
            Text = "Conectar Ewelink:\n\n" +
                   "• Clique no link abaixo\n" +
                   "• Faça login na plataforma\n" +
                   "• Clique em Dispositivos Ewelink\n" +
                   "• Faça login com sua conta Ewelink\n" +
                   "• Volte ao Software e veja se seus dispositivos aparecem.\n\n",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = false,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 200
        };

        var linkLabel = new LinkLabel
        {
            Text = $"{ApiConfig.WebBaseUrl}/automacao.html",
            Font = new Font("Segoe UI", 10, FontStyle.Underline),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter
        };
        linkLabel.LinkClicked += (s, e) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"{ApiConfig.WebBaseUrl}/automacao.html",
                UseShellExecute = true
            });
        };

        pnlMessage.Controls.Add(linkLabel);
        pnlMessage.Controls.Add(lblMessage);
        _dgvDispositivos.Controls.Add(pnlMessage);
    }

    private async Task LoadDispositivos()
    {
        try
        {
            _dispositivos = await _apiService.GetEwelinkDevicesAsync();
            _dgvDispositivos!.Rows.Clear();
            
            // Remover qualquer painel de mensagem se existir
            var panelsToRemove = _dgvDispositivos.Controls.OfType<Panel>().ToList();
            foreach (var panel in panelsToRemove)
            {
                _dgvDispositivos.Controls.Remove(panel);
            }

            foreach (var dispositivo in _dispositivos)
            {
                var onlineStatus = dispositivo.Online ? "Online" : "Offline";
                var estadoStatus = dispositivo.IsOn ? "Ligado" : "Desligado";
                var onlineColor = dispositivo.Online ? Color.Green : Color.Red;
                var estadoColor = dispositivo.IsOn ? Color.Green : Color.Gray;
                
                var rowIndex = _dgvDispositivos.Rows.Add(
                    dispositivo.DeviceId,
                    dispositivo.Name,
                    onlineStatus,
                    estadoStatus
                );
                
                // Colorir células de status
                if (rowIndex >= 0 && rowIndex < _dgvDispositivos.Rows.Count)
                {
                    var row = _dgvDispositivos.Rows[rowIndex];
                    if (row.Cells.Count > 2)
                    {
                        row.Cells[2].Style.ForeColor = onlineColor;
                    }
                    if (row.Cells.Count > 3)
                    {
                        row.Cells[3].Style.ForeColor = estadoColor;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar dispositivos Ewelink: {ex.Message}");
            MessageBox.Show($"Erro ao carregar dispositivos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnAtualizarStatus_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        await CheckEwelinkStatus();
    }

    private async void BtnAtualizar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        await LoadDispositivos();
    }

    private async void BtnLigar_Click(object? sender, EventArgs e)
    {
        await ControlarDispositivo(true);
    }

    private async void BtnDesligar_Click(object? sender, EventArgs e)
    {
        await ControlarDispositivo(false);
    }

    private async Task ControlarDispositivo(bool ligar)
    {
        SoundPlayer.PlayClick();
        if (_dgvDispositivos!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um dispositivo primeiro!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var deviceId = _dgvDispositivos.SelectedRows[0].Cells[0].Value.ToString()!;
        var device = _dispositivos.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null) return;

        try
        {
            if (await _apiService.ControlEwelinkDeviceAsync(deviceId, ligar))
            {
                SoundPlayer.PlaySuccess();
                
                // Gerar resposta similar ao CommandProcessor
                var acao = ligar ? "ligado" : "desligado";
                var respostasAleatorias = new[]
                {
                    $"{device.Name} {acao}, posso ajudar em algo mais?",
                    $"{device.Name} {acao}, mais alguma coisa?",
                    $"{device.Name} {acao}, está tudo certo!",
                    $"{device.Name} {acao}, precisa de mais alguma coisa?",
                    $"{device.Name} {acao}, pronto!"
                };
                var random = new Random();
                var resposta = respostasAleatorias[random.Next(respostasAleatorias.Length)];
                
                // IMPORTANTE: Respostas Ewelink devem manter acentuação e pontuação originais para fala natural
                // NÃO aplicar NormalizeText nas respostas que serão faladas
                
                // Falar a resposta
                if (_speechService != null)
                {
                    _speechService.Speak(resposta);
                }
                
                // Enviar resposta via WebSocket com prefixo toApp:
                if (_webSocketService != null)
                {
                    await _webSocketService.SendRespostaAsync(device.Name ?? "", "", 0, resposta);
                    System.Diagnostics.Debug.WriteLine($"[EWELINK] Resposta enviada via WebSocket (botão): {resposta}");
                }
                
                // Atualizar atividade do usuário
                var comandoTexto = ligar ? $"ligar {device.Name}" : $"desligar {device.Name}";
                _ = _apiService.UpdateUserActivityAsync(ultimoComandoEwelink: comandoTexto);
                
                // Atualizar estado do dispositivo na lista
                await LoadDispositivos();
            }
            else
            {
                SoundPlayer.PlayError();
                var resposta = "Erro ao controlar dispositivo Ewelink";
                
                // Enviar resposta de erro via WebSocket com prefixo toApp:
                if (_webSocketService != null)
                {
                    await _webSocketService.SendRespostaAsync(device.Name ?? "", "", 0, resposta);
                }
                
                MessageBox.Show("Erro ao controlar dispositivo!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            var resposta = $"Erro: {ex.Message}";
            
            // Enviar resposta de erro via WebSocket com prefixo toApp:
            if (_webSocketService != null)
            {
                await _webSocketService.SendRespostaAsync(device.Name ?? "", "", 0, resposta);
            }
            
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
