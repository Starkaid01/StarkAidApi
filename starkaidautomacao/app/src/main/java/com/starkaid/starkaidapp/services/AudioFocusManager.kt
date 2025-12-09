package com.starkaid.starkaidapp.services

import android.content.Context
import android.media.AudioManager
import android.media.AudioFocusRequest
import android.media.AudioAttributes
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.util.Log

class AudioFocusManager(private val context: Context) {
    private val TAG = "AudioFocusManager"
    private val audioManager: AudioManager = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
    private var audioFocusRequest: AudioFocusRequest? = null
    private var hasAudioFocus = false

    private var focusChangeListener: ((Int) -> Unit)? = null

    fun setOnAudioFocusChangeListener(listener: (focusChange: Int) -> Unit) {
        focusChangeListener = listener
    }

    fun requestAudioFocusForVoice() {
        try {
            val focusRequest = AudioFocusRequest.Builder(AudioManager.AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK)
                .setAudioAttributes(
                    AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_VOICE_COMMUNICATION)
                        .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
                        .build()
                )
                .setAcceptsDelayedFocusGain(true)
                .setWillPauseWhenDucked(true)
                .setOnAudioFocusChangeListener { focusChange ->
                    Log.d(TAG, "🔊 Mudança no foco de áudio: $focusChange")
                    focusChangeListener?.invoke(focusChange)

                    when (focusChange) {
                        AudioManager.AUDIOFOCUS_GAIN -> {
                            hasAudioFocus = true
                            Log.d(TAG, "✅ Foco de áudio ganho para voz")
                        }
                        AudioManager.AUDIOFOCUS_LOSS_TRANSIENT -> {
                            hasAudioFocus = false
                            Log.d(TAG, "⚠️ Foco de áudio perdido temporariamente")
                        }
                        AudioManager.AUDIOFOCUS_LOSS -> {
                            hasAudioFocus = false
                            Log.d(TAG, "❌ Foco de áudio perdido permanentemente")
                            abandonAudioFocus()
                        }
                        AudioManager.AUDIOFOCUS_LOSS_TRANSIENT_CAN_DUCK -> {
                            Log.d(TAG, "🎧 Modo fala: reconhecimento deve ficar em escuta passiva")
                            focusChangeListener?.invoke(AudioManager.AUDIOFOCUS_LOSS_TRANSIENT_CAN_DUCK)
                        }
                    }
                }
                .build()

            audioFocusRequest = focusRequest
            val result = audioManager.requestAudioFocus(focusRequest)
            hasAudioFocus = result == AudioManager.AUDIOFOCUS_REQUEST_GRANTED

            if (hasAudioFocus) {
                Log.d(TAG, "🎙️ Foco de áudio obtido com sucesso para reconhecimento de voz")
            } else {
                Log.w(TAG, "🚫 Não foi possível obter foco de áudio para reconhecimento de voz")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao solicitar foco de áudio: ${e.message}")
        }
    }

    fun abandonAudioFocus() {
        try {
            if (hasAudioFocus) {
                Handler(Looper.getMainLooper()).postDelayed({
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                        audioFocusRequest?.let {
                            audioManager.abandonAudioFocusRequest(it)
                        }
                    } else {
                        @Suppress("DEPRECATION")
                        audioManager.abandonAudioFocus(null)
                    }
                    hasAudioFocus = false
                    Log.d(TAG, "🔇 Foco de áudio liberado")
                }, 1000) // Delay de 1 segundo
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao liberar foco de áudio: ${e.message}")
        }
    }
}
