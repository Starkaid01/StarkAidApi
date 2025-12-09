using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class AlarmeEditForm : Form
{
    private readonly LocalDatabase _database;
    private readonly Lembrete? _lembreteEditando;
    private TextBox? _txtLembrar;
    private NumericUpDown? _numDia;
    private NumericUpDown? _numMes;
    private NumericUpDown? _numHora;
    private NumericUpDown? _numMinuto;
    private CheckBox? _chkDiario;
    private Button? _btnSalvar;

    public AlarmeEditForm(LocalDatabase database, Lembrete? lembrete = null)
    {
        _database = database;
        _lembreteEditando = lembrete;
        InitializeComponent();
        
        if (_lembreteEditando != null)
        {
            _txtLembrar!.Text = _lembreteEditando.Lembrar;
            _numDia!.Value = _lembreteEditando.Dia ?? 1;
            _numMes!.Value = _lembreteEditando.Mes ?? 1;
            _numHora!.Value = _lembreteEditando.Hora ?? 7;
            _numMinuto!.Value = _lembreteEditando.Minuto ?? 0;
            _chkDiario!.Checked = !_lembreteEditando.Dia.HasValue && !_lembreteEditando.Mes.HasValue;
        }
    }

    private void InitializeComponent()
    {
        this.Text = _lembreteEditando == null ? "Criar Alarme" : "Editar Alarme";
        this.Size = new Size(500, 420);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblLembrar = new Label
        {
            Text = "Lembrar de",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtLembrar = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(440, 35),
            Location = new Point(20, 50),
            Multiline = false
        };

        _chkDiario = new CheckBox
        {
            Text = "Alarme Diário (repetir todos os dias)",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 100),
            BackColor = Color.Transparent
        };
        _chkDiario.CheckedChanged += ChkDiario_CheckedChanged;

        var lblDia = new Label
        {
            Text = "Dia",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 140)
        };

        _numDia = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(100, 35),
            Location = new Point(20, 170),
            Minimum = 1,
            Maximum = 31,
            Value = DateTime.Now.Day
        };

        var lblMes = new Label
        {
            Text = "Mês",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(140, 140)
        };

        _numMes = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(100, 35),
            Location = new Point(140, 170),
            Minimum = 1,
            Maximum = 12,
            Value = DateTime.Now.Month
        };

        var lblHora = new Label
        {
            Text = "Hora",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(260, 140)
        };

        _numHora = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(80, 35),
            Location = new Point(260, 170),
            Minimum = 0,
            Maximum = 23,
            Value = 7
        };

        var lblMinuto = new Label
        {
            Text = "Minuto",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(360, 140)
        };

        _numMinuto = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(80, 35),
            Location = new Point(360, 170),
            Minimum = 0,
            Maximum = 59,
            Value = 0
        };

        _btnSalvar = new Button
        {
            Text = _lembreteEditando == null ? "CRIAR" : "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(440, 40),
            Location = new Point(20, 230),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnSalvar.Click += BtnSalvar_Click;

        this.Controls.Add(lblLembrar);
        this.Controls.Add(_txtLembrar);
        this.Controls.Add(_chkDiario);
        this.Controls.Add(lblDia);
        this.Controls.Add(_numDia);
        this.Controls.Add(lblMes);
        this.Controls.Add(_numMes);
        this.Controls.Add(lblHora);
        this.Controls.Add(_numHora);
        this.Controls.Add(lblMinuto);
        this.Controls.Add(_numMinuto);
        this.Controls.Add(_btnSalvar);
    }

    private void ChkDiario_CheckedChanged(object? sender, EventArgs e)
    {
        bool diario = _chkDiario!.Checked;
        _numDia!.Enabled = !diario;
        _numMes!.Enabled = !diario;
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();

        if (string.IsNullOrWhiteSpace(_txtLembrar!.Text))
        {
            MessageBox.Show("O que lembrar é obrigatório!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_lembreteEditando != null)
        {
            // Atualizar
            var lembrete = new Lembrete
            {
                Id = _lembreteEditando.Id,
                Lembrar = _txtLembrar.Text.Trim(),
                Dia = _chkDiario!.Checked ? null : (int?)_numDia!.Value,
                Mes = _chkDiario.Checked ? null : (int?)_numMes!.Value,
                Hora = (int?)_numHora!.Value,
                Minuto = (int?)_numMinuto!.Value,
                Concluido = _lembreteEditando.Concluido,
                DataCriacao = _lembreteEditando.DataCriacao,
                UltimaNotificacao = _lembreteEditando.UltimaNotificacao
            };
            
            _database.SaveLembrete(lembrete);
            SoundPlayer.PlaySuccess();
            MessageBox.Show("Alarme atualizado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            // Criar novo
            var lembrete = new Lembrete
            {
                Lembrar = _txtLembrar.Text.Trim(),
                Dia = _chkDiario!.Checked ? null : (int?)_numDia!.Value,
                Mes = _chkDiario.Checked ? null : (int?)_numMes!.Value,
                Hora = (int?)_numHora!.Value,
                Minuto = (int?)_numMinuto!.Value,
                Concluido = false,
                DataCriacao = DateTime.Now
            };
            
            _database.SaveLembrete(lembrete);
            SoundPlayer.PlaySuccess();
            MessageBox.Show("Alarme criado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}

