package com.starkaid.starkaidapp.models

data class DisparoResponse(
    val id: String,
    val dispositivoId: String,
    val dispositivoNome: String,
    val disparadoEm: String,
    val mensagem: String,
    val confirmado: Boolean,
    val confirmadoEm: String?
)