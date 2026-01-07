package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.DispositivoEsp
import retrofit2.Response
import retrofit2.http.*

data class CreateComodoRequest(
    val nome: String
)

data class UpdateComodoRequest(
    val nome: String
)

data class AssociateDeviceRequest(
    val dispositivoId: String,
    val tipo: String, // Device, Esp, Ewelink
    val papel: String?
)

data class ComodoDto(
    val id: String,
    val nome: String,
    val dispositivos: List<ComodoDispositivoDto>
)

data class ComodoDispositivoDto(
    val dispositivoId: String,
    val tipo: String,
    val nomeDispositivo: String,
    val papel: String?,
    val isOn: Boolean
)

data class ComandoAmbienteResult(
    val sucesso: Boolean,
    val mensagemVoz: String,
    val requerConfirmacao: Boolean,
    val dispositivosAcionados: List<String>? // Guids as strings
)

data class DeviceSelectionDto(
    val dispositivoId: String,
    val tipo: String,
    val name: String
)

interface ComodosApi {
    @GET("api/v1/Comodos/devices/available")
    suspend fun getAvailableDevices(): Response<List<DeviceSelectionDto>>

    @GET("api/v1/Comodos")
    suspend fun getAll(): Response<List<ComodoDto>>

    @GET("api/v1/Comodos/{id}")
    suspend fun getById(@Path("id") id: String): Response<ComodoDto>

    @POST("api/v1/Comodos")
    suspend fun create(@Body request: CreateComodoRequest): Response<ComodoDto>

    @PUT("api/v1/Comodos/{id}")
    suspend fun update(@Path("id") id: String, @Body request: UpdateComodoRequest): Response<ComodoDto>

    @DELETE("api/v1/Comodos/{id}")
    suspend fun delete(@Path("id") id: String): Response<Unit>

    @POST("api/v1/Comodos/{id}/dispositivos")
    suspend fun addDevice(@Path("id") id: String, @Body request: AssociateDeviceRequest): Response<Unit>

    @DELETE("api/v1/Comodos/{id}/dispositivos/{dispositivoId}")
    suspend fun removeDevice(@Path("id") id: String, @Path("dispositivoId") dispositivoId: String): Response<Unit>

    @POST("api/v1/Comodos/resolver-dispositivo")
    suspend fun resolverDispositivo(
        @Query("tipo") tipo: String,
        @Query("comando") comando: String?,
        @Query("comodoConfirmado") comodoConfirmado: String?
    ): Response<ComandoAmbienteResult>

    @POST("api/v1/Comodos/toggle-device")
    suspend fun toggleDevice(
        @Query("dispositivoId") dispositivoId: String,
        @Query("tipo") tipo: String
    ): Response<Unit>
}
