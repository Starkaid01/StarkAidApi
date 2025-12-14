package com.starkaid.starkaidapp.services

import android.Manifest
import android.annotation.SuppressLint
import android.content.Context
import android.content.Intent
import android.media.AudioFormat
import android.media.AudioRecord
import android.media.MediaRecorder
import android.media.audiofx.AcousticEchoCanceler
import android.media.audiofx.NoiseSuppressor
import android.util.Log
import androidx.annotation.RequiresPermission
import androidx.localbroadcastmanager.content.LocalBroadcastManager
//import com.starkaid.starkaidapp.services.FullDuplexAssistantAdvancedService.Companion.pediuMusica
import com.google.gson.Gson
import com.starkaid.starkaidapp.models.EconomicPayload
import kotlinx.coroutines.*
import okhttp3.*
import okio.ByteString
import org.json.JSONObject
import java.io.ByteArrayOutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.util.concurrent.atomic.AtomicBoolean
import kotlin.math.sqrt

class TranscribeWebSocketService(
    private val context: Context,
    private val wsUrl: String,
    private val apiKey: String,
    private val languageCode: String,
    private val onTranscriptReceived: (String) -> Unit,
    private val onError: (String) -> Unit,
    private val onEconomyUpdate: (EconomicPayload?) -> Unit = {}
) {
    private val TAG = "TranscribeWS"
    private val client = OkHttpClient.Builder().build()
    private val gson = Gson()
    private var webSocket: WebSocket? = null
    private val isRecording = AtomicBoolean(false)
    private var audioJob: Job? = null
    private val isRecorderStopped = AtomicBoolean(true) // nova flag

    private val SAMPLE_RATE = 16000
    private val CHANNEL_CONFIG = AudioFormat.CHANNEL_IN_MONO
    private val AUDIO_FORMAT = AudioFormat.ENCODING_PCM_16BIT
    private val BUFFER_SIZE = AudioRecord.getMinBufferSize(
        SAMPLE_RATE, CHANNEL_CONFIG, AUDIO_FORMAT
    ).coerceAtLeast(3200)
    private val AUDIO_SOURCE = MediaRecorder.AudioSource.VOICE_RECOGNITION
    private val CHUNK_SIZE = 1600
    private val forceStopped = AtomicBoolean(false)
    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    private var recorder: AudioRecord? = null


    fun start() {
        forceStopped.set(false)
        val intent = Intent("starkaid.UPDATE_RECOG")
        intent.putExtra("source", "Rec: STARKAID-PRO")
        LocalBroadcastManager.getInstance(context).sendBroadcast(intent)

        if (webSocket == null) {
            connectWebSocket()
            Log.i(TAG, "Nova sessão iniciada")
        } else {
            try {
                webSocket?.send("START")
                Log.i(TAG, "Sessão existente reiniciada (START enviado)")
            } catch (e: Exception) {
                Log.e(TAG, "Erro enviando START: ${e.message}")
            }
        }
    }

    fun stop() {
        if (!isRecording.get() && webSocket == null) return

        forceStopped.set(true)
        Log.i(TAG, "Stopping transcription service...")

        // 1. Para gravação imediatamente
        isRecording.set(false)
        audioJob?.cancel()
        audioJob = null
        stopAndReleaseRecorder()

        // 2. Fecha WebSocket gracefulmente
        webSocket?.let { ws ->
            try {
                if (ws.queueSize() < 1000) { // Só envia BYE se não está congestionado
                    ws.send("BYE")
                }
                ws.close(1000, "User stop")
            } catch (e: Exception) {
                Log.w(TAG, "WebSocket close warning: ${e.message}")
            }
        }

        webSocket = null

        // 3. Limpa recursos
        scope.coroutineContext.cancelChildren()
        Log.i(TAG, "Transcription service stopped")
    }

    private fun stopAndReleaseRecorder() {
        if (isRecorderStopped.compareAndSet(false, true)) {
            try {
                recorder?.let {
                    if (it.recordingState == AudioRecord.RECORDSTATE_RECORDING) {
                        it.stop()
                    }
                    it.release()
                }
            } catch (e: Exception) {
                Log.e(TAG, "Erro ao liberar AudioRecord: ${e.message}")
            } finally {
                recorder = null
                Log.i(TAG, "AudioRecord liberado")
            }
        }
    }

    fun restart() {
        forceStopped.set(false)
        webSocket?.let { ws ->
            try {
                ws.send("START")
                Log.i(TAG, "RECONECT enviado")
            } catch (e: Exception) {
                Log.e(TAG, "Erro enviando RECONECT: ${e.message}")
            }
        }
    }

    fun pause() {
        forceStopped.set(false)
        webSocket?.let { ws ->
            try {
                ws.send("STOP")
                Log.i(TAG, "STOP enviado")
            } catch (e: Exception) {
                Log.e(TAG, "Erro enviando STOP: ${e.message}")
            }
        }
    }

    private fun connectWebSocket() {
        //Log.d("FullDuplexAssistantAdv", "connectando WebSocket: pediuMusica = ${pediuMusica.get()}")
        Log.d(TAG, "Conectando WebSocket...")

        if (!forceStopped.get()) {
            val request = Request.Builder()
                .url("$wsUrl?apiKey=$apiKey&language=$languageCode")
                .build()

            webSocket = client.newWebSocket(request, object : WebSocketListener() {
                @SuppressLint("MissingPermission")
                override fun onOpen(ws: WebSocket, response: Response) {
                    Log.i(TAG, "WebSocket connected")
                    //Log.d("FullDuplexAssistantAdv", "WebSocket connected: pediuMusica = ${pediuMusica.get()}")
                    startRecording()
                }

                override fun onMessage(ws: WebSocket, text: String) {
                    Log.d(TAG, "Received: $text")
                    val handled = tryHandleJsonMessage(text)
                    if (!handled) {
                        when {
                            text.startsWith("[PARCIAL]") -> {
                                val parcial = text.removePrefix("[PARCIAL]").trim()
                                onTranscriptReceived("[PARCIAL] $parcial")
                            }
                            text.startsWith("[FINAL]") -> {
                                val finalText = text.removePrefix("[FINAL]").trim()
                                onTranscriptReceived("[FINAL] $finalText")
                            }
                        }
                    }
                }

                override fun onMessage(ws: WebSocket, bytes: ByteString) {
                    // só recebemos texto
                }

                override fun onClosing(ws: WebSocket, code: Int, reason: String) {
                    val intent = Intent("starkaid.UPDATE_RECOG")
                    intent.putExtra("source", "google")
                    LocalBroadcastManager.getInstance(context).sendBroadcast(intent)
                    Log.i(TAG, "WebSocket closing: $code / $reason")
                }

                override fun onFailure(ws: WebSocket, t: Throwable, response: Response?) {
                    Log.e(TAG, "WebSocket failed: ${t.message}")
                    onError(t.message ?: "WebSocket failure")
                    reconnectWithDelay()
                }
            })
        }
    }

    private fun reconnectWithDelay() {
        scope.launch {
            delay(3000)
            connectWebSocket()
        }
    }

    fun isMicReleased(): Boolean {
        return recorder == null || recorder?.state != AudioRecord.STATE_INITIALIZED
    }

    @RequiresPermission(Manifest.permission.RECORD_AUDIO)
    private fun startRecording() {
        if (isRecording.get()) return
        isRecording.set(true)
        isRecorderStopped.set(false)

        audioJob = scope.launch {
            try {
                recorder = AudioRecord(
                    AUDIO_SOURCE,
                    SAMPLE_RATE,
                    CHANNEL_CONFIG,
                    AUDIO_FORMAT,
                    BUFFER_SIZE
                )

                val sessionId = recorder?.audioSessionId ?: -1
                if (NoiseSuppressor.isAvailable()) NoiseSuppressor.create(sessionId).enabled = true
                if (AcousticEchoCanceler.isAvailable()) AcousticEchoCanceler.create(sessionId).enabled = true

                val buffer = ShortArray(BUFFER_SIZE / 2)
                val audioBuffer = ByteArrayOutputStream()
                recorder?.startRecording()
                Log.i(TAG, "Recording started with buffer: $BUFFER_SIZE")

                var silentChunks = 0
                val maxSilentChunks = 10

                while (isRecording.get() && isActive) {
                    val read = recorder?.read(buffer, 0, buffer.size) ?: 0
                    if (read > 0) {
                        val pcmBytes = shortArrayToByteArray(buffer, read)
                        audioBuffer.write(pcmBytes)

                        if (bufferHasSound(buffer, read, 500)) {
                            silentChunks = 0
                            if (audioBuffer.size() >= CHUNK_SIZE) {
                                val chunk = audioBuffer.toByteArray().copyOfRange(0, CHUNK_SIZE)
                                webSocket?.send(ByteString.of(*chunk))
                                val remaining = audioBuffer.toByteArray().copyOfRange(CHUNK_SIZE, audioBuffer.size())
                                audioBuffer.reset()
                                audioBuffer.write(remaining)
                            }
                        } else {
                            silentChunks++
                            if (silentChunks >= maxSilentChunks && audioBuffer.size() > 0) {
                                webSocket?.send(ByteString.of(*audioBuffer.toByteArray()))
                                audioBuffer.reset()
                                silentChunks = 0
                            }
                        }
                    }
                    delay(20L)
                }

                if (audioBuffer.size() > 0) {
                    webSocket?.send(ByteString.of(*audioBuffer.toByteArray()))
                }
            } catch (e: Exception) {
                Log.e(TAG, "Recording error: ${e.message}")
            } finally {
                stopAndReleaseRecorder() // segura chamada dupla
                Log.i(TAG, "Recording cleaned up")
            }
        }
    }

    // ✅ CORREÇÃO: Função convertendo ShortArray para ByteArray (LITTLE ENDIAN)
    private fun shortArrayToByteArray(shorts: ShortArray, length: Int): ByteArray {
        val bytes = ByteArray(length * 2)
        for (i in 0 until length) {
            bytes[i * 2] = (shorts[i].toInt() and 0xFF).toByte()
            bytes[i * 2 + 1] = ((shorts[i].toInt() ushr 8) and 0xFF).toByte()
        }
        return bytes
    }

    // ✅ Alternativa mais simples usando ByteBuffer
    private fun shortArrayToByteArrayAlternative(shorts: ShortArray, length: Int): ByteArray {
        val byteBuffer = ByteBuffer.allocate(length * 2)
        byteBuffer.order(ByteOrder.LITTLE_ENDIAN)
        for (i in 0 until length) {
            byteBuffer.putShort(shorts[i])
        }
        return byteBuffer.array()
    }

    private fun bufferHasSound(buffer: ShortArray, read: Int, threshold: Int = 300): Boolean {
        if (read <= 0) return false
        var sum = 0.0
        for (i in 0 until read) sum += buffer[i] * buffer[i]
        val rms = Math.sqrt(sum / read.toDouble())
        return rms > threshold
    }

    private fun calculateRMS(buffer: ShortArray, read: Int): Double {
        var sum = 0.0
        for (i in 0 until read) {
            val sample = buffer[i].toDouble() / Short.MAX_VALUE
            sum += sample * sample
        }
        return sqrt(sum / read)
    }

    private fun tryHandleJsonMessage(text: String): Boolean {
        return try {
            val obj = JSONObject(text)
            val message = obj.optString("message", "")
            val transcript = obj.optString("transcript", null)
            val isPartial = obj.optBoolean("isPartial", false)
            val economyObj = obj.optJSONObject("economy")
            val economy = economyObj?.let {
                EconomicPayload(
                    planType = it.optString("planType", null),
                    starkCoinBalance = it.optInt("StarkCoinBalance", it.optInt("starkCoinBalance", 0)),
                    tokensConsumidosSemana = it.optInt("tokensConsumidosSemana", 0),
                    tokensSemanaMax = it.optInt("tokensSemanaMax", 0),
                    tokensRestantes = it.optInt("tokensRestantes", 0),
                    adsEnabled = it.optBoolean("adsEnabled", true),
                    agendamentosMax = it.optInt("agendamentosMax", 0),
                    agendamentosRestantes = it.optInt("agendamentosRestantes", 0),
                    rate = it.optInt("rate", 100)
                )
            }

            onEconomyUpdate(economy)

            if (message.isNotEmpty()) {
                onTranscriptReceived(message)
            }
            if (!transcript.isNullOrEmpty()) {
                val prefix = if (isPartial) "[PARCIAL]" else "[FINAL]"
                onTranscriptReceived("$prefix $transcript")
            }
            true
        } catch (_: Exception) {
            false
        }
    }
}
