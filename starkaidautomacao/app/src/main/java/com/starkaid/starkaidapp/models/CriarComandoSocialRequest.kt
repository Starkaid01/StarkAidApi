package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class CriarComandoSocialRequest(
    @SerializedName("comando") val comando: String,
    @SerializedName("resposta") val resposta: String
)