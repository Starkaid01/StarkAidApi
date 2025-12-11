package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

// DTOs para autenticação
data class AuthRequest(
    val email: String,
    val password: String,
    val origem: String = "app" // default "app"
)

data class UserInfo(
    val id: String,
    val name: String,
    val email: String,
    val apiKey: String,
    val starkCoins: Double
)

data class AuthResponse(
    val token: String,
    val refreshToken: String,
    val user: UserInfo? = null,
    // Mantido para compatibilidade com código antigo
    val id: String? = null,
    val apiKey: String? = null
)

// --Commented out by Inspection START (20/08/2025 14:16):
//data class RefreshTokenRequest(
//    val refreshToken: String
//)
// --Commented out by Inspection STOP (20/08/2025 14:16)

data class RegistrarTokenRequest(
    val fcmToken: String
)

// Retrofit interface
interface AuthApi {
    @POST("api/v1/Notificacoes/registrar-token")
    suspend fun registrarToken(@Body request: RegistrarTokenRequest): Response<Void>

    @POST("api/v1/Auth/login")
    suspend fun login(@Body request: AuthRequest): Response<AuthResponse>

//    @POST("api/v1/Auth/refresh-token")
//    suspend fun refreshToken(@Body request: RefreshTokenRequest): Response<AuthResponse>

    @POST("api/v1/Users/request-password-reset")
    suspend fun requestPasswordReset(@Body request: Map<String, String>): Response<String>
}

class AuthService(context: Context) {
    // --Commented out by Inspection (20/08/2025 13:57):private val sessionManager = SessionManager(context)
    private val api = ApiClient.getClient(context).create(AuthApi::class.java)

    suspend fun login(email: String, password: String): AuthResponse? = withContext(Dispatchers.IO) {
        try {
            val response = api.login(AuthRequest(email, password, origem = "app"))
            if (response.isSuccessful) {
                val body = response.body()
                Log.d("AuthService", "Login successful: $body")
                return@withContext body
            } else {
                val errorBody = response.errorBody()?.string()
                Log.e("AuthService", "Login failed: code=${response.code()} body=$errorBody")
            }
            return@withContext null
        } catch (e: Exception) {
            Log.e("AuthService", "Exception during login", e)
            return@withContext null
        }
    }

//    suspend fun refreshToken(): Boolean = withContext(Dispatchers.IO) {
//        val refreshToken = sessionManager.fetchRefreshToken() ?: return@withContext false
//
//        try {
//            val response = api.refreshToken(RefreshTokenRequest(refreshToken))
//            if (response.isSuccessful) {
//                response.body()?.let {
//                    sessionManager.saveAuthToken(it.token)
//                    sessionManager.saveRefreshToken(it.refreshToken)
//                    return@withContext true
//                }
//            }
//        } catch (e: Exception) {
//            Log.e("AuthService", "Falha ao renovar token", e)
//        }
//        return@withContext false
//    }
//
//    suspend fun isValidToken(): Boolean = withContext(Dispatchers.IO) {
//        val token = sessionManager.fetchAuthToken()
//        if (token.isNullOrEmpty()) return@withContext false
//
//        try {
//            val parts = token.split(".")
//            if (parts.size < 2) return@withContext false
//
//            val payload = String(
//                Base64.decode(parts[1], Base64.URL_SAFE or Base64.NO_WRAP or Base64.NO_PADDING),
//                Charsets.UTF_8
//            )
//            val json = JSONObject(payload)
//            val exp = json.getLong("exp") * 1000
//            return@withContext System.currentTimeMillis() < exp
//        } catch (e: Exception) {
//            Log.e("AuthService", "Erro ao validar token", e)
//            return@withContext false
//        }
//    }


}