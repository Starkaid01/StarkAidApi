using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;

namespace StarkAid.WindowsForms.Services;

public class WebView2RuntimeService
{
    private const string RuntimeFolderName = "WebView2Runtime";
    private readonly string _runtimePath;
    private readonly string _appDirectory;

    public WebView2RuntimeService()
    {
        _appDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        _runtimePath = Path.Combine(_appDirectory, RuntimeFolderName);
    }

    public string RuntimePath => _runtimePath;

    public async Task<bool> EnsureRuntimeAvailableAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Verificando WebView2 Runtime local...");

        // Verificar se o runtime já existe localmente
        var msedgePath = Path.Combine(_runtimePath, GetEdgeExecutablePath());
        if (File.Exists(msedgePath))
        {
            progress?.Report("WebView2 Runtime local encontrado.");
            return true;
        }

        // Se não existe, retornar false - o usuário precisará baixar manualmente
        progress?.Report("WebView2 Runtime local não encontrado.");
        return false;
    }

    private async Task<bool> DownloadRuntimeAsync(IProgress<string>? progress)
    {
        try
        {
            // Criar diretório se não existir
            Directory.CreateDirectory(_runtimePath);

            // Determinar arquitetura
            var architecture = GetSystemArchitecture();
            progress?.Report($"Arquitetura detectada: {architecture}");

            // URLs do WebView2 Runtime (Fixed Version)
            // Usando versão estável mais recente (pode precisar atualizar)
            var downloadUrl = GetDownloadUrl(architecture);
            
            if (string.IsNullOrEmpty(downloadUrl))
            {
                progress?.Report("Arquitetura não suportada para download automático.");
                return false;
            }

            progress?.Report($"Baixando WebView2 Runtime de: {downloadUrl}");

            // Nome do arquivo ZIP temporário
            var zipPath = Path.Combine(_runtimePath, "webview2_runtime.zip");

            // Baixar arquivo
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10); // Timeout de 10 minutos

            var response = await httpClient.GetAsync(downloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            long downloadedBytes = 0;

            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var httpStream = await response.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var percent = (int)((downloadedBytes * 100) / totalBytes);
                        progress?.Report($"Baixando... {percent}% ({downloadedBytes / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB)");
                    }
                }
            }

            progress?.Report("Download concluído. Extraindo...");

            // Extrair ZIP
            ZipFile.ExtractToDirectory(zipPath, _runtimePath, true);

            // Limpar arquivo ZIP
            File.Delete(zipPath);

            // Verificar se o executável foi extraído corretamente
            var msedgePathAfterExtraction = Path.Combine(_runtimePath, GetEdgeExecutablePath());
            if (File.Exists(msedgePathAfterExtraction))
            {
                progress?.Report("WebView2 Runtime instalado com sucesso!");
                return true;
            }
            else
            {
                progress?.Report("Erro: Executável não encontrado após extração.");
                return false;
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"Erro ao baixar/extrarir WebView2 Runtime: {ex.Message}");
            return false;
        }
    }

    private string GetDownloadUrl(string architecture)
    {
        // URLs para o WebView2 Runtime Fixed Version Bootstrapper
        // Usando o link oficial da Microsoft que baixa automaticamente a versão correta
        // Nota: Para Fixed Version, precisamos baixar o pacote específico
        // Vamos usar o Evergreen Bootstrapper e depois extrair, ou usar um link direto
        
        // Link alternativo: usar o pacote completo do WebView2 Runtime
        // Para Fixed Version, precisamos do pacote completo (não apenas o bootstrapper)
        
        // Usando link para o Runtime completo (cerca de 150-200 MB)
        return architecture.ToLower() switch
        {
            "x64" => "https://go.microsoft.com/fwlink/p/?LinkId=2124703", // Evergreen Bootstrapper
            "x86" => "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
            "arm64" => "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
            _ => string.Empty
        };
    }

    private string GetSystemArchitecture()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            return arch switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                _ => "x64" // Default para x64
            };
        }
        return "x64";
    }

    private string GetEdgeExecutablePath()
    {
        var architecture = GetSystemArchitecture();
        return architecture.ToLower() switch
        {
            "x64" => "x64\\MicrosoftEdgeWebView2.exe",
            "x86" => "x86\\MicrosoftEdgeWebView2.exe",
            "arm64" => "arm64\\MicrosoftEdgeWebView2.exe",
            _ => "x64\\MicrosoftEdgeWebView2.exe"
        };
    }

    public async Task<CoreWebView2Environment?> CreateEnvironmentAsync()
    {
        try
        {
            // Verificar se o runtime está disponível
            if (!await EnsureRuntimeAvailableAsync())
            {
                return null;
            }

            var msedgePath = Path.Combine(_runtimePath, GetEdgeExecutablePath());
            if (!File.Exists(msedgePath))
            {
                return null;
            }

            // Obter diretório do executável Edge
            var browserFolder = Path.GetDirectoryName(msedgePath);
            if (browserFolder == null)
            {
                return null;
            }

            // Criar pasta de dados do usuário para o WebView2
            var userDataFolder = Path.Combine(_appDirectory, "WebView2Data");

            // Criar ambiente usando o runtime local
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: browserFolder,
                userDataFolder: userDataFolder
            );

            return environment;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao criar ambiente WebView2: {ex.Message}");
            return null;
        }
    }
}

