package com.starkaid.starkaidapp.config

/**
 * Configuração centralizada da API.
 * Valores padrão vêm do BuildConfig gerado a partir de starkaid.local.properties.
 */
object ApiConfig {
    private fun normalizeBaseUrl(value: String): String {
        return value.trim().trimEnd('/')
    }

    private val isDevelopment: Boolean
        get() = com.starkaid.starkaidapp.BuildConfig.STARKAID_IS_DEVELOPMENT

    private val devApiBaseUrl: String
        get() = normalizeBaseUrl(com.starkaid.starkaidapp.BuildConfig.STARKAID_DEV_API_BASE_URL)

    private val devWebBaseUrl: String
        get() = normalizeBaseUrl(com.starkaid.starkaidapp.BuildConfig.STARKAID_DEV_WEB_BASE_URL)

    private val prodApiBaseUrl: String
        get() = normalizeBaseUrl(com.starkaid.starkaidapp.BuildConfig.STARKAID_PROD_API_BASE_URL)

    private val prodWebBaseUrl: String
        get() = normalizeBaseUrl(com.starkaid.starkaidapp.BuildConfig.STARKAID_PROD_WEB_BASE_URL)

    // ============================================
    // PROPRIEDADES PÚBLICAS
    // ============================================
    /**
     * URL base da API (com /api no final)
     */
    val apiBaseUrl: String
        get() = if (isDevelopment) devApiBaseUrl else prodApiBaseUrl

    /**
     * URL base da web (sem /api)
     */
    val webBaseUrl: String
        get() = if (isDevelopment) devWebBaseUrl else prodWebBaseUrl

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
        get() = com.starkaid.starkaidapp.BuildConfig.STARKAID_SPOTIFY_CLIENT_ID

    val spotifyClientSecret: String
        get() = com.starkaid.starkaidapp.BuildConfig.STARKAID_SPOTIFY_CLIENT_SECRET

    val ewelinkClientId: String
        get() = com.starkaid.starkaidapp.BuildConfig.STARKAID_EWELINK_CLIENT_ID

    val ewelinkClientSecret: String
        get() = com.starkaid.starkaidapp.BuildConfig.STARKAID_EWELINK_CLIENT_SECRET

    const val spotifyRedirectUri: String = "starkaid://spotifycallback"
    const val spotifyAuthorizeUrl: String = "https://accounts.spotify.com/authorize"
    const val spotifyTokenUrl: String = "https://accounts.spotify.com/api/token"
}

