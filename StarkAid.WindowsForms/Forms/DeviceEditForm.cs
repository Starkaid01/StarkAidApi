using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class DeviceEditForm : Form
{
    private readonly ApiService _apiService;
    private readonly Device? _device;
    private TextBox? _txtName;
    private TextBox? _txtComando;
    private Button? _btnSalvar;

    public DeviceEditForm(ApiService apiService, Device? device)
    {
        _apiService = apiService;
        _device = device;
        InitializeComponent();
        if (_device != null)
        {
            _txtName!.Text = _device.Name;
            _txtComando!.Text = _device.Comando ?? "";
        }
    }

    private void InitializeComponent()
    {
        this.Text = _device == null ? "Criar Dispositivo" : "Editar Dispositivo";
        this.Size = new Size(500, 250);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblName = new Label
        {
            Text = "Nome",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtName = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 50)
        };

        var lblComando = new Label
        {
            Text = "Comando",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 100)
        };

        _txtComando = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 130)
        };

        _btnSalvar = new Button
        {
            Text = "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(440, 40),
            Location = new Point(20, 180),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnSalvar.Click += BtnSalvar_Click;

        this.Controls.Add(lblName);
        this.Controls.Add(_txtName);
        this.Controls.Add(lblComando);
        this.Controls.Add(_txtComando);
        this.Controls.Add(_btnSalvar);
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (string.IsNullOrWhiteSpace(_txtName!.Text))
        {
            MessageBox.Show("Preencha o nome!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSalvar!.Enabled = false;
        _btnSalvar.Text = "SALVANDO...";

        try
        {
            bool success;
            if (_device == null)
            {
                success = await _apiService.CreateDeviceAsync(_txtName.Text, _txtComando!.Text) != null;
            }
            else
            {
                success = await _apiService.UpdateDeviceAsync(_device.Id, _txtName.Text, _txtComando!.Text);
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

