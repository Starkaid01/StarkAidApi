package com.starkaid.starkaidapp.models

data class IaResponse(
    val texto: String,
    val promptTokens: Int,
    val completionTokens: Int,
    val modelo: String,
    val novoSaldo: Double? = null
)