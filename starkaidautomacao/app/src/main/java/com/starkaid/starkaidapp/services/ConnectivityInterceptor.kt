package com.starkaid.starkaidapp.services

import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.Protocol
import okhttp3.Response
import okhttp3.ResponseBody.Companion.toResponseBody
import java.io.IOException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

class ConnectivityInterceptor : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        try {
            return chain.proceed(chain.request())
        } catch (e: Exception) {
            val msg = when (e) {
                is SocketTimeoutException -> "Timeout de conexão"
                is UnknownHostException -> "Host desconhecido"
                is IOException -> "Erro de conexão"
                else -> e.message ?: "Erro desconhecido"
            }
            
            // Retorna um erro 503 (Service Unavailable) simulado para evitar crash
            // e permitir que o app trate como falha de API
            return Response.Builder()
                .request(chain.request())
                .protocol(Protocol.HTTP_1_1)
                .code(503)
                .message(msg)
                .body("{\"error\": \"$msg\"}".toResponseBody("application/json".toMediaTypeOrNull()))
                .build()
        }
    }
}
