package com.starkaid.starkaidapp.models

data class MusicResolveRequest(
    val text: String
)

data class MusicResolveResponse(
    val type: String,
    val source: String? = null,
    val tts: String,
    val station: MusicStation? = null,
    val youtubeVideoId: String? = null,
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
