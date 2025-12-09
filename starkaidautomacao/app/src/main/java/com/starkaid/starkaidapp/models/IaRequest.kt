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
    val contextoIA: String = ""
)
