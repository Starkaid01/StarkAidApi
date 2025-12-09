package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class ComandoSocial(
    @SerializedName("id") val id: String,
    @SerializedName("userId") val userId: String,
    @SerializedName("comando") val comando: String,
    @SerializedName("resposta") val resposta: String,
    @SerializedName("respostasAleatorias") val respostasAleatorias: String? // JSON string
)