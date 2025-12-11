using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using NAudio.Wave;
using StarkAid.WindowsForms.Database;

namespace StarkAid.WindowsForms.Services;

public class SpeechService
{
    private SpeechRecognitionEngine? _recognizer;
    private SpeechSynthesizer? _synthesizer;
    private bool _isListening = false;
    private bool _isSpeaking = false;
    private int _microphoneDeviceId = -1; // -1 = padrão do sistema
    private string? _currentCulture;
    private string? _selectedVoiceName;

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler? SpeakStarted;
    
    public bool IsInitialized => _recognizer != null;
    public bool IsListening => _isListening;
    public bool IsSpeaking => _isSpeaking;
    public string? CurrentCulture => _currentCulture;
    public bool IsPortugueseRecognizer => _currentCulture?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true;

    public void Initialize(int? microphoneId = null, LocalDatabase? database = null)
    {
        try
        {
            // Inicializar TTS
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
            
            // Eventos para controlar flag IsSpeaking e parar/reiniciar reconhecimento
            _synthesizer.SpeakStarted += (s, e) => 
            { 
                _isSpeaking = true;
                System.Diagnostics.Debug.WriteLine("[SpeechService] TTS começou a falar, parando reconhecimento...");
                // Parar reconhecimento de voz enquanto TTS está falando para evitar capturar a própria fala
                if (_isListening)
                {
                    StopListening();
                }
                SpeakStarted?.Invoke(this, EventArgs.Empty);
            };
            _synthesizer.SpeakCompleted += (s, e) => 
            { 
                _isSpeaking = false;
                System.Diagnostics.Debug.WriteLine("[SpeechService] TTS terminou de falar, reiniciando reconhecimento...");
                // Reiniciar reconhecimento de voz após TTS terminar
                // Usar um pequeno delay para garantir que o áudio foi completamente liberado
                System.Threading.Tasks.Task.Delay(300).ContinueWith(_ =>
                {
                    try
                    {
                        if (!_isListening && _recognizer != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[SpeechService] Reiniciando reconhecimento após TTS");
                            StartListening();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SpeechService] Erro ao reiniciar reconhecimento: {ex.Message}");
                    }
                });
            };
            
            // Tentar carregar voz salva do banco de dados
            if (database != null)
            {
                var config = database.GetConfigAssistente();
                if (!string.IsNullOrEmpty(config.VozName))
                {
                    try
                    {
                        _synthesizer.SelectVoice(config.VozName);
                        _selectedVoiceName = config.VozName;
                    }
                    catch
                    {
                        // Voz não encontrada, usar padrão
                        _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                    }
                }
                else
                {
                    _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                }
            }
            else
            {
                _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
            }

            // Inicializar reconhecimento - primeiro verificar quais reconhecedores estão instalados
            _recognizer = null;
            
            try
            {
                var installedRecognizers = SpeechRecognitionEngine.InstalledRecognizers();
                System.Diagnostics.Debug.WriteLine($"Reconhecedores instalados: {installedRecognizers.Count}");
                
                if (installedRecognizers.Count == 0)
                {
                    throw new Exception("Nenhum reconhecedor de voz instalado no sistema. Instale o Windows Speech Recognition e um pacote de idioma.");
                }

                // Priorizar pt-BR, depois pt-PT, depois en-US, depois qualquer outro
                var preferredCultures = new[] { "pt-BR", "pt-PT", "en-US", "en-GB" };
                RecognizerInfo? selectedRecognizer = null;

                // Tentar encontrar um reconhecedor preferido
                foreach (var preferredCulture in preferredCultures)
                {
                    selectedRecognizer = installedRecognizers.FirstOrDefault(r => 
                        r.Culture.Name.Equals(preferredCulture, StringComparison.OrdinalIgnoreCase));
                    if (selectedRecognizer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Reconhecedor preferido encontrado: {preferredCulture}");
                        break;
                    }
                }

                // Se não encontrou um preferido, usar o primeiro disponível
                if (selectedRecognizer == null)
                {
                    selectedRecognizer = installedRecognizers[0];
                    System.Diagnostics.Debug.WriteLine($"Usando primeiro reconhecedor disponível: {selectedRecognizer.Culture.Name}");
                }

                // Criar o reconhecedor com o selecionado
                _recognizer = new SpeechRecognitionEngine(selectedRecognizer);
                _currentCulture = selectedRecognizer.Culture.Name;
                System.Diagnostics.Debug.WriteLine($"Reconhecedor inicializado com sucesso: {_currentCulture}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao inicializar reconhecedor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return; // Não conseguiu inicializar, retornar sem erro (o _recognizer ficará null)
            }

            if (_recognizer == null)
            {
                System.Diagnostics.Debug.WriteLine("AVISO: Reconhecedor de voz não inicializado. O reconhecimento não funcionará.");
                return;
            }
            
            // Criar gramática livre (dictation)
            try
            {
                var dictationGrammar = new DictationGrammar();
                _recognizer.LoadGrammar(dictationGrammar);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar gramática de ditado: {ex.Message}");
                // Tentar usar gramática livre como alternativa
                try
                {
                    var freeDictationGrammar = new DictationGrammar("grammar:dictation");
                    _recognizer.LoadGrammar(freeDictationGrammar);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Não foi possível carregar gramática de ditado.");
                }
            }

            _recognizer.SpeechRecognized += (sender, e) =>
            {
                // Ajustar limiar de confiança baseado na cultura
                // Se não for português, usar limiar mais baixo (0.25) para tentar capturar algo
                // Se for português, usar limiar normal (0.5)
                float confidenceThreshold = IsPortugueseRecognizer ? 0.5f : 0.25f;
                
                if (e.Result.Confidence > confidenceThreshold)
                {
                    System.Diagnostics.Debug.WriteLine($"Texto reconhecido (confiança: {e.Result.Confidence:F2}, cultura: {_currentCulture}): {e.Result.Text}");
                    SpeechRecognized?.Invoke(this, e.Result.Text);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Reconhecimento com baixa confiança ({e.Result.Confidence:F2}): {e.Result.Text}");
                    // Se não for português e a confiança for muito baixa, pode ser que esteja falando em português
                    // Tentar processar mesmo assim se for acima de um limiar mínimo
                    if (!IsPortugueseRecognizer && e.Result.Confidence > 0.15f)
                    {
                        System.Diagnostics.Debug.WriteLine($"AVISO: Tentando processar com confiança baixa ({e.Result.Confidence:F2}) - pode ser português sendo reconhecido como inglês");
                        // Não processar automaticamente, mas logar para debug
                    }
                }
            };

            _recognizer.SpeechRecognitionRejected += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Reconhecimento rejeitado (confiança: {e.Result.Confidence:F2}): {e.Result.Text}");
            };

            // Configurar microfone se especificado
            if (microphoneId.HasValue)
            {
                SetMicrophone(microphoneId.Value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar SpeechService: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public void SetMicrophone(int deviceId)
    {
        _microphoneDeviceId = deviceId;
        
        // Se estiver escutando, reiniciar com o novo microfone
        if (_isListening && _recognizer != null)
        {
            var wasListening = _isListening;
            StopListening();
            
            if (wasListening)
            {
                StartListening();
            }
        }
    }

    public void SetVoice(string voiceName)
    {
        if (_synthesizer == null) return;

        try
        {
            _synthesizer.SelectVoice(voiceName);
            _selectedVoiceName = voiceName;
            System.Diagnostics.Debug.WriteLine($"Voz alterada para: {voiceName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao alterar voz: {ex.Message}");
        }
    }

    public void StartListening()
    {
        if (_isListening || _recognizer == null)
        {
            if (_recognizer == null)
            {
                System.Diagnostics.Debug.WriteLine("AVISO: Reconhecedor não está inicializado. Não é possível iniciar a escuta.");
            }
            return;
        }

        try
        {
            // Configurar microfone baseado no ID salvo
            // Índice 0 = Padrão do Sistema, Índice 1+ = Dispositivos específicos
            // Por enquanto, o SpeechRecognitionEngine não suporta diretamente seleção de dispositivo
            // então sempre usamos o padrão. A configuração é salva para referência futura.
            // TODO: Implementar seleção de dispositivo específico usando InstalledRecognizers ou APIs mais avançadas
            
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _isListening = true;
            System.Diagnostics.Debug.WriteLine("Escuta iniciada com sucesso!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao iniciar escuta: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            _isListening = false;
        }
    }

    public void StopListening()
    {
        if (!_isListening || _recognizer == null) return;

        try
        {
            _recognizer.RecognizeAsyncStop();
            _isListening = false;
            System.Diagnostics.Debug.WriteLine("Escuta parada com sucesso.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao parar escuta: {ex.Message}");
            _isListening = false;
        }
    }

    /// <summary>
    /// Fala o texto usando TTS. O texto deve ter acentuação e pontuação corretas para uma fala natural.
    /// IMPORTANTE: NUNCA normalize o texto antes de passar para este método.
    /// </summary>
    public void Speak(string text)
    {
        if (_synthesizer == null) return;

        try
        {
            // IMPORTANTE: O texto deve manter acentuação e pontuação originais para uma fala natural.
            // NÃO usar NormalizeText aqui - isso é apenas para processar comandos de entrada.
            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao falar: {ex.Message}");
        }
    }

    public void SpeakAsyncCancel()
    {
        _synthesizer?.SpeakAsyncCancelAll();
        _isSpeaking = false;
    }

    /// <summary>
    /// Normaliza o texto removendo acentos e pontuação para processamento de comandos.
    /// IMPORTANTE: Use APENAS para processar comandos de entrada. NUNCA use nas respostas que serão faladas pelo TTS.
    /// As respostas TTS devem manter acentuação e pontuação corretas para uma fala natural.
    /// </summary>
    public string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Converter para minúsculas
        text = text.ToLowerInvariant();

        // Remover acentos
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            // Manter apenas letras, números e espaços (remover acentos e pontuação)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                // Remover pontuação: .,?!;:()[]{}\"'- etc
                // Manter apenas letras, números e espaços
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    stringBuilder.Append(c);
                }
            }
        }

        // Normalizar espaços múltiplos em um único espaço
        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
        
        return result;
    }

    public void Dispose()
    {
        StopListening();
        _recognizer?.Dispose();
        _synthesizer?.Dispose();
    }
}

