package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class UsuarioResponse(
    @SerializedName("id") val id: String,
    @SerializedName("name") val name: String,
    @SerializedName("email") val email: String,
    @SerializedName("apiKey") val apiKey: String? = null,
    @SerializedName("role") val role: String? = null,
    @SerializedName("removalAds") val removalAds: String? = null,
    @SerializedName("estado") val estado: String? = null,
    @SerializedName("cidade") val cidade: String? = null,
    @SerializedName("bairro") val bairro: String? = null,
    @SerializedName("isActive") val isActive: Boolean = true,
    @SerializedName("createdAt") val createdAt: String? = null,
    @SerializedName("economy") val economy: EconomicPayload? = null
) {
    fun balance(): Int = economy?.starkCoinBalance ?: 0
}