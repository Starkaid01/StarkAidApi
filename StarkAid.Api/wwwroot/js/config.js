/**
 * Configuração centralizada da API.
 * Para mudar entre desenvolvimento e produção, altere apenas o valor de IS_DEVELOPMENT.
 */

// ============================================
// CONFIGURAÇÃO DE AMBIENTE
// ============================================
// Altere este valor para true (desenvolvimento) ou false (produção)
const IS_DEVELOPMENT = false; // PRODUÇÃO

// ============================================
// URLs DE DESENVOLVIMENTO
// ============================================
const DEV_API_BASE_URL = 'https://localhost:5001/api';
const DEV_WEB_BASE_URL = 'https://localhost:5001';

// ============================================
// URLs DE PRODUÇÃO
// ============================================
const PROD_API_BASE_URL = 'https://starkaid.runasp.net/api';
const PROD_WEB_BASE_URL = 'https://starkaid.runasp.net';

// ============================================
// PROPRIEDADES PÚBLICAS
// ============================================
/**
 * URL base da API (com /api no final)
 */
const API_BASE_URL = IS_DEVELOPMENT ? DEV_API_BASE_URL : PROD_API_BASE_URL;

/**
 * URL base da web (sem /api)
 */
const WEB_BASE_URL = IS_DEVELOPMENT ? DEV_WEB_BASE_URL : PROD_WEB_BASE_URL;

/**
 * URL base da API com barra final (para compatibilidade)
 */
const API_BASE_URL_WITH_SLASH = API_BASE_URL.endsWith('/') ? API_BASE_URL : API_BASE_URL + '/';

/**
 * URL base da web com barra final
 */
const WEB_BASE_URL_WITH_SLASH = WEB_BASE_URL.endsWith('/') ? WEB_BASE_URL : WEB_BASE_URL + '/';

/**
 * URL para buscar configuração da API
 */
const CONFIG_URL = `${API_BASE_URL}/v1/Config/app-config`;

