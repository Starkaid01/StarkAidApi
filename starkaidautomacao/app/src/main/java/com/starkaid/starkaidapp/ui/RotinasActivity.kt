package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.adapters.RotinasAdapter
import com.starkaid.starkaidapp.databinding.ActivityRotinasBinding
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.RotinaDto
import com.starkaid.starkaidapp.services.RotinasApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class RotinasActivity : AppCompatActivity() {
    private lateinit var binding: ActivityRotinasBinding
    private val rotinasList = mutableListOf<RotinaDto>()
    private lateinit var adapter: RotinasAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityRotinasBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupToolbar()
        setupRecyclerView()
        setupButtons()
        loadRotinas()
    }

    override fun onResume() {
        super.onResume()
        loadRotinas() // Recarrega ao voltar do editor
    }

    private fun setupToolbar() {
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        binding.toolbar.setNavigationOnClickListener { finish() }
    }

    private fun setupRecyclerView() {
        adapter = RotinasAdapter(rotinasList, 
            onToggle = { id, ativa -> setAtiva(id, ativa) },
            onExecute = { id -> executarRotina(id) },
            onDelete = { id -> confirmarExclusao(id) },
            onItemClick = { rotina -> 
                val intent = android.content.Intent(this, EditarRotinaActivity::class.java)
                intent.putExtra("ROTINA_ID", rotina.id)
                startActivity(intent)
            }
        )
        binding.rvRotinas.layoutManager = LinearLayoutManager(this)
        binding.rvRotinas.adapter = adapter
    }

    private fun setupButtons() {
        binding.fabAddRotina.setOnClickListener {
            val intent = android.content.Intent(this, EditarRotinaActivity::class.java)
            startActivity(intent)
        }
    }

    private fun loadRotinas() {
        binding.progressBar.visibility = View.VISIBLE
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@RotinasActivity).create(RotinasApi::class.java)
                val response = api.getAll()
                withContext(Dispatchers.Main) {
                    binding.progressBar.visibility = View.GONE
                    if (response.isSuccessful && response.body() != null) {
                        rotinasList.clear()
                        rotinasList.addAll(response.body()!!)
                        adapter.notifyDataSetChanged()
                    } else {
                        Toast.makeText(this@RotinasActivity, "Erro ao carregar rotinas", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("Rotinas", "Erro", e)
                withContext(Dispatchers.Main) {
                    binding.progressBar.visibility = View.GONE
                    Toast.makeText(this@RotinasActivity, "Erro de conexão", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun setAtiva(id: String, ativa: Boolean) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@RotinasActivity).create(RotinasApi::class.java)
                val response = if (ativa) api.ativar(id) else api.desativar(id)
                withContext(Dispatchers.Main) {
                    if (!response.isSuccessful) {
                        Toast.makeText(this@RotinasActivity, "Erro ao alterar estado", Toast.LENGTH_SHORT).show()
                        loadRotinas() // Reverte UI
                    } else {
                        // Atualiza localmente para evitar reload completo
                        val index = rotinasList.indexOfFirst { it.id == id }
                        if (index != -1) {
                            // Infelizmente RotinaDto é val, então o ideal é o reload ou recriar o objeto
                            loadRotinas()
                        }
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) { loadRotinas() }
            }
        }
    }

    private fun executarRotina(id: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@RotinasActivity).create(RotinasApi::class.java)
                val response = api.executar(id)
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@RotinasActivity, "Rotina iniciada!", Toast.LENGTH_SHORT).show()
                    } else {
                        Toast.makeText(this@RotinasActivity, "Erro ao executar", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) { Toast.makeText(this@RotinasActivity, "Erro de conexão", Toast.LENGTH_SHORT).show() }
            }
        }
    }

    private fun confirmarExclusao(id: String) {
        AlertDialog.Builder(this)
            .setTitle("Excluir Rotina")
            .setMessage("Tem certeza que deseja excluir esta rotina?")
            .setPositiveButton("Excluir") { _, _ ->
                excluirRotina(id)
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun excluirRotina(id: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@RotinasActivity).create(RotinasApi::class.java)
                val response = api.delete(id)
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@RotinasActivity, "Rotina excluída", Toast.LENGTH_SHORT).show()
                        loadRotinas()
                    } else {
                        Toast.makeText(this@RotinasActivity, "Erro ao excluir", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) { Toast.makeText(this@RotinasActivity, "Erro de conexão", Toast.LENGTH_SHORT).show() }
            }
        }
    }

    private fun mostrarDetalhesRotina(rotina: RotinaDto) {
        val dialogView = layoutInflater.inflate(R.layout.dialog_rotina_detalhes, null)
        val tvTitulo = dialogView.findViewById<android.widget.TextView>(R.id.tvTituloRotina)
        val tvGatilhos = dialogView.findViewById<android.widget.TextView>(R.id.tvGatilhos)
        val tvAcoes = dialogView.findViewById<android.widget.TextView>(R.id.tvAcoes)
        val btnFechar = dialogView.findViewById<android.widget.Button>(R.id.btnFechar)

        tvTitulo.text = rotina.nome
        
        // Gatilhos
        val gatilhosText = StringBuilder()
        rotina.gatilhos.forEach { g ->
            val tipo = when (g.tipo?.toString()?.lowercase()) {
                "0", "tempo" -> "⏰ Horário: ${g.expressao}"
                "1", "comando" -> "🗣️ Comando: \"${g.expressao}\""
                "2", "evento" -> "📡 Evento: ${g.expressao}"
                else -> "❓ Desconhecido: ${g.expressao}"
            }
            gatilhosText.append("• $tipo\n")
        }
        tvGatilhos.text = if (gatilhosText.isEmpty()) "Nenhum gatilho" else gatilhosText.toString()

        // Ações
        val acoesText = StringBuilder()
        rotina.acoes.sortedBy { it.ordemExecucao }.forEachIndexed { index, a ->
            val tipo = when (a.tipo?.toString()?.lowercase()) {
                "0", "dispositivo" -> "🔌 Dispositivo"
                "1", "comando" -> "💬 Executar comando"
                "2", "delay" -> "⏳ Aguardar"
                "5", "comandoassistente" -> "🤖 Comando Assistente"
                else -> "❓ Ação"
            }
            
            // Tenta extrair info do payload
            var extra = ""
            try {
                val json = org.json.JSONObject(a.payload)
                extra = when (a.tipo?.toString()?.lowercase()) {
                    "0", "dispositivo" -> " (${json.optString("tipo")}: ${json.optString("action")})"
                    "1", "comando" -> ": \"${json.optString("comando")}\""
                    "2", "delay" -> ": ${json.optInt("seconds", json.optInt("delaySeconds"))}s"
                    "5", "comandoassistente" -> ": \"${json.optString("comando")}\""
                    else -> ""
                }
            } catch (e: Exception) {}

            acoesText.append("${index + 1}. $tipo$extra\n")
        }
        tvAcoes.text = if (acoesText.isEmpty()) "Nenhuma ação" else acoesText.toString()

        val dialog = AlertDialog.Builder(this)
            .setView(dialogView)
            .create()

        btnFechar.setOnClickListener { dialog.dismiss() }
        dialog.show()
    }
}
