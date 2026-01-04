package com.starkaid.starkaidapp.services

import com.google.gson.annotations.SerializedName
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
    @SerializedName("id") val id: String,
    @SerializedName("name") val name: String,
    @SerializedName("email") val email: String,
    @SerializedName("role") val role: String,
    @SerializedName("apiKey") val apiKey: String,
    @SerializedName("removalAds") val removalAds: String?,
    @SerializedName("estado") val estado: String?,
    @SerializedName("cidade") val cidade: String?,
    @SerializedName("bairro") val bairro: String?,
    @SerializedName("isActive") val isActive: Boolean,
    @SerializedName("createdAt") val createdAt: String?,
    @SerializedName("economy") val economy: EconomicPayload? = null
)

data class DeleteAccountResponse(val message: String)