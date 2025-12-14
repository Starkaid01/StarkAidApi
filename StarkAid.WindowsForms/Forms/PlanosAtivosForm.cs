using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Collections.Generic;

namespace StarkAid.WindowsForms.Forms;

public partial class PlanosAtivosForm : Form
{
    private readonly ApiService _apiService;
    private Panel? _pnlContent;
    private Button? _btnRefresh;
    private Label? _lblTitle;
    private Panel? _pnlTitleBar;

    public PlanosAtivosForm(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
        LoadPlanosAtivos();
    }

    private void InitializeComponent()
    {
        this.Text = "Planos Ativos";
        this.Size = new Size(900, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(15, 15, 25);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Padding = new Padding(0);

        // Painel principal
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 25, 35),
            Padding = new Padding(0)
        };

        // Barra de título
        _pnlTitleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(35, 35, 45)
        };

        _lblTitle = new Label
        {
            Text = "💳 PLANOS ATIVOS",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _btnRefresh = new Button
        {
            Text = "🔄 Atualizar",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 35),
            Location = new Point(650, 8),
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.MouseEnter += (s, e) => { _btnRefresh.BackColor = Color.FromArgb(0, 100, 180); };
        _btnRefresh.MouseLeave += (s, e) => { _btnRefresh.BackColor = Color.FromArgb(0, 120, 215); };
        _btnRefresh.Click += (s, e) => LoadPlanosAtivos();

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

        _pnlTitleBar.Controls.Add(_lblTitle);
        _pnlTitleBar.Controls.Add(_btnRefresh);
        _pnlTitleBar.Controls.Add(btnClose);

        // Painel de conteúdo com scroll
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(25, 25, 35),
            Padding = new Padding(20, 200, 20, 20)
        };

        mainPanel.Controls.Add(_pnlTitleBar);
        mainPanel.Controls.Add(_pnlContent);
        this.Controls.Add(mainPanel);
    }

    private async void LoadPlanosAtivos()
    {
        if (_pnlContent == null) return;

        _pnlContent.Controls.Clear();

        // Mostrar loading
        var lblLoading = new Label
        {
            Text = "Carregando planos ativos...",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };
        _pnlContent.Controls.Add(lblLoading);

        try
        {
            var planos = await _apiService.GetPlanosAtivosAsync();
            
            _pnlContent.Controls.Clear();

            if (planos == null || planos.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "Nenhum plano ativo encontrado",
                    Font = new Font("Segoe UI", 14),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Location = new Point(20, 20)
                };
                _pnlContent.Controls.Add(lblEmpty);
                return;
            }

            int yPos = 70;
            foreach (var plano in planos)
            {
                var card = CreatePlanoCard(plano, yPos);
                _pnlContent.Controls.Add(card);
                yPos += card.Height + 15;
            }
        }
        catch (Exception ex)
        {
            _pnlContent.Controls.Clear();
            var lblError = new Label
            {
                Text = $"Erro ao carregar planos: {ex.Message}",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Red,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            _pnlContent.Controls.Add(lblError);
        }
    }

    private Panel CreatePlanoCard(PlanoAtivo plano, int yPos)
    {
        var card = new Panel
        {
            BackColor = Color.FromArgb(35, 35, 45),
            Size = new Size(840, 180),
            Location = new Point(0, yPos),
            Padding = new Padding(15, 15, 15, 15)
        };

        // Determinar cor do badge baseado no nível
        Color badgeColor = Color.FromArgb(0, 120, 215);
        var nomePlano = plano.NomePlano;
        if (plano.Nivel == 2)
        {
            badgeColor = Color.FromArgb(16, 185, 129); // Verde para Premium
            nomePlano = "StarkAid Premium"; // Garantir nome correto
        }
        else if (plano.Nivel >= 3 && plano.Nivel <= 7)
        {
            badgeColor = Color.FromArgb(59, 130, 246); // Azul para planos de StarkCoins
        }

        // Título do plano
        var lblNome = new Label
        {
            Text = nomePlano,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, 15)
        };

        // Badge de status
        var lblStatus = new Label
        {
            Text = plano.Status,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = badgeColor,
            AutoSize = true,
            Padding = new Padding(8, 4, 8, 4),
            Location = new Point(700, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Botão cancelar
        var btnCancelar = new Button
        {
            Text = "Cancelar Plano",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(239, 68, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 35),
            Location = new Point(700, 50),
            Cursor = Cursors.Hand,
            Tag = plano.Id
        };
        btnCancelar.FlatAppearance.BorderSize = 0;
        btnCancelar.MouseEnter += (s, e) => { btnCancelar.BackColor = Color.FromArgb(220, 38, 38); };
        btnCancelar.MouseLeave += (s, e) => { btnCancelar.BackColor = Color.FromArgb(239, 68, 68); };
        btnCancelar.Click += async (s, e) => await CancelarPlano(plano.Id);

        // Informações do plano
        int infoY = 60;
        var lblNivel = new Label
        {
            Text = $"Nível: {plano.Nivel}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, infoY)
        };
        infoY += 25;

        var lblValor = new Label
        {
            Text = $"Valor: R$ {plano.Valor:F2}/mês",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, infoY)
        };
        infoY += 25;

        var dataInicio = plano.IniciadaEm?.ToString("dd/MM/yyyy") ?? "N/A";
        var lblInicio = new Label
        {
            Text = $"Iniciado em: {dataInicio}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, infoY)
        };
        infoY += 25;

        var dataExpiracao = plano.ExpiraEm?.ToString("dd/MM/yyyy") ?? "Sem expiração";
        var lblExpiracao = new Label
        {
            Text = $"Expira em: {dataExpiracao}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, infoY)
        };
        infoY += 25;

        var dataCriacao = plano.DataCriacao.ToString("dd/MM/yyyy");
        var lblCriacao = new Label
        {
            Text = $"Criado em: {dataCriacao}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, infoY)
        };

        if (!string.IsNullOrEmpty(plano.StripeSubscriptionId))
        {
            infoY += 25;
            var lblStripeId = new Label
            {
                Text = $"ID Stripe: {plano.StripeSubscriptionId}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, infoY)
            };
            card.Controls.Add(lblStripeId);
        }

        card.Controls.Add(lblNome);
        card.Controls.Add(lblStatus);
        card.Controls.Add(btnCancelar);
        card.Controls.Add(lblNivel);
        card.Controls.Add(lblValor);
        card.Controls.Add(lblInicio);
        card.Controls.Add(lblExpiracao);
        card.Controls.Add(lblCriacao);

        return card;
    }

    private async Task CancelarPlano(Guid assinaturaId)
    {
        var result = MessageBox.Show(
            "Tem certeza que deseja cancelar este plano? O cancelamento será processado imediatamente.",
            "Confirmar Cancelamento",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        try
        {
            var sucesso = await _apiService.CancelarPlanoAsync(assinaturaId);
            
            if (sucesso)
            {
                SoundPlayer.PlaySuccess();
                MessageBox.Show("Plano cancelado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadPlanosAtivos();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao cancelar plano. Tente novamente.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro ao cancelar plano: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

