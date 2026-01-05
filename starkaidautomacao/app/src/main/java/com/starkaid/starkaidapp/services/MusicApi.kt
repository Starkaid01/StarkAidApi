package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.MusicResolveRequest
import com.starkaid.starkaidapp.models.MusicResolveResponse
import com.starkaid.starkaidapp.models.ExternalAudioStreamResult
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface MusicApi {
    @POST("api/v1/Music/resolve")
    suspend fun resolveMusic(@Body request: MusicResolveRequest): Response<MusicResolveResponse>

    @GET("api/v1/Music/online/stream/{id}")
    suspend fun getAudioStream(@Path("id") id: String): Response<ExternalAudioStreamResult>
}
