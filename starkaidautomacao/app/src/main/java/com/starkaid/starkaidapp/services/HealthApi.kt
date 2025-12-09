package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface HealthApi {
    @GET("api/HealthCheck/mqtt")
    suspend fun checkMqttHealth(
        @Header("Authorization") auth: String,
        @Header("X-API-Key") apiKey: String
    ): Response<HealthResponse>
}

data class HealthResponse(
    val status: String,
    val message: String
)