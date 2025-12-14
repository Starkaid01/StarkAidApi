using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class PlanosForm : Form
{
    private readonly ApiService _apiService;
    private readonly List<PlanoInfo> _planos;

    public PlanosForm(ApiService apiService)
    {
        _apiService = apiService;
        _planos = new List<PlanoInfo>
        {
            new PlanoInfo { Nivel = 2, Nome = "StarkAid Premium", Preco = 10.00m, Descricao = "4500 tokens/sem, agendamentos ilimitados, anúncios OFF, +50 SC/mês" }
        };
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Contratar Plano";
        this.Size = new Size(1000, 700);
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
            Text = "CONTRATAR PLANO",
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

        // Painel de conteúdo com scroll
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 80, 0, 20),
            AutoScrollMargin = new Size(0, 20)
        };
        // Desabilitar scroll horizontal completamente
        contentPanel.HorizontalScroll.Enabled = false;
        contentPanel.HorizontalScroll.Visible = false;
        contentPanel.HorizontalScroll.Maximum = 0;
        contentPanel.AutoScrollMinSize = new Size(0, 0);

        // Grid de planos (2 colunas)
        var planosPanel = new Panel
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };

        int x = 0;
        int y = 0;
        const int cardWidth = 280;
        const int cardHeight = 160;
        const int spacing = 20;

        foreach (var plano in _planos)
        {
            var card = CreatePlanoCard(plano);
            card.Location = new Point(x, y);
            card.Size = new Size(cardWidth, cardHeight);
            planosPanel.Controls.Add(card);

            x += cardWidth + spacing;
            if (x + cardWidth > 600) // 2 colunas - ajustado para cards menores
            {
                x = 0;
                y += cardHeight + spacing;
            }
        }

        // Centralizar o painel de planos horizontalmente
        var panelWidth = 580; // Largura total para 2 colunas
        var panelHeight = y + cardHeight + spacing;
        planosPanel.Size = new Size(panelWidth, panelHeight);
        
        // Centralizar horizontalmente no contentPanel
        planosPanel.Anchor = AnchorStyles.None;
        contentPanel.Resize += (s, e) =>
        {
            var centerX = (contentPanel.ClientSize.Width - planosPanel.Width) / 2;
            planosPanel.Location = new Point(Math.Max(0, centerX), contentPanel.Padding.Top);
        };
        
        // Posicionar inicialmente (centralizado e respeitando padding top)
        var initialCenterX = (contentPanel.ClientSize.Width - planosPanel.Width) / 2;
        planosPanel.Location = new Point(Math.Max(0, initialCenterX), contentPanel.Padding.Top);
        
        contentPanel.Controls.Add(planosPanel);

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(contentPanel);

        this.Controls.Add(mainPanel);
    }

    private Panel CreatePlanoCard(PlanoInfo plano)
    {
        var card = new Panel
        {
            BackColor = Color.FromArgb(30, 30, 40),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15)
        };

        // Nome do plano
        var lblNome = new Label
        {
            Text = plano.Nome,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(15, 15)
        };

        // Preço (verde como no Android)
        var lblPreco = new Label
        {
            Text = $"R$ {plano.Preco:F2}",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(76, 175, 80), // Verde similar ao holo_green_dark
            AutoSize = true,
            Location = new Point(15, 50)
        };

        // Botão Contratar
        var btnContratar = new Button
        {
            Text = "CONTRATAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 32),
            Location = new Point(15, 110),
            Cursor = Cursors.Hand,
            Tag = plano.Nivel
        };
        btnContratar.FlatAppearance.BorderSize = 0;
        btnContratar.Click += async (s, e) => await BtnContratar_Click(plano.Nivel, btnContratar);

        card.Controls.Add(lblNome);
        card.Controls.Add(lblPreco);
        card.Controls.Add(btnContratar);

        return card;
    }

    private async Task BtnContratar_Click(int nivel, Button btn)
    {
        SoundPlayer.PlayClick();

        btn.Enabled = false;
        btn.Text = "PROCESSANDO...";

        try
        {
            var checkoutUrl = await _apiService.CreatePlanoCheckoutAsync(nivel);
            
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
            btn.Enabled = true;
            btn.Text = "CONTRATAR";
        }
    }

    private class PlanoInfo
    {
        public int Nivel { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }
}

