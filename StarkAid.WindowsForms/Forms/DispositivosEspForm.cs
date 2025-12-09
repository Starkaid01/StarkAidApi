using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class DispositivosEspForm : Form
{
    private readonly ApiService _apiService;
    private readonly LocalDatabase _database;
    private DataGridView? _dgvDispositivos;
    private Button? _btnCriar;
    private Button? _btnEditar;
    private Button? _btnDeletar;
    private Button? _btnLigarDesligar;
    private List<DispositivoEsp> _dispositivos = new();
    private bool _isOnline = false;

    public DispositivosEspForm(ApiService apiService, LocalDatabase database)
    {
        _apiService = apiService;
        _database = database;
        InitializeComponent();
        LoadDispositivos();
    }

    private void InitializeComponent()
    {
        this.Text = "Dispositivos ESP";
        this.Size = new Size(1000, 680);
        this.MinimumSize = new Size(1000, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _dgvDispositivos = new DataGridView
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
        _dgvDispositivos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvDispositivos.ColumnHeadersHeight = 35;
        _dgvDispositivos.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvDispositivos.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvDispositivos.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvDispositivos.Columns.Add("Id", "ID");
        _dgvDispositivos.Columns.Add("Nome", "Nome");
        _dgvDispositivos.Columns.Add("Ip", "IP");
        _dgvDispositivos.Columns.Add("Porta", "Porta");
        _dgvDispositivos.Columns.Add("Comando", "Comando");
        _dgvDispositivos.Columns.Add("ComandToEsp", "Comando para ESP");
        _dgvDispositivos.Columns.Add("Status", "Status");
        _dgvDispositivos.Columns[0].Visible = false;

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
            Text = "LIGAR/DESLIGAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Orange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 40),
            Location = new Point(410, 10),
            Cursor = Cursors.Hand
        };
        _btnLigarDesligar.FlatAppearance.BorderSize = 0;
        _btnLigarDesligar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnLigarDesligar.Click += BtnLigarDesligar_Click;

        pnlButtons.Controls.Add(_btnCriar);
        pnlButtons.Controls.Add(_btnEditar);
        pnlButtons.Controls.Add(_btnDeletar);
        pnlButtons.Controls.Add(_btnLigarDesligar);

        this.Controls.Add(_dgvDispositivos);
        this.Controls.Add(pnlButtons);
    }

    private async void LoadDispositivos()
    {
        // Verificar status da API
        _isOnline = await _apiService.CheckApiStatusAsync();
        
        if (_isOnline)
        {
            // Se online, buscar da API
            _dispositivos = await _apiService.GetDispositivosEspAsync();
        }
        else
        {
            // Se offline, buscar do banco local
            _dispositivos = _database.GetDispositivosEsp();
        }
        
        _dgvDispositivos!.Rows.Clear();
        foreach (var dispositivo in _dispositivos)
        {
            _dgvDispositivos.Rows.Add(
                dispositivo.Id.ToString(),
                dispositivo.Nome,
                dispositivo.Ip,
                dispositivo.Porta,
                dispositivo.Comando ?? "",
                dispositivo.ComandToEsp ?? "",
                dispositivo.Status
            );
        }
        
        // Desabilitar botões de edição quando offline
        _btnCriar!.Enabled = _isOnline;
        _btnEditar!.Enabled = _isOnline;
        _btnDeletar!.Enabled = _isOnline;
        _btnLigarDesligar!.Enabled = _isOnline;
        
        if (!_isOnline)
        {
            _btnCriar.BackColor = Color.Gray;
            _btnEditar.BackColor = Color.Gray;
            _btnDeletar.BackColor = Color.Gray;
            _btnLigarDesligar.BackColor = Color.Gray;
        }
        else
        {
            _btnCriar.BackColor = Color.Green;
            _btnEditar.BackColor = Color.Cyan;
            _btnDeletar.BackColor = Color.Red;
            _btnLigarDesligar.BackColor = Color.Orange;
        }
    }

    private void BtnCriar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível criar dispositivos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        var form = new DispositivoEspEditForm(_apiService, null);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadDispositivos();
        }
    }

    private void BtnEditar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível editar dispositivos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        if (_dgvDispositivos!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvDispositivos.SelectedRows[0].Cells[0].Value.ToString()!);
        var dispositivo = _dispositivos.FirstOrDefault(d => d.Id == id);
        if (dispositivo == null) return;

        var form = new DispositivoEspEditForm(_apiService, dispositivo);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadDispositivos();
        }
    }

    private async void BtnDeletar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível deletar dispositivos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        if (_dgvDispositivos!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvDispositivos.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este dispositivo?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            if (await _apiService.DeleteDispositivoEspAsync(id))
            {
                SoundPlayer.PlaySuccess();
                LoadDispositivos();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao deletar dispositivo!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnLigarDesligar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível ligar/desligar dispositivos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        if (_dgvDispositivos!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvDispositivos.SelectedRows[0].Cells[0].Value.ToString()!);
        var dispositivo = _dispositivos.FirstOrDefault(d => d.Id == id);
        if (dispositivo == null) return;

        dispositivo.LigadoDesligado = !dispositivo.LigadoDesligado;
        if (await _apiService.UpdateDispositivoEspAsync(id, dispositivo))
        {
            SoundPlayer.PlaySuccess();
            LoadDispositivos();
        }
        else
        {
            SoundPlayer.PlayError();
            MessageBox.Show("Erro ao atualizar dispositivo!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

