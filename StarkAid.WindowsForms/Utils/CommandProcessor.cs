using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;
using System.Diagnostics;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarkAid.WindowsForms.Utils;

/// <summary>
/// Processa comandos de voz do usuário.
/// IMPORTANTE SOBRE NORMALIZAÇÃO:
/// - NormalizeText é usado APENAS para processar comandos de entrada (comparação, busca, etc.)
/// - Todas as respostas que serão faladas pelo TTS devem SEMPRE manter acentuação e pontuação originais
/// - As respostas da IA devem vir do backend com acentuação e pontuação corretas
/// - Respostas hardcoded no código devem ter acentuação e pontuação corretas
/// - NUNCA aplicar NormalizeText nas respostas que serão faladas pelo TTS
/// </summary>

public class CommandProcessor
{
    private readonly LocalDatabase _database;
    private readonly SpeechService _speechService;
    private readonly ProcessComandoGeral _processComandoGeral;
    private readonly UdpService _udpService;
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private bool _iaEnabled = false;
    private bool _aprendizadoEnabled = false;
    private DateTime? _timeOfStopStart = null;
    private bool _timeOfStopBlocked = false;
    private string? _ultimoComandoUser = null;
    private string? _ultimaRespostaIa = null;

    // Evento para notificar quando um comando de IA for executado
    public event EventHandler? IaCommandExecuted;
    
    // Evento para notificar quando o status de bloqueio mudar
    public event EventHandler<bool>? TimeOfStopBlockedChanged;
    
    // Evento para notificar quando ativar inteligência por comando de voz
    public event EventHandler? AtivarInteligenciaRequested;
    
    // Evento para notificar quando desativar inteligência por comando de voz
    public event EventHandler? DesativarInteligenciaRequested;

    public bool IaEnabled
    {
        get => _iaEnabled;
        set => _iaEnabled = value;
    }

    public bool AprendizadoEnabled
    {
        get => _aprendizadoEnabled;
        set => _aprendizadoEnabled = value;
    }

    public bool IsBlocked
    {
        get => _timeOfStopBlocked;
    }
    
    public void VerificarEAtualizarBloqueio()
    {
        // Verificar se deve bloquear (passou 3 minutos sem TTS)
        if (!_timeOfStopBlocked && _timeOfStopStart.HasValue)
        {
            var tempoDecorrido = DateTime.Now - _timeOfStopStart.Value;
            if (tempoDecorrido.TotalMinutes >= 3)
            {
                var wasBlocked = _timeOfStopBlocked;
                _timeOfStopBlocked = true;
                
                // Notificar mudança de status se acabou de bloquear
                if (!wasBlocked && _timeOfStopBlocked)
                {
                    TimeOfStopBlockedChanged?.Invoke(this, _timeOfStopBlocked);
                }
            }
        }
    }

    public void ResetTimeOfStop()
    {
        var wasBlocked = _timeOfStopBlocked;
        _timeOfStopStart = DateTime.Now;
        _timeOfStopBlocked = false;
        
        // Notificar mudança de status se necessário
        if (wasBlocked != _timeOfStopBlocked)
        {
            TimeOfStopBlockedChanged?.Invoke(this, _timeOfStopBlocked);
        }
    }

    public void StartTimeOfStop()
    {
        _timeOfStopStart = DateTime.Now;
        _timeOfStopBlocked = false;
    }
    // private readonly ProcessComandoGeral _processComandoGeral;
    public CommandProcessor(
        LocalDatabase database,
        SpeechService speechService,
        ProcessComandoGeral processComandoGeral,
        UdpService udpService,
        ApiService apiService,
        WebSocketService webSocketService)
    {
        _database = database;
        _speechService = speechService;
        _processComandoGeral = processComandoGeral;
        _udpService = udpService;
        _apiService = apiService;
        _webSocketService = webSocketService;
        
        // Conectar evento para resetar timeOfStop quando falar
        _speechService.SpeakStarted += (s, e) => ResetTimeOfStop();
    }

    public async Task ProcessCommandAsync(string comando)
    {
        // IMPORTANTE: NormalizeText é usado APENAS para processar comandos de entrada.
        // As respostas que serão faladas pelo TTS devem SEMPRE manter acentuação e pontuação originais.
        
        // Verificar se está falando - se estiver, só processar comandos de parar
        var normalized = _speechService.NormalizeText(comando);
        var comandoOriginal = comando;
        
        // Verificar timeOfStop - se bloqueado, só processar se contém nome do assistente
        if (_timeOfStopBlocked)
        {
            var config = _database.GetConfigAssistente();
            var nomeAssistente = config.NomeAssistente ?? "";
            if (!string.IsNullOrEmpty(nomeAssistente) && normalized.Contains(nomeAssistente))
            {
                // Liberar processamento e resetar contador
                ResetTimeOfStop();
            }
            else
            {
                // Ainda bloqueado, não processar
                return;
            }
        }
        else if (_timeOfStopStart.HasValue)
        {
            // Verificar se passou 3 minutos sem TTS
            var tempoDecorrido = DateTime.Now - _timeOfStopStart.Value;
            if (tempoDecorrido.TotalMinutes >= 3)
            {
                var wasBlocked = _timeOfStopBlocked;
                _timeOfStopBlocked = true;
                
                // Notificar mudança de status se acabou de bloquear
                if (!wasBlocked && _timeOfStopBlocked)
                {
                    TimeOfStopBlockedChanged?.Invoke(this, _timeOfStopBlocked);
                }
                
                return; // Bloquear processamento
            }
        }
        
        // Comandos para parar de falar
        if (normalized.Contains("cala a boca") || normalized.Contains("pare de falar") || 
            normalized.Contains("para de falar") || normalized.Contains("parar de falar"))
        {
            _speechService.SpeakAsyncCancel();
            _database.SaveUltimoComando(comando);
            ResetTimeOfStop(); // Resetar ao cancelar fala
            return;
        }
        
        // Se estiver falando, não processar outros comandos
        if (_speechService.IsSpeaking)
        {
            return;
        }
        
        _database.SaveUltimoComando(comando);
        
        // Verificar se comando é exatamente igual ao nome do assistente (resposta padrão)
        if (ProcessRespostaPadrao(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }
        
        // Verificar se é comando de lembrete (antes de outros processamentos)
        if (await ProcessLembreteCommandAsync(normalized, comandoOriginal))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }
        
        // Verificar comandos para desativar inteligência (primeiro para evitar conflito com "ativar")
        if (ProcessDesativarInteligenciaCommand(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }
        
        // Verificar comandos para ativar inteligência
        if (await ProcessAtivarInteligenciaCommandAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Verificar comandos shell (antes de comandos locais)
        if (await ProcessShellCommandsAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Comandos locais do Windows (horas, data, calculadora, links, tempo, saudações)
        if (await ProcessLocalCommandsAsync(normalized, comandoOriginal))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Comandos de dispositivos Ewelink (primeiro)
        if (await ProcessEwelinkDeviceCommandsAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Comandos de dispositivos StarkSwitch (segundo)
        if (await ProcessStarkSwitchDeviceCommandsAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Comandos de dispositivos ESP (terceiro)
        if (await ProcessDeviceCommandsAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Comandos sociais do banco local (quarto)
        if (await ProcessSocialCommandsAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Comandos de aprendizado (quinto)
        if (await ProcessAprendizadoCommandsAsync(normalized))
        {
            ResetTimeOfStop(); // Resetar ao processar comando
            return;
        }

        // Se não encontrou nenhum comando e IA está ativada, chamar Super IA
        if (_iaEnabled)
        {
            // Verificar se está online antes de chamar IA
            var isOnline = await _apiService.CheckApiStatusAsync();
            if (!isOnline)
            {
                _speechService.Speak("A inteligência artificial requer conexão com a API. Por favor, aguarde a conexão ser restabelecida.");
                return;
            }
            
            _ultimoComandoUser = comandoOriginal;
            await ProcessIaCommandAsync(comandoOriginal);
            ResetTimeOfStop(); // Resetar ao processar comando
        }
    }
    
    private bool ProcessRespostaPadrao(string comandoNormalizado)
    {
        try
        {
            var config = _database.GetConfigAssistente();
            var nomeAssistente = config.NomeAssistente;
            var respostaPadrao = config.RespostaPadrao;
            
            // Se não tem nome do assistente ou resposta padrão configurada, retornar false
            if (string.IsNullOrEmpty(nomeAssistente) || string.IsNullOrEmpty(respostaPadrao))
            {
                return false;
            }
            
            // Normalizar o nome do assistente para comparação
            var nomeAssistenteNormalizado = _speechService.NormalizeText(nomeAssistente);
            
            // Verificar se o comando é EXATAMENTE igual ao nome do assistente (sem outras palavras)
            // Remover espaços extras e comparar
            var comandoLimpo = comandoNormalizado.Trim();
            var nomeLimpo = nomeAssistenteNormalizado.Trim();
            
            if (comandoLimpo.Equals(nomeLimpo, StringComparison.OrdinalIgnoreCase))
            {
                // Comando é exatamente igual ao nome do assistente - falar resposta padrão
                _speechService.Speak(respostaPadrao);
                System.Diagnostics.Debug.WriteLine($"[RESPOSTA PADRÃO] Comando '{comandoLimpo}' é exatamente igual ao nome '{nomeLimpo}', falando resposta padrão");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar resposta padrão: {ex.Message}");
            return false;
        }
    }
    
    private async Task<bool> ProcessAtivarInteligenciaCommandAsync(string normalized)
    {
        try
        {
            // Primeiro verificar se NÃO é um comando de desativar (evitar capturar "desativar" como "ativar")
            bool temDesativar = normalized.Contains("desativar") || normalized.Contains("desativa") || normalized.Contains("desative");
            bool temDesligar = normalized.Contains("desliga") || normalized.Contains("desligue") || normalized.Contains("desligar");
            
            if (temDesativar || temDesligar)
            {
                // É comando de desativar, não processar aqui
                return false;
            }
            
            // Verificar se contém palavras-chave para ativar inteligência
            bool temAtivar = normalized.Contains("ativar") || normalized.Contains("ativa") || normalized.Contains("ative");
            bool temLigar = normalized.Contains("liga") || normalized.Contains("ligue") || normalized.Contains("ligar");
            bool temInteligencia = normalized.Contains("inteligencia") || normalized.Contains("inteligência");
            
            // Verificar se é um comando de ativar inteligência
            if (temInteligencia && (temAtivar || temLigar))
            {
                // Verificar se está online antes de ativar IA
                var isOnline = await _apiService.CheckApiStatusAsync();
                if (!isOnline)
                {
                    _speechService.Speak("A inteligência artificial requer conexão com a API. Por favor, aguarde a conexão ser restabelecida.");
                    return true;
                }
                
                // Disparar evento para ativar inteligência
                AtivarInteligenciaRequested?.Invoke(this, EventArgs.Empty);
                
                // Enviar comando "Ativar inteligencia" para a IA e falar a resposta
                var request = new SuperIaRequest
                {
                    Texto = "Ativar inteligencia",
                    ContextoUser = "",
                    ContextoIA = "",
                    Estilo = ""
                };
                
                var response = await _apiService.CallSuperIaAsync(request);
                if (response != null && !string.IsNullOrEmpty(response.Texto))
                {
                    _speechService.Speak(response.Texto);
                    return true;
                }
                
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando de ativar inteligência: {ex.Message}");
        }
        
        return false;
    }
    
    private bool ProcessDesativarInteligenciaCommand(string normalized)
    {
        try
        {
            // Verificar se contém palavras-chave para desativar inteligência
            // Usar uma abordagem mais flexível que detecta as palavras principais
            bool temDesativar = normalized.Contains("desativar") || normalized.Contains("desativa") || normalized.Contains("desative");
            bool temDesligar = normalized.Contains("desliga") || normalized.Contains("desligue") || normalized.Contains("desligar");
            bool temInteligencia = normalized.Contains("inteligencia") || normalized.Contains("inteligência");
            
            // Verificar se é um comando de desativar inteligência
            if (temInteligencia && (temDesativar || temDesligar))
            {
                // Disparar evento para desativar inteligência
                DesativarInteligenciaRequested?.Invoke(this, EventArgs.Empty);
                
                // Falar confirmação
                _speechService.Speak("Inteligência desativada");
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando de desativar inteligência: {ex.Message}");
        }
        
        return false;
    }

    private async Task<bool> ProcessLocalCommandsAsync(string comando, string comandoOriginal)
    {
        // Todas as verificações usam texto normalizado (minúsculas, sem acentos, sem pontuação)
        
        // Saudações
        if (comando.Contains("bom dia"))
        {
            var hora = DateTime.Now.Hour;
            if (hora >= 12 && hora < 18)
            {
                _speechService.Speak("Na verdade, já é tarde. Boa tarde!");
            }
            else if (hora >= 18 || hora < 6)
            {
                _speechService.Speak("Na verdade, já é noite. Boa noite!");
            }
            else
            {
                _speechService.Speak("Bom dia! Como posso ajudar?");
            }
            return true;
        }

        if (comando.Contains("boa tarde"))
        {
            var hora = DateTime.Now.Hour;
            if (hora >= 6 && hora < 12)
            {
                _speechService.Speak("Na verdade, ainda é manhã. Bom dia!");
            }
            else if (hora >= 18 || hora < 6)
            {
                _speechService.Speak("Na verdade, já é noite. Boa noite!");
            }
            else
            {
                _speechService.Speak("Boa tarde! Como posso ajudar?");
            }
            return true;
        }

        if (comando.Contains("boa noite"))
        {
            var hora = DateTime.Now.Hour;
            if (hora >= 6 && hora < 12)
            {
                _speechService.Speak("Na verdade, ainda é manhã. Bom dia!");
            }
            else if (hora >= 12 && hora < 18)
            {
                _speechService.Speak("Na verdade, ainda é tarde. Boa tarde!");
            }
            else
            {
                _speechService.Speak("Boa noite! Como posso ajudar?");
            }
            return true;
        }
        
        if (comando.Contains("que horas sao"))
        {
            Debug.WriteLine("Processando comando de horas");

            var hora = DateTime.Now.ToString("HH:mm");
            Debug.WriteLine($"São {hora}");
            _speechService.Speak($"São {hora}");
            return true;
        }

        if (comando.Contains("que dia e hoje"))
        {
            Debug.WriteLine("Processando comando de data");
            
            var dia = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));
            Debug.WriteLine($"Hoje é {dia}");
            _speechService.Speak($"Hoje é {dia}");
            return true;
        }

        // Previsão do tempo
        if (_processComandoGeral.IsAskingWeather(comando))
        {
            try
            {
                var weatherData = await _apiService.GetWeatherForecastAsync();
                if (weatherData?.Current != null)
                {
                    var temp = Math.Round(weatherData.Current.Temperature);
                    var condition = weatherData.Current.WeatherDescription ?? "sem informações";
                    var wind = Math.Round(weatherData.Current.WindSpeed);
                    var windDir = weatherData.Current.WindDirectionText ?? "";
                    
                    var resposta = $"Temperatura {temp} graus Celsius, {condition} e vento {wind} quilômetros por hora";
                    if (!string.IsNullOrEmpty(windDir))
                    {
                        resposta += $" {windDir}";
                    }
                    
                    _speechService.Speak(resposta);
                }
                else
                {
                    _speechService.Speak("Não foi possível obter informações do tempo no momento");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar previsão do tempo: {ex.Message}");
                _speechService.Speak("Não foi possível obter informações do tempo no momento");
            }
            return true;
        }

        // Abrir links
        if (_processComandoGeral.IsAskingInternetLink(comando))
        {
            string? url = null;
            string? nomeSite = null;
            
            if (comando.Contains("youtube"))
            {
                url = "https://www.youtube.com";
                nomeSite = "YouTube";
            }
            else if (comando.Contains("facebook"))
            {
                url = "https://www.facebook.com";
                nomeSite = "Facebook";
            }
            else if (comando.Contains("instagram"))
            {
                url = "https://www.instagram.com";
                nomeSite = "Instagram";
            }
            else if (comando.Contains("google"))
            {
                url = "https://www.google.com";
                nomeSite = "Google";
            }
            else if (comando.Contains("whatsapp"))
            {
                url = "https://web.whatsapp.com";
                nomeSite = "WhatsApp";
            }
            else if (comando.Contains("gmail"))
            {
                url = "https://mail.google.com";
                nomeSite = "Gmail";
            }
            else if (comando.Contains("twitter") || comando.Contains("x.com"))
            {
                url = "https://www.twitter.com";
                nomeSite = "Twitter";
            }
            else if (comando.Contains("linkedin"))
            {
                url = "https://www.linkedin.com";
                nomeSite = "LinkedIn";
            }
            
            if (url != null && nomeSite != null)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    _speechService.Speak($"{nomeSite} aberto");
                }
                catch
                {
                    _speechService.Speak($"Não foi possível abrir o {nomeSite}");
                }
                return true;
            }
        }

        if (comando.Contains("abra calculadora") || comando.Contains("abrir calculadora"))
        {
            System.Diagnostics.Process.Start("calc.exe");
            _speechService.Speak("Calculadora aberta");
            return true;
        }

        if (comando.Contains("abra meus documentos") || comando.Contains("abrir meus documentos"))
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Diagnostics.Process.Start("explorer.exe", documentsPath);
            _speechService.Speak("Meus documentos aberto");
            return true;
        }

        if (comando.Contains("esvazie a lixeira") || comando.Contains("esvaziar lixeira"))
        {
            try
            {
                System.Diagnostics.Process.Start("cleanmgr.exe", "/d");
                _speechService.Speak("Limpando lixeira");
            }
            catch
            {
                _speechService.Speak("Não foi possível esvaziar a lixeira");
            }
            return true;
        }

        return false;
    }

    private async Task<bool> ProcessSocialCommandsAsync(string comando)
    {
        var comandosSociais = _database.GetComandosSociais();
        
        foreach (var cmd in comandosSociais)
        {
            var cmdNormalized = _speechService.NormalizeText(cmd.Comando);
            if (comando.Contains(cmdNormalized))
            {
                string respostaFinal;
                
                // Verificar se RespostasAleatorias não é um erro
                if (string.IsNullOrEmpty(cmd.RespostasAleatorias))
                {
                    // Se vazio, usar apenas a resposta padrão
                    respostaFinal = cmd.Resposta;
                }
                else
                {
                    try
                    {
                        // Parsear JSON de respostas aleatórias
                        var json = JObject.Parse(cmd.RespostasAleatorias);
                        var alternativas = json["alternativas"]?.ToObject<List<string>>() ?? new List<string>();
                        
                        // Verificar se a primeira alternativa começa com "Erro"
                        bool isErro = alternativas.Count > 0 && 
                                     alternativas[0].TrimStart().StartsWith("Erro", StringComparison.OrdinalIgnoreCase);
                        
                        if (isErro)
                        {
                            // Se for erro, usar apenas a resposta padrão
                            respostaFinal = cmd.Resposta;
                        }
                        else
                        {
                            // Adicionar a resposta padrão à lista
                            alternativas.Add(cmd.Resposta);
                            
                            // Selecionar uma resposta aleatória
                            var random = new Random();
                            respostaFinal = alternativas[random.Next(alternativas.Count)];
                        }
                    }
                    catch
                    {
                        // Se houver erro ao parsear, usar apenas a resposta padrão
                        respostaFinal = cmd.Resposta;
                    }
                }
                
                _speechService.Speak(respostaFinal);
                return true;
            }
        }

        return false;
    }

    private async Task<bool> ProcessDeviceCommandsAsync(string comando)
    {
        try
        {
            var dispositivos = _database.GetDispositivosEsp();
            var comandoNormalized = _speechService.NormalizeText(comando);
            
            // Lista de dispositivos encontrados com suas pontuações
            var dispositivosEncontrados = new List<(DispositivoEsp dispositivo, int palavrasEncontradas, bool matchCompleto, string comandoUDP)>();
            
            var stopWords = new[] { "ligar", "desligar", "acende", "apaga", "liga", "desliga", 
                                   "acenda", "apague", "acender", "apagar", "ligue", "desligue",
                                   "a", "o", "as", "os", "da", "do", "das", "dos", "de", "em", 
                                   "na", "no", "nas", "nos", "para", "por", "com", "sem" };
            
            var palavrasComando = comandoNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !stopWords.Any(sw => p.Equals(sw, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var dispositivo in dispositivos)
            {
                if (string.IsNullOrEmpty(dispositivo.Comando))
                    continue;
                
                var cmdNormalized = _speechService.NormalizeText(dispositivo.Comando);
                var nomeNormalized = _speechService.NormalizeText(dispositivo.Nome);
                
                // Verificar match por comando
                bool matchPorComando = comandoNormalized.Contains(cmdNormalized);
                
                // Verificar match por nome completo
                bool matchPorNomeCompleto = comandoNormalized.Contains(nomeNormalized) || nomeNormalized.Contains(comandoNormalized);
                
                if (matchPorComando || matchPorNomeCompleto)
                {
                    var palavrasNome = nomeNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(p => !stopWords.Any(sw => p.Equals(sw, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    
                    var palavrasEncontradas = palavrasComando.Count(p => palavrasNome.Any(n => 
                        n.Equals(p, StringComparison.OrdinalIgnoreCase) || 
                        n.Contains(p, StringComparison.OrdinalIgnoreCase) || 
                        p.Contains(n, StringComparison.OrdinalIgnoreCase)));
                    
                    // Usar ComandToEsp se disponível, senão usar Comando
                    var comandoParaEnviar = !string.IsNullOrWhiteSpace(dispositivo.ComandToEsp) 
                        ? dispositivo.ComandToEsp 
                        : dispositivo.Comando;
                    
                    var comandoUDP = $"{dispositivo.Ip}|{dispositivo.Porta}|{comandoParaEnviar}";
                    dispositivosEncontrados.Add((dispositivo, palavrasEncontradas, matchPorNomeCompleto, comandoUDP));
                }
            }
            
            DispositivoEsp? dispositivoEncontrado = null;
            string? comandoEnviarUDP = null;
            
            if (dispositivosEncontrados.Count == 0)
            {
                return false;
            }
            else if (dispositivosEncontrados.Count == 1)
            {
                dispositivoEncontrado = dispositivosEncontrados[0].dispositivo;
                comandoEnviarUDP = dispositivosEncontrados[0].comandoUDP;
            }
            else
            {
                // Múltiplos dispositivos encontrados - priorizar o com mais palavras
                var dispositivoComMaisPalavras = dispositivosEncontrados
                    .OrderByDescending(d => d.matchCompleto) // Primeiro os matches completos
                    .ThenByDescending(d => d.palavrasEncontradas) // Depois os com mais palavras
                    .First();
                
                dispositivoEncontrado = dispositivoComMaisPalavras.dispositivo;
                comandoEnviarUDP = dispositivoComMaisPalavras.comandoUDP;
                System.Diagnostics.Debug.WriteLine($"[ESP] Múltiplos dispositivos encontrados, selecionado o com mais palavras: {dispositivoEncontrado.Nome}");
            }

            if (!string.IsNullOrEmpty(comandoEnviarUDP))
            {
                var parts = comandoEnviarUDP.Split('|');
                if (parts.Length == 3)
                {
                    if (System.Net.IPAddress.TryParse(parts[0], out _) && int.TryParse(parts[1], out var porta))
                    {
                        _udpService.SendCommand(parts[0], porta, parts[2]);
                        
                        // Resposta aleatória
                        string? nomeDispositivo = null;
                        if (dispositivoEncontrado != null)
                        {
                            nomeDispositivo = dispositivoEncontrado.Nome;
                            var respostasAleatorias = new[]
                            {
                                $"{nomeDispositivo} acionado, posso ajudar em algo mais?",
                                $"{nomeDispositivo} acionado, mais alguma coisa?",
                                $"{nomeDispositivo} acionado, está tudo certo!",
                                $"{nomeDispositivo} acionado, precisa de mais alguma coisa?",
                                $"{nomeDispositivo} acionado, pronto!"
                            };
                            var random = new Random();
                            var resposta = respostasAleatorias[random.Next(respostasAleatorias.Length)];
                            _speechService.Speak(resposta);
                        }
                        else
                        {
                            var respostasAleatorias = new[]
                            {
                                "Comando enviado, posso ajudar em algo mais?",
                                "Comando enviado, mais alguma coisa?",
                                "Comando enviado, está tudo certo!",
                                "Comando enviado, precisa de mais alguma coisa?",
                                "Comando enviado, pronto!"
                            };
                            var random = new Random();
                            var resposta = respostasAleatorias[random.Next(respostasAleatorias.Length)];
                            _speechService.Speak(resposta);
                        }
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando dispositivo ESP: {ex.Message}");
            LocalDatabase.LogError(_database, ex, "ERR_002", "ao processar comando de dispositivo ESP", 
                null, null, null);
            return false;
        }
    }

    private async Task<bool> ProcessEwelinkDeviceCommandsAsync(string comando)
    {
        try
        {
            // Verificar se está logado no Ewelink
            var status = await _apiService.GetEwelinkStatusAsync();
            if (status == null || !status.IsLoggedIn)
            {
                return false; // Não está logado, não processar comandos Ewelink
            }

            // Buscar dispositivos Ewelink
            var dispositivos = await _apiService.GetEwelinkDevicesAsync();
            if (dispositivos == null || dispositivos.Count == 0)
            {
                return false; // Não há dispositivos
            }

            // Normalizar e corrigir variações de palavras (ex: "acende" -> "ligar")
            var comandoNormalized = _speechService.NormalizeText(comando);
            var comandoCorrigido = _processComandoGeral.CorrectingWordVariationsToAutomation(comandoNormalized);
            var comandoLower = comandoCorrigido.ToLower();
            
            System.Diagnostics.Debug.WriteLine($"[EWELINK] Comando original: {comando}");
            System.Diagnostics.Debug.WriteLine($"[EWELINK] Comando normalizado: {comandoNormalized}");
            System.Diagnostics.Debug.WriteLine($"[EWELINK] Comando corrigido: {comandoCorrigido}");

            // Procurar por comandos do tipo "ligar dispositivo X" ou "desligar dispositivo X"
            // Usar comandoNormalized para detectar variações antes da correção
            bool isLigar = comandoLower.Contains("ligar") || comandoLower.Contains("ligue") || 
                          comandoNormalized.Contains("acende") || comandoNormalized.Contains("acenda") || 
                          comandoNormalized.Contains("acender");
            bool isDesligar = comandoLower.Contains("desligar") || comandoLower.Contains("desligue") ||
                             comandoNormalized.Contains("apaga") || comandoNormalized.Contains("apague") ||
                             comandoNormalized.Contains("apagar");

            System.Diagnostics.Debug.WriteLine($"[EWELINK] isLigar: {isLigar}, isDesligar: {isDesligar}");
            
            // Se ambos forem detectados, priorizar desligar (mais específico)
            if (isLigar && isDesligar)
            {
                System.Diagnostics.Debug.WriteLine("[EWELINK] Ambos detectados, priorizando desligar");
                isLigar = false;
            }

            if (!isLigar && !isDesligar)
            {
                System.Diagnostics.Debug.WriteLine("[EWELINK] Comando não é de ligar/desligar");
                return false; // Não é comando de ligar/desligar
            }

            // Procurar dispositivo pelo nome no comando (usar comando original normalizado para busca)
            var comandoParaBusca = _speechService.NormalizeText(comando);
            
            System.Diagnostics.Debug.WriteLine($"[EWELINK] Buscando dispositivo em {dispositivos.Count} dispositivos...");
            System.Diagnostics.Debug.WriteLine($"[EWELINK] Comando para busca: '{comandoParaBusca}'");
            
            // Extrair palavras-chave do comando (remover verbos de ação e artigos/preposições comuns)
            var stopWords = new[] { "ligar", "desligar", "acende", "apaga", "liga", "desliga", 
                                   "acenda", "apague", "acender", "apagar", "ligue", "desligue",
                                   "a", "o", "as", "os", "da", "do", "das", "dos", "de", "em", 
                                   "na", "no", "nas", "nos", "para", "por", "com", "sem" };
            
            var palavrasComando = comandoParaBusca.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !stopWords.Any(sw => p.Equals(sw, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[EWELINK] Palavras-chave extraídas: {string.Join(", ", palavrasComando)}");
            
            // Lista de dispositivos encontrados com suas pontuações
            var dispositivosEncontrados = new List<(EwelinkDevice dispositivo, int palavrasEncontradas, bool matchCompleto)>();
            
            foreach (var dispositivo in dispositivos)
            {
                var nomeNormalized = _speechService.NormalizeText(dispositivo.Name);
                System.Diagnostics.Debug.WriteLine($"[EWELINK] Verificando dispositivo: '{dispositivo.Name}' (normalizado: '{nomeNormalized}')");
                
                // Verificar se o nome do dispositivo está no comando (busca completa)
                if (comandoParaBusca.Contains(nomeNormalized) || nomeNormalized.Contains(comandoParaBusca))
                {
                    dispositivosEncontrados.Add((dispositivo, nomeNormalized.Split(' ').Length, true));
                    System.Diagnostics.Debug.WriteLine($"[EWELINK] Dispositivo encontrado (busca completa): {dispositivo.Name}");
                    continue;
                }
                
                // Verificar se palavras-chave do comando estão no nome do dispositivo
                var palavrasNome = nomeNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => !stopWords.Any(sw => p.Equals(sw, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                var palavrasEncontradas = palavrasComando.Count(p => palavrasNome.Any(n => 
                    n.Equals(p, StringComparison.OrdinalIgnoreCase) || 
                    n.Contains(p, StringComparison.OrdinalIgnoreCase) || 
                    p.Contains(n, StringComparison.OrdinalIgnoreCase)));
                
                // Se encontrou pelo menos 2 palavras ou todas as palavras-chave
                if (palavrasEncontradas >= 2 || (palavrasComando.Count > 0 && palavrasComando.Count <= 3 && palavrasEncontradas == palavrasComando.Count))
                {
                    dispositivosEncontrados.Add((dispositivo, palavrasEncontradas, false));
                    System.Diagnostics.Debug.WriteLine($"[EWELINK] Dispositivo encontrado (busca por palavras-chave: {palavrasEncontradas}/{palavrasComando.Count}): {dispositivo.Name}");
                }
            }

            EwelinkDevice? dispositivoEncontrado = null;
            
            if (dispositivosEncontrados.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[EWELINK] Dispositivo não encontrado no comando");
                return false; // Não encontrou, mas não avisa - fluxo continua
            }
            else if (dispositivosEncontrados.Count == 1)
            {
                dispositivoEncontrado = dispositivosEncontrados[0].dispositivo;
            }
            else
            {
                // Múltiplos dispositivos encontrados - priorizar o com mais palavras
                var dispositivoComMaisPalavras = dispositivosEncontrados
                    .OrderByDescending(d => d.matchCompleto) // Primeiro os matches completos
                    .ThenByDescending(d => d.palavrasEncontradas) // Depois os com mais palavras
                    .First();
                
                dispositivoEncontrado = dispositivoComMaisPalavras.dispositivo;
                System.Diagnostics.Debug.WriteLine($"[EWELINK] Múltiplos dispositivos encontrados, selecionado o com mais palavras: {dispositivoEncontrado.Name}");
            }

            // Verificar estado atual antes de ligar/desligar
            var deviceStatus = await _apiService.GetEwelinkDeviceStatusAsync(dispositivoEncontrado.DeviceId);
            if (deviceStatus == null)
            {
                _speechService.Speak("Erro ao verificar estado do dispositivo");
                return false;
            }

            // Determinar ação: se isDesligar é true, deveLigar é false
            // Se isDesligar for true, deveLigar é false (independente de isLigar)
            bool deveLigar = isDesligar ? false : isLigar;
            bool estadoAtual = deviceStatus.IsOn;

            System.Diagnostics.Debug.WriteLine($"[EWELINK] isLigar: {isLigar}, isDesligar: {isDesligar}");
            System.Diagnostics.Debug.WriteLine($"[EWELINK] deveLigar: {deveLigar}, estadoAtual: {estadoAtual}");

            // Se já está no estado desejado, informar
            if (deveLigar && estadoAtual)
            {
                System.Diagnostics.Debug.WriteLine("[EWELINK] Dispositivo já está ligado - não precisa fazer nada");
                var resposta = $"{dispositivoEncontrado.Name}, já estava ligado";
                _speechService.Speak(resposta);
                
                // Enviar resposta via WebSocket com prefixo toApp:
                await _webSocketService.SendRespostaAsync(dispositivoEncontrado.Name ?? "", "", 0, resposta);
                return true;
            }

            if (!deveLigar && !estadoAtual)
            {
                System.Diagnostics.Debug.WriteLine("[EWELINK] Dispositivo já está desligado - não precisa fazer nada");
                var resposta = $"{dispositivoEncontrado.Name}, já estava desligado";
                _speechService.Speak(resposta);
                
                // Enviar resposta via WebSocket com prefixo toApp:
                await _webSocketService.SendRespostaAsync(dispositivoEncontrado.Name ?? "", "", 0, resposta);
                return true;
            }

            // Executar comando
            if (await _apiService.ControlEwelinkDeviceAsync(dispositivoEncontrado.DeviceId, deveLigar))
            {
                var acao = deveLigar ? "ligado" : "desligado";
                var respostasAleatorias = new[]
                {
                    $"{dispositivoEncontrado.Name} {acao}, posso ajudar em algo mais?",
                    $"{dispositivoEncontrado.Name} {acao}, mais alguma coisa?",
                    $"{dispositivoEncontrado.Name} {acao}, está tudo certo!",
                    $"{dispositivoEncontrado.Name} {acao}, precisa de mais alguma coisa?",
                    $"{dispositivoEncontrado.Name} {acao}, pronto!"
                };
                var random = new Random();
                var resposta = respostasAleatorias[random.Next(respostasAleatorias.Length)];
                
                // IMPORTANTE: Respostas Ewelink devem manter acentuação e pontuação originais para fala natural
                // NÃO aplicar NormalizeText nas respostas que serão faladas
                _speechService.Speak(resposta);
                
                // Enviar resposta via WebSocket com prefixo toApp:
                await _webSocketService.SendRespostaAsync(dispositivoEncontrado.Name ?? "", "", 0, resposta);
                return true;
            }
            else
            {
                var resposta = "Erro ao controlar dispositivo Ewelink";
                _speechService.Speak(resposta);
                
                // Enviar resposta de erro via WebSocket com prefixo toApp:
                await _webSocketService.SendRespostaAsync(dispositivoEncontrado.Name ?? "", "", 0, resposta);
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando Ewelink: {ex.Message}");
            LocalDatabase.LogError(_database, ex, "ERR_003", "ao processar comando de dispositivo Ewelink", 
                null, null, null);
            return false;
        }
    }

    private async Task<bool> ProcessStarkSwitchDeviceCommandsAsync(string comando)
    {
        try
        {
            // Buscar dispositivos StarkSwitch
            var dispositivos = await _apiService.GetDevicesAsync();
            if (dispositivos == null || dispositivos.Count == 0)
            {
                return false; // Não há dispositivos
            }

            // Normalizar e corrigir variações de palavras
            var comandoNormalized = _speechService.NormalizeText(comando);
            var comandoCorrigido = _processComandoGeral.CorrectingWordVariationsToAutomation(comandoNormalized);
            var comandoLower = comandoCorrigido.ToLower();
            
            System.Diagnostics.Debug.WriteLine($"[STARKSWITCH] Comando original: {comando}");
            System.Diagnostics.Debug.WriteLine($"[STARKSWITCH] Comando normalizado: {comandoNormalized}");
            System.Diagnostics.Debug.WriteLine($"[STARKSWITCH] Comando corrigido: {comandoCorrigido}");

            // Procurar por comandos do tipo "ligar dispositivo X" ou "desligar dispositivo X"
            bool isLigar = comandoLower.Contains("ligar") || comandoLower.Contains("ligue") || 
                          comandoNormalized.Contains("acende") || comandoNormalized.Contains("acenda") || 
                          comandoNormalized.Contains("acender");
            bool isDesligar = comandoLower.Contains("desligar") || comandoLower.Contains("desligue") ||
                             comandoNormalized.Contains("apaga") || comandoNormalized.Contains("apague") ||
                             comandoNormalized.Contains("apagar");

            // Se ambos forem detectados, priorizar desligar (mais específico)
            if (isLigar && isDesligar)
            {
                isLigar = false;
            }

            if (!isLigar && !isDesligar)
            {
                return false; // Não é comando de ligar/desligar
            }

            // Procurar dispositivo pelo nome no comando
            var comandoParaBusca = _speechService.NormalizeText(comando);
            
            var stopWords = new[] { "ligar", "desligar", "acende", "apaga", "liga", "desliga", 
                                   "acenda", "apague", "acender", "apagar", "ligue", "desligue",
                                   "a", "o", "as", "os", "da", "do", "das", "dos", "de", "em", 
                                   "na", "no", "nas", "nos", "para", "por", "com", "sem" };
            
            var palavrasComando = comandoParaBusca.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !stopWords.Any(sw => p.Equals(sw, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            // Lista de dispositivos encontrados com suas pontuações
            var dispositivosEncontrados = new List<(Device dispositivo, int palavrasEncontradas, bool matchCompleto)>();
            
            foreach (var dispositivo in dispositivos)
            {
                var nomeNormalized = _speechService.NormalizeText(dispositivo.Name);
                
                // Verificar se o nome do dispositivo está no comando (busca completa)
                if (comandoParaBusca.Contains(nomeNormalized) || nomeNormalized.Contains(comandoParaBusca))
                {
                    dispositivosEncontrados.Add((dispositivo, nomeNormalized.Split(' ').Length, true));
                    continue;
                }
                
                // Verificar se palavras-chave do comando estão no nome do dispositivo
                var palavrasNome = nomeNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => !stopWords.Any(sw => p.Equals(sw, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                var palavrasEncontradas = palavrasComando.Count(p => palavrasNome.Any(n => 
                    n.Equals(p, StringComparison.OrdinalIgnoreCase) || 
                    n.Contains(p, StringComparison.OrdinalIgnoreCase) || 
                    p.Contains(n, StringComparison.OrdinalIgnoreCase)));
                
                if (palavrasEncontradas >= 2 || (palavrasComando.Count > 0 && palavrasComando.Count <= 3 && palavrasEncontradas == palavrasComando.Count))
                {
                    dispositivosEncontrados.Add((dispositivo, palavrasEncontradas, false));
                }
            }

            Device? dispositivoEncontrado = null;
            
            if (dispositivosEncontrados.Count == 0)
            {
                return false; // Não encontrou, mas não avisa - fluxo continua
            }
            else if (dispositivosEncontrados.Count == 1)
            {
                dispositivoEncontrado = dispositivosEncontrados[0].dispositivo;
            }
            else
            {
                // Múltiplos dispositivos encontrados - priorizar o com mais palavras
                var dispositivoComMaisPalavras = dispositivosEncontrados
                    .OrderByDescending(d => d.matchCompleto) // Primeiro os matches completos
                    .ThenByDescending(d => d.palavrasEncontradas) // Depois os com mais palavras
                    .First();
                
                dispositivoEncontrado = dispositivoComMaisPalavras.dispositivo;
                System.Diagnostics.Debug.WriteLine($"[STARKSWITCH] Múltiplos dispositivos encontrados, selecionado o com mais palavras: {dispositivoEncontrado.Name}");
            }

            // Determinar ação
            bool deveLigar = isDesligar ? false : isLigar;
            var comandoEnviar = deveLigar ? "ligar" : "desligar";

            // Enviar comando via MQTT
            if (await _apiService.PublishCommandAsync(dispositivoEncontrado.Id, comandoEnviar))
            {
                var acao = deveLigar ? "ligado" : "desligado";
                var respostasAleatorias = new[]
                {
                    $"{dispositivoEncontrado.Name} {acao}, posso ajudar em algo mais?",
                    $"{dispositivoEncontrado.Name} {acao}, mais alguma coisa?",
                    $"{dispositivoEncontrado.Name} {acao}, está tudo certo!",
                    $"{dispositivoEncontrado.Name} {acao}, precisa de mais alguma coisa?",
                    $"{dispositivoEncontrado.Name} {acao}, pronto!"
                };
                var random = new Random();
                var resposta = respostasAleatorias[random.Next(respostasAleatorias.Length)];
                _speechService.Speak(resposta);
                return true;
            }
            else
            {
                _speechService.Speak("Erro ao controlar dispositivo StarkSwitch");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando StarkSwitch: {ex.Message}");
            LocalDatabase.LogError(_database, ex, "ERR_004", "ao processar comando de dispositivo StarkSwitch", 
                null, null, null);
            return false;
        }
    }

    private async Task<bool> ProcessLembreteCommandAsync(string comando, string comandoOriginal)
    {
        // Detectar se é um comando de lembrete
        var variacoesLembrete = new[] { "me lembre", "me lembra", "lembre me", "lembra me", 
            "crie um alerta", "criar alerta", "criar um lembrete", "criar lembrete",
            "me lembrando", "me lembrando de", "me lembrando que", "tem que", "tenho que" };
        
        bool isLembrete = variacoesLembrete.Any(v => comando.Contains(v));
        
        // Verificar se já está aguardando confirmação
        var confirmacaoPendente = _database.GetSetting("LembreteConfirmacaoPendente");
        if (!string.IsNullOrEmpty(confirmacaoPendente) && confirmacaoPendente == "true")
        {
            // Se é um novo comando de lembrete, cancelar a confirmação pendente anterior e processar o novo
            if (isLembrete)
            {
                System.Diagnostics.Debug.WriteLine("[LEMBRETE] Novo comando de lembrete detectado, cancelando confirmação pendente anterior");
                _database.SaveSetting("LembreteConfirmacaoPendente", "");
                _database.SaveSetting("LembreteDados", "");
                // Continuar processando o novo comando abaixo
            }
            else
            {
                // Verificar se é confirmação ou negação
                var confirmacoes = new[] { "sim", "quero", "claro", "pode", "pode me lembrar", 
                    "sim me lembre", "quero sim", "claro me lembre", "pode salvar", "por favor",
                    "sim por favor", "claro sim", "pode ser", "aceito", "ok", "tudo bem",
                    "perfeito", "pode guardar", "salve", "salva", "guarda", "guarde", "beleza" };
                
                var comandoLower = comando.ToLowerInvariant();
                var negacoes = new[] { "não", "nao", "cancelar", "esquece", "esqueça", "não quero", "nao quero" };
                
                if (confirmacoes.Any(c => comandoLower.Contains(c)))
                {
                    // Criar lembrete
                    var lembreteData = _database.GetSetting("LembreteDados");
                    if (!string.IsNullOrEmpty(lembreteData))
                    {
                        var dados = lembreteData.Split('|');
                        if (dados.Length >= 1)
                        {
                            int? diaVal = null;
                            int? mesVal = null;
                            int? horaVal = null;
                            int? minutoVal = null;
                            
                            if (dados.Length > 1 && !string.IsNullOrEmpty(dados[1]) && int.TryParse(dados[1], out var diaTemp))
                                diaVal = diaTemp;
                            if (dados.Length > 2 && !string.IsNullOrEmpty(dados[2]) && int.TryParse(dados[2], out var mesTemp))
                                mesVal = mesTemp;
                            if (dados.Length > 3 && !string.IsNullOrEmpty(dados[3]) && int.TryParse(dados[3], out var horaTemp))
                                horaVal = horaTemp;
                            if (dados.Length > 4 && !string.IsNullOrEmpty(dados[4]) && int.TryParse(dados[4], out var minutoTemp))
                                minutoVal = minutoTemp;
                            
                            // Garantir que o texto do lembrete não comece com "você deve"
                            var textoLembrar = dados[0];
                            if (textoLembrar.StartsWith("você deve ", StringComparison.OrdinalIgnoreCase) || 
                                textoLembrar.StartsWith("voce deve ", StringComparison.OrdinalIgnoreCase))
                            {
                                textoLembrar = textoLembrar.Substring(textoLembrar.IndexOf(" ", StringComparison.OrdinalIgnoreCase) + 1).TrimStart();
                            }
                            
                            var lembrete = new Lembrete
                            {
                                Lembrar = textoLembrar,
                                Dia = diaVal,
                                Mes = mesVal,
                                Hora = horaVal,
                                Minuto = minutoVal,
                                Concluido = false,
                                DataCriacao = DateTime.Now
                            };
                            
                            _database.SaveLembrete(lembrete);
                            _database.SaveSetting("LembreteConfirmacaoPendente", "");
                            _database.SaveSetting("LembreteDados", "");
                            System.Diagnostics.Debug.WriteLine($"[LEMBRETE] Lembrete salvo: {textoLembrar}, Dia: {diaVal}, Mes: {mesVal}, Hora: {horaVal}, Minuto: {minutoVal}");
                            _speechService.Speak("Ok senhor, certamente te lembrarei.");
                            return true;
                        }
                    }
                    // Dados não encontrados, limpar confirmação pendente
                    _database.SaveSetting("LembreteConfirmacaoPendente", "");
                    _database.SaveSetting("LembreteDados", "");
                    return false;
                }
                else if (negacoes.Any(n => comandoLower.Contains(n)))
                {
                    // Cancelar
                    _database.SaveSetting("LembreteConfirmacaoPendente", "");
                    _database.SaveSetting("LembreteDados", "");
                    System.Diagnostics.Debug.WriteLine("[LEMBRETE] Confirmação cancelada pelo usuário");
                    return false;
                }
                else
                {
                    // Não é confirmação nem negação, aguardar confirmação válida
                    System.Diagnostics.Debug.WriteLine($"[LEMBRETE] Aguardando confirmação, comando recebido: {comando}");
                    return false;
                }
            }
        }
        
        // Se não é um comando de lembrete, não processar
        if (!isLembrete)
            return false;

        // Extrair informações do lembrete
        var (dia, mes) = ExtrairDataLembrete(comando);
        var (hora, minuto) = ExtrairHoraLembrete(comando);
        var lembrar = ExtrairAcaoLembrete(comando, dia.HasValue);

        // Se não especificou hora, não definir hora (será usado para lembretes diários contínuos)
        // Não forçar 7h aqui - isso será tratado no ProcessarLembretes

        // Construir mensagem de confirmação com informações extraídas
        string mensagemConfirmacao = "você quer que eu salve na memória que você deve ";
        
        bool isHoje = comando.Contains("hoje") || comando.Contains("agora");
        
        if (dia.HasValue && mes.HasValue && !isHoje)
        {
            var data = new DateTime(DateTime.Now.Year, mes.Value, dia.Value);
            if (data < DateTime.Today)
                data = data.AddYears(1);
            mensagemConfirmacao += lembrar + " no dia " + data.ToString("dd/MM/yyyy");
        }
        else if (isHoje || (dia.HasValue && mes.HasValue && dia == DateTime.Now.Day && mes == DateTime.Now.Month))
        {
            mensagemConfirmacao += lembrar + " hoje";
        }
        else
        {
            mensagemConfirmacao += lembrar;
        }
        
        if (hora.HasValue && minuto.HasValue)
        {
            mensagemConfirmacao += $" às {hora.Value} e {minuto.Value:D2}";
        }
        else if (hora.HasValue)
        {
            mensagemConfirmacao += $" às {hora.Value} horas";
        }
        
        mensagemConfirmacao += ", para que eu te lembre?";

        // Confirmar antes de criar
        System.Diagnostics.Debug.WriteLine($"[LEMBRETE] Perguntando confirmação: {mensagemConfirmacao}");
        System.Diagnostics.Debug.WriteLine($"[LEMBRETE] Dados salvos: {lembrar}|{(dia?.ToString() ?? "")}|{(mes?.ToString() ?? "")}|{(hora?.ToString() ?? "")}|{(minuto?.ToString() ?? "")}");
        _speechService.Speak(mensagemConfirmacao);
        _database.SaveSetting("LembreteConfirmacaoPendente", "true");
        _database.SaveSetting("LembreteDados", $"{lembrar}|{(dia?.ToString() ?? "")}|{(mes?.ToString() ?? "")}|{(hora?.ToString() ?? "")}|{(minuto?.ToString() ?? "")}");
        
        return true;
    }

    private string ExtrairAcaoLembrete(string comando, bool temData)
    {
        var acao = comando;
        
        // Remover comandos de lembrete no início
        var comandosLembrete = new[] { "me lembre", "me lembra", "lembre me", "lembra me", 
            "crie um alerta", "criar alerta", "criar um lembrete", "criar lembrete",
            "me lembrando", "me lembrando de", "me lembrando que" };
        
        foreach (var cmd in comandosLembrete.OrderByDescending(c => c.Length))
        {
            if (acao.StartsWith(cmd, StringComparison.OrdinalIgnoreCase))
            {
                acao = acao.Substring(cmd.Length).TrimStart();
                // Remover "que" ou "de" no início após remover o comando
                if (acao.StartsWith("que ", StringComparison.OrdinalIgnoreCase) || 
                    acao.StartsWith("de ", StringComparison.OrdinalIgnoreCase))
                {
                    acao = acao.Substring(3).TrimStart();
                }
                break;
            }
        }
        
        // Remover palavras relacionadas a tempo/data/hora (mais cuidadosamente)
        var palavrasTempo = new[] { "hoje", "amanha", "amanhã", "agora", "as", "da", "do", "dia", "de",
            "novembro", "dezembro", "janeiro", "fevereiro", "marco", "março", "abril", 
            "maio", "junho", "julho", "agosto", "setembro", "outubro",
            "horas", "hora", "e meia", "e 30", "da tarde", "da manha", "da manhã", "da noite",
            "minutos", "minuto", "min" };
        
        // Remover padrões de hora primeiro (mais específicos)
        acao = System.Text.RegularExpressions.Regex.Replace(acao, @"\s*\d{1,2}\s*(?:horas?|h)(?:\s+e\s+\d{1,2}\s*(?:minutos?|min)?)?", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        acao = System.Text.RegularExpressions.Regex.Replace(acao, @"\s*\d{1,2}\s+e\s+\d{1,2}", " "); // "18 e 30"
        acao = System.Text.RegularExpressions.Regex.Replace(acao, @"\s*\d{1,2}\s+e\s+meia", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Remover números isolados restantes (que não fazem parte do texto)
        acao = System.Text.RegularExpressions.Regex.Replace(acao, @"\s+\d{1,2}\s+", " ");
        
        // Remover palavras de tempo
        foreach (var palavra in palavrasTempo.OrderByDescending(p => p.Length))
        {
            acao = System.Text.RegularExpressions.Regex.Replace(acao, 
                @"\b" + System.Text.RegularExpressions.Regex.Escape(palavra) + @"\b", 
                " ", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        
        // Remover "que tenho que", "que preciso", etc.
        var frasesRemover = new[] { "que tenho que", "que preciso", "que tenho", "que devo", 
            "tem que", "preciso", "tenho que", "devo" };
        
        foreach (var frase in frasesRemover.OrderByDescending(f => f.Length))
        {
            acao = System.Text.RegularExpressions.Regex.Replace(acao, 
                @"\b" + System.Text.RegularExpressions.Regex.Escape(frase) + @"\b", 
                "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        
        // Limpar espaços múltiplos e espaços no início/fim
        acao = System.Text.RegularExpressions.Regex.Replace(acao, @"\s+", " ");
        
        return acao.Trim();
    }

    private (int? dia, int? mes) ExtrairDataLembrete(string comando)
    {
        int? dia = null;
        int? mes = null;

        // Verificar se é "hoje"
        if (comando.Contains("hoje") || comando.Contains("agora"))
        {
            dia = DateTime.Now.Day;
            mes = DateTime.Now.Month;
            return (dia, mes);
        }

        // Verificar se é "amanhã"
        if (comando.Contains("amanha") || comando.Contains("amanhã"))
        {
            var amanha = DateTime.Now.AddDays(1);
            dia = amanha.Day;
            mes = amanha.Month;
            return (dia, mes);
        }

        // Extrair mês primeiro
        var meses = new Dictionary<string, int>
        {
            { "janeiro", 1 }, { "fevereiro", 2 }, { "marco", 3 }, { "março", 3 },
            { "abril", 4 }, { "maio", 5 }, { "junho", 6 }, { "julho", 7 },
            { "agosto", 8 }, { "setembro", 9 }, { "outubro", 10 },
            { "novembro", 11 }, { "dezembro", 12 }
        };

        foreach (var mesNome in meses.Keys)
        {
            if (comando.Contains(mesNome))
            {
                mes = meses[mesNome];
                break;
            }
        }

        // Extrair dia numérico (após o mês ou separadamente)
        var matchDia = System.Text.RegularExpressions.Regex.Match(comando, @"(?:dia\s+)?(\d{1,2})(?!\s*horas?)(?!\s*e\s*\d)");
        if (matchDia.Success)
        {
            if (int.TryParse(matchDia.Groups[1].Value, out var diaNum) && diaNum >= 1 && diaNum <= 31)
                dia = diaNum;
        }

        // Se não encontrou dia mas encontrou mês, tentar outras formas
        if (mes.HasValue && !dia.HasValue)
        {
            // Tentar capturar padrão "dia X de mês"
            var matchDiaDe = System.Text.RegularExpressions.Regex.Match(comando, @"(\d{1,2})\s*(?:de\s+)?(?:janeiro|fevereiro|março|marco|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (matchDiaDe.Success)
            {
                if (int.TryParse(matchDiaDe.Groups[1].Value, out var diaNum))
                    dia = diaNum;
            }
        }

        return (dia, mes);
    }

    private (int? hora, int? minuto) ExtrairHoraLembrete(string comando)
    {
        int? hora = null;
        int? minuto = null;

        // Primeiro tentar capturar padrões como "18 e 30" ou "18 horas e 30 minutos"
        var matchHoraMinuto = System.Text.RegularExpressions.Regex.Match(comando, 
            @"(\d{1,2})\s*(?:horas?|h)?\s*(?:e|\s)\s*(\d{1,2})\s*(?:minutos?|min)?", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (matchHoraMinuto.Success)
        {
            if (int.TryParse(matchHoraMinuto.Groups[1].Value, out var horaNum))
            {
                hora = horaNum;
                if (int.TryParse(matchHoraMinuto.Groups[2].Value, out var minutoNum) && minutoNum < 60)
                {
                    minuto = minutoNum;
                }
                
                // Ajustar para 24h se for "da tarde"
                if (comando.Contains("da tarde") && hora < 12)
                    hora += 12;
                
                return (hora, minuto);
            }
        }

        // Verificar "e meia" ou "e 30"
        if (comando.Contains("e meia") || comando.Contains("e 30"))
        {
            var matchHoraMeia = System.Text.RegularExpressions.Regex.Match(comando, @"(\d{1,2})\s*(?:horas?|h)?\s*e\s*(?:meia|30)");
            if (matchHoraMeia.Success && int.TryParse(matchHoraMeia.Groups[1].Value, out var horaMeia))
            {
                hora = horaMeia;
                minuto = 30;
                
                if (comando.Contains("da tarde") && hora < 12)
                    hora += 12;
                
                return (hora, minuto);
            }
        }

        // Extrair apenas hora
        var matchHora = System.Text.RegularExpressions.Regex.Match(comando, 
            @"(\d{1,2})\s*(?:horas?|h|da\s+tarde|da\s+manha|da\s+manhã|da\s+noite)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (matchHora.Success)
        {
            if (int.TryParse(matchHora.Groups[1].Value, out var horaNum))
            {
                hora = horaNum;
                
                // Ajustar para 24h se for "da tarde"
                if (comando.Contains("da tarde") && hora < 12)
                    hora += 12;
            }
        }

        return (hora, minuto);
    }

    private async Task<bool> ProcessAprendizadoCommandsAsync(string comando)
    {
        var aprendizados = _database.GetAprendizados();
        
        foreach (var aprendizado in aprendizados)
        {
            var cmdNormalized = _speechService.NormalizeText(aprendizado.ComandoUser);
            if (comando.Contains(cmdNormalized))
            {
                // IMPORTANTE: Respostas de aprendizado devem manter acentuação e pontuação originais para fala natural
                // NÃO aplicar NormalizeText nas respostas que serão faladas
                _speechService.Speak(aprendizado.RespostaIa);
                return true;
            }
        }

        return false;
    }

    private async Task<bool> ProcessShellCommandsAsync(string normalized)
    {
        try
        {
            var comandosShell = _database.GetComandosShell();
            
            foreach (var comandoShell in comandosShell)
            {
                var cmdNormalized = _speechService.NormalizeText(comandoShell.ComandoInput);
                if (normalized.Contains(cmdNormalized))
                {
                    // Executar comando CMD
                    try
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {comandoShell.ComandoCMD}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var process = System.Diagnostics.Process.Start(processInfo);
                        if (process != null)
                        {
                            var output = await process.StandardOutput.ReadToEndAsync();
                            var error = await process.StandardError.ReadToEndAsync();
                            await process.WaitForExitAsync();

                            if (!string.IsNullOrEmpty(error))
                            {
                                System.Diagnostics.Debug.WriteLine($"Erro ao executar comando shell: {error}");
                            }

                            System.Diagnostics.Debug.WriteLine($"Comando shell executado: {comandoShell.ComandoCMD}");
                            System.Diagnostics.Debug.WriteLine($"Saída: {output}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao executar comando shell: {ex.Message}");
                        LocalDatabase.LogError(_database, ex, "ERR_005", "Erro ao executar comando shell por voz.", 
                            comandoShell.ComandoInput, null, null);
                    }
                    
                    // IMPORTANTE: Respostas de comandos shell devem manter acentuação e pontuação originais para fala natural
                    // NÃO aplicar NormalizeText nas respostas que serão faladas
                    _speechService.Speak(comandoShell.Resposta);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comandos shell: {ex.Message}");
            LocalDatabase.LogError(_database, ex, "ERR_006", "Erro ao processar comandos shell por voz.", null, null, null);
        }
        
        return false;
    }

    private async Task ProcessIaCommandAsync(string comando)
    {
        try
        {
            var request = new SuperIaRequest
            {
                Texto = comando,
                ContextoUser = "",
                ContextoIA = "",
                Estilo = ""
            };

            var response = await _apiService.CallSuperIaAsync(request);
            if (response != null && !string.IsNullOrEmpty(response.Texto))
            {
                _ultimaRespostaIa = response.Texto;
                // IMPORTANTE: Respostas da IA devem manter acentuação e pontuação originais para fala natural
                // NÃO aplicar NormalizeText nas respostas que serão faladas
                _speechService.Speak(response.Texto);
                
                // Salvar aprendizado se estiver ativado e o comando tiver mais de duas palavras
                if (_aprendizadoEnabled && !string.IsNullOrEmpty(_ultimoComandoUser))
                {
                    var palavrasComando = _ultimoComandoUser.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (palavrasComando.Length > 2)
                    {
                        _database.SaveAprendizado(_ultimoComandoUser, response.Texto);
                    }
                }
                
                // Notificar que comando de IA foi executado (para atualizar StarkCoins)
                IaCommandExecuted?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar comando IA: {ex.Message}");
            LocalDatabase.LogError(_database, ex, "ERR_001", "ao processar comando de IA", 
                _ultimoComandoUser, _ultimaRespostaIa, null);
        }
    }
}

