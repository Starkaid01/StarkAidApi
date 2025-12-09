// EwelinkVoiceControl.kt
package com.starkaid.starkaidapp.ewelink

import android.content.Context
import android.util.Log
import com.starkaid.starkaidapp.ewelink.models.EwelinkDevice
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.EwelinkApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import retrofit2.Retrofit

class EwelinkVoiceControl(private val context: Context, private val deviceService: EwelinkDeviceService) {

    private var dispositivos: List<EwelinkDevice> = emptyList()
    private val sessionManager = SessionManager(context)
    
    // Cache de instâncias para melhor performance
    private var cachedRetrofit: Retrofit? = null
    private var cachedApi: EwelinkApi? = null
    private val dispositivosMap: MutableMap<String, EwelinkDevice> = mutableMapOf()

    fun setDispositivos(lista: List<EwelinkDevice>) {
        this.dispositivos = lista
        // Criar mapa para busca O(1) ao invés de O(n)
        dispositivosMap.clear()
        lista.forEach { dispositivo ->
            dispositivosMap[dispositivo.name.lowercase().trim()] = dispositivo
        }
    }
    
    private fun getEwelinkApi(): EwelinkApi {
        if (cachedApi == null) {
            if (cachedRetrofit == null) {
                cachedRetrofit = ApiClient.getClient(context)
            }
            cachedApi = cachedRetrofit!!.create(EwelinkApi::class.java)
        }
        return cachedApi!!
    }

    fun controlarDispositivoPorComandoAsync(
        comando: String,
        callback: (String) -> Unit
    ) {
        Log.d("EWE_VOICE_TEST_ACAO", "🔍 Analisando comando: $comando")

        // Normalizar o comando
        val comandoNormalizado = comando.lowercase().trim()
            //ligar desligar
            .replace("liga", "ligar")
            .replace("ligue", "ligar")

            .replace("acenda", "ligar")
            .replace("acende", "ligar")
            .replace("acender", "ligar")

            .replace("desligue", "desligar")
            .replace("desliga", "desligar")

            .replace("apague", "desligar")
            .replace("apaga", "desligar")
            .replace("apagar", "desligar")


            //brilho
            .replace("ajustar o brilho", "ajustar brilho")
            .replace("ajusta o brilho", "ajustar brilho")
            .replace("ajuste o brilho", "ajustar brilho")


            .replace("aumenta o brilho", "aumentar brilho")
            .replace("aumente o brilho", "aumentar brilho")
            .replace("aumenta brilho", "aumentar brilho")
            .replace("aumentar o brilho", "aumentar brilho")

            .replace("mais brilho", "aumentar brilho")
            .replace("aumenta mais o brilho", "aumentar brilho")
            .replace("aumenta a luz", "aumentar brilho")
            .replace("luz mais forte", "aumentar brilho")
            .replace("aumenta mais a luz", "aumentar brilho")

            .replace("diminuir o brilho", "diminuir brilho")
            .replace("diminuir brilho", "diminuir brilho")
            .replace("diminui o brilho", "diminuir brilho")
            .replace("diminua o brilho", "diminuir brilho")
            .replace("diminui brilho", "diminuir brilho")
            .replace("diminua brilho", "diminuir brilho")


            .replace("menos brilho", "diminuir brilho")
            .replace("diminui mais o brilho", "diminuir brilho")
            .replace("diminuir mais o brilho", "diminuir brilho")
            .replace("diminua mais o brilho", "diminuir brilho")

            .replace("diminuir a luz", "diminuir brilho")
            .replace("diminua a luz", "diminuir brilho")
            .replace("diminue a luz", "diminuir brilho")

            .replace("diminuir luz", "diminuir brilho")
            .replace("diminua luz", "diminuir brilho")
            .replace("diminue luz", "diminuir brilho")

            .replace("luz mais fraca", "diminuir brilho")
            .replace("diminui mais a luz", "diminuir brilho")


        // Encontrar dispositivo e ação
        val (dispositivo, acao) = encontrarDispositivoEAcao(comandoNormalizado)

        if (dispositivo != null && acao != null) {
            executarAcao(dispositivo, acao, callback)
        } else {
            callback("erro: Não consegui identificar o dispositivo ou ação no comando: $comando")
        }
    }

    private fun encontrarDispositivoEAcao(comandorecive: String): Pair<EwelinkDevice?, String?> {
        var dispositivoEncontrado: EwelinkDevice? = null
        var acaoEncontrada: String? = null

        val comando = comandorecive.lowercase().trim()

        // Otimização: Buscar no mapa primeiro (mais rápido)
        // Tentar encontrar por nome exato no mapa
        for ((nome, dispositivo) in dispositivosMap) {
            if (comando.contains(nome)) {
                dispositivoEncontrado = dispositivo
                Log.d("EWE_VOICE", "✅ Dispositivo encontrado: ${dispositivo.name}")
                break
            }
        }
        
        // Se não encontrou no mapa, tentar na lista (fallback)
        if (dispositivoEncontrado == null) {
            for (dispositivo in dispositivos) {
                val nomeDispositivo = dispositivo.name.lowercase().trim()
                if (comando.contains(nomeDispositivo)) {
                    dispositivoEncontrado = dispositivo
                    Log.d("EWE_VOICE", "✅ Dispositivo encontrado (fallback): ${dispositivo.name}")
                    break
                }
            }
        }

        // CORREÇÃO: Detecção de ação simplificada e mais precisa
        acaoEncontrada = when {
            // 🔥 CORREÇÃO: Verificar desligar PRIMEIRO (ordem importa!)
            comando.contains("desligar") -> {
                Log.d("EWE_VOICE_TEST_ACAO", "🎯 Ação detectada: DESLIGAR")
                "desligar"
            }

            // 🔥 CORREÇÃO: Depois verificar ligar
            comando.contains("ligar") -> {
                Log.d("EWE_VOICE_TEST_ACAO", "🎯 Ação detectada: LIGAR")
                "ligar"
            }

            // Ações de brilho
            comando.contains("aumentar brilho") -> {
                Log.d("EWE_VOICE_TEST_ACAO", "🎯 Ação detectada: AUMENTAR BRILHO")
                "aumentar brilho"
            }

            comando.contains("diminuir brilho") -> {
                Log.d("EWE_VOICE_TEST_ACAO", "🎯 Ação detectada: DIMINUIR BRILHO")
                "diminuir brilho"
            }

            comando.contains("brilho") && comando.contains("porcento") || comando.contains("ajustar brilho") -> {
                Log.d("EWE_VOICE_TEST_ACAO", "🎯 Ação detectada: AJUSTAR BRILHO")
                "ajustar brilho"
            }

            else -> {
                Log.d("EWE_VOICE_TEST_ACAO", "❌ Nenhuma ação detectada")
                null
            }
        }


        Log.d("EWE_VOICE_TEST_ACAO", "🔍 Ação encontrada: $acaoEncontrada")

        return Pair(dispositivoEncontrado, acaoEncontrada)
    }



    private fun executarAcao(
        dispositivo: EwelinkDevice,
        acao: String,
        callback: (String) -> Unit
    ) {
        Log.d("EWE_VOICE", "⚡ Executando ação: $acao no dispositivo: ${dispositivo.name}")

        if (acao.contains("ligar") && !acao.contains("desligar")) {
            ligarDispositivo(dispositivo, callback)
            return
        }

        when (acao) {
            "desligar" -> desligarDispositivo(dispositivo, callback)
            "aumentar brilho" -> ajustarBrilho(dispositivo, 75, callback) // Valor padrão para aumentar
            "diminuir brilho" -> ajustarBrilho(dispositivo, 25, callback) // Valor padrão para diminuir
            "ajustar brilho" -> extrairEAjustarBrilho(acao, dispositivo, callback)
            else -> callback("erro: Ação não suportada: $acao")
        }
    }

    private fun ligarDispositivo(dispositivo: EwelinkDevice, callback: (String) -> Unit) {
        // Verificação rápida de online
        if (!dispositivo.online) {
            Log.e("EWE_VOICE_STATUS", "❌ ${dispositivo.name} está OFFLINE")
            callback("erro: ${dispositivo.name} está offline e não pode ser controlado")
            return
        }

        // Pré-carregar credenciais uma vez
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            callback("erro: Credenciais não encontradas")
            return
        }

        // Verificar status atual antes de executar
        // Primeiro verificar status local (mais rápido)
        val statusLocal = dispositivo.params.optString("switch", "off")
        if (statusLocal == "on") {
            Log.d("EWE_VOICE_STATUS", "ℹ️ ${dispositivo.name} já está ligado (status local)")
            callback("ja_estado: dispositivoName:${dispositivo.name} acao:ligar status:ligado acaoExecutada:nao")
            return
        }
        
        // Se status local não confirma, verificar via API
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val ewelinkApi = getEwelinkApi()
                
                // Verificar status atual do dispositivo via API
                val statusResponse = ewelinkApi.listarDispositivos()
                
                if (statusResponse.isSuccessful && statusResponse.body() != null) {
                    val devices = statusResponse.body()!!
                    val currentDevice = devices.find { it.deviceId == dispositivo.id }
                    
                    // Verificar se já está ligado
                    val jaEstaLigado = currentDevice?.isOn == true || 
                                      currentDevice?.params?.get("switch") == "on"
                    
                    if (jaEstaLigado) {
                        Log.d("EWE_VOICE_STATUS", "ℹ️ ${dispositivo.name} já está ligado (status API)")
                        withContext(Dispatchers.Main) {
                            callback("ja_estado: dispositivoName:${dispositivo.name} acao:ligar status:ligado acaoExecutada:nao")
                        }
                        return@launch
                    }
                }
                
                // Se não está ligado, executar o comando
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = true
                )
                
                val response = ewelinkApi.controlarDispositivo(dispositivo.id, request)
                
                if (response.isSuccessful && response.body() != null) {
                    Log.d("EWE_VOICE_STATUS", "✅ ${dispositivo.name} ligado com sucesso")
                    withContext(Dispatchers.Main) {
                        callback("sucesso: dispositivoName:${dispositivo.name} acao:ligar status:ligado acaoExecutada:sim")
                    }
                } else {
                    val errorBody = response.errorBody()?.string()
                    Log.e("EWE_VOICE_STATUS", "❌ Erro ao ligar ${dispositivo.name}: ${response.code()} - $errorBody")
                    withContext(Dispatchers.Main) {
                        callback("erro: Falha ao ligar ${dispositivo.name}: ${response.code()}")
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_VOICE_STATUS", "❌ Erro ao ligar ${dispositivo.name}", e)
                withContext(Dispatchers.Main) {
                    callback("erro: Falha ao ligar ${dispositivo.name}: ${e.message}")
                }
            }
        }
    }

    private fun desligarDispositivo(dispositivo: EwelinkDevice, callback: (String) -> Unit) {
        // Verificação rápida de online
        if (!dispositivo.online) {
            Log.e("EWE_VOICE_STATUS", "❌ ${dispositivo.name} está OFFLINE")
            callback("erro: ${dispositivo.name} está offline e não pode ser controlado")
            return
        }

        // Pré-carregar credenciais uma vez
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            callback("erro: Credenciais não encontradas")
            return
        }

        // Verificar status atual antes de executar
        // Primeiro verificar status local (mais rápido)
        val statusLocal = dispositivo.params.optString("switch", "off")
        if (statusLocal == "off") {
            Log.d("EWE_VOICE_STATUS", "ℹ️ ${dispositivo.name} já está desligado (status local)")
            callback("ja_estado: dispositivoName:${dispositivo.name} acao:desligar status:desligado acaoExecutada:nao")
            return
        }
        
        // Se status local não confirma, verificar via API
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val ewelinkApi = getEwelinkApi()
                
                // Verificar status atual do dispositivo via API
                val statusResponse = ewelinkApi.listarDispositivos()
                
                if (statusResponse.isSuccessful && statusResponse.body() != null) {
                    val devices = statusResponse.body()!!
                    val currentDevice = devices.find { it.deviceId == dispositivo.id }
                    
                    // Verificar se já está desligado
                    val jaEstaDesligado = currentDevice?.isOn == false || 
                                         currentDevice?.params?.get("switch") == "off"
                    
                    if (jaEstaDesligado) {
                        Log.d("EWE_VOICE_STATUS", "ℹ️ ${dispositivo.name} já está desligado (status API)")
                        withContext(Dispatchers.Main) {
                            callback("ja_estado: dispositivoName:${dispositivo.name} acao:desligar status:desligado acaoExecutada:nao")
                        }
                        return@launch
                    }
                }
                
                // Se não está desligado, executar o comando
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = false
                )
                
                val response = ewelinkApi.controlarDispositivo(dispositivo.id, request)
                
                if (response.isSuccessful && response.body() != null) {
                    Log.d("EWE_VOICE_STATUS", "✅ ${dispositivo.name} desligado com sucesso")
                    withContext(Dispatchers.Main) {
                        callback("sucesso: dispositivoName:${dispositivo.name} acao:desligar status:desligado acaoExecutada:sim")
                    }
                } else {
                    val errorBody = response.errorBody()?.string()
                    Log.e("EWE_VOICE_STATUS", "❌ Erro ao desligar ${dispositivo.name}: ${response.code()} - $errorBody")
                    withContext(Dispatchers.Main) {
                        callback("erro: Falha ao desligar ${dispositivo.name}: ${response.code()}")
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_VOICE_STATUS", "❌ Erro ao desligar ${dispositivo.name}", e)
                withContext(Dispatchers.Main) {
                    callback("erro: Falha ao desligar ${dispositivo.name}: ${e.message}")
                }
            }
        }
    }

    private fun ajustarBrilho(dispositivo: EwelinkDevice, brilho: Int, callback: (String) -> Unit) {
        // Pré-carregar credenciais uma vez
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            callback("erro: Credenciais não encontradas")
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // Usar API em cache
                val ewelinkApi = getEwelinkApi()
                
                // Para controlar brilho, precisamos enviar switch on
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = true
                )
                
                val response = ewelinkApi.controlarDispositivo(dispositivo.id, request)
                
                // Processar resposta de forma mais eficiente
                if (response.isSuccessful && response.body() != null) {
                    withContext(Dispatchers.Main) {
                        callback("sucesso: dispositivoName:${dispositivo.name} acao:ajustar_brilho brilho:$brilho acaoExecutada:sim")
                    }
                } else {
                    val errorBody = response.errorBody()?.string()
                    Log.e("EWE_VOICE_STATUS", "❌ Erro ao ajustar brilho: ${response.code()} - $errorBody")
                    withContext(Dispatchers.Main) {
                        callback("erro: Falha ao ajustar brilho de ${dispositivo.name}: ${response.code()}")
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_VOICE_STATUS", "❌ Erro ao ajustar brilho", e)
                withContext(Dispatchers.Main) {
                    callback("erro: Falha ao ajustar brilho de ${dispositivo.name}: ${e.message}")
                }
            }
        }
    }

    private fun extrairEAjustarBrilho(comando: String, dispositivo: EwelinkDevice, callback: (String) -> Unit) {
        try {
            val regex = """(\d{1,3})\s*porcento""".toRegex(RegexOption.IGNORE_CASE)
            val matchResult = regex.find(comando)

            if (matchResult != null) {
                val brilho = matchResult.groupValues[1].toInt().coerceIn(1, 100)
                ajustarBrilho(dispositivo, brilho, callback)
            } else {
                callback("erro: Não consegui identificar o valor do brilho no comando")
            }
        } catch (e: Exception) {
            callback("erro: Erro ao processar valor do brilho: ${e.message}")
        }
    }
}