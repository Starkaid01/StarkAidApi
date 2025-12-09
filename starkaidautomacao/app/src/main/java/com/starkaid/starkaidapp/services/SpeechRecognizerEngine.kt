package com.starkaid.starkaidapp.services

interface SpeechRecognizerEngine {
    fun startListening(callback: (String) -> Unit)
    fun stopListening()
}