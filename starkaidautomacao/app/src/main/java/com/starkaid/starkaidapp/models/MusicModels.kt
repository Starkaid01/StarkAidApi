package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class MusicResolveRequest(
    val text: String
)

data class MusicResolveResponse(
    val type: String,
    val source: String? = null,
    val tts: String,
    val station: MusicStation? = null,
    @SerializedName("youTubeVideoId")
    val externalId: String? = null,
    val title: String? = null,
    val confidence: Double? = null
)

data class MusicStation(
    val name: String,
    val streamUrl: String,
    val tags: String? = null,
    val country: String? = null,
    val bitrate: Int? = null
)

data class ExternalAudioStreamResult(
    val streamUrl: String,
    val expiresAt: String? = null
)
