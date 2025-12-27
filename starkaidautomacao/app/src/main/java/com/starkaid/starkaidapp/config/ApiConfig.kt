package com.starkaid.starkaidapp.config

/**
 * Configuração centralizada da API.
 * Para mudar entre desenvolvimento e produção, altere apenas o valor de IS_DEVELOPMENT.
 */
object ApiConfig {
    // ============================================
    // CONFIGURAÇÃO DE AMBIENTE
    // ============================================
    // Altere este valor para true (desenvolvimento) ou false (produção)
    private const val IS_DEVELOPMENT = true // PRODUÇÃO

    // ============================================
    // URLs DE DESENVOLVIMENTO
    // ============================================
    // IMPORTANTE: No app Android, use o IP da máquina de desenvolvimento, não localhost
    // Altere o IP abaixo conforme necessário (ex: 192.168.2.106)
    // A API roda em HTTP na porta 5000 e HTTPS na porta 5001
    // Para desenvolvimento, use HTTP (porta 5000) para evitar problemas com certificados SSL
    private const val DEV_IP = "192.168.2.103"
    private const val DEV_PORT = "5000" // HTTP (porta 5000) ou HTTPS (porta 5001)
    private const val DEV_API_BASE_URL = "http://$DEV_IP:$DEV_PORT" // base sem /api (endpoints já incluem api/v1)
    private const val DEV_WEB_BASE_URL = "http://$DEV_IP:$DEV_PORT"

    // ============================================
    // URLs DE PRODUÇÃO
    // ============================================
    private const val PROD_API_BASE_URL = "https://starkaid.runasp.net" // base sem /api (endpoints já incluem api/v1)
    private const val PROD_WEB_BASE_URL = "https://starkaid.runasp.net"

    // ============================================
    // PROPRIEDADES PÚBLICAS
    // ============================================
    /**
     * URL base da API (com /api no final)
     */
    val apiBaseUrl: String
        get() = if (IS_DEVELOPMENT) DEV_API_BASE_URL else PROD_API_BASE_URL

    /**
     * URL base da web (sem /api)
     */
    val webBaseUrl: String
        get() = if (IS_DEVELOPMENT) DEV_WEB_BASE_URL else PROD_WEB_BASE_URL

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
}

