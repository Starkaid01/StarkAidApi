package com.starkaid.starkaidapp.models

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager
import okhttp3.*
import org.json.JSONObject

object SpotifyWebApi {
    private val client = OkHttpClient()

    fun refreshToken(refreshToken: String, context: Context? = null): SpotifyTokens {
        val sessionManager = context?.let { SessionManager.getInstance(it) }
        
        // Buscar credenciais do SessionManager ou usar padrão
        val clientId = sessionManager?.fetchSpotifyClientId() ?: ApiConfig.spotifyClientId
        val clientSecret = sessionManager?.fetchSpotifyClientSecret() ?: ApiConfig.spotifyClientSecret

        val body = FormBody.Builder()
            .add("grant_type", "refresh_token")
            .add("refresh_token", refreshToken)
            .build()

        val request = Request.Builder()
            .url(ApiConfig.spotifyTokenUrl)
            .post(body)
            .addHeader(
                "Authorization",
                Credentials.basic(clientId, clientSecret)
            )
            .build()

        client.newCall(request).execute().use { response ->
            if (!response.isSuccessful) {
                val errorBody = response.body?.string()
                Log.e("SpotifyWebApi", "Erro no refresh: ${response.code} - $errorBody")
                throw Exception("Erro no refresh: ${response.code}")
            }

            val json = JSONObject(response.body?.string() ?: throw Exception("Resposta vazia"))
            val accessToken = json.getString("access_token")
            val expiresIn = json.getInt("expires_in")
            val newRefreshToken = if (json.has("refresh_token")) json.getString("refresh_token") else null

            return SpotifyTokens(
                accessToken = accessToken,
                refreshToken = newRefreshToken,
                expiresIn = expiresIn
            )
        }
    }
}
