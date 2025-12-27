package com.starkaid.starkaidapp.models

import com.google.gson.annotations.SerializedName

data class Device(
    @SerializedName("id") val id: String,
    @SerializedName("deviceId") val deviceId: String,
    @SerializedName("name") val name: String,
    @SerializedName("type") val type: String = "Switch",
    @SerializedName("online") val online: Boolean = false,
    @SerializedName("isOn") var isOn: Boolean = false,
    @SerializedName("familyId") val familyId: String? = null,
    @SerializedName("roomId") val roomId: String? = null,
    @SerializedName("apiKey") val apiKey: String? = null,
    @SerializedName("userId") val userId: String? = null,
    @SerializedName("mqttTopic") val mqttTopic: String? = null,
    @SerializedName("comando") val comando: String? = null,
    var ip: String? = null
)