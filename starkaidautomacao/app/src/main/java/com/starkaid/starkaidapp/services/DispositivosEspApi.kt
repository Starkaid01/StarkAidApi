package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.*
import retrofit2.Response
import retrofit2.http.*

interface DispositivosEspApi {
    @GET("api/v1/DispositivosEsp")
    suspend fun listarDispositivos(): Response<List<DispositivoEsp>>

    @GET("api/v1/DispositivosEsp/{id}")
    suspend fun obterDispositivo(@Path("id") id: String): Response<DispositivoEsp>

    @POST("api/v1/DispositivosEsp")
    suspend fun criarDispositivo(@Body request: CreateDispositivoEspRequest): Response<DispositivoEsp>

    @PUT("api/v1/DispositivosEsp/{id}")
    suspend fun atualizarDispositivo(
        @Path("id") id: String,
        @Body request: UpdateDispositivoEspRequest
    ): Response<Unit>

    @DELETE("api/v1/DispositivosEsp/{id}")
    suspend fun excluirDispositivo(@Path("id") id: String): Response<Unit>

    @POST("api/v1/DispositivosEsp/{id}/ping")
    suspend fun pingDispositivo(@Path("id") id: String): Response<PingResponse>

    @POST("api/v1/DispositivosEsp/enviar-comando")
    suspend fun enviarComando(@Body request: EnviarComandoRequest): Response<EnviarComandoResponse>
}

