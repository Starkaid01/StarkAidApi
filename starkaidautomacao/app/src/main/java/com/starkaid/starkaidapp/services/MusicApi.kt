package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.MusicResolveRequest
import com.starkaid.starkaidapp.models.MusicResolveResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface MusicApi {
    @POST("api/v1/Music/resolve")
    suspend fun resolveMusic(@Body request: MusicResolveRequest): Response<MusicResolveResponse>
}
