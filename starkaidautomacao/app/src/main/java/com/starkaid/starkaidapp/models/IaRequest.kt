package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class IaRequest(
    @SerializedName("texto")
    val texto: String,
    @SerializedName("estilo")
    val estilo: String,
    @SerializedName("contextoUser")
    val contextoUser: String = "",
    @SerializedName("contextoIA")
    val contextoIA: String = "",
    @SerializedName("useStarkCoins")
    val useStarkCoins: Boolean = false, // Indica se o app autorizou uso de StarkCoins
    @SerializedName("skipAi")
    val skipAi: Boolean = false // Indica se o app deseja apenas comandos locais/rotinas
)
