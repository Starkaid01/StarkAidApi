package com.starkaid.starkaidapp.data

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import com.starkaid.starkaidapp.models.SpotifyWebApi
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import androidx.core.content.edit

class SessionManager(context: Context) {
    private val prefs: SharedPreferences = context.getSharedPreferences("starkaid_prefs", Context.MODE_PRIVATE)


    companion object {
        @Volatile
        private var INSTANCE: SessionManager? = null

        private const val PREFS_NAME = "spotify_session"
        private const val KEY_ACCESS_TOKEN = "spotify_access_token"
        private const val KEY_REFRESH_TOKEN = "spotify_refresh_token"
        private const val KEY_EXPIRES_AT = "spotify_expires_at"

        fun getInstance(context: Context): SessionManager {
            return INSTANCE ?: synchronized(this) {
                INSTANCE ?: SessionManager(context.applicationContext).also { INSTANCE = it }
            }
        }
    }



    fun saveUserId(id: String) {
        prefs.edit().putString("USER_ID", id).apply()
    }

    fun fetchUserId(): String? {
        return prefs.getString("USER_ID", null)
    }

    fun saveApiKey(apiKey: String) {
        prefs.edit().putString("API_KEY", apiKey).apply()
    }

    fun fetchApiKey(): String? {
        return prefs.getString("API_KEY", null)
    }

    fun saveFcmToken(token: String) {
        prefs.edit().putString("FCM_TOKEN", token).apply()
    }

// --Commented out by Inspection START (20/08/2025 14:16):
//    fun fetchFcmToken(): String? {
//        return prefs.getString("FCM_TOKEN", null)
//    }
// --Commented out by Inspection STOP (20/08/2025 14:16)

    fun saveQrLogged(token: String) {
        prefs.edit().putString("QR_LOGGED", token).apply()
    }

    fun fetchQrLogged(): String? {
        return prefs.getString("QR_LOGGED", null)
    }

    fun saveAuthToken(token: String) {
        prefs.edit().putString("ACCESS_TOKEN", token).apply()
    }

    fun fetchAuthToken(): String? {
        return prefs.getString("ACCESS_TOKEN", null)
    }

    fun saveRefreshToken(token: String) {
        prefs.edit().putString("REFRESH_TOKEN", token).apply()
    }

    fun fetchRefreshToken(): String? {
        return prefs.getString("REFRESH_TOKEN", null)
    }



    // 👇 sirene
    fun isSireneAtivada(): Boolean {
        return prefs.getBoolean("SIRENE_ATIVADA", true)
    }

    fun setSireneAtivada(ativada: Boolean) {
        prefs.edit().putBoolean("SIRENE_ATIVADA", ativada).apply()
    }

    fun isSessionExpired(): Boolean {
        return prefs.getBoolean("session_expired", false)
    }

    fun clearSessionExpired() {
        val prefs = prefs.edit()
        prefs.remove("session_expired")
        prefs.apply()
    }

    fun saveUserRole(role: String) {
        prefs.edit().putString("USER_ROLE", role).apply()
    }

    fun fetchUserRole(): String? {
        return prefs.getString("USER_ROLE", null)
    }

    fun saveUserEmail(role: String) {
        prefs.edit().putString("USER_EMAIL", role).apply()
    }

    fun fetchUserEmail(): String? {
        return prefs.getString("USER_EMAIL", null)
    }

    fun saveUserName(role: String) {
        prefs.edit().putString("USER_NAME", role).apply()
    }

    fun fetchUserName(): String? {
        return prefs.getString("USER_NAME", null)
    }

    fun saveContNv1(count: Int) {
        prefs.edit().putInt("CONT_NV1", count).apply()
    }

    fun fetchContNv1(): Int {
        return prefs.getInt("CONT_NV1", 0)
    }

    fun saveLastResetDate(date: String) {
        prefs.edit().putString("LAST_RESET_DATE", date).apply()
    }

    fun fetchLastResetDate(): String? {
        return prefs.getString("LAST_RESET_DATE", null)
    }

    fun clearTokens() {
        prefs.edit().apply {
            remove("ACCESS_TOKEN")   // Chave do token de acesso
            remove("REFRESH_TOKEN")  // Chave do refresh token
            remove("API_KEY")        // Chave da API Key
            apply()
        }
        Log.d("SessionManager", "Tokens limpos")
    }

    fun saveAdCounter(value: Int) {
        prefs.edit().putInt("ad_counter", value).apply()
    }

    fun fetchAdCounter(): Int {
        return prefs.getInt("ad_counter", 0)
    }

    fun saveAppOpenCount(value: Int) {
        prefs.edit().putInt("app_open_count", value).apply()
    }

    fun fetchAppOpenCount(): Int {
        return prefs.getInt("app_open_count", 0)
    }

    fun saveLastCloseTime(time: Long) {
        prefs.edit().putLong("last_close_time", time).apply()
    }

    fun fetchLastCloseTime(): Long {
        return prefs.getLong("last_close_time", 0)
    }




    fun saveCurrentArtist(artist: String) {
        Log.d("SessionManager", "Salvando artista: $artist")
        prefs.edit().putString("ARTISTA_ATUAL", artist).apply()
    }

    fun getCurrentArtist(): String? {
        val artist = prefs.getString("ARTISTA_ATUAL", null)
        Log.d("SessionManager", "Recuperando artista: $artist")
        return artist
    }


    // ---------- Spotify Token ----------
    fun saveSpotifyAccessToken(token: String) {
        prefs.edit().putString("spotify_access_token", token).apply()
    }



    fun getCurrentTrack(): String? {
        return prefs.getString("CURRENT_TRACK", null)
    }


    fun getSpotifyTokenExpiresAt(): Long? {
        return prefs.getLong("spotify_token_expires_at", 0L).takeIf { it != 0L }
    }


    // ---------- Track atual ----------
    fun saveCurrentTrack(track: String) {
        prefs.edit().putString("CURRENT_TRACK", track).apply()
        Log.d("SessionManager", "Track atual salvo: $track")
    }

    fun saveSpotifyTokens(accessToken: String, refreshToken: String?, expiresIn: Int) {
        val expiresAt = System.currentTimeMillis() + (expiresIn * 1000L)
        prefs.edit()
            .putString(KEY_ACCESS_TOKEN, accessToken)
            .putString(KEY_REFRESH_TOKEN, refreshToken)
            .putLong(KEY_EXPIRES_AT, expiresAt)
            .apply()
    }

    fun getSpotifyAccessToken(): String? {
        val token = prefs.getString(KEY_ACCESS_TOKEN, null)
        val expiresAt = prefs.getLong(KEY_EXPIRES_AT, 0)
        return if (token != null && System.currentTimeMillis() < expiresAt) {
            token
        } else {
            null
        }
    }

    fun getSpotifyRefreshToken(): String? {
        return prefs.getString(KEY_REFRESH_TOKEN, null)
    }

    suspend fun getValidAccessToken(context: Context): String? {
        val currentToken = getSpotifyAccessToken()
        if (currentToken != null) return currentToken

        val refreshToken = getSpotifyRefreshToken()
        if (refreshToken != null) {
            return try {
                val newTokens = withContext(Dispatchers.IO) {
                    SpotifyWebApi.refreshToken(refreshToken, context)
                }
                saveSpotifyTokens(
                    newTokens.accessToken,
                    newTokens.refreshToken ?: refreshToken,
                    newTokens.expiresIn
                )
                newTokens.accessToken
            } catch (e: Exception) {
                Log.e("SessionManager", "Erro ao renovar token", e)
                null
            }
        }
        return null
    }

    fun clearSession() {
        prefs.edit { clear() }
    }


    // Refresh token
    fun saveSpotifyRefreshToken(token: String) {
        prefs.edit { putString("spotify_refresh_token", token) }
    }


    fun saveAssistantName(nameAssistent: String) {
        prefs.edit().putString("NAME_ASSISTENT", nameAssistent).apply()
        Log.d("SessionManager", "Nome do assistente salvo: '$nameAssistent'")
    }

    fun fetchAssistantName(): String? {
        val name = prefs.getString("NAME_ASSISTENT", null)
        Log.d("SessionManager", "Nome do assistente recuperado: '$name'")
        return name
    }

    fun saveDefaultResponse(respostaAssistent: String) {
        prefs.edit().putString("RESPOSTA_ASSISTENT", respostaAssistent).apply()
        Log.d("SessionManager", "Resposta padrão salva: '$respostaAssistent'")
    }

    fun fetchDefaultResponse(): String? {
        return prefs.getString("RESPOSTA_ASSISTENT", null)
    }

    fun saveAssistantPerson(nameAssistent: String) {
        prefs.edit().putString("PERSON_ASSISTENT", nameAssistent).apply()
    }

    fun fetchAssistantPerson(): String? {
        return prefs.getString("PERSON_ASSISTENT", null)
    }

    // ---------- App Config ----------
    fun saveApiBaseUrl(baseUrl: String) {
        prefs.edit().putString("API_BASE_URL", baseUrl).apply()
    }

    fun fetchApiBaseUrl(): String? {
        return prefs.getString("API_BASE_URL", null)
    }

    fun saveSpotifyClientId(clientId: String) {
        prefs.edit().putString("SPOTIFY_CLIENT_ID", clientId).apply()
    }

    fun fetchSpotifyClientId(): String? {
        return prefs.getString("SPOTIFY_CLIENT_ID", null)
    }

    fun saveSpotifyClientSecret(clientSecret: String) {
        prefs.edit().putString("SPOTIFY_CLIENT_SECRET", clientSecret).apply()
    }

    fun fetchSpotifyClientSecret(): String? {
        return prefs.getString("SPOTIFY_CLIENT_SECRET", null)
    }

    fun saveEwelinkClientId(clientId: String) {
        prefs.edit().putString("EWELINK_CLIENT_ID", clientId).apply()
    }

    fun fetchEwelinkClientId(): String? {
        return prefs.getString("EWELINK_CLIENT_ID", null)
    }

    fun saveEwelinkClientSecret(clientSecret: String) {
        prefs.edit().putString("EWELINK_CLIENT_SECRET", clientSecret).apply()
    }

    fun fetchEwelinkClientSecret(): String? {
        return prefs.getString("EWELINK_CLIENT_SECRET", null)
    }

    fun saveEwelinkRedirectUri(redirectUri: String) {
        prefs.edit().putString("EWELINK_REDIRECT_URI", redirectUri).apply()
    }

    fun fetchEwelinkRedirectUri(): String? {
        return prefs.getString("EWELINK_REDIRECT_URI", null)
    }

}
