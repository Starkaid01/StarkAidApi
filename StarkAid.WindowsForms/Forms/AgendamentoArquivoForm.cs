using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;

namespace StarkAid.WindowsForms.Forms;

public partial class AgendamentoArquivoForm : Form
{
    private readonly LocalDatabase _database;
    private AgendamentoArquivo? _agendamentoEditando;
    private TextBox? _txtCaminhoArquivo;
    private DateTimePicker? _dtpData;
    private NumericUpDown? _nudHora;
    private NumericUpDown? _nudMinuto;
    private ComboBox? _cmbFrequencia;
    private Button? _btnSelecionarArquivo;
    private Button? _btnSalvar;
    private Button? _btnCancelar;

    public AgendamentoArquivoForm(LocalDatabase database, AgendamentoArquivo? agendamento = null)
    {
        _database = database;
        _agendamentoEditando = agendamento;
        InitializeComponent();
        LoadDados();
    }

    private void InitializeComponent()
    {
        this.Text = _agendamentoEditando == null ? "Novo Agendamento de Arquivo" : "Editar Agendamento";
        this.Size = new Size(600, 450);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);
        this.FormBorderStyle = FormBorderStyle.None;

        // Title Bar
        var titleBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(35, 35, 45) };
        var lblTitle = new Label 
        { 
            Text = _agendamentoEditando == null ? "📅 NOVO AGENDAMENTO" : "✏️ EDITAR AGENDAMENTO", 
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
        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(btnClose);

        // Content Panel
        var contentPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(60, 50, 60, 40) };

        int yPos = 30; // Margem inicial maior do topo

        // Seleção de Arquivo
        var lblArquivo = new Label
        {
            Text = "Arquivo/Video",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, yPos) // Margem esquerda adicional
        };
        yPos += 30;

        var pnlArquivo = new Panel
        {
            Size = new Size(500, 40),
            Location = new Point(10, yPos),
            BackColor = Color.FromArgb(30, 30, 40)
        };

        _txtCaminhoArquivo = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Size = new Size(400, 40),
            Location = new Point(10, 0),
            ReadOnly = true
        };

        _btnSelecionarArquivo = new Button
        {
            Text = "📁 Selecionar",
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 32),
            Location = new Point(410, 4),
            Cursor = Cursors.Hand
        };
        _btnSelecionarArquivo.FlatAppearance.BorderSize = 0;
        _btnSelecionarArquivo.Click += BtnSelecionarArquivo_Click;

        pnlArquivo.Controls.Add(_txtCaminhoArquivo);
        pnlArquivo.Controls.Add(_btnSelecionarArquivo);
        yPos += 55;

        // Data
        var lblData = new Label
        {
            Text = "Data",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, yPos) // Margem esquerda adicional
        };
        yPos += 30;

        _dtpData = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(500, 35),
            Location = new Point(10, yPos), // Margem esquerda adicional
            Format = DateTimePickerFormat.Short,
            CalendarForeColor = Color.White,
            CalendarTitleBackColor = Color.FromArgb(35, 35, 45),
            CalendarTitleForeColor = Color.White,
            CalendarTrailingForeColor = Color.Gray
        };
        yPos += 50;

        // Hora e Minuto
        var lblHora = new Label
        {
            Text = "Hora e Minuto",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, yPos)
        };
        yPos += 30;

        var pnlHoraMinuto = new Panel
        {
            Size = new Size(500, 40),
            Location = new Point(10, yPos),
            BackColor = Color.Transparent
        };

        var lblH = new Label
        {
            Text = "Hora:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, 10)
        };

        _nudHora = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(80, 35),
            Location = new Point(60, 0),
            Minimum = 0,
            Maximum = 23,
            Value = DateTime.Now.Hour
        };

        var lblM = new Label
        {
            Text = "Minuto:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(160, 10)
        };

        _nudMinuto = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(80, 35),
            Location = new Point(220, 0),
            Minimum = 0,
            Maximum = 59,
            Value = DateTime.Now.Minute
        };

        pnlHoraMinuto.Controls.Add(lblH);
        pnlHoraMinuto.Controls.Add(_nudHora);
        pnlHoraMinuto.Controls.Add(lblM);
        pnlHoraMinuto.Controls.Add(_nudMinuto);
        yPos += 55;

        // Frequência
        var lblFrequencia = new Label
        {
            Text = "Frequência",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, yPos) // Margem esquerda adicional
        };
        yPos += 30;

        _cmbFrequencia = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(500, 35),
            Location = new Point(10, yPos)
        };
        _cmbFrequencia.Items.Add("Nenhum (executar uma vez)");
        _cmbFrequencia.Items.Add("Por Hora");
        _cmbFrequencia.Items.Add("Por Minuto");
        _cmbFrequencia.Items.Add("Diariamente");
        _cmbFrequencia.Items.Add("Semanalmente");
        _cmbFrequencia.Items.Add("Mensalmente");
        _cmbFrequencia.SelectedIndex = 0;
        yPos += 55;

        // Botões
        var pnlBotoes = new Panel
        {
            Size = new Size(500, 50),
            Location = new Point(10, yPos), // Margem esquerda adicional
            BackColor = Color.Transparent
        };

        _btnSalvar = new Button
        {
            Text = "💾 SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(260, 45),
            Location = new Point(0, 0),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.Click += BtnSalvar_Click;

        _btnCancelar = new Button
        {
            Text = "❌ CANCELAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(260, 45),
            Location = new Point(280, 0),
            Cursor = Cursors.Hand
        };
        _btnCancelar.FlatAppearance.BorderSize = 0;
        _btnCancelar.Click += (s, e) => this.Close();

        pnlBotoes.Controls.Add(_btnSalvar);
        pnlBotoes.Controls.Add(_btnCancelar);

        contentPanel.Controls.Add(lblArquivo);
        contentPanel.Controls.Add(pnlArquivo);
        contentPanel.Controls.Add(lblData);
        contentPanel.Controls.Add(_dtpData);
        contentPanel.Controls.Add(lblHora);
        contentPanel.Controls.Add(pnlHoraMinuto);
        contentPanel.Controls.Add(lblFrequencia);
        contentPanel.Controls.Add(_cmbFrequencia);
        contentPanel.Controls.Add(pnlBotoes);

        this.Controls.Add(titleBar);
        this.Controls.Add(contentPanel);
    }

    private void LoadDados()
    {
        if (_agendamentoEditando != null)
        {
            _txtCaminhoArquivo!.Text = _agendamentoEditando.CaminhoArquivo;
            _dtpData!.Value = _agendamentoEditando.DataHora.Date;
            _nudHora!.Value = _agendamentoEditando.DataHora.Hour;
            _nudMinuto!.Value = _agendamentoEditando.DataHora.Minute;
            _cmbFrequencia!.SelectedIndex = (int)_agendamentoEditando.Frequencia;
        }
    }

    private void BtnSelecionarArquivo_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        using var dialog = new OpenFileDialog
        {
            Title = "Selecionar Arquivo ou Vídeo",
            Filter = "Todos os arquivos|*.*|Vídeos|*.mp4;*.avi;*.mkv;*.mov;*.wmv|Arquivos|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _txtCaminhoArquivo!.Text = dialog.FileName;
        }
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();

        if (string.IsNullOrWhiteSpace(_txtCaminhoArquivo!.Text))
        {
            MessageBox.Show("Selecione um arquivo!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(_txtCaminhoArquivo.Text))
        {
            MessageBox.Show("O arquivo selecionado não existe!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _btnSalvar!.Enabled = false;
        _btnSalvar.Text = "SALVANDO...";

        try
        {
            var dataHora = new DateTime(
                _dtpData!.Value.Year,
                _dtpData.Value.Month,
                _dtpData.Value.Day,
                (int)_nudHora!.Value,
                (int)_nudMinuto!.Value,
                0
            );

            var agendamento = _agendamentoEditando ?? new AgendamentoArquivo();
            agendamento.CaminhoArquivo = _txtCaminhoArquivo.Text;
            agendamento.DataHora = dataHora;
            agendamento.Frequencia = (FrequenciaAgendamento)_cmbFrequencia!.SelectedIndex;
            agendamento.Ativo = true;

            _database.SaveAgendamentoArquivo(agendamento);

            SoundPlayer.PlaySuccess();
            MessageBox.Show("Agendamento salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro ao salvar agendamento: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvar.Enabled = true;
            _btnSalvar.Text = "💾 SALVAR";
        }
    }
}

