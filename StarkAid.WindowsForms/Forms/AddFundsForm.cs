using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class AddFundsForm : Form
{
    private readonly ApiService _apiService;
    private Button? _btnR10;
    private Button? _btnR25;
    private Button? _btnR50;
    private Button? _btnR100;

    public AddFundsForm(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Adicionar Fundos - StarkCoins";
        this.Size = new Size(500, 450); // Aumentado altura para acomodar todos os elementos
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
            Text = "ADICIONAR FUNDOS - STARKCOINS",
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

        // Conteúdo
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 40, 0, 0) // Aumentado padding top para 40
        };

        int yPos = 0; // Começar do topo do padding

        // Label Pacotes
        var lblAmount = new Label
        {
            Text = "Escolha um pacote de StarkCoins:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 40; // Espaço maior após o label

        // Botões de pacotes fixos de StarkCoins
        var buttonsPanel = new Panel
        {
            Size = new Size(420, 120), // Altura ajustada para 2 linhas de botões
            Location = new Point(0, yPos),
            BackColor = Color.Transparent
        };

        // Criar botões diretamente com tamanho e posição corretos
        _btnR10 = new Button
        {
            Text = "5 SC - R$ 4,90",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 50),
            Location = new Point(0, 0),
            Cursor = Cursors.Hand
        };
        _btnR10.FlatAppearance.BorderSize = 0;
        _btnR10.MouseEnter += (s, e) => { _btnR10.BackColor = Color.FromArgb(70, 70, 80); SoundPlayer.PlayMouseMove(); };
        _btnR10.MouseLeave += (s, e) => { _btnR10.BackColor = Color.FromArgb(50, 50, 60); };
        _btnR10.Click += async (s, e) => { SoundPlayer.PlayClick(); await ProcessarPagamento(5); };

        _btnR25 = new Button
        {
            Text = "15 SC - R$ 9,90",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 50),
            Location = new Point(220, 0), // Espaço de 20px entre botões
            Cursor = Cursors.Hand
        };
        _btnR25.FlatAppearance.BorderSize = 0;
        _btnR25.MouseEnter += (s, e) => { _btnR25.BackColor = Color.FromArgb(70, 70, 80); SoundPlayer.PlayMouseMove(); };
        _btnR25.MouseLeave += (s, e) => { _btnR25.BackColor = Color.FromArgb(50, 50, 60); };
        _btnR25.Click += async (s, e) => { SoundPlayer.PlayClick(); await ProcessarPagamento(15); };

        _btnR50 = new Button
        {
            Text = "50 SC - R$ 19,90",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 50),
            Location = new Point(0, 60), // Segunda linha, espaço de 10px após primeira linha
            Cursor = Cursors.Hand
        };
        _btnR50.FlatAppearance.BorderSize = 0;
        _btnR50.MouseEnter += (s, e) => { _btnR50.BackColor = Color.FromArgb(70, 70, 80); SoundPlayer.PlayMouseMove(); };
        _btnR50.MouseLeave += (s, e) => { _btnR50.BackColor = Color.FromArgb(50, 50, 60); };
        _btnR50.Click += async (s, e) => { SoundPlayer.PlayClick(); await ProcessarPagamento(50); };

        _btnR100 = new Button
        {
            Text = "120 SC - R$ 39,90",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 50),
            Location = new Point(220, 60), // Segunda linha, segunda coluna
            Cursor = Cursors.Hand
        };
        _btnR100.FlatAppearance.BorderSize = 0;
        _btnR100.MouseEnter += (s, e) => { _btnR100.BackColor = Color.FromArgb(70, 70, 80); SoundPlayer.PlayMouseMove(); };
        _btnR100.MouseLeave += (s, e) => { _btnR100.BackColor = Color.FromArgb(50, 50, 60); };
        _btnR100.Click += async (s, e) => { SoundPlayer.PlayClick(); await ProcessarPagamento(120); };

        buttonsPanel.Controls.Add(_btnR10);
        buttonsPanel.Controls.Add(_btnR25);
        buttonsPanel.Controls.Add(_btnR50);
        buttonsPanel.Controls.Add(_btnR100);
        yPos += 130; // Altura dos botões + espaçamento

        // Botão Continuar removido - os botões de pacote já processam o pagamento diretamente

        // Botão Cancelar
        var btnCancelar = new Button
        {
            Text = "CANCELAR",
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(420, 40),
            Location = new Point(0, yPos),
            Cursor = Cursors.Hand
        };
        btnCancelar.FlatAppearance.BorderSize = 0;
        btnCancelar.Click += (s, e) => { SoundPlayer.PlayClick(); this.Close(); };

        contentPanel.Controls.Add(lblAmount);
        contentPanel.Controls.Add(buttonsPanel);
        contentPanel.Controls.Add(btnCancelar);

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(contentPanel);

        this.Controls.Add(mainPanel);
    }


    private async Task ProcessarPagamento(int coins)
    {
        try
        {
            var checkoutUrl = await _apiService.CreateAddFundsCheckoutAsync(coins);
            
            if (!string.IsNullOrEmpty(checkoutUrl))
            {
                // Abrir URL no navegador padrão
                Process.Start(new ProcessStartInfo
                {
                    FileName = checkoutUrl,
                    UseShellExecute = true
                });

                // Não mostrar MessageBox, apenas fechar
                // A mensagem será exibida no TextView do formulário StarkCoinsPlanos
                this.Close();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao criar sessão de pagamento. Tente novamente.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro ao processar pagamento: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

