package com.starkaid.starkaidapp.security

import android.content.Context
import android.content.SharedPreferences
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Log
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import com.google.gson.Gson
import com.google.gson.JsonSyntaxException
import com.starkaid.starkaidapp.ewelink.models.EwelinkTokens
import java.util.*

class SecureStorageManager(private val context: Context) {

    private val sharedPreferences: SharedPreferences
    private val useEncryptedStorage: Boolean

    init {
        // 🔥 CORREÇÃO: Tentar usar EncryptedSharedPreferences, com fallback para SharedPreferences normal
        val (prefs, encrypted) = initializePreferences()
        sharedPreferences = prefs
        useEncryptedStorage = encrypted

        Log.d("EWE_STORAGE", "✅ SecureStorageManager inicializado - Encrypted: $useEncryptedStorage")
    }

    private fun initializePreferences(): Pair<SharedPreferences, Boolean> {
        return try {
            val masterKey = MasterKey.Builder(context)
                .setKeyGenParameterSpec(
                    KeyGenParameterSpec.Builder(
                        MasterKey.DEFAULT_MASTER_KEY_ALIAS,
                        KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
                    )
                        .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                        .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                        .setKeySize(256)
                        .build()
                )
                .build()

            val encryptedPrefs = EncryptedSharedPreferences.create(
                context,
                "secure_ewelink_storage",
                masterKey,
                EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
                EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
            )

            Pair(encryptedPrefs, true)
        } catch (e: Exception) {
            Log.e("EWE_STORAGE", "❌ EncryptedSharedPreferences falhou, usando SharedPreferences normal: ${e.message}")

            // Fallback para SharedPreferences normal
            val normalPrefs = context.getSharedPreferences("ewelink_storage_fallback", Context.MODE_PRIVATE)
            Pair(normalPrefs, false)
        }
    }

    fun saveEwelinkTokens(tokens: EwelinkTokens) {
        try {
            val json = Gson().toJson(tokens)
            sharedPreferences.edit().putString(KEY_EWELINK_TOKENS_JSON, json).apply()

            Log.d("EWE_STORAGE", "✅ Tokens salvos com sucesso (Encrypted: $useEncryptedStorage)")
            Log.d("EWE_STORAGE", "Access Token: ${tokens.accessToken.take(10)}...")
            Log.d("EWE_STORAGE", "Expira em: ${Date(tokens.atExpiredTime)}")
            Log.d("EWE_STORAGE", "Tempo atual: ${Date()}")

            // 🔥 VERIFICAÇÃO: Confirmar que foi salvo
            val saved = sharedPreferences.getString(KEY_EWELINK_TOKENS_JSON, null)
            if (saved != null) {
                Log.d("EWE_STORAGE", "✅ Confirmação: Tokens foram salvos corretamente")
            } else {
                Log.e("EWE_STORAGE", "❌ ERRO CRÍTICO: Tokens NÃO foram salvos")
            }
        } catch (e: Exception) {
            Log.e("EWE_STORAGE", "❌ Erro ao salvar tokens: ${e.message}")
        }
    }

    fun getEwelinkTokens(): EwelinkTokens? {
        return try {
            val json = sharedPreferences.getString(KEY_EWELINK_TOKENS_JSON, null)
            if (json == null) {
                Log.d("EWE_STORAGE", "📭 Nenhum token armazenado")
                return null
            }

            val tokens = Gson().fromJson(json, EwelinkTokens::class.java)

            Log.d("EWE_STORAGE", "✅ Tokens recuperados com sucesso (Encrypted: $useEncryptedStorage)")
            Log.d("EWE_STORAGE", "Access Token: ${tokens.accessToken.take(10)}...")
            Log.d("EWE_STORAGE", "Expira em: ${Date(tokens.atExpiredTime)}")
            Log.d("EWE_STORAGE", "Tempo atual: ${Date()}")

            tokens
        } catch (e: JsonSyntaxException) {
            Log.e("EWE_STORAGE", "❌ JSON corrompido - limpando tokens inválidos")
            clearEwelinkTokens()
            null
        } catch (e: Exception) {
            Log.e("EWE_STORAGE", "❌ Erro ao recuperar tokens: ${e.message}")
            null
        }
    }

    // 🔥 NOVA FUNÇÃO: Verificação FORTE de validade do token
    fun isAccessTokenValidWithMargin(marginMinutes: Long = 5): Boolean {
        val tokens = getEwelinkTokens() ?: return false

        val margemSeguranca = marginMinutes * 60 * 1000
        val tokenValido = tokens.atExpiredTime > (System.currentTimeMillis() + margemSeguranca)

        Log.d("EWE_STORAGE", "🔍 Verificação Token: ${Date(tokens.atExpiredTime)} > ${Date(System.currentTimeMillis() + margemSeguranca)} = $tokenValido")

        return tokenValido
    }

    fun canRefreshToken(): Boolean {
        val tokens = getEwelinkTokens() ?: return false
        val refreshValido = tokens.rtExpiredTime > System.currentTimeMillis()

        Log.d("EWE_STORAGE", "🔍 Refresh Token válido: ${Date(tokens.rtExpiredTime)} > ${Date()} = $refreshValido")

        return refreshValido
    }

    fun isAccessTokenExpired(): Boolean {
        val tokens = getEwelinkTokens() ?: return true
        return tokens.atExpiredTime <= System.currentTimeMillis()
    }

    fun clearEwelinkTokens() {
        try {
            sharedPreferences.edit().remove(KEY_EWELINK_TOKENS_JSON).apply()
            Log.d("EWE_STORAGE", "🗑️ Tokens limpos com sucesso")

            // 🔥 VERIFICAÇÃO: Confirmar que foi removido
            val afterClear = sharedPreferences.getString(KEY_EWELINK_TOKENS_JSON, null)
            if (afterClear == null) {
                Log.d("EWE_STORAGE", "✅ Confirmação: Tokens foram removidos corretamente")
            } else {
                Log.e("EWE_STORAGE", "❌ ERRO: Tokens NÃO foram removidos")
            }
        } catch (e: Exception) {
            Log.e("EWE_STORAGE", "❌ Erro ao limpar tokens: ${e.message}")
        }
    }

    // 🔥 NOVA FUNÇÃO: Debug completo do storage
    fun debugStorage() {
        Log.d("EWE_STORAGE_DEBUG", "=== DEBUG STORAGE ===")
        Log.d("EWE_STORAGE_DEBUG", "Encrypted: $useEncryptedStorage")

        val json = sharedPreferences.getString(KEY_EWELINK_TOKENS_JSON, null)
        if (json == null) {
            Log.d("EWE_STORAGE_DEBUG", "Nenhum token armazenado")
        } else {
            Log.d("EWE_STORAGE_JSON", "JSON armazenado: $json")
            try {
                val tokens = Gson().fromJson(json, EwelinkTokens::class.java)
                Log.d("EWE_STORAGE_DEBUG", "Access Token: ${tokens.accessToken.take(15)}...")
                Log.d("EWE_STORAGE_DEBUG", "Expira: ${Date(tokens.atExpiredTime)}")
                Log.d("EWE_STORAGE_DEBUG", "Refresh Expira: ${Date(tokens.rtExpiredTime)}")
                Log.d("EWE_STORAGE_DEBUG", "Região: ${tokens.region}")
            } catch (e: Exception) {
                Log.e("EWE_STORAGE_DEBUG", "JSON inválido: ${e.message}")
            }
        }
        Log.d("EWE_STORAGE_DEBUG", "=== FIM DEBUG ===")
    }

    companion object {
        private const val KEY_EWELINK_TOKENS_JSON = "ewelink_tokens_json"
    }
}