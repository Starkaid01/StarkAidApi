using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class StarkCoinsPlanosForm : Form
{
    private readonly ApiService _apiService;
    private readonly User _currentUser;
    private Label? _lblStarkCoins;
    private Label? _lblStatus;
    private Button? _btnAddFunds;
    private Button? _btnPlanos;
    private Panel? _pnlContent;

    public StarkCoinsPlanosForm(ApiService apiService, User currentUser)
    {
        _apiService = apiService;
        _currentUser = currentUser;
        InitializeComponent();
        LoadStarkCoins();
    }

    private void InitializeComponent()
    {
        this.Text = "StarkCoins | Planos";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(15, 15, 25);
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
            Text = "STARKCOINS | PLANOS",
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
        btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Red; };
        btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; };
        btnClose.Click += (s, e) => this.Close();

        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(btnClose);

        // Painel de conteúdo
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 20, 0, 0)
        };

        int yPos = 20;

        // Label StarkCoins no topo
        var lblStarkCoinsTitle = new Label
        {
            Text = "StarkCoins:",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 35;

        _lblStarkCoins = new Label
        {
            Text = $"{_currentUser.StarkCoinBalance} SC",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 60;

        // Botões
        var buttonsPanel = new Panel
        {
            Size = new Size(720, 60),
            Location = new Point(0, yPos),
            BackColor = Color.Transparent
        };

        _btnAddFunds = new Button
        {
            Text = "💰 ADICIONAR FUNDOS",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 150, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(340, 50),
            Location = new Point(0, 5),
            Cursor = Cursors.Hand
        };
        _btnAddFunds.FlatAppearance.BorderSize = 0;
        _btnAddFunds.MouseEnter += (s, e) => { _btnAddFunds.BackColor = Color.FromArgb(60, 170, 60); SoundPlayer.PlayMouseMove(); };
        _btnAddFunds.MouseLeave += (s, e) => { _btnAddFunds.BackColor = Color.FromArgb(50, 150, 50); };
        _btnAddFunds.Click += BtnAddFunds_Click;

        _btnPlanos = new Button
        {
            Text = "📋 CONTRATAR PLANO",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(340, 50),
            Location = new Point(380, 5),
            Cursor = Cursors.Hand
        };
        _btnPlanos.FlatAppearance.BorderSize = 0;
        _btnPlanos.MouseEnter += (s, e) => { _btnPlanos.BackColor = Color.FromArgb(60, 120, 220); SoundPlayer.PlayMouseMove(); };
        _btnPlanos.MouseLeave += (s, e) => { _btnPlanos.BackColor = Color.FromArgb(50, 100, 200); };
        _btnPlanos.Click += BtnPlanos_Click;

        buttonsPanel.Controls.Add(_btnAddFunds);
        buttonsPanel.Controls.Add(_btnPlanos);
        yPos += 80;

        // TextView de status
        var statusPanel = new Panel
        {
            Size = new Size(720, 200),
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(30, 30, 40),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15)
        };

        _lblStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(690, 170),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft
        };

        statusPanel.Controls.Add(_lblStatus);

        _pnlContent.Controls.Add(lblStarkCoinsTitle);
        _pnlContent.Controls.Add(_lblStarkCoins);
        _pnlContent.Controls.Add(buttonsPanel);
        _pnlContent.Controls.Add(statusPanel);

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(_pnlContent);

        this.Controls.Add(mainPanel);
    }

    private async void LoadStarkCoins()
    {
        try
        {
            var user = await _apiService.GetCurrentUserAsync();
            if (user != null && _lblStarkCoins != null)
            {
                _lblStarkCoins.Text = $"{user.StarkCoinBalance} SC";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar StarkCoins: {ex.Message}");
        }
    }

    public void UpdateStarkCoins(int newValue)
    {
        if (_lblStarkCoins != null)
        {
            _lblStarkCoins.Text = $"{newValue} SC";
        }
    }
    
    // Overload para compatibilidade
    public void UpdateStarkCoins(decimal newValue)
    {
        UpdateStarkCoins((int)newValue);
    }

    public void UpdateStatus(string message)
    {
        if (_lblStatus != null)
        {
            _lblStatus.Text = message;
        }
    }

    private void BtnAddFunds_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        UpdateStatus("Redirecionando para pagamento...");
        var form = new AddFundsForm(_apiService);
        form.ShowDialog();
    }

    private void BtnPlanos_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        UpdateStatus("Redirecionando para pagamento...");
        var form = new PlanosForm(_apiService);
        form.ShowDialog();
    }
}

