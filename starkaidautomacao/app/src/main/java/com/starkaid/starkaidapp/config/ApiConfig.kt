package com.starkaid.starkaidapp.config

import com.starkaid.starkaidapp.BuildConfig

/**
 * Configuração centralizada da API.
 * O build injeta os valores via BuildConfig, com fallback configurável em starkaid.local.properties.
 */
object ApiConfig {
    private val isDevelopment: Boolean
        get() = BuildConfig.STARKAID_IS_DEVELOPMENT

    /**
     * URL base da API (com /api no final)
     */
    val apiBaseUrl: String
        get() = if (isDevelopment) BuildConfig.STARKAID_DEV_API_BASE_URL else BuildConfig.STARKAID_PROD_API_BASE_URL

    /**
     * URL base da web (sem /api)
     */
    val webBaseUrl: String
        get() = if (isDevelopment) BuildConfig.STARKAID_DEV_WEB_BASE_URL else BuildConfig.STARKAID_PROD_WEB_BASE_URL

    /**
     * URL base da API com barra final (para compatibilidade com Retrofit)
     */
    val apiBaseUrlWithSlash: String
        get() = if (apiBaseUrl.endsWith("/")) apiBaseUrl else "$apiBaseUrl/"

    /**
     * URL base da web com barra final
     */
    val webBaseUrlWithSlash: String
        get() = if (webBaseUrl.endsWith("/")) webBaseUrl else "$webBaseUrl/"

    /**
     * URL para buscar configuração da API
     */
    val configUrl: String
        get() = "$apiBaseUrl/api/v1/Config/app-config"

    val spotifyClientId: String
        get() = BuildConfig.SPOTIFY_CLIENT_ID

    val spotifyClientSecret: String
        get() = BuildConfig.SPOTIFY_CLIENT_SECRET

    val spotifyRedirectUri: String
        get() = BuildConfig.SPOTIFY_REDIRECT_URI

    val unityAdsGameId: String
        get() = BuildConfig.UNITY_ADS_GAME_ID
}

