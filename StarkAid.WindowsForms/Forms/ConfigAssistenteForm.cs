using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;
using System.Speech.Recognition;
using NAudio.Wave;

namespace StarkAid.WindowsForms.Forms;

public partial class ConfigAssistenteForm : Form
{
    private readonly LocalDatabase _database;
    private readonly SpeechService _speechService;
    private TextBox? _txtNomeAssistente;
    private TextBox? _txtRespostaPadrao;
    private ComboBox? _cmbMicrofone;
    private ComboBox? _cmbVoz;
    private Button? _btnSalvar;

    public ConfigAssistenteForm(LocalDatabase database, SpeechService speechService)
    {
        _database = database;
        _speechService = speechService;
        InitializeComponent();
        LoadMicrophones();
        LoadVoices();
        LoadConfig(); // Carregar após carregar microfones e vozes para garantir que os itens estejam disponíveis
    }

    private void InitializeComponent()
    {
        this.Text = "Configurar Assistente";
        this.Size = new Size(600, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);

        var lblNome = new Label
        {
            Text = "Nome do Assistente",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtNomeAssistente = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(540, 35),
            Location = new Point(20, 50)
        };

        var lblRespostaPadrao = new Label
        {
            Text = "Resposta Padrão",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 100)
        };

        _txtRespostaPadrao = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(540, 80),
            Location = new Point(20, 130),
            Multiline = true
        };

        var lblMicrofone = new Label
        {
            Text = "Microfone",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 230)
        };

        _cmbMicrofone = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(540, 35),
            Location = new Point(20, 260)
        };

        var lblVoz = new Label
        {
            Text = "Voz do Assistente",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 310)
        };

        _cmbVoz = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(540, 35),
            Location = new Point(20, 340)
        };

        _btnSalvar = new Button
        {
            Text = "SALVAR",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(540, 40),
            Location = new Point(20, 390),
            Cursor = Cursors.Hand
        };
        _btnSalvar.FlatAppearance.BorderSize = 0;
        _btnSalvar.Click += BtnSalvar_Click;

        this.Controls.Add(lblNome);
        this.Controls.Add(_txtNomeAssistente);
        this.Controls.Add(lblRespostaPadrao);
        this.Controls.Add(_txtRespostaPadrao);
        this.Controls.Add(lblMicrofone);
        this.Controls.Add(_cmbMicrofone);
        this.Controls.Add(lblVoz);
        this.Controls.Add(_cmbVoz);
        this.Controls.Add(_btnSalvar);
    }

    private void LoadMicrophones()
    {
        try
        {
            _cmbMicrofone!.Items.Clear();
            _cmbMicrofone.Items.Add("Padrão do Sistema (0)");

            // Obter dispositivos de áudio usando NAudio
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var deviceInfo = WaveIn.GetCapabilities(i);
                _cmbMicrofone.Items.Add($"{deviceInfo.ProductName} ({i + 1})");
            }

            if (_cmbMicrofone.Items.Count > 0)
                _cmbMicrofone.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar microfones: {ex.Message}");
            _cmbMicrofone!.Items.Add("Padrão do Sistema (0)");
            _cmbMicrofone.SelectedIndex = 0;
        }
    }

    private void LoadVoices()
    {
        try
        {
            _cmbVoz!.Items.Clear();
            
            using var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
            var voices = synthesizer.GetInstalledVoices();
            
            foreach (var voice in voices)
            {
                var voiceInfo = voice.VoiceInfo;
                var displayName = $"{voiceInfo.Name} ({voiceInfo.Culture.Name})";
                _cmbVoz.Items.Add(new VoiceItem { DisplayName = displayName, VoiceName = voiceInfo.Name });
            }
            
            if (_cmbVoz.Items.Count > 0)
                _cmbVoz.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar vozes: {ex.Message}");
            _cmbVoz!.Items.Add("Voz Padrão");
            _cmbVoz.SelectedIndex = 0;
        }
    }

    private void LoadConfig()
    {
        var config = _database.GetConfigAssistente();
        _txtNomeAssistente!.Text = config.NomeAssistente ?? "";
        _txtRespostaPadrao!.Text = config.RespostaPadrao ?? "";
        
        // Carregar microfone salvo após carregar a lista de microfones
        if (_cmbMicrofone != null)
        {
            if (config.MicrofoneId.HasValue && config.MicrofoneId.Value >= 0 && config.MicrofoneId.Value < _cmbMicrofone.Items.Count)
            {
                _cmbMicrofone.SelectedIndex = config.MicrofoneId.Value;
            }
            else if (_cmbMicrofone.Items.Count > 0)
            {
                _cmbMicrofone.SelectedIndex = 0; // Padrão
            }
        }

        // Carregar voz salva após carregar a lista de vozes
        if (_cmbVoz != null && !string.IsNullOrEmpty(config.VozName))
        {
            for (int i = 0; i < _cmbVoz.Items.Count; i++)
            {
                if (_cmbVoz.Items[i] is VoiceItem voiceItem && voiceItem.VoiceName == config.VozName)
                {
                    _cmbVoz.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        SoundPlayer.PlayClick();
        
        if (string.IsNullOrWhiteSpace(_txtNomeAssistente!.Text))
        {
            MessageBox.Show("Preencha o nome do assistente!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSalvar!.Enabled = false;
        _btnSalvar.Text = "SALVANDO...";

        try
        {
            // Extrair ID do microfone (o índice no combo)
            var microfoneId = _cmbMicrofone!.SelectedIndex >= 0 ? _cmbMicrofone.SelectedIndex : 0;
            
            // Extrair nome da voz selecionada
            string? vozName = null;
            if (_cmbVoz!.SelectedItem is VoiceItem voiceItem)
            {
                vozName = voiceItem.VoiceName;
            }
            
            _database.SaveConfigAssistente(
                _txtNomeAssistente.Text,
                _txtRespostaPadrao!.Text ?? "",
                microfoneId,
                vozName
            );

            // Aplicar configurações ao SpeechService
            _speechService.SetMicrophone(microfoneId);
            if (!string.IsNullOrEmpty(vozName))
            {
                _speechService.SetVoice(vozName);
            }

            SoundPlayer.PlaySuccess();
            MessageBox.Show("Configurações salvas com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
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

    // Classe auxiliar para armazenar informações da voz no ComboBox
    private class VoiceItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;

        public override string ToString() => DisplayName;
    }
}

