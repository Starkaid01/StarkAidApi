package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class UsuarioResponse(
    @SerializedName("id")
    val id: String,
    @SerializedName("name")
    val name: String,
    @SerializedName("email")
    val email: String,
    @SerializedName("starkCoins")
    val starkCoins: Double,
    @SerializedName("createdAt")
    val createdAt: String,
    @SerializedName("isActive")
    val isActive: Boolean
)