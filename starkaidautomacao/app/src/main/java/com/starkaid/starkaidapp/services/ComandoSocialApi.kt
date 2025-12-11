package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.ComandoSocial
import com.starkaid.starkaidapp.models.CriarComandoSocialRequest
import retrofit2.Response
import retrofit2.http.*

interface ComandoSocialApi {
    @GET("api/v1/ComandosSociais")
    suspend fun listarComandos(): Response<List<ComandoSocial>>

    @POST("api/v1/ComandosSociais")
    suspend fun criarComando(@Body request: CriarComandoSocialRequest): Response<ComandoSocial>

    @PUT("api/v1/ComandosSociais/{id}")
    suspend fun atualizarComando(
        @Path("id") id: String,
        @Body comando: ComandoSocial
    ): Response<Void>

    @DELETE("api/v1/ComandosSociais/{id}")
    suspend fun excluirComando(@Path("id") id: String): Response<Void>

    @GET("api/v1/ComandosSociais/random-answers")
    suspend fun obterRespostasAleatorias(
        @Query("resposta") resposta: String
    ): Response<List<String>>
}