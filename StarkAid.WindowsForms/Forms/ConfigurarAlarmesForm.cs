using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class ConfigurarAlarmesForm : Form
{
    private readonly LocalDatabase _database;
    private DataGridView? _dgvAlarmes;
    private Button? _btnEditar;
    private Button? _btnDeletar;
    private Button? _btnMarcarConcluido;
    private List<Lembrete> _lembretes = new();

    public ConfigurarAlarmesForm(LocalDatabase database)
    {
        _database = database;
        InitializeComponent();
        LoadAlarmes();
    }

    private void InitializeComponent()
    {
        this.Text = "Configurar Alarmes";
        this.Size = new Size(1100, 680);
        this.MinimumSize = new Size(1100, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _dgvAlarmes = new DataGridView
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
        _dgvAlarmes.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvAlarmes.ColumnHeadersHeight = 35;
        _dgvAlarmes.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvAlarmes.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvAlarmes.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvAlarmes.Columns.Add("Id", "ID");
        _dgvAlarmes.Columns.Add("Lembrar", "Lembrar de");
        _dgvAlarmes.Columns.Add("Data", "Data");
        _dgvAlarmes.Columns.Add("Hora", "Hora");
        _dgvAlarmes.Columns.Add("Status", "Status");
        _dgvAlarmes.Columns.Add("DataCriacao", "Data de Criação");
        _dgvAlarmes.Columns[0].Visible = false;
        
        // Todas as colunas visíveis usarão Fill para ocupar todo o espaço
        _dgvAlarmes.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgvAlarmes.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgvAlarmes.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgvAlarmes.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dgvAlarmes.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

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

        _btnMarcarConcluido = new Button
        {
            Text = "MARCAR CONCLUÍDO",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 40),
            Location = new Point(150, 10),
            Cursor = Cursors.Hand
        };
        _btnMarcarConcluido.FlatAppearance.BorderSize = 0;
        _btnMarcarConcluido.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnMarcarConcluido.Click += BtnMarcarConcluido_Click;

        _btnDeletar = new Button
        {
            Text = "DELETAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(330, 10),
            Cursor = Cursors.Hand
        };
        _btnDeletar.FlatAppearance.BorderSize = 0;
        _btnDeletar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnDeletar.Click += BtnDeletar_Click;

        pnlButtons.Controls.Add(_btnEditar);
        pnlButtons.Controls.Add(_btnMarcarConcluido);
        pnlButtons.Controls.Add(_btnDeletar);

        this.Controls.Add(_dgvAlarmes);
        this.Controls.Add(pnlButtons);
    }

    private void LoadAlarmes()
    {
        _lembretes = _database.GetLembretes(false); // Todos os lembretes
        _dgvAlarmes!.Rows.Clear();
        foreach (var lembrete in _lembretes)
        {
            string dataStr = "";
            if (lembrete.Dia.HasValue && lembrete.Mes.HasValue)
            {
                var ano = DateTime.Now.Year;
                if (lembrete.Mes < DateTime.Now.Month || (lembrete.Mes == DateTime.Now.Month && lembrete.Dia < DateTime.Now.Day))
                {
                    ano = DateTime.Now.Year + 1;
                }
                try
                {
                    var data = new DateTime(ano, lembrete.Mes.Value, lembrete.Dia.Value);
                    dataStr = data.ToString("dd/MM/yyyy");
                }
                catch
                {
                    dataStr = $"{lembrete.Dia.Value}/{lembrete.Mes.Value}";
                }
            }
            else
            {
                dataStr = "Diário";
            }

            string horaStr = "";
            if (lembrete.Hora.HasValue && lembrete.Minuto.HasValue)
            {
                horaStr = $"{lembrete.Hora.Value:D2}:{lembrete.Minuto.Value:D2}";
            }
            else if (lembrete.Hora.HasValue)
            {
                horaStr = $"{lembrete.Hora.Value:D2}:00";
            }

            var status = lembrete.Concluido ? "Concluído" : "Pendente";
            
            _dgvAlarmes.Rows.Add(
                lembrete.Id.ToString(),
                lembrete.Lembrar,
                dataStr,
                horaStr,
                status,
                lembrete.DataCriacao.ToString("dd/MM/yyyy HH:mm")
            );
        }
    }

    private void BtnEditar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvAlarmes!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um alarme para editar!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = int.Parse(_dgvAlarmes.SelectedRows[0].Cells[0].Value.ToString()!);
        var lembrete = _lembretes.FirstOrDefault(l => l.Id == id);
        if (lembrete == null) return;

        var form = new AlarmeEditForm(_database, lembrete);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAlarmes();
        }
    }

    private void BtnMarcarConcluido_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvAlarmes!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um alarme para marcar como concluído!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = int.Parse(_dgvAlarmes.SelectedRows[0].Cells[0].Value.ToString()!);
        var lembrete = _lembretes.FirstOrDefault(l => l.Id == id);
        if (lembrete == null) return;

        if (lembrete.Concluido)
        {
            MessageBox.Show("Este alarme já está concluído!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _database.MarcarLembreteConcluido(id);
        SoundPlayer.PlaySuccess();
        LoadAlarmes();
    }

    private void BtnDeletar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvAlarmes!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um alarme para deletar!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = int.Parse(_dgvAlarmes.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este alarme?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _database.DeleteLembrete(id);
            SoundPlayer.PlaySuccess();
            LoadAlarmes();
        }
    }
}

