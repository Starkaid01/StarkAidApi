package com.starkaid.starkaidapp.services

import android.util.Log
import okhttp3.Interceptor
import okhttp3.Response
import java.io.IOException
import java.util.concurrent.TimeUnit

/**
 * Interceptor para tratar erros de Rate Limiting (429 Too Many Requests)
 * Implementa retry automático com backoff exponencial
 */
class RateLimitInterceptor : Interceptor {
    
    companion object {
        private const val TAG = "RateLimitInterceptor"
        private const val MAX_RETRIES = 2
        private const val INITIAL_DELAY_MS = 1000L // 1 segundo
    }

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        var response = chain.proceed(request)
        var retryCount = 0

        // Se não for 429, retorna a resposta normalmente
        while (response.code == 429 && retryCount < MAX_RETRIES) {
            // Ler headers ANTES de fechar a resposta
            val retryAfter = response.header("Retry-After")?.toIntOrNull()
            val remaining = response.header("X-RateLimit-Remaining")
            val reset = response.header("X-RateLimit-Reset")
            
            // Calcular delay
            val delay = if (retryAfter != null && retryAfter > 0) {
                // Usar o valor do Retry-After em segundos
                retryAfter * 1000L
            } else {
                // Backoff exponencial: 1s, 2s, 4s...
                INITIAL_DELAY_MS * (1 shl retryCount)
            }
            
            Log.w(TAG, "⚠️ Rate limit excedido (429). Tentativa ${retryCount + 1}/$MAX_RETRIES")
            Log.d(TAG, "📊 Requisições restantes: $remaining")
            Log.d(TAG, "🔄 Reset em: $reset")
            Log.d(TAG, "⏱️ Aguardando ${delay}ms antes de tentar novamente...")

            // Fechar a resposta anterior
            response.close()

            try {
                // Aguardar antes de tentar novamente
                Thread.sleep(delay)
            } catch (e: InterruptedException) {
                Thread.currentThread().interrupt()
                Log.e(TAG, "Interrompido durante espera de rate limit", e)
                break
            }

            // Tentar novamente
            try {
                response = chain.proceed(request)
                retryCount++
            } catch (e: IOException) {
                Log.e(TAG, "Erro ao tentar novamente após rate limit", e)
                throw e
            }
        }

        // Se ainda for 429 após todas as tentativas, loga e retorna
        if (response.code == 429) {
            val errorBody = try {
                response.peekBody(1024).string()
            } catch (e: Exception) {
                "Não foi possível ler o corpo da resposta"
            }
            
            Log.e(TAG, "❌ Rate limit excedido após $MAX_RETRIES tentativas")
            Log.e(TAG, "💬 Mensagem: $errorBody")
            
            // Ler headers finais
            val remaining = response.header("X-RateLimit-Remaining")
            val reset = response.header("X-RateLimit-Reset")
            val retryAfter = response.header("Retry-After")
            
            Log.w(TAG, "📊 Estado final - Restantes: $remaining, Reset: $reset, Retry-After: $retryAfter")
        }

        return response
    }
}

