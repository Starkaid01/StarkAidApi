using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms.Forms;

public partial class AlarmesDisparadosForm : Form
{
    private readonly LocalDatabase _database;
    private List<Lembrete> _lembretesDisparados = new();

    public AlarmesDisparadosForm(LocalDatabase database)
    {
        _database = database;
        InitializeComponent();
        LoadAlarmesDisparados();
    }

    private void InitializeComponent()
    {
        this.Text = "Alarmes Disparados";
        this.Size = new Size(500, 600);
        this.MinimumSize = new Size(500, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(20, 20, 30);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Padding = new Padding(0);

        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 25, 35),
            Padding = new Padding(20)
        };

        var lblTitulo = new Label
        {
            Text = "⏰ Alarmes Disparados",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var pnlAlarmes = new Panel
        {
            AutoScroll = true,
            BackColor = Color.FromArgb(30, 30, 40),
            Location = new Point(20, 60),
            Size = new Size(440, 450),
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnFechar = new Button
        {
            Text = "FECHAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(100, 100, 120),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(440, 40),
            Location = new Point(20, 520),
            Cursor = Cursors.Hand
        };
        btnFechar.FlatAppearance.BorderSize = 0;
        btnFechar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
        btnFechar.Click += (s, e) => { SoundPlayer.PlayClick(); this.Close(); };

        _pnlAlarmes = pnlAlarmes;
        _lblTitulo = lblTitulo;

        pnlMain.Controls.Add(lblTitulo);
        pnlMain.Controls.Add(pnlAlarmes);
        pnlMain.Controls.Add(btnFechar);

        this.Controls.Add(pnlMain);
    }

    private Panel? _pnlAlarmes;
    private Label? _lblTitulo;

    private void LoadAlarmesDisparados()
    {
        _lembretesDisparados = _database.GetLembretesDisparados();
        
        if (_pnlAlarmes == null) return;
        
        _pnlAlarmes.Controls.Clear();
        
        if (_lembretesDisparados.Count == 0)
        {
            var lblVazio = new Label
            {
                Text = "Nenhum alarme disparado no momento.",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            _pnlAlarmes.Controls.Add(lblVazio);
            return;
        }

        int yPos = 20;
        foreach (var lembrete in _lembretesDisparados)
        {
            var pnlAlarme = new Panel
            {
                BackColor = Color.FromArgb(40, 40, 50),
                Size = new Size(400, 80),
                Location = new Point(20, yPos),
                Padding = new Padding(15)
            };

            string horaStr = "";
            if (lembrete.Hora.HasValue && lembrete.Minuto.HasValue)
            {
                horaStr = $"({lembrete.Hora.Value:D2}:{lembrete.Minuto.Value:D2})";
            }
            else if (lembrete.Hora.HasValue)
            {
                horaStr = $"({lembrete.Hora.Value:D2}:00)";
            }

            var lblTexto = new Label
            {
                Text = $"{lembrete.Lembrar}{horaStr}",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(300, 50),
                Location = new Point(15, 15)
            };

            var btnConfirmar = new Button
            {
                Text = "CONFIRMAR",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 35),
                Location = new Point(285, 20),
                Cursor = Cursors.Hand,
                Tag = lembrete.Id
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.MouseEnter += (s, e) => SoundPlayer.PlayMouseMove();
            btnConfirmar.Click += (s, e) => {
                SoundPlayer.PlayClick();
                if (btnConfirmar.Tag is int id)
                {
                    ConfirmarAlarme(id);
                }
            };

            pnlAlarme.Controls.Add(lblTexto);
            pnlAlarme.Controls.Add(btnConfirmar);
            _pnlAlarmes.Controls.Add(pnlAlarme);

            yPos += 90;
        }
    }

    private void ConfirmarAlarme(int id)
    {
        _database.MarcarLembreteConcluido(id);
        SoundPlayer.PlaySuccess();
        
        // Remover o alarme da lista local
        _lembretesDisparados = _lembretesDisparados.Where(l => l.Id != id).ToList();
        
        LoadAlarmesDisparados();
        
        // Notificar o MainForm para atualizar o contador
        if (this.Owner is MainForm mainForm)
        {
            mainForm.AtualizarContadorAlarmes();
        }
        
        // Se não há mais alarmes, fechar o form
        if (_lembretesDisparados.Count == 0)
        {
            this.Close();
        }
    }
}

