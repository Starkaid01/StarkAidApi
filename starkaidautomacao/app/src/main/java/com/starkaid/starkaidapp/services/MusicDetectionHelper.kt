package com.starkaid.starkaidapp.services

import android.content.Context
import android.media.AudioManager
import android.util.Log
import kotlinx.coroutines.*

class MusicDetectionHelper(
    context: Context,
    private val shouldBlockMusicDetection: () -> Boolean,
    private val isTtsSpeaking: () -> Boolean
) {
    private val audioManager = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
    private var detectionJob: Job? = null

    companion object {
        private const val TAG = "MusicDetectionHelper"
    }

    fun isMusicPlaying(): Boolean {
        //  Bloqueia se Ads ativos
        if (shouldBlockMusicDetection()) return false
        //  Bloqueia se TTS ativo
        if (isTtsSpeaking()) {
            Log.d("MusicDetectionHelper", "Ignorando áudio porque é TTS")
            return false
        }
        return audioManager.isMusicActive
    }

    fun registerMusicListener(listener: (Boolean) -> Unit) {
        CoroutineScope(Dispatchers.Main).launch {
            var lastState = false
            var lastChangeTime = 0L
            val debounceMs = 1200L // só troca se o estado se mantiver por >1.2s

            while (isActive) {
                val currentState = isMusicPlaying()
                val now = System.currentTimeMillis()

                if (currentState != lastState && now - lastChangeTime > debounceMs) {
                    listener(currentState)
                    lastState = currentState
                    lastChangeTime = now
                }
                delay(300)
            }
        }
    }

    fun stopListening() {
        detectionJob?.cancel()
        detectionJob = null
        Log.d(TAG, "🛑 Monitoramento de música cancelado")
    }
}
