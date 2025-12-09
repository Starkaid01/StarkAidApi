package com.starkaid.starkaidapp.ewelink

import android.app.AlertDialog
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.util.Log
import android.widget.Button
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.ewelink.models.EwelinkTokens
import com.starkaid.starkaidapp.security.SecureStorageManager
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import org.json.JSONObject
import java.io.IOException
import java.util.Date
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

class EwelinkLoginActivity : AppCompatActivity() {

    private val clientId = "qPNNDkWlhKwh4xn41bteq2qD02aiGs3D"
    private val clientSecret = "kdG0r5OPddNB90tPKvarWyMWmpppIX9s"
    private val redirectUrl = "https://starkaid.runasp.net/auth/ewelink/calback/callback.html"

    private lateinit var secureStorage: SecureStorageManager
    private lateinit var deviceService: EwelinkDeviceService

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_ewelink_login)

        // Inicializar serviços
        secureStorage = SecureStorageManager(this)
        deviceService = EwelinkDeviceService(secureStorage)

        Log.d("EWE_LOGIN", "EwelinkLoginActivity - onCreate")

        // 🔥 DEBUG: Verificar storage ao abrir a tela
        secureStorage.debugStorage()

        findViewById<Button>(R.id.btnLoginEwelink).setOnClickListener {
            mostrarMensagemConectar()
        }

        // Verificação de autenticação
        verificarEAutenticar()

        processarIntent(intent)
    }

    // 🔥 CORREÇÃO: Função de verificação melhorada
    private fun verificarEAutenticar() {
        Log.d("EWE_LOGIN", "🔍 Verificando autenticação...")
        secureStorage.debugStorage() // ← DEBUG EXTRA

        val tokens = secureStorage.getEwelinkTokens()

        if (tokens == null) {
            Log.d("EWE_LOGIN", "❌ Nenhum token encontrado - usuário não logado")
            return
        }

        Log.d("EWE_LOGIN", "✅ Token encontrado - Verificando validade...")
        debugTokenInfo()

        // 🔥 CORREÇÃO: Margem maior de segurança (10 minutos)
        if (secureStorage.isAccessTokenValidWithMargin(10)) {
            Log.d("EWE_LOGIN", "🎯 Access Token VÁLIDO - Redirecionando...")
            mostrarTelaDispositivos()
        } else if (secureStorage.canRefreshToken()) {
            Log.d("EWE_LOGIN", "🔄 Token expirado mas pode ser renovado...")
            deviceService.refreshTokens(
                onSuccess = {
                    Log.d("EWE_LOGIN", "✅ Token renovado com sucesso")
                    mostrarTelaDispositivos()
                },
                onError = { erro ->
                    Log.e("EWE_LOGIN", "❌ Falha no refresh: $erro")
                    secureStorage.clearEwelinkTokens()
                    runOnUiThread {
                        Toast.makeText(this, "Sessão expirada. Faça login novamente.", Toast.LENGTH_LONG).show()
                        secureStorage.debugStorage() // ← DEBUG APÓS LIMPEZA
                    }
                }
            )
        } else {
            Log.e("EWE_LOGIN", "💀 Refresh Token também expirado")
            secureStorage.clearEwelinkTokens()
            runOnUiThread {
                Toast.makeText(this, "Sessão completamente expirada. Faça login.", Toast.LENGTH_LONG).show()
            }
        }
    }


    // 🔥 FUNÇÃO DE DEBUG: Mostrar informações detalhadas dos tokens
    private fun debugTokenInfo() {
        val tokens = secureStorage.getEwelinkTokens()
        if (tokens != null) {
            Log.d("EWE_DEBUG", "=== DEBUG TOKEN INFO ===")
            Log.d("EWE_DEBUG", "Access Token: ${tokens.accessToken.take(15)}...")
            Log.d("EWE_DEBUG", "Refresh Token: ${tokens.refreshToken.take(15)}...")
            Log.d("EWE_DEBUG", "Access Expira: ${Date(tokens.atExpiredTime)}")
            Log.d("EWE_DEBUG", "Refresh Expira: ${Date(tokens.rtExpiredTime)}")
            Log.d("EWE_DEBUG", "Região: ${tokens.region}")
            Log.d("EWE_DEBUG", "Tempo atual: ${Date()}")
            Log.d("EWE_DEBUG", "Access Válido: ${tokens.atExpiredTime > System.currentTimeMillis()}")
            Log.d("EWE_DEBUG", "Refresh Válido: ${tokens.rtExpiredTime > System.currentTimeMillis()}")
            Log.d("EWE_DEBUG", "Com margem 5min: ${secureStorage.isAccessTokenValidWithMargin(5)}")
            Log.d("EWE_DEBUG", "=== FIM DEBUG ===")
        } else {
            Log.d("EWE_DEBUG", "Nenhum token para debug")
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        Log.d("EWE", "EwelinkLoginActivity - onNewIntent")
        Log.d("EWE", "New Intent data: ${intent.data}")
        setIntent(intent)
        processarIntent(intent)
    }

    private fun processarIntent(intent: Intent) {
        val data = intent.data ?: return

        Log.d("EWE", "Processando intent: $data")

        when {
            data.scheme == "starkaid" && data.host == "ewelink" -> {
                val code = data.getQueryParameter("code")
                val region = data.getQueryParameter("region") ?: "as"
                if (code != null) {
                    Log.d("EWE", "DEEP LINK RECEBIDO! CODE=$code, REGION=$region")
                    trocarCodePorToken(code, region)
                }
            }
            data.toString().startsWith(redirectUrl) -> {
                val code = data.getQueryParameter("code")
                val region = data.getQueryParameter("region") ?: "as"
                if (code != null) {
                    Log.d("EWE", "HTTP CALLBACK RECEBIDO! CODE=$code, REGION=$region")
                    trocarCodePorToken(code, region)
                }
            }
        }
    }

    private fun mostrarMensagemConectar() {
        val mensagem = """
            Conectar Ewelink:

            Para conectar sua conta ewelink,
            
            acesse o link abaixo,
            
            Faça login na plataforma
            
            Clique em Dispositivos Ewelink
            
            Faça login com sua conta Ewelink
            
            Volte ao Aplicativo e veja se seus dispositivos aparecem.
            
            Link: https://starkaid.runasp.net/automacao.html?
        """.trimIndent()

        AlertDialog.Builder(this)
            .setTitle("Conectar Conta Ewelink")
            .setMessage(mensagem)
            .setPositiveButton("OK") { dialog, _ ->
                dialog.dismiss()
            }
            .setCancelable(true)
            .show()
    }

    private fun iniciarLogin() {
        val seq = System.currentTimeMillis().toString()
        val nonce = generateNonce(8)
        val state = "12345"

        val message = "${clientId}_${seq}"
        val sign = hmac(clientSecret, message)

        Log.d("EWE", "Iniciando login...")

        val authUrl = Uri.parse("https://c2ccdn.coolkit.cc/oauth/index.html")
            .buildUpon()
            .appendQueryParameter("clientId", clientId)
            .appendQueryParameter("seq", seq)
            .appendQueryParameter("authorization", sign)
            .appendQueryParameter("redirectUrl", redirectUrl)
            .appendQueryParameter("grantType", "authorization_code")
            .appendQueryParameter("state", state)
            .appendQueryParameter("nonce", nonce)
            .appendQueryParameter("showQRCode", "false")
            .build()

        Log.d("EWE", "URL de Auth: $authUrl")

        try {
            val intent = Intent(Intent.ACTION_VIEW, authUrl)
            startActivity(intent)
        } catch (e: Exception) {
            Log.e("EWE", "Erro ao abrir navegador: ${e.message}")
            Toast.makeText(this, "Erro ao abrir navegador", Toast.LENGTH_LONG).show()
        }
    }

    private fun trocarCodePorToken(code: String, region: String) {
        Log.d("EWE", "=== TROCANDO CODE POR TOKEN ===")

        val tokenUrl = when(region) {
            "us" -> "https://us-apia.coolkit.cc/v2/user/oauth/token"
            "eu" -> "https://eu-apia.coolkit.cc/v2/user/oauth/token"
            "cn" -> "https://cn-apia.coolkit.cn/v2/user/oauth/token"
            else -> "https://as-apia.coolkit.cc/v2/user/oauth/token"
        }

        val timestamp = System.currentTimeMillis()
        val nonce = generateNonce(8)

        val bodyJson = """
        {
            "clientId": "$clientId",
            "clientSecret": "$clientSecret",
            "code": "$code",
            "grantType": "authorization_code",
            "redirectUrl": "$redirectUrl"
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

        OkHttpClient().newCall(request).enqueue(object : Callback {
            override fun onFailure(call: Call, e: IOException) {
                Log.e("EWE", "Token error: ${e.message}")
                runOnUiThread {
                    Toast.makeText(this@EwelinkLoginActivity, "Erro de rede: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }

            override fun onResponse(call: Call, response: Response) {
                val raw = response.body?.string() ?: ""
                Log.d("EWE", "TOKEN RESPONSE: $raw")

                try {
                    val json = JSONObject(raw)
                    val error = json.optInt("error")

                    if (error != 0) {
                        val errorMsg = json.optString("msg", "Erro desconhecido")
                        Log.e("EWE", "API Error $error: $errorMsg")
                        runOnUiThread {
                            Toast.makeText(this@EwelinkLoginActivity, "Erro: $errorMsg", Toast.LENGTH_LONG).show()
                        }
                        return
                    }

                    val data = json.getJSONObject("data")
                    val accessToken = data.getString("accessToken")
                    val refreshToken = data.getString("refreshToken")
                    val atExpiredTime = data.getLong("atExpiredTime")
                    val rtExpiredTime = data.getLong("rtExpiredTime")

                    // 🔐 Salvar tokens de forma segura
                    val tokens = EwelinkTokens(
                        accessToken,
                        refreshToken,
                        atExpiredTime,
                        rtExpiredTime,
                        region
                    )
                    secureStorage.saveEwelinkTokens(tokens)

                    Log.d("EWE", "✅ TOKEN OBTIDO E SALVO COM SUCESSO")

                    runOnUiThread {
                        Toast.makeText(this@EwelinkLoginActivity, "Login realizado com sucesso!", Toast.LENGTH_SHORT).show()
                        mostrarTelaDispositivos()
                    }

                } catch (e: Exception) {
                    Log.e("EWE", "JSON Parse error: ${e.message}")
                    runOnUiThread {
                        Toast.makeText(this@EwelinkLoginActivity, "Erro ao processar resposta", Toast.LENGTH_LONG).show()
                    }
                }
            }
        })
    }

    private fun mostrarTelaDispositivos() {
        val intent = Intent(this, EwelinkDevicesActivity::class.java)
        startActivity(intent)
        finish() // Fecha a tela de login para não voltar para ela
    }

    private fun listarFamiliasEListarDispositivos() {
        val tokens = secureStorage.getEwelinkTokens() ?: return

        // Primeiro listar famílias, depois dispositivos da primeira família
        listarFamilias(tokens.accessToken, tokens.region) { familyId ->
            deviceService.listarDispositivos(familyId,
                onSuccess = { dispositivos ->
                    runOnUiThread {
                        Log.d("EWE", "✅ ${dispositivos.size} dispositivos carregados")
                        // Aqui você pode atualizar a UI com a lista de dispositivos
                        dispositivos.forEach { dispositivo ->
                            Log.d("EWE", "📱 ${dispositivo.name} (${dispositivo.id}) - Online: ${dispositivo.online}")
                        }
                    }
                },
                onError = { error ->
                    runOnUiThread {
                        Toast.makeText(this, "Erro: $error", Toast.LENGTH_LONG).show()
                    }
                }
            )
        }
    }

    // Exemplo de como controlar um dispositivo
    private fun exemploControlarDispositivo(deviceId: String) {
        val params = JSONObject().apply {
            put("switch", "on") // ou "off" para desligar
        }

        deviceService.controlarDispositivo(deviceId, params,
            onSuccess = {
                runOnUiThread {
                    Toast.makeText(this, "Dispositivo controlado com sucesso!", Toast.LENGTH_SHORT).show()
                }
            },
            onError = { error ->
                runOnUiThread {
                    Toast.makeText(this, "Erro: $error", Toast.LENGTH_LONG).show()
                }
            }
        )
    }

    // 🔓 Logout
    private fun fazerLogout() {
        secureStorage.clearEwelinkTokens()
        Toast.makeText(this, "Logout realizado", Toast.LENGTH_SHORT).show()
        // Redirecionar para tela de login
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

    private fun listarFamilias(accessToken: String, region: String, onFamilySelected: (String) -> Unit) {
        val timestamp = System.currentTimeMillis()
        val nonce = generateNonce(8)

        val url = when(region) {
            "us" -> "https://us-apia.coolkit.cc/v2/family"
            "eu" -> "https://eu-apia.coolkit.cc/v2/family"
            "cn" -> "https://cn-apia.coolkit.cn/v2/family"
            else -> "https://as-apia.coolkit.cc/v2/family"
        }

        val request = Request.Builder()
            .url(url)
            .get()
            .addHeader("Authorization", "Bearer $accessToken")
            .addHeader("X-CK-Appid", clientId)
            .addHeader("X-CK-Nonce", nonce)
            .addHeader("X-CK-Timestamp", timestamp.toString())
            .build()

        OkHttpClient().newCall(request).enqueue(object : Callback {
            override fun onFailure(call: Call, e: IOException) {
                Log.e("EWE", "Erro familias: ${e.message}")
            }

            override fun onResponse(call: Call, response: Response) {
                val responseBody = response.body?.string() ?: ""
                Log.d("EWE", "FAMILIAS RESPONSE: ${response.code} - $responseBody")

                if (response.isSuccessful) {
                    try {
                        val json = JSONObject(responseBody)
                        val data = json.getJSONObject("data")
                        val currentFamilyId = data.getString("currentFamilyId")
                        onFamilySelected(currentFamilyId)
                    } catch (e: Exception) {
                        Log.e("EWE", "Erro ao processar famílias: ${e.message}")
                    }
                }
            }
        })
    }
}