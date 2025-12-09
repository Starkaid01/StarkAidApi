package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.data.AppDatabase
import com.starkaid.starkaidapp.models.LogToSuporteEntity
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.io.PrintWriter
import java.io.StringWriter
import java.text.SimpleDateFormat
import java.util.*

/**
 * Serviço centralizado para captura e registro de erros
 */
class ErrorLoggerService(private val context: Context) {
    
    private val database = AppDatabase.getInstance(context)
    private val logDao = database.logToSuporteDao()
    private val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
    private val timeFormat = SimpleDateFormat("HH:mm:ss", Locale.getDefault())
    
    /**
     * Registra um erro de forma padronizada
     * 
     * @param exception A exceção capturada
     * @param codigoDeErro Código de erro padronizado (ex: ERR_001)
     * @param acaoErro Descrição da ação que estava ocorrendo (ex: "ao abrir menu")
     * @param ultimoComando Último comando executado (opcional)
     * @param ultimaResposta Última resposta recebida (opcional)
     * @param ultimoDispositivoAcionado Último dispositivo acionado (opcional)
     */
    fun logError(
        exception: Throwable,
        codigoDeErro: String,
        acaoErro: String,
        ultimoComando: String? = null,
        ultimaResposta: String? = null,
        ultimoDispositivoAcionado: String? = null
    ) {
        try {
            val now = Date()
            val dataErro = dateFormat.format(now)
            val horaErro = timeFormat.format(now)
            
            // Obter stacktrace completo
            val sw = StringWriter()
            val pw = PrintWriter(sw)
            exception.printStackTrace(pw)
            val erroCompleto = sw.toString()
            
            val log = LogToSuporteEntity(
                ultimoComando = ultimoComando,
                ultimaResposta = ultimaResposta,
                ultimoDispositivoAcionado = ultimoDispositivoAcionado,
                erroCompleto = erroCompleto,
                codigoDeErro = codigoDeErro,
                dataErro = dataErro,
                horaErro = horaErro,
                acaoErro = acaoErro
            )
            
            // Salvar no banco de forma assíncrona
            CoroutineScope(Dispatchers.IO).launch {
                try {
                    logDao.insertLog(log)
                    Log.d("ErrorLogger", "✅ Erro registrado: $codigoDeErro - $acaoErro")
                } catch (e: Exception) {
                    Log.e("ErrorLogger", "❌ Erro ao salvar log no banco", e)
                }
            }
            
        } catch (e: Exception) {
            Log.e("ErrorLogger", "❌ Erro crítico ao registrar erro", e)
        }
    }
    
    /**
     * Obtém todos os logs salvos
     */
    suspend fun getAllLogs(): List<LogToSuporteEntity> {
        return try {
            logDao.getAllLogs()
        } catch (e: Exception) {
            Log.e("ErrorLogger", "Erro ao obter logs", e)
            emptyList()
        }
    }
    
    /**
     * Limpa todos os logs (usado após sincronização)
     */
    suspend fun clearAllLogs() {
        try {
            logDao.deleteAllLogs()
        } catch (e: Exception) {
            Log.e("ErrorLogger", "Erro ao limpar logs", e)
        }
    }
}

/**
 * Códigos de erro padronizados para o app
 */
object ErrorCodes {
    // Erros de IA
    const val ERR_001 = "ERR_001" // Erro ao processar comando de IA
    const val ERR_002 = "ERR_002" // Erro ao chamar API de IA
    const val ERR_003 = "ERR_003" // Erro ao processar resposta da IA
    
    // Erros de Rede
    const val ERR_101 = "ERR_101" // Erro de conexão de rede
    const val ERR_102 = "ERR_102" // Timeout de requisição
    const val ERR_103 = "ERR_103" // Erro HTTP não tratado
    const val ERR_104 = "ERR_104" // Erro ao fazer requisição API
    
    // Erros de Dispositivos IoT
    const val ERR_201 = "ERR_201" // Erro ao carregar dispositivos eWeLink
    const val ERR_202 = "ERR_202" // Erro ao controlar dispositivo eWeLink
    const val ERR_203 = "ERR_203" // Erro ao obter status do dispositivo
    const val ERR_204 = "ERR_204" // Erro ao conectar com dispositivo ESP
    const val ERR_205 = "ERR_205" // Erro ao acionar dispositivo ESP
    
    // Erros de Banco de Dados Local
    const val ERR_301 = "ERR_301" // Erro ao acessar banco de dados local
    const val ERR_302 = "ERR_302" // Erro ao salvar dados no banco
    const val ERR_303 = "ERR_303" // Erro ao ler dados do banco
    const val ERR_304 = "ERR_304" // Erro ao deletar dados do banco
    
    // Erros de UI
    const val ERR_401 = "ERR_401" // Erro ao carregar interface
    const val ERR_402 = "ERR_402" // Erro ao atualizar UI
    const val ERR_403 = "ERR_403" // Erro ao navegar entre telas
    const val ERR_404 = "ERR_404" // Erro ao renderizar componente
    
    // Erros de JSON
    const val ERR_501 = "ERR_501" // Erro ao parsear JSON
    const val ERR_502 = "ERR_502" // Erro ao serializar JSON
    const val ERR_503 = "ERR_503" // JSON malformado
    
    // Erros de Autenticação
    const val ERR_601 = "ERR_601" // Erro ao fazer login
    const val ERR_602 = "ERR_602" // Token expirado
    const val ERR_603 = "ERR_603" // Erro ao validar token
    const val ERR_604 = "ERR_604" // Erro ao fazer logout
    
    // Erros de TTS/STT
    const val ERR_701 = "ERR_701" // Erro no Text-to-Speech
    const val ERR_702 = "ERR_702" // Erro no Speech-to-Text
    const val ERR_703 = "ERR_703" // Erro ao inicializar reconhecimento de voz
    const val ERR_704 = "ERR_704" // Erro ao processar áudio
    
    // Erros de Inicialização
    const val ERR_801 = "ERR_801" // Erro ao inicializar aplicativo
    const val ERR_802 = "ERR_802" // Erro ao carregar configurações
    const val ERR_803 = "ERR_803" // Erro ao inicializar serviços
    const val ERR_804 = "ERR_804" // Erro ao verificar permissões
    
    // Erros de WebSocket
    const val ERR_901 = "ERR_901" // Erro ao conectar WebSocket
    const val ERR_902 = "ERR_902" // Erro ao enviar mensagem WebSocket
    const val ERR_903 = "ERR_903" // Erro ao receber mensagem WebSocket
    const val ERR_904 = "ERR_904" // Erro ao desconectar WebSocket
    
    // Erros Críticos Inesperados
    const val ERR_999 = "ERR_999" // Erro crítico inesperado
}

