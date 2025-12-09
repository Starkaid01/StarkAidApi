package com.starkaid.starkaidapp.services

import com.google.gson.annotations.SerializedName
import retrofit2.Response
import retrofit2.http.*

interface EwelinkApi {
    @GET("api/Ewelink/status")
    suspend fun getStatus(): Response<EwelinkStatusResponse>

    @GET("api/Ewelink/dispositivos")
    suspend fun listarDispositivos(): Response<List<EwelinkDeviceResponse>>

    @GET("api/Ewelink/dispositivos/{deviceId}/status")
    suspend fun getDeviceStatus(
        @Path("deviceId") deviceId: String
    ): Response<EwelinkDeviceResponse>

    @POST("api/Ewelink/dispositivos/{deviceId}/controlar")
    suspend fun controlarDispositivo(
        @Path("deviceId") deviceId: String,
        @Body request: EwelinkControlRequest
    ): Response<EwelinkDeviceResponse>

    @POST("api/Ewelink/sincronizar")
    suspend fun sincronizarDispositivos(): Response<String>
}

data class EwelinkStatusResponse(
    val isLoggedIn: Boolean,
    val account: EwelinkAccountResponse?
)

data class EwelinkAccountResponse(
    val id: String,
    val userId: String,
    val isActive: Boolean
)

data class EwelinkDeviceResponse(
    val id: Int,
    @SerializedName("deviceId")
    val deviceId: String,
    val name: String,
    val online: Boolean,
    val params: Map<String, Any>?,
    val type: String?,
    @SerializedName("isOn")
    val isOn: Boolean = false
)

data class EwelinkControlRequest(
    @SerializedName("Switch")
    val switch: Boolean // true = ligar, false = desligar
)
