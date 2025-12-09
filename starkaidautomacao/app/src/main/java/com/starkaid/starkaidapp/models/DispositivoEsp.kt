package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class DispositivoEsp(
    val id: String,
    val nome: String,
    val ip: String,
    val porta: Int,
    val comando: String?,
    @SerializedName("comandToEsp")
    val comandToEsp: String?,
    val status: String,
    val ligadoDesligado: Boolean,
    val userId: String? = null,
    val createdAt: String? = null,
    val lastPingAt: String? = null,
    val lastUpdatedAt: String? = null
)

data class CreateDispositivoEspRequest(
    val nome: String,
    val ip: String,
    val porta: Int,
    val comando: String? = null,
    @SerializedName("comandToEsp")
    val comandToEsp: String? = null
)

data class UpdateDispositivoEspRequest(
    val nome: String,
    val ip: String,
    val porta: Int,
    val comando: String? = null,
    @SerializedName("comandToEsp")
    val comandToEsp: String? = null,
    val status: String? = null,
    val ligadoDesligado: Boolean? = null
)

data class EnviarComandoRequest(
    val comando: String
)

data class EnviarComandoResponse(
    val dispositivo: DispositivoInfo,
    val comandoEnviado: String,
    val mensagem: String
)

data class DispositivoInfo(
    val nome: String,
    val ip: String,
    val porta: Int,
    val comando: String?,
    @SerializedName("comandToEsp")
    val comandToEsp: String?
)

data class PingResponse(
    val status: String,
    val isOnline: Boolean
)

