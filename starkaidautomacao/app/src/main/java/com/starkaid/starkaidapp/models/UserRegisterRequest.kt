package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class UserRegisterRequest(
    @SerializedName("name")
    val name: String,

    @SerializedName("email")
    val email: String,

    @SerializedName("password")
    val password: String,

    val origem: String = "app", // 🔥 adiciona a origem

    @SerializedName("estado")
    val estado: String? = null,

    @SerializedName("cidade")
    val cidade: String? = null,

    @SerializedName("bairro")
    val bairro: String? = null
){
    // Função adicional para garantir que o ProGuard não otimize a classe
    fun ensureFields() {
        if (name.isEmpty() || email.isEmpty() || password.isEmpty()) {
            throw IllegalStateException("Empty fields in UserRegisterRequest")
        }
    }
}