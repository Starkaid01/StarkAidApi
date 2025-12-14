package com.starkaid.starkaidapp.models

data class IaResultado(
    val texto: String? = null,
    val promptTokens: Int = 0,
    val completionTokens: Int = 0,
    val modelo: String? = null
)

data class IaResponse(
    val resultado: IaResultado? = null,
    val planType: String? = null,
    val tokensConsumidosSemana: Int? = null,
    val tokensSemanaMax: Int? = null,
    val tokensRestantes: Int? = null,
    val starkCoinBalance: Int? = null,
    val adsEnabled: Boolean? = null,
    val agendamentosMax: Int? = null,
    val rate: Int? = null,
    val economy: EconomicPayload? = null,
    val requiredCoins: Int? = null,
    val novoSaldo: Double? = null
)