package com.starkaid.starkaidapp.models

data class SpotifyTokens(
    val accessToken: String,
    val refreshToken: String?,
    val expiresIn: Int
)