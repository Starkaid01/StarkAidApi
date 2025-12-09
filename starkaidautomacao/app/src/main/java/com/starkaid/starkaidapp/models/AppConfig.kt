package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class AppConfig(
    @SerializedName("apiBaseUrl")
    val apiBaseUrl: String,
    @SerializedName("spotify")
    val spotify: SpotifyConfig?,
    @SerializedName("ewelink")
    val ewelink: EwelinkConfig?
)

data class SpotifyConfig(
    @SerializedName("clientId")
    val clientId: String,
    @SerializedName("clientSecret")
    val clientSecret: String,
    @SerializedName("tokenUrl")
    val tokenUrl: String
)

data class EwelinkConfig(
    @SerializedName("clientId")
    val clientId: String,
    @SerializedName("clientSecret")
    val clientSecret: String,
    @SerializedName("redirectUri")
    val redirectUri: String
)
