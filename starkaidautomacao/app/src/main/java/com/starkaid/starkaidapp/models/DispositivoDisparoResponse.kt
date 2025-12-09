@file:Suppress("unused", "unused")

package com.starkaid.starkaidapp.models

@Suppress("unused", "unused", "unused", "unused")
data class DispositivoDisparoResponse(
    val id: String,
    val userId: String,
    val nome: String,
    val mqttTopic: String,
    val statusTopic: String,
    val dataCadastro: String
)
