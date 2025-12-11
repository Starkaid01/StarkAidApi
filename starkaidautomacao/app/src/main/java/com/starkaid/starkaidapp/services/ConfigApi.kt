package com.starkaid.starkaidapp.services

import com.starkaid.starkaidapp.models.AppConfig
import retrofit2.Response
import retrofit2.http.GET

interface ConfigApi {
    @GET("api/v1/Config/app-config")
    suspend fun getAppConfig(): Response<AppConfig>
}

