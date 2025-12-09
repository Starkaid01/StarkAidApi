package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Path

interface StatusApi {
    @GET("api/Status/{deviceId}/status")
    suspend fun getStatus(
        @Path("deviceId") deviceId: String,
        @Header("Authorization") authHeader: String,
        @Header("Api-Key") apiKey: String
    ): Response<DeviceStatus>
}