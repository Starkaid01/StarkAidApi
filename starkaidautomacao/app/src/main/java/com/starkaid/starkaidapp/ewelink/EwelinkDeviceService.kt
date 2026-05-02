package com.starkaid.starkaidapp.ewelink

import android.util.Log
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.ewelink.models.EwelinkDevice
import com.starkaid.starkaidapp.ewelink.models.EwelinkTokens
import com.starkaid.starkaidapp.security.SecureStorageManager
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import org.json.JSONArray
import org.json.JSONObject
import java.io.IOException
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

class EwelinkDeviceService(private val secureStorage: SecureStorageManager) {

    private val clientId = ApiConfig.ewelinkClientId
    private val clientSecret = ApiConfig.ewelinkClientSecret

    // 🔄 Função para refresh token
    fun refreshTokens(onSuccess: (EwelinkTokens) -> Unit, onError: (String) -> Unit) {
        val tokens = secureStorage.getEwelinkTokens() ?: run {
            onError("Nenhum token encontrado")
            return
        }

        // 🔥 CORREÇÃO: Verificar se o refresh token é válido
        if (tokens.rtExpiredTime <= System.currentTimeMillis()) {
            onError("Refresh token expirado")
            return
        }

        val region = tokens.region
        val refreshToken = tokens.refreshToken

        Log.d("EWE", "🔃 Iniciando refresh token...")



        val tokenUrl = when(region) {
            "us" -> "https://us-apia.coolkit.cc/v2/user/oauth/refresh"
            "eu" -> "https://eu-apia.coolkit.cc/v2/user/oauth/refresh"
            "cn" -> "https://cn-apia.coolkit.cn/v2/user/oauth/refresh"
            else -> "https://as-apia.coolkit.cc/v2/user/oauth/refresh"
        }

        val timestamp = System.currentTimeMillis()
        val nonce = generateNonce(8)

        val bodyJson = """
        {
            "clientId": "$clientId",
            "clientSecret": "$clientSecret",
            "grantType": "refresh_token",
            "refreshToken": "$refreshToken"
        }
        """.trimIndent()

        val sign = hmac(clientSecret, bodyJson)

        val request = Request.Builder()
            .url(tokenUrl)
            .post(RequestBody.create("application/json".toMediaTypeOrNull(), bodyJson))
            .addHeader("X-CK-Appid", clientId)
            .addHeader("X-CK-Nonce", nonce)
            .addHeader("X-CK-Timestamp", timestamp.toString())
            .addHeader("Authorization", "Sign $sign")
            .build()

        Log.d("EWE", "Refresh Token Request: $tokenUrl")

        OkHttpClient().newCall(request).enqueue(object : Callback {
            override fun onFailure(call: Call, e: IOException) {
                Log.e("EWE", "Refresh token error: ${e.message}")
                onError("Erro de rede: ${e.message}")
            }

            override fun onResponse(call: Call, response: Response) {
                val raw = response.body?.string() ?: ""
                Log.d("EWE", "REFRESH TOKEN RESPONSE: ${response.code} - $raw")

                try {
                    val json = JSONObject(raw)
                    val error = json.optInt("error")

                    if (error != 0) {
                        val errorMsg = json.optString("msg", "Erro desconhecido")
                        Log.e("EWE", "Refresh token API Error $error: $errorMsg")

                        // 🔥 CORREÇÃO: Se for erro de token inválido, limpar os tokens
                        if (error == 401 || errorMsg.contains("token", ignoreCase = true)) {
                            secureStorage.clearEwelinkTokens()
                        }

                        onError(errorMsg)
                        return
                    }

                    val data = json.getJSONObject("data")
                    val newAccessToken = data.getString("accessToken")
                    val newRefreshToken = data.getString("refreshToken")
                    val atExpiredTime = data.getLong("atExpiredTime")
                    val rtExpiredTime = data.getLong("rtExpiredTime")

                    val newTokens = EwelinkTokens(newAccessToken, newRefreshToken, atExpiredTime, rtExpiredTime, region)
                    secureStorage.saveEwelinkTokens(newTokens)

                    Log.d("EWE", "✅ TOKENS ATUALIZADOS COM SUCESSO")
                    Log.d("EWE", "Novo Access Token Expira: $atExpiredTime")
                    Log.d("EWE", "Novo Refresh Token Expira: $rtExpiredTime")

                    onSuccess(newTokens)

                } catch (e: Exception) {
                    Log.e("EWE", "JSON Parse error: ${e.message}")
                    onError("Erro ao processar resposta")
                }
            }
        })
    }

    // 📱 Listar dispositivos de uma família
    fun listarDispositivos(familyId: String, onSuccess: (List<EwelinkDevice>) -> Unit, onError: (String) -> Unit) {
        val tokens = secureStorage.getEwelinkTokens() ?: run {
            onError("Usuário não autenticado")
            return
        }

        if (secureStorage.isAccessTokenExpired()) {
            refreshTokens(
                onSuccess = { newTokens ->
                    fazerRequisicaoDispositivos(newTokens.accessToken, newTokens.region, familyId, onSuccess, onError)
                },
                onError = { error ->
                    onError("Token expirado: $error")
                }
            )
            return
        }

        fazerRequisicaoDispositivos(tokens.accessToken, tokens.region, familyId, onSuccess, onError)
    }

    private fun fazerRequisicaoDispositivos(
        accessToken: String,
        region: String,
        familyId: String,
        onSuccess: (List<EwelinkDevice>) -> Unit,
        onError: (String) -> Unit
    ) {
        val timestamp = System.currentTimeMillis()
        val nonce = generateNonce(8)

        val url = when(region) {
            "us" -> "https://us-apia.coolkit.cc/v2/device/thing"
            "eu" -> "https://eu-apia.coolkit.cc/v2/device/thing"
            "cn" -> "https://cn-apia.coolkit.cn/v2/device/thing"
            else -> "https://as-apia.coolkit.cc/v2/device/thing"
        }

        val request = Request.Builder()
            .url("$url?num=0&familyId=$familyId")
            .get()
            .addHeader("Authorization", "Bearer $accessToken")
            .addHeader("X-CK-Appid", clientId)
            .addHeader("X-CK-Nonce", nonce)
            .addHeader("X-CK-Timestamp", timestamp.toString())
            .build()

        Log.d("EWE", "Dispositivos Request: $url?num=0&familyId=$familyId")

        OkHttpClient().newCall(request).enqueue(object : Callback {
            override fun onResponse(call: Call, response: Response) {
                val responseBody = response.body?.string() ?: ""
                Log.d("EWE", "DISPOSITIVOS RESPONSE: ${response.code} - $responseBody")

                try {
                    val json = JSONObject(responseBody)
                    val error = json.optInt("error")

                    if (error != 0) {
                        val errorMsg = json.optString("msg", "Erro desconhecido")
                        onError(errorMsg)
                        return
                    }

                    val data = json.getJSONObject("data")
                    val thingList = data.getJSONArray("thingList")
                    val dispositivos = mutableListOf<EwelinkDevice>()

                    for (i in 0 until thingList.length()) {
                        val item = thingList.getJSONObject(i)
                        val itemData = item.getJSONObject("itemData")
                        val extra = itemData.getJSONObject("extra")

                        val dispositivo = EwelinkDevice(
                            id = itemData.getString("deviceid"),
                            name = itemData.getString("name"),
                            type = item.optInt("itemType", 1), // Usar itemType do item principal
                            uiid = extra.getInt("uiid"), // uiid está dentro de "extra"
                            params = itemData.optJSONObject("params") ?: JSONObject(),
                            online = itemData.getBoolean("online"),
                            familyId = familyId,
                            roomId = itemData.optString("roomid", "")
                        )
                        dispositivos.add(dispositivo)
                    }

                    Log.d("EWE", "✅ ${dispositivos.size} dispositivos encontrados")
                    onSuccess(dispositivos)

                } catch (e: Exception) {
                    Log.e("EWE", "Erro ao processar dispositivos: ${e.message}")
                    Log.e("EWE", "Stack trace: ${e.stackTraceToString()}")
                    onError("Erro ao processar resposta: ${e.message}")
                }
            }

            override fun onFailure(call: Call, e: IOException) {
                Log.e("EWE", "Erro dispositivos: ${e.message}")
                onError("Erro de rede: ${e.message}")
            }
        })
    }

    // 🔌 Controlar dispositivo (ligar/desligar)
    // 🔌 Controlar dispositivo (ligar/desligar) - VERSÃO CORRIGIDA
    fun controlarDispositivo(deviceId: String, params: JSONObject, onSuccess: () -> Unit, onError: (String) -> Unit) {
        val tokens = secureStorage.getEwelinkTokens() ?: run {
            onError("Usuário não autenticado")
            return
        }

        if (secureStorage.isAccessTokenExpired()) {
            refreshTokens(
                onSuccess = { newTokens ->
                    fazerRequisicaoControle(newTokens.accessToken, newTokens.region, deviceId, params, onSuccess, onError)
                },
                onError = { error ->
                    onError("Token expirado: $error")
                }
            )
            return
        }

        fazerRequisicaoControle(tokens.accessToken, tokens.region, deviceId, params, onSuccess, onError)
    }

    private fun fazerRequisicaoControle(
        accessToken: String,
        region: String,
        deviceId: String,
        params: JSONObject,
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {
        val timestamp = System.currentTimeMillis()
        val nonce = generateNonce(8)

        val url = when(region) {
            "us" -> "https://us-apia.coolkit.cc/v2/device/thing/status"
            "eu" -> "https://eu-apia.coolkit.cc/v2/device/thing/status"
            "cn" -> "https://cn-apia.coolkit.cn/v2/device/thing/status"
            else -> "https://as-apia.coolkit.cc/v2/device/thing/status"
        }

        // 🔥 CORREÇÃO: Estrutura correta para a API eWeLink
        val bodyJson = """
    {
        "type": 1,
        "id": "$deviceId",
        "params": $params
    }
    """.trimIndent()

        val request = Request.Builder()
            .url(url)
            .post(RequestBody.create("application/json".toMediaTypeOrNull(), bodyJson))
            .addHeader("Authorization", "Bearer $accessToken")
            .addHeader("X-CK-Appid", clientId)
            .addHeader("X-CK-Nonce", nonce)
            .addHeader("X-CK-Timestamp", timestamp.toString())
            .build()

        Log.d("EWE", "Controlar Dispositivo Request: $url")
        Log.d("EWE", "Body: $bodyJson")

        OkHttpClient().newCall(request).enqueue(object : Callback {
            override fun onFailure(call: Call, e: IOException) {
                Log.e("EWE", "Erro controle dispositivo: ${e.message}")
                onError("Erro de rede: ${e.message}")
            }

            override fun onResponse(call: Call, response: Response) {
                val responseBody = response.body?.string() ?: ""
                Log.d("EWE", "CONTROLE DISPOSITIVO RESPONSE: ${response.code} - $responseBody")

                try {
                    val json = JSONObject(responseBody)
                    val error = json.optInt("error")

                    if (error != 0) {
                        val errorMsg = json.optString("msg", "Erro desconhecido")
                        onError(errorMsg)
                        return
                    }

                    Log.d("EWE", "✅ Dispositivo controlado com sucesso")
                    onSuccess()

                } catch (e: Exception) {
                    Log.e("EWE", "Erro ao processar controle: ${e.message}")
                    onError("Erro ao processar resposta")
                }
            }
        })
    }

    // 🛠️ Utilitários
    private fun hmac(secret: String, message: String): String {
        val mac = Mac.getInstance("HmacSHA256")
        val keySpec = SecretKeySpec(secret.toByteArray(), "HmacSHA256")
        mac.init(keySpec)
        return android.util.Base64.encodeToString(mac.doFinal(message.toByteArray()), android.util.Base64.NO_WRAP)
    }

    private fun generateNonce(length: Int): String {
        val allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        return (1..length).map { allowedChars.random() }.joinToString("")
    }
}
