package com.starkaid.starkaidapp.services

import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.*

interface DeviceApi {
    @GET("api/v1/Devices")
    suspend fun getDevices(): Response<List<DeviceResponse>>

    @PUT("api/v1/Devices/{deviceId}")
    suspend fun renameDevice(
        @Path("deviceId") deviceId: String,
        @Body request: RenameRequest
    ): Response<ResponseBody>

    @DELETE("api/v1/Devices/{deviceId}")
    suspend fun deleteDevice(
        @Path("deviceId") deviceId: String
    ): Response<ResponseBody>

    @POST("api/v1/Devices")
    suspend fun addDevice(
        @Body request: AddDeviceRequest
    ): Response<DeviceResponse>

    @POST("api/v1/Devices/pair")
    suspend fun pairDevice(
        @Body request: PairDeviceRequest,
        @Header("apiKey") apiKey: String
    ): Response<DeviceResponse>
}

data class DeviceResponse(
    val id: String,
    val deviceId: String? = null,
    val name: String,
    val type: String? = null,
    val online: Boolean = false,
    val isOn: Boolean = false,
    val familyId: String? = null,
    val roomId: String? = null,
    val apiKey: String?,
    val userId: String?,
    val mqttTopic: String?,
    val comando: String?
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