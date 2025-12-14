package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.EconomicPayload
import retrofit2.Response
import retrofit2.http.*

data class NivelResponse(val nivel: String)

data class AdsResponse(val adsAtiv: String)

data class AddFundsRequest(val coins: Int)

data class AddFundsResponse(
    val checkoutUrl: String,
    val sessionId: String,
    val economy: EconomicPayload? = null
)

interface UsersApi {


    @GET("api/v1/Users/ads")
    suspend fun getAds(): Response<AdsResponse>

    @GET("api/v1/Users/nivel")
    suspend fun getNivel(): Response<NivelResponse>

    // Adicione este método para deletar conta
    @DELETE("api/v1/Users/delete-account")
    suspend fun deleteAccount(): Response<DeleteAccountResponse>

    // Adicionar fundos (pagamento avulso)
    @POST("api/v1/Users/add-funds")
    suspend fun addFunds(
        @Body request: AddFundsRequest
    ): Response<AddFundsResponse>

    // Buscar dados do usuário atual
    @GET("api/v1/Users/me")
    suspend fun getCurrentUser(): Response<CurrentUserResponse>
}

data class CurrentUserResponse(
    val id: String,
    val name: String,
    val email: String,
    val role: String,
    val apiKey: String,
    val removalAds: String?,
    val estado: String?,
    val cidade: String?,
    val bairro: String?,
    val isActive: Boolean,
    val createdAt: String?,
    val economy: EconomicPayload? = null
)

data class DeleteAccountResponse(val message: String)