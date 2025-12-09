using System.Media;
using NAudio.Wave;
using System.IO;
using System.Collections.Concurrent;

namespace StarkAid.WindowsForms.Utils;

public static class SoundPlayer
{
    private static readonly ConcurrentQueue<string> _soundQueue = new ConcurrentQueue<string>();
    private static readonly SemaphoreSlim _playbackSemaphore = new SemaphoreSlim(1, 1);
    private static int _isProcessingQueue = 0; // 0 = false, 1 = true (usado com Interlocked)
    
    // Controle específico para mouseMove (pode ser interrompido)
    private static readonly object _mouseMoveLock = new object();
    private static WaveOutEvent? _currentMouseMoveDevice = null;
    private static AudioFileReader? _currentMouseMoveFile = null;
    private static CancellationTokenSource? _mouseMoveCancellation = null;

    private static string GetSoundPath(string filename)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var soundPath = Path.Combine(baseDir, "efectsound", filename);
        
        // Se encontrar no diretório base, retornar
        if (File.Exists(soundPath))
        {
            return soundPath;
        }
        
        // Tentar o diretório do executável (bin/Debug ou bin/Release)
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(exeDir))
        {
            var exeSoundPath = Path.Combine(exeDir, "efectsound", filename);
            if (File.Exists(exeSoundPath))
            {
                return exeSoundPath;
            }
        }
        
        // Tentar o diretório do projeto (para desenvolvimento)
        var projectDir = Path.Combine(baseDir, "..", "..", "..", "..", "efectsound", filename);
        if (File.Exists(projectDir))
        {
            return projectDir;
        }
        
        // Tentar caminho relativo ao diretório atual
        var currentDir = Directory.GetCurrentDirectory();
        var currentSoundPath = Path.Combine(currentDir, "efectsound", filename);
        if (File.Exists(currentSoundPath))
        {
            return currentSoundPath;
        }
        
        // Retornar o caminho padrão mesmo se não existir (será tratado no catch)
        return soundPath;
    }

    public static void PlayMouseMove()
    {
        try
        {
            var soundPath = GetSoundPath("mouseMouve.mp3");
            if (File.Exists(soundPath))
            {
                PlayMouseMoveSound(soundPath);
            }
        }
        catch { }
    }

    public static void StopMouseMove()
    {
        lock (_mouseMoveLock)
        {
            try
            {
                // Cancelar token se existir
                _mouseMoveCancellation?.Cancel();
                _mouseMoveCancellation?.Dispose();
                _mouseMoveCancellation = null;

                // Parar e liberar dispositivo atual
                if (_currentMouseMoveDevice != null)
                {
                    try
                    {
                        if (_currentMouseMoveDevice.PlaybackState != PlaybackState.Stopped)
                        {
                            _currentMouseMoveDevice.Stop();
                        }
                        _currentMouseMoveDevice.Dispose();
                    }
                    catch { }
                    finally
                    {
                        _currentMouseMoveDevice = null;
                    }
                }

                // Liberar arquivo de áudio
                if (_currentMouseMoveFile != null)
                {
                    try
                    {
                        _currentMouseMoveFile.Dispose();
                    }
                    catch { }
                    finally
                    {
                        _currentMouseMoveFile = null;
                    }
                }
            }
            catch { }
        }
    }

    private static void PlayMouseMoveSound(string filePath)
    {
        // Parar som anterior se estiver tocando
        StopMouseMove();

        // Tocar novo som em background
        _ = Task.Run(async () =>
        {
            CancellationTokenSource? cancellationToken = null;
            WaveOutEvent? outputDevice = null;
            AudioFileReader? audioFile = null;

            try
            {
                if (!File.Exists(filePath))
                    return;

                lock (_mouseMoveLock)
                {
                    // Criar novo token de cancelamento
                    _mouseMoveCancellation = new CancellationTokenSource();
                    cancellationToken = _mouseMoveCancellation;

                    audioFile = new AudioFileReader(filePath);
                    outputDevice = new WaveOutEvent();

                    _currentMouseMoveFile = audioFile;
                    _currentMouseMoveDevice = outputDevice;

                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                }

                // Aguardar até terminar ou ser cancelado
                while (!cancellationToken.IsCancellationRequested)
                {
                    lock (_mouseMoveLock)
                    {
                        if (outputDevice == null || 
                            outputDevice.PlaybackState != PlaybackState.Playing)
                        {
                            break;
                        }
                    }
                    await Task.Delay(50);
                }
            }
            catch { }
            finally
            {
                // Limpar recursos quando terminar (apenas se ainda for o dispositivo atual)
                lock (_mouseMoveLock)
                {
                    if (outputDevice == _currentMouseMoveDevice)
                    {
                        try
                        {
                            if (outputDevice != null && outputDevice.PlaybackState != PlaybackState.Stopped)
                            {
                                outputDevice.Stop();
                            }
                            outputDevice?.Dispose();
                            audioFile?.Dispose();
                        }
                        catch { }
                        finally
                        {
                            if (outputDevice == _currentMouseMoveDevice)
                            {
                                _currentMouseMoveDevice = null;
                                _currentMouseMoveFile = null;
                                _mouseMoveCancellation = null;
                            }
                        }
                    }
                }
            }
        });
    }

    public static void PlayClick()
    {
        try
        {
            var soundPath = GetSoundPath("clik.mp3");
            if (File.Exists(soundPath))
            {
                PlayMp3(soundPath);
            }
            else
            {
                // Fallback para som do sistema se o arquivo não existir
                SystemSounds.Asterisk.Play();
            }
        }
        catch { }
    }

    private static void PlayMp3(string filePath)
    {
        // Adicionar à fila
        _soundQueue.Enqueue(filePath);
        
        // Iniciar processamento da fila se ainda não estiver rodando
        if (System.Threading.Interlocked.CompareExchange(ref _isProcessingQueue, 1, 0) == 0)
        {
            _ = ProcessSoundQueueAsync();
        }
    }

    private static async Task ProcessSoundQueueAsync()
    {

        try
        {
            while (_soundQueue.TryDequeue(out string? filePath))
            {
                // Aguardar semáforo antes de tocar
                await _playbackSemaphore.WaitAsync();

                try
                {
                    await PlayMp3FileAsync(filePath);
                }
                finally
                {
                    // Liberar semáforo após tocar
                    _playbackSemaphore.Release();
                    
                    // Pequeno delay entre sons para evitar sobreposição
                    await Task.Delay(50);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao processar fila de áudio: {ex.Message}");
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _isProcessingQueue, 0);
            
            // Se ainda houver itens na fila, processar novamente
            if (!_soundQueue.IsEmpty)
            {
                if (System.Threading.Interlocked.CompareExchange(ref _isProcessingQueue, 1, 0) == 0)
                {
                    _ = ProcessSoundQueueAsync();
                }
            }
        }
    }

    private static async Task PlayMp3FileAsync(string filePath)
    {
        AudioFileReader? audioFile = null;
        WaveOutEvent? outputDevice = null;

        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();

            outputDevice.Init(audioFile);
            outputDevice.Play();

            // Aguardar até o áudio terminar de forma assíncrona
            await Task.Run(() =>
            {
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(50);
                }
            });
        }
        catch (System.AccessViolationException)
        {
            // Ignorar erros de acesso à memória
            System.Diagnostics.Debug.WriteLine("⚠️ AccessViolationException ao tocar áudio (ignorado)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao tocar áudio: {ex.Message}");
        }
        finally
        {
            // Garantir que os recursos sejam liberados corretamente
            try
            {
                if (outputDevice != null)
                {
                    try
                    {
                        if (outputDevice.PlaybackState != PlaybackState.Stopped)
                        {
                            outputDevice.Stop();
                        }
                        outputDevice.Dispose();
                    }
                    catch { }
                }

                if (audioFile != null)
                {
                    try
                    {
                        audioFile.Dispose();
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    public static void PlaySuccess()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch { }
    }

    public static void PlayError()
    {
        try
        {
            SystemSounds.Hand.Play();
        }
        catch { }
    }
}

