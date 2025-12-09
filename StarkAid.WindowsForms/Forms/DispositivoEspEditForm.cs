using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class DispositivoEspEditForm : Form
{
    private readonly ApiService _apiService;
    private readonly DispositivoEsp? _dispositivo;
    private TextBox? _txtNome;
    private TextBox? _txtIp;
    private NumericUpDown? _numPorta;
    private TextBox? _txtComando;
    private TextBox? _txtComandToEsp;
    private Button? _btnSalvar;

    public DispositivoEspEditForm(ApiService apiService, DispositivoEsp? dispositivo)
    {
        _apiService = apiService;
        _dispositivo = dispositivo;
        InitializeComponent();
        if (_dispositivo != null)
        {
            _txtNome!.Text = _dispositivo.Nome;
            _txtIp!.Text = _dispositivo.Ip;
            _numPorta!.Value = _dispositivo.Porta;
            _txtComando!.Text = _dispositivo.Comando ?? "";
            _txtComandToEsp!.Text = _dispositivo.ComandToEsp ?? "";
        }
    }

    private void InitializeComponent()
    {
        this.Text = _dispositivo == null ? "Criar Dispositivo ESP" : "Editar Dispositivo ESP";
        this.Size = new Size(500, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblNome = new Label
        {
            Text = "Nome",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtNome = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 50)
        };

        var lblIp = new Label
        {
            Text = "IP",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 100)
        };

        _txtIp = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 130)
        };

        var lblPorta = new Label
        {
            Text = "Porta",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 180)
        };

        _numPorta = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(440, 35),
            Location = new Point(20, 210),
            Minimum = 1,
            Maximum = 65535,
            Value = 8888
        };

        var lblComando = new Label
        {
            Text = "Comando",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 260)
        };

        _txtComando = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 290)
        };

        var lblComandToEsp = new Label
        {
            Text = "Comando para ESP",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 340)
        };

        _txtComandToEsp = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 370)
        };

        _btnSalvar = new Button
        {
            Text = "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(440, 40),
            Location = new Point(20, 420),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnSalvar.Click += BtnSalvar_Click;

        this.Controls.Add(lblNome);
        this.Controls.Add(_txtNome);
        this.Controls.Add(lblIp);
        this.Controls.Add(_txtIp);
        this.Controls.Add(lblPorta);
        this.Controls.Add(_numPorta);
        this.Controls.Add(lblComando);
        this.Controls.Add(_txtComando);
        this.Controls.Add(lblComandToEsp);
        this.Controls.Add(_txtComandToEsp);
        this.Controls.Add(_btnSalvar);
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (string.IsNullOrWhiteSpace(_txtNome!.Text) || string.IsNullOrWhiteSpace(_txtIp!.Text))
        {
            MessageBox.Show("Preencha nome e IP!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSalvar!.Enabled = false;
        _btnSalvar.Text = "SALVANDO...";

        try
        {
            var dispositivo = new DispositivoEsp
            {
                Nome = _txtNome.Text,
                Ip = _txtIp.Text,
                Porta = (int)_numPorta!.Value,
                Comando = _txtComando!.Text,
                ComandToEsp = _txtComandToEsp!.Text
            };

            bool success;
            if (_dispositivo == null)
            {
                success = await _apiService.CreateDispositivoEspAsync(dispositivo) != null;
            }
            else
            {
                dispositivo.Id = _dispositivo.Id;
                dispositivo.Status = _dispositivo.Status;
                dispositivo.LigadoDesligado = _dispositivo.LigadoDesligado;
                success = await _apiService.UpdateDispositivoEspAsync(_dispositivo.Id, dispositivo);
            }

            if (success)
            {
                SoundPlayer.PlaySuccess();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao salvar dispositivo!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvar.Enabled = true;
            _btnSalvar.Text = "SALVAR";
        }
    }
}

