namespace StarkAid.WindowsForms.Config;

/// <summary>
/// Configuração centralizada da API.
/// Para mudar entre desenvolvimento e produção, altere apenas o valor de IsDevelopment.
/// </summary>
public static class ApiConfig
{
    // ============================================
    // CONFIGURAÇÃO DE AMBIENTE
    // ============================================
    // Altere este valor para true (desenvolvimento) ou false (produção)
#if DEBUG
    private const bool IsDevelopment = true;
#else
    private const bool IsDevelopment = false;
#endif
    
    // ============================================
    // URLs DE DESENVOLVIMENTO
    // ============================================
    private const string DevApiBaseUrl = "http://192.168.2.103:5000/api";
    private const string DevWebBaseUrl = "http://192.168.2.103:5000";
    
    // ============================================
    // URLs DE PRODUÇÃO
    // ============================================
    private const string ProdApiBaseUrl = "https://starkaid.runasp.net/api";
    private const string ProdWebBaseUrl = "https://starkaid.runasp.net";
    
    // ============================================
    // PROPRIEDADES PÚBLICAS
    // ============================================
    /// <summary>
    /// URL base da API (com /api no final)
    /// </summary>
    public static string ApiBaseUrl => IsDevelopment ? DevApiBaseUrl : ProdApiBaseUrl;
    
    /// <summary>
    /// URL base da web (sem /api)
    /// </summary>
    public static string WebBaseUrl => IsDevelopment ? DevWebBaseUrl : ProdWebBaseUrl;
    
    /// <summary>
    /// URL base da API com barra final (para compatibilidade)
    /// </summary>
    public static string ApiBaseUrlWithSlash => ApiBaseUrl.EndsWith("/") ? ApiBaseUrl : ApiBaseUrl + "/";
    
    /// <summary>
    /// URL para buscar configuração da API
    /// </summary>
    public static string ConfigUrl => $"{ApiBaseUrl}/v1/Config/app-config";
}

