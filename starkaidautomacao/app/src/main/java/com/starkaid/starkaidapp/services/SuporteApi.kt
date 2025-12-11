package com.starkaid.starkaidapp.services

import retrofit2.Response
import retrofit2.http.*

interface SuporteApi {
    @GET("api/v1/suporte/verificar-resolvendo-suporte")
    suspend fun verificarResolvendoSuporte(@Query("origem") origem: String): Response<org.json.JSONObject>

    @POST("api/v1/suporte/marcar-resolvido")
    suspend fun marcarResolvido(@Body request: MarcarResolvidoRequest): Response<org.json.JSONObject>

    @POST("api/v1/suporte/enviar-formulario-limite")
    suspend fun enviarFormularioLimite(@Body request: FormularioLimiteRequest): Response<org.json.JSONObject>
}

data class MarcarResolvidoRequest(val origem: String)
data class FormularioLimiteRequest(val mensagem: String, val detalhes: String? = null)
