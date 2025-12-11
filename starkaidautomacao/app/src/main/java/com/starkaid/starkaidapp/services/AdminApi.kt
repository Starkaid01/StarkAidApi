package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface AdminApi {
    @GET("api/v1/Admin/admin-only")
    suspend fun adminOnly(
        @Header("Authorization") authHeader: String,
        @Header("Api-Key") apiKey: String
    ): Response<Void>
}

