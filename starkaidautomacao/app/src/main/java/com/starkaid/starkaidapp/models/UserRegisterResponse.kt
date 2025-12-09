package com.starkaid.starkaidapp.models

data class UserRegisterResponse(
    val id: String,
    val name: String,
    val email: String,
    val apiKey: String,
    val starkCoins: Int,
    val createdAt: String,
    val token: String
)