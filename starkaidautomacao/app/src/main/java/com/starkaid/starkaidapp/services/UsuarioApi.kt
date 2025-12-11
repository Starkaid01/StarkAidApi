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
    // @GET("api/v1/v1/Users/{id}/starkcoins")
    // suspend fun obterStarkCoins(
    //     @Path("id") userId: String,
    //     @Header("Authorization") token: String
    // ): Response<UsuarioResponse>

    @PATCH("api/v1/Users/{id}/update-starkcoins-ads")
    suspend fun updateStarkCoinsAds(
        @Path("id") userId: String,
        @Header("Authorization") token: String
    ): Response<UpdateStarkCoinsResponse>

    @PATCH("api/v1/Users/{id}/update-starkcoins-ia")
    suspend fun updateStarkCoinsIa(
        @Path("id") userId: String,
        @Header("Authorization") token: String
    ): Response<UpdateStarkCoinsResponse>

    @GET("api/v1/Users/{id}")
    suspend fun obterUsuario(
        @Path("id") userId: String
    ): Response<UsuarioResponse>

    @PUT("api/v1/Users/change-password")
    suspend fun alterarSenha(
        @Body senhaRequest: SenhaRequest,
        @Header("Authorization") token: String
    ): Response<String>

    @POST("api/v1/Users/request-password-reset")
    suspend fun solicitarResetSenha(
        @Body resetRequest: ResetSenhaRequest
    ): Response<String>

    @POST("api/v1/Users")
    suspend fun registerUser(@Body request: UserRegisterRequest): Response<UserRegisterResponse>

    // 'https://starkaid.vbweb.com.br/api/Users/musica/tocar'
    @POST("api/v1/Users/musica/tocar")
    suspend fun tocarMusica(
        @Header("Authorization") token: String,
        @Header("Api-Key") apiKey: String,
        @Body dto: MusicaDto
    ): Response<MusicaResponse>

    @POST("api/v1/Users/ia/super")
    suspend fun chamarSuperIA(
        @Body request: IaRequest
    ): Response<IaResponse>

    @POST("api/v1/Users/online")
    suspend fun setUserOnline(
        @Body request: SetUserOnlineRequest
    ): Response<SetUserOnlineResponse>

    @POST("api/v1/Users/offline")
    suspend fun setUserOffline(
        @Body request: SetUserOfflineRequest
    ): Response<SetUserOfflineResponse>
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

data class SetUserOnlineRequest(
    val origem: String = "app"
)

data class SetUserOnlineResponse(
    val message: String
)

data class SetUserOfflineRequest(
    val origem: String = "app"
)

data class SetUserOfflineResponse(
    val message: String
)

