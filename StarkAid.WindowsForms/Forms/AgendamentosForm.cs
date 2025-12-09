using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class AgendamentosForm : Form
{
    private readonly ApiService _apiService;
    private DataGridView? _dgvAgendamentos;
    private Button? _btnCriarEsp;
    private Button? _btnCriarStarkswitch;
    private Button? _btnCriarEwelink;
    private Button? _btnDeletar;
    private List<Agendamento> _agendamentos = new();

    public AgendamentosForm(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
        LoadAgendamentos();
    }

    private void InitializeComponent()
    {
        this.Text = "Agendamentos";
        this.Size = new Size(1200, 680);
        this.MinimumSize = new Size(1200, 680);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        _dgvAgendamentos = new DataGridView
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
        _dgvAgendamentos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 25, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        _dgvAgendamentos.ColumnHeadersHeight = 35;
        _dgvAgendamentos.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        
        // Estilo das células padrão
        _dgvAgendamentos.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(50, 150, 200),
            SelectionForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };
        
        // Estilo das linhas alternadas
        _dgvAgendamentos.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White
        };
        
        _dgvAgendamentos.Columns.Add("Id", "ID");
        _dgvAgendamentos.Columns.Add("Tipo", "Tipo");
        _dgvAgendamentos.Columns.Add("DataHora", "Data/Hora");
        _dgvAgendamentos.Columns.Add("Comando", "Comando");
        _dgvAgendamentos.Columns.Add("Recorrencia", "Recorrência");
        _dgvAgendamentos.Columns.Add("Status", "Status");
        _dgvAgendamentos.Columns[0].Visible = false;

        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(25, 25, 35)
        };

        _btnCriarEsp = new Button
        {
            Text = "CRIAR ESP",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 40),
            Location = new Point(20, 10),
            Cursor = Cursors.Hand
        };
        _btnCriarEsp.FlatAppearance.BorderSize = 0;
        _btnCriarEsp.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnCriarEsp.Click += BtnCriarEsp_Click;

        _btnCriarStarkswitch = new Button
        {
            Text = "CRIAR STARKSWITCH",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Blue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 40),
            Location = new Point(180, 10),
            Cursor = Cursors.Hand
        };
        _btnCriarStarkswitch.FlatAppearance.BorderSize = 0;
        _btnCriarStarkswitch.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnCriarStarkswitch.Click += BtnCriarStarkswitch_Click;

        _btnCriarEwelink = new Button
        {
            Text = "CRIAR EWELINK",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Orange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 40),
            Location = new Point(390, 10),
            Cursor = Cursors.Hand
        };
        _btnCriarEwelink.FlatAppearance.BorderSize = 0;
        _btnCriarEwelink.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnCriarEwelink.Click += BtnCriarEwelink_Click;

        _btnDeletar = new Button
        {
            Text = "DELETAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 40),
            Location = new Point(580, 10),
            Cursor = Cursors.Hand
        };
        _btnDeletar.FlatAppearance.BorderSize = 0;
        _btnDeletar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnDeletar.Click += BtnDeletar_Click;

        pnlButtons.Controls.Add(_btnCriarEsp);
        pnlButtons.Controls.Add(_btnCriarStarkswitch);
        pnlButtons.Controls.Add(_btnCriarEwelink);
        pnlButtons.Controls.Add(_btnDeletar);

        this.Controls.Add(_dgvAgendamentos);
        this.Controls.Add(pnlButtons);
    }

    private async void LoadAgendamentos()
    {
        _agendamentos = await _apiService.GetAgendamentosAsync();
        _dgvAgendamentos!.Rows.Clear();
        foreach (var agendamento in _agendamentos)
        {
            var tipo = agendamento.TipoAgendamento switch
            {
                TipoAgendamento.ESP => "ESP",
                TipoAgendamento.Ewelink => "Ewelink",
                _ => "Starkswitch"
            };
            var dataHora = agendamento.AgendadoPara.ToLocalTime();
            var dataFormatada = dataHora.ToString("dd/MM/yyyy HH:mm");
            var status = agendamento.Executado ? "Executado" : "Pendente";
            var recorrencia = agendamento.Recorrencia ?? "Não Repetir";
            
            _dgvAgendamentos.Rows.Add(
                agendamento.Id.ToString(),
                tipo,
                dataFormatada,
                agendamento.Comando ?? "",
                recorrencia,
                status
            );
        }
    }

    private void BtnCriarEsp_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var form = new CriarAgendamentoEspForm(_apiService);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAgendamentos();
        }
    }

    private void BtnCriarStarkswitch_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var form = new CriarAgendamentoStarkswitchForm(_apiService);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAgendamentos();
        }
    }

    private void BtnCriarEwelink_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        var form = new CriarAgendamentoEwelinkForm(_apiService);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAgendamentos();
        }
    }

    private async void BtnDeletar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (_dgvAgendamentos!.SelectedRows.Count == 0) return;

        var id = Guid.Parse(_dgvAgendamentos.SelectedRows[0].Cells[0].Value.ToString()!);
        if (MessageBox.Show("Deseja realmente deletar este agendamento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            if (await _apiService.DeleteAgendamentoAsync(id))
            {
                SoundPlayer.PlaySuccess();
                LoadAgendamentos();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao deletar agendamento!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
