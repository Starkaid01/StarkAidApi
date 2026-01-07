package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.*

data class RotinaDto(
    val id: String,
    val nome: String,
    val descricao: String?,
    val ativa: Boolean,
    val gatilhos: List<RotinaGatilhoDto>,
    val acoes: List<RotinaAcaoDto>
)

data class RotinaGatilhoDto(
    val id: String,
    val tipo: Any?, // Pode ser Int ou String (ex: 0/"tempo")
    val expressao: String,
    val diasSemana: String?
)

data class RotinaAcaoDto(
    val id: String,
    val ordemExecucao: Int,
    val tipo: Any?, // Pode ser Int ou String (ex: 0/"dispositivo")
    val payload: String
)

data class CreateRotinaRequest(
    val nome: String,
    val descricao: String?,
    val gatilhos: List<CreateRotinaGatilhoRequest>,
    val acoes: List<CreateRotinaAcaoRequest>
)

data class CreateRotinaGatilhoRequest(
    val tipo: Int,
    val expressao: String,
    val diasSemana: String?
)

data class CreateRotinaAcaoRequest(
    val ordemExecucao: Int,
    val tipo: Int,
    val payload: String
)

data class UpdateRotinaRequest(
    val nome: String,
    val descricao: String?,
    val ativa: Boolean,
    val gatilhos: List<CreateRotinaGatilhoRequest>,
    val acoes: List<CreateRotinaAcaoRequest>
)

interface RotinasApi {
    @GET("api/Rotinas")
    suspend fun getAll(): Response<List<RotinaDto>>

    @GET("api/Rotinas/{id}")
    suspend fun getById(@Path("id") id: String): Response<RotinaDto>

    @POST("api/Rotinas")
    suspend fun create(@Body request: CreateRotinaRequest): Response<RotinaDto>

    @PUT("api/Rotinas/{id}")
    suspend fun update(@Path("id") id: String, @Body request: UpdateRotinaRequest): Response<RotinaDto>

    @DELETE("api/Rotinas/{id}")
    suspend fun delete(@Path("id") id: String): Response<Unit>

    @POST("api/Rotinas/{id}/ativar")
    suspend fun ativar(@Path("id") id: String): Response<Unit>

    @POST("api/Rotinas/{id}/desativar")
    suspend fun desativar(@Path("id") id: String): Response<Unit>

    @POST("api/Rotinas/{id}/executar")
    suspend fun executar(@Path("id") id: String): Response<Unit>
}
