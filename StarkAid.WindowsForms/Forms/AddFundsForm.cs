using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class AddFundsForm : Form
{
    private readonly ApiService _apiService;
    private TextBox? _txtAmount;
    private Button? _btnContinuar;
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
        this.Size = new Size(500, 400);
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
            Padding = new Padding(0, 20, 0, 0)
        };

        int yPos = 20;

        // Label Valor
        var lblAmount = new Label
        {
            Text = "Valor (R$)",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        // Campo de valor
        _txtAmount = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(420, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0)
        };
        _txtAmount.KeyPress += (s, e) =>
        {
            // Permitir apenas números, vírgula e ponto
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }
            // Converter ponto para vírgula
            if (e.KeyChar == '.')
            {
                e.KeyChar = ',';
            }
        };
        yPos += 55;

        // Label Valores Sugeridos
        var lblSuggested = new Label
        {
            Text = "Valores Sugeridos:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        // Botões de valores sugeridos
        var buttonsPanel = new Panel
        {
            Size = new Size(420, 50),
            Location = new Point(0, yPos),
            BackColor = Color.Transparent
        };

        _btnR10 = CreateSuggestedButton("R$ 10", 0, buttonsPanel);
        _btnR25 = CreateSuggestedButton("R$ 25", 1, buttonsPanel);
        _btnR50 = CreateSuggestedButton("R$ 50", 2, buttonsPanel);
        _btnR100 = CreateSuggestedButton("R$ 100", 3, buttonsPanel);

        _btnR10.Click += (s, e) => { SoundPlayer.PlayClick(); _txtAmount!.Text = "10"; };
        _btnR25.Click += (s, e) => { SoundPlayer.PlayClick(); _txtAmount!.Text = "25"; };
        _btnR50.Click += (s, e) => { SoundPlayer.PlayClick(); _txtAmount!.Text = "50"; };
        _btnR100.Click += (s, e) => { SoundPlayer.PlayClick(); _txtAmount!.Text = "100"; };

        buttonsPanel.Controls.Add(_btnR10);
        buttonsPanel.Controls.Add(_btnR25);
        buttonsPanel.Controls.Add(_btnR50);
        buttonsPanel.Controls.Add(_btnR100);
        yPos += 70;

        // Botão Continuar
        _btnContinuar = new Button
        {
            Text = "CONTINUAR PARA PAGAMENTO",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(420, 45),
            Location = new Point(0, yPos),
            Cursor = Cursors.Hand
        };
        _btnContinuar.FlatAppearance.BorderSize = 0;
        _btnContinuar.Click += BtnContinuar_Click;
        yPos += 60;

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
        contentPanel.Controls.Add(_txtAmount);
        contentPanel.Controls.Add(lblSuggested);
        contentPanel.Controls.Add(buttonsPanel);
        contentPanel.Controls.Add(_btnContinuar);
        contentPanel.Controls.Add(btnCancelar);

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(contentPanel);

        this.Controls.Add(mainPanel);
    }

    private Button CreateSuggestedButton(string text, int index, Panel parent)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(95, 40),
            Location = new Point(index * 105, 5),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(70, 70, 80); SoundPlayer.PlayMouseMove(); };
        btn.MouseLeave += (s, e) => { btn.BackColor = Color.FromArgb(50, 50, 60); };
        return btn;
    }

    private async void BtnContinuar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();

        if (string.IsNullOrWhiteSpace(_txtAmount!.Text))
        {
            MessageBox.Show("Por favor, informe o valor desejado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Converter valor (aceitar vírgula ou ponto)
        var amountText = _txtAmount.Text.Replace(',', '.');
        if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
        {
            MessageBox.Show("Valor inválido. Por favor, informe um valor numérico válido.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (amount <= 0)
        {
            MessageBox.Show("O valor deve ser maior que zero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnContinuar!.Enabled = false;
        _btnContinuar.Text = "PROCESSANDO...";

        try
        {
            var checkoutUrl = await _apiService.CreateAddFundsCheckoutAsync(amount);
            
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
        finally
        {
            _btnContinuar.Enabled = true;
            _btnContinuar.Text = "CONTINUAR PARA PAGAMENTO";
        }
    }
}

