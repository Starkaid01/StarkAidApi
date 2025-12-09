package com.starkaid.starkaidapp.models

interface HubListener {
    fun onDeviceStatusUpdated(deviceId: String, statusResponse: String)
    fun onDeviceCommandReceived(deviceId: String, command: String)
    fun onSuporteComandoReceived(comando: String)
}