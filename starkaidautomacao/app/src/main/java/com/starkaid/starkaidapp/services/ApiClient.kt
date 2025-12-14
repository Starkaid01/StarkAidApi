package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import okhttp3.Dns
import okhttp3.OkHttpClient
import okhttp3.Protocol
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object ApiClient {
    private var retrofit: Retrofit? = null
    private var configLoaded = false
    private val defaultBaseUrl = ApiConfig.apiBaseUrlWithSlash

    fun getClient(context: Context): Retrofit {
        if (retrofit == null) {
            val sessionManager = SessionManager.getInstance(context)

            // Normalizar base: remover /api duplicado ou final
            fun normalize(url: String?): String? {
                if (url.isNullOrBlank()) return null
                var u = url.trimEnd('/')
                u = u.removeSuffix("/api")
                return if (u.endsWith("/")) u else "$u/"
            }

            val isDevBase = defaultBaseUrl.contains("192.168") || defaultBaseUrl.contains("10.") || defaultBaseUrl.contains("localhost")
            val storedBase = normalize(sessionManager.fetchApiBaseUrl())
            val normalizedDefault = normalize(defaultBaseUrl) ?: defaultBaseUrl
            val baseUrl = if (isDevBase) normalizedDefault!! else storedBase ?: normalizedDefault!!

            val client = OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .writeTimeout(10, TimeUnit.SECONDS)
                .dns(Dns.SYSTEM)
                .retryOnConnectionFailure(true)
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
                    
                    requestBuilder.addHeader("X-From-App", "true")

                    val request = requestBuilder.build()
                    Log.d("API_HEADERS", "Request to ${request.url}")

                    val response = chain.proceed(request)
                    Log.d("API_HEADERS", "Response: ${response.code} for ${request.url}")
                    response
                }
                .addInterceptor(RefreshTokenInterceptor(context))
                .addInterceptor(RateLimitInterceptor())
                .protocols(listOf(Protocol.HTTP_1_1))
                .build()

            retrofit = Retrofit.Builder()
                .baseUrl(baseUrl)
                .addConverterFactory(GsonConverterFactory.create())
                .client(client)
                .build()

            // Carregar configuração em background se ainda não foi carregada
            if (!configLoaded) {
                loadConfigAsync(context, sessionManager, isDevBase)
            }
        }
        return retrofit!!
    }

    private fun loadConfigAsync(context: Context, sessionManager: SessionManager, isDevBase: Boolean) {
        configLoaded = true
        CoroutineScope(Dispatchers.IO).launch {
            try {
                // Em ambiente dev (IP local), não sobrepor com URL do servidor
                if (isDevBase) {
                    Log.d("API_CONFIG", "Ambiente dev detectado, mantendo base local: $defaultBaseUrl")
                    return@launch
                }

                // Criar cliente temporário com URL padrão para buscar config
                val tempClient = OkHttpClient.Builder()
                    .connectTimeout(5, TimeUnit.SECONDS)
                    .readTimeout(5, TimeUnit.SECONDS)
                    .build()

                val tempRetrofit = Retrofit.Builder()
                    .baseUrl(defaultBaseUrl)
                    .addConverterFactory(GsonConverterFactory.create())
                    .client(tempClient)
                    .build()

                val configApi = tempRetrofit.create(ConfigApi::class.java)
                val response = configApi.getAppConfig()

                if (response.isSuccessful && response.body() != null) {
                    val config = response.body()!!
                    
                    // Salvar configurações
                    // Usar a URL do servidor se diferente da config local, senão usar a config local
                    val serverApiBaseUrl = config.apiBaseUrl.trimEnd('/')
                    val localApiBaseUrl = ApiConfig.apiBaseUrl.trimEnd('/')
                    val finalApiBaseUrl = if (serverApiBaseUrl != localApiBaseUrl && !serverApiBaseUrl.contains("localhost")) {
                        serverApiBaseUrl // Usar URL do servidor se for diferente e não for localhost
                    } else {
                        localApiBaseUrl // Usar URL da configuração local
                    }
                    
                    sessionManager.saveApiBaseUrl(finalApiBaseUrl)
                    config.spotify?.let {
                        sessionManager.saveSpotifyClientId(it.clientId)
                        sessionManager.saveSpotifyClientSecret(it.clientSecret)
                    }
                    config.ewelink?.let {
                        sessionManager.saveEwelinkClientId(it.clientId)
                        sessionManager.saveEwelinkClientSecret(it.clientSecret)
                        sessionManager.saveEwelinkRedirectUri(it.redirectUri)
                    }

                    // Recriar retrofit com nova base URL se mudou
                    val newBaseUrl = if (finalApiBaseUrl.endsWith("/")) finalApiBaseUrl else "$finalApiBaseUrl/"
                    if (newBaseUrl != defaultBaseUrl) {
                        retrofit = null // Força recriação na próxima chamada
                        Log.d("ApiClient", "Configuração carregada. Nova base URL: $newBaseUrl")
                    } else {
                        Log.d("ApiClient", "Configuração carregada. Usando URL padrão: $newBaseUrl")
                    }
                } else {
                    Log.w("ApiClient", "Falha ao carregar configuração: ${response.code()}")
                }
            } catch (e: Exception) {
                Log.e("ApiClient", "Erro ao carregar configuração", e)
            }
        }
    }

    fun reloadConfig(context: Context) {
        configLoaded = false
        retrofit = null
        getClient(context) // Força recarregar
    }
}