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

    fun speak(text: String) {
        val t = tts ?: return
        val uttId = "utt-${System.currentTimeMillis()}"
        isSpeaking.set(true)
        ultimaFalaAssistente = text

        commandScope.launch {
            delay(200) // tempo para SR se estabilizar
            tryStartListeningSafe("TTS.onDone") // só reinicia se não estiver ouvindo
        }

        t.setOnUtteranceProgressListener(object : UtteranceProgressListener() {
            override fun onStart(utteranceId: String?) {
                isSpeaking.set(true)
                // Broadcast para MainActivity iniciar animações
                val intent = Intent(BROADCAST_TTS_STARTED)
                LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService).sendBroadcast(intent)
                // Iniciar monitoramento de áudio em tempo real
                startAudioMonitoring()
            }

            override fun onDone(utteranceId: String?) {
                // Parar monitoramento de áudio
                stopAudioMonitoring()
                commandScope.launch {
                    delay(100) // tempo para SR se estabilizar//
                    tryStartListeningSafe("TTS.onDone") // só reinicia se não estiver ouvindo

                    delay(400)
                    isSpeaking.set(false)
                    // Broadcast para MainActivity parar animações
                    val intent = Intent(BROADCAST_TTS_STOPPED)
                    LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService).sendBroadcast(intent)
                }

            }

            override fun onError(utteranceId: String?) {
                Log.w(TAG, "❌ Google SR erro: $utteranceId")
                isListeningGoogle.set(false) // marca como parado
                isSpeaking.set(false)
                // Parar monitoramento de áudio
                stopAudioMonitoring()
                // Broadcast para MainActivity parar animações em caso de erro
                val intent = Intent(BROADCAST_TTS_STOPPED)
                LocalBroadcastManager.getInstance(this@FullDuplexAssistantAdvancedService).sendBroadcast(intent)

                commandScope.launch {
                    delay(200)
                    if (!forceStopped.get()) safeDelayedRestart()
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
        isSpeaking.set(false)
        Log.d(TAG, "🛑 TTS interrompido manualmente")
        // Parar monitoramento de áudio
        stopAudioMonitoring()
        // Broadcast para MainActivity parar animações
        val intent = Intent(BROADCAST_TTS_STOPPED)
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)

        commandScope.launch {
            delay(300)
            if (!forceStopped.get()) safeDelayedRestart()
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
                    commandScope.launch {
                        delay(300)
                        if (!isSpeaking.get()) tryStartListeningSafe("SR.onError")
                    }
                }

                override fun onResults(results: Bundle) {
                    isListeningGoogle.set(false) // já terminou de ouvir
                    results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                        ?.firstOrNull()?.let { handleCommand(it, false) }
                    tryStartListeningSafe("SR.onResults")
                }

                override fun onPartialResults(partialResults: Bundle) {
                    partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                        ?.firstOrNull()?.let { text ->
                            val cleanText = text.lowercase()

                            // Comandos para parar TTS - expanda a lista
                            val stopCommands = listOf(
                                "para de falar", "parar de falar", "pare de falar",
                                "cala a boca", "cale a boca", "cala boca", "cale boca",
                                "calar a boca", "calar boca", "cale-se", "cala-se",
                                "silencio", "silêncio", "fique quieto", "fica quieto",
                                "ficar quieto", "para com isso", "para com essa",
                                "chega de falar", "basta de falar", "pare com isso"
                            )

                            // SEMPRE processa comandos de parar, mesmo durante TTS
                            if (stopCommands.any { cleanText.contains(it) }) {
                                Log.d(TAG, "🛑 Comando de parar detectado: $cleanText")
                                stopSpeak()
                                // Opcional: confirmar que parou
                                // speak("Ok, parei de falar")
                            } else if (!isSpeaking.get()) {
                                // Outros comandos só quando não está falando
                                handleCommand(text, true)
                            }
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

            // ⬇️ Delays de silêncio reduzidos para acelerar a resposta
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, 900L) // antes 2000
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, 800L) // antes 1500
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, 500L) // antes 800
        }
    }

    private fun startGoogleListening() {
        if (forceStopped.get() || isListeningGoogle.get()) return
        try {
            sr?.startListening(srIntent)
            isListeningGoogle.set(true)
            Log.d(TAG, "✅ Google SR iniciado")
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao iniciar SR: ${e.message}")
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

    private fun handleCommand(text: String?, partial: Boolean = false) {
        if (text.isNullOrBlank()) return
        val cleanText = text.lowercase().trim()
        val lastSpeak = ultimaFalaAssistente.lowercase().trim()

        var naoPodeContinuar = false

        val lislastSpeak = lastSpeak.lowercase().split(" ")
        val listWordcleanText = cleanText.split(" ")
        var worCount = 0
        for (word in lislastSpeak) {
            if (word != " " && !word.isEmpty() && listWordcleanText.contains(word)) {
                worCount++
                if (worCount >= 7) {
                    naoPodeContinuar = true
                    break
                }
            }
        }
        if (naoPodeContinuar)
            return


        if (lastSpeak.lowercase() == cleanText){
            Log.d("TestandoIA", "reconheceu propria fala")
            return
        }


        val stopCmds = listOf(
            "ficar quieto", "fique quieto", "calece", "ficar calado", "fique calado",
            "parar de falar", "para de falar", "pare de falar", "fica quieto", "silencio",
            "cale-se", "fica calado", "fique quieto", "calar a boca", "cala a boca",
            "cala boca", "cale a boca", "cale boca"
        )

        if (isSpeaking.get() && stopCmds.any { cleanText.contains(it) }) {
            Log.d(TAG, "🛑 Parar fala detectado")
            stopSpeak()
            return
        }

        val resultText =
            if (partial) "parcial:$cleanText" else cleanText


        val resultTextFinal = if (isSpeaking.get()) "speaking:$resultText" else resultText

        Log.d("TestandoIA", "antes de enviar a Mainactivity: $resultTextFinal")

        broadcastSpeechResult(resultTextFinal)
    }
}
