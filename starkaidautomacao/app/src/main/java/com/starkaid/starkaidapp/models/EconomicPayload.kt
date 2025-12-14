package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class EconomicPayload(
    @SerializedName("planType") val planType: String = "Free",
    @SerializedName("starkCoinBalance") val starkCoinBalance: Int = 0,
    @SerializedName("tokensConsumidosSemana") val tokensConsumidosSemana: Int = 0,
    @SerializedName("tokensSemanaMax") val tokensSemanaMax: Int = 0,
    @SerializedName("tokensRestantes") val tokensRestantes: Int = 0,
    @SerializedName("adsEnabled") val adsEnabled: Boolean = true,
    @SerializedName("agendamentosMax") val agendamentosMax: Int = 0,
    @SerializedName("agendamentosRestantes") val agendamentosRestantes: Int = 0,
    @SerializedName("rate") val rate: Int = 100
) {
    fun balance(): Int = starkCoinBalance
}

