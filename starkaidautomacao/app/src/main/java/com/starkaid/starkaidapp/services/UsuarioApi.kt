package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.IaRequest
import com.starkaid.starkaidapp.models.IaResponse
import com.starkaid.starkaidapp.models.MusicaDto
import com.starkaid.starkaidapp.models.MusicaResponse
import com.starkaid.starkaidapp.models.UserRegisterRequest
import com.starkaid.starkaidapp.models.UserRegisterResponse
import retrofit2.Response
import com.starkaid.starkaidapp.models.UsuarioResponse
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface UsuarioApi {

    // Endpoint removido da API - usar obterUsuario() ao invés disso
    // @GET("api/Users/{id}/starkcoins")
    // suspend fun obterStarkCoins(
    //     @Path("id") userId: String,
    //     @Header("Authorization") token: String
    // ): Response<UsuarioResponse>

    @PATCH("api/Users/{id}/update-starkcoins-ads")
    suspend fun updateStarkCoinsAds(
        @Path("id") userId: String,
        @Header("Authorization") token: String
    ): Response<UpdateStarkCoinsResponse>

    @PATCH("api/Users/{id}/update-starkcoins-ia")
    suspend fun updateStarkCoinsIa(
        @Path("id") userId: String,
        @Header("Authorization") token: String
    ): Response<UpdateStarkCoinsResponse>

    @GET("api/Users/{id}")
    suspend fun obterUsuario(
        @Path("id") userId: String
    ): Response<UsuarioResponse>

    @PUT("api/Users/change-password")
    suspend fun alterarSenha(
        @Body senhaRequest: SenhaRequest,
        @Header("Authorization") token: String
    ): Response<String>

    @POST("api/Users/request-password-reset")
    suspend fun solicitarResetSenha(
        @Body resetRequest: ResetSenhaRequest
    ): Response<String>

    @POST("api/Users")
    suspend fun registerUser(@Body request: UserRegisterRequest): Response<UserRegisterResponse>

    // 'https://starkaid.vbweb.com.br/api/Users/musica/tocar'
    @POST("api/Users/musica/tocar")
    suspend fun tocarMusica(
        @Header("Authorization") token: String,
        @Header("Api-Key") apiKey: String,
        @Body dto: MusicaDto
    ): Response<MusicaResponse>

    @POST("api/Users/ia/super")
    suspend fun chamarSuperIA(
        @Body request: IaRequest
    ): Response<IaResponse>
}

data class SenhaRequest(
    val currentPassword: String,
    val newPassword: String
)

data class ResetSenhaRequest(
    val email: String
)

// DTO para resposta dos updates
data class UpdateStarkCoinsResponse(
    val message: String,
    val saldoAtual: Double
)

