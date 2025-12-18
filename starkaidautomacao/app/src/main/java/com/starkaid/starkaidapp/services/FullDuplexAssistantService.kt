package com.starkaid.starkaidapp.services

import android.Manifest
import android.annotation.SuppressLint
import android.app.*
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.media.AudioAttributes
import android.media.AudioManager
import android.media.AudioPlaybackConfiguration
import android.media.AudioRecord
import android.media.MediaRecorder
import android.os.Build
import android.os.Bundle
import android.speech.RecognitionListener
import android.speech.RecognizerIntent
import android.speech.SpeechRecognizer
import android.speech.tts.TextToSpeech
import android.speech.tts.UtteranceProgressListener
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.content.ContextCompat
import androidx.localbroadcastmanager.content.LocalBroadcastManager

import kotlinx.coroutines.*

import java.util.*
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicLong
import kotlin.math.abs
import kotlin.math.sqrt

class FullDuplexAssistantAdvancedService : Service(), TextToSpeech.OnInitListener {

    companion object {
        private const val TAG = "FullDuplexAssistantAdv"
        private const val NOTIF_CHANNEL_ID = "starkaid_voice_channel"
        private const val NOTIF_ID = 8245



        @SuppressLint("ObsoleteSdkInt")
        fun start(ctx: Context) {
            val i = Intent(ctx, FullDuplexAssistantAdvancedService::class.java)
            if (Build.VERSION.SDK_INT >= 26) ctx.startForegroundService(i) else ctx.startService(i)
        }

        fun stop(ctx: Context) {
            ctx.stopService(Intent(ctx, FullDuplexAssistantAdvancedService::class.java))
        }

        const val ACTION_START_LISTENING = "START_LISTENING"
        const val ACTION_STOP_LISTENING = "STOP_LISTENING"
        const val ACTION_STOP_SPEAKING = "STOP_SPEAKING"
        const val EXTRA_RECOGNIZED_TEXT = "recognized_text"
        const val BROADCAST_SPEECH_RESULT = "com.starkaid.SPEECH_RESULT"
        const val BROADCAST_TTS_STARTED = "com.starkaid.TTS_STARTED"
        const val BROADCAST_TTS_STOPPED = "com.starkaid.TTS_STOPPED"
        const val BROADCAST_TTS_AUDIO_LEVEL = "com.starkaid.TTS_AUDIO_LEVEL"
        const val EXTRA_AUDIO_LEVEL = "audio_level"

        var isAdShowing  = AtomicBoolean(false)

        var lastSpeak = ""
    }

    private lateinit var audioManager: AudioManager
    private var tts: TextToSpeech? = null
    private var isSpeaking = AtomicBoolean(false)

    private var ultimaFalaAssistente = ""

    private var sr: SpeechRecognizer? = null
    private var srIntent: Intent? = null
    private val isListeningGoogle = AtomicBoolean(false)
    private val commandScope = CoroutineScope(Dispatchers.Main)
    private var forceStopped = AtomicBoolean(true)
    private var isRestarting = AtomicBoolean(false)
    private val isExternalPlay = AtomicBoolean(false)

    override fun onBind(intent: Intent?) = null
    private lateinit var musicHelper: MusicDetectionHelper
    private var audioMonitorJob: Job? = null
    private val currentAudioLevel = AtomicLong(0)
    private var audioRecord: AudioRecord? = null
    private var estimatedSpeechEndTime = AtomicLong(0)
    private var silenceStartTime = AtomicLong(0)
    private var speechEndValidationJob: Job? = null


    override fun onCreate() {
        super.onCreate()

        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO)
            != PackageManager.PERMISSION_GRANTED
        ) {
            Log.e(TAG, "Permissão RECORD_AUDIO não concedida")
            stopSelf()
            return
        }


        audioManager = getSystemService(Context.AUDIO_SERVICE) as AudioManager
        setupNotification()
        setupAudio()
        setupTTS()


        //startListeningMonitor()

        musicHelper = MusicDetectionHelper(
            this,
            shouldBlockMusicDetection = { isAdShowing.get() },
            isTtsSpeaking = { isSpeaking.get() }
        )

        musicHelper.registerMusicListener { isPlaying ->
            Log.d(TAG, "Música externa ativa?: $isPlaying | TTS falando?: ${isSpeaking.get()}")

            if (isSpeaking.get()) {
                Log.d(TAG, "Ignorando detecção de música porque é TTS")
                return@registerMusicListener
            }

            if (isPlaying) {
                if (!isExternalPlay.get()) {
                    isExternalPlay.set(true)
                    Log.d(TAG, "🎵 Reprodução EXTERNA detectada — pausando reconhecimento")
                    if (!isSpeaking.get()){
                        // Para o SR sem reiniciar automaticamente
                        sr?.cancel()
                        isListeningGoogle.set(false)
                    }

                }
            } else {
                if (isExternalPlay.get()) {
                    isExternalPlay.set(false)
                    Log.d(TAG, "🎵 Reprodução externa parada — retomando reconhecimento")
                    if (!isSpeaking.get() && !forceStopped.get()){
                        commandScope.launch {
                            delay(1500) // dá tempo de a mídia liberar o áudio
                            tryStartListeningSafe("MIDIA_EXTERNA_PAROU")
                        }
                    }


                }
            }
        }
    }



    override fun onDestroy() {
        super.onDestroy()
        speechEndValidationJob?.cancel()
        stopAudioMonitoring()
        teardownSpeechRecognizer()
        tts?.stop()
        tts?.shutdown()
        commandScope.cancel()
    }

    private fun broadcastSpeechResult(text: String) {
        val intent = Intent(BROADCAST_SPEECH_RESULT).apply {
            putExtra(EXTRA_RECOGNIZED_TEXT, text)
        }
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
    }

    @SuppressLint("ObsoleteSdkInt")
    private fun setupNotification() {
        if (Build.VERSION.SDK_INT >= 26) {
            val chan = NotificationChannel(
                NOTIF_CHANNEL_ID,
                "StarkAid Assistente de Voz",
                NotificationManager.IMPORTANCE_LOW
            )
            val nm = getSystemService(NotificationManager::class.java)
            nm.createNotificationChannel(chan)
        }

        val notif = NotificationCompat.Builder(this, NOTIF_CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_btn_speak_now)
            .setContentTitle("Assistente StarkAid")
            .setContentText("Ouvindo e falando")
            .setOngoing(true)
            .build()

        startForeground(NOTIF_ID, notif)
    }

    @Suppress("DEPRECATION")
    private fun setupAudio() {
        // 🔧 modo comunicação melhora captação e reduz atraso
        audioManager.mode = AudioManager.MODE_IN_COMMUNICATION
        audioManager.isSpeakerphoneOn = true
    }

    private fun setupTTS() {
        tts = TextToSpeech(this, this)
    }

    override fun onInit(status: Int) {
        if (status == TextToSpeech.SUCCESS) {
            tts?.language = Locale("pt", "BR")
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                tts?.setAudioAttributes(
                    AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_MEDIA)
                        .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
                        .build()
                )
            }
            Log.i(TAG, "TTS inicializado")
            setupSpeechRecognizer()

            commandScope.launch {
                delay(500)
                if (!forceStopped.get()) startGoogleListening()
            }
        }
    }

    /**
     * Calcula o tempo estimado de fala baseado no número de palavras.
     * Foca especialmente na última palavra para determinar quando realmente termina.
     * Velocidade média do TTS em português: ~150 palavras/minuto = ~400ms por palavra
     */
    private fun calculateEstimatedSpeechDuration(text: String): Long {
        // Contar palavras (palavras são separadas por espaços)
        val words = text.trim().split(Regex("\\s+")).filter { it.isNotBlank() }
        val wordCount = words.size
        
        if (wordCount == 0) return 1000L
        
        // Tempo base: ~400ms por palavra
        val baseTimeMs = wordCount * 400L
        
        // Para textos muito curtos (< 5 palavras), usar tempo mínimo de 1 segundo
        val minTime = if (wordCount < 5) 1000L else baseTimeMs
        
        // Adicionar pequena margem de segurança (20% ao invés de 40%)
        // O callback onDone do TTS é confiável, então não precisamos de margem muito grande
        val withMargin = (minTime * 1.2).toLong()
        
        // Limite máximo de 30 segundos para evitar travamento
        return withMargin.coerceAtMost(30000L)
    }
    
    /**
     * Extrai a última palavra de uma frase para uso em validação
     */
    private fun getLastWord(text: String): String {
        val words = text.trim().split(Regex("\\s+")).filter { it.isNotBlank() }
        return if (words.isNotEmpty()) {
            words.last().lowercase().replace(Regex("[^a-z0-9]"), "")
        } else {
            ""
        }
    }

    fun speak(text: String) {
        val t = tts ?: return
        val uttId = "utt-${System.currentTimeMillis()}"
        isSpeaking.set(true)
        ultimaFalaAssistente = text
        lastSpeak = text
        
        // Calcular tempo estimado de fala e última palavra
        val estimatedDuration = calculateEstimatedSpeechDuration(text)
        val speechStartTime = System.currentTimeMillis()
        estimatedSpeechEndTime.set(speechStartTime + estimatedDuration)
        silenceStartTime.set(0)
        val lastWord = getLastWord(text)
        
        Log.d(TAG, "🎙️ Iniciando fala - Texto: ${text.take(50)}..., Duração estimada: ${estimatedDuration}ms, Última palavra: '$lastWord'")

        // NÃO reiniciar reconhecimento aqui - será reiniciado após TTS terminar completamente

        t.setOnUtteranceProgressListener(object : UtteranceProgressListener() {
            override fun onStart(utteranceId: String?) {
                isSpeaking.set(true)
                estimatedSpeechEndTime.set(System.currentTimeMillis() + estimatedDuration)
                silenceStartTime.set(0)
                
                // NÃO PAUSAR reconhecimento - manter sempre ativo para capturar comandos de parar
                // A lógica de não processar comandos será feita no handleCommand
                Log.d(TAG, "🎙️ TTS iniciado - reconhecimento continua ativo")
                
                // Broadcast para MainActivity iniciar animações
                val intent = Intent(BROADCAST_TTS_STARTED)
                LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService).sendBroadcast(intent)
                // Iniciar monitoramento de áudio em tempo real
                startAudioMonitoring()
            }

            override fun onDone(utteranceId: String?) {
                Log.d(TAG, "✅ TTS onDone chamado - callback confiável, parando imediatamente")
                
                // CANCELAR validação anterior se houver
                speechEndValidationJob?.cancel()
                
                // O callback onDone do TTS é confiável e indica que terminou de falar
                // Usar apenas um delay mínimo para garantir que o áudio realmente saiu do alto-falante
                speechEndValidationJob = commandScope.launch(Dispatchers.Default) {
                    // Delay muito pequeno (150ms) para garantir que o último som saiu
                    delay(150)
                    forceStopSpeakingImmediate()
                }
            }

                override fun onError(utteranceId: String?) {
                Log.w(TAG, "❌ TTS erro: $utteranceId")
                
                // Cancelar validação em andamento
                speechEndValidationJob?.cancel()
                
                // Parar monitoramento de áudio
                stopAudioMonitoring()
                
                // Forçar parada imediata em caso de erro
                isSpeaking.set(false)
                estimatedSpeechEndTime.set(0)
                silenceStartTime.set(0)
                
                // Broadcast para MainActivity parar animações em caso de erro
                val intent = Intent(BROADCAST_TTS_STOPPED)
                LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService).sendBroadcast(intent)

                // Reiniciar reconhecimento rapidamente
                commandScope.launch {
                    delay(100)
                    if (!forceStopped.get() && !isListeningGoogle.get()) {
                        tryStartListeningSafe("TTS.onError")
                    }
                }
            }
        })

        if (Build.VERSION.SDK_INT >= 21)
            t.speak(text, TextToSpeech.QUEUE_FLUSH, null, uttId)
        else
            @Suppress("DEPRECATION") t.speak(text, TextToSpeech.QUEUE_FLUSH, null)
    }

    private fun tryStartListeningSafe(origin: String) {
        if (isExternalPlay.get()) {
            Log.d(TAG, "🎧 Ignorado startGoogleListening (mídia externa ativa) [origem=$origin]")
            return
        }

        if (forceStopped.get()) {
            Log.d(TAG, "🚫 Ignorado startGoogleListening (forceStopped=true) [origem=$origin]")
            return
        }

        // Não verificar isSpeaking aqui - reconhecimento deve estar sempre ativo
        // A lógica de não processar comandos durante TTS está no handleCommand
        startGoogleListening()
    }

    private suspend fun safeDelayedRestart() {
        if (forceStopped.get() || isRestarting.get()) return

        isRestarting.set(true)
        try {
            if (!forceStopped.get()) startGoogleListening()
        } finally {
            isRestarting.set(false)
        }
    }

    fun stopSpeak() {
        tts?.stop()
        
        // Cancelar validação em andamento
        speechEndValidationJob?.cancel()
        
        // Parar monitoramento de áudio
        stopAudioMonitoring()
        
        // Forçar parada imediata
        isSpeaking.set(false)
        estimatedSpeechEndTime.set(0)
        silenceStartTime.set(0)
        
        Log.d(TAG, "🛑 TTS interrompido manualmente")
        
        // Broadcast para MainActivity parar animações
        val intent = Intent(BROADCAST_TTS_STOPPED)
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)

        // Reiniciar reconhecimento rapidamente
        commandScope.launch {
            delay(100)
            if (!forceStopped.get() && !isListeningGoogle.get()) {
                tryStartListeningSafe("stopSpeak")
            }
        }
    }

    // Monitorar áudio do sistema em tempo real enquanto TTS está falando
    @SuppressLint("MissingPermission")
    private fun startAudioMonitoring() {
        stopAudioMonitoring() // Garantir que não há monitoramento anterior
        
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) 
            != PackageManager.PERMISSION_GRANTED) {
            Log.w(TAG, "Sem permissão RECORD_AUDIO para monitorar áudio")
            return
        }

        audioMonitorJob = commandScope.launch(Dispatchers.IO) {
            try {
                // Usar AudioRecord para monitorar o áudio do sistema
                val sampleRate = 44100
                val channelConfig = android.media.AudioFormat.CHANNEL_IN_MONO
                val audioFormat = android.media.AudioFormat.ENCODING_PCM_16BIT
                val bufferSize = AudioRecord.getMinBufferSize(sampleRate, channelConfig, audioFormat)
                
                if (bufferSize == AudioRecord.ERROR_BAD_VALUE || bufferSize == AudioRecord.ERROR) {
                    Log.e(TAG, "Erro ao obter buffer size para monitoramento de áudio")
                    // Fallback: usar AudioManager para estimativa
                    useAudioManagerLevel()
                    return@launch
                }

                // Tentar REMOTE_SUBMIX primeiro (captura áudio do sistema), senão VOICE_RECOGNITION
                val audioSource = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                    try {
                        MediaRecorder.AudioSource.REMOTE_SUBMIX
                    } catch (e: Exception) {
                        MediaRecorder.AudioSource.VOICE_RECOGNITION
                    }
                } else {
                    MediaRecorder.AudioSource.VOICE_RECOGNITION
                }
                
                val recorder = AudioRecord(
                    audioSource,
                    sampleRate,
                    channelConfig,
                    audioFormat,
                    bufferSize * 2
                )

                if (recorder.state != AudioRecord.STATE_INITIALIZED) {
                    Log.e(TAG, "AudioRecord não inicializado")
                    useAudioManagerLevel()
                    return@launch
                }

                audioRecord = recorder
                recorder.startRecording()
                
                val buffer = ShortArray(bufferSize)
                
                while (coroutineContext.isActive && isSpeaking.get()) {
                    val read = recorder.read(buffer, 0, buffer.size)
                    if (read > 0) {
                        // Calcular RMS (Root Mean Square) do áudio com melhor precisão
                        var sum = 0.0
                        var maxSample = 0.0
                        for (i in 0 until read) {
                            val sample = abs(buffer[i].toDouble())
                            sum += sample * sample
                            if (sample > maxSample) maxSample = sample
                        }
                        val rms = sqrt(sum / read)
                        
                        // Usar tanto RMS quanto peak para melhor detecção
                        val peakLevel = (maxSample / Short.MAX_VALUE) * 100
                        val rmsLevel = (rms / Short.MAX_VALUE) * 100
                        
                        // Combinar RMS e peak com peso maior no peak para resposta mais rápida
                        val normalizedLevel = ((rmsLevel * 0.4 + peakLevel * 0.6) * 1.5).toInt().coerceIn(0, 100)
                        
                        // Garantir nível mínimo quando há áudio
                        val finalLevel = if (normalizedLevel < 10 && rms > 100) 15 else normalizedLevel
                        
                        // Log periódico para debug (a cada 1 segundo)
                        if (System.currentTimeMillis() % 1000 < 30) {
                            Log.d(TAG, "🎵 Áudio detectado - RMS: $rms, Peak: $maxSample, Nível: $finalLevel")
                        }
                        
                        currentAudioLevel.set(finalLevel.toLong())
                        
                        // Broadcast do nível de áudio com maior frequência
                        val intent = Intent(BROADCAST_TTS_AUDIO_LEVEL).apply {
                            putExtra(EXTRA_AUDIO_LEVEL, finalLevel)
                        }
                        LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService)
                            .sendBroadcast(intent)
                    }
                    delay(30) // ~33 atualizações por segundo para resposta mais rápida
                }
                
                recorder.stop()
                recorder.release()
                audioRecord = null
            } catch (e: Exception) {
                Log.e(TAG, "Erro ao monitorar áudio: ${e.message}")
                useAudioManagerLevel()
            }
        }
    }

    // Fallback: usar AudioManager quando AudioRecord não está disponível
    private fun useAudioManagerLevel() {
        audioMonitorJob?.cancel()
        audioMonitorJob = commandScope.launch {
            var baseTime = System.currentTimeMillis()
            while (coroutineContext.isActive && isSpeaking.get()) {
                try {
                    // Simular variação mais realista baseada em padrões de fala
                    val time = (System.currentTimeMillis() - baseTime) / 50.0
                    
                    // Múltiplas frequências para simular padrões de fala natural
                    val wave1 = kotlin.math.sin(time * 0.15) * 30
                    val wave2 = kotlin.math.sin(time * 0.25) * 20
                    val wave3 = kotlin.math.sin(time * 0.08) * 15
                    
                    // Adicionar variações aleatórias ocasionais (picos de fala)
                    val randomSpike = if (kotlin.random.Random.nextFloat() < 0.1f) {
                        kotlin.random.Random.nextFloat() * 40f
                    } else 0f
                    
                    val simulatedLevel = (40 + wave1 + wave2 + wave3 + randomSpike).toInt().coerceIn(0, 100)
                    currentAudioLevel.set(simulatedLevel.toLong())
                    
                    val intent = Intent(BROADCAST_TTS_AUDIO_LEVEL).apply {
                        putExtra(EXTRA_AUDIO_LEVEL, simulatedLevel)
                    }
                    LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService)
                        .sendBroadcast(intent)
                } catch (e: Exception) {
                    Log.e(TAG, "Erro no fallback de monitoramento: ${e.message}")
                }
                delay(30) // Mesma frequência do monitoramento real
            }
        }
    }

    /**
     * Para o TTS imediatamente após callback onDone (mais confiável e rápido)
     */
    private suspend fun forceStopSpeakingImmediate() {
        withContext(Dispatchers.Main) {
            // Parar monitoramento de áudio
            stopAudioMonitoring()
            
            // Marcar como não falando IMEDIATAMENTE
            isSpeaking.set(false)
            
            // Resetar contadores
            estimatedSpeechEndTime.set(0)
            silenceStartTime.set(0)
            
            // Broadcast para MainActivity parar animações
            val intent = Intent(BROADCAST_TTS_STOPPED)
            LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService).sendBroadcast(intent)
            Log.d(TAG, "🛑 TTS marcado como parado IMEDIATAMENTE após onDone")
            
            // Reiniciar reconhecimento IMEDIATAMENTE (sem delay grande)
            commandScope.launch {
                delay(100) // Delay mínimo apenas para estabilizar
                if (!isListeningGoogle.get() && !forceStopped.get()) {
                    tryStartListeningSafe("TTS.onDoneImmediate")
                }
            }
        }
    }
    

    private fun stopAudioMonitoring() {
        audioMonitorJob?.cancel()
        audioMonitorJob = null
        try {
            audioRecord?.stop()
            audioRecord?.release()
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao parar monitoramento: ${e.message}")
        }
        audioRecord = null
        currentAudioLevel.set(0)
    }

    private val lastSrErrorTime = AtomicLong(0)
    private fun setupSpeechRecognizer() {
        if (!SpeechRecognizer.isRecognitionAvailable(this)) {
            Log.e(TAG, "Reconhecimento Google indisponível")
            return
        }

        sr = SpeechRecognizer.createSpeechRecognizer(this).apply {
            setRecognitionListener(object : RecognitionListener {
                override fun onReadyForSpeech(params: Bundle?) {
                    Log.d(TAG, "🎤 Pronto para ouvir")
                }

                override fun onBeginningOfSpeech() {
                    Log.d(TAG, "🎤 Início da fala detectado")
                }

                override fun onEndOfSpeech() {
                    Log.d(TAG, "🎤 Fim da fala detectado")
                }

                override fun onError(error: Int) {
                    val now = System.currentTimeMillis()
                    if (now - lastSrErrorTime.get() < 500) return // ignora se <500ms desde último erro
                    lastSrErrorTime.set(now)

                    Log.w(TAG, "❌ Google SR erro: $error")
                    isListeningGoogle.set(false)
                    
                    // Reiniciar reconhecimento rapidamente (reconhecimento contínuo)
                    // Não verificar isSpeaking aqui - deixar sempre ativo
                    commandScope.launch {
                        delay(300)
                        if (!forceStopped.get()) tryStartListeningSafe("SR.onError")
                    }
                }

                override fun onResults(results: Bundle) {
                    // Processar resultado
                    results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                        ?.firstOrNull()?.let { handleCommand(it, false) }
                    
                    // Reiniciar reconhecimento imediatamente para reconhecimento contínuo
                    isListeningGoogle.set(false)
                    tryStartListeningSafe("SR.onResults")
                }

                override fun onPartialResults(partialResults: Bundle) {
                    partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                        ?.firstOrNull()?.let { text ->
                            val cleanText = text.lowercase().trim()

                            // SEMPRE verificar comandos de parar primeiro (mesmo durante TTS)
                            if (isStopSpeakingCommand(cleanText)) {
                                Log.d(TAG, "🛑 Comando de parar detectado (parcial): '$cleanText'")
                                stopSpeak()
                                return@let
                            }
                            
                            // Outros comandos parciais: processar normalmente (handleCommand filtra se TTS está falando)
                            handleCommand(text, true)
                        }
                }

                override fun onRmsChanged(rmsdB: Float) {}
                override fun onBufferReceived(buffer: ByteArray?) {}
                override fun onEvent(eventType: Int, params: Bundle?) {}
            })
        }

        srIntent = Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH).apply {
            putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM)
            putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true)
            putExtra(RecognizerIntent.EXTRA_LANGUAGE, "pt-BR")
            putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 3)

            // ⬇️ Delays de silêncio otimizados para reconhecimento contínuo e responsivo
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, 700L) // Silêncio mínimo para considerar fim
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, 600L) // Possível fim mais cedo
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, 300L) // Duração mínima da fala (mais rápido)
        }
    }

    private fun startGoogleListening() {
        if (forceStopped.get()) {
            Log.d(TAG, "🚫 Ignorado startGoogleListening (forceStopped=true)")
            return
        }
        
        // Se já está ouvindo, não precisa reiniciar
        if (isListeningGoogle.get()) {
            Log.d(TAG, "✅ Google SR já está ouvindo")
            return
        }
        
        try {
            sr?.startListening(srIntent)
            isListeningGoogle.set(true)
            Log.d(TAG, "✅ Google SR iniciado - reconhecimento contínuo ativo")
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao iniciar SR: ${e.message}")
            isListeningGoogle.set(false)
            // Tentar reiniciar após um delay em caso de erro
            commandScope.launch {
                delay(1000)
                if (!forceStopped.get() && !isListeningGoogle.get()) {
                    tryStartListeningSafe("SR.errorRecovery")
                }
            }
        }
    }

    private fun teardownSpeechRecognizer() {
        try {
            sr?.cancel()
            sr?.destroy()
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao destruir SR: ${e.message}")
        }
        sr = null
        isListeningGoogle.set(false)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_START_LISTENING -> {
                forceStopped.set(false)
                commandScope.launch {
                    delay(200)
                    startGoogleListening()
                }
            }
            ACTION_STOP_LISTENING -> {
                forceStopped.set(true)
                isExternalPlay.set(false)
                sr?.cancel()
                isListeningGoogle.set(false)
                Log.i(TAG, "🛑 Reconhecimento parado manualmente (forceStopped=true)")
            }
            ACTION_STOP_SPEAKING -> {
                Log.d(TAG, "🛑 Recebido comando para parar TTS")
                stopSpeak()
            }
            else -> {
                val text = intent?.getStringExtra("text") ?: return START_STICKY
                speak(text)
            }
        }
        return START_STICKY
    }

    /**
     * Verifica se é um comando de parar de falar
     * Versão melhorada e mais permissiva para detectar comandos de parar
     */
    private fun isStopSpeakingCommand(text: String): Boolean {
        val normalized = text.lowercase()
            .replace(Regex("[^a-z0-9\\s]"), "") // Remove pontuação
            .trim()
        
        // Lista expandida de comandos de parar
        val stopCommands = listOf(
            "para de falar", "parar de falar", "pare de falar",
            "cala a boca", "cale a boca", "calar a boca",
            "cala boca", "cale boca", "calar boca",
            "cale-se", "cala-se", "calar-se",
            "fica quieto", "fique quieto", "ficar quieto",
            "fica calado", "fique calado", "ficar calado",
            "silencio", "silêncio",
            "para com isso", "pare com isso", "parar com isso",
            "chega de falar", "basta de falar",
            "calece", "cala se", "cale se"
        )
        
        // Verificar correspondência exata ou parcial
        val hasExactMatch = stopCommands.any { normalized.contains(it) }
        
        // Verificar combinações de palavras (mais permissivo)
        val hasParaFalar = (normalized.contains("para") || normalized.contains("pare") || normalized.contains("parar")) &&
                          (normalized.contains("falar") || normalized.contains("falando"))
        
        val hasCalaBoca = (normalized.contains("cala") || normalized.contains("cale") || normalized.contains("calar")) &&
                         normalized.contains("boca")
        
        val hasQuieto = normalized.contains("quieto") || normalized.contains("calado") || normalized.contains("silencio")
        
        return hasExactMatch || hasParaFalar || hasCalaBoca || hasQuieto
    }

    /**
     * Calcula similaridade entre dois textos usando algoritmo de Levenshtein simplificado
     * Retorna um valor entre 0.0 (totalmente diferente) e 1.0 (idênticos)
     */
    private fun calculateSimilarity(text1: String, text2: String): Double {
        if (text1 == text2) return 1.0
        if (text1.isEmpty() || text2.isEmpty()) return 0.0
        
        // Normalizar textos: remover pontuação e converter para minúsculas
        val normalized1 = text1.lowercase().replace(Regex("[^a-z0-9\\s]"), "").trim()
        val normalized2 = text2.lowercase().replace(Regex("[^a-z0-9\\s]"), "").trim()
        
        // Verificar se um texto contém o outro (substring)
        if (normalized1.contains(normalized2) || normalized2.contains(normalized1)) {
            val longer = maxOf(normalized1.length, normalized2.length)
            val shorter = minOf(normalized1.length, normalized2.length)
            return (shorter.toDouble() / longer) * 0.9 // 90% se for substring
        }
        
        // Calcular palavras em comum
        val words1 = normalized1.split(Regex("\\s+")).filter { it.isNotBlank() }.toSet()
        val words2 = normalized2.split(Regex("\\s+")).filter { it.isNotBlank() }.toSet()
        
        if (words1.isEmpty() || words2.isEmpty()) return 0.0
        
        val commonWords = words1.intersect(words2)
        val totalWords = words1.union(words2).size
        
        // Similaridade baseada em palavras comuns
        val wordSimilarity = (commonWords.size * 2.0) / totalWords
        
        // Verificar sequências de palavras consecutivas (mais importante)
        val words1List = normalized1.split(Regex("\\s+")).filter { it.isNotBlank() }
        val words2List = normalized2.split(Regex("\\s+")).filter { it.isNotBlank() }
        
        var longestSequence = 0
        for (i in words1List.indices) {
            for (j in words2List.indices) {
                var seq = 0
                var k = 0
                while (i + k < words1List.size && j + k < words2List.size && 
                       words1List[i + k] == words2List[j + k]) {
                    seq++
                    k++
                }
                if (seq > longestSequence) longestSequence = seq
            }
        }
        
        // Similaridade baseada em sequências (peso maior)
        val sequenceSimilarity = if (longestSequence > 0) {
            (longestSequence * 2.0) / (words1List.size + words2List.size)
        } else {
            0.0
        }
        
        // Combinar similaridades (sequências têm peso maior)
        return (sequenceSimilarity * 0.6 + wordSimilarity * 0.4).coerceIn(0.0, 1.0)
    }

    private fun handleCommand(text: String?, partial: Boolean = false) {
        if (text.isNullOrBlank()) return
        val cleanText = text.lowercase().trim()
        
        // PRIMEIRO: Verificar se é comando de parar (sempre processar, mesmo durante TTS)
        if (isStopSpeakingCommand(cleanText)) {
            Log.d(TAG, "🛑 Comando de parar detectado: '$cleanText'")
            stopSpeak()
            return
        }
        
        // SEGUNDO: Se TTS está falando, IGNORAR todos os outros comandos
        if (isSpeaking.get()) {
            Log.d(TAG, "🚫 Ignorando comando durante TTS (não é parar): '$cleanText'")
            return
        }
        
        // TERCEIRO: Verificar se não é a própria fala do assistente sendo reconhecida
        lastSpeak = ultimaFalaAssistente.lowercase().trim()
        
        if (lastSpeak.isNotEmpty()) {
            // Se for exatamente igual, ignorar
            if (cleanText == lastSpeak) {
                Log.d(TAG, "🚫 Ignorando comando idêntico à última fala do assistente")
                return
            }
            
            // Calcular similaridade (threshold mais baixo para ser mais permissivo)
            val similarity = calculateSimilarity(cleanText, lastSpeak)
            
            // Se similaridade for muito alta (>80%), provavelmente é o próprio TTS
            if (similarity > 0.8) {
                Log.d(TAG, "🚫 Ignorando comando muito similar à última fala - Similaridade: ${(similarity * 100).toInt()}%")
                Log.d(TAG, "   Última fala: '$lastSpeak'")
                Log.d(TAG, "   Comando: '$cleanText'")
                return
            }
        }

        // Processar comando normalmente
        val resultText = if (partial) "parcial:$cleanText" else cleanText
        Log.d(TAG, "✅ Processando comando: '$cleanText'")
        broadcastSpeechResult(resultText)
    }
}
