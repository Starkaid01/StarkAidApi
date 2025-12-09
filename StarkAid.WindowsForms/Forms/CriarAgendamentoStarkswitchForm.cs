using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class CriarAgendamentoStarkswitchForm : Form
{
    private readonly ApiService _apiService;
    private ComboBox? _cmbDevice;
    private ComboBox? _cmbAcao;
    private DateTimePicker? _dtpData;
    private NumericUpDown? _numHora;
    private NumericUpDown? _numMinuto;
    private ComboBox? _cmbRecorrencia;
    private Button? _btnSalvar;
    private List<Device> _devices = new();

    public CriarAgendamentoStarkswitchForm(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
        LoadDevices();
    }

    private void InitializeComponent()
    {
        this.Text = "Criar Agendamento Starkswitch";
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblDevice = new Label
        {
            Text = "Dispositivo Starkswitch",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _cmbDevice = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(440, 35),
            Location = new Point(20, 50)
        };

        var lblAcao = new Label
        {
            Text = "Ação",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 100)
        };

        _cmbAcao = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(440, 35),
            Location = new Point(20, 130)
        };
        _cmbAcao.Items.AddRange(new[] { "ligar", "desligar" });
        _cmbAcao.SelectedIndex = 0;

        var lblData = new Label
        {
            Text = "Data",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 180)
        };

        _dtpData = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Format = DateTimePickerFormat.Short,
            Size = new Size(440, 35),
            Location = new Point(20, 210),
            MinDate = DateTime.Today
        };

        var lblHora = new Label
        {
            Text = "Hora (0-23)",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 260)
        };

        _numHora = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(440, 35),
            Location = new Point(20, 290),
            Minimum = 0,
            Maximum = 23,
            Value = DateTime.Now.Hour
        };

        var lblMinuto = new Label
        {
            Text = "Minuto (0-59)",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 340)
        };

        _numMinuto = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(440, 35),
            Location = new Point(20, 370),
            Minimum = 0,
            Maximum = 59,
            Value = DateTime.Now.Minute
        };

        var lblRecorrencia = new Label
        {
            Text = "Recorrência",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 420)
        };

        _cmbRecorrencia = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(440, 35),
            Location = new Point(20, 450),
            DropDownHeight = 150
        };
        _cmbRecorrencia.Items.AddRange(new[] { "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno" });
        _cmbRecorrencia.SelectedIndex = 0;

        _btnSalvar = new Button
        {
            Text = "CRIAR AGENDAMENTO",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Blue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(440, 40),
            Location = new Point(20, 500),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnSalvar.Click += BtnSalvar_Click;

        this.Size = new Size(500, 580);

        this.Controls.Add(lblDevice);
        this.Controls.Add(_cmbDevice);
        this.Controls.Add(lblAcao);
        this.Controls.Add(_cmbAcao);
        this.Controls.Add(lblData);
        this.Controls.Add(_dtpData);
        this.Controls.Add(lblHora);
        this.Controls.Add(_numHora);
        this.Controls.Add(lblMinuto);
        this.Controls.Add(_numMinuto);
        this.Controls.Add(lblRecorrencia);
        this.Controls.Add(_cmbRecorrencia);
        this.Controls.Add(_btnSalvar);
    }

    private async void LoadDevices()
    {
        _devices = await _apiService.GetDevicesAsync();
        _cmbDevice!.Items.Clear();
        foreach (var device in _devices)
        {
            _cmbDevice.Items.Add(device.Name);
        }
        if (_cmbDevice.Items.Count > 0)
        {
            _cmbDevice.SelectedIndex = 0;
        }
    }

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        
        if (_cmbDevice!.SelectedIndex < 0)
        {
            MessageBox.Show("Selecione um dispositivo Starkswitch!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var device = _devices[_cmbDevice.SelectedIndex];
        var acao = _cmbAcao!.SelectedItem?.ToString() ?? "ligar";
        var data = _dtpData!.Value.Date;
        var hora = (int)_numHora!.Value;
        var minuto = (int)_numMinuto!.Value;
        var recorrencia = _cmbRecorrencia!.SelectedItem?.ToString() ?? "NaoRepetir";

        _btnSalvar!.Enabled = false;
        _btnSalvar.Text = "CRIANDO...";

        try
        {
            var agendamento = await _apiService.CreateAgendamentoStarkswitchAsync(
                device.Id,
                acao,
                data,
                hora,
                minuto,
                recorrencia
            );

            if (agendamento != null)
            {
                SoundPlayer.PlaySuccess();
                MessageBox.Show("Agendamento Starkswitch criado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao criar agendamento Starkswitch!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _btnSalvar.Text = "CRIAR AGENDAMENTO";
        }
    }
}
