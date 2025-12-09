using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;

namespace StarkAid.WindowsForms.Services;

public class ErrorLoggerService
{
    private readonly LocalDatabase _database;
    private string? _ultimoComando;
    private string? _ultimaResposta;
    private string? _ultimoDispositivoAcionado;

    public ErrorLoggerService(LocalDatabase database)
    {
        _database = database;
    }

    public void SetUltimoComando(string? comando)
    {
        _ultimoComando = comando;
    }

    public void SetUltimaResposta(string? resposta)
    {
        _ultimaResposta = resposta;
    }

    public void SetUltimoDispositivoAcionado(string? dispositivo)
    {
        _ultimoDispositivoAcionado = dispositivo;
    }

    public void LogError(Exception ex, string codigoErro, string acaoErro, bool incluirComandoRespostaDispositivo = false)
    {
        try
        {
            var agora = DateTime.Now;
            var log = new LogToSuporte
            {
                UltimoComando = incluirComandoRespostaDispositivo ? _ultimoComando : null,
                UltimaResposta = incluirComandoRespostaDispositivo ? _ultimaResposta : null,
                UltimoDispositivoAcionado = incluirComandoRespostaDispositivo ? _ultimoDispositivoAcionado : null,
                ErroCompleto = $"{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                CodigoDeErro = codigoErro,
                DataErro = agora.ToString("yyyy-MM-dd"),
                HoraErro = agora.ToString("HH:mm:ss"),
                AcaoErro = acaoErro
            };

            _database.SaveLogToSuporte(log);
        }
        catch
        {
            // Se falhar ao salvar log, não fazer nada para evitar loops
            System.Diagnostics.Debug.WriteLine("Erro ao tentar salvar log de erro");
        }
    }

    public void ClearContext()
    {
        _ultimoComando = null;
        _ultimaResposta = null;
        _ultimoDispositivoAcionado = null;
    }
}

