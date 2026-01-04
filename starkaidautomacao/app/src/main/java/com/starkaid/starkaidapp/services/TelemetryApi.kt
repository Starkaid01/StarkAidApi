package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface TelemetryApi {
    @POST("api/v1/telemetry")
    suspend fun postTelemetry(@Body dto: TelemetryEventDto): Response<Unit>

    @POST("api/v1/telemetry/ia")
    suspend fun postAiTelemetry(@Body dto: AiTelemetryEventDto): Response<Unit>
}
