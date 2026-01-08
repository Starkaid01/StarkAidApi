package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.POST
import retrofit2.http.Path

interface LembretesApi {
    @POST("api/v1/Lembretes/{id}/falado")
    suspend fun marcarFalado(@Path("id") id: String): Response<Unit>

    @POST("api/v1/Lembretes")
    suspend fun criarLembrete(@retrofit2.http.Body request: CreateLembreteRequest): Response<CreateLembreteResponse>
}

data class CreateLembreteRequest(
    val texto: String,
    val dispararEm: String? = null
)

data class CreateLembreteResponse(
    val success: Boolean,
    val message: String?,
    val code: String?,
    val id: String?,
    val texto: String?
)
