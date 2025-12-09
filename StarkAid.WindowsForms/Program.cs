using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Forms;
using StarkAid.WindowsForms.Services;
using StarkAid.WindowsForms.Utils;

namespace StarkAid.WindowsForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread] // OBRIGATÓRIO - WebView2 requer STA thread
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Inicializar serviços - TUDO SÍNCRONO até Application.Run
        // NUNCA use async/await aqui - isso quebra o threading STA
        var apiService = new ApiService();
        var database = new LocalDatabase();
        var webSocketService = new WebSocketService();
        var udpService = new UdpService();
        var speechService = new SpeechService();
        var processComandoGeral = new ProcessComandoGeral();
        var commandProcessor = new CommandProcessor(database, speechService, processComandoGeral, udpService, apiService, webSocketService);

        // BootstrapForm executará o login e verificação de licença de forma assíncrona
        // Isso mantém o thread STA intacto para o WebView2
        Application.Run(new BootstrapForm(
            apiService,
            database,
            webSocketService,
            udpService,
            speechService,
            commandProcessor
        ));
    }
}
