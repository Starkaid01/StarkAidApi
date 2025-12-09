package com.starkaid.starkaidapp.services

import android.content.Context

class SpeechService(private val context: Context) {
    private var engine: SpeechRecognizerEngine? = null

    fun useLocal() {
        //
    }

    fun useOnline() {
        engine = GoogleSpeechRecognizer(context)
    }

    fun start(callback: (String) -> Unit) {
        engine?.startListening(callback)
    }

    fun stop() {
        engine?.stopListening()
    }
}