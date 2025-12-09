package com.starkaid.starkaidapp.ui

import android.content.Intent
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.Uri
import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.databinding.ActivityContratarPlanosBinding
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.AssinaturasApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class ContratarPlanosActivity : AppCompatActivity() {

    private lateinit var binding: ActivityContratarPlanosBinding
    private lateinit var sessionManager: SessionManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityContratarPlanosBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        // Checagem de integridade de sessão
        val userId = sessionManager.fetchUserId()
        val apiKey = sessionManager.fetchApiKey()
        val authToken = sessionManager.fetchAuthToken()

        if (userId.isNullOrEmpty() || apiKey.isNullOrEmpty() || authToken.isNullOrEmpty()) {
            Toast.makeText(this, "Sessão inválida, faça login novamente.", Toast.LENGTH_LONG).show()
            finish()
            return
        }

        // Configurar botões dos planos
        binding.btnNivel2.setOnClickListener { contratarPlano(2) }
        binding.btnNivel3.setOnClickListener { contratarPlano(3) }
        binding.btnNivel4.setOnClickListener { contratarPlano(4) }
        binding.btnNivel5.setOnClickListener { contratarPlano(5) }
        binding.btnNivel6.setOnClickListener { contratarPlano(6) }
        binding.btnNivel7.setOnClickListener { contratarPlano(7) }
    }

    private fun contratarPlano(nivel: Int) {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        if (!isOnline()) {
            Toast.makeText(this, "Sem conexão com a internet", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@ContratarPlanosActivity)
                val assinaturasApi = retrofit.create(AssinaturasApi::class.java)

                // Usar o endpoint checkout que aceita JSON body
                val request = com.starkaid.starkaidapp.services.CheckoutRequest(nivel = nivel)
                val response = assinaturasApi.checkout(request)

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val checkoutUrl = response.body()!!.checkoutUrl
                        openInBrowser(checkoutUrl)
                    } else {
                        val errorBody = response.errorBody()?.string() ?: "Erro desconhecido"
                        Log.e("ContratarPlano", "Erro: ${response.code()} - $errorBody")
                        Toast.makeText(
                            this@ContratarPlanosActivity,
                            "Falha ao processar plano (${response.code()}): $errorBody",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("ContratarPlano", "Erro ao contratar plano", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@ContratarPlanosActivity,
                        "Erro de rede: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    private fun openInBrowser(url: String) {
        try {
            startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url)))
        } catch (ex: Exception) {
            Log.e("ContratarPlano", "openInBrowser failed", ex)
            Toast.makeText(this, "Não foi possível abrir o navegador", Toast.LENGTH_SHORT).show()
        }
    }

    @Suppress("DEPRECATION")
    private fun isOnline(): Boolean {
        val connectivityManager = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = connectivityManager.activeNetwork
        val capabilities = connectivityManager.getNetworkCapabilities(network)
        return capabilities != null && (capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) ||
                capabilities.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) ||
                capabilities.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET))
    }
}
