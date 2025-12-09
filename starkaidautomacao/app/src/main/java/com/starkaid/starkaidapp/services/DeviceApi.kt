package com.starkaid.starkaidapp.services

import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.*

interface DeviceApi {
    @GET("api/Devices")
    suspend fun getDevices(): Response<List<DeviceResponse>>

    @PUT("api/Devices/{deviceId}")
    suspend fun renameDevice(
        @Path("deviceId") deviceId: String,
        @Body request: RenameRequest
    ): Response<ResponseBody>

    @DELETE("api/Devices/{deviceId}")
    suspend fun deleteDevice(
        @Path("deviceId") deviceId: String
    ): Response<ResponseBody>

    @POST("api/Devices")
    suspend fun addDevice(
        @Body request: AddDeviceRequest
    ): Response<DeviceResponse>

    @POST("api/Devices/pair")
    suspend fun pairDevice(
        @Body request: PairDeviceRequest,
        @Header("apiKey") apiKey: String
    ): Response<DeviceResponse>
}

data class DeviceResponse(
    val id: String,
    val name: String,
    val mqttTopic: String,
    val ip: String?,
    val comando: String?,
    val resposta: String?
)

data class AddDeviceRequest(
    val name: String,
    val comandoDevice: String?,
    val userId: String
)

data class RenameRequest(
    val newName: String,
    val newComando: String?
)

data class PairDeviceRequest(
    val name: String
)