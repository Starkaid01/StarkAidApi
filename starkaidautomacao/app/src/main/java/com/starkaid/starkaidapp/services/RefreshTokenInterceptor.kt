package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.util.SessionExpiredHandler
import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Protocol
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import org.json.JSONObject
import java.util.concurrent.TimeUnit

class RefreshTokenInterceptor(context: Context) : Interceptor {
    private val sessionManager = SessionManager(context)

    // Lock para evitar múltiplos refresh simultâneos
    @Volatile
    private var isRefreshing = false

    private val lock = Object()

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val initialResponse = chain.proceed(request)

        // Se não for 401, retorne a resposta normalmente
        if (initialResponse.code != 401) {
            return initialResponse
        }

        initialResponse.close() // Feche a resposta imediatamente

        return synchronized(lock) {
            // Se já estamos atualizando, aguarde e tente novamente
            if (isRefreshing) {
                lock.wait(3000)
                val newToken = sessionManager.fetchAuthToken()
                if (!newToken.isNullOrEmpty()) {
                    return@synchronized chain.proceed(
                        request.newBuilder()
                            .header("Authorization", "Bearer $newToken")
                            .build()
                    )
                }
            }

            isRefreshing = true
            try {
                val newToken = refreshTokenSync()

                if (newToken != null) {
                    // Tente a requisição original com o novo token
                    chain.proceed(
                        request.newBuilder()
                            .header("Authorization", "Bearer $newToken")
                            .build()
                    )
                } else {
                    // Falha ao renovar - sessão expirada
                    SessionExpiredHandler.notifySessionExpired()
                    initialResponse // Retorne a resposta original
                }
            } finally {
                isRefreshing = false
                lock.notifyAll()
            }
        }
    }

    private fun refreshTokenSync(): String? {
        val refreshToken = sessionManager.fetchRefreshToken() ?: run {
            Log.e("RefreshToken", "Refresh token não encontrado")
            return null
        }

        var response: Response? = null
        try {
            Log.d("RefreshToken", "Enviando refresh token: $refreshToken")

            val json = JSONObject().apply {
                put("refreshToken", refreshToken)
            }

            val requestBody = json.toString()
                .toRequestBody("application/json".toMediaType())

            val request = Request.Builder()
                .url("https://starkaid.runasp.net/api/Auth/refresh-token")
                .addHeader("Connection", "close")
                .post(requestBody)
                .build()

            val client = OkHttpClient.Builder()
                .connectTimeout(15, TimeUnit.SECONDS)
                .readTimeout(15, TimeUnit.SECONDS)
                .protocols(listOf(Protocol.HTTP_1_1))
                .build()

            response = client.newCall(request).execute()

            if (response.isSuccessful) {
                response.body?.let { responseBody ->
                    try {
                        val responseString = responseBody.string()
                        if (responseString.isNotEmpty()) {
                            val responseJson = JSONObject(responseString)
                            val newToken = responseJson.getString("token")
                            val newRefreshToken = responseJson.optString("refreshToken", "")

                            sessionManager.saveAuthToken(newToken)
                            if (!newRefreshToken.isNullOrEmpty()) {
                                sessionManager.saveRefreshToken(newRefreshToken)
                            }

                            Log.d("RefreshToken", "Token renovado com sucesso")
                            return newToken
                        } else {
                            Log.e("RefreshToken", "Corpo da resposta vazio")
                        }
                    } catch (e: Exception) {
                        Log.e("RefreshToken", "Erro ao processar resposta do refresh token", e)
                    }
                } ?: Log.e("RefreshToken", "Response body é nulo")
            } else {
                try {
                    val errorBody = response.body?.string() ?: "Sem corpo de erro"
                    Log.e("RefreshToken", "Falha ao renovar token: ${response.code} - $errorBody")
                } catch (e: Exception) {
                    Log.e("RefreshToken", "Erro ao ler corpo de erro: ${e.message}")
                }

                // Se for 401, limpe os tokens
                if (response.code == 401) {
                    Log.e("RefreshToken", "Refresh token inválido ou expirado")
                    sessionManager.clearTokens()
                }
            }
        } catch (e: Exception) {
            Log.e("RefreshToken", "Erro ao renovar token", e)
        } finally {
            response?.close()
        }
        return null
    }
}