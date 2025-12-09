package com.starkaid.starkaidapp.services

import android.content.Context
import android.content.Intent
import android.content.SharedPreferences
import android.media.AudioManager
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.Uri
import android.util.Log
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import okhttp3.FormBody
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONObject
import java.util.concurrent.TimeUnit
import kotlin.math.max
import kotlin.math.min

class SpotifyService(private val context: Context) {

    private val prefs: SharedPreferences =
        context.getSharedPreferences("starkaid_prefs", Context.MODE_PRIVATE)

    private val client = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .build()

    private val clientId = "b777ae2408054cebafda44c36a80be31"
    private val clientSecret = "68ecca5ce10743919b003e732c999842"
    private val tokenUrl = "https://accounts.spotify.com/api/token"
    private val TAG = "SpotifyService"

    // ---------------------------
    // Controle Premium/Free
    // ---------------------------
    var isPremium: Boolean
        get() = prefs.getBoolean("spotify_is_premium", false)
        private set(value) {
            prefs.edit().putBoolean("spotify_is_premium", value).apply()
        }

    // Atualiza se usuário é Premium
    suspend fun updateUserProduct() = withContext(Dispatchers.IO) {
        try {
            val token = getValidAccessToken()
            val req = Request.Builder()
                .url("https://api.spotify.com/v1/me")
                .addHeader("Authorization", "Bearer $token")
                .build()

            client.newCall(req).execute().use { res ->
                if (!res.isSuccessful) throw Exception("Erro ao consultar perfil: ${res.code}")
                val json = JSONObject(res.body?.string() ?: "")
                val product = json.optString("product", "free")
                isPremium = product.equals("premium", ignoreCase = true)
                Log.i(TAG, "Produto do Spotify: $product (isPremium=$isPremium)")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Falha ao checar produto: ${e.message}")
        }
    }

    // ---------------------------
    // Verifica conexão
    // ---------------------------
    private fun isOnline(): Boolean {
        val cm = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = cm.activeNetwork ?: return false
        val capabilities = cm.getNetworkCapabilities(network) ?: return false
        return capabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
    }

    // ---------------------------
    // Tokens
    // ---------------------------
    suspend fun getValidAccessToken(): String = withContext(Dispatchers.IO) {
        if (!isOnline()) throw IllegalStateException("Sem conexão com a internet")

        val accessToken = prefs.getString("spotify_access_token", null)
        val expiresAt = prefs.getLong("spotify_expires_at", 0)
        val refreshToken = prefs.getString("spotify_refresh_token", null)

        if (accessToken != null && System.currentTimeMillis() < expiresAt) {
            return@withContext accessToken
        }

        if (refreshToken != null) {
            val body = FormBody.Builder()
                .add("grant_type", "refresh_token")
                .add("refresh_token", refreshToken)
                .add("client_id", clientId)
                .add("client_secret", clientSecret)
                .build()

            val req = Request.Builder()
                .url(tokenUrl)
                .post(body)
                .build()

            var attempts = 0
            while (attempts < 3) {
                try {
                    client.newCall(req).execute().use { res ->
                        if (!res.isSuccessful) throw Exception("Erro ao refresh token: ${res.code}")
                        val json = JSONObject(res.body?.string() ?: "")
                        val newAccessToken = json.getString("access_token")
                        val expiresIn = json.getLong("expires_in")

                        prefs.edit()
                            .putString("spotify_access_token", newAccessToken)
                            .putLong("spotify_expires_at", System.currentTimeMillis() + expiresIn * 1000)
                            .apply()

                        return@withContext newAccessToken
                    }
                } catch (e: Exception) {
                    attempts++
                    if (attempts >= 3) throw e
                }
            }
        }

        throw IllegalStateException("Usuário precisa logar no Spotify primeiro.")
    }

    // ---------------------------
    // Buscar track/artista
    // ---------------------------
    suspend fun searchTrack(query: String): Pair<String?, String?> = withContext(Dispatchers.IO) {
        if (!isOnline()) return@withContext Pair(null, null)

        val token = getValidAccessToken()
        val req = Request.Builder()
            .url("https://api.spotify.com/v1/search?q=${Uri.encode(query)}&type=track&limit=1")
            .addHeader("Authorization", "Bearer $token")
            .build()

        try {
            client.newCall(req).execute().use { res ->
                if (!res.isSuccessful) throw Exception("Erro na busca de track: ${res.code}")
                val json = JSONObject(res.body?.string() ?: "")
                val items = json.getJSONObject("tracks").getJSONArray("items")
                if (items.length() > 0) {
                    val track = items.getJSONObject(0)
                    val trackUri = track.getString("uri")
                    val artistUri = track.getJSONArray("artists").getJSONObject(0).getString("uri")
                    return@withContext Pair(trackUri, artistUri)
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Falha na busca track: ${e.message}")
        }
        return@withContext Pair(null, null)
    }

    // ---------------------------
    // Devices
    // ---------------------------
    suspend fun getActiveDeviceId(): String? = withContext(Dispatchers.IO) {
        val token = getValidAccessToken()
        val req = Request.Builder()
            .url("https://api.spotify.com/v1/me/player/devices")
            .addHeader("Authorization", "Bearer $token")
            .build()

        try {
            client.newCall(req).execute().use { res ->
                if (!res.isSuccessful) throw Exception("Erro ao buscar devices: ${res.code}")
                val json = JSONObject(res.body?.string() ?: "")
                val devices = json.getJSONArray("devices")

                for (i in 0 until devices.length()) {
                    val device = devices.getJSONObject(i)
                    if (device.getBoolean("is_active")) return@withContext device.getString("id")
                }

                if (devices.length() > 0) {
                    return@withContext devices.getJSONObject(0).getString("id") // Não tocar aqui
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Falha ao buscar devices: ${e.message}")
        }
        return@withContext null
    }

    // ---------------------------
    // Play/Stop
    // ---------------------------
    suspend fun play(query: String) {
        if (isPremium) {
            var deviceId = getActiveDeviceId()
            if (deviceId == null) {
                Log.w(TAG, "Nenhum dispositivo ativo, abrindo app Spotify...")
                // Abre o app Spotify para ativar um device
                val intent = Intent(Intent.ACTION_VIEW, Uri.parse("spotify:app"))
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                context.startActivity(intent)

                // Dá um tempo para o usuário abrir/ativar o device
                delay(2000L) // 2 segundos, você pode ajustar

                // Tenta novamente pegar device ativo
                deviceId = getActiveDeviceId()
                if (deviceId == null) {
                    Log.e(TAG, "Ainda nenhum dispositivo ativo, não foi possível tocar música")
                    return
                }
            }
            playTrackPremium(query, deviceId)
        } else {
            playFree(query)
        }
    }

    suspend fun stopMusic() {
        if (isPremium) {
            val deviceId = getActiveDeviceId()
            if (deviceId != null) pauseTrack(deviceId)
        } else {
            try {
                val intent = Intent(Intent.ACTION_VIEW, Uri.parse("spotify:home")).apply {
                    flags = Intent.FLAG_ACTIVITY_NEW_TASK
                }
                context.startActivity(intent)
                Log.i(TAG, "Spotify Free: stop simulado (home)")
            } catch (e: Exception) {
                Log.e(TAG, "Erro ao simular stop Free: ${e.message}")
            }
        }
    }

    // ---------------------------
    // Premium → Play track
    // ---------------------------
    private suspend fun playTrackPremium(query: String, deviceId: String) = withContext(Dispatchers.IO) {
        try {
            val token = getValidAccessToken()
            val (trackUri, _) = searchTrack(query)
            if (trackUri == null) {
                Log.w(TAG, "Nenhuma faixa encontrada no Premium")
                return@withContext
            }

            val playUrl = "https://api.spotify.com/v1/me/player/play?device_id=$deviceId"
            val playBody = "{\"uris\":[\"$trackUri\"]}"
            val playRequest = Request.Builder()
                .url(playUrl)
                .addHeader("Authorization", "Bearer $token")
                .put(playBody.toRequestBody("application/json".toMediaTypeOrNull()))
                .build()

            client.newCall(playRequest).execute().use { response ->
                if (response.isSuccessful) Log.i(TAG, "Tocando $trackUri no dispositivo $deviceId")
                else Log.e(TAG, "Erro ao tocar música Premium: ${response.code}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro Premium play: ${e.message}")
        }
    }

    // ---------------------------
    // Free → Rádio/artista
    // ---------------------------
    private suspend fun playFree(query: String) = withContext(Dispatchers.Main) {
        try {
            val (_, artistUri) = searchTrack(query)
            val uriToPlay = artistUri?.let { "spotify:radio:$it" } ?: "spotify:search:$query"

            val intent = Intent(Intent.ACTION_VIEW, Uri.parse(uriToPlay)).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK
            }
            context.startActivity(intent)
            Log.i(TAG, "Spotify Free: abriu $uriToPlay")
        } catch (e: Exception) {
            Log.e(TAG, "Erro no Free play: ${e.message}")
        }
    }

    // ---------------------------
    // Pause Premium
    // ---------------------------
    private suspend fun pauseTrack(deviceId: String) = withContext(Dispatchers.IO) {
        try {
            val token = getValidAccessToken()
            val url = "https://api.spotify.com/v1/me/player/pause?device_id=$deviceId"
            val request = Request.Builder()
                .url(url)
                .addHeader("Authorization", "Bearer $token")
                .put("".toRequestBody())
                .build()

            client.newCall(request).execute().use { response ->
                if (response.isSuccessful) Log.i(TAG, "Música pausada")
                else Log.e(TAG, "Erro ao pausar: ${response.code}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro pause Premium: ${e.message}")
        }
    }


    // ---------------------------
    // Ajuste de volume (local vs Spotify Connect)
    // ---------------------------
    suspend fun adjustVolume(increase: Boolean) = withContext(Dispatchers.IO) {
        try {
            val deviceId = getActiveDeviceId()
            val token = getValidAccessToken()

            if (deviceId == null || isLocalPlayback(deviceId)) {
                // Ajuste local
                adjustLocalVolume(increase)
            } else {
                // Ajuste remoto via API Spotify
                adjustRemoteVolume(deviceId, increase)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Falha ao ajustar volume: ${e.message}")
        }
    }

    private fun isLocalPlayback(deviceId: String): Boolean {
        // se o deviceId pertence ao próprio app Spotify rodando no celular,
        // ele aparece como "is_active = true" e "type = Smartphone"
        return try {
            val token = prefs.getString("spotify_access_token", null) ?: return false
            val req = Request.Builder()
                .url("https://api.spotify.com/v1/me/player/devices")
                .addHeader("Authorization", "Bearer $token")
                .build()

            client.newCall(req).execute().use { res ->
                if (!res.isSuccessful) return false
                val json = JSONObject(res.body?.string() ?: "")
                val devices = json.getJSONArray("devices")
                for (i in 0 until devices.length()) {
                    val device = devices.getJSONObject(i)
                    if (device.optString("id") == deviceId &&
                        device.optString("type").equals("Smartphone", true)) {
                        return true
                    }
                }
            }
            false
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao verificar device local: ${e.message}")
            false
        }
    }

    private fun adjustLocalVolume(increase: Boolean) {
        val audioManager = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
        val current = audioManager.getStreamVolume(AudioManager.STREAM_MUSIC)
        val maxVolume = audioManager.getStreamMaxVolume(AudioManager.STREAM_MUSIC)

        val step = (maxVolume * 0.1).toInt().coerceAtLeast(1)
        val newVolume = if (increase) {
            min(current + step, maxVolume)
        } else {
            max(current - step, 0)
        }

        audioManager.setStreamVolume(AudioManager.STREAM_MUSIC, newVolume, AudioManager.FLAG_SHOW_UI)
        Log.i(TAG, "Volume local ajustado para $newVolume/$maxVolume")
    }

    private suspend fun adjustRemoteVolume(deviceId: String, increase: Boolean) {
        val token = getValidAccessToken()
        val volumePercent = getSpotifyCurrentVolumePercent(deviceId, token)
        val newPercent = if (increase) {
            min(volumePercent + 10, 100)
        } else {
            max(volumePercent - 10, 0)
        }

        val url = "https://api.spotify.com/v1/me/player/volume?volume_percent=$newPercent&device_id=$deviceId"
        val req = Request.Builder()
            .url(url)
            .addHeader("Authorization", "Bearer $token")
            .put("".toRequestBody())
            .build()

        client.newCall(req).execute().use { res ->
            if (res.isSuccessful) {
                Log.i(TAG, "Volume Spotify ajustado para $newPercent%")
            } else {
                Log.e(TAG, "Erro ao ajustar volume Spotify: ${res.code}")
            }
        }
    }

    // ---------------------------
    // Consulta volume atual do Spotify
    // ---------------------------
    private fun getSpotifyCurrentVolumePercent(deviceId: String, token: String): Int {
        return try {
            val req = Request.Builder()
                .url("https://api.spotify.com/v1/me/player?device_id=$deviceId")
                .addHeader("Authorization", "Bearer $token")
                .build()

            client.newCall(req).execute().use { res ->
                if (!res.isSuccessful) return 50 // default
                val json = JSONObject(res.body?.string() ?: "")
                json.optInt("device")?.let {
                    json.getJSONObject("device").optInt("volume_percent", 50)
                } ?: 50
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao pegar volume atual: ${e.message}")
            50
        }
    }

    // ---------------------------
    // Métodos públicos
    // ---------------------------
    suspend fun increaseVolume() = adjustVolume(increase = true)
    suspend fun decreaseVolume() = adjustVolume(increase = false)
}
