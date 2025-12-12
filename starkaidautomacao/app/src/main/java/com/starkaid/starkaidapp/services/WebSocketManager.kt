package com.starkaid.starkaidapp.services

import android.util.Log
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager
import kotlinx.coroutines.*
import okhttp3.*
import java.util.concurrent.TimeUnit

class WebSocketManager(
    private val sessionManager: SessionManager, // sua classe de sessão
    private val toastCallback: (String) -> Unit
) {
    private var webSocket: WebSocket? = null
    private val pingInterval = 30_000L // 30 segundos

    private val okHttpClient = OkHttpClient.Builder()
        .pingInterval(pingInterval, TimeUnit.MILLISECONDS)
        .build()

    private var isActive = true
    private var pingJob: Job? = null

    fun start() {
        val userId = sessionManager.fetchUserId() ?: return
        if (userId.isEmpty()) return

        val token = sessionManager.fetchAuthToken() ?: return
        if (token.isEmpty()) return

        val webBaseUrl = ApiConfig.webBaseUrl.replace("https://", "").replace("http://", "")
        val url = "wss://$webBaseUrl/api/Websocket/connect/$userId"
        val request = Request.Builder()
            .url(url)
            .addHeader("Authorization", "Bearer $token")
            .build()

        val listener = object : WebSocketListener() {
            override fun onOpen(ws: WebSocket, response: Response) {
                Log.d("WebSocket", "✅ Conexão aberta")
                webSocket = ws
                startPing()
            }

            override fun onMessage(ws: WebSocket, text: String) {
                Log.d("WebSocket", "📩 Mensagem recebida: $text")
                toastCallback(text)
            }

            override fun onClosed(ws: WebSocket, code: Int, reason: String) {
                Log.d("WebSocket", "❌ Conexão fechada: $reason")
                reconnect()
            }

            override fun onFailure(ws: WebSocket, t: Throwable, response: Response?) {
                Log.e("WebSocket", "⚠️ Falha: ${t.message}")
                t.printStackTrace()
                reconnect()
            }
        }

        okHttpClient.newWebSocket(request, listener)
    }

    private fun startPing() {
        pingJob?.cancel()
        pingJob = CoroutineScope(Dispatchers.IO).launch {
            while (isActive && webSocket != null) {
                try {
                    webSocket?.send("ping")
                    delay(pingInterval)
                } catch (e: Exception) {
                    Log.e("WebSocket", "Falha no ping: ${e.message}")
                    reconnect()
                    break
                }
            }
        }
    }

    private fun reconnect() {
        CoroutineScope(Dispatchers.IO).launch {
            delay(5000)
            Log.d("WebSocket", "🔄 Tentando reconectar...")
            start()
        }
    }

    fun send(message: String): Boolean {
        return try {
            webSocket?.send(message) ?: false
        } catch (e: Exception) {
            Log.e("WebSocket", "Erro ao enviar mensagem: ${e.message}")
            false
        }
    }

    fun stop() {
        isActive = false
        pingJob?.cancel()
        webSocket?.close(1000, "App fechado")
        webSocket = null
    }
}
