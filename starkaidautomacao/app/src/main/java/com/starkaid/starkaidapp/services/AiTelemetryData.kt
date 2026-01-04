package com.starkaid.starkaidapp.services

import com.google.gson.annotations.SerializedName

data class AiTelemetryEventDto(
    @SerializedName("userId") val userId: String,
    @SerializedName("userHash") val userHash: String? = null,
    @SerializedName("textoOriginal") val textoOriginal: String,
    @SerializedName("textoNormalizado") val textoNormalizado: String? = null,
    @SerializedName("resultado") val resultado: String,
    @SerializedName("similarityScore") val similarityScore: Double? = null,
    @SerializedName("aprendizadoTipo") val aprendizadoTipo: String? = null,
    @SerializedName("aprendizadoId") val aprendizadoId: String? = null,
    @SerializedName("latenciaMs") val latenciaMs: Int,
    @SerializedName("chamouIaExterna") val chamouIaExterna: Boolean,
    @SerializedName("tokensEstimadosEvitados") val tokensEstimadosEvitados: Int = 0,
    @SerializedName("economiaUSD") val economiaUSD: Double = 0.0,
    @SerializedName("origem") val origem: String = "Android",
    @SerializedName("createdAt") val createdAt: String? = null
)
