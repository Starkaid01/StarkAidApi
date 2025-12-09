using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class ComandosSociaisForm : Form
{
    private readonly ApiService _apiService;
    private readonly LocalDatabase _database;
    private DataGridView? _dgvComandos;
    private Button? _btnCriar;
    private Button? _btnEditar;
    private Button? _btnDeletar;
    private List<ComandoSocial> _comandos = new();
    private bool _isOnline = false;

    public ComandosSociaisForm(ApiService apiService, LocalDatabase database)
    {
        _apiService = apiService;
        _database = database;
        InitializeComponent();
        LoadComandos();
    }

    private void InitializeComponent()
    {
        this.Text = "Comandos Sociais";
        this.Size = new Size(900, 680);
        this.MinimumSize = new Size(900, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _dgvComandos = new DataGridView
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
        _dgvComandos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvComandos.ColumnHeadersHeight = 35;
        _dgvComandos.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvComandos.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvComandos.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvComandos.Columns.Add("Id", "ID");
        _dgvComandos.Columns.Add("Comando", "Comando");
        _dgvComandos.Columns.Add("Resposta", "Resposta");
        _dgvComandos.Columns[0].Visible = false;

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

        pnlButtons.Controls.Add(_btnCriar);
        pnlButtons.Controls.Add(_btnEditar);
        pnlButtons.Controls.Add(_btnDeletar);

        this.Controls.Add(_dgvComandos);
        this.Controls.Add(pnlButtons);
    }

    private async void LoadComandos()
    {
        // Verificar status da API
        _isOnline = await _apiService.CheckApiStatusAsync();
        
        if (_isOnline)
        {
            // Se online, buscar da API
            _comandos = await _apiService.GetComandosSociaisAsync();
        }
        else
        {
            // Se offline, buscar do banco local
            _comandos = _database.GetComandosSociais();
        }
        
        _dgvComandos!.Rows.Clear();
        foreach (var cmd in _comandos)
        {
            _dgvComandos.Rows.Add(cmd.Id.ToString(), cmd.Comando, cmd.Resposta);
        }
        
        // Desabilitar botões de edição quando offline
        _btnCriar!.Enabled = _isOnline;
        _btnEditar!.Enabled = _isOnline;
        _btnDeletar!.Enabled = _isOnline;
        
        if (!_isOnline)
        {
            _btnCriar.BackColor = Color.Gray;
            _btnEditar.BackColor = Color.Gray;
            _btnDeletar.BackColor = Color.Gray;
        }
        else
        {
            _btnCriar.BackColor = Color.Green;
            _btnEditar.BackColor = Color.Cyan;
            _btnDeletar.BackColor = Color.Red;
        }
    }

    private void BtnCriar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível criar comandos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        var form = new ComandoSocialEditForm(_apiService, null);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadComandos();
        }
    }

    private void BtnEditar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível editar comandos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        if (_dgvComandos!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvComandos.SelectedRows[0].Cells[0].Value.ToString()!);
        var comando = _comandos.FirstOrDefault(c => c.Id == id);
        if (comando == null) return;

        var form = new ComandoSocialEditForm(_apiService, comando);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadComandos();
        }
    }

    private async void BtnDeletar_Click(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            MessageBox.Show("Não é possível deletar comandos enquanto a API estiver offline.", "API Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        SoundPlayer.PlayClick();
        if (_dgvComandos!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvComandos.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este comando?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            if (await _apiService.DeleteComandoSocialAsync(id))
            {
                SoundPlayer.PlaySuccess();
                LoadComandos();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao deletar comando!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

