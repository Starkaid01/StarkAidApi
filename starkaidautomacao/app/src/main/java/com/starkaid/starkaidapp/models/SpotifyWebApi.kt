package com.starkaid.starkaidapp.models

import okhttp3.*
import org.json.JSONObject

object SpotifyWebApi {
    private const val CLIENT_ID = "b777ae2408054cebafda44c36a80be31"
    private const val CLIENT_SECRET = "68ecca5ce10743919b003e732c999842"
    private const val TOKEN_URL = "https://accounts.spotify.com/api/token"

    private val client = OkHttpClient()

    fun refreshToken(refreshToken: String): SpotifyTokens {
        val body = FormBody.Builder()
            .add("grant_type", "refresh_token")
            .add("refresh_token", refreshToken)
            .build()

        val request = Request.Builder()
            .url(TOKEN_URL)
            .post(body)
            .addHeader(
                "Authorization",
                Credentials.basic(CLIENT_ID, CLIENT_SECRET)
            )
            .build()

        client.newCall(request).execute().use { response ->
            if (!response.isSuccessful) throw Exception("Erro no refresh: ${response.code}")

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
