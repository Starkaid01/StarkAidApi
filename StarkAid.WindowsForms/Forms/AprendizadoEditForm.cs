using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class AprendizadoEditForm : Form
{
    private readonly LocalDatabase _database;
    private readonly Aprendizado? _aprendizadoEditando;
    private TextBox? _txtComando;
    private TextBox? _txtResposta;
    private Button? _btnSalvar;

    public AprendizadoEditForm(LocalDatabase database, Aprendizado? aprendizado = null)
    {
        _database = database;
        _aprendizadoEditando = aprendizado;
        InitializeComponent();
        
        if (_aprendizadoEditando != null)
        {
            _txtComando!.Text = _aprendizadoEditando.ComandoUser;
            _txtResposta!.Text = _aprendizadoEditando.RespostaIa;
        }
    }

    private void InitializeComponent()
    {
        this.Text = _aprendizadoEditando == null ? "Criar Aprendizado" : "Editar Aprendizado";
        this.Size = new Size(600, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblComando = new Label
        {
            Text = "Comando do Usuário",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtComando = new TextBox
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
            Text = "Resposta da IA",
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
            Size = new Size(540, 100),
            Location = new Point(20, 130),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        _btnSalvar = new Button
        {
            Text = _aprendizadoEditando == null ? "CRIAR" : "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(540, 40),
            Location = new Point(20, 250),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        _btnSalvar.Click += BtnSalvar_Click;

        this.Controls.Add(lblComando);
        this.Controls.Add(_txtComando);
        this.Controls.Add(lblResposta);
        this.Controls.Add(_txtResposta);
        this.Controls.Add(_btnSalvar);
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();

        if (string.IsNullOrWhiteSpace(_txtComando!.Text))
        {
            MessageBox.Show("O comando do usuário é obrigatório!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtResposta!.Text))
        {
            MessageBox.Show("A resposta da IA é obrigatória!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_aprendizadoEditando != null)
        {
            // Atualizar
            var aprendizado = new Aprendizado
            {
                Id = _aprendizadoEditando.Id,
                ComandoUser = _txtComando.Text.Trim(),
                RespostaIa = _txtResposta.Text.Trim(),
                DataCriacao = _aprendizadoEditando.DataCriacao
            };
            
            _database.UpdateAprendizado(aprendizado);
            SoundPlayer.PlaySuccess();
            MessageBox.Show("Aprendizado atualizado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            // Criar novo (será salvo pela lógica de aprendizado quando usado)
            MessageBox.Show("Os novos aprendizados são criados automaticamente quando você usa comandos com a IA ativada. Este formulário é apenas para edição de aprendizados existentes.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}

