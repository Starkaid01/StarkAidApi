package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.data.SessionManager
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

/**
 * Serviço para sincronizar logs de erro com o backend
 */
class ErrorLogSyncService(private val context: Context) {
    
    private val errorLogger = ErrorLoggerService(context)
    private val sessionManager = SessionManager.getInstance(context)
    
    /**
     * Sincroniza todos os logs locais com o backend
     * Deve ser chamado uma única vez ao iniciar o app
     */
    suspend fun syncLogsToBackend(): Boolean {
        return try {
            Log.d("ErrorLogSync", "[syncLogsToBackend] Iniciando sincronização de logs...")
            
            val userId = sessionManager.fetchUserId()
            val token = sessionManager.fetchAuthToken()
            
            if (userId == null || token.isNullOrEmpty()) {
                Log.w("ErrorLogSync", "[syncLogsToBackend] ⚠️ Usuário não autenticado, pulando sincronização de logs. UserId: $userId, Token presente: ${!token.isNullOrEmpty()}")
                return false
            }
            
            Log.d("ErrorLogSync", "[syncLogsToBackend] Usuário autenticado: $userId")
            
            val logs = errorLogger.getAllLogs()
            
            Log.d("ErrorLogSync", "[syncLogsToBackend] Logs encontrados localmente: ${logs.size}")
            
            if (logs.isEmpty()) {
                Log.d("ErrorLogSync", "[syncLogsToBackend] ✅ Nenhum log para sincronizar")
                return true
            }
            
            // Converter para formato DTO
            val logsDto = logs.map { log ->
                ErrorLogAppDto(
                    ultimoComando = log.ultimoComando,
                    ultimaResposta = log.ultimaResposta,
                    ultimoDispositivoAcionado = log.ultimoDispositivoAcionado,
                    erroCompleto = log.erroCompleto,
                    codigoDeErro = log.codigoDeErro,
                    dataErro = log.dataErro,
                    horaErro = log.horaErro,
                    acaoErro = log.acaoErro
                )
            }
            
            Log.d("ErrorLogSync", "[syncLogsToBackend] Criando request com ${logsDto.size} logs")
            
            val request = SyncErrorLogsAppRequest(
                userId = userId,
                logs = logsDto
            )
            
            val retrofit = ApiClient.getClient(context)
            val api = retrofit.create(ErrorLogSyncApi::class.java)
            
            Log.d("ErrorLogSync", "[syncLogsToBackend] Enviando requisição para API...")
            val response = api.syncErrorLogs(request)
            
            if (response.isSuccessful) {
                val responseBody = response.body()
                Log.d("ErrorLogSync", "[syncLogsToBackend] ✅ Logs sincronizados com sucesso: ${logs.size} logs. Resposta: ${responseBody?.message}")
                // Limpar logs locais após sincronização bem-sucedida
                errorLogger.clearAllLogs()
                true
            } else {
                val errorBody = response.errorBody()?.string()
                Log.e("ErrorLogSync", "[syncLogsToBackend] ❌ Erro ao sincronizar logs: ${response.code()} - ${response.message()} - $errorBody")
                false
            }
            
        } catch (e: Exception) {
            Log.e("ErrorLogSync", "[syncLogsToBackend] ❌ Exceção ao sincronizar logs: ${e.message}", e)
            e.printStackTrace()
            false
        }
    }
}

/**
 * Interface da API para sincronização de logs
 */
interface ErrorLogSyncApi {
    @POST("api/v1/Users/error-logs/app/sync")
    suspend fun syncErrorLogs(
        @Body request: SyncErrorLogsAppRequest
    ): Response<SyncErrorLogsAppResponse>
}

/**
 * DTOs para sincronização
 */
data class SyncErrorLogsAppRequest(
    val userId: String,
    val logs: List<ErrorLogAppDto>
)

data class ErrorLogAppDto(
    val ultimoComando: String?,
    val ultimaResposta: String?,
    val ultimoDispositivoAcionado: String?,
    val erroCompleto: String?,
    val codigoDeErro: String?,
    val dataErro: String,
    val horaErro: String,
    val acaoErro: String
)

data class SyncErrorLogsAppResponse(
    val message: String,
    val count: Int
)

