package com.starkaid.starkaidapp.viewmodels

import android.app.Application
import android.content.Context
import android.content.Intent
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.starkaid.starkaidapp.data.AppDatabase
import com.starkaid.starkaidapp.models.ComandoSocial
import com.starkaid.starkaidapp.models.ComandoSocialEntity
import com.starkaid.starkaidapp.models.CriarComandoSocialRequest
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.ComandoSocialApi
import com.starkaid.starkaidapp.services.FullDuplexAssistantAdvancedService
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class ComandosSociaisViewModel(application: Application) : AndroidViewModel(application) {

    // ---- Dependências ----------------------------------------------------
    private val database = AppDatabase.getInstance(application)
    private val dao = database.comandoSocialDao()
    private val api = ApiClient.getClient(application).create(ComandoSocialApi::class.java)

    // ---- Cache local ------------------------------------------------------
    var comandosLocais: List<ComandoSocialEntity> = emptyList()
        private set

    // ---- LiveData ---------------------------------------------------------
    private val _comandos = MutableLiveData<List<ComandoSocial>>()
    val comandos: LiveData<List<ComandoSocial>> = _comandos

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String>()
    val errorMessage: LiveData<String> = _errorMessage

    // -----------------------------------------------------------------------
    // Carregamento (Room + Servidor)
    // -----------------------------------------------------------------------
    fun carregarComandos() {
        Log.d("ComandosSociaisViewModel", "Iniciando carregamento de comandos")
        _isLoading.value = true

        viewModelScope.launch {
            try {
                // 1️⃣ Cache local
                comandosLocais = dao.getAll()
                if (comandosLocais.isNotEmpty()) {
                    _comandos.value = comandosLocais.map {
                        ComandoSocial(it.id, it.userId, it.comando, it.resposta, it.respostasAleatorias)
                    }
                }

                // 2️⃣ Busca remota se estiver online
                if (isOnline()) {
                    val response = api.listarComandos()
                    if (response.isSuccessful) {
                        val wrapper = response.body()
                        val comandosRemotos = wrapper?.data ?: emptyList()
                        _comandos.value = comandosRemotos

                        // Atualiza Room
                        dao.deleteAll()
                        dao.insertAll(comandosRemotos.map {
                            ComandoSocialEntity(
                                it.id,
                                it.userId,
                                it.comando,
                                it.resposta,
                                it.respostasAleatorias
                            )
                        })

                        // Atualiza cache em memória
                        comandosLocais = dao.getAll()
                        Log.d("ComandosSociaisViewModel", "Comandos atualizados: ${comandosLocais.size}")
                    }
                }
            } catch (e: Exception) {
                Log.e("ComandosSociais", "Erro ao carregar comandos: ${e.message}")
                _errorMessage.value = "Erro ao carregar comandos: ${e.message}"
            } finally {
                _isLoading.value = false
            }
        }
    }

    // -----------------------------------------------------------------------
    // Criação / Atualização / Exclusão (com tratamento de saldo)
    // -----------------------------------------------------------------------
    fun criarComando(comando: String, resposta: String, onSuccess: () -> Unit = {}) {
        _isLoading.value = true
        viewModelScope.launch {
            try {
                val request = CriarComandoSocialRequest(comando = comando, resposta = resposta)
                    val response = api.criarComando(request)

                    if (response.code() == 402) {
                        _errorMessage.value = "Saldo insuficiente. Recarregue ou faça upgrade."
                        speakTextFromService("Você não tem StarkCoins suficientes.")
                    } else if (response.isSuccessful) {
                    carregarComandos()
                    delay(600)                // pequeno “buffer” para garantir DB atualizado
                    comandosLocais = dao.getAll()
                    onSuccess()
                } else {
                    val errorText = response.errorBody()?.string() ?: "Erro desconhecido"
                    _errorMessage.value = "Erro ao criar: $errorText"
                    if (errorText.contains("Saldo insuficiente")) {
                        speakTextFromService("Saldo insuficiente para criar o comando")
                    }
                }
            } catch (e: Exception) {
                _errorMessage.value = "Falha na conexão: ${e.message}"
                Log.e("ComandosSociais", "Erro ao criar comando: ${e.message}")
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun atualizarComando(comandoSocial: ComandoSocial, onSuccess: () -> Unit = {}) {
        _isLoading.value = true
        viewModelScope.launch {
            try {
                val response = api.atualizarComando(comandoSocial.id, comandoSocial)

                if (response.code() == 402) {
                    _errorMessage.value = "Saldo insuficiente. Recarregue ou faça upgrade."
                    speakTextFromService("Você não tem StarkCoins suficientes.")
                } else if (response.isSuccessful) {
                    carregarComandos()
                    onSuccess()
                } else {
                    val errorText = response.errorBody()?.string() ?: "Erro desconhecido"
                    _errorMessage.value = "Erro ao atualizar: $errorText"
                    if (errorText.contains("Saldo insuficiente")) {
                        speakTextFromService("Saldo insuficiente para atualizar o comando")
                    }
                }
            } catch (e: Exception) {
                _errorMessage.value = "Falha na conexão: ${e.message}"
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun excluirComando(id: String, onSuccess: () -> Unit = {}) {
        _isLoading.value = true
        viewModelScope.launch {
            try {
                val response = api.excluirComando(id)
                if (response.isSuccessful) {
                    carregarComandos()
                    onSuccess()
                } else {
                    _errorMessage.value = "Erro ao excluir: ${response.errorBody()?.string()}"
                }
            } catch (e: Exception) {
                _errorMessage.value = "Falha na conexão: ${e.message}"
            } finally {
                _isLoading.value = false
            }
        }
    }

    // -----------------------------------------------------------------------
    // Funções auxiliares
    // -----------------------------------------------------------------------
    private fun isOnline(): Boolean {
        val connectivityManager = getApplication<Application>()
            .getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = connectivityManager.activeNetwork ?: return false
        val capabilities = connectivityManager.getNetworkCapabilities(network) ?: return false

        return capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) ||
                capabilities.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) ||
                capabilities.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET)
    }

    fun speakTextFromService(text: String) {
        val intent = Intent(getApplication<Application>(), FullDuplexAssistantAdvancedService::class.java).apply {
            action = "SPEAK_TEXT"
            putExtra("text", text)
        }
        getApplication<Application>().startForegroundService(intent)
    }
}
