package com.starkaid.starkaidapp.models

data class MusicaResponse(
    val autorizado: Boolean,
    val saldoAtual: Double,
    val message: String? = null
)