package com.starkaid.starkaidapp.ewelink.models

import org.json.JSONObject

data class EwelinkTokens(
    val accessToken: String,
    val refreshToken: String,
    val atExpiredTime: Long,  // Access Token Expiry
    val rtExpiredTime: Long,  // Refresh Token Expiry
    val region: String
)

data class EwelinkDevice(
    val id: String,
    val name: String,
    val type: Int,
    val uiid: Int,
    val params: JSONObject,
    val online: Boolean,
    val familyId: String,
    val roomId: String
)

data class EwelinkFamily(
    val id: String,
    val name: String,
    val rooms: List<EwelinkRoom>
)

data class EwelinkRoom(
    val id: String,
    val name: String
)