using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class ListaAgendamentosArquivosForm : Form
{
    private readonly LocalDatabase _database;
    private Panel? _pnlContent;
    private Button? _btnNovo;

    public ListaAgendamentosArquivosForm(LocalDatabase database)
    {
        _database = database;
        InitializeComponent();
        LoadAgendamentos();
    }

    private void InitializeComponent()
    {
        this.Text = "Agendamentos de Arquivos";
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(15, 15, 25);
        this.FormBorderStyle = FormBorderStyle.None;

        // Title Bar
        var titleBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(35, 35, 45) };
        var lblTitle = new Label 
        { 
            Text = "📅 AGENDAMENTOS DE ARQUIVOS", 
            Font = new Font("Segoe UI", 16, FontStyle.Bold), 
            ForeColor = Color.Cyan, 
            AutoSize = true, 
            Location = new Point(20, 15) 
        };
        var btnClose = new Button 
        { 
            Text = "✕", 
            Font = new Font("Segoe UI", 14, FontStyle.Bold), 
            BackColor = Color.Transparent, 
            ForeColor = Color.White, 
            FlatStyle = FlatStyle.Flat, 
            Size = new Size(40, 40), 
            Dock = DockStyle.Right, 
            Cursor = Cursors.Hand 
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Red; };
        btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; };
        btnClose.Click += (s, e) => this.Close();

        _btnNovo = new Button
        {
            Text = "➕ NOVO AGENDAMENTO",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 35),
            Location = new Point(this.Width - 250, 7),
            Cursor = Cursors.Hand
        };
        _btnNovo.FlatAppearance.BorderSize = 0;
        _btnNovo.Click += BtnNovo_Click;

        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(_btnNovo);
        titleBar.Controls.Add(btnClose);

        // Content Panel
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(25, 25, 35),
            Padding = new Padding(20, 60, 20, 20) // Margem superior maior para espaçar do titleBar
        };

        this.Controls.Add(titleBar);
        this.Controls.Add(_pnlContent);
    }

    private void LoadAgendamentos()
    {
        _pnlContent!.Controls.Clear();

        try
        {
            var agendamentos = _database.GetAgendamentosArquivos();

            if (agendamentos.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "Nenhum agendamento encontrado.\nClique em 'NOVO AGENDAMENTO' para criar um.",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.Gray,
                    AutoSize = false,
                    Size = new Size(800, 100),
                    Location = new Point(50, 80),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlContent.Controls.Add(lblEmpty);
                return;
            }

            int yPos = 60; // Aumentar margem inicial do topo
            foreach (var agendamento in agendamentos)
            {
                var card = CreateAgendamentoCard(agendamento, yPos);
                _pnlContent.Controls.Add(card);
                yPos += card.Height + 15;
            }
        }
        catch (Exception ex)
        {
            var lblError = new Label
            {
                Text = $"Erro ao carregar agendamentos: {ex.Message}",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Red,
                AutoSize = true,
                Location = new Point(20, 40) // Aumentar margem do topo
            };
            _pnlContent.Controls.Add(lblError);
        }
    }

    private Panel CreateAgendamentoCard(AgendamentoArquivo agendamento, int yPos)
    {
        var card = new Panel
        {
            BackColor = Color.FromArgb(35, 35, 45),
            Size = new Size(840, 180), // Aumentado para acomodar melhor o conteúdo
            Location = new Point(0, yPos),
            Padding = new Padding(20, 20, 20, 20) // Padding uniforme de 20px em todos os lados
        };

        // Nome do arquivo
        var fileName = Path.GetFileName(agendamento.CaminhoArquivo);
        var lblArquivo = new Label
        {
            Text = $"📄 {fileName}",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20) // Margem esquerda e topo maiores
        };

        // Data/Hora
        var lblDataHora = new Label
        {
            Text = $"📅 {agendamento.DataHora:dd/MM/yyyy HH:mm}",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(20, 50) // Margem esquerda e espaçamento vertical
        };

        // Frequência
        var frequenciaText = agendamento.Frequencia switch
        {
            FrequenciaAgendamento.Nenhum => "Uma vez",
            FrequenciaAgendamento.PorHora => "Por Hora",
            FrequenciaAgendamento.PorMinuto => "Por Minuto",
            FrequenciaAgendamento.Diariamente => "Diariamente",
            FrequenciaAgendamento.Semanalmente => "Semanalmente",
            FrequenciaAgendamento.Mensalmente => "Mensalmente",
            _ => "Desconhecido"
        };

        var lblFrequencia = new Label
        {
            Text = $"🔄 {frequenciaText}",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(20, 75) // Margem esquerda e espaçamento vertical
        };

        // Status
        var statusColor = agendamento.Ativo ? Color.FromArgb(16, 185, 129) : Color.Gray;
        var lblStatus = new Label
        {
            Text = agendamento.Ativo ? "✅ ATIVO" : "⏸️ INATIVO",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = statusColor,
            AutoSize = true,
            Location = new Point(20, 100) // Margem esquerda e espaçamento vertical
        };

        // Última execução
        if (agendamento.UltimaExecucao.HasValue)
        {
            var lblUltimaExec = new Label
            {
                Text = $"⏰ Última execução: {agendamento.UltimaExecucao.Value:dd/MM/yyyy HH:mm:ss}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(20, 125) // Margem esquerda e espaçamento vertical
            };
            card.Controls.Add(lblUltimaExec);
        }

        // Botões
        var btnEditar = new Button
        {
            Text = "✏️ Editar",
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(50, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 30),
            Location = new Point(680, 20),
            Cursor = Cursors.Hand
        };
        btnEditar.FlatAppearance.BorderSize = 0;
        btnEditar.Click += (s, e) => EditarAgendamento(agendamento);

        var btnToggleAtivo = new Button
        {
            Text = agendamento.Ativo ? "⏸️ Desativar" : "▶️ Ativar",
            Font = new Font("Segoe UI", 9),
            BackColor = agendamento.Ativo ? Color.FromArgb(200, 150, 50) : Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 30),
            Location = new Point(680, 55),
            Cursor = Cursors.Hand
        };
        btnToggleAtivo.FlatAppearance.BorderSize = 0;
        btnToggleAtivo.Click += (s, e) => ToggleAtivo(agendamento);

        var btnExcluir = new Button
        {
            Text = "🗑️ Excluir",
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(200, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 30),
            Location = new Point(680, 90),
            Cursor = Cursors.Hand
        };
        btnExcluir.FlatAppearance.BorderSize = 0;
        btnExcluir.Click += (s, e) => ExcluirAgendamento(agendamento);

        card.Controls.Add(lblArquivo);
        card.Controls.Add(lblDataHora);
        card.Controls.Add(lblFrequencia);
        card.Controls.Add(lblStatus);
        card.Controls.Add(btnEditar);
        card.Controls.Add(btnToggleAtivo);
        card.Controls.Add(btnExcluir);

        return card;
    }

    private void BtnNovo_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        using var form = new AgendamentoArquivoForm(_database);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAgendamentos();
        }
    }

    private void EditarAgendamento(AgendamentoArquivo agendamento)
    {
        SoundPlayer.PlayClick();
        using var form = new AgendamentoArquivoForm(_database, agendamento);
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadAgendamentos();
        }
    }

    private void ToggleAtivo(AgendamentoArquivo agendamento)
    {
        SoundPlayer.PlayClick();
        agendamento.Ativo = !agendamento.Ativo;
        _database.SaveAgendamentoArquivo(agendamento);
        LoadAgendamentos();
    }

    private void ExcluirAgendamento(AgendamentoArquivo agendamento)
    {
        SoundPlayer.PlayClick();
        if (MessageBox.Show(
            $"Tem certeza que deseja excluir o agendamento de '{Path.GetFileName(agendamento.CaminhoArquivo)}'?",
            "Confirmar Exclusão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            _database.DeleteAgendamentoArquivo(agendamento.Id);
            LoadAgendamentos();
        }
    }
}

