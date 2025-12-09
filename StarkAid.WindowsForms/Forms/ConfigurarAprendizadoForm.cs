using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class ConfigurarAprendizadoForm : Form
{
    private readonly LocalDatabase _database;
    private DataGridView? _dgvAprendizados;
    private Button? _btnEditar;
    private Button? _btnDeletar;
    private List<Aprendizado> _aprendizados = new();

    public ConfigurarAprendizadoForm(LocalDatabase database)
    {
        _database = database;
        InitializeComponent();
        LoadAprendizados();
    }

    private void InitializeComponent()
    {
        this.Text = "Configurar Aprendizado";
        this.Size = new Size(1000, 680);
        this.MinimumSize = new Size(1000, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _dgvAprendizados = new DataGridView
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
        _dgvAprendizados.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvAprendizados.ColumnHeadersHeight = 35;
        _dgvAprendizados.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvAprendizados.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvAprendizados.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvAprendizados.Columns.Add("Id", "ID");
        _dgvAprendizados.Columns.Add("Comando", "Comando do Usuário");
        _dgvAprendizados.Columns.Add("Resposta", "Resposta da IA");
        _dgvAprendizados.Columns.Add("DataCriacao", "Data de Criação");
        _dgvAprendizados.Columns[0].Visible = false;
        _dgvAprendizados.Columns[3].Width = 200;

        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(25, 25, 35)
        };

        _btnEditar = new Button
        {
            Text = "EDITAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(20, 10),
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
            Location = new Point(150, 10),
            Cursor = Cursors.Hand
        };
        _btnDeletar.FlatAppearance.BorderSize = 0;
        _btnDeletar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnDeletar.Click += BtnDeletar_Click;

        pnlButtons.Controls.Add(_btnEditar);
        pnlButtons.Controls.Add(_btnDeletar);

        this.Controls.Add(_dgvAprendizados);
        this.Controls.Add(pnlButtons);
    }

    private void LoadAprendizados()
    {
        _aprendizados = _database.GetAprendizados();
        _dgvAprendizados!.Rows.Clear();
        foreach (var aprendizado in _aprendizados)
        {
            _dgvAprendizados.Rows.Add(
                aprendizado.Id.ToString(),
                aprendizado.ComandoUser,
                aprendizado.RespostaIa,
                aprendizado.DataCriacao.ToString("dd/MM/yyyy HH:mm")
            );
        }
    }

    private void BtnEditar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvAprendizados!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um aprendizado para editar!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = int.Parse(_dgvAprendizados.SelectedRows[0].Cells[0].Value.ToString()!);
        var aprendizado = _aprendizados.FirstOrDefault(a => a.Id == id);
        if (aprendizado == null) return;

        var form = new AprendizadoEditForm(_database, aprendizado);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAprendizados();
        }
    }

    private void BtnDeletar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvAprendizados!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um aprendizado para deletar!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = int.Parse(_dgvAprendizados.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este aprendizado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _database.DeleteAprendizado(id);
            SoundPlayer.PlaySuccess();
            LoadAprendizados();
        }
    }
}

