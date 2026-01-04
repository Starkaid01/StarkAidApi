package com.starkaid.starkaidapp.services

import com.google.gson.annotations.SerializedName

data class TelemetryEventDto(
    @SerializedName("userId") val userId: String,
    @SerializedName("origem") val origem: String,
    @SerializedName("evento") val evento: String,
    @SerializedName("categoria") val categoria: String,
    @SerializedName("metadataJson") val metadataJson: String? = null,
    @SerializedName("criadoEm") val criadoEm: String? = null // API gera se núllo
)
