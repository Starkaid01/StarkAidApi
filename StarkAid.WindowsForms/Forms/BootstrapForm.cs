using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

/// <summary>
/// Formulário bootstrap que executa login e verificação de licença de forma assíncrona
/// antes de abrir a MainForm. Isso garante que o threading STA seja mantido.
/// </summary>
public partial class BootstrapForm : Form
{
    private readonly ApiService _apiService;
    private readonly LocalDatabase _database;
    private readonly WebSocketService _webSocketService;
    private readonly UdpService _udpService;
    private readonly SpeechService _speechService;
    private readonly CommandProcessor _commandProcessor;

    public BootstrapForm(
        ApiService apiService,
        LocalDatabase database,
        WebSocketService webSocketService,
        UdpService udpService,
        SpeechService speechService,
        CommandProcessor commandProcessor)
    {
        _apiService = apiService;
        _database = database;
        _webSocketService = webSocketService;
        _udpService = udpService;
        _speechService = speechService;
        _commandProcessor = commandProcessor;

        InitializeComponent();
        InitializeBootstrap();
    }

    private void InitializeComponent()
    {
        this.Text = "StarkAid - Carregando...";
        this.Size = new Size(400, 150);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(15, 15, 25);

        var lblStatus = new Label
        {
            Text = "Inicializando StarkAid...",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            AutoSize = true,
            Location = new Point(20, 50)
        };

        this.Controls.Add(lblStatus);
    }

    private async void InitializeBootstrap()
    {
        try
        {
            // Verificar status da API primeiro
            var isOnline = await _apiService.CheckApiStatusAsync();
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Status da API: {(isOnline ? "Online" : "Offline")}");

            User? user = null;
            LoginResponse? loginResult = null;
            var (savedEmail, savedPasswordHash, savedToken) = _database.GetLoginCredentials();

            if (isOnline)
            {
                // API ONLINE: Tentar fazer login normalmente
                if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPasswordHash) && !string.IsNullOrEmpty(savedToken))
                {
                    try
                    {
                        _apiService.SetToken(savedToken);

                        var passwordBytes = Convert.FromBase64String(savedPasswordHash);
                        var password = System.Text.Encoding.UTF8.GetString(passwordBytes);

                        var loginRequest = new LoginRequest
                        {
                            Email = savedEmail,
                            Password = password,
                            Origem = "app"
                        };

                        loginResult = await _apiService.LoginAsync(loginRequest);
                        if (loginResult != null)
                        {
                            _apiService.SetToken(loginResult.Token);
                            _database.UpdateLoginToken(loginResult.Token);
                            user = loginResult.User;
                        }
                    }
                    catch
                    {
                        loginResult = null;
                    }
                }

                // Se não conseguiu fazer login automático, mostrar formulário
                if (loginResult == null)
                {
                    this.Hide();
                    using var loginForm = new LoginForm(_apiService, _database);
                    if (loginForm.ShowDialog() != DialogResult.OK || loginForm.LoginResult == null)
                    {
                        Application.Exit();
                        return;
                    }
                    loginResult = loginForm.LoginResult;
                    // Garantir que o token está configurado
                    if (loginResult != null && !string.IsNullOrEmpty(loginResult.Token))
                    {
                        _apiService.SetToken(loginResult.Token);
                        user = loginResult.User;
                    }
                    this.Show();
                }
            }
            else
            {
                // API OFFLINE: Usar dados locais
                System.Diagnostics.Debug.WriteLine("[BootstrapForm] API offline - usando dados locais");
                
                var localUser = _database.GetUser();
                if (localUser == null)
                {
                    // Se não há dados locais, não pode abrir offline
                    MessageBox.Show(
                        "A API está offline e não há dados salvos localmente.\n\n" +
                        "Por favor, conecte-se à internet e abra o aplicativo pelo menos uma vez para sincronizar os dados.",
                        "Dados Locais Não Encontrados",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    Application.Exit();
                    return;
                }

                user = localUser;
                
                // Configurar token salvo (mesmo que não funcione offline, pode ser útil quando voltar online)
                if (!string.IsNullOrEmpty(savedToken))
                {
                    _apiService.SetToken(savedToken);
                }
                
                System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Usando dados locais do usuário: {user.Name}, StarkCoins: {user.StarkCoins}");
            }

            // Verificar licença
            var licenseService = new LicenseService(_apiService, _database);
            var licenseKey = licenseService.GetStoredLicenseKey();

            if (string.IsNullOrEmpty(licenseKey))
            {
                // Se não tem licença salva e está offline, não pode ativar
                if (!isOnline)
                {
                    MessageBox.Show(
                        "A API está offline e não há licença ativada.\n\n" +
                        "Por favor, conecte-se à internet e ative uma licença.",
                        "Licença Necessária",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    Application.Exit();
                    return;
                }

                // Se está online, pode ativar licença
                this.Hide();
                using var licenseForm = new LicenseActivationForm(licenseService);
                if (licenseForm.ShowDialog() != DialogResult.OK || !licenseForm.LicenseActivated)
                {
                    MessageBox.Show("É necessário ativar uma licença para usar o StarkAid Windows Forms.",
                        "Licença Necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                    return;
                }
                this.Show();
            }
            else
            {
                // Se está online, verificar licença na API
                if (isOnline)
                {
                    try
                    {
                        var isValid = await licenseService.VerifyLicenseAsync();
                        if (!isValid)
                        {
                            this.Hide();
                            MessageBox.Show("Sua licença não está mais válida. Por favor, ative uma nova licença.",
                                "Licença Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            using var licenseForm = new LicenseActivationForm(licenseService);
                            if (licenseForm.ShowDialog() != DialogResult.OK || !licenseForm.LicenseActivated)
                            {
                                Application.Exit();
                                return;
                            }
                            this.Show();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao verificar licença: {ex.Message}");
                        // Em caso de erro, continuar (modo offline tolerante)
                    }
                }
                else
                {
                    // Se está offline, assumir que licença está válida (já foi verificada quando estava online)
                    System.Diagnostics.Debug.WriteLine("[BootstrapForm] API offline - assumindo licença válida (modo offline)");
                }
            }

            // Se estiver online, sincronizar dados e marcar usuário como online
            if (isOnline)
            {
                try
                {
                    var token = _apiService.GetToken();
                    if (string.IsNullOrEmpty(token))
                    {
                        System.Diagnostics.Debug.WriteLine("[BootstrapForm] Token não encontrado, tentando obter do loginResult...");
                        if (loginResult != null && !string.IsNullOrEmpty(loginResult.Token))
                        {
                            _apiService.SetToken(loginResult.Token);
                            System.Diagnostics.Debug.WriteLine("[BootstrapForm] Token configurado do loginResult");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[BootstrapForm] ERRO: Não foi possível obter o token!");
                            return;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[BootstrapForm] Chamando SetUserOnlineAsync...");
                    var result = await _apiService.SetUserOnlineAsync();
                    System.Diagnostics.Debug.WriteLine($"[BootstrapForm] SetUserOnlineAsync resultado: {result}");

                    // Sincronizar todos os dados
                    System.Diagnostics.Debug.WriteLine("[BootstrapForm] Sincronizando dados...");
                    await SyncAllDataAsync();

                    // Sincronizar logs de erro
                    try
                    {
                        if (loginResult?.User?.Id != null)
                        {
                            var logs = _database.GetAllLogsToSuporte();
                            if (logs.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Sincronizando {logs.Count} logs de erro...");
                                var syncResult = await _apiService.SyncErrorLogsSoftAsync(loginResult.User.Id, logs);
                                System.Diagnostics.Debug.WriteLine($"[BootstrapForm] SyncErrorLogsSoftAsync resultado: {syncResult}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Erro ao sincronizar logs: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Stack trace: {ex.StackTrace}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Erro ao sincronizar dados: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BootstrapForm] Sistema offline - usando dados locais");
            }

            // Fechar BootstrapForm e abrir MainForm
            // IMPORTANTE: Precisamos fazer isso na UI thread usando BeginInvoke
            // para não bloquear o método assíncrono
            this.BeginInvoke((MethodInvoker)delegate
            {
                AbrirMainForm(user, licenseService);
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao inicializar aplicação: {ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private void AbrirMainForm(User user, LicenseService licenseService)
    {
        var mainForm = new MainForm(
            _apiService,
            _database,
            _webSocketService,
            _udpService,
            _speechService,
            _commandProcessor,
            user,
            licenseService);
        
        // Esconder BootstrapForm e mostrar MainForm
        this.Hide();
        mainForm.FormClosed += (s, e) => 
        { 
            // Se o DialogResult for Retry, significa que foi logout - mostrar login novamente
            if (mainForm.DialogResult == DialogResult.Retry)
            {
                // Mostrar BootstrapForm novamente e reiniciar o processo de login
                this.Show();
                InitializeBootstrap();
            }
            else
            {
                // Fechar aplicação normalmente
                this.Close();
            }
        };
        mainForm.Show();
    }

    private async Task SyncAllDataAsync()
    {
        try
        {
            // Buscar e salvar dados do usuário
            var user = await _apiService.GetCurrentUserAsync();
            if (user != null)
            {
                _database.SaveUser(user);
                _database.SaveDadosUI(user.StarkCoins);
                System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Usuário salvo: {user.Name}, StarkCoins: {user.StarkCoins}");
            }

            // Buscar e salvar comandos sociais
            var comandos = await _apiService.GetComandosSociaisAsync();
            _database.SaveComandosSociais(comandos);
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] {comandos.Count} comandos sociais salvos");

            // Buscar e salvar dispositivos ESP
            var dispositivosEsp = await _apiService.GetDispositivosEspAsync();
            _database.SaveDispositivosEsp(dispositivosEsp);
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] {dispositivosEsp.Count} dispositivos ESP salvos");

            // Buscar e salvar dispositivos Ewelink
            var dispositivosEwelink = await _apiService.GetEwelinkDevicesAsync();
            _database.SaveEwelinkDevices(dispositivosEwelink);
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] {dispositivosEwelink.Count} dispositivos Ewelink salvos");

            // Buscar e salvar dispositivos Starkswitch
            var dispositivos = await _apiService.GetDevicesAsync();
            _database.SaveDevices(dispositivos);
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] {dispositivos.Count} dispositivos Starkswitch salvos");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Erro ao sincronizar dados: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BootstrapForm] Stack trace: {ex.StackTrace}");
        }
    }
}

