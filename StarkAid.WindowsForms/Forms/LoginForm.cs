using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class LoginForm : Form
{
    private readonly ApiService _apiService;
    private readonly LocalDatabase? _database;
    private TextBox? _txtEmail;
    private TextBox? _txtPassword;
    private Button? _btnLogin;
    private Button? _btnRegister;
    private Label? _lblTitle;
    private Label? _lblEmail;
    private Label? _lblPassword;
    private Panel? _pnlMain;

    public LoginResponse? LoginResult { get; private set; }

    public LoginForm(ApiService apiService, LocalDatabase? database = null)
    {
        _apiService = apiService;
        _database = database;
        InitializeComponent();
        ApplyFuturisticStyle();
        LoadSavedCredentials();
    }

    private void InitializeComponent()
    {
        this.Text = "StarkAid - Login";
        this.Size = new Size(500, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(40)
        };

        _lblTitle = new Label
        {
            Text = "STARK AID",
            Font = new Font("Segoe UI", 32, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(100, 30)
        };

        _lblEmail = new Label
        {
            Text = "Email",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(50, 120)
        };

        _txtEmail = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(400, 35),
            Location = new Point(50, 150)
        };

        _lblPassword = new Label
        {
            Text = "Senha",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(50, 200)
        };

        _txtPassword = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(400, 35),
            Location = new Point(50, 230),
            UseSystemPasswordChar = true
        };

        _btnLogin = new Button
        {
            Text = "ENTRAR",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(400, 45),
            Location = new Point(50, 290),
            Cursor = Cursors.Hand
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.Click += BtnLogin_Click;

        _btnRegister = new Button
        {
            Text = "CADASTRAR",
            Font = new Font("Segoe UI", 12),
            BackColor = Color.Transparent,
            ForeColor = Color.Cyan,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(400, 35),
            Location = new Point(50, 345),
            Cursor = Cursors.Hand
        };
        _btnRegister.FlatAppearance.BorderSize = 0;
        _btnRegister.Click += BtnRegister_Click;

        // Link Esqueceu Senha
        var lnkEsqueceuSenha = new LinkLabel
        {
            Text = "Esqueceu sua senha? Clique aqui.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(400, 25),
            Location = new Point(50, 280),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        lnkEsqueceuSenha.LinkColor = Color.White;
        lnkEsqueceuSenha.ActiveLinkColor = Color.FromArgb(200, 200, 200);
        lnkEsqueceuSenha.VisitedLinkColor = Color.White;
        lnkEsqueceuSenha.LinkClicked += async (s, e) =>
        {
            SoundPlayer.PlayClick();
            var email = _txtEmail!.Text;
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Por favor, preencha o campo Email primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Deseja enviar um email de redefinição de senha para {email}?",
                "Esqueceu sua senha?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                lnkEsqueceuSenha.Enabled = false;
                lnkEsqueceuSenha.Text = "Enviando email...";

                try
                {
                    if (await _apiService.RequestPasswordResetAsync(email))
                    {
                        SoundPlayer.PlaySuccess();
                        MessageBox.Show(
                            "Instruções para redefinir sua senha foram enviadas para seu email.\n\nVerifique sua caixa de entrada e siga as instruções.",
                            "Email Enviado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        SoundPlayer.PlayError();
                        MessageBox.Show(
                            "Erro ao enviar email de redefinição de senha.\n\nVerifique se o email está correto e tente novamente.",
                            "Erro",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    SoundPlayer.PlayError();
                    MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    lnkEsqueceuSenha.Enabled = true;
                    lnkEsqueceuSenha.Text = "Esqueceu sua senha? Clique aqui.";
                }
            }
        };

        // Ajustar posição do botão de login para dar espaço ao link
        _btnLogin.Location = new Point(50, 310);
        _btnRegister.Location = new Point(50, 365);

        _pnlMain.Controls.Add(_lblTitle);
        _pnlMain.Controls.Add(_lblEmail);
        _pnlMain.Controls.Add(_txtEmail);
        _pnlMain.Controls.Add(_lblPassword);
        _pnlMain.Controls.Add(_txtPassword);
        _pnlMain.Controls.Add(lnkEsqueceuSenha);
        _pnlMain.Controls.Add(_btnLogin);
        _pnlMain.Controls.Add(_btnRegister);

        this.Controls.Add(_pnlMain);
    }

    private void ApplyFuturisticStyle()
    {
        // Adicionar efeitos de hover
        if (_btnLogin != null)
        {
            _btnLogin.MouseEnter += (s, e) => { _btnLogin!.BackColor = Color.FromArgb(0, 255, 255, 200); SoundPlayer.PlayClick(); };
            _btnLogin.MouseLeave += (s, e) => { _btnLogin!.BackColor = Color.Cyan; };
        }

        if (_btnRegister != null)
        {
            _btnRegister.MouseEnter += (s, e) => { _btnRegister!.ForeColor = Color.White; };
            _btnRegister.MouseLeave += (s, e) => { _btnRegister!.ForeColor = Color.Cyan; };
        }
    }

    private void LoadSavedCredentials()
    {
        if (_database == null || _txtEmail == null) return;

        try
        {
            var (email, _, _) = _database.GetLoginCredentials();
            if (!string.IsNullOrEmpty(email))
            {
                _txtEmail.Text = email;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar credenciais salvas: {ex.Message}");
        }
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        if (_txtEmail == null || _txtPassword == null) return;

        SoundPlayer.PlayClick();

        var email = _txtEmail.Text.Trim();
        var password = _txtPassword.Text;

        var request = new LoginRequest
        {
            Email = email,
            Password = password,
            Origem = "app"
        };

        _btnLogin!.Enabled = false;
        _btnLogin.Text = "ENTRANDO...";

        try
        {
            LoginResult = await _apiService.LoginAsync(request);
            if (LoginResult != null)
            {
                _apiService.SetToken(LoginResult.Token);
                
                // Salvar credenciais
                if (_database != null)
                {
                    _database.SaveLoginCredentials(email, password, LoginResult.Token);
                }
                
                SoundPlayer.PlaySuccess();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Email ou senha inválidos!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SoundPlayer.PlayError();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao fazer login: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SoundPlayer.PlayError();
        }
        finally
        {
            _btnLogin.Enabled = true;
            _btnLogin.Text = "ENTRAR";
        }
    }

    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        if (_txtEmail == null || _txtPassword == null) return;

        SoundPlayer.PlayClick();

        // Por enquanto, apenas mostra mensagem
        MessageBox.Show("Funcionalidade de cadastro será implementada em breve!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

