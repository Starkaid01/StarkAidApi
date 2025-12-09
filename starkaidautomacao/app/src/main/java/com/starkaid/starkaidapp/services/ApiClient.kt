package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.data.SessionManager
import okhttp3.Dns
import okhttp3.OkHttpClient
import okhttp3.Protocol
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object ApiClient {
    private var retrofit: Retrofit? = null

    fun getClient(context: Context): Retrofit {
        if (retrofit == null) {
            val sessionManager = SessionManager.getInstance(context) // 🔥 CORREÇÃO: getInstance

            val client = OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS) // Reduzido de 30s para 10s
                .readTimeout(10, TimeUnit.SECONDS) // Reduzido de 30s para 10s
                .writeTimeout(10, TimeUnit.SECONDS) // Reduzido de 30s para 10s
                .dns(Dns.SYSTEM)
                .retryOnConnectionFailure(true) // Adicionar retry automático
                .addInterceptor { chain ->
                    val requestBuilder = chain.request().newBuilder()

                    sessionManager.fetchAuthToken()?.let {
                        requestBuilder.addHeader("Authorization", "Bearer $it")
                        Log.d("API_HEADERS", "Added Auth token: $it")
                    }
                    sessionManager.fetchApiKey()?.let {
                        requestBuilder.addHeader("Api-Key", it)
                        Log.d("API_HEADERS", "Added API Key: $it")
                    }
                    
                    // Marcar todas as requisições como vindas do app
                    requestBuilder.addHeader("X-From-App", "true")

                    val request = requestBuilder.build()
                    Log.d("API_HEADERS", "Request to ${request.url}")
                    Log.d("API_HEADERS", "Headers: ${request.headers}")

                    val response = chain.proceed(request)
                    Log.d("API_HEADERS", "Response: ${response.code} for ${request.url}")
                    // Não consumir o response body aqui para evitar conflitos
                    response
                }
                .addInterceptor(RefreshTokenInterceptor(context))
                .protocols(listOf(Protocol.HTTP_1_1))
                .build()

            retrofit = Retrofit.Builder()
                .baseUrl("https://starkaid.runasp.net/")
                .addConverterFactory(GsonConverterFactory.create())
                .client(client)
                .build()
        }
        return retrofit!!
    }

}