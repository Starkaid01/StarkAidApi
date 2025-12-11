using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Utils;
using System.Diagnostics;
using System.IO;

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

    private async void BtnExecutar_Click(object? sender, EventArgs e)
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

        // Efeito visual: mudar texto do botão para "PRONTO"
        _btnExecutar!.Text = "PRONTO";
        _btnExecutar.Enabled = false;
        Application.DoEvents();

        try
        {
            var cmdLower = comando.ComandoCMD.ToLower().Trim();
            
            // PRIMEIRO: Verificar se é um comando complexo (timeout, powershell, etc) - sempre executar via cmd.exe
            // Esta verificação deve vir ANTES da verificação de arquivo simples
            // Se contém &&, ||, >nul, >, timeout, powershell, etc, é comando complexo
            bool isComplexCommand = cmdLower.Contains("&&") ||
                                   cmdLower.Contains("||") ||
                                   cmdLower.Contains(">nul") ||
                                   cmdLower.Contains(">") ||
                                   cmdLower.StartsWith("timeout") || 
                                   cmdLower.StartsWith("powershell") ||
                                   cmdLower.Contains("cmd") ||
                                   cmdLower.Contains(" /c ") ||
                                   cmdLower.Contains(" /k ");

            // SEGUNDO: Verificar se o comando é para abrir arquivo/programa (usa start ou caminho direto)
            // Só verificar se NÃO for comando complexo
            // Um arquivo simples não contém &&, ||, >, timeout, etc
            bool isOpenFile = !isComplexCommand && (
                            (cmdLower.StartsWith("start") && cmdLower.Split(' ').Length <= 3) || 
                            ((cmdLower.Contains(".exe") || cmdLower.Contains(".png") || cmdLower.Contains(".jpg") || 
                              cmdLower.Contains(".jpeg") || cmdLower.Contains(".pdf") || cmdLower.Contains(".txt") ||
                              cmdLower.Contains(".doc") || cmdLower.Contains(".mp4") || cmdLower.Contains(".mp3")) 
                             && !cmdLower.Contains("&&") && !cmdLower.Contains("||") && !cmdLower.Contains(">") 
                             && !cmdLower.Contains("timeout") && !cmdLower.Contains("powershell")));

            if (isComplexCommand)
            {
                // Para PowerShell, executar diretamente sem cmd.exe
                if (cmdLower.StartsWith("powershell"))
                {
                    // Extrair o comando PowerShell completo (remove "powershell" do início)
                    string psArgs = comando.ComandoCMD.Substring("powershell".Length).TrimStart();
                    
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = psArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    try
                    {
                        // Executar e não aguardar
                        var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            // Liberar recursos imediatamente - não aguardar processo
                            process.EnableRaisingEvents = false;
                            // Não chamar WaitForExit - deixar rodar em background
                        }
                        System.Diagnostics.Debug.WriteLine($"Comando PowerShell executado: {comando.ComandoCMD}");
                        System.Diagnostics.Debug.WriteLine($"Argumentos PowerShell: {psArgs}");
                    }
                    catch (Exception psEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao executar PowerShell: {psEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack: {psEx.StackTrace}");
                    }
                }
                else
                {
                    // Verificar se é um comando com timeout seguido de start
                    var timeoutMatch = System.Text.RegularExpressions.Regex.Match(
                        comando.ComandoCMD, 
                        @"timeout\s+/t\s+(\d+)\s+/nobreak\s*>nul\s*&&\s*start\s+""[^""]*""\s+""([^""]+)""",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (timeoutMatch.Success)
                    {
                        // Comando com timeout - executar delay em C# e depois abrir arquivo
                        int delaySeconds = int.Parse(timeoutMatch.Groups[1].Value);
                        string filePath = timeoutMatch.Groups[2].Value;
                        
                        System.Diagnostics.Debug.WriteLine($"Comando com timeout detectado: {delaySeconds}s, arquivo: {filePath}");
                        
                        // Executar delay e abrir arquivo em background
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(delaySeconds * 1000);
                            
                            try
                            {
                                var fileProcessInfo = new ProcessStartInfo
                                {
                                    FileName = filePath,
                                    UseShellExecute = true
                                };
                                
                                Process.Start(fileProcessInfo);
                                System.Diagnostics.Debug.WriteLine($"Arquivo aberto após delay: {filePath}");
                            }
                            catch (Exception fileEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Erro ao abrir arquivo após delay: {fileEx.Message}");
                            }
                        });
                    }
                    else
                    {
                        // Outros comandos complexos via cmd.exe
                        // Criar um arquivo batch temporário para executar comandos complexos
                        string tempBatchFile = Path.Combine(Path.GetTempPath(), $"starkaid_cmd_{Guid.NewGuid()}.bat");
                        try
                        {
                            // Escrever comando no arquivo batch com encoding correto
                            string batchContent = $"@echo off\r\n{comando.ComandoCMD}\r\nexit\r\n";
                            File.WriteAllText(tempBatchFile, batchContent, System.Text.Encoding.Default);
                            
                            var processInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"{tempBatchFile}\"",
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                WorkingDirectory = Path.GetTempPath()
                            };

                            var process = Process.Start(processInfo);
                            if (process != null)
                            {
                                // Não aguardar o processo - deixar rodar em background
                                process.EnableRaisingEvents = false;
                                
                                // Agendar exclusão do arquivo batch após 30 segundos
                                System.Threading.Tasks.Task.Run(async () =>
                                {
                                    await System.Threading.Tasks.Task.Delay(30000);
                                    try
                                    {
                                        if (File.Exists(tempBatchFile))
                                            File.Delete(tempBatchFile);
                                    }
                                    catch { }
                                });
                            }
                            System.Diagnostics.Debug.WriteLine($"Comando complexo executado via batch: {comando.ComandoCMD}");
                            System.Diagnostics.Debug.WriteLine($"Arquivo batch criado: {tempBatchFile}");
                        }
                        catch (Exception cmdEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erro ao executar comando CMD: {cmdEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack: {cmdEx.StackTrace}");
                            // Tentar limpar arquivo batch em caso de erro
                            try
                            {
                                if (File.Exists(tempBatchFile))
                                    File.Delete(tempBatchFile);
                            }
                            catch { }
                        }
                    }
                }
            }
            else if (isOpenFile)
            {
                // Extrair o caminho do arquivo se usar "start"
                string filePath = comando.ComandoCMD;
                if (cmdLower.StartsWith("start"))
                {
                    // Extrair caminho entre aspas ou após "start"
                    var match = System.Text.RegularExpressions.Regex.Match(comando.ComandoCMD, @"start\s+""[^""]*""\s+""([^""]+)""|start\s+""([^""]+)""|start\s+([^\s]+)");
                    if (match.Success)
                    {
                        filePath = match.Groups[1].Success ? match.Groups[1].Value : 
                                  (match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value);
                    }
                }

                // Abrir arquivo diretamente usando Process.Start
                var processInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };

                Process.Start(processInfo);
                System.Diagnostics.Debug.WriteLine($"Arquivo aberto: {filePath}");
            }
            else
            {
                // Para comandos que precisam capturar saída, usar UseShellExecute = false
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
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao executar comando: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // Restaurar texto do botão após um pequeno delay
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(500);
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        _btnExecutar!.Text = "EXECUTAR";
                        _btnExecutar.Enabled = true;
                    }));
                }
                else
                {
                    _btnExecutar!.Text = "EXECUTAR";
                    _btnExecutar.Enabled = true;
                }
            });
        }
    }
}

