using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class DevicesForm : Form
{
    private readonly ApiService _apiService;
    private DataGridView? _dgvDevices;
    private Button? _btnCriar;
    private Button? _btnEditar;
    private Button? _btnDeletar;
    private Button? _btnLigarDesligar;
    private List<Device> _devices = new();

    public DevicesForm(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
        LoadDevices();
    }

    private void InitializeComponent()
    {
        this.Text = "Dispositivos StarkSwitch";
        this.Size = new Size(900, 680);
        this.MinimumSize = new Size(900, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _dgvDevices = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            GridColor = Color.FromArgb(50, 50, 60),
            EnableHeadersVisualStyles = false
        };
        
        // Estilo do cabeçalho
        _dgvDevices.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvDevices.ColumnHeadersHeight = 35;
        _dgvDevices.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvDevices.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvDevices.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvDevices.Columns.Add("Id", "ID");
        _dgvDevices.Columns.Add("Name", "Nome");
        _dgvDevices.Columns.Add("Comando", "Comando");
        _dgvDevices.Columns[0].Visible = false;

        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(25, 25, 35)
        };

        _btnCriar = new Button
        {
            Text = "CRIAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(20, 10),
            Cursor = Cursors.Hand
        };
        _btnCriar.FlatAppearance.BorderSize = 0;
        _btnCriar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnCriar.Click += BtnCriar_Click;

        _btnEditar = new Button
        {
            Text = "EDITAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(150, 10),
            Cursor = Cursors.Hand
        };
        _btnEditar.FlatAppearance.BorderSize = 0;
        _btnEditar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnEditar.Click += BtnEditar_Click;

        _btnDeletar = new Button
        {
            Text = "DELETAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(280, 10),
            Cursor = Cursors.Hand
        };
        _btnDeletar.FlatAppearance.BorderSize = 0;
        _btnDeletar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnDeletar.Click += BtnDeletar_Click;

        _btnLigarDesligar = new Button
        {
            Text = "LIGAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(410, 10),
            Cursor = Cursors.Hand
        };
        _btnLigarDesligar.FlatAppearance.BorderSize = 0;
        _btnLigarDesligar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnLigarDesligar.Click += BtnLigar_Click;

        var btnDesligar = new Button
        {
            Text = "DESLIGAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(540, 10),
            Cursor = Cursors.Hand
        };
        btnDesligar.FlatAppearance.BorderSize = 0;
        btnDesligar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        btnDesligar.Click += BtnDesligar_Click;

        pnlButtons.Controls.Add(_btnCriar);
        pnlButtons.Controls.Add(_btnEditar);
        pnlButtons.Controls.Add(_btnDeletar);
        pnlButtons.Controls.Add(_btnLigarDesligar);
        pnlButtons.Controls.Add(btnDesligar);

        this.Controls.Add(_dgvDevices);
        this.Controls.Add(pnlButtons);
    }

    private async void LoadDevices()
    {
        _devices = await _apiService.GetDevicesAsync();
        _dgvDevices!.Rows.Clear();
        foreach (var device in _devices)
        {
            _dgvDevices.Rows.Add(device.Id.ToString(), device.Name, device.Comando ?? "");
        }
    }

    private void BtnCriar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var form = new DeviceEditForm(_apiService, null);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadDevices();
        }
    }

    private void BtnEditar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvDevices!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvDevices.SelectedRows[0].Cells[0].Value.ToString()!);
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device == null) return;

        var form = new DeviceEditForm(_apiService, device);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadDevices();
        }
    }

    private async void BtnDeletar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvDevices!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvDevices.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este dispositivo?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            if (await _apiService.DeleteDeviceAsync(id))
            {
                SoundPlayer.PlaySuccess();
                LoadDevices();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao deletar dispositivo!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnLigar_Click(object? sender, EventArgs e)
    {
        await EnviarComando("ligar");
    }

    private async void BtnDesligar_Click(object? sender, EventArgs e)
    {
        await EnviarComando("desligar");
    }

    private async Task EnviarComando(string comando)
    {
        SoundPlayer.PlayClick();
        if (_dgvDevices!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um dispositivo primeiro!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = Guid.Parse(_dgvDevices.SelectedRows[0].Cells[0].Value.ToString()!);
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device == null) return;

        try
        {
            if (await _apiService.PublishCommandAsync(id, comando))
            {
                SoundPlayer.PlaySuccess();
                MessageBox.Show($"Comando '{comando}' enviado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao enviar comando. Verifique se o MQTT está conectado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

