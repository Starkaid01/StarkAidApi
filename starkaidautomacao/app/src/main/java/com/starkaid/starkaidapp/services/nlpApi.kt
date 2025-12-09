package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Query

interface NlpApi {
    @POST("/api/NlpServer/add-name")
    suspend fun salvarContatosBackend(
        @Body body: AddNameRequest
    ): Response<SalvarContatosBackendResponse>

    @POST("/api/NlpServer/extract-entities")
    suspend fun extractEntities(
        @Query("id") userId: String,              // ID como query param
        @Header("Authorization") token: String,  // token no header
        @Body body: NlpExtractRequest
    ): Response<NlpExtractResponse>
}

data class SalvarContatosBackendResponse(
    val input: String?,
    val parsed_names: List<String>?,
    val parsed_surnames: List<String>?,
    val saved_names: List<String>?,
    val saved_surnames: List<String>?,
    val duplicate_names: List<String>?,
    val duplicate_surnames: List<String>?,
    val message: String?
)

data class AddNameRequest(
    val full_name: String
)

data class NlpExtractRequest(
    val text: String
)

data class NlpExtractResponse(
    val input: String?,
    val entities: Map<String, List<String>>?,
    val method: String?
)
