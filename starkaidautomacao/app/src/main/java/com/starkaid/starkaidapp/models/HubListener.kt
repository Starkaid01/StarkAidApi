package com.starkaid.starkaidapp.models

interface HubListener {
    fun onDeviceStatusUpdated(deviceId: String, statusResponse: String)
    fun onDeviceCommandReceived(deviceId: String, command: String)
    fun onSuporteComandoReceived(comando: String)
    fun onOpenUrl(url: String)
    fun onNotificationReceived(titulo: String, mensagem: String)
    fun onAssistantCommandReceived(comando: String)
    fun onLembreteReceived(texto: String, lembreteId: String)
}