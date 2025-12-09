package com.starkaid.starkaidapp.models

data class Device(
    val id: String,
    val name: String,
    val mqttTopic: String,
    val comando: String?,
    val resposta: String?,
    var ip: String?,
    var isOn: Boolean = false // Novo campo para controlar o estado local
)