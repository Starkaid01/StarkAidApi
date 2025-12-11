package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.DisparoResponse
import com.starkaid.starkaidapp.models.DispositivoDisparoResponse
import com.starkaid.starkaidapp.models.DispositivoRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface DisparoApi {
    @GET("api/v1/DispositivosDisparo")
    suspend fun listarDispositivos(): Response<List<DispositivoDisparoResponse>>

    @POST("api/v1/DispositivosDisparo")
    suspend fun cadastrarDispositivo(
        @Body dispositivo: DispositivoRequest
    ): Response<DispositivoDisparoResponse>

    @PUT("api/v1/DispositivosDisparo/{id}")
    suspend fun atualizarDispositivo(
        @Path("id") id: String,
        @Body dispositivo: DispositivoRequest
    ): Response<Void>

    @DELETE("api/v1/DispositivosDisparo/{id}")
    suspend fun deletarDispositivo(
        @Path("id") id: String,
    ): Response<Void>

    @GET("api/v1/Disparos")
    suspend fun listarDisparos(): Response<List<DisparoResponse>>

    @POST("api/v1/Disparos")
    suspend fun criarDisparo(
        @Body request: CriarDisparoRequest
    ): Response<DisparoResponse>

    @PUT("api/v1/Disparos/{id}/confirmar")
    suspend fun confirmarDisparo(
        @Path("id") disparoId: String
    ): Response<Void>


    @DELETE("api/v1/Disparos/{id}")
    suspend fun deletarDisparo(
        @Path("id") id: String
    ): Response<Void>

    @GET("api/v1/DispositivosDisparo/{id}")
    suspend fun buscarDispositivo(
        @Path("id") id: String
    ): Response<DispositivoDisparoResponse>
}

data class CriarDisparoRequest(
    val dispositivoId: String,
    val mensagem: String
)