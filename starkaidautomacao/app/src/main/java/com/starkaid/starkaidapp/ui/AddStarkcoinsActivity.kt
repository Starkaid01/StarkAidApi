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
import com.starkaid.starkaidapp.databinding.ActivityAddStarkcoinsBinding
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.UsersApi
import com.starkaid.starkaidapp.services.AddFundsRequest
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class AddStarkcoinsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityAddStarkcoinsBinding
    private lateinit var sessionManager: SessionManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityAddStarkcoinsBinding.inflate(layoutInflater)
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

        // Configurar título
        binding.titleTextView.text = "Adicionar Fundos - StarkCoins"

        // Configurar botões de valores sugeridos
        binding.btnValue10.setOnClickListener { setAmount(10.0) }
        binding.btnValue25.setOnClickListener { setAmount(25.0) }
        binding.btnValue50.setOnClickListener { setAmount(50.0) }
        binding.btnValue100.setOnClickListener { setAmount(100.0) }

        // Configurar clique do botão de pagamento
        binding.payButton.setOnClickListener {
            onPayButtonClicked()
        }
    }

    private fun setAmount(amount: Double) {
        binding.fundsAmount.setText(amount.toString())
    }

    private fun onPayButtonClicked() {
        val amountText = binding.fundsAmount.text.toString()
        
        if (amountText.isEmpty()) {
            Toast.makeText(this, "Por favor, informe um valor", Toast.LENGTH_SHORT).show()
            return
        }

        val amount = try {
            amountText.toDouble()
        } catch (e: NumberFormatException) {
            Toast.makeText(this, "Valor inválido", Toast.LENGTH_SHORT).show()
            return
        }

        if (amount <= 0) {
            Toast.makeText(this, "Valor deve ser maior que zero", Toast.LENGTH_SHORT).show()
            return
        }

        if (!isOnline()) {
            Toast.makeText(this, "Sem conexão com a internet", Toast.LENGTH_LONG).show()
            return
        }

        addFunds(amount)
    }

    private fun addFunds(amount: Double) {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AddStarkcoinsActivity)
                val usersApi = retrofit.create(UsersApi::class.java)
                val addFundsRequest = AddFundsRequest(amount = amount)
                val response = usersApi.addFunds(addFundsRequest)

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val checkoutUrl = response.body()!!.checkoutUrl
                        openInBrowser(checkoutUrl)
                    } else {
                        val errorBody = response.errorBody()?.string() ?: "Erro desconhecido"
                        Log.e("AddFunds", "Erro: ${response.code()} - $errorBody")
                        Toast.makeText(
                            this@AddStarkcoinsActivity,
                            "Falha ao processar pagamento (${response.code()}): $errorBody",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("AddFunds", "Erro ao adicionar fundos", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AddStarkcoinsActivity,
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
            Log.e("AddFunds", "openInBrowser failed", ex)
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