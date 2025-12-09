package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.*

data class AssinarResponse(
    val checkoutUrl: String,
    val sessionId: String,
    val customerId: String?
)

data class PlanoStatusResponse(
    val status: String,
    val iniciadaEm: String?,
    val canceladaEm: String?,
    val expiraEm: String?,
    val pagamentoConfirmadoEm: String?,
    val stripeCustomerId: String?,
    val stripeSubscriptionId: String?,
    val stripePriceId: String?,
    val valor: Int?
)

data class CancelarPlanoResponse(
    val message: String,
    val results: List<CancelResult>
)

data class CancelResult(
    val subscriptionId: String,
    val localStatus: String,
    val stripeStatus: String
)

data class AdsAssinaturaStatusResponse(
    val status: String
)

interface AssinaturasApi {

    @POST("api/Assinaturas/create/{nivel}")
    suspend fun criarAssinatura(
        @Path("nivel") nivel: Int,
        @Query("userId") userId: String?,
        @Header("Authorization") bearerToken: String,
        @Header("Api-Key") apiKey: String
    ): Response<AssinarResponse>

    @POST("api/Assinaturas/avulso/{valor}")
    suspend fun criarCompraAvulsa(
        @Path("valor") valor: Double,
        @Query("userId") userId: String?,
        @Header("Authorization") bearerToken: String,
        @Header("Api-Key") apiKey: String
    ): Response<AssinarResponse>

    @GET("api/Assinaturas/status")
    suspend fun getPlanoStatus(
        @Header("Authorization") bearerToken: String,
        @Header("Api-Key") apiKey: String
    ): Response<PlanoStatusResponse>

    @GET("api/Assinaturas/ads/assinatura/status")
    suspend fun getAdsAssinaturaStatus(
        @Header("Authorization") bearerToken: String,
        @Header("Api-Key") apiKey: String
    ): Response<AdsAssinaturaStatusResponse>

    @POST("api/Assinaturas/cancelar")
    suspend fun cancelarAssinatura(
        @Header("Authorization") bearerToken: String,
        @Header("Api-Key") apiKey: String
    ): Response<CancelarPlanoResponse>

    @POST("api/Assinaturas/checkout")
    suspend fun checkout(
        @Body request: CheckoutRequest
    ): Response<AssinarResponse>

    @GET("api/Assinaturas/ativas")
    suspend fun listarAtivas(): Response<List<PlanoAtivoResponse>>

    @POST("api/Assinaturas/cancelar/{assinaturaId}")
    suspend fun cancelarAssinaturaPorId(
        @Path("assinaturaId") assinaturaId: String
    ): Response<CancelarPlanoResponse>
}

data class CheckoutRequest(
    val nivel: Int
)

data class PlanoAtivoResponse(
    val id: String,
    val nivel: Int,
    val nomePlano: String,
    val valor: Double,
    val status: String,
    val iniciadaEm: String?,
    val expiraEm: String?,
    val dataCriacao: String,
    val stripeSubscriptionId: String?
)