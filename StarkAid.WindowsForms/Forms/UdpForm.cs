using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class UdpForm : Form
{
    private readonly UdpService _udpService;
    private Label? _lblStatus;
    private Label? _lblIP;
    private Label? _lblPorta;
    private TextBox? _txtIP;
    private TextBox? _txtPorta;
    private Button? _btnCopiarIP;
    private Button? _btnCopiarPorta;

    public UdpForm(UdpService udpService)
    {
        _udpService = udpService;
        InitializeComponent();
        LoadUdpInfo();
    }

    private void InitializeComponent()
    {
        this.Text = "UDP";
        this.Size = new Size(500, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Padding = new Padding(0);

        // Painel principal
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 25, 35),
            Padding = new Padding(30)
        };

        // Barra de título
        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(35, 35, 45)
        };

        var lblTitle = new Label
        {
            Text = "UDP",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(40, 40),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Red; SoundPlayer.PlayMouseMove(); };
        btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; SoundPlayer.StopMouseMove(); };
        btnClose.Click += (s, e) => { SoundPlayer.PlayClick(); this.Close(); };

        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(btnClose);

        // Painel de conteúdo
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        // Label de status
        _lblStatus = new Label
        {
            Text = "Escutando na porta.",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        // IP
        var lblIPLabel = new Label
        {
            Text = "IP:",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 70)
        };

        _txtIP = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(250, 30),
            Location = new Point(60, 68),
            ReadOnly = true
        };

        _btnCopiarIP = new Button
        {
            Text = "Copiar",
            Font = new Font("Segoe UI", 10),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(80, 30),
            Location = new Point(320, 68),
            Cursor = Cursors.Hand
        };
        _btnCopiarIP.FlatAppearance.BorderSize = 0;
        _btnCopiarIP.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnCopiarIP.Click += (s, e) =>
        {
            SoundPlayer.PlayClick();
            if (!string.IsNullOrEmpty(_txtIP.Text))
            {
                Clipboard.SetText(_txtIP.Text);
                MessageBox.Show("IP copiado para a área de transferência!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        // PORTA
        var lblPortaLabel = new Label
        {
            Text = "PORTA:",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 120)
        };

        _txtPorta = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(250, 30),
            Location = new Point(100, 118),
            ReadOnly = true
        };

        _btnCopiarPorta = new Button
        {
            Text = "Copiar",
            Font = new Font("Segoe UI", 10),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(80, 30),
            Location = new Point(360, 118),
            Cursor = Cursors.Hand
        };
        _btnCopiarPorta.FlatAppearance.BorderSize = 0;
        _btnCopiarPorta.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnCopiarPorta.Click += (s, e) =>
        {
            SoundPlayer.PlayClick();
            if (!string.IsNullOrEmpty(_txtPorta.Text))
            {
                Clipboard.SetText(_txtPorta.Text);
                MessageBox.Show("Porta copiada para a área de transferência!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        contentPanel.Controls.Add(_lblStatus);
        contentPanel.Controls.Add(lblIPLabel);
        contentPanel.Controls.Add(_txtIP);
        contentPanel.Controls.Add(_btnCopiarIP);
        contentPanel.Controls.Add(lblPortaLabel);
        contentPanel.Controls.Add(_txtPorta);
        contentPanel.Controls.Add(_btnCopiarPorta);

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(contentPanel);

        this.Controls.Add(mainPanel);
    }

    private void LoadUdpInfo()
    {
        var ip = _udpService.GetLocalIP();
        var porta = _udpService.GetPort();

        if (_txtIP != null)
        {
            _txtIP.Text = ip ?? "Não disponível";
        }

        if (_txtPorta != null)
        {
            _txtPorta.Text = porta.ToString();
        }

        if (_lblStatus != null)
        {
            if (_udpService.IsListening)
            {
                _lblStatus.Text = "Escutando na porta.";
                _lblStatus.ForeColor = Color.FromArgb(76, 175, 80); // Verde
            }
            else
            {
                _lblStatus.Text = "Não está escutando.";
                _lblStatus.ForeColor = Color.FromArgb(239, 68, 68); // Vermelho
            }
        }
    }
}

