package com.starkaid.starkaidapp.services

import android.content.Context
import android.speech.tts.TextToSpeech
import android.util.Log
import java.util.Locale

class VoiceSynthesizer(context: Context) : TextToSpeech.OnInitListener {

    var tts: TextToSpeech? = null
    private var isTtsInitialized = false

    init {
        tts = TextToSpeech(context, this)
    }

    override fun onInit(status: Int) {
        if (status == TextToSpeech.SUCCESS) {
            val result = tts?.setLanguage(Locale("pt", "BR"))
            if (result == TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED) {
                Log.e("TTS", "Linguagem não suportada.")
            } else {
                isTtsInitialized = true
            }
        } else {
            Log.e("TTS", "Falha na inicialização.")
        }
    }

    fun speak(phrase: String?) {
        if (isTtsInitialized) {
            tts?.speak(phrase, TextToSpeech.QUEUE_FLUSH, null, "utteranceId")
        } else {
            Log.e("TTS", "TextToSpeech ainda não inicializado.")
        }
    }

}