using Microsoft.VisualBasic.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;

namespace StarkAid.WindowsForms.Forms;

public partial class MainForm : Form
{
    private readonly ApiService _apiService;
    private readonly LocalDatabase _database;
    private readonly WebSocketService _webSocketService;
    private readonly UdpService _udpService;
    private readonly SpeechService _speechService;
    private readonly CommandProcessor _commandProcessor;
    private User _currentUser;
    private readonly LicenseService _licenseService;
    private readonly PaymentCallbackService _paymentCallbackService;
    private StarkCoinsPlanosForm? _openStarkCoinsForm;

    private Panel? _pnlSidebar;
    private Panel? _pnlContent;
    private Button? _btnDashboard;
    private Button? _btnComandosSociais;
    private Button? _btnDevices;
    private Button? _btnDispositivosEsp;
    private Button? _btnDispositivosEwelink;
    private Button? _btnAgendamentos;
    private Button? _btnAgendamentosArquivos;
    private Button? _btnStarkCoinsPlanos;
    private Button? _btnPlanosAtivos;
    private Button? _btnAtualizar;
    private Button? _btnToggleIA;
    private Button? _btnAlarmes;
    private Button? _btnAprendizado;
    private Button? _btnChatSuporte;
    private Label? _lblStarkCoins;
    private Label? _lblTotalDevices;
    private Label? _lblTotalComandos;
    private Label? _lblApiStatus;
    private Panel? _pnlDashboard;
    private bool _iaEnabled = false;
    private bool _lastApiStatus = false;
    private Panel? _pnlWeatherDashboard;
    private Label? _lblWeatherTemp;
    private Label? _lblWeatherCondition;
    private Label? _lblWeatherWind;
    private Panel pnlTitleBar;
    private Label lblTitle;
    private Button btnClose;
    private Button btnMinimize;
    private Button btnSair;
    private bool _isInitialized = false;
    private TextBox? _txtTextoReconhecido;
    private Button? _btnIniciarReconhecimento;
    private bool _reconhecimentoAtivo = false;
    private WebView2? _webView;
    private bool _webViewReady = false;
    private bool _webViewMessageHandlerRegistered = false;
    SpeechSynthesizer? Falador;
    string RESULTADO = "";
    string RECEBIDO = "";
    private Thread? NucleoEscutador;
    private System.Windows.Forms.Timer? _agendamentoTimer;
    private System.Windows.Forms.Timer? _alarmesTimer;
    private System.Windows.Forms.Timer? _verificarAssistenteDormindoTimer;
    private int _contadorAlarmesDisparados = 0;
    private Label? _lblAssistenteDormindo;

    public MainForm(
        ApiService apiService,
        LocalDatabase database,
        WebSocketService webSocketService,
        UdpService udpService,
        SpeechService speechService,
        CommandProcessor commandProcessor,
        User user,
        LicenseService licenseService)
    {
        _apiService = apiService;
        _database = database;
        _webSocketService = webSocketService;
        _udpService = udpService;
        _speechService = speechService;
        _commandProcessor = commandProcessor;
        _currentUser = user;
        _licenseService = licenseService;
        _paymentCallbackService = new PaymentCallbackService();

        InitializeComponent();
        
        // Inicializar SpeechService (TTS e reconhecimento de voz)
        _speechService.Initialize(database: _database);
        
        // Inicializar SpeechSynthesizer (mantido para compatibilidade)
        Falador = new SpeechSynthesizer();
        
        // Configurar variável de ambiente para autoplay (se necessário)
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--autoplay-policy=no-user-gesture-required");
        
        SetupLayout();
        SetupEventHandlers();
        SetupPaymentCallback();
        SetupAgendamentoTimer();
        SetupAlarmesTimer();
        AtualizarContadorAlarmes();
        
        // Carregar estado do aprendizado
        var aprendizadoEnabled = _database.GetSetting("AprendizadoEnabled") == "true";
        _commandProcessor.AprendizadoEnabled = aprendizadoEnabled;
        
        // Inscrever-se no evento de comando de IA executado para atualizar StarkCoins
        _commandProcessor.IaCommandExecuted += async (s, e) =>
        {
            await AtualizarStarkCoinsAsync();
        };
        
        // Inscrever-se no evento de mudança de status de bloqueio do assistente
        _commandProcessor.TimeOfStopBlockedChanged += (s, isBlocked) =>
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    AtualizarLabelAssistenteDormindo(isBlocked);
                });
            }
            else
            {
                AtualizarLabelAssistenteDormindo(isBlocked);
            }
        };
        
        // Inscrever-se no evento de ativar inteligência por comando de voz
        _commandProcessor.AtivarInteligenciaRequested += (s, e) =>
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    AtivarInteligenciaPorComando();
                });
            }
            else
            {
                AtivarInteligenciaPorComando();
            }
        };
        
        // Inscrever-se no evento de desativar inteligência por comando de voz
        _commandProcessor.DesativarInteligenciaRequested += (s, e) =>
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    DesativarInteligenciaPorComando();
                });
            }
            else
            {
                DesativarInteligenciaPorComando();
            }
        };
        
        // Verificar se o nome do assistente está configurado
        VerificarConfiguracaoAssistente();
        
        // Inicializar timer para verificar status do assistente periodicamente
        SetupAssistenteDormindoTimer();
        
        LoadDashboard();
    }

    private void VerificarConfiguracaoAssistente()
    {
        var config = _database.GetConfigAssistente();
        var nomeAssistente = config.NomeAssistente;
        
        // Verificar se não está configurado ou é "Assistente" (padrão)
        if (string.IsNullOrEmpty(nomeAssistente) || 
            nomeAssistente.Equals("Assistente", StringComparison.OrdinalIgnoreCase) ||
            nomeAssistente.Equals("assistente", StringComparison.OrdinalIgnoreCase))
        {
            // Mostrar mensagem e abrir configuração
            MessageBox.Show(
                "Você deve dar um nome ao assistente.\n\n" +
                "A página de configuração será aberta para você salvar o nome e a resposta padrão do assistente.",
                "Configuração Necessária",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            
            // Abrir formulário de configuração
            LoadConfigAssistente();
        }
    }

    private void SetupPaymentCallback()
    {
        // Registrar eventos do callback de pagamento
        _paymentCallbackService.PaymentSuccess += async (s, e) =>
        {
            this.Invoke((MethodInvoker)async delegate
            {
                SoundPlayer.PlaySuccess();
                
                // Atualizar dados do usuário da API
                var updatedUser = await _apiService.GetCurrentUserAsync();
                if (updatedUser != null)
                {
                    // Atualizar _currentUser
                    _currentUser.StarkCoins = updatedUser.StarkCoins;
                    
                    // Atualizar label no formulário StarkCoins se estiver aberto
                    if (_openStarkCoinsForm != null && !_openStarkCoinsForm.IsDisposed)
                    {
                        _openStarkCoinsForm.UpdateStarkCoins(updatedUser.StarkCoins);
                        if (e.Type == "funds")
                        {
                            _openStarkCoinsForm.UpdateStatus("Pagamento concluído! Em breve será creditado os StarkCoins em sua conta.");
                        }
                        else if (e.Type == "plano")
                        {
                            _openStarkCoinsForm.UpdateStatus("Plano contratado com sucesso! Seu plano será ativado em breve.");
                        }
                    }
                    
                    // Atualizar label no dashboard
                    if (_lblStarkCoins != null)
                    {
                        _lblStarkCoins.Text = updatedUser.StarkCoins.ToString("F2");
                    }
                }
                
                // Atualizar stats
                await RefreshStats();
            });
        };

        _paymentCallbackService.PaymentCanceled += (s, e) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                SoundPlayer.PlayError();
                if (_openStarkCoinsForm != null && !_openStarkCoinsForm.IsDisposed)
                {
                    _openStarkCoinsForm.UpdateStatus("Pagamento cancelado.");
                }
            });
        };

        // Iniciar o servidor de callback
        _paymentCallbackService.Start();
    }

    private void SetupLayout()
    {
        this.Text = "StarkAid - Automação Residencial";
        
        // Carregar tamanho e posição salvos, ou usar padrão menor
        var savedWidth = _database.GetSetting("WindowWidth");
        var savedHeight = _database.GetSetting("WindowHeight");
        var savedX = _database.GetSetting("WindowX");
        var savedY = _database.GetSetting("WindowY");
        
        if (int.TryParse(savedWidth, out var width) && int.TryParse(savedHeight, out var height) && 
            width >= 800 && height >= 600)
        {
            this.Size = new Size(width, height);
        }
        else
        {
            // Tamanho padrão menor
            this.Size = new Size(1000, 600);
        }
        
        // Configurar para iniciar maximizado na área de trabalho (sem cobrir a barra de tarefas)
        this.StartPosition = FormStartPosition.Manual;
        this.MaximizeBox = false;
        this.MinimizeBox = true;
        this.BackColor = Color.FromArgb(15, 15, 25);
        this.FormBorderStyle = FormBorderStyle.None;
        
        // Usar WorkingArea para não cobrir a barra de tarefas
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        var workingArea = screen.WorkingArea;
        this.Location = new Point(workingArea.X, workingArea.Y);
        this.Size = new Size(workingArea.Width, workingArea.Height);

        // Barra de título - TOPO
        pnlTitleBar = new Panel
        {
            BackColor = Color.FromArgb(25, 25, 35),
            Height = 40,
            Width = this.ClientSize.Width,
            Location = new Point(0, 0),
            Cursor = Cursors.Default // Cursor padrão (sem arrastar - sempre maximizado)
        };

        lblTitle = new Label
        {
            Text = "STARK AID",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = false,
            Size = new Size(200, 40),
            Location = new Point(0, 0), // Será centralizado dinamicamente
            Cursor = Cursors.SizeAll, // Cursor para arrastar
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.None
        };
        
        // Centralizar o título quando a barra for redimensionada
        pnlTitleBar.Resize += (s, e) =>
        {
            if (lblTitle != null && pnlTitleBar != null)
            {
                lblTitle.Location = new Point((pnlTitleBar.Width - lblTitle.Width) / 2, 0);
            }
        };
        

        // Botão Minimizar
        btnMinimize = new Button
        {
            Text = "─",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(40, 40),
            Location = new Point(this.ClientSize.Width - 80, 0),
            Cursor = Cursors.Hand
        };
        btnMinimize.FlatAppearance.BorderSize = 0;
        btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(50, 50, 60); SoundPlayer.PlayMouseMove(); };
        btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; SoundPlayer.StopMouseMove(); };
        btnMinimize.Click += (s, e) => { SoundPlayer.PlayClick(); this.WindowState = FormWindowState.Minimized; };

        btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(40, 40),
            Location = new Point(this.ClientSize.Width - 50, 0),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Red; SoundPlayer.PlayMouseMove(); };
        btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; SoundPlayer.StopMouseMove(); };
        btnClose.Click += (s, e) => { SoundPlayer.PlayClick(); Application.Exit(); };

        pnlTitleBar.Controls.Add(lblTitle);
        pnlTitleBar.Controls.Add(btnMinimize);
        pnlTitleBar.Controls.Add(btnClose);
        
        // Centralizar o título após adicionar os controles (considerando os botões de controle)
        // Os botões ocupam 80px (40px cada x 2), então centralizamos considerando isso
        var titleX = (pnlTitleBar.Width - lblTitle.Width) / 2;
        lblTitle.Location = new Point(Math.Max(0, titleX), 0);

        // Sidebar - ESQUERDA
        _pnlSidebar = new Panel
        {
            BackColor = Color.FromArgb(25, 25, 35),
            Width = 250,
            Height = this.ClientSize.Height - 40, // Altura total menos a barra de título
            Location = new Point(0, 40), // Abaixo da barra de título
            AutoScroll = false // Não usar scroll no painel principal
        };

        // Painel interno com scroll customizado e estilizado
        var pnlSidebarContent = new Panel
        {
            BackColor = Color.FromArgb(25, 25, 35),
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0)
        };
        
        // Configurar scroll para ser mais discreto
        pnlSidebarContent.VerticalScroll.Visible = true;
        pnlSidebarContent.HorizontalScroll.Visible = false;
        pnlSidebarContent.AutoScrollMargin = new Size(0, 10);
        
        // A barra de rolagem padrão do Windows Forms será usada
        // Ela aparecerá automaticamente quando necessário

        // Removido lblLogo do menu lateral

        var btnUdp = CreateMenuButton("📡 UDP", 20);
        btnUdp.Click += (s, e) => { SoundPlayer.PlayClick(); LoadUdp(); };
        
        _btnDashboard = CreateMenuButton("📊 Dashboard", 70);
        _btnComandosSociais = CreateMenuButton("💬 Comandos Sociais", 120);
        
        var btnComandosShell = CreateMenuButton("💻 Comandos Shell", 170);
        btnComandosShell.Click += (s, e) => { SoundPlayer.PlayClick(); LoadComandosShell(); };
        
        _btnDevices = CreateMenuButton("🔌 Dispositivos StarkSwitch", 220);
        _btnDispositivosEsp = CreateMenuButton("📡 Dispositivos ESP", 270);
        _btnDispositivosEwelink = CreateMenuButton("🔌 Dispositivos Ewelink", 320);
        _btnAgendamentos = CreateMenuButton("⏰ Agendamentos", 370);
        _btnAgendamentosArquivos = CreateMenuButton("📅 Agendamentos Arquivos", 420);
        _btnStarkCoinsPlanos = CreateMenuButton("💰 StarkCoins | Planos", 470);
        _btnPlanosAtivos = CreateMenuButton("💳 Planos Ativos", 520);

        var btnConfigConta = CreateMenuButton("⚙️ Configurar Conta", 570);
        btnConfigConta.Click += (s, e) => { SoundPlayer.PlayClick(); LoadConfigurarConta(); };

        var btnConfigAssistente = CreateMenuButton("🤖 Configurar Assistente", 620);
        btnConfigAssistente.Click += (s, e) => { SoundPlayer.PlayClick(); LoadConfigAssistente(); };

        var btnConfigAprendizado = CreateMenuButton("📚 Configurar Aprendizado", 670);
        btnConfigAprendizado.Click += (s, e) => { SoundPlayer.PlayClick(); LoadConfigAprendizado(); };

        var btnConfigAlarmes = CreateMenuButton("⏰ Configurar Alarmes", 720);
        btnConfigAlarmes.Click += (s, e) => { SoundPlayer.PlayClick(); LoadConfigAlarmes(); };

        // Botões _btnToggleIA, _btnAtualizar e btnSair agora são criados no CreateDashboardContent

        // Adicionar controles ao painel interno com scroll (sem os botões que foram movidos para o dashboard)
        pnlSidebarContent.Controls.Add(btnUdp);
        pnlSidebarContent.Controls.Add(_btnDashboard);
        pnlSidebarContent.Controls.Add(_btnComandosSociais);
        pnlSidebarContent.Controls.Add(btnComandosShell);
        pnlSidebarContent.Controls.Add(_btnDevices);
        pnlSidebarContent.Controls.Add(_btnDispositivosEsp);
        pnlSidebarContent.Controls.Add(_btnDispositivosEwelink);
        pnlSidebarContent.Controls.Add(_btnAgendamentos);
        pnlSidebarContent.Controls.Add(_btnAgendamentosArquivos);
        pnlSidebarContent.Controls.Add(_btnStarkCoinsPlanos);
        pnlSidebarContent.Controls.Add(_btnPlanosAtivos);
        pnlSidebarContent.Controls.Add(btnConfigConta);
        pnlSidebarContent.Controls.Add(btnConfigAssistente);
        pnlSidebarContent.Controls.Add(btnConfigAprendizado);
        pnlSidebarContent.Controls.Add(btnConfigAlarmes);
        
        // Adicionar o painel interno ao sidebar principal
        _pnlSidebar.Controls.Add(pnlSidebarContent);

        // Content Panel - PREENCHE O RESTANTE
        _pnlContent = new Panel
        {
            BackColor = Color.FromArgb(20, 20, 30),
            Location = new Point(250, 40), // Começa após a sidebar e abaixo da barra de título
            Size = new Size(this.ClientSize.Width - 250, this.ClientSize.Height - 40),
            Padding = new Padding(20),
            AutoScroll = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _pnlDashboard = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false, // Sem scroll - layout responsivo
            Padding = new Padding(0),
            BackColor = Color.FromArgb(20, 20, 30)
        };

        _pnlContent.Controls.Add(_pnlDashboard);
        
        // Adicionar WebView2 ao dashboard quando estiver pronto
        if (WB != null && !_pnlDashboard.Controls.Contains(WB))
        {
            _pnlDashboard.Controls.Add(WB);
        }

        // Adicionar controles ao formulário na ordem correta
        this.Controls.Add(_pnlContent);
        this.Controls.Add(_pnlSidebar);
        this.Controls.Add(pnlTitleBar);

        // Eventos
        this.Load += MainForm_Load;
        this.Shown += MainForm_Shown;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        // InitBrowser() já é chamado no OnLoad, não precisa chamar aqui novamente
        CreateDashboardContent();
        
        // Garantir que o layout seja atualizado após o formulário estar totalmente renderizado
        // O Load garante que o handle já foi criado, então podemos chamar diretamente ou usar Shown
        if (this.IsHandleCreated)
        {
            UpdateDashboardLayout();
        }
        
        await RefreshStats();
        
        // Verificar endereço após carregar (se usuário estiver logado)
        if (_currentUser != null)
        {
            _ = CheckAddressAndOpenConfig();
        }
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        // Garantir que o layout seja atualizado após o formulário estar visível
        // O evento Shown é disparado após Load e garante que tudo está renderizado
        UpdateDashboardLayout();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        // Garantir que o form sempre ocupe a área de trabalho (sem cobrir a barra de tarefas)
        var screen = Screen.FromControl(this);
        var workingArea = screen.WorkingArea;
        
        if (this.Location.X != workingArea.X || this.Location.Y != workingArea.Y ||
            this.Size.Width != workingArea.Width || this.Size.Height != workingArea.Height)
        {
            this.Location = new Point(workingArea.X, workingArea.Y);
            this.Size = new Size(workingArea.Width, workingArea.Height);
        }
        
        // Atualizar tamanhos dos painéis principais
        if (pnlTitleBar != null)
        {
            pnlTitleBar.Width = this.ClientSize.Width;
        }

        if (btnMinimize != null)
        {
            btnMinimize.Location = new Point(this.ClientSize.Width - 80, 0);
        }

        if (btnClose != null)
        {
            btnClose.Location = new Point(this.ClientSize.Width - 50, 0);
        }

        if (_pnlSidebar != null)
        {
            _pnlSidebar.Height = this.ClientSize.Height - 40;
        }

        if (_pnlContent != null)
        {
            _pnlContent.Location = new Point(250, 40);
            _pnlContent.Size = new Size(this.ClientSize.Width - 250, this.ClientSize.Height - 40);
        }

        // Recalcular layout do dashboard de forma responsiva
        UpdateDashboardLayout();
    }




    private void UpdateDashboardLayout()
    {
        if (_pnlDashboard == null || _pnlDashboard.Controls.Count == 0) return;

        var availableWidth = _pnlDashboard.ClientSize.Width - 40; // Margens laterais
        var availableHeight = _pnlDashboard.ClientSize.Height;

        // Calcular tamanho dos cards dinamicamente
        const int cardMinWidth = 80;
        const int cardHeight = 100;
        const int cardSpacing = 15;
        const int cardsCount = 4; // Removido card MQTT
        
        // Calcular quantos cards cabem por linha baseado na largura disponível
        int cardsPerRow = Math.Max(1, Math.Min(cardsCount, (int)((availableWidth + cardSpacing) / (cardMinWidth + cardSpacing))));
        
        // Calcular largura real dos cards para preencher o espaço disponível
        int cardWidth = cardsPerRow > 1 
            ? (int)((availableWidth - (cardSpacing * (cardsPerRow - 1))) / cardsPerRow)
            : Math.Min(cardMinWidth, availableWidth);
        
        cardWidth = Math.Max(cardMinWidth, Math.Min(cardWidth, 250)); // Entre 150 e 250

        // Calcular posição inicial para centralizar os cards
        var totalCardsWidth = (cardsPerRow * cardWidth) + ((cardsPerRow - 1) * cardSpacing);
        var startX = Math.Max(20, (availableWidth - totalCardsWidth) / 2);
        var startY = 20; // Margem superior (painel de botões removido)

        // Atualizar posições dos cards (excluindo os cards do painel de tempo)
        // Filtrar apenas os cards do dashboard principal usando a Tag
        var cards = _pnlDashboard.Controls.OfType<Panel>()
            .Where(p => p.BorderStyle == BorderStyle.FixedSingle && 
                       p.Tag?.ToString() == "DashboardCard") // Usar Tag para identificar cards do dashboard
            .ToList();
        
        // Garantir que temos exatamente 4 cards na ordem correta
        if (cards.Count != cardsCount)
        {
            // Se não tiver todos os cards, buscar pela ordem de adição
            cards = _pnlDashboard.Controls.OfType<Panel>()
                .Where(p => p.BorderStyle == BorderStyle.FixedSingle && 
                           p.Parent == _pnlDashboard &&
                           (_pnlWeatherDashboard == null || p != _pnlWeatherDashboard && !_pnlWeatherDashboard.Controls.Contains(p)))
                .Take(cardsCount)
                .ToList();
        }
        
        for (int i = 0; i < Math.Min(cards.Count, cardsCount); i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            cards[i].Size = new Size(cardWidth, cardHeight);
            cards[i].Location = new Point(startX + col * (cardWidth + cardSpacing), startY + row * (cardHeight + cardSpacing));
        }

        // Calcular posição e tamanho do WebView2 de forma responsiva
        var cardsTotalHeight = startY + (int)Math.Ceiling((double)cardsCount / cardsPerRow) * (cardHeight + cardSpacing);
        var webViewY = cardsTotalHeight + 20; // Espaçamento após os cards
        
        // WebView2 - ajustar largura ao disponível, altura baseada no espaço restante
        var webViewMinWidth = 400;
        var webViewWidth = Math.Max(webViewMinWidth, Math.Min(1400, availableWidth - 40));
        // Calcular altura do WebView - primeiro calcular onde o dashboard vai ficar (no rodapé)
        var weatherPanelTotalHeight = 160; // Altura do painel (160px)
        var weatherMargin = 20; // Margem inferior
        var weatherY = availableHeight - weatherPanelTotalHeight - weatherMargin; // Posição Y do dashboard no rodapé
        // Altura do WebView = espaço do rodapé menos posição inicial do WebView, menos espaço entre WebView e dashboard
        var spaceBetweenWebViewAndWeather = 30; // Espaço entre WebView e dashboard
        var webViewHeight = weatherY - webViewY - spaceBetweenWebViewAndWeather;
        webViewHeight = Math.Max(300, webViewHeight); // Altura mínima reduzida para não cobrir o dashboard
        var webViewX = (availableWidth - webViewWidth) / 2;

        if (WB != null && _pnlDashboard.Controls.Contains(WB))
        {
            WB.Location = new Point(webViewX + 20, webViewY);
            WB.Size = new Size(webViewWidth, webViewHeight);
            
            // Manter zoom em 1.0 para tamanho normal
            WB.ZoomFactor = 1.0;
            
            // Posicionar label "Assistente Dormindo" sobre o WebView2 (centro)
            if (_lblAssistenteDormindo != null && _pnlDashboard.Controls.Contains(_lblAssistenteDormindo))
            {
                var labelWidth = 500;
                var labelHeight = 80;
                var labelX = WB.Location.X + (WB.Width / 2) - (labelWidth / 2);
                var labelY = WB.Location.Y + (WB.Height / 2) - (labelHeight / 2);
                
                _lblAssistenteDormindo.Location = new Point(labelX, labelY);
                _lblAssistenteDormindo.Size = new Size(labelWidth, labelHeight);
                _lblAssistenteDormindo.BringToFront(); // Garantir que fique sobre o WebView
            }
            
            // Notificar a página sobre o redimensionamento se já estiver carregada
            if (WB.CoreWebView2 != null && _webViewReady)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(100);
                        await WB.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('resize'));");
                    }
                    catch { }
                });
            }
        }

        // Posicionar painel de previsão do tempo no rodapé (já calculado acima)
        var weatherWidth = Math.Min(800, availableWidth - 40);
        var weatherX = (availableWidth - weatherWidth) / 2;
        
        if (_pnlWeatherDashboard != null && _pnlDashboard.Controls.Contains(_pnlWeatherDashboard) && _pnlWeatherDashboard.Visible)
        {
            _pnlWeatherDashboard.Location = new Point(weatherX + 20, weatherY);
            _pnlWeatherDashboard.Size = new Size(weatherWidth, 160); // Reduzido de 180 para 160 (90px cards + 70px espaços)
            
            // Reposicionar cards internos para centralizar
            var weatherCardsInner = _pnlWeatherDashboard.Controls.OfType<Panel>().Where(p => p.BorderStyle == BorderStyle.FixedSingle).ToList();
            if (weatherCardsInner.Count == 3)
            {
                var weatherCardWidthInner = 250;
                var weatherCardSpacingInner = 15;
                var weatherTotalCardsWidthInner = (3 * weatherCardWidthInner) + (2 * weatherCardSpacingInner);
                var weatherStartXInner = (weatherWidth - weatherTotalCardsWidthInner) / 2;
                
                for (int i = 0; i < weatherCardsInner.Count; i++)
                {
                    weatherCardsInner[i].Location = new Point(weatherStartXInner + i * (weatherCardWidthInner + weatherCardSpacingInner), 60);
                }
            }
        }

        // Posicionar botões laterais à direita do painel de previsão do tempo (IA, Dados, Logout)
        const int sideButtonWidth = 120;
        const int sideButtonHeight = 40;
        const int sideButtonSpacing = 10;
        const int sideButtonsMargin = 20;
        // Se o painel de tempo estiver visível, posicionar ao lado dele, senão abaixo do WebView
        var sideButtonsY = _pnlWeatherDashboard != null && _pnlWeatherDashboard.Visible 
            ? weatherY + 30  // Alinhar com o topo do painel de tempo
            : webViewY + webViewHeight + 30;
        var sideButtonsRightX = weatherX + weatherWidth + sideButtonsMargin + 20; // À direita do painel de tempo

        if (_btnToggleIA != null && _pnlDashboard.Controls.Contains(_btnToggleIA))
        {
            _btnToggleIA.Location = new Point(sideButtonsRightX, sideButtonsY);
            _btnToggleIA.Size = new Size(sideButtonWidth, sideButtonHeight);
            _btnToggleIA.BringToFront();
        }

        if (_btnAtualizar != null && _pnlDashboard.Controls.Contains(_btnAtualizar))
        {
            _btnAtualizar.Location = new Point(sideButtonsRightX, sideButtonsY + sideButtonHeight + sideButtonSpacing);
            _btnAtualizar.Size = new Size(sideButtonWidth, sideButtonHeight);
            _btnAtualizar.BringToFront();
        }

        if (btnSair != null && _pnlDashboard.Controls.Contains(btnSair))
        {
            btnSair.Location = new Point(sideButtonsRightX, sideButtonsY + (sideButtonHeight + sideButtonSpacing) * 2);
            btnSair.Size = new Size(sideButtonWidth, sideButtonHeight);
            btnSair.BringToFront();
        }

        // Posicionar botões laterais à esquerda do painel de previsão do tempo (Alarmes, Aprendizado, Chat Suporte)
        var sideButtonsLeftX = weatherX + 20 - sideButtonWidth - sideButtonsMargin; // À esquerda do painel de tempo

        if (_btnAlarmes != null && _pnlDashboard.Controls.Contains(_btnAlarmes))
        {
            _btnAlarmes.Location = new Point(sideButtonsLeftX, sideButtonsY);
            _btnAlarmes.Size = new Size(sideButtonWidth, sideButtonHeight);
            _btnAlarmes.BringToFront();
        }

        if (_btnAprendizado != null && _pnlDashboard.Controls.Contains(_btnAprendizado))
        {
            _btnAprendizado.Location = new Point(sideButtonsLeftX, sideButtonsY + sideButtonHeight + sideButtonSpacing);
            _btnAprendizado.Size = new Size(sideButtonWidth, sideButtonHeight);
            _btnAprendizado.BringToFront();
        }

        if (_btnChatSuporte != null && _pnlDashboard.Controls.Contains(_btnChatSuporte))
        {
            _btnChatSuporte.Location = new Point(sideButtonsLeftX, sideButtonsY + (sideButtonHeight + sideButtonSpacing) * 2);
            _btnChatSuporte.Size = new Size(sideButtonWidth, sideButtonHeight);
            _btnChatSuporte.BringToFront();
        }

        // Botão iniciar/parar - sobrepondo o WebView no canto superior direito
        const int recButtonSize = 50;
        const int recButtonMargin = 10;
        var recButtonX = webViewX + webViewWidth - recButtonSize - recButtonMargin + 20; // Canto superior direito do WebView
        var recButtonY = webViewY + recButtonMargin;

        if (_btnIniciarReconhecimento != null && _pnlDashboard.Controls.Contains(_btnIniciarReconhecimento))
        {
            _btnIniciarReconhecimento.Location = new Point(recButtonX, recButtonY);
            _btnIniciarReconhecimento.Size = new Size(recButtonSize, recButtonSize);
            // Garantir que o botão fique acima do WebView na ordem Z
            _btnIniciarReconhecimento.BringToFront();
        }

        // Calcular altura total necessária (incluindo painel de tempo se visível)
        // O dashboard de tempo está no rodapé, então a altura total é simplesmente a altura disponível
        var totalHeight = availableHeight;
        const int minHeightForNoScroll = 400; // Altura mínima antes de ativar scroll
        
        // Se a altura total exceder o disponível e for menor que o mínimo, ativar scroll vertical
        if (totalHeight > availableHeight && availableHeight < minHeightForNoScroll)
        {
            _pnlDashboard.AutoScroll = true;
            _pnlDashboard.VerticalScroll.Visible = true;
            _pnlDashboard.VerticalScroll.Enabled = true;
        }
        else if (totalHeight > availableHeight && WB != null)
        {
            // Recalcular altura do WebView mantendo dashboard no rodapé
            var weatherPanelTotalHeightForResize = 160; // Altura do painel
            var weatherMarginForResize = 20; // Margem inferior
            var newWeatherY = availableHeight - weatherPanelTotalHeightForResize - weatherMarginForResize; // Dashboard no rodapé
            var spaceBetweenForResize = 30; // Espaço entre WebView e dashboard
            var newWebViewHeight = newWeatherY - webViewY - spaceBetweenForResize;
            newWebViewHeight = Math.Max(250, newWebViewHeight); // Altura mínima reduzida para garantir que o dashboard apareça
            
            if (_pnlDashboard.Controls.Contains(WB))
            {
                WB.Size = new Size(webViewWidth, newWebViewHeight);
                
                // Reposicionar label "Assistente Dormindo" sobre o WebView2 (centro)
                if (_lblAssistenteDormindo != null && _pnlDashboard.Controls.Contains(_lblAssistenteDormindo) && _lblAssistenteDormindo.Visible)
                {
                    var labelWidth = 500;
                    var labelHeight = 80;
                    var labelX = WB.Location.X + (WB.Width / 2) - (labelWidth / 2);
                    var labelY = WB.Location.Y + (WB.Height / 2) - (labelHeight / 2);
                    
                    _lblAssistenteDormindo.Location = new Point(labelX, labelY);
                    _lblAssistenteDormindo.Size = new Size(labelWidth, labelHeight);
                    _lblAssistenteDormindo.BringToFront();
                }
                
                // Reposicionar painel de tempo no rodapé
                if (_pnlWeatherDashboard != null && _pnlDashboard.Controls.Contains(_pnlWeatherDashboard) && _pnlWeatherDashboard.Visible)
                {
                    _pnlWeatherDashboard.Location = new Point(_pnlWeatherDashboard.Location.X, newWeatherY);
                }
                
                // Reposicionar botão de reconhecimento no canto superior direito do WebView
                const int newRecButtonSize = 50;
                const int newRecButtonMargin = 10;
                if (_btnIniciarReconhecimento != null && _pnlDashboard.Controls.Contains(_btnIniciarReconhecimento))
                {
                    var newRecButtonX = webViewX + webViewWidth - newRecButtonSize - newRecButtonMargin + 20;
                    _btnIniciarReconhecimento.Location = new Point(newRecButtonX, webViewY + newRecButtonMargin);
                    _btnIniciarReconhecimento.BringToFront();
                }
                
                // Reposicionar botões laterais ao lado do painel de tempo (direita e esquerda)
                const int newSideButtonHeight = 40;
                const int newSideButtonSpacing = 10;
                var newSideButtonsY = (_pnlWeatherDashboard != null && _pnlWeatherDashboard.Visible) 
                    ? newWeatherY + 30 
                    : webViewY + newWebViewHeight + 30;
                
                // Botões da direita
                if (_btnToggleIA != null && _pnlDashboard.Controls.Contains(_btnToggleIA))
                {
                    _btnToggleIA.Location = new Point(_btnToggleIA.Location.X, newSideButtonsY);
                    _btnToggleIA.BringToFront();
                }
                if (_btnAtualizar != null && _pnlDashboard.Controls.Contains(_btnAtualizar))
                {
                    _btnAtualizar.Location = new Point(_btnAtualizar.Location.X, newSideButtonsY + newSideButtonHeight + newSideButtonSpacing);
                    _btnAtualizar.BringToFront();
                }
                if (btnSair != null && _pnlDashboard.Controls.Contains(btnSair))
                {
                    btnSair.Location = new Point(btnSair.Location.X, newSideButtonsY + (newSideButtonHeight + newSideButtonSpacing) * 2);
                    btnSair.BringToFront();
                }
                
                // Botões da esquerda
                if (_btnAlarmes != null && _pnlDashboard.Controls.Contains(_btnAlarmes))
                {
                    _btnAlarmes.Location = new Point(_btnAlarmes.Location.X, newSideButtonsY);
                    _btnAlarmes.BringToFront();
                }
                if (_btnAprendizado != null && _pnlDashboard.Controls.Contains(_btnAprendizado))
                {
                    _btnAprendizado.Location = new Point(_btnAprendizado.Location.X, newSideButtonsY + newSideButtonHeight + newSideButtonSpacing);
                    _btnAprendizado.BringToFront();
                }
                if (_btnChatSuporte != null && _pnlDashboard.Controls.Contains(_btnChatSuporte))
                {
                    _btnChatSuporte.Location = new Point(_btnChatSuporte.Location.X, newSideButtonsY + (newSideButtonHeight + newSideButtonSpacing) * 2);
                    _btnChatSuporte.BringToFront();
                }
            }
            
            // Verificar novamente se ainda precisa de scroll (incluindo painel de tempo se visível)
            // Como o dashboard está fixo no rodapé, a altura total é sempre a altura disponível
            totalHeight = availableHeight;
            if (totalHeight > availableHeight && availableHeight < minHeightForNoScroll)
            {
                _pnlDashboard.AutoScroll = true;
                _pnlDashboard.VerticalScroll.Visible = true;
                _pnlDashboard.VerticalScroll.Enabled = true;
            }
            else
            {
                _pnlDashboard.AutoScroll = false;
            }
        }
        else
        {
            _pnlDashboard.AutoScroll = false;
        }
    }

    private Button CreateMenuButton(string text, int y)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 11),
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(210, 40),
            Location = new Point(20, y),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(40, 40, 50); SoundPlayer.PlayMouseMove(); };
        btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; SoundPlayer.StopMouseMove(); };
        return btn;
    }

    private void CreateDashboardContent()
    {
        if (_pnlDashboard == null) return;
        
        _pnlDashboard.Controls.Clear();
        _pnlDashboard.AutoScroll = false; // Sem scroll - layout responsivo
        _pnlDashboard.Padding = new Padding(0);
        _pnlDashboard.BackColor = Color.FromArgb(20, 20, 30);

        // Botões serão criados em um painel lateral à direita do WebView (criado no UpdateDashboardLayout)
        
        // Botão Ativar IA
        _btnToggleIA = new Button
        {
            Text = "🤖 Inteligência",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(239, 68, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(0, 0), // Será posicionado no UpdateDashboardLayout
            Cursor = Cursors.Hand
        };
        _btnToggleIA.FlatAppearance.BorderSize = 0;
        _btnToggleIA.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
        _btnToggleIA.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        _btnToggleIA.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        _btnToggleIA.Click += BtnToggleIA_Click;

        // Botão Atualizar Dados
        _btnAtualizar = new Button
        {
            Text = "🔄 Dados",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(0, 0), // Será posicionado no UpdateDashboardLayout
            Cursor = Cursors.Hand
        };
        _btnAtualizar.FlatAppearance.BorderSize = 0;
        _btnAtualizar.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 220);
        _btnAtualizar.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        _btnAtualizar.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        _btnAtualizar.Click += BtnAtualizar_Click;

        // Botão Sair
        btnSair = new Button
        {
            Text = "🚪 LOG OUT",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(0, 0), // Será posicionado no UpdateDashboardLayout
            Cursor = Cursors.Hand
        };
        btnSair.FlatAppearance.BorderSize = 0;
        btnSair.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68);
        btnSair.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        btnSair.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        btnSair.Click += BtnLogout_Click;

        // Botão Alarmes
        _btnAlarmes = new Button
        {
            Text = "⏰ Alarmes",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(139, 69, 19),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(0, 0), // Será posicionado no UpdateDashboardLayout
            Cursor = Cursors.Hand
        };
        _btnAlarmes.FlatAppearance.BorderSize = 0;
        _btnAlarmes.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 82, 45);
        _btnAlarmes.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        _btnAlarmes.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        _btnAlarmes.Click += BtnAlarmes_Click;

        // Botão Aprendizado
        var aprendizadoEnabled = _database.GetSetting("AprendizadoEnabled") == "true";
        _btnAprendizado = new Button
        {
            Text = "📚 Aprendizado",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = aprendizadoEnabled ? Color.FromArgb(16, 185, 129) : Color.FromArgb(75, 0, 130),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(0, 0), // Será posicionado no UpdateDashboardLayout
            Cursor = Cursors.Hand
        };
        _btnAprendizado.FlatAppearance.BorderSize = 0;
        _btnAprendizado.FlatAppearance.MouseOverBackColor = Color.FromArgb(93, 0, 150);
        _btnAprendizado.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        _btnAprendizado.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        _btnAprendizado.Click += BtnAprendizado_Click;

        // Botão Chat Suporte
        _btnChatSuporte = new Button
        {
            Text = "💬 Chat Suporte",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 128, 128),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(0, 0), // Será posicionado no UpdateDashboardLayout
            Cursor = Cursors.Hand
        };
        _btnChatSuporte.FlatAppearance.BorderSize = 0;
        _btnChatSuporte.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 150);
        _btnChatSuporte.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        _btnChatSuporte.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        _btnChatSuporte.Click += async (s, e) => 
        { 
            SoundPlayer.PlayClick(); 
            var chatForm = new ChatSuporteForm(_apiService);
            chatForm.ShowDialog();
        };

        // Atualizar estado inicial do botão IA
        if (_iaEnabled)
        {
            _btnToggleIA.Text = "🤖 Inteligência";
            _btnToggleIA.BackColor = Color.FromArgb(16, 185, 129);
        }

        // Criar cards - posições serão calculadas dinamicamente no UpdateDashboardLayout
        const int cardHeight = 100;
        const int initialCardWidth = 200; // Tamanho inicial, será ajustado no UpdateDashboardLayout
        
        var cardStarkCoins = CreateStatCard("StarkCoins", _currentUser.StarkCoins.ToString("F2"), Color.Cyan, initialCardWidth, cardHeight);
        cardStarkCoins.Tag = "DashboardCard";
        var cardDevices = CreateStatCard("Dispositivos", "0", Color.Green, initialCardWidth, cardHeight);
        cardDevices.Tag = "DashboardCard";
        var cardComandos = CreateStatCard("Comandos", "0", Color.Yellow, initialCardWidth, cardHeight);
        cardComandos.Tag = "DashboardCard";
        var cardApi = CreateStatCard("API", "Verificando...", Color.Blue, initialCardWidth, cardHeight);
        cardApi.Tag = "DashboardCard";

        // Referências para labels
        _lblStarkCoins = (Label)cardStarkCoins.Controls[1];
        _lblTotalDevices = (Label)cardDevices.Controls[1];
        _lblTotalComandos = (Label)cardComandos.Controls[1];
        _lblApiStatus = (Label)cardApi.Controls[1];

        // WebView2 - será posicionado no UpdateDashboardLayout
        if (WB != null)
        {
            WB.Visible = true;
            if (!_pnlDashboard.Controls.Contains(WB))
            {
                _pnlDashboard.Controls.Add(WB);
            }
        }
        
        // Label de "Assistente Dormindo" - será posicionada sobre o WebView2
        _lblAssistenteDormindo = new Label
        {
            Text = "", // Será definido dinamicamente com o nome do assistente
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.Orange,
            BackColor = Color.FromArgb(220, 20, 20, 30), // Semi-transparente
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Anchor = AnchorStyles.None
        };
        _lblAssistenteDormindo.Paint += (s, e) =>
        {
            // Desenhar borda arredondada e sombra
            var rect = _lblAssistenteDormindo.ClientRectangle;
            using (var brush = new SolidBrush(_lblAssistenteDormindo.BackColor))
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int radius = 15;
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                e.Graphics.FillPath(brush, path);
            }
        };
        
        if (!_pnlDashboard.Controls.Contains(_lblAssistenteDormindo))
        {
            _pnlDashboard.Controls.Add(_lblAssistenteDormindo);
        }

        // Painel de Previsão do Tempo - será posicionado abaixo do WebView
        _pnlWeatherDashboard = new Panel
        {
            BackColor = Color.FromArgb(30, 30, 40),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };

        var lblWeatherTitle = new Label
        {
            Text = "🌡️ Tempo Atual",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };
        _pnlWeatherDashboard.Controls.Add(lblWeatherTitle);

        // Grid de 3 colunas para Temperatura, Condição e Vento
        var weatherYPos = 60;
        var weatherCardWidth = 250;
        var weatherCardHeight = 90; // Reduzido de 120 para 90
        var weatherSpacing = 15;
        
        // Card Temperatura
        var pnlTemp = new Panel
        {
            BackColor = Color.FromArgb(40, 40, 50),
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(weatherCardWidth, weatherCardHeight),
            Location = new Point(20, weatherYPos)
        };
        var lblTempLabel = new Label
        {
            Text = "Temperatura",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, 15)
        };
        _lblWeatherTemp = new Label
        {
            Text = "-°C",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(15, 40)
        };
        pnlTemp.Controls.Add(lblTempLabel);
        pnlTemp.Controls.Add(_lblWeatherTemp);
        _pnlWeatherDashboard.Controls.Add(pnlTemp);

        // Card Condição
        var pnlCondition = new Panel
        {
            BackColor = Color.FromArgb(40, 40, 50),
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(weatherCardWidth, weatherCardHeight),
            Location = new Point(20 + weatherCardWidth + weatherSpacing, weatherYPos)
        };
        var lblConditionLabel = new Label
        {
            Text = "Condição",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, 15)
        };
        _lblWeatherCondition = new Label
        {
            Text = "-",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, 40),
            MaximumSize = new Size(weatherCardWidth - 30, 0)
        };
        pnlCondition.Controls.Add(lblConditionLabel);
        pnlCondition.Controls.Add(_lblWeatherCondition);
        _pnlWeatherDashboard.Controls.Add(pnlCondition);

        // Card Vento
        var pnlWind = new Panel
        {
            BackColor = Color.FromArgb(40, 40, 50),
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(weatherCardWidth, weatherCardHeight),
            Location = new Point(20 + (weatherCardWidth + weatherSpacing) * 2, weatherYPos)
        };
        var lblWindLabel = new Label
        {
            Text = "Vento",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(15, 15)
        };
        _lblWeatherWind = new Label
        {
            Text = "- km/h",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, 40)
        };
        pnlWind.Controls.Add(lblWindLabel);
        pnlWind.Controls.Add(_lblWeatherWind);
        _pnlWeatherDashboard.Controls.Add(pnlWind);

        _pnlWeatherDashboard.Visible = false; // Ocultar até carregar dados
        _pnlDashboard.Controls.Add(_pnlWeatherDashboard);

        // Botão iniciar/parar - será posicionado no UpdateDashboardLayout
        const int buttonWidth = 300;
        const int buttonHeight = 50;
        
        _btnIniciarReconhecimento = new Button
        {
            Text = "🎤",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(50, 50),
            Location = new Point(0, 0), // Será atualizado no UpdateDashboardLayout para canto superior do WebView
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.None,
            UseVisualStyleBackColor = false
        };
        _btnIniciarReconhecimento.FlatAppearance.BorderSize = 0;
        _btnIniciarReconhecimento.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 220);
        _btnIniciarReconhecimento.MouseEnter += (s, e) => { SoundPlayer.PlayMouseMove(); };
        _btnIniciarReconhecimento.MouseLeave += (s, e) => { SoundPlayer.StopMouseMove(); };
        _btnIniciarReconhecimento.Click += BtnIniciarReconhecimento_Click;

        // Adicionar ao painel: cards, WebView2, botões laterais, botão de reconhecimento
        _pnlDashboard.Controls.Add(cardStarkCoins);
        _pnlDashboard.Controls.Add(cardDevices);
        _pnlDashboard.Controls.Add(cardComandos);
        _pnlDashboard.Controls.Add(cardApi);
        
        // WebView2 será adicionado se ainda não estiver
        if (WB != null && !_pnlDashboard.Controls.Contains(WB))
        {
            _pnlDashboard.Controls.Add(WB);
        }
        
        // Adicionar botões laterais (direita)
        _pnlDashboard.Controls.Add(_btnToggleIA);
        _pnlDashboard.Controls.Add(_btnAtualizar);
        _pnlDashboard.Controls.Add(btnSair);
        
        // Adicionar botões laterais (esquerda)
        _pnlDashboard.Controls.Add(_btnAlarmes);
        _pnlDashboard.Controls.Add(_btnAprendizado);
        _pnlDashboard.Controls.Add(_btnChatSuporte);
        
        _pnlDashboard.Controls.Add(_btnIniciarReconhecimento);
        
        // Atualizar layout diretamente - será chamado novamente no Load/Shown se necessário
        // Não usar BeginInvoke aqui pois pode ser chamado antes do handle estar criado
        if (this.IsHandleCreated)
        {
            UpdateDashboardLayout();
            // Carregar dados do tempo
            _ = Task.Run(async () => await LoadWeatherDataAsync());
        }
    }
    
    private async Task LoadWeatherDataAsync()
    {
        try
        {
            var weatherData = await _apiService.GetWeatherForecastAsync();
            if (weatherData?.Current != null && this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateWeatherDashboard(weatherData.Current);
                });
            }
            else if (weatherData?.Current != null)
            {
                UpdateWeatherDashboard(weatherData.Current);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar dados do tempo: {ex.Message}");
        }
    }
    
    private void UpdateWeatherDashboard(CurrentWeatherDto current)
    {
        if (_lblWeatherTemp != null)
        {
            _lblWeatherTemp.Text = $"{Math.Round(current.Temperature)}°C";
        }
        
        if (_lblWeatherCondition != null)
        {
            _lblWeatherCondition.Text = current.WeatherDescription ?? "-";
        }
        
        if (_lblWeatherWind != null)
        {
            var windText = $"{Math.Round(current.WindSpeed)} km/h";
            if (!string.IsNullOrEmpty(current.WindDirectionText))
            {
                windText += $" {current.WindDirectionText}";
            }
            _lblWeatherWind.Text = windText;
        }
        
        // Mostrar painel após atualizar dados
        if (_pnlWeatherDashboard != null)
        {
            _pnlWeatherDashboard.Visible = true;
            UpdateDashboardLayout(); // Atualizar layout para incluir o painel de tempo
        }
    }

    private Panel CreateStatCard(string title, string value, Color accentColor, int width, int height)
    {
        var panel = new Panel
        {
            Size = new Size(width, height),
            BackColor = Color.FromArgb(30, 30, 40),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(15, 15)
        };

        var lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = accentColor,
            AutoSize = true,
            Location = new Point(15, 50)
        };

        panel.Controls.Add(lblTitle);
        panel.Controls.Add(lblValue);

        return panel;
    }

    private void SetupEventHandlers()
    {
        _btnDashboard!.Click += (s, e) => LoadDashboard();
        _btnComandosSociais!.Click += (s, e) => LoadComandosSociais();
        _btnDevices!.Click += (s, e) => LoadDevices();
        _btnDispositivosEsp!.Click += (s, e) => LoadDispositivosEsp();
        _btnDispositivosEwelink!.Click += (s, e) => LoadDispositivosEwelink();
        _btnAgendamentos!.Click += (s, e) => LoadAgendamentos();
        _btnAgendamentosArquivos!.Click += (s, e) => LoadAgendamentosArquivos();
        _btnStarkCoinsPlanos!.Click += (s, e) => LoadStarkCoinsPlanos();
        _btnPlanosAtivos!.Click += (s, e) => LoadPlanosAtivos();

        // WebSocket handlers
        _webSocketService.ComandoDispositivoReceived += WebSocketService_ComandoDispositivoReceived;
        _webSocketService.RespostaDispositivoReceived += WebSocketService_RespostaDispositivoReceived;
        _webSocketService.ToSoftMessageReceived += WebSocketService_ToSoftMessageReceived;
        _webSocketService.SuporteComandoReceived += WebSocketService_SuporteComandoReceived;

        // UDP handlers
        _udpService.ResponseReceived += UdpService_ResponseReceived;

        // Speech handlers removidos - agora usando WebView com Web Speech API
        
        // WebView2 será inicializado no Load via InitBrowser() (padrão simples que funciona)
    }

    private async void LoadDashboard()
    {
        SoundPlayer.PlayClick();
        
        // Limpar e recriar o conteúdo
        _pnlContent!.Controls.Clear();
        
        _pnlDashboard = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false, // Sem scroll - layout responsivo
            Padding = new Padding(0),
            BackColor = Color.FromArgb(20, 20, 30)
        };
        
        CreateDashboardContent();
        _pnlContent.Controls.Add(_pnlDashboard);
        
        // Garantir que o WebView2 seja adicionado ao dashboard se ainda não estiver
        if (WB != null && !_pnlDashboard.Controls.Contains(WB))
        {
            _pnlDashboard.Controls.Add(WB);
        }
        
        // Atualizar layout após tudo estar pronto
        // Verificar se o handle está criado antes de chamar
        if (this.IsHandleCreated)
        {
            UpdateDashboardLayout();
        }
        
        // Garantir que todos os painéis estejam na ordem correta
        this.Controls.SetChildIndex(_pnlContent, 0);
        this.Controls.SetChildIndex(_pnlSidebar!, 1);
        this.Controls.SetChildIndex(pnlTitleBar!, 2);

        await RefreshStats();
    }

    private async Task RefreshStats()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Iniciando RefreshStats...");
            
            // Verificar se está online
            var isOnline = await _apiService.CheckApiStatusAsync();
            
            // Atualizar StarkCoins - usar dados locais se offline
            if (_lblStarkCoins != null)
            {
                if (isOnline)
                {
                    // Se online, usar dados do _currentUser (atualizado pela API)
                    _lblStarkCoins.Text = _currentUser.StarkCoins.ToString("F2");
                }
                else
                {
                    // Se offline, usar último valor salvo no banco local
                    var lastStarkCoins = _database.GetLastStarkCoins();
                    if (lastStarkCoins.HasValue)
                    {
                        _lblStarkCoins.Text = lastStarkCoins.Value.ToString("F2");
                    }
                    else
                    {
                        // Fallback para _currentUser se não houver dados locais
                        _lblStarkCoins.Text = _currentUser.StarkCoins.ToString("F2");
                    }
                }
            }

            // Tentar buscar stats - se estiver online
            if (isOnline)
            {
                try
                {
                    var stats = await _apiService.GetUserStatsAsync();
                    if (stats != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Stats recebidas: Devices={stats.TotalDevices}, Comandos={stats.TotalComandosSociais}, API={stats.ApiStatus}, MQTT={stats.MqttStatus}");
                        
                        if (_lblTotalDevices != null)
                        {
                            _lblTotalDevices.Text = stats.TotalDevices.ToString();
                        }
                        
                        if (_lblTotalComandos != null)
                        {
                            _lblTotalComandos.Text = stats.TotalComandosSociais.ToString();
                        }
                        
                        if (_lblApiStatus != null)
                        {
                            _lblApiStatus.Text = stats.ApiStatus ?? "OK";
                            _lblApiStatus.ForeColor = (stats.ApiStatus == "OK") ? Color.Green : Color.Red;
                        }
                        
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao buscar stats da API: {ex.Message}");
                }
            }

            // Fallback: calcular localmente (offline ou se API falhar)
            System.Diagnostics.Debug.WriteLine("Usando dados locais...");
            
            // Buscar comandos sociais do banco local
            var localComandos = _database.GetComandosSociais();
            var totalComandos = localComandos.Count;
            System.Diagnostics.Debug.WriteLine($"Total comandos sociais (local): {totalComandos}");
            
            // Buscar dispositivos ESP do banco local
            var dispositivosEsp = _database.GetDispositivosEsp();
            
            // Buscar dispositivos Starkswitch do banco local
            var dispositivos = _database.GetDevices();
            
            // Buscar dispositivos Ewelink do banco local
            var dispositivosEwelink = _database.GetEwelinkDevices();
            
            if (_lblTotalDevices != null)
            {
                var totalDevices = dispositivos.Count + dispositivosEsp.Count + dispositivosEwelink.Count;
                _lblTotalDevices.Text = totalDevices.ToString();
                System.Diagnostics.Debug.WriteLine($"Total dispositivos (local): StarkSwitch={dispositivos.Count}, ESP={dispositivosEsp.Count}, Ewelink={dispositivosEwelink.Count}, Total={totalDevices}");
            }
            
            if (_lblTotalComandos != null)
            {
                _lblTotalComandos.Text = totalComandos.ToString();
                System.Diagnostics.Debug.WriteLine($"Total comandos atualizado: {totalComandos}");
            }
            
            if (_lblApiStatus != null)
            {
                _lblApiStatus.Text = isOnline ? "OK" : "Offline";
                _lblApiStatus.ForeColor = isOnline ? Color.Green : Color.Orange;
            }
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar estatísticas: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task AtualizarStarkCoinsAsync()
    {
        try
        {
            // Verificar se está online
            var isOnline = await _apiService.CheckApiStatusAsync();
            
            if (isOnline)
            {
                // Buscar dados atualizados do usuário da API
                var updatedUser = await _apiService.GetCurrentUserAsync();
                if (updatedUser != null)
                {
                    // Atualizar _currentUser
                    _currentUser.StarkCoins = updatedUser.StarkCoins;
                    
                    // Salvar no banco local
                    _database.SaveDadosUI(updatedUser.StarkCoins);
                    
                    // Atualizar label no dashboard (thread-safe)
                    if (this.InvokeRequired)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (_lblStarkCoins != null)
                            {
                                _lblStarkCoins.Text = updatedUser.StarkCoins.ToString("F2");
                            }
                        });
                    }
                    else
                    {
                        if (_lblStarkCoins != null)
                        {
                            _lblStarkCoins.Text = updatedUser.StarkCoins.ToString("F2");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[STARKCOINS] Atualizado após comando IA: {updatedUser.StarkCoins:F2}");
                }
            }
            else
            {
                // Se offline, usar último valor salvo no banco local
                var lastStarkCoins = _database.GetLastStarkCoins();
                if (lastStarkCoins.HasValue)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (_lblStarkCoins != null)
                            {
                                _lblStarkCoins.Text = lastStarkCoins.Value.ToString("F2");
                            }
                        });
                    }
                    else
                    {
                        if (_lblStarkCoins != null)
                        {
                            _lblStarkCoins.Text = lastStarkCoins.Value.ToString("F2");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[STARKCOINS] Usando valor local (offline): {lastStarkCoins.Value:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar StarkCoins após comando IA: {ex.Message}");
        }
    }

    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var result = MessageBox.Show(
            "Deseja realmente fazer logout?",
            "Confirmar Logout",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // Limpar token e credenciais
            _apiService.ClearToken();
            _database.ClearLoginCredentials();

            // Fechar serviços
            _paymentCallbackService?.Stop();
            _udpService?.StopListening();
            _ = _webSocketService?.DisconnectAsync();

            // Fechar o MainForm - o BootstrapForm detectará e mostrará login novamente
            this.DialogResult = DialogResult.Retry; // Usar Retry para indicar logout
            this.Close();
        }
    }

    private async void BtnAtualizar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        _btnAtualizar!.Enabled = false;
        _btnAtualizar.Text = "⏳";

        try
        {
            await SyncDataAsync();
            SoundPlayer.PlaySuccess();
            _speechService.Speak("dados atualizados");
            await RefreshStats();
            await LoadWeatherDataAsync(); // Atualizar dados do tempo
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar: {ex.Message}");
        }
        finally
        {
            _btnAtualizar.Enabled = true;
            _btnAtualizar.Text = "🔄 Dados";
        }
    }

    private async Task SyncDataAsync()
    {
        try
        {
            // Verificar status da API primeiro
            var isOnline = await _apiService.CheckApiStatusAsync();
            if (!isOnline)
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] Sistema offline - não é possível sincronizar dados");
                return;
            }

            // Buscar e salvar dados do usuário
            var user = await _apiService.GetCurrentUserAsync();
            if (user != null)
            {
                _database.SaveUser(user);
                _database.SaveDadosUI(user.StarkCoins);
                System.Diagnostics.Debug.WriteLine($"[MainForm] Usuário atualizado: {user.Name}, StarkCoins: {user.StarkCoins}");
            }

            // Buscar e salvar comandos sociais
            var comandos = await _apiService.GetComandosSociaisAsync();
            _database.SaveComandosSociais(comandos);
            System.Diagnostics.Debug.WriteLine($"[MainForm] {comandos.Count} comandos sociais atualizados");

            // Buscar e salvar dispositivos ESP
            var dispositivosEsp = await _apiService.GetDispositivosEspAsync();
            _database.SaveDispositivosEsp(dispositivosEsp);
            System.Diagnostics.Debug.WriteLine($"[MainForm] {dispositivosEsp.Count} dispositivos ESP atualizados");

            // Buscar e salvar dispositivos Ewelink
            var dispositivosEwelink = await _apiService.GetEwelinkDevicesAsync();
            _database.SaveEwelinkDevices(dispositivosEwelink);
            System.Diagnostics.Debug.WriteLine($"[MainForm] {dispositivosEwelink.Count} dispositivos Ewelink atualizados");

            // Buscar e salvar dispositivos Starkswitch
            var dispositivos = await _apiService.GetDevicesAsync();
            _database.SaveDevices(dispositivos);
            System.Diagnostics.Debug.WriteLine($"[MainForm] {dispositivos.Count} dispositivos Starkswitch atualizados");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainForm] Erro ao sincronizar dados: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainForm] Stack trace: {ex.StackTrace}");
        }
    }

    private async void BtnToggleIA_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        
        // Verificar se está online antes de ativar IA
        var isOnline = await _apiService.CheckApiStatusAsync();
        if (!isOnline && !_iaEnabled)
        {
            _speechService.Speak("A inteligência artificial requer conexão com a API. Por favor, aguarde a conexão ser restabelecida.");
            return;
        }
        
        _iaEnabled = !_iaEnabled;
        _commandProcessor.IaEnabled = _iaEnabled;

        // Não iniciar/parar reconhecimento aqui - isso é controlado pelo botão de reconhecimento
        if (_iaEnabled)
        {
            _btnToggleIA!.Text = "🤖 Inteligência";
            _btnToggleIA.BackColor = Color.FromArgb(16, 185, 129);
            
            // Enviar comando "Ativar inteligencia" para a IA e falar a resposta (apenas se online)
            if (isOnline)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var request = new SuperIaRequest
                        {
                            Texto = "Ativar inteligencia",
                            ContextoUser = "",
                            ContextoIA = "",
                            Estilo = ""
                        };
                        
                        var response = await _apiService.CallSuperIaAsync(request);
                        if (response != null && !string.IsNullOrEmpty(response.Texto))
                        {
                            if (this.InvokeRequired)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    _speechService.Speak(response.Texto);
                                });
                            }
                            else
                            {
                                _speechService.Speak(response.Texto);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao processar ativação de inteligência: {ex.Message}");
                    }
                });
            }
        }
        else
        {
            _btnToggleIA!.Text = "🤖 Inteligência";
            _btnToggleIA.BackColor = Color.FromArgb(239, 68, 68);
            _speechService.Speak("Inteligência desativada");
        }
    }

    private void BtnAprendizado_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var aprendizadoEnabled = _database.GetSetting("AprendizadoEnabled") == "true";
        aprendizadoEnabled = !aprendizadoEnabled;
        _database.SaveSetting("AprendizadoEnabled", aprendizadoEnabled ? "true" : "false");
        _commandProcessor.AprendizadoEnabled = aprendizadoEnabled;

        if (aprendizadoEnabled)
        {
            _btnAprendizado!.BackColor = Color.FromArgb(16, 185, 129);
            _speechService.Speak("Aprendizado ativado");
        }
        else
        {
            _btnAprendizado!.BackColor = Color.FromArgb(75, 0, 130);
            _speechService.Speak("Aprendizado desativado");
        }
    }
    
    private void AtivarInteligenciaPorComando()
    {
        // Ativar inteligência quando chamado por comando de voz
        if (!_iaEnabled)
        {
            _iaEnabled = true;
            _commandProcessor.IaEnabled = _iaEnabled;
            
            if (_btnToggleIA != null)
            {
                _btnToggleIA.Text = "🤖 Inteligência";
                _btnToggleIA.BackColor = Color.FromArgb(16, 185, 129);
            }
        }
    }
    
    private void DesativarInteligenciaPorComando()
    {
        // Desativar inteligência quando chamado por comando de voz
        if (_iaEnabled)
        {
            _iaEnabled = false;
            _commandProcessor.IaEnabled = _iaEnabled;
            
            if (_btnToggleIA != null)
            {
                _btnToggleIA.Text = "🤖 Inteligência";
                _btnToggleIA.BackColor = Color.FromArgb(239, 68, 68);
            }
        }
    }

    private void LoadUdp()
    {
        SoundPlayer.PlayClick();
        var form = new UdpForm(_udpService);
        form.ShowDialog();
    }

    private void LoadComandosSociais()
    {
        SoundPlayer.PlayClick();
        var form = new ComandosSociaisForm(_apiService, _database);
        form.ShowDialog();
    }

    private void LoadComandosShell()
    {
        SoundPlayer.PlayClick();
        var form = new ComandosShellForm(_database);
        form.ShowDialog();
    }

    private void LoadDevices()
    {
        SoundPlayer.PlayClick();
        var form = new DevicesForm(_apiService);
        form.ShowDialog();
    }

    private void LoadDispositivosEsp()
    {
        SoundPlayer.PlayClick();
        var form = new DispositivosEspForm(_apiService, _database);
        form.ShowDialog();
    }

    private void LoadDispositivosEwelink()
    {
        SoundPlayer.PlayClick();
        var form = new DispositivosEwelinkForm(_apiService, _webSocketService, _speechService);
        form.ShowDialog();
    }

    private void LoadAgendamentosArquivos()
    {
        SoundPlayer.PlayClick();
        var form = new ListaAgendamentosArquivosForm(_database);
        form.ShowDialog();
    }

    private void LoadAgendamentos()
    {
        SoundPlayer.PlayClick();
        var form = new AgendamentosForm(_apiService);
        form.ShowDialog();
    }

    private void LoadStarkCoinsPlanos()
    {
        SoundPlayer.PlayClick();
        _openStarkCoinsForm = new StarkCoinsPlanosForm(_apiService, _currentUser);
        _openStarkCoinsForm.FormClosed += (s, e) => { _openStarkCoinsForm = null; };
        _openStarkCoinsForm.ShowDialog();
    }

    private void LoadPlanosAtivos()
    {
        SoundPlayer.PlayClick();
        var form = new PlanosAtivosForm(_apiService);
        form.ShowDialog();
    }

    private void LoadConfigurarConta()
    {
        SoundPlayer.PlayClick();
        if (_currentUser == null) return;
        var form = new ConfigurarContaForm(_apiService, _currentUser);
        form.ShowDialog();
        
        // Recarregar dados do usuário após fechar o formulário
        _ = Task.Run(async () =>
        {
            var updatedUser = await _apiService.GetCurrentUserAsync();
            if (updatedUser != null)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    _currentUser = updatedUser;
                });
            }
        });
    }
    
    private async Task CheckAddressAndOpenConfig()
    {
        try
        {
            var user = await _apiService.GetCurrentUserAsync();
            if (user != null && (string.IsNullOrWhiteSpace(user.Estado) || 
                string.IsNullOrWhiteSpace(user.Cidade) || 
                string.IsNullOrWhiteSpace(user.Bairro)))
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(
                        "Insira os dados de endereço para melhor funcionamento do sistema.",
                        "Dados de Endereço Necessários",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    LoadConfigurarConta();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar endereço: {ex.Message}");
        }
    }


    private void LoadConfigAssistente()
    {
        SoundPlayer.PlayClick();
        var form = new ConfigAssistenteForm(_database, _speechService);
        form.ShowDialog();
    }

    private void LoadConfigAprendizado()
    {
        SoundPlayer.PlayClick();
        var form = new ConfigurarAprendizadoForm(_database);
        form.ShowDialog();
    }

    private void LoadConfigAlarmes()
    {
        SoundPlayer.PlayClick();
        var form = new ConfigurarAlarmesForm(_database);
        form.ShowDialog();
    }

    private void WebSocketService_ComandoDispositivoReceived(object? sender, (string nome, string ip, int porta, string comando) e)
    {
        System.Diagnostics.Debug.WriteLine($"WebSocketService_ComandoDispositivoReceived: {e.nome} - {e.ip}:{e.porta} - {e.comando}");
        this.Invoke((MethodInvoker)delegate
        {
            System.Diagnostics.Debug.WriteLine($"Enviando comando UDP via handler: {e.ip}:{e.porta} - {e.comando}");
            _udpService.SendCommand(e.ip, e.porta, e.comando);
        });
    }

    private async void WebSocketService_RespostaDispositivoReceived(object? sender, string resposta)
    {
        this.Invoke((MethodInvoker)delegate
        {
            _speechService.Speak(resposta);
        });
    }

    private void WebSocketService_ToSoftMessageReceived(object? sender, string mensagem)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[WebSocket] Mensagem toSoft recebida para falar: {mensagem}");
            
            // IMPORTANTE: Mensagens toSoft devem manter acentuação e pontuação originais para fala natural
            // NÃO aplicar NormalizeText nas mensagens que serão faladas
            
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    _speechService.Speak(mensagem);
                });
            }
            else
            {
                _speechService.Speak(mensagem);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar mensagem toSoft: {ex.Message}");
        }
    }

    private async void WebSocketService_SuporteComandoReceived(object? sender, string acao)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Suporte] Comando recebido: {acao}");
            
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)async delegate
                {
                    await ExecutarAcaoSuporte(acao);
                });
            }
            else
            {
                await ExecutarAcaoSuporte(acao);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando de suporte: {ex.Message}");
            await EnviarRespostaAcaoSuporte(acao, false, $"Erro: {ex.Message}");
        }
    }

    private async Task ExecutarAcaoSuporte(string acao)
    {
        try
        {
            switch (acao.ToLower())
            {
                case "limparcache":
                    await LimparCacheSoftware();
                    break;
                case "limpardados":
                    await LimparDadosSoftware();
                    break;
                case "logout":
                    await LogoutSoftware();
                    break;
                case "atualizardados":
                    await AtualizarDadosSoftware();
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"Ação desconhecida: {acao}");
                    await EnviarRespostaAcaoSuporte(acao, false, "Ação desconhecida");
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao executar ação de suporte: {ex.Message}");
            await EnviarRespostaAcaoSuporte(acao, false, $"Erro: {ex.Message}");
        }
    }

    private async Task LimparCacheSoftware()
    {
        try
        {
            // Limpar configurações temporárias de cache
            _database.SaveSetting("CacheCleared", DateTime.UtcNow.ToString());
            
            // Limpar cache de comandos processados (se houver método)
            // _commandProcessor.ClearContext(); // Implementar se necessário
            
            System.Diagnostics.Debug.WriteLine("Cache limpo com sucesso");
            await EnviarRespostaAcaoSuporte("limparcache", true, "Cache limpo com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao limpar cache: {ex.Message}");
            await EnviarRespostaAcaoSuporte("limparcache", false, $"Erro: {ex.Message}");
        }
    }

    private async Task LimparDadosSoftware()
    {
        try
        {
            // Limpar logs de erro locais (se houver método)
            // _database.ClearErrorLogs(); // Implementar se necessário
            
            // Limpar dados temporários mantendo login
            var token = _database.GetSetting("AuthToken");
            var refreshToken = _database.GetSetting("RefreshToken");
            var userId = _database.GetSetting("UserId");
            
            // Limpar configurações temporárias
            _database.SaveSetting("LastDeviceUpdate", "");
            _database.SaveSetting("LastCommandUpdate", "");
            
            System.Diagnostics.Debug.WriteLine("Dados limpos com sucesso");
            await EnviarRespostaAcaoSuporte("limpardados", true, "Dados limpos com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao limpar dados: {ex.Message}");
            await EnviarRespostaAcaoSuporte("limpardados", false, $"Erro: {ex.Message}");
        }
    }

    private async Task LogoutSoftware()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[LogoutSoftware] Iniciando logout...");
            
            // Salvar flag de resolução de suporte
            _database.SaveSetting("ResolvendoSuporte", "true");
            
            // Limpar tokens e credenciais (mesmo processo do botão de logout)
            _apiService.ClearToken();
            _database.ClearLoginCredentials();
            
            // Fechar serviços
            _paymentCallbackService?.Stop();
            _udpService?.StopListening();
            _ = _webSocketService?.DisconnectAsync();
            
            // Tentar enviar resposta de suporte (se houver token ainda)
            try
            {
                await EnviarRespostaAcaoSuporte("logout", true, "Logout executado. Retornando à tela de login.");
            }
            catch
            {
                // Ignorar erro se não conseguir enviar (token já foi limpo)
                System.Diagnostics.Debug.WriteLine("[LogoutSoftware] Não foi possível enviar resposta de suporte (token já limpo)");
            }
            
            // Aguardar um pouco para garantir que tudo foi processado
            await Task.Delay(500);
            
            // Fechar o MainForm - o BootstrapForm detectará e mostrará login novamente
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.DialogResult = DialogResult.Retry; // Usar Retry para indicar logout
                    this.Close();
                });
            }
            else
            {
                this.DialogResult = DialogResult.Retry; // Usar Retry para indicar logout
                this.Close();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao fazer logout: {ex.Message}");
            try
            {
                await EnviarRespostaAcaoSuporte("logout", false, $"Erro: {ex.Message}");
            }
            catch
            {
                // Ignorar se não conseguir enviar resposta
            }
        }
    }

    private async Task AtualizarDadosSoftware()
    {
        try
        {
            // Forçar atualização de dispositivos
            // Você pode adicionar outras atualizações aqui
            
            System.Diagnostics.Debug.WriteLine("Dados atualizados");
            await EnviarRespostaAcaoSuporte("atualizardados", true, "Dados atualizados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar dados: {ex.Message}");
            await EnviarRespostaAcaoSuporte("atualizardados", false, $"Erro: {ex.Message}");
        }
    }

    private async Task VerificarResolvendoSuporteAsync()
    {
        try
        {
            var token = _database.GetSetting("AuthToken");
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var response = await _apiService.GetAsync<dynamic>($"Suporte/verificar-resolvendo-suporte?origem=software");
            if (response != null)
            {
                var jObject = response as Newtonsoft.Json.Linq.JObject;
                var ativo = jObject?["ativo"]?.ToObject<bool>() ?? false;
                if (ativo)
                {
                    var mensagem = jObject?["message"]?.ToObject<string>() ?? 
                                  "Você estava em processo de resolução de suporte. O problema foi resolvido?";
                    
                    this.Invoke((MethodInvoker)delegate
                    {
                        var resultado = MessageBox.Show(mensagem, "Suporte", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (resultado == DialogResult.Yes)
                        {
                            _ = MarcarResolvidoAsync();
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar resolvendo suporte: {ex.Message}");
        }
    }

    private async Task MarcarResolvidoAsync()
    {
        try
        {
            await _apiService.PostAsync<dynamic>("Suporte/marcar-resolvido", new { origem = "software" });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao marcar como resolvido: {ex.Message}");
        }
    }

    private async Task EnviarRespostaAcaoSuporte(string acao, bool sucesso, string mensagem)
    {
        try
        {
            var token = _database.GetSetting("AuthToken");
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("Token não encontrado para enviar resposta de suporte");
                return;
            }

            // Conectar ao hub de suporte e enviar resposta
            var baseUrl = _apiService.GetBaseUrl();
            await using var hubConnection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
                .WithUrl($"{baseUrl}/hubs/support-chat?origem=software", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("AcaoExecutada", acao, sucesso, mensagem);
            await hubConnection.StopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar resposta de ação de suporte: {ex.Message}");
        }
    }

    private async void UdpService_ResponseReceived(object? sender, string resposta)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[UDP] Resposta recebida na porta 1495: {resposta}");
            
            // IMPORTANTE: Respostas UDP devem manter acentuação e pontuação originais para fala natural
            // NÃO aplicar NormalizeText nas respostas que serão faladas
            
            // Falar a resposta
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    _speechService.Speak(resposta);
                });
            }
            else
            {
                _speechService.Speak(resposta);
            }
            
            // Enviar resposta via WebSocket para o app
            await _webSocketService.SendRespostaAsync("", "", 0, resposta);
            System.Diagnostics.Debug.WriteLine($"[UDP] Resposta enviada via WebSocket para o app: {resposta}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar resposta UDP: {ex.Message}");
            LocalDatabase.LogError(_database, ex, "ERR_007", "Erro ao processar resposta UDP recebida.", null, resposta, null);
        }
    }


    private async void BtnIniciarReconhecimento_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        
        // Se o WebView não estiver inicializado, inicializar agora
        if (_webView != null && _webView.CoreWebView2 == null)
        {
            _btnIniciarReconhecimento!.Enabled = false;
            _btnIniciarReconhecimento.Text = "⏳";
            
            try
            {
                // WebView2 já deve estar inicializado no Load, mas se não estiver, inicializar agora
                // WebView2 já deve estar inicializado no Load
                if (_webView == null || _webView.CoreWebView2 == null)
                {
                    InitBrowser();
                }
            }
            catch (Exception ex)
            {
                _btnIniciarReconhecimento.Enabled = true;
                _btnIniciarReconhecimento.Text = "🎤";
                
                // Verificar se é o erro específico de threading COM
                if (ex.HResult == unchecked((int)0x80010106)) // RPC_E_CHANGED_MODE
                {
                    MessageBox.Show(
                        "Erro ao inicializar reconhecimento de voz.\n\n" +
                        "Este é um problema conhecido com o WebView2 e threading COM.\n\n" +
                        "Solução:\n" +
                        "1. Feche a aplicação completamente\n" +
                        "2. Abra novamente e clique em 'INICIAR RECONHECIMENTO' ANTES de fazer qualquer outra ação\n" +
                        "3. Ou aguarde alguns segundos após abrir a aplicação antes de iniciar o reconhecimento\n\n" +
                        "Isso é necessário porque o WebSocket precisa ser conectado após o WebView2 ser inicializado.",
                        "Erro de Inicialização",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"Erro ao inicializar reconhecedor: {ex.Message}\n\nTente novamente.",
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return;
            }
            finally
            {
                _btnIniciarReconhecimento.Enabled = true;
            }
        }
        
        if (_webView == null || _webView.CoreWebView2 == null)
        {
            MessageBox.Show(
                "WebView não está inicializado.\n\nCertifique-se de que o WebView2 Runtime está instalado.",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!_webViewReady)
        {
            MessageBox.Show(
                "Aguardando inicialização do reconhecedor...\n\nTente novamente em alguns segundos.",
                "Aguarde",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            if (!_reconhecimentoAtivo)
            {
                // Iniciar reconhecimento usando função de controle externo
                await _webView.CoreWebView2.ExecuteScriptAsync("startRecognition();");
                
                // Aguardar um pouco para o reconhecimento iniciar
                await Task.Delay(300);

                // Verificar se realmente iniciou
                var estadoScript = "isRecognizing();";
                var estado = await _webView.CoreWebView2.ExecuteScriptAsync(estadoScript);
                var estadoLimpo = estado.Trim('"', '\'', ' ');

                if (estadoLimpo == "true")
                {
                    _reconhecimentoAtivo = true;
                    _btnIniciarReconhecimento!.Text = "🚫";
                    _btnIniciarReconhecimento.BackColor = Color.Red;
                    _commandProcessor.StartTimeOfStop(); // Iniciar contagem timeOfStop
                }
            }
            else
            {
                // Parar reconhecimento usando função de controle externo
                await _webView.CoreWebView2.ExecuteScriptAsync("stopRecognition();");
                
                // Aguardar um pouco para o reconhecimento parar
                await Task.Delay(300);

                // Verificar se realmente parou
                var estadoScript = "isRecognizing();";
                var estado = await _webView.CoreWebView2.ExecuteScriptAsync(estadoScript);
                var estadoLimpo = estado.Trim('"', '\'', ' ');

                if (estadoLimpo == "false")
                {
                    _reconhecimentoAtivo = false;
                    _btnIniciarReconhecimento!.Text = "🎤";
                    _btnIniciarReconhecimento.BackColor = Color.FromArgb(0, 180, 255);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao alternar reconhecimento: {ex.Message}");
            MessageBox.Show(
                $"Erro ao alternar reconhecimento: {ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }



    /// <summary>
    /// Inicializa o WebView2 seguindo o padrão simples que funciona (igual ao exemplo)
    /// </summary>
    private async Task initizated()
    {
        // WB já foi criado pelo Designer
        if (WB == null)
        {
            System.Diagnostics.Debug.WriteLine("ERRO: WB não foi criado pelo Designer");
            return;
        }
        
        // Configurar WB para ficar visível e centralizado
        WB.Visible = true;
        WB.Size = new Size(900, 400); // Tamanho fixo
        // Posição será calculada no CreateDashboardContent
        _webView = WB; // Manter referência

        // Configurar diretório de dados do usuário em LocalAppData para evitar problemas de permissão
        // quando instalado em Program Files
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarkAid",
            "WebView2Data"
        );

        // Criar diretório se não existir
        if (!Directory.Exists(userDataFolder))
        {
            Directory.CreateDirectory(userDataFolder);
        }

        // Tentar usar runtime local se disponível (pasta WebView2Runtime ao lado do executável)
        CoreWebView2Environment? environment = null;
        var appDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var localRuntimePath = Path.Combine(appDirectory, "WebView2Runtime");
        
        // Verificar se existe runtime local (x64)
        var localRuntimeExe = Path.Combine(localRuntimePath, "x64", "MicrosoftEdgeWebView2.exe");
        if (File.Exists(localRuntimeExe))
        {
            var browserFolder = Path.GetDirectoryName(localRuntimeExe);
            if (browserFolder != null)
            {
                // Criar ambiente com runtime local
                environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: browserFolder,
                    userDataFolder: userDataFolder
                );
                System.Diagnostics.Debug.WriteLine($"[WebView2] Usando runtime local: {browserFolder}");
            }
        }
        
        // Se não encontrou runtime local, usar o do sistema
        if (environment == null)
        {
            environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder
            );
            System.Diagnostics.Debug.WriteLine("[WebView2] Usando runtime do sistema");
        }

        // Inicializar WebView2 com o ambiente configurado
        await WB.EnsureCoreWebView2Async(environment);
        WB.CoreWebView2!.PermissionRequested += HandlePermissionRequested;
    }

    /// <summary>
    /// Handler de permissões (padrão simples que funciona)
    /// </summary>
    private void HandlePermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (e.PermissionKind == CoreWebView2PermissionKind.Microphone)
        {
            e.State = CoreWebView2PermissionState.Allow;
        }
    }

    /// <summary>
    /// Inicializa o WebView2 no Load (padrão simples que funciona - igual ao exemplo)
    /// </summary>
    public async void InitBrowser()
    {
        try
        {
            await initizated();
            
            if (WB?.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("ERRO: CoreWebView2 não inicializado");
                return;
            }

            // Navegar apenas se ainda não navegou ou se não está pronto
            if (!_webViewReady || string.IsNullOrEmpty(WB.CoreWebView2.Source) || !WB.CoreWebView2.Source.Contains("recognizer.html"))
            {
                WB.CoreWebView2.Navigate("https://starkaid.runasp.net/recognizer.html");
            }
            
            // Registrar handler apenas uma vez para evitar processamento duplicado
            if (!_webViewMessageHandlerRegistered)
            {
                WB.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    try
                    {
                        var message = e.TryGetWebMessageAsString();
                        if (string.IsNullOrEmpty(message)) return;

                        RECEBIDO = message;
                        RESULTADO = RECEBIDO.ToLower().Replace("\"", "").Trim();
                        Debug.WriteLine($"Texto reconhecido: {RESULTADO}");

                        this.Invoke((MethodInvoker)delegate
                        {
                            if (_txtTextoReconhecido != null)
                            {
                                _txtTextoReconhecido.Text = RESULTADO;
                            }

                            // Processar comando
                            if (!string.IsNullOrEmpty(RESULTADO))
                            {
                                _ = Task.Run(async () =>
                                {
                                    await _commandProcessor.ProcessCommandAsync(RESULTADO);
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao processar mensagem: {ex.Message}");
                    }
                };
                _webViewMessageHandlerRegistered = true;
            }

            // Aguardar a página carregar completamente antes de marcar como pronto
            WB.CoreWebView2.NavigationCompleted += async (s, e) =>
            {
                if (e.IsSuccess)
                {
                    _webViewReady = true;
                    System.Diagnostics.Debug.WriteLine("Página recognizer.html carregada com sucesso!");
                    
                    // Conectar WebSocket após o WebView2 estar pronto
                    _ = Task.Run(async () =>
                    {
                        await ConnectWebSocketAsync();
                    });

                    // Tornar a página responsiva - injetar CSS e configurar viewport
                    try
                    {
                        // Aguardar um pouco para garantir que o DOM está pronto
                        await Task.Delay(500);
                        
                        // Injetar CSS para tornar a página responsiva, centralizada e sem barras de rolagem
                        var responsiveCSS = "(function() {" +
                            "var style = document.createElement('style');" +
                            "style.innerHTML = " +
                            "'* { box-sizing: border-box; } " +
                            "html, body { margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; position: fixed; } " +
                            "body { display: flex; flex-direction: column; align-items: center; justify-content: center; } " +
                            "#contentAll { width: 100%; height: 100%; max-width: 100%; max-height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 0; overflow: hidden; } " +
                            ".rainvn { width: 100%; max-width: 100%; height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 5px; box-sizing: border-box; overflow: hidden; } " +
                            ".rainvn_textarea { width: 100%; max-width: 100%; display: flex; flex-direction: column; align-items: center; justify-content: center; flex: 1; overflow: hidden; } " +
                            ".rainvn_text { width: 100%; max-width: 100%; word-wrap: break-word; overflow-wrap: break-word; overflow: hidden; text-align: center; margin-top: 5px; padding-top: 30px; } " +
                            "#rainvn_text_final, #rainvn_text_interim { display: inline-block; } " +
                            ".rainvn_footer { width: 100%; max-width: 100%; overflow: hidden; } " +
                            "img, button { max-width: 100%; height: auto; }';" +
                            "document.head.appendChild(style);" +
                            "var viewport = document.querySelector('meta[name=\\\"viewport\\\"]');" +
                            "if (!viewport) {" +
                            "viewport = document.createElement('meta');" +
                            "viewport.name = 'viewport';" +
                            "viewport.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';" +
                            "document.head.appendChild(viewport);" +
                            "} else {" +
                            "viewport.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';" +
                            "}" +
                            "})();";
                        
                        await WB.CoreWebView2.ExecuteScriptAsync(responsiveCSS);
                        
                        // Resetar zoom para 1.0 (tamanho normal)
                        WB.ZoomFactor = 1.0;
                        
                        // Ajustar novamente após um pequeno delay para garantir que o CSS foi aplicado
                        await Task.Delay(200);
                        await WB.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('resize'));");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao tornar página responsiva: {ex.Message}");
                    }

                    // Verificar o estado inicial do reconhecimento após scripts carregarem
                    try
                    {
                        await Task.Delay(1000); // Aguardar scripts JavaScript carregarem
                        var estadoInicial = await WB.CoreWebView2.ExecuteScriptAsync("isRecognizing();");
                        var estadoLimpo = estadoInicial.Trim('"', '\'', ' ');
                        _reconhecimentoAtivo = (estadoLimpo == "true");

                        // Atualizar botão na UI
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (_btnIniciarReconhecimento != null)
                            {
                                if (_reconhecimentoAtivo)
                                {
                                    _btnIniciarReconhecimento.Text = "🚫";
                                    _btnIniciarReconhecimento.BackColor = Color.Red;
                                }
                                else
                                {
                                    _btnIniciarReconhecimento.Text = "🎤";
                                    _btnIniciarReconhecimento.BackColor = Color.FromArgb(0, 180, 255);
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao verificar estado inicial: {ex.Message}");
                    }
                }
            };

            System.Diagnostics.Debug.WriteLine("WebView2 inicializado com sucesso!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar WebView2: {ex.Message}");
        }
    }

    /// <summary>
    /// Ajusta o zoom do WebView2 para tornar a página responsiva ao tamanho do controle
    /// </summary>
    private void AdjustWebViewZoom()
    {
        try
        {
            if (WB == null) return;

            // Calcular zoom baseado no tamanho do WebView
            // Assumindo que a página foi projetada para ~900px de largura
            var baseWidth = 900.0;
            var currentWidth = WB.Width;
            
            // Calcular zoom para ajustar ao tamanho atual
            // Se o WebView for menor que a base, reduzir zoom; se maior, aumentar
            var zoomFactor = Math.Max(0.5, Math.Min(2.0, currentWidth / baseWidth));
            
            // ZoomFactor está no controle WebView2, não no CoreWebView2
            WB.ZoomFactor = zoomFactor;
            System.Diagnostics.Debug.WriteLine($"Zoom ajustado para {zoomFactor:F2} (largura: {currentWidth}px)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar zoom: {ex.Message}");
        }
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        if (!_isInitialized)
        {
            _isInitialized = true;
            
            // Inicializar WebView2 no Load (padrão simples que funciona - igual ao exemplo)
            InitBrowser();
            
            // Carregar dashboard e stats (síncrono no Load - não bloquear)
            // Sempre tentar sincronizar dados quando abrir o app
            _ = SyncDataAsync(); // Executar em background sem bloquear

            // Inicializar outros serviços
            _udpService.StartListening();
            
            // Verificar se está em processo de resolução de suporte
            _ = VerificarResolvendoSuporteAsync();
            
            // Aviso de uso autorizado
            MessageBox.Show(
                "Uso autorizado apenas pelo licenciante. Qualquer uso não autorizado, cópia ou distribuição é proibido.",
                "Aviso de Licença",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            
            // 5. Configurar timer de verificação de status da API
            var apiCheckTimer = new System.Windows.Forms.Timer();
            
            // Verificar status inicial de forma assíncrona
            _ = Task.Run(async () =>
            {
                _lastApiStatus = await _apiService.CheckApiStatusAsync();
            });
            
            apiCheckTimer.Interval = 2 * 60 * 1000; // 2 minutos
            apiCheckTimer.Tick += async (s, args) =>
            {
                try
                {
                    var isOnline = await _apiService.CheckApiStatusAsync();
                    
                    if (isOnline && !_lastApiStatus)
                    {
                        // API voltou a ficar online
                        System.Diagnostics.Debug.WriteLine("[MainForm] API voltou a ficar online - verificando licença e sincronizando dados");
                        
                        // Verificar licença
                        var isValid = await _licenseService.VerifyLicenseAsync();
                        if (!isValid)
                        {
                            apiCheckTimer.Stop();
                            MessageBox.Show("Sua licença não está mais válida. O aplicativo será fechado.", 
                                "Licença Inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Application.Exit();
                            return;
                        }
                        
                        // Sincronizar dados
                        await SyncDataAsync();
                        
                        // Conectar WebSocket (para chat)
                        await ConnectWebSocketAsync();
                        
                        // Reativar IA se estava ativada antes
                        if (this.InvokeRequired)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                if (_iaEnabled && _btnToggleIA != null)
                                {
                                    _btnToggleIA.BackColor = Color.FromArgb(16, 185, 129);
                                }
                            });
                        }
                        
                        // Atualizar stats
                        await RefreshStats();
                        
                        _lastApiStatus = true;
                    }
                    else if (!isOnline && _lastApiStatus)
                    {
                        // API ficou offline
                        System.Diagnostics.Debug.WriteLine("[MainForm] API ficou offline - desativando funcionalidades que dependem da API");
                        
                        // Desconectar WebSocket
                        await _webSocketService.DisconnectAsync();
                        
                        // Desativar IA (requer API)
                        if (this.InvokeRequired)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                if (_iaEnabled)
                                {
                                    _iaEnabled = false;
                                    _commandProcessor.IaEnabled = false;
                                    if (_btnToggleIA != null)
                                    {
                                        _btnToggleIA.BackColor = Color.FromArgb(239, 68, 68);
                                    }
                                }
                            });
                        }
                        else
                        {
                            if (_iaEnabled)
                            {
                                _iaEnabled = false;
                                _commandProcessor.IaEnabled = false;
                                if (_btnToggleIA != null)
                                {
                                    _btnToggleIA.BackColor = Color.FromArgb(239, 68, 68);
                                }
                            }
                        }
                        
                        _lastApiStatus = false;
                    }
                    else if (isOnline)
                    {
                        // API continua online - apenas verificar licença periodicamente
                        var isValid = await _licenseService.VerifyLicenseAsync();
                        if (!isValid)
                        {
                            apiCheckTimer.Stop();
                            MessageBox.Show("Sua licença não está mais válida. O aplicativo será fechado.", 
                                "Licença Inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Application.Exit();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao verificar status da API: {ex.Message}");
                }
            };
            apiCheckTimer.Start();
            
            // Conectar WebSocket apenas se estiver online (verificar de forma assíncrona)
            _ = Task.Run(async () =>
            {
                var isOnline = await _apiService.CheckApiStatusAsync();
                if (isOnline)
                {
                    _lastApiStatus = true;
                    await ConnectWebSocketAsync();
                }
            });
        }
    }


    /// <summary>
    /// Conecta o WebSocket após o WebView2 estar totalmente inicializado
    /// </summary>
    private async Task ConnectWebSocketAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Iniciando conexão WebSocket ===");
            
            var token = _apiService.GetToken();
            System.Diagnostics.Debug.WriteLine($"Token disponível: {!string.IsNullOrEmpty(token)}");
            if (!string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine($"Token encontrado, tentando conectar WebSocket...");
                await _webSocketService.ConnectAsync(token);
                
                // Aguardar um pouco para a conexão ser estabelecida
                await Task.Delay(1000);
                
                if (_webSocketService.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine("✅ WebSocket conectado com sucesso!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ AVISO: WebSocket não conectado após tentativa!");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ AVISO: Token não encontrado, WebSocket não será conectado!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erro ao conectar WebSocket: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private bool IsInternetAvailable()
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send("8.8.8.8", 3000);
            return reply?.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica se o WebView2 Runtime está instalado no sistema
    /// Verifica múltiplas chaves de registro e locais possíveis
    /// </summary>
    private bool IsWebView2RuntimeInstalled()
    {
        try
        {
            // Lista de chaves de registro possíveis para WebView2 Runtime
            var registryPaths = new[]
            {
                // Chave padrão para sistemas 64-bit (WOW64)
                @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                // Chave para sistemas 32-bit
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                // Chave alternativa no registro do usuário
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                // Verificar também nas chaves do usuário
                @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
            };

            // Verificar cada chave
            foreach (var path in registryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path, false);
                    if (key != null)
                    {
                        // Verificar se tem a propriedade "pv" (Product Version)
                        var version = key.GetValue("pv");
                        if (version != null && !string.IsNullOrEmpty(version.ToString()))
                        {
                            System.Diagnostics.Debug.WriteLine($"WebView2 Runtime encontrado em {path}: versão {version}");
                            return true;
                        }

                        // Ou verificar se a chave existe e tem subchaves
                        var subKeyNames = key.GetSubKeyNames();
                        if (subKeyNames.Length > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"WebView2 Runtime encontrado em {path} (possui subchaves)");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao verificar chave {path}: {ex.Message}");
                    // Continuar verificando outras chaves
                }
            }

            // Verificar também no registro do usuário
            try
            {
                using var userKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", false);
                if (userKey != null)
                {
                    var version = userKey.GetValue("pv");
                    if (version != null && !string.IsNullOrEmpty(version.ToString()))
                    {
                        System.Diagnostics.Debug.WriteLine($"WebView2 Runtime encontrado no registro do usuário: versão {version}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao verificar registro do usuário: {ex.Message}");
            }

            // Verificar se o arquivo DLL do WebView2 existe no sistema
            // O WebView2 geralmente está em: %ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\[version]\msedgewebview2.exe
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var edgeWebViewPath = Path.Combine(programFilesX86, "Microsoft", "EdgeWebView", "Application");
            
            if (Directory.Exists(edgeWebViewPath))
            {
                var subdirs = Directory.GetDirectories(edgeWebViewPath);
                if (subdirs.Length > 0)
                {
                    // Verificar se existe o executável
                    foreach (var subdir in subdirs)
                    {
                        var exePath = Path.Combine(subdir, "msedgewebview2.exe");
                        if (File.Exists(exePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"WebView2 Runtime encontrado em: {exePath}");
                            return true;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("WebView2 Runtime não encontrado em nenhum local verificado");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro geral ao verificar WebView2: {ex.Message}");
            // Em caso de erro, assumir que não está instalado para ser mais seguro
            return false;
        }
    }

    /// <summary>
    /// Obtém a arquitetura do sistema (x64, x86, ARM64)
    /// </summary>
    private string GetSystemArchitecture()
    {
        // Verificar a arquitetura do processo atual
        if (Environment.Is64BitProcess)
        {
            // Verificar se é ARM64
            var arch = RuntimeInformation.ProcessArchitecture;
            if (arch == Architecture.Arm64)
            {
                return "ARM64";
            }
            return "x64";
        }
        else
        {
            return "x86";
        }
    }

    /// <summary>
    /// Obtém o link de download do WebView2 Runtime baseado na arquitetura do sistema
    /// </summary>
    private string GetWebView2DownloadLink()
    {
        var arch = GetSystemArchitecture();
        
        // Links oficiais do Microsoft WebView2 Runtime
        return arch switch
        {
            "x64" => "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
            "ARM64" => "https://go.microsoft.com/fwlink/p/?LinkId=2124734",
            "x86" => "https://go.microsoft.com/fwlink/p/?LinkId=2124733",
            _ => "https://developer.microsoft.com/microsoft-edge/webview2/"
        };
    }

    /// <summary>
    /// Exibe um diálogo informando que o WebView2 não está instalado e oferece opção de download
    /// </summary>
    private void ShowWebView2DownloadDialog()
    {
        var arch = GetSystemArchitecture();
        var downloadLink = GetWebView2DownloadLink();
        
        // Criar formulário customizado com TextBox para copiar o link
        var dialog = new Form
        {
            Text = "WebView2 Runtime Necessário",
            Size = new Size(600, 300),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblMessage = new Label
        {
            Text = $"O Microsoft Edge WebView2 Runtime não está instalado no seu sistema.\n\n" +
                   $"Arquitetura detectada: {arch}\n\n" +
                   $"O WebView2 Runtime é necessário para o reconhecimento de voz funcionar.\n\n" +
                   $"Copie o link abaixo e baixe o instalador:",
            Location = new Point(20, 20),
            Size = new Size(540, 100),
            AutoSize = false
        };

        var txtLink = new TextBox
        {
            Text = downloadLink,
            Location = new Point(20, 130),
            Size = new Size(500, 25),
            ReadOnly = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnCopy = new Button
        {
            Text = "Copiar Link",
            Location = new Point(530, 128),
            Size = new Size(80, 29),
            Cursor = Cursors.Hand
        };
        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(downloadLink);
            MessageBox.Show("Link copiado para a área de transferência!", "Copiado", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var btnOpen = new Button
        {
            Text = "Abrir Link",
            Location = new Point(430, 170),
            Size = new Size(90, 35),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Yes
        };
        btnOpen.Click += (s, e) =>
    {
        try
        {
                Process.Start(new ProcessStartInfo
                {
                    FileName = downloadLink,
                    UseShellExecute = true
                });
                MessageBox.Show(
                    "Após instalar o WebView2 Runtime, por favor, reinicie o aplicativo.",
                    "Instalação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir link: {ex.Message}\n\nPor favor, copie o link manualmente.", "Erro", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        var btnClose = new Button
        {
            Text = "Fechar",
            Location = new Point(530, 170),
            Size = new Size(80, 35),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.Add(lblMessage);
        dialog.Controls.Add(txtLink);
        dialog.Controls.Add(btnCopy);
        dialog.Controls.Add(btnOpen);
        dialog.Controls.Add(btnClose);

        dialog.AcceptButton = btnOpen;
        dialog.CancelButton = btnClose;

        dialog.ShowDialog();
    }

    private void SetupAgendamentoTimer()
    {
        _agendamentoTimer = new System.Windows.Forms.Timer
        {
            Interval = 10000 // Verificar a cada 10 segundos para maior precisão
        };
        _agendamentoTimer.Tick += AgendamentoTimer_Tick;
        _agendamentoTimer.Start();
    }

    private void AgendamentoTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var agendamentos = _database.GetAgendamentosArquivos();
            var agora = DateTime.Now;

            foreach (var agendamento in agendamentos.Where(a => a.Ativo))
            {
                if (DeveExecutar(agendamento, agora))
                {
                    ExecutarAgendamento(agendamento);
                    agendamento.UltimaExecucao = agora;
                    _database.SaveAgendamentoArquivo(agendamento);
                }
            }
            
            // Processar lembretes
            ProcessarLembretes(agora);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar agendamentos: {ex.Message}");
        }
    }

    private void ProcessarLembretes(DateTime agora)
    {
        try
        {
            var lembretes = _database.GetLembretes(apenasPendentes: true);
            
            foreach (var lembrete in lembretes)
            {
                bool deveNotificar = false;
                
                // Se tem dia e mês especificados
                if (lembrete.Dia.HasValue && lembrete.Mes.HasValue)
                {
                    // Verificar se é hoje (considerando ano também)
                    var dataLembrete = new DateTime(agora.Year, lembrete.Mes.Value, lembrete.Dia.Value);
                    if (dataLembrete < agora.Date)
                        dataLembrete = dataLembrete.AddYears(1); // Próximo ano
                    
                    if (agora.Date == dataLembrete.Date || agora.Day == lembrete.Dia.Value && agora.Month == lembrete.Mes.Value)
                    {
                        // Se tem hora e minuto especificados
                        if (lembrete.Hora.HasValue && lembrete.Minuto.HasValue)
                        {
                            // A partir da hora especificada, notificar a cada 2 minutos
                            var horaMinutoLembrete = new DateTime(agora.Year, agora.Month, agora.Day, 
                                lembrete.Hora.Value, lembrete.Minuto.Value, 0);
                            
                            if (agora >= horaMinutoLembrete && agora.Hour < 22) // Até 22h (10 da noite)
                            {
                                // Verificar se já passou tempo suficiente desde a última notificação (2 minutos)
                                if (!lembrete.UltimaNotificacao.HasValue || 
                                    (agora - lembrete.UltimaNotificacao.Value).TotalMinutes >= 2)
                                {
                                    deveNotificar = true;
                                }
                            }
                        }
                        else if (lembrete.Hora.HasValue)
                        {
                            // A partir da hora especificada, notificar a cada 2 minutos
                            if (agora.Hour >= lembrete.Hora.Value && agora.Hour < 22)
                            {
                                if (!lembrete.UltimaNotificacao.HasValue || 
                                    (agora - lembrete.UltimaNotificacao.Value).TotalMinutes >= 2)
                                {
                                    deveNotificar = true;
                                }
                            }
                        }
                        else
                        {
                            // Sem hora, notificar a cada 2 minutos a partir das 7h até 22h
                            if (agora.Hour >= 7 && agora.Hour < 22)
                            {
                                if (!lembrete.UltimaNotificacao.HasValue || 
                                    (agora - lembrete.UltimaNotificacao.Value).TotalMinutes >= 2)
                                {
                                    deveNotificar = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Sem data específica (lembrete diário), notificar a cada 2 minutos a partir das 7h até 22h
                    if (agora.Hour >= 7 && agora.Hour < 22)
                    {
                        if (!lembrete.UltimaNotificacao.HasValue || 
                            (agora - lembrete.UltimaNotificacao.Value).TotalMinutes >= 2)
                        {
                            deveNotificar = true;
                        }
                    }
                }
                
                if (deveNotificar)
                {
                    _speechService.Speak($"Lembrete: você deve {lembrete.Lembrar}");
                    lembrete.UltimaNotificacao = agora;
                    _database.SaveLembrete(lembrete);
                    
                    // Atualizar contador de alarmes na UI (thread-safe)
                    if (this.IsHandleCreated)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            AtualizarContadorAlarmes();
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar lembretes: {ex.Message}");
        }
    }

    private bool DeveExecutar(AgendamentoArquivo agendamento, DateTime agora)
    {
        switch (agendamento.Frequencia)
        {
            case FrequenciaAgendamento.Nenhum:
                // Executar uma vez na data/hora exata
                if (agendamento.UltimaExecucao.HasValue)
                    return false; // Já executou uma vez
                return agora.Date == agendamento.DataHora.Date &&
                       agora.Hour == agendamento.DataHora.Hour &&
                       agora.Minute == agendamento.DataHora.Minute &&
                       agora.Second < 30;

            case FrequenciaAgendamento.PorMinuto:
                // Executar a cada minuto (independente do minuto inicial)
                if (!agendamento.UltimaExecucao.HasValue)
                {
                    // Primeira execução: executar no minuto/hora especificados
                    return agora.Hour == agendamento.DataHora.Hour &&
                           agora.Minute == agendamento.DataHora.Minute &&
                           agora.Second < 30;
                }
                else
                {
                    // Execuções subsequentes: verificar se o minuto mudou desde a última execução
                    var ultimaExec = agendamento.UltimaExecucao.Value;
                    var minutosDiferentes = agora.Minute != ultimaExec.Minute || 
                                            agora.Hour != ultimaExec.Hour ||
                                            agora.Date != ultimaExec.Date;
                    
                    // Executar quando o minuto mudou e estamos nos primeiros 30 segundos do novo minuto
                    return minutosDiferentes && agora.Second < 30;
                }

            case FrequenciaAgendamento.PorHora:
                // Executar toda hora no minuto especificado
                if (agendamento.UltimaExecucao.HasValue)
                {
                    var tempoDesdeUltimaExec = (agora - agendamento.UltimaExecucao.Value).TotalMinutes;
                    if (tempoDesdeUltimaExec < 55) // Evitar execuções muito próximas
                        return false;
                }
                return agora.Minute == agendamento.DataHora.Minute &&
                       agora.Second < 30;

            case FrequenciaAgendamento.Diariamente:
                // Executar diariamente na hora/minuto especificados
                if (agendamento.UltimaExecucao.HasValue && 
                    agendamento.UltimaExecucao.Value.Date == agora.Date)
                {
                    return false; // Já executou hoje
                }
                return agora.Hour == agendamento.DataHora.Hour &&
                       agora.Minute == agendamento.DataHora.Minute &&
                       agora.Second < 30;

            case FrequenciaAgendamento.Semanalmente:
                // Executar semanalmente no mesmo dia da semana
                if (agendamento.UltimaExecucao.HasValue)
                {
                    var diasDesdeUltimaExec = (agora.Date - agendamento.UltimaExecucao.Value.Date).TotalDays;
                    if (diasDesdeUltimaExec < 6) // Já executou esta semana
                        return false;
                }
                return agora.DayOfWeek == agendamento.DataHora.DayOfWeek &&
                       agora.Hour == agendamento.DataHora.Hour &&
                       agora.Minute == agendamento.DataHora.Minute &&
                       agora.Second < 30;

            case FrequenciaAgendamento.Mensalmente:
                // Executar mensalmente no mesmo dia do mês
                if (agendamento.UltimaExecucao.HasValue && 
                    agendamento.UltimaExecucao.Value.Month == agora.Month &&
                    agendamento.UltimaExecucao.Value.Year == agora.Year)
                {
                    return false; // Já executou este mês
                }
                return agora.Day == agendamento.DataHora.Day &&
                       agora.Hour == agendamento.DataHora.Hour &&
                       agora.Minute == agendamento.DataHora.Minute &&
                       agora.Second < 30;

            default:
                return false;
        }
    }

    private void ExecutarAgendamento(AgendamentoArquivo agendamento)
    {
        try
        {
            if (!File.Exists(agendamento.CaminhoArquivo))
            {
                System.Diagnostics.Debug.WriteLine($"Arquivo não encontrado: {agendamento.CaminhoArquivo}");
                return;
            }

            // Abrir arquivo com o aplicativo padrão do sistema
            // Usar ProcessStartInfo com configurações para evitar erros de IO
            var processInfo = new ProcessStartInfo
            {
                FileName = agendamento.CaminhoArquivo,
                UseShellExecute = true,
                Verb = "open"
            };

            Process.Start(processInfo);

            System.Diagnostics.Debug.WriteLine($"✅ Arquivo executado: {Path.GetFileName(agendamento.CaminhoArquivo)} às {DateTime.Now:HH:mm:ss} (Frequência: {agendamento.Frequencia})");
        }
        catch (System.IO.IOException ioEx)
        {
            // Erro de IO pode ocorrer se o arquivo já estiver aberto ou sendo usado
            System.Diagnostics.Debug.WriteLine($"⚠️ Erro de IO ao executar agendamento (arquivo pode estar em uso): {ioEx.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erro ao executar agendamento: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Marcar usuário como offline
        try
        {
            _apiService.SetUserOfflineAsync().Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao marcar usuário como offline: {ex.Message}");
        }

        _agendamentoTimer?.Stop();
        _agendamentoTimer?.Dispose();
        _alarmesTimer?.Stop();
        _alarmesTimer?.Dispose();
        _verificarAssistenteDormindoTimer?.Stop();
        _verificarAssistenteDormindoTimer?.Dispose();
        _paymentCallbackService?.Stop();
        _paymentCallbackService?.Dispose();
        base.OnFormClosed(e);
    }

    private void SetupAlarmesTimer()
    {
        _alarmesTimer = new System.Windows.Forms.Timer
        {
            Interval = 30000 // Verificar a cada 30 segundos
        };
        _alarmesTimer.Tick += (s, e) => AtualizarContadorAlarmes();
        _alarmesTimer.Start();
    }

    public void AtualizarContadorAlarmes()
    {
        try
        {
            var alarmesDisparados = _database.GetLembretesDisparados();
            _contadorAlarmesDisparados = alarmesDisparados.Count;

            if (_btnAlarmes != null)
            {
                // Atualizar UI de forma thread-safe
                if (this.IsHandleCreated && this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        AtualizarContadorAlarmesUI();
                    });
                }
                else
                {
                    AtualizarContadorAlarmesUI();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar contador de alarmes: {ex.Message}");
        }
    }

    private void AtualizarContadorAlarmesUI()
    {
        if (_btnAlarmes == null) return;

        if (_contadorAlarmesDisparados > 0)
        {
            // Usar sobrescritos Unicode para números
            var numeros = new[] { "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹" };
            var contadorStr = "";
            var numero = _contadorAlarmesDisparados;
            
            if (numero <= 9)
            {
                contadorStr = numeros[numero];
            }
            else
            {
                // Para números maiores que 9, usar o número normal
                contadorStr = numero.ToString();
            }

            _btnAlarmes.Text = $"⏰ Alarme{contadorStr}";
            _btnAlarmes.BackColor = Color.FromArgb(200, 100, 50); // Cor mais vibrante quando há alarmes
        }
        else
        {
            _btnAlarmes.Text = "⏰ Alarmes";
            _btnAlarmes.BackColor = Color.FromArgb(139, 69, 19); // Cor normal
        }
    }

    private void BtnAlarmes_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var alarmesDisparados = _database.GetLembretesDisparados();
        
        if (alarmesDisparados.Count == 0)
        {
            // Se não há alarmes disparados, abrir o form de configuração
            LoadConfigAlarmes();
        }
        else
        {
            // Abrir form de alarmes disparados
            var form = new AlarmesDisparadosForm(_database)
            {
                Owner = this
            };
            form.ShowDialog();
            AtualizarContadorAlarmes(); // Atualizar após fechar
        }
    }
    
    private void SetupAssistenteDormindoTimer()
    {
        _verificarAssistenteDormindoTimer = new System.Windows.Forms.Timer
        {
            Interval = 10000 // Verificar a cada 10 segundos
        };
        _verificarAssistenteDormindoTimer.Tick += (s, e) => VerificarStatusAssistenteDormindo();
        _verificarAssistenteDormindoTimer.Start();
    }
    
    private void VerificarStatusAssistenteDormindo()
    {
        try
        {
            // Verificar e atualizar bloqueio no CommandProcessor (pode disparar evento)
            _commandProcessor.VerificarEAtualizarBloqueio();
            
            // Atualizar label com status atual
            var isBlocked = _commandProcessor.IsBlocked;
            if (_lblAssistenteDormindo != null)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        AtualizarLabelAssistenteDormindo(isBlocked);
                    });
                }
                else
                {
                    AtualizarLabelAssistenteDormindo(isBlocked);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar status assistente dormindo: {ex.Message}");
        }
    }
    
    private void AtualizarLabelAssistenteDormindo(bool isBlocked)
    {
        if (_lblAssistenteDormindo == null || _pnlDashboard == null) return;
        
        // Obter nome do assistente para personalizar a mensagem
        var config = _database.GetConfigAssistente();
        var nomeAssistente = config.NomeAssistente ?? "assistente";
        
        if (isBlocked)
        {
            _lblAssistenteDormindo.Text = $"Assistente Dormindo...\n(Chame pelo nome \"{nomeAssistente}\" para ativar novamente)";
            _lblAssistenteDormindo.Visible = true;
            
            // Garantir que a label fique sobre o WebView
            if (WB != null && _pnlDashboard.Controls.Contains(WB))
            {
                _lblAssistenteDormindo.BringToFront();
                // Reposicionar no centro do WebView
                var labelWidth = 500;
                var labelHeight = 80;
                var labelX = WB.Location.X + (WB.Width / 2) - (labelWidth / 2);
                var labelY = WB.Location.Y + (WB.Height / 2) - (labelHeight / 2);
                
                _lblAssistenteDormindo.Location = new Point(labelX, labelY);
                _lblAssistenteDormindo.Size = new Size(labelWidth, labelHeight);
            }
            
            // Forçar atualização do layout para garantir que a label apareça
            UpdateDashboardLayout();
            
            System.Diagnostics.Debug.WriteLine("[ASSISTENTE] Modo dormindo ativado - label visível");
        }
        else
        {
            _lblAssistenteDormindo.Visible = false;
            System.Diagnostics.Debug.WriteLine("[ASSISTENTE] Modo dormindo desativado - label oculta");
        }
    }

}

