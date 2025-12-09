package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.GET

interface HealthCheckApi {
    @GET("api/HealthCheck/mqtt")
    suspend fun checkMqttStatus(): Response<MqttStatusResponse>

    @GET("api/HealthCheck/api")
    suspend fun checkApiStatus(): Response<MqttStatusResponse>
}

data class MqttStatusResponse(
    val status: String,
    val message: String
)
