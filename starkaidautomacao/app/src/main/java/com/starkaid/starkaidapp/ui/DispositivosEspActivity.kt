package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.floatingactionbutton.FloatingActionButton
import com.google.android.material.textfield.TextInputEditText
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.models.*
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.DispositivosEspApi
import com.starkaid.starkaidapp.services.FullDuplexAssistantAdvancedService
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject

class DispositivosEspActivity : BaseActivity() {

    private lateinit var sessionManager: SessionManager
    private lateinit var recyclerView: RecyclerView
    private lateinit var progressBar: ProgressBar
    private lateinit var emptyState: TextView
    private lateinit var fabAddDispositivo: FloatingActionButton
    private lateinit var toolbar: MaterialToolbar
    private var dispositivoList = mutableListOf<DispositivoEsp>()
    private lateinit var adapter: DispositivoEspAdapter
    
    // WebSocket Hub para receber respostas
    private var hubConnection: HubConnection? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_dispositivos_esp)

        sessionManager = SessionManager(this)
        
        // Verificar sessão
        val userId = sessionManager.fetchUserId()
        val apiKey = sessionManager.fetchApiKey()
        val authToken = sessionManager.fetchAuthToken()
        
        if (userId.isNullOrEmpty() || apiKey.isNullOrEmpty() || authToken.isNullOrEmpty()) {
            Toast.makeText(this, "Sessão inválida, faça login novamente.", Toast.LENGTH_LONG).show()
            finish()
            return
        }

        initViews()
        setupRecyclerView()
        setupToolbar()
        connectWebSocketHub()
        loadDispositivos()
    }

    private fun initViews() {
        recyclerView = findViewById(R.id.recyclerViewDispositivos)
        progressBar = findViewById(R.id.progressBar)
        emptyState = findViewById(R.id.textEmptyState)
        fabAddDispositivo = findViewById(R.id.fabAddDispositivo)
        toolbar = findViewById(R.id.toolbar)

        fabAddDispositivo.setOnClickListener {
            showAddDispositivoDialog()
        }
    }

    private fun setupRecyclerView() {
        adapter = DispositivoEspAdapter(dispositivoList, object : DispositivoEspAdapter.OnDispositivoClickListener {
            override fun onEditClick(dispositivo: DispositivoEsp) {
                showEditDispositivoDialog(dispositivo)
            }

            override fun onDeleteClick(dispositivo: DispositivoEsp) {
                confirmDeleteDispositivo(dispositivo)
            }

            override fun onPingClick(dispositivo: DispositivoEsp) {
                pingDispositivo(dispositivo.id)
            }

            override fun onEnviarClick(dispositivo: DispositivoEsp) {
                showEnviarComandoDialog(dispositivo)
            }
        })
        
        recyclerView.layoutManager = LinearLayoutManager(this)
        recyclerView.adapter = adapter
    }

    private fun setupToolbar() {
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)
        toolbar.setNavigationOnClickListener {
            onBackPressed()
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }

    // Conectar ao WebSocket Hub para receber respostas
    private fun connectWebSocketHub() {
        val token = sessionManager.fetchAuthToken() ?: return
        val userId = sessionManager.fetchUserId() ?: return

        try {
            hubConnection = HubConnectionBuilder.create("${ApiConfig.webBaseUrl}/hubs/dispositivo-esp?type=app")
                .withAccessTokenProvider(Single.defer { Single.just(token) })
                .build()

            // Listener para receber respostas dos dispositivos
            hubConnection?.on("RespostaDispositivo", { data: Any ->
                try {
                    Log.d("ESP_HUB", "Resposta recebida (raw): $data")
                    
                    // Processar como Map (LinkedTreeMap do SignalR)
                    val resposta: String = when (data) {
                        is Map<*, *> -> {
                            // Extrair a resposta do Map
                            val respostaRaw = data["resposta"]?.toString() ?: ""
                            
                            // Verificar se contém "toApp:" e remover o prefixo
                            if (respostaRaw.startsWith("toApp:")) {
                                respostaRaw.substringAfter("toApp:").trim()
                            } else {
                                respostaRaw
                            }
                        }
                        is String -> {
                            // Se for string, verificar se contém "toApp:"
                            if (data.startsWith("toApp:")) {
                                data.substringAfter("toApp:").trim()
                            } else {
                                data
                            }
                        }
                        else -> {
                            // Tentar converter para string e processar
                            val str = data.toString()
                            if (str.startsWith("toApp:")) {
                                str.substringAfter("toApp:").trim()
                            } else {
                                str
                            }
                        }
                    }
                    
                    Log.d("ESP_HUB", "Resposta processada: $resposta")
                    
                    // Falar apenas a resposta (sem prefixos)
                    if (resposta.isNotEmpty()) {
                        runOnUiThread {
                            speakTextFromService(resposta)
                        }
                    }
                } catch (e: Exception) {
                    Log.e("ESP_HUB", "Erro ao processar resposta", e)
                }
            }, Any::class.java)

            // Listener para atualizações de status
            hubConnection?.on("StatusDispositivoAtualizado", { data: Any ->
                try {
                    Log.d("ESP_HUB", "Status atualizado: $data")
                    // Recarregar lista quando status for atualizado
                    loadDispositivos()
                } catch (e: Exception) {
                    Log.e("ESP_HUB", "Erro ao processar atualização de status", e)
                }
            }, Any::class.java)

            hubConnection?.start()?.blockingAwait()
            Log.d("ESP_HUB", "✅ Conectado ao DispositivoESP Hub")
            
            // Identificar cliente
            hubConnection?.invoke("IdentificarCliente", "app", userId)
        } catch (e: Exception) {
            Log.e("ESP_HUB", "Erro ao conectar ao Hub", e)
        }
    }

    private fun loadDispositivos() {
        val authToken = sessionManager.fetchAuthToken() ?: return

        progressBar.visibility = View.VISIBLE
        emptyState.visibility = View.GONE

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DispositivosEspActivity)
                val api = retrofit.create(DispositivosEspApi::class.java)
                
                val response = api.listarDispositivos()
                
                withContext(Dispatchers.Main) {
                    progressBar.visibility = View.GONE
                    
                    if (response.isSuccessful) {
                        dispositivoList.clear()
                        response.body()?.let { dispositivoList.addAll(it) }
                        adapter.notifyDataSetChanged()
                        
                        if (dispositivoList.isEmpty()) {
                            emptyState.visibility = View.VISIBLE
                        } else {
                            emptyState.visibility = View.GONE
                        }
                    } else {
                        Toast.makeText(
                            this@DispositivosEspActivity,
                            "Erro ao carregar dispositivos: ${response.code()}",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    progressBar.visibility = View.GONE
                    Toast.makeText(
                        this@DispositivosEspActivity,
                        "Erro: ${e.message}",
                        Toast.LENGTH_SHORT
                    ).show()
                    Log.e("DispositivosESP", "Erro ao carregar dispositivos", e)
                }
            }
        }
    }

    private fun showAddDispositivoDialog() {
        val dialog = AlertDialog.Builder(this).create()
        val view = LayoutInflater.from(this).inflate(R.layout.dialog_add_edit_dispositivo_esp, null)

        val editNome = view.findViewById<TextInputEditText>(R.id.editNome)
        val editIp = view.findViewById<TextInputEditText>(R.id.editIp)
        val editPorta = view.findViewById<TextInputEditText>(R.id.editPorta)
        val editComando = view.findViewById<TextInputEditText>(R.id.editComando)
        val editComandToEsp = view.findViewById<TextInputEditText>(R.id.editComandToEsp)
        val textTitle = view.findViewById<TextView>(R.id.textTitle)
        
        textTitle.text = "Adicionar Dispositivo ESP"

        view.findViewById<Button>(R.id.buttonSave).setOnClickListener {
            val nome = editNome.text.toString().trim()
            val ip = editIp.text.toString().trim()
            val porta = editPorta.text.toString().trim().toIntOrNull()
            val comando = editComando.text.toString().trim().takeIf { it.isNotEmpty() }
            val comandToEsp = editComandToEsp.text.toString().trim().takeIf { it.isNotEmpty() }

            if (nome.isEmpty() || ip.isEmpty() || porta == null) {
                Toast.makeText(this, "Preencha todos os campos obrigatórios", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            criarDispositivo(nome, ip, porta, comando, comandToEsp)
            dialog.dismiss()
        }

        view.findViewById<Button>(R.id.buttonCancel).setOnClickListener {
            dialog.dismiss()
        }

        dialog.setView(view)
        dialog.show()
    }

    private fun showEditDispositivoDialog(dispositivo: DispositivoEsp) {
        val dialog = AlertDialog.Builder(this).create()
        val view = LayoutInflater.from(this).inflate(R.layout.dialog_add_edit_dispositivo_esp, null)

        val editNome = view.findViewById<TextInputEditText>(R.id.editNome)
        val editIp = view.findViewById<TextInputEditText>(R.id.editIp)
        val editPorta = view.findViewById<TextInputEditText>(R.id.editPorta)
        val editComando = view.findViewById<TextInputEditText>(R.id.editComando)
        val editComandToEsp = view.findViewById<TextInputEditText>(R.id.editComandToEsp)
        val textTitle = view.findViewById<TextView>(R.id.textTitle)
        
        textTitle.text = "Editar Dispositivo ESP"
        
        editNome.setText(dispositivo.nome)
        editIp.setText(dispositivo.ip)
        editPorta.setText(dispositivo.porta.toString())
        editComando.setText(dispositivo.comando ?: "")
        editComandToEsp.setText(dispositivo.comandToEsp ?: "")

        view.findViewById<Button>(R.id.buttonSave).setOnClickListener {
            val nome = editNome.text.toString().trim()
            val ip = editIp.text.toString().trim()
            val porta = editPorta.text.toString().trim().toIntOrNull()
            val comando = editComando.text.toString().trim().takeIf { it.isNotEmpty() }
            val comandToEsp = editComandToEsp.text.toString().trim().takeIf { it.isNotEmpty() }

            if (nome.isEmpty() || ip.isEmpty() || porta == null) {
                Toast.makeText(this, "Preencha todos os campos obrigatórios", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            atualizarDispositivo(dispositivo.id, nome, ip, porta, comando, comandToEsp)
            dialog.dismiss()
        }

        view.findViewById<Button>(R.id.buttonCancel).setOnClickListener {
            dialog.dismiss()
        }

        dialog.setView(view)
        dialog.show()
    }

    private fun showEnviarComandoDialog(dispositivo: DispositivoEsp) {
        val dialog = AlertDialog.Builder(this).create()
        val view = LayoutInflater.from(this).inflate(R.layout.dialog_enviar_comando_esp, null)

        val editComando = view.findViewById<TextInputEditText>(R.id.editComando)
        editComando.setText(dispositivo.comando ?: "")

        view.findViewById<Button>(R.id.buttonEnviar).setOnClickListener {
            val comando = editComando.text.toString().trim()
            if (comando.isEmpty()) {
                Toast.makeText(this, "Digite um comando", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }
            enviarComando(comando)
            dialog.dismiss()
        }

        view.findViewById<Button>(R.id.buttonCancel).setOnClickListener {
            dialog.dismiss()
        }

        dialog.setView(view)
        dialog.show()
    }

    private fun criarDispositivo(nome: String, ip: String, porta: Int, comando: String?, comandToEsp: String?) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DispositivosEspActivity)
                val api = retrofit.create(DispositivosEspApi::class.java)
                
                val request = CreateDispositivoEspRequest(nome, ip, porta, comando, comandToEsp)
                val response = api.criarDispositivo(request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@DispositivosEspActivity, "Dispositivo criado com sucesso", Toast.LENGTH_SHORT).show()
                        speakTextFromService("Dispositivo ESP $nome criado com sucesso")
                        loadDispositivos()
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Toast.makeText(this@DispositivosEspActivity, "Erro ao criar dispositivo: ${response.code()}", Toast.LENGTH_SHORT).show()
                        Log.e("DispositivosESP", "Erro: $errorBody")
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@DispositivosEspActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                    Log.e("DispositivosESP", "Erro ao criar dispositivo", e)
                }
            }
        }
    }

    private fun atualizarDispositivo(id: String, nome: String, ip: String, porta: Int, comando: String?, comandToEsp: String?) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DispositivosEspActivity)
                val api = retrofit.create(DispositivosEspApi::class.java)
                
                val request = UpdateDispositivoEspRequest(nome, ip, porta, comando, comandToEsp, null, null)
                val response = api.atualizarDispositivo(id, request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@DispositivosEspActivity, "Dispositivo atualizado com sucesso", Toast.LENGTH_SHORT).show()
                        speakTextFromService("Dispositivo ESP $nome atualizado com sucesso")
                        loadDispositivos()
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Toast.makeText(this@DispositivosEspActivity, "Erro ao atualizar dispositivo: ${response.code()}", Toast.LENGTH_SHORT).show()
                        Log.e("DispositivosESP", "Erro: $errorBody")
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@DispositivosEspActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                    Log.e("DispositivosESP", "Erro ao atualizar dispositivo", e)
                }
            }
        }
    }

    private fun confirmDeleteDispositivo(dispositivo: DispositivoEsp) {
        AlertDialog.Builder(this)
            .setTitle("Confirmar Exclusão")
            .setMessage("Tem certeza que deseja excluir o dispositivo ${dispositivo.nome}?")
            .setPositiveButton("Excluir") { _, _ ->
                excluirDispositivo(dispositivo.id, dispositivo.nome)
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun excluirDispositivo(id: String, nome: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DispositivosEspActivity)
                val api = retrofit.create(DispositivosEspApi::class.java)
                
                val response = api.excluirDispositivo(id)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@DispositivosEspActivity, "Dispositivo excluído com sucesso", Toast.LENGTH_SHORT).show()
                        speakTextFromService("Dispositivo ESP $nome excluído com sucesso")
                        loadDispositivos()
                    } else {
                        Toast.makeText(this@DispositivosEspActivity, "Erro ao excluir dispositivo: ${response.code()}", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@DispositivosEspActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                    Log.e("DispositivosESP", "Erro ao excluir dispositivo", e)
                }
            }
        }
    }

    private fun pingDispositivo(id: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DispositivosEspActivity)
                val api = retrofit.create(DispositivosEspApi::class.java)
                
                val response = api.pingDispositivo(id)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        val pingResponse = response.body()
                        val status = pingResponse?.status ?: "Desconhecido"
                        val isOnline = pingResponse?.isOnline ?: false
                        
                        val mensagem = if (isOnline) {
                            "Dispositivo está online. Status: $status"
                        } else {
                            "Dispositivo está offline. Status: $status"
                        }
                        
                        Toast.makeText(this@DispositivosEspActivity, mensagem, Toast.LENGTH_SHORT).show()
                        speakTextFromService(mensagem)
                        loadDispositivos() // Recarregar para atualizar status
                    } else {
                        Toast.makeText(this@DispositivosEspActivity, "Erro ao fazer ping: ${response.code()}", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@DispositivosEspActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                    Log.e("DispositivosESP", "Erro ao fazer ping", e)
                }
            }
        }
    }

    private fun enviarComando(comando: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DispositivosEspActivity)
                val api = retrofit.create(DispositivosEspApi::class.java)
                
                val request = EnviarComandoRequest(comando)
                val response = api.enviarComando(request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        // Apenas enviar o comando silenciosamente
                        // A resposta será recebida via WebSocket e falada automaticamente
                        Toast.makeText(this@DispositivosEspActivity, "Comando enviado", Toast.LENGTH_SHORT).show()
                    } else {
                        val errorBody = response.errorBody()?.string()
                        val errorMsg = errorBody ?: "Erro ao enviar comando: ${response.code()}"
                        Toast.makeText(this@DispositivosEspActivity, errorMsg, Toast.LENGTH_SHORT).show()
                        speakTextFromService(errorMsg)
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@DispositivosEspActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                    speakTextFromService("Erro ao enviar comando: ${e.message}")
                    Log.e("DispositivosESP", "Erro ao enviar comando", e)
                }
            }
        }
    }

    private fun speakTextFromService(text: String) {
        val intent = android.content.Intent(this, FullDuplexAssistantAdvancedService::class.java)
        intent.action = "SPEAK_TEXT"
        intent.putExtra("text", text)
        startForegroundService(intent)
    }

    override fun onDestroy() {
        super.onDestroy()
        hubConnection?.stop()?.blockingAwait()
        hubConnection = null
    }

    // Adapter para RecyclerView
    class DispositivoEspAdapter(
        private val dispositivos: List<DispositivoEsp>,
        private val listener: OnDispositivoClickListener
    ) : RecyclerView.Adapter<DispositivoEspAdapter.DispositivoViewHolder>() {

        interface OnDispositivoClickListener {
            fun onEditClick(dispositivo: DispositivoEsp)
            fun onDeleteClick(dispositivo: DispositivoEsp)
            fun onPingClick(dispositivo: DispositivoEsp)
            fun onEnviarClick(dispositivo: DispositivoEsp)
        }

        inner class DispositivoViewHolder(view: View) : RecyclerView.ViewHolder(view) {
            val textNome: TextView = view.findViewById(R.id.textNome)
            val textStatus: TextView = view.findViewById(R.id.textStatus)
            val textIp: TextView = view.findViewById(R.id.textIp)
            val textPorta: TextView = view.findViewById(R.id.textPorta)
            val textComando: TextView = view.findViewById(R.id.textComando)
            val textComandToEsp: TextView = view.findViewById(R.id.textComandToEsp)
            val buttonPing: Button = view.findViewById(R.id.buttonPing)
            val buttonEnviar: Button = view.findViewById(R.id.buttonEnviar)
            val buttonEditar: Button = view.findViewById(R.id.buttonEditar)
            val buttonExcluir: Button = view.findViewById(R.id.buttonExcluir)
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): DispositivoViewHolder {
            val view = LayoutInflater.from(parent.context)
                .inflate(R.layout.item_dispositivo_esp, parent, false)
            return DispositivoViewHolder(view)
        }

        override fun onBindViewHolder(holder: DispositivoViewHolder, position: Int) {
            val dispositivo = dispositivos[position]
            
            holder.textNome.text = dispositivo.nome
            holder.textIp.text = "IP: ${dispositivo.ip}"
            holder.textPorta.text = "Porta: ${dispositivo.porta}"
            holder.textComando.text = "Comando: ${dispositivo.comando ?: "N/A"}"
            holder.textComandToEsp.text = "Comando ESP: ${dispositivo.comandToEsp ?: "N/A"}"
            
            // Status
            val isConectado = dispositivo.status == "Conectado"
            holder.textStatus.text = dispositivo.status
            holder.textStatus.setTextColor(
                if (isConectado) android.graphics.Color.parseColor("#4CAF50")
                else android.graphics.Color.parseColor("#FF5252")
            )
            
            holder.buttonPing.setOnClickListener {
                listener.onPingClick(dispositivo)
            }
            
            holder.buttonEnviar.setOnClickListener {
                listener.onEnviarClick(dispositivo)
            }
            
            holder.buttonEditar.setOnClickListener {
                listener.onEditClick(dispositivo)
            }
            
            holder.buttonExcluir.setOnClickListener {
                listener.onDeleteClick(dispositivo)
            }
        }

        override fun getItemCount() = dispositivos.size
    }
}

