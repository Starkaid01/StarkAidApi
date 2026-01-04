package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.data.SessionManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.json.JSONObject

object TelemetryClient {
    private var telemetryApi: TelemetryApi? = null

    private fun getApi(context: Context): TelemetryApi {
        return telemetryApi ?: ApiClient.getClient(context).create(TelemetryApi::class.java).also {
            telemetryApi = it
        }
    }

    fun sendEvent(
        context: Context,
        evento: String,
        categoria: String,
        metadata: Map<String, Any>? = null
    ) {
        val sessionManager = SessionManager.getInstance(context)
        val userId = sessionManager.fetchUserId() ?: return

        val metadataJson = metadata?.let { JSONObject(it).toString() }
        
        val dto = TelemetryEventDto(
            userId = userId,
            origem = "Android",
            evento = evento,
            categoria = categoria,
            metadataJson = metadataJson
        )

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = getApi(context).postTelemetry(dto)
                if (!response.isSuccessful) {
                    Log.e("Telemetry", "Falha ao enviar telemetria: ${response.code()}")
                }
            } catch (e: Exception) {
                Log.e("Telemetry", "Erro ao enviar telemetria", e)
            }
        }
    }

    fun sendAiEvent(
        context: Context,
        textoOriginal: String,
        resultado: String,
        latenciaMs: Int,
        chamouIaExterna: Boolean = false,
        similarityScore: Double? = null,
        aprendizadoTipo: String? = null,
        aprendizadoId: String? = null
    ) {
        val sessionManager = SessionManager.getInstance(context)
        val userId = sessionManager.fetchUserId() ?: return

        val dto = AiTelemetryEventDto(
            userId = userId,
            textoOriginal = textoOriginal,
            resultado = resultado,
            latenciaMs = latenciaMs,
            chamouIaExterna = chamouIaExterna,
            similarityScore = similarityScore,
            aprendizadoTipo = aprendizadoTipo,
            aprendizadoId = aprendizadoId,
            origem = "Android Pipeline"
        )

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = getApi(context).postAiTelemetry(dto)
                if (!response.isSuccessful) {
                    Log.e("Telemetry", "Falha ao enviar telemetria IA: ${response.code()}")
                }
            } catch (e: Exception) {
                Log.e("Telemetry", "Erro ao enviar telemetria IA", e)
            }
        }
    }
}
