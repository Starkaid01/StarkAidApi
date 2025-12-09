@file:Suppress("DEPRECATION")

package com.starkaid.starkaidapp.services

import android.content.Context
import android.speech.tts.TextToSpeech
import android.speech.tts.UtteranceProgressListener
import android.util.Log
import java.util.Locale
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

class BlockingVoiceSynthesizer(context: Context) : TextToSpeech.OnInitListener {

    private var tts: TextToSpeech? = null
    private var isInitialized = false
    private var currentLatch: CountDownLatch? = null

    init {
        tts = TextToSpeech(context, this)
    }

    override fun onInit(status: Int) {
        if (status == TextToSpeech.SUCCESS) {
            val result = tts?.setLanguage(Locale("pt", "BR"))
            if (result == TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED) {
                Log.e("BlockingTTS", "Linguagem não suportada.")
            } else {
                isInitialized = true
                // Configurar listener para detectar quando a fala termina
                tts?.setOnUtteranceProgressListener(object : UtteranceProgressListener() {
                    override fun onStart(utteranceId: String?) {
                        Log.d("BlockingTTS", "Fala iniciada: $utteranceId")
                    }

                    override fun onDone(utteranceId: String?) {
                        Log.d("BlockingTTS", "Fala concluída: $utteranceId")
                        currentLatch?.countDown()
                    }

                    override fun onError(utteranceId: String?) {
                        Log.e("BlockingTTS", "Erro na fala: $utteranceId")
                        currentLatch?.countDown()
                    }
                })
            }
        } else {
            Log.e("BlockingTTS", "Falha na inicialização.")
        }
    }

    fun speakAwait(phrase: String, timeoutSeconds: Long = 10) {
        if (!isInitialized) {
            Log.e("BlockingTTS", "TextToSpeech não inicializado.")
            return
        }

        // Criar um latch para aguardar a conclusão da fala
        currentLatch = CountDownLatch(1)
        val utteranceId = "blocking_utterance_${System.currentTimeMillis()}"

        // Falar a frase
        tts?.speak(phrase, TextToSpeech.QUEUE_FLUSH, null, utteranceId)

        try {
            // Aguardar até que a fala seja concluída (com timeout)
            currentLatch?.await(timeoutSeconds, TimeUnit.SECONDS)
        } catch (e: InterruptedException) {
            Log.e("BlockingTTS", "Interrompido enquanto aguardava a fala: ${e.message}")
        } finally {
            currentLatch = null
        }
    }

    fun stop() {
        tts?.stop()
        currentLatch?.countDown() // Liberar qualquer await pendente
        currentLatch = null
    }

    fun shutdown() {
        stop()
        tts?.shutdown()
    }
}
