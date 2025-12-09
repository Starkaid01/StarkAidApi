package com.starkaid.starkaidapp.services

import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.Response

data class ListarContatosRequest(
    val userId: String,
    val sessionName: String
)

data class ContatoResponse(
    val nome: String,
    val numero: String
)

data class ListarContatosResponse(
    val session: String,
    val sessionStatus: String,
    val totalContatosSalvos: Int,
    val contatos: List<ContatoResponse>
)


data class EnviarMensagemRequest(
    val userId: String,
    val sessionName: String,
    val phoneNumber: String,
    val message: String,
    val isGroup: Boolean,
    val isNewsletter: Boolean,
    val isLid: Boolean
)

data class EnviarMensagemResponse(
    val status: String,
    val response: List<MensagemResponse>
)

data class MensagemResponse(
    val id: String,
    val viewed: Boolean,
    val body: String,
    val type: String
)

interface WhatsappApi {

    @POST("/api/wpp/status-session")
    suspend fun statusSessao(
        @Body body: StatusSessaoRequest,
        @Header("Authorization") token: String
    ): Response<StatusSessaoResponse>

    @POST("/api/wpp/session")
    suspend fun criarSessao(
        @Body body: CriarSessaoRequest,
        @Header("Authorization") token: String
    ): Response<CriarSessaoResponse>

    @POST("/api/wpp/listar-contatos-salvos")
    suspend fun listarContatos(
        @Body body: ListarContatosRequest,
        @Header("Authorization") token: String
    ): Response<ListarContatosResponse>

    @POST("/api/wpp/enviar-mensagem")
    suspend fun enviarMensagem(
        @Body body: EnviarMensagemRequest,
        @Header("Authorization") token: String
    ): Response<EnviarMensagemResponse>
}

data class StatusSessaoRequest(
    val userId: String,
    val sessionName: String
)

data class CriarSessaoRequest(
    val userId: String,
    val sessionName: String,
    val waitQrCode: Boolean
)

data class CriarSessaoResponse(
    val status: String,
    val qrcode: String?,     // Base64
    val urlcode: String?,
    val session: String
)


data class StatusSessaoResponse(
    val session: String,
    val status: String,
    val version: String,     // Base64
    val qrCode: String,
    val cloudflareEndpoint: String,
    val tokenPrefix: String,
    val logFile: String
)