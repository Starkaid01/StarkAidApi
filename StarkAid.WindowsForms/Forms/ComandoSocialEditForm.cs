using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class ComandoSocialEditForm : Form
{
    private readonly ApiService _apiService;
    private readonly ComandoSocial? _comando;
    private TextBox? _txtComando;
    private TextBox? _txtResposta;
    private Button? _btnSalvar;

    public ComandoSocialEditForm(ApiService apiService, ComandoSocial? comando)
    {
        _apiService = apiService;
        _comando = comando;
        InitializeComponent();
        if (_comando != null)
        {
            _txtComando!.Text = _comando.Comando;
            _txtResposta!.Text = _comando.Resposta;
        }
    }

    private void InitializeComponent()
    {
        this.Text = _comando == null ? "Criar Comando Social" : "Editar Comando Social";
        this.Size = new Size(500, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblComando = new Label
        {
            Text = "Comando",
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
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 50)
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
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(440, 35),
            Location = new Point(20, 130),
            Multiline = true,
            Height = 80
        };

        _btnSalvar = new Button
        {
            Text = "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(440, 40),
            Location = new Point(20, 220),
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

    private async void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        if (string.IsNullOrWhiteSpace(_txtComando!.Text) || string.IsNullOrWhiteSpace(_txtResposta!.Text))
        {
            MessageBox.Show("Preencha todos os campos!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSalvar!.Enabled = false;
        _btnSalvar.Text = "SALVANDO...";

        try
        {
            bool success;
            if (_comando == null)
            {
                var novo = new ComandoSocial
                {
                    Comando = _txtComando.Text,
                    Resposta = _txtResposta.Text
                };
                success = await _apiService.CreateComandoSocialAsync(novo) != null;
            }
            else
            {
                success = await _apiService.UpdateComandoSocialAsync(_comando.Id, new ComandoSocial
                {
                    Comando = _txtComando.Text,
                    Resposta = _txtResposta.Text
                });
            }

            if (success)
            {
                SoundPlayer.PlaySuccess();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                SoundPlayer.PlayError();
                MessageBox.Show("Erro ao salvar comando!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SoundPlayer.PlayError();
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvar.Enabled = true;
            _btnSalvar.Text = "SALVAR";
        }
    }
}

