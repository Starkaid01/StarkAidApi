package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST

interface CommandApi {
    @POST("api/Commands/publish")
    suspend fun sendCommand(
        @Body command: CommandRequest,
        @Header("Authorization") authHeader: String,
        @Header("Api-Key") apiKey: String
    ): Response<CommandResponse>


}