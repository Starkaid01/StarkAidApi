package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.*
import retrofit2.Response
import retrofit2.http.*

interface DispositivosEspApi {
    @GET("api/DispositivosEsp")
    suspend fun listarDispositivos(): Response<List<DispositivoEsp>>

    @GET("api/DispositivosEsp/{id}")
    suspend fun obterDispositivo(@Path("id") id: String): Response<DispositivoEsp>

    @POST("api/DispositivosEsp")
    suspend fun criarDispositivo(@Body request: CreateDispositivoEspRequest): Response<DispositivoEsp>

    @PUT("api/DispositivosEsp/{id}")
    suspend fun atualizarDispositivo(
        @Path("id") id: String,
        @Body request: UpdateDispositivoEspRequest
    ): Response<Unit>

    @DELETE("api/DispositivosEsp/{id}")
    suspend fun excluirDispositivo(@Path("id") id: String): Response<Unit>

    @POST("api/DispositivosEsp/{id}/ping")
    suspend fun pingDispositivo(@Path("id") id: String): Response<PingResponse>

    @POST("api/DispositivosEsp/enviar-comando")
    suspend fun enviarComando(@Body request: EnviarComandoRequest): Response<EnviarComandoResponse>
}

