using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class ComandosShellForm : Form
{
    private readonly LocalDatabase _database;
    private DataGridView? _dgvComandos;
    private Button? _btnCriar;
    private Button? _btnEditar;
    private Button? _btnDeletar;
    private Button? _btnExecutar;
    private List<ComandoShell> _comandos = new();

    public ComandosShellForm(LocalDatabase database)
    {
        _database = database;
        InitializeComponent();
        LoadComandos();
    }

    private void InitializeComponent()
    {
        this.Text = "Comandos Shell";
        this.Size = new Size(1000, 680);
        this.MinimumSize = new Size(1000, 680);
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
        _dgvComandos.Columns.Add("ComandoInput", "Comando Input");
        _dgvComandos.Columns.Add("Resposta", "Resposta");
        _dgvComandos.Columns.Add("ComandoCMD", "Comando CMD");
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

        _btnExecutar = new Button
        {
            Text = "EXECUTAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Orange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(410, 10),
            Cursor = Cursors.Hand
        };
        _btnExecutar.FlatAppearance.BorderSize = 0;
        _btnExecutar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnExecutar.Click += BtnExecutar_Click;

        pnlButtons.Controls.Add(_btnCriar);
        pnlButtons.Controls.Add(_btnEditar);
        pnlButtons.Controls.Add(_btnDeletar);
        pnlButtons.Controls.Add(_btnExecutar);

        this.Controls.Add(_dgvComandos);
        this.Controls.Add(pnlButtons);
    }

    private void LoadComandos()
    {
        _comandos = _database.GetComandosShell();
        _dgvComandos!.Rows.Clear();
        foreach (var cmd in _comandos)
        {
            _dgvComandos.Rows.Add(cmd.Id.ToString(), cmd.ComandoInput, cmd.Resposta, cmd.ComandoCMD);
        }
    }

    private void BtnCriar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var form = new ComandoShellEditForm(_database, null);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadComandos();
        }
    }

    private void BtnEditar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvComandos!.SelectedRows.Count == 0) return;

        var id = int.Parse(_dgvComandos.SelectedRows[0].Cells[0].Value.ToString()!);
        var comando = _comandos.FirstOrDefault(c => c.Id == id);
        if (comando == null) return;

        var form = new ComandoShellEditForm(_database, comando);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadComandos();
        }
    }

    private void BtnDeletar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvComandos!.SelectedRows.Count == 0) return;

        var id = int.Parse(_dgvComandos.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este comando?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _database.DeleteComandoShell(id);
            SoundPlayer.PlaySuccess();
            LoadComandos();
        }
    }

    private void BtnExecutar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvComandos!.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um comando para executar!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = int.Parse(_dgvComandos.SelectedRows[0].Cells[0].Value.ToString()!);
        var comando = _comandos.FirstOrDefault(c => c.Id == id);
        if (comando == null) return;

        try
        {
            // Executar comando CMD
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {comando.ComandoCMD}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao executar comando: {error}");
                }

                System.Diagnostics.Debug.WriteLine($"Comando executado: {comando.ComandoCMD}");
                System.Diagnostics.Debug.WriteLine($"Saída: {output}");

                MessageBox.Show($"Comando executado com sucesso!\n\nComando: {comando.ComandoCMD}", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao executar comando: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

