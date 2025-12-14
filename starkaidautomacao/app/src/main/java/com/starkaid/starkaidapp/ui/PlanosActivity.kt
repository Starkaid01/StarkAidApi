package com.starkaid.starkaidapp.ui

import android.content.Intent
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Toast
import androidx.recyclerview.widget.LinearLayoutManager
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.databinding.ActivityPlanosBinding
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.AssinaturasApi
import com.starkaid.starkaidapp.services.PlanoAtivoResponse
import com.starkaid.starkaidapp.ui.adapters.PlanosAdapter
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class PlanosActivity : AppCompatActivity() {
    private lateinit var binding: ActivityPlanosBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var adapter: PlanosAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityPlanosBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        setupRecyclerView()
        setupButtons()
        loadPlanosAtivos()
    }

    private fun setupButtons() {
        val goToCheckout = {
            val intent = Intent(this, ContratarPlanosActivity::class.java)
            startActivity(intent)
        }
        binding.btnContratarPlano.setOnClickListener { goToCheckout() }
        binding.btnUpgradePremium.setOnClickListener { goToCheckout() }
    }

    override fun onResume() {
        super.onResume()
        loadPlanosAtivos()
    }

    private fun setupRecyclerView() {
        adapter = PlanosAdapter(
            planos = emptyList(),
            onCancelarClick = { plano ->
                cancelarPlano(plano)
            }
        )

        binding.planosRecyclerView.layoutManager = LinearLayoutManager(this)
        binding.planosRecyclerView.adapter = adapter
    }

    private fun loadPlanosAtivos() {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@PlanosActivity)
                val api = retrofit.create(AssinaturasApi::class.java)
                val response = api.listarAtivas()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val planos = response.body()!!
                        adapter.updatePlanos(planos)

                        val temPremium = planos.any { it.nivel == 2 && it.status.equals("ativa", true) }

                        binding.cardFreeBenefits.visibility = if (temPremium) View.GONE else View.VISIBLE
                        binding.btnContratarPlano.visibility = if (temPremium) View.GONE else View.VISIBLE

                        if (adapter.itemCount == 0) {
                            binding.emptyState.visibility = View.VISIBLE
                            binding.planosRecyclerView.visibility = View.GONE
                        } else {
                            binding.emptyState.visibility = View.GONE
                            binding.planosRecyclerView.visibility = View.VISIBLE
                        }
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("Planos", "Erro ao buscar planos: ${response.code()} - $errorBody")
                        Toast.makeText(
                            this@PlanosActivity,
                            "Erro ao carregar planos: ${response.code()}",
                            Toast.LENGTH_LONG
                        ).show()
                        binding.emptyState.visibility = View.VISIBLE
                        binding.planosRecyclerView.visibility = View.GONE
                    }
                }
            } catch (e: Exception) {
                Log.e("Planos", "Erro ao buscar planos", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@PlanosActivity,
                        "Erro ao buscar planos: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                    binding.emptyState.visibility = View.VISIBLE
                    binding.planosRecyclerView.visibility = View.GONE
                }
            }
        }
    }

    private fun cancelarPlano(plano: PlanoAtivoResponse) {
        val builder = AlertDialog.Builder(this)
        builder.setTitle("Confirmar Cancelamento")
        builder.setMessage("Tem certeza que deseja cancelar o plano ${plano.nomePlano}? Esta ação não pode ser desfeita.")
        builder.setPositiveButton("Sim") { dialog, _ ->
            dialog.dismiss()
            executarCancelamento(plano.id)
        }
        builder.setNegativeButton("Não") { dialog, _ ->
            dialog.dismiss()
        }
        builder.show()
    }

    private fun executarCancelamento(assinaturaId: String) {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@PlanosActivity)
                val api = retrofit.create(AssinaturasApi::class.java)
                val response = api.cancelarAssinaturaPorId(assinaturaId)

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        Toast.makeText(
                            this@PlanosActivity,
                            "Plano cancelado com sucesso!",
                            Toast.LENGTH_LONG
                        ).show()
                        loadPlanosAtivos() // Recarrega a lista
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("CancelarPlano", "Erro: ${response.code()} - $errorBody")
                        Toast.makeText(
                            this@PlanosActivity,
                            "Erro ao cancelar plano (${response.code()})",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("CancelarPlano", "Erro ao cancelar", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@PlanosActivity,
                        "Erro: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }
}
