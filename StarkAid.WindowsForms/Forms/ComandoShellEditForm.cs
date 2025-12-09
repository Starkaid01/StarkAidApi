using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class ComandoShellEditForm : Form
{
    private readonly LocalDatabase _database;
    private readonly ComandoShell? _comandoEditando;
    private TextBox? _txtComandoInput;
    private TextBox? _txtResposta;
    private TextBox? _txtComandoCMD;
    private Button? _btnSalvar;

    public ComandoShellEditForm(LocalDatabase database, ComandoShell? comando = null)
    {
        _database = database;
        _comandoEditando = comando;
        InitializeComponent();
        
        if (_comandoEditando != null)
        {
            _txtComandoInput!.Text = _comandoEditando.ComandoInput;
            _txtResposta!.Text = _comandoEditando.Resposta;
            _txtComandoCMD!.Text = _comandoEditando.ComandoCMD;
        }
    }

    private void InitializeComponent()
    {
        this.Text = _comandoEditando == null ? "Criar Comando Shell" : "Editar Comando Shell";
        this.Size = new Size(600, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblComandoInput = new Label
        {
            Text = "Comando Input",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtComandoInput = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(540, 35),
            Location = new Point(20, 50),
            Multiline = false
        };

        var lblResposta = new Label
        {
            Text = "Resposta",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 100)
        };

        _txtResposta = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(540, 80),
            Location = new Point(20, 130),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        var lblComandoCMD = new Label
        {
            Text = "Comando CMD",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 230)
        };

        _txtComandoCMD = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            Size = new Size(540, 35),
            Location = new Point(20, 260),
            Multiline = false
        };

        _btnSalvar = new Button
        {
            Text = _comandoEditando == null ? "CRIAR" : "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(540, 40),
            Location = new Point(20, 310),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnSalvar.Click += BtnSalvar_Click;

        this.Controls.Add(lblComandoInput);
        this.Controls.Add(_txtComandoInput);
        this.Controls.Add(lblResposta);
        this.Controls.Add(_txtResposta);
        this.Controls.Add(lblComandoCMD);
        this.Controls.Add(_txtComandoCMD);
        this.Controls.Add(_btnSalvar);
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();

        if (string.IsNullOrWhiteSpace(_txtComandoInput!.Text))
        {
            MessageBox.Show("O comando input é obrigatório!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtResposta!.Text))
        {
            MessageBox.Show("A resposta é obrigatória!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtComandoCMD!.Text))
        {
            MessageBox.Show("O comando CMD é obrigatório!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var comando = new ComandoShell
        {
            Id = _comandoEditando?.Id ?? 0,
            ComandoInput = _txtComandoInput.Text.Trim(),
            Resposta = _txtResposta.Text.Trim(),
            ComandoCMD = _txtComandoCMD.Text.Trim()
        };
        
        _database.SaveComandoShell(comando);
        SoundPlayer.PlaySuccess();
        MessageBox.Show(_comandoEditando == null ? "Comando criado com sucesso!" : "Comando atualizado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}

