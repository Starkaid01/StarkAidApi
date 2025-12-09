package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.GET

interface DispositivoEspApi {
    @GET("api/DispositivosEsp")
    suspend fun listarDispositivosEsp(): Response<List<DispositivoEspResponse>>
}

data class DispositivoEspResponse(
    val id: String,
    val nome: String,
    val ip: String,
    val porta: Int,
    val comando: String?,
    val comandToEsp: String?,
    val status: String,
    val ligadoDesligado: Boolean,
    val userId: String?
)

