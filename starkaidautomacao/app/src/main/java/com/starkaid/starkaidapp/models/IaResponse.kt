package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class IaResultado(
    @SerializedName("texto")
    val texto: String? = null,
    @SerializedName("promptTokens")
    val promptTokens: Int = 0,
    @SerializedName("completionTokens")
    val completionTokens: Int = 0,
    @SerializedName("modelo")
    val modelo: String? = null,
    @SerializedName("hitResult")
    val hitResult: String? = null,
    @SerializedName("similarityScore")
    val similarityScore: Double? = null,
    @SerializedName("aprendizadoTipo")
    val aprendizadoTipo: String? = null,
    @SerializedName("aprendizadoId")
    val aprendizadoId: String? = null
)

data class IaResponse(
    @SerializedName("resultado")
    val resultado: IaResultado? = null,
    @SerializedName("planType")
    val planType: String? = null,
    @SerializedName("tokensConsumidosSemana")
    val tokensConsumidosSemana: Int? = null,
    @SerializedName("tokensSemanaMax")
    val tokensSemanaMax: Int? = null,
    @SerializedName("tokensRestantes")
    val tokensRestantes: Int? = null,
    @SerializedName("starkCoinBalance")
    val starkCoinBalance: Int? = null,
    @SerializedName("adsEnabled")
    val adsEnabled: Boolean? = null,
    @SerializedName("agendamentosMax")
    val agendamentosMax: Int? = null,
    @SerializedName("rate")
    val rate: Int? = null,
    @SerializedName("economy")
    val economy: EconomicPayload? = null,
    @SerializedName("requiredCoins")
    val requiredCoins: Int? = null,
    @SerializedName("novoSaldo")
    val novoSaldo: Double? = null
)