using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Text;

namespace StarkAid.WindowsForms.Forms;

public partial class ConfigurarContaForm : Form
{
    private readonly ApiService _apiService;
    private readonly User _user;
    private TextBox? _txtName;
    private TextBox? _txtEmail;
    private TextBox? _txtEstado;
    private TextBox? _txtCidade;
    private TextBox? _txtBairro;
    private TextBox? _txtCurrentPassword;
    private TextBox? _txtNewPassword;
    private TextBox? _txtConfirmPassword;
    private Label? _lblApiKey;
    private Button? _btnSalvarPerfil;
    private Button? _btnAlterarSenha;
    private Button? _btnCopiarApiKey;
    private Button? _btnExcluirConta;
    private LinkLabel? _lnkEsqueceuSenha;
    private string _fullApiKey = string.Empty;

    public ConfigurarContaForm(ApiService apiService, User user)
    {
        _apiService = apiService;
        _user = user;
        _fullApiKey = user.ApiKey;
        InitializeComponent();
        LoadUserDataAsync();
    }

    private void InitializeComponent()
    {
        this.Text = "Configurar Conta";
        this.Size = new Size(700, 750);
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
            Text = "⚙️ CONFIGURAR CONTA",
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

        // Conteúdo do formulário
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 20, 0, 0)
        };

        int yPos = 20;

        // === SEÇÃO: EDITAR PERFIL ===
        var lblSecaoPerfil = new Label
        {
            Text = "📝 Editar Perfil",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 35;

        // Nome
        var lblName = new Label
        {
            Text = "Nome",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtName = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0)
        };
        yPos += 55;

        // Email
        var lblEmail = new Label
        {
            Text = "Email",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtEmail = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0)
        };
        yPos += 55;

        // Estado
        var lblEstado = new Label
        {
            Text = "Estado",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtEstado = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0),
            PlaceholderText = "Ex: São Paulo"
        };
        yPos += 55;

        // Cidade
        var lblCidade = new Label
        {
            Text = "Cidade",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtCidade = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0),
            PlaceholderText = "Ex: São Paulo"
        };
        yPos += 55;

        // Bairro
        var lblBairro = new Label
        {
            Text = "Bairro",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtBairro = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0),
            PlaceholderText = "Ex: Centro"
        };
        yPos += 55;

        // Botão Salvar Perfil
        _btnSalvarPerfil = new Button
        {
            Text = "💾 SALVAR ALTERAÇÕES",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(620, 40),
            Location = new Point(0, yPos),
            Cursor = Cursors.Hand
        };
        _btnSalvarPerfil.FlatAppearance.BorderSize = 0;
        _btnSalvarPerfil.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 220);
        _btnSalvarPerfil.Click += BtnSalvarPerfil_Click;
        yPos += 60;

        // Separador
        var separator1 = new Panel
        {
            Size = new Size(620, 1),
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(50, 50, 60)
        };
        yPos += 30;

        // === SEÇÃO: ALTERAR SENHA ===
        var lblSecaoSenha = new Label
        {
            Text = "🔒 Alterar Senha",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 35;

        // Senha Atual
        var lblCurrentPassword = new Label
        {
            Text = "Senha Atual",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtCurrentPassword = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0),
            UseSystemPasswordChar = true
        };
        yPos += 55;

        // Nova Senha
        var lblNewPassword = new Label
        {
            Text = "Nova Senha",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtNewPassword = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0),
            UseSystemPasswordChar = true
        };
        yPos += 55;

        // Confirmar Nova Senha
        var lblConfirmPassword = new Label
        {
            Text = "Confirmar Nova Senha",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 30;

        _txtConfirmPassword = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            Padding = new Padding(10, 0, 0, 0),
            UseSystemPasswordChar = true
        };
        yPos += 55;

        // Botão Alterar Senha
        _btnAlterarSenha = new Button
        {
            Text = "🔐 ALTERAR SENHA",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(620, 40),
            Location = new Point(0, yPos),
            Cursor = Cursors.Hand
        };
        _btnAlterarSenha.FlatAppearance.BorderSize = 0;
        _btnAlterarSenha.FlatAppearance.MouseOverBackColor = Color.FromArgb(14, 165, 119);
        _btnAlterarSenha.Click += BtnAlterarSenha_Click;
        yPos += 55;

        // Link Esqueceu Senha (centralizado, abaixo do botão)
        _lnkEsqueceuSenha = new LinkLabel
        {
            Text = "Esqueceu sua senha? Clique aqui.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(620, 25),
            Location = new Point(0, yPos),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _lnkEsqueceuSenha.LinkColor = Color.White;
        _lnkEsqueceuSenha.ActiveLinkColor = Color.FromArgb(200, 200, 200);
        _lnkEsqueceuSenha.VisitedLinkColor = Color.White;
        _lnkEsqueceuSenha.LinkClicked += LnkEsqueceuSenha_LinkClicked;
        yPos += 35;

        // Separador
        var separator2 = new Panel
        {
            Size = new Size(620, 1),
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(50, 50, 60)
        };
        yPos += 30;

        // === SEÇÃO: API KEY ===
        var lblSecaoApiKey = new Label
        {
            Text = "🔑 API Key",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 35;

        // Container para API Key e botão copiar
        var apiKeyContainer = new Panel
        {
            Size = new Size(620, 38),
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(35, 35, 45)
        };

        _lblApiKey = new Label
        {
            Font = new Font("Consolas", 10),
            ForeColor = Color.Gray,
            AutoSize = false,
            Size = new Size(520, 38),
            Location = new Point(10, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = MaskApiKey(_fullApiKey)
        };

        _btnCopiarApiKey = new Button
        {
            Text = "📋 Copiar",
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 32),
            Location = new Point(535, 3),
            Cursor = Cursors.Hand
        };
        _btnCopiarApiKey.FlatAppearance.BorderSize = 0;
        _btnCopiarApiKey.Click += BtnCopiarApiKey_Click;

        apiKeyContainer.Controls.Add(_lblApiKey);
        apiKeyContainer.Controls.Add(_btnCopiarApiKey);
        yPos += 60;

        // Separador
        var separator3 = new Panel
        {
            Size = new Size(620, 1),
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(50, 50, 60)
        };
        yPos += 30;

        // === SEÇÃO: EXCLUIR CONTA ===
        var lblSecaoExcluir = new Label
        {
            Text = "🗑️ Excluir Conta",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            AutoSize = true,
            Location = new Point(0, yPos)
        };
        yPos += 35;

        // Botão Excluir Conta
        _btnExcluirConta = new Button
        {
            Text = "🗑️ EXCLUIR CONTA",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(239, 68, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(620, 40),
            Location = new Point(0, yPos),
            Cursor = Cursors.Hand
        };
        _btnExcluirConta.FlatAppearance.BorderSize = 0;
        _btnExcluirConta.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
        _btnExcluirConta.Click += BtnExcluirConta_Click;

        contentPanel.Controls.Add(lblSecaoPerfil);
        contentPanel.Controls.Add(lblName);
        contentPanel.Controls.Add(_txtName);
        contentPanel.Controls.Add(lblEmail);
        contentPanel.Controls.Add(_txtEmail);
        contentPanel.Controls.Add(lblEstado);
        contentPanel.Controls.Add(_txtEstado);
        contentPanel.Controls.Add(lblCidade);
        contentPanel.Controls.Add(_txtCidade);
        contentPanel.Controls.Add(lblBairro);
        contentPanel.Controls.Add(_txtBairro);
        contentPanel.Controls.Add(_btnSalvarPerfil);
        contentPanel.Controls.Add(separator1);
        contentPanel.Controls.Add(lblSecaoSenha);
        contentPanel.Controls.Add(lblCurrentPassword);
        contentPanel.Controls.Add(_txtCurrentPassword);
        contentPanel.Controls.Add(lblNewPassword);
        contentPanel.Controls.Add(_txtNewPassword);
        contentPanel.Controls.Add(lblConfirmPassword);
        contentPanel.Controls.Add(_txtConfirmPassword);
        contentPanel.Controls.Add(_btnAlterarSenha);
        contentPanel.Controls.Add(_lnkEsqueceuSenha);
        contentPanel.Controls.Add(separator2);
        contentPanel.Controls.Add(lblSecaoApiKey);
        contentPanel.Controls.Add(apiKeyContainer);
        contentPanel.Controls.Add(separator3);
        contentPanel.Controls.Add(lblSecaoExcluir);
        contentPanel.Controls.Add(_btnExcluirConta);

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(contentPanel);

        this.Controls.Add(mainPanel);
    }

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
            return apiKey;

        var start = apiKey.Substring(0, 4);
        var end = apiKey.Substring(apiKey.Length - 2);
        var masked = new string('*', Math.Max(apiKey.Length - 6, 4));
        return $"{start}{masked}{end}";
    }

    private void BtnCopiarApiKey_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        try
        {
            Clipboard.SetText(_fullApiKey);
            SoundPlayer.PlaySuccess();
            MessageBox.Show("API Key copiada para a área de transferência!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro ao copiar API Key: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadUserDataAsync()
    {
        try
        {
            _txtName!.Text = _user.Name;
            _txtEmail!.Text = _user.Email;
            _txtEstado!.Text = _user.Estado ?? string.Empty;
            _txtCidade!.Text = _user.Cidade ?? string.Empty;
            _txtBairro!.Text = _user.Bairro ?? string.Empty;
            _fullApiKey = _user.ApiKey;
            _lblApiKey!.Text = MaskApiKey(_fullApiKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar dados do usuário: {ex.Message}");
        }
    }

    private async void BtnSalvarPerfil_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (string.IsNullOrWhiteSpace(_txtName!.Text) || string.IsNullOrWhiteSpace(_txtEmail!.Text))
        {
            MessageBox.Show("Preencha todos os campos!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSalvarPerfil!.Enabled = false;
        _btnSalvarPerfil.Text = "SALVANDO...";

        try
        {
            if (await _apiService.UpdateUserAsync(_txtName.Text, _txtEmail.Text, 
                _txtEstado!.Text, _txtCidade!.Text, _txtBairro!.Text))
            {
                SoundPlayer.PlaySuccess();
                MessageBox.Show("Perfil atualizado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao atualizar perfil!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvarPerfil.Enabled = true;
            _btnSalvarPerfil.Text = "💾 SALVAR ALTERAÇÕES";
        }
    }

    private async void BtnAlterarSenha_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        
        if (string.IsNullOrWhiteSpace(_txtCurrentPassword!.Text) || 
            string.IsNullOrWhiteSpace(_txtNewPassword!.Text) || 
            string.IsNullOrWhiteSpace(_txtConfirmPassword!.Text))
        {
            MessageBox.Show("Preencha todos os campos de senha!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_txtNewPassword.Text.Length < 6)
        {
            MessageBox.Show("A nova senha deve ter pelo menos 6 caracteres!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_txtNewPassword.Text != _txtConfirmPassword.Text)
        {
            MessageBox.Show("As senhas não coincidem!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnAlterarSenha!.Enabled = false;
        _btnAlterarSenha.Text = "ALTERANDO...";

        try
        {
            if (await _apiService.ChangePasswordAsync(_txtCurrentPassword.Text, _txtNewPassword.Text))
            {
                SoundPlayer.PlaySuccess();
                MessageBox.Show("Senha alterada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtCurrentPassword.Text = "";
                _txtNewPassword.Text = "";
                _txtConfirmPassword.Text = "";
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao alterar senha. Verifique se a senha atual está correta.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnAlterarSenha.Enabled = true;
            _btnAlterarSenha.Text = "🔐 ALTERAR SENHA";
        }
    }

    private async void LnkEsqueceuSenha_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
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
            _lnkEsqueceuSenha!.Enabled = false;
            _lnkEsqueceuSenha.Text = "Enviando email...";

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
                _lnkEsqueceuSenha.Enabled = true;
                _lnkEsqueceuSenha.Text = "Esqueceu sua senha? Clique aqui.";
            }
        }
    }

    private async void BtnExcluirConta_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var result = MessageBox.Show(
            "Tem certeza que deseja excluir sua conta?\n\nEsta ação é IRREVERSÍVEL e todos os seus dados serão perdidos permanentemente!",
            "⚠️ Confirmar Exclusão de Conta",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            // Solicitar senha
            using var passwordForm = new Form
            {
                Text = "Confirmar Senha",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(25, 25, 35),
                FormBorderStyle = FormBorderStyle.None
            };

            var lblPassword = new Label
            {
                Text = "Digite sua senha para confirmar:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(35, 35, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(340, 35),
                Location = new Point(20, 50),
                UseSystemPasswordChar = true
            };

            var btnConfirm = new Button
            {
                Text = "CONFIRMAR",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(170, 35),
                Location = new Point(20, 95),
                DialogResult = DialogResult.OK
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "CANCELAR",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(170, 35),
                Location = new Point(190, 95),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            passwordForm.Controls.Add(lblPassword);
            passwordForm.Controls.Add(txtPassword);
            passwordForm.Controls.Add(btnConfirm);
            passwordForm.Controls.Add(btnCancel);
            passwordForm.AcceptButton = btnConfirm;
            passwordForm.CancelButton = btnCancel;

            if (passwordForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                _btnExcluirConta!.Enabled = false;
                _btnExcluirConta.Text = "EXCLUINDO...";

                try
                {
                    if (await _apiService.DeleteAccountAsync(txtPassword.Text))
                    {
                        SoundPlayer.PlaySuccess();
                        MessageBox.Show("Conta excluída com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.Abort; // Indica que a conta foi excluída
                        this.Close();
                    }
                    else
                    {
                        SoundPlayer.PlayError();
                        MessageBox.Show("Senha incorreta ou erro ao excluir conta.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    SoundPlayer.PlayError();
                    MessageBox.Show($"Erro ao excluir conta: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _btnExcluirConta.Enabled = true;
                    _btnExcluirConta.Text = "🗑️ EXCLUIR CONTA";
                }
            }
        }
    }
}

