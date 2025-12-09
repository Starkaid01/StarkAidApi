package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface ProtectedApi {
    @GET("api/Protected/secreto")
    suspend fun acessarSecreto(
        @Header("Authorization") authHeader: String,
        @Header("Api-Key") apiKey: String
    ): Response<String>
}

