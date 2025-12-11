using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using StarkAid.WindowsForms.Config;

namespace StarkAid.WindowsForms.Forms;

public partial class LicenseActivationForm : Form
{
    private readonly LicenseService _licenseService;
    private TextBox? _txtLicenseKey;
    private Button? _btnActivate;
    private Button? _btnCancel;
    private Label? _lblTitle;
    private Label? _lblLicenseKey;
    private Label? _lblInfo;
    private LinkLabel? _lnkLicenseUrl;
    private Panel? _pnlMain;

    public bool LicenseActivated { get; private set; }

    public LicenseActivationForm(LicenseService licenseService)
    {
        _licenseService = licenseService;
        InitializeComponent();
        ApplyFuturisticStyle();
    }

    private void InitializeComponent()
    {
        this.Text = "Ativar Licença - StarkAid";
        this.Size = new Size(600, 400);
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
            Text = "ATIVAR LICENÇA",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(150, 30)
        };

        _lblInfo = new Label
        {
            Text = "Digite sua chave de licença para ativar o StarkAid Windows Forms nesta máquina.\n\n" +
                   "Você pode adquirir uma licença em:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(520, 60),
            Location = new Point(40, 80)
        };

        _lnkLicenseUrl = new LinkLabel
        {
            Text = $"{ApiConfig.WebBaseUrl}/licenses.html",
            Font = new Font("Segoe UI", 10, FontStyle.Underline),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Location = new Point(40, 145),
            LinkColor = Color.Cyan,
            VisitedLinkColor = Color.Cyan,
            ActiveLinkColor = Color.FromArgb(0, 200, 255)
        };
        _lnkLicenseUrl.LinkClicked += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"{ApiConfig.WebBaseUrl}/licenses.html",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o link: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        _lblLicenseKey = new Label
        {
            Text = "Chave de Licença",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(40, 180)
        };

        _txtLicenseKey = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(520, 35),
            Location = new Point(40, 210),
            CharacterCasing = CharacterCasing.Upper
        };

        _btnActivate = new Button
        {
            Text = "ATIVAR",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(250, 45),
            Location = new Point(40, 270),
            Cursor = Cursors.Hand
        };
        _btnActivate.FlatAppearance.BorderSize = 0;
        _btnActivate.Click += BtnActivate_Click;

        _btnCancel = new Button
        {
            Text = "CANCELAR",
            Font = new Font("Segoe UI", 12),
            BackColor = Color.Transparent,
            ForeColor = Color.Red,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(250, 45),
            Location = new Point(310, 270),
            Cursor = Cursors.Hand
        };
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        _pnlMain.Controls.Add(_lblTitle);
        _pnlMain.Controls.Add(_lblInfo);
        _pnlMain.Controls.Add(_lnkLicenseUrl);
        _pnlMain.Controls.Add(_lblLicenseKey);
        _pnlMain.Controls.Add(_txtLicenseKey);
        _pnlMain.Controls.Add(_btnActivate);
        _pnlMain.Controls.Add(_btnCancel);

        this.Controls.Add(_pnlMain);
    }

    private void ApplyFuturisticStyle()
    {
        if (_btnActivate != null)
        {
            _btnActivate.MouseEnter += (s, e) => { _btnActivate!.BackColor = Color.FromArgb(0, 255, 255, 200); SoundPlayer.PlayClick(); };
            _btnActivate.MouseLeave += (s, e) => { _btnActivate!.BackColor = Color.Cyan; };
        }

        if (_btnCancel != null)
        {
            _btnCancel.MouseEnter += (s, e) => { _btnCancel!.ForeColor = Color.White; };
            _btnCancel.MouseLeave += (s, e) => { _btnCancel!.ForeColor = Color.Red; };
        }

        if (_lnkLicenseUrl != null)
        {
            _lnkLicenseUrl.MouseEnter += (s, e) => { _lnkLicenseUrl!.ForeColor = Color.FromArgb(0, 200, 255); };
            _lnkLicenseUrl.MouseLeave += (s, e) => { _lnkLicenseUrl!.ForeColor = Color.Cyan; };
        }
    }

    private async void BtnActivate_Click(object? sender, EventArgs e)
    {
        if (_txtLicenseKey == null || string.IsNullOrWhiteSpace(_txtLicenseKey.Text))
        {
            MessageBox.Show("Por favor, digite a chave de licença!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SoundPlayer.PlayClick();
        _btnActivate!.Enabled = false;
        _btnActivate.Text = "ATIVANDO...";

        try
        {
            var licenseKey = _txtLicenseKey.Text.Trim().ToUpper();
            var machineName = Environment.MachineName;

            // Nota: A verificação de token será feita no ApiService

            var activated = await _licenseService.ActivateLicenseAsync(licenseKey, machineName);

            if (activated)
            {
                SoundPlayer.PlaySuccess();
                MessageBox.Show("Licença ativada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LicenseActivated = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                SoundPlayer.PlayError();
                var errorMsg = "Não foi possível ativar a licença.\n\n" +
                               "Verifique se:\n" +
                               "- A chave de licença está correta\n" +
                               "- A licença está ativa\n" +
                               "- Não excedeu o limite de máquinas permitidas\n" +
                               "- Você está conectado à internet\n" +
                               "- Você está autenticado no sistema";
                System.Diagnostics.Debug.WriteLine($"[LicenseActivationForm] Falha na ativação - {errorMsg}");
                MessageBox.Show(errorMsg, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            SoundPlayer.PlayError();
            var errorMessage = $"Sessão expirada: {ex.Message}\n\nPor favor, feche esta janela e faça login novamente.";
            System.Diagnostics.Debug.WriteLine($"[LicenseActivationForm] Não autorizado: {errorMessage}");
            MessageBox.Show(errorMessage, "Sessão Expirada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            var errorMessage = $"Erro ao ativar licença: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LicenseActivationForm] Exceção: {errorMessage}");
            System.Diagnostics.Debug.WriteLine($"[LicenseActivationForm] StackTrace: {ex.StackTrace}");
            MessageBox.Show(errorMessage, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnActivate.Enabled = true;
            _btnActivate.Text = "ATIVAR";
        }
    }
}

