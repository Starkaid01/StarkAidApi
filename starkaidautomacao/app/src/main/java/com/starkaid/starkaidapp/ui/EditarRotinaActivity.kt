package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.app.TimePickerDialog
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.databinding.ActivityEditarRotinaBinding
import com.starkaid.starkaidapp.services.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.util.*

class EditarRotinaActivity : AppCompatActivity() {
    private lateinit var binding: ActivityEditarRotinaBinding
    private var rotinaId: String? = null
    
    // Listas locais para o editor
    private val gatilhosList = mutableListOf<CreateRotinaGatilhoRequest>()
    private val acoesList = mutableListOf<CreateRotinaAcaoRequest>()
    private var availableDevices = listOf<DeviceSelectionDto>()

    private lateinit var gatilhosAdapter: ConfigAdapter<CreateRotinaGatilhoRequest>
    private lateinit var acoesAdapter: ConfigAdapter<CreateRotinaAcaoRequest>

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityEditarRotinaBinding.inflate(layoutInflater)
        setContentView(binding.root)

        rotinaId = intent.getStringExtra("ROTINA_ID")
        
        setupToolbar()
        setupRecyclerViews()
        setupButtons()
        
        loadAvailableDevices()
        
        if (rotinaId != null) {
            loadRotina()
        }
    }

    private fun setupToolbar() {
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        binding.toolbar.title = if (rotinaId == null) "Nova Rotina" else "Editar Rotina"
        binding.toolbar.setNavigationOnClickListener { finish() }
    }

    private fun setupRecyclerViews() {
        gatilhosAdapter = ConfigAdapter(gatilhosList, { item ->
            // Descrição do gatilho
            val prefix = when(item.tipo) {
                0 -> "⏰ Às "
                1 -> "🗣️ Ao dizer "
                2 -> "📡 Evento: "
                else -> "❓ "
            }
            "$prefix${item.expressao}"
        }, { position ->
            gatilhosList.removeAt(position)
            gatilhosAdapter.notifyItemRemoved(position)
        })

        acoesAdapter = ConfigAdapter(acoesList, { item ->
            // Descrição da ação
            val prefix = when(item.tipo) {
                0 -> "🔌 Dispositivo"
                1 -> "💬 Comando"
                2 -> "⏳ Aguardar"
                3 -> "🔔 Notificação"
                4 -> "🔗 Abrir URL"
                5 -> "🤖 Comando Assistente"
                else -> "❓ Ação"
            }
            
            var extra = ""
            try {
                val json = JSONObject(item.payload)
                extra = when(item.tipo) {
                    0 -> ": ${json.optString("name", "Dispositivo")} (${json.optString("action")})"
                    1 -> ": \"${json.optString("comando")}\""
                    2 -> ": ${json.optInt("seconds", json.optInt("delaySeconds"))}s"
                    3 -> ": ${json.optString("mensagem").take(20)}..."
                    4 -> ": ${json.optString("url")}"
                    5 -> ": \"${json.optString("comando")}\""
                    else -> ""
                }
            } catch (e: Exception) {}
            
            "$prefix$extra"
        }, { position ->
            acoesList.removeAt(position)
            acoesAdapter.notifyItemRemoved(position)
        })

        binding.rvGatilhos.layoutManager = LinearLayoutManager(this)
        binding.rvGatilhos.adapter = gatilhosAdapter

        binding.rvAcoes.layoutManager = LinearLayoutManager(this)
        binding.rvAcoes.adapter = acoesAdapter
    }

    private fun setupButtons() {
        binding.btnAddGatilho.setOnClickListener { showAddGatilhoDialog() }
        binding.btnAddAcao.setOnClickListener { showAddAcaoDialog() }
        binding.btnSalvar.setOnClickListener { salvarRotina() }
    }

    private fun loadAvailableDevices() {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@EditarRotinaActivity).create(ComodosApi::class.java)
                val response = api.getAvailableDevices()
                if (response.isSuccessful) {
                    availableDevices = response.body() ?: emptyList()
                }
            } catch (e: Exception) {
                Log.e("EditarRotina", "Erro ao carregar dispositivos", e)
            }
        }
    }

    private fun loadRotina() {
        binding.btnSalvar.isEnabled = false
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@EditarRotinaActivity).create(RotinasApi::class.java)
                val response = api.getById(rotinaId!!)
                withContext(Dispatchers.Main) {
                    binding.btnSalvar.isEnabled = true
                    if (response.isSuccessful && response.body() != null) {
                        val rotina = response.body()!!
                        binding.etNome.setText(rotina.nome)
                        binding.etDescricao.setText(rotina.descricao)
                        
                        gatilhosList.clear()
                        rotina.gatilhos.forEach { 
                            val tipoInt = when(it.tipo?.toString()?.lowercase()) {
                                "tempo", "0" -> 0
                                "comando", "1" -> 1
                                "evento", "2" -> 2
                                else -> 0
                            }
                            gatilhosList.add(CreateRotinaGatilhoRequest(tipoInt, it.expressao, it.diasSemana))
                        }
                        
                        acoesList.clear()
                        rotina.acoes.sortedBy { it.ordemExecucao }.forEach {
                            val tipoInt = when(it.tipo?.toString()?.lowercase()) {
                                "dispositivo", "0" -> 0
                                "comando", "1" -> 1
                                "delay", "2" -> 2
                                "notificacao", "3" -> 3
                                "abrirurl", "4" -> 4
                                "comandoassistente", "5" -> 5
                                else -> 0
                            }
                            acoesList.add(CreateRotinaAcaoRequest(it.ordemExecucao, tipoInt, it.payload))
                        }
                        
                        gatilhosAdapter.notifyDataSetChanged()
                        acoesAdapter.notifyDataSetChanged()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) { 
                    binding.btnSalvar.isEnabled = true
                    Toast.makeText(this@EditarRotinaActivity, "Erro ao carregar", Toast.LENGTH_SHORT).show() 
                }
            }
        }
    }

    private fun showAddGatilhoDialog() {
        val options = arrayOf("⏰ Horário Fixo", "🗣️ Comando de Voz/Texto", "📡 Evento IoT")
        AlertDialog.Builder(this)
            .setTitle("Escolher tipo de Gatilho")
            .setItems(options) { _, which ->
                when(which) {
                    0 -> showTimePicker()
                    1 -> showTextEntryDialog("Digite o comando (ex: 'boa noite')") { text ->
                        gatilhosList.add(CreateRotinaGatilhoRequest(1, text, null))
                        gatilhosAdapter.notifyItemInserted(gatilhosList.size - 1)
                    }
                    2 -> Toast.makeText(this, "Em breve!", Toast.LENGTH_SHORT).show()
                }
            }.show()
    }

    private fun showTimePicker() {
        val cal = Calendar.getInstance()
        TimePickerDialog(this, { _, hour, minute ->
            val time = String.format("%02d:%02d", hour, minute)
            gatilhosList.add(CreateRotinaGatilhoRequest(0, time, "1,2,3,4,5,6,7")) // Todos os dias por padrão
            gatilhosAdapter.notifyItemInserted(gatilhosList.size - 1)
        }, cal.get(Calendar.HOUR_OF_DAY), cal.get(Calendar.MINUTE), true).show()
    }

    private fun showAddAcaoDialog() {
        val options = arrayOf("🔌 Controlar Dispositivo", "💬 Executar Comando (IA)", "⏳ Aguardar (Delay)", "🔔 Notificação Push", "🔗 Abrir URL/Link", "🤖 Executar Comando (Assistente)")
        AlertDialog.Builder(this)
            .setTitle("O que a rotina deve fazer?")
            .setItems(options) { _, which ->
                when(which) {
                    0 -> showDeviceSelectorDialog()
                    1 -> showTextEntryDialog("Comando para a IA (ex: 'diga a previsão do tempo')") { text ->
                        val payload = JSONObject().put("comando", text).toString()
                        addAcao(1, payload)
                    }
                    2 -> showTextEntryDialog("Segundos para aguardar:", true) { text ->
                        val payload = JSONObject().put("seconds", text.toIntOrNull() ?: 5).toString()
                        addAcao(2, payload)
                    }
                    3 -> showTextEntryDialog("Mensagem da notificação:") { text ->
                        val payload = JSONObject().put("titulo", "Rotina StarkAid").put("mensagem", text).toString()
                        addAcao(3, payload)
                    }
                    4 -> showTextEntryDialog("URL para abrir (http...):") { text ->
                        val payload = JSONObject().put("url", text).toString()
                        addAcao(4, payload)
                    }
                    5 -> showTextEntryDialog("Comando para o App (ex: 'Que horas sao')") { text ->
                        val payload = JSONObject().put("comando", text).toString()
                        addAcao(5, payload)
                    }
                }
            }.show()
    }

    private fun addAcao(tipo: Int, payload: String) {
        acoesList.add(CreateRotinaAcaoRequest(acoesList.size, tipo, payload))
        acoesAdapter.notifyItemInserted(acoesList.size - 1)
    }

    private fun showDeviceSelectorDialog() {
        if (availableDevices.isEmpty()) {
            Toast.makeText(this, "Carregando dispositivos...", Toast.LENGTH_SHORT).show()
            loadAvailableDevices()
            return
        }

        val names = availableDevices.map { it.name }.toTypedArray()
        AlertDialog.Builder(this)
            .setTitle("Escolher Dispositivo")
            .setItems(names) { _, which ->
                val device = availableDevices[which]
                showActionSelectorForDevice(device)
            }.show()
    }

    private fun showActionSelectorForDevice(device: DeviceSelectionDto) {
        val actions = arrayOf("Ligar", "Desligar")
        AlertDialog.Builder(this)
            .setTitle("Ação para ${device.name}")
            .setItems(actions) { _, which ->
                val actionText = if (which == 0) "on" else "off"
                val payload = JSONObject().apply {
                    put("deviceId", device.dispositivoId)
                    put("tipo", device.tipo)
                    put("name", device.name)
                    put("action", actionText)
                }.toString()
                addAcao(0, payload)
            }.show()
    }

    private fun showTextEntryDialog(hint: String, isNumber: Boolean = false, onConfirm: (String) -> Unit) {
        val input = EditText(this)
        if (isNumber) input.inputType = android.text.InputType.TYPE_CLASS_NUMBER
        input.hint = hint
        
        AlertDialog.Builder(this)
            .setTitle("Configurar Ação")
            .setView(input)
            .setPositiveButton("Adicionar") { _, _ -> onConfirm(input.text.toString()) }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun salvarRotina() {
        val nome = binding.etNome.text.toString()
        if (nome.isBlank()) {
            Toast.makeText(this, "Nome é obrigatório", Toast.LENGTH_SHORT).show()
            return
        }

        binding.btnSalvar.isEnabled = false
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val api = ApiClient.getClient(this@EditarRotinaActivity).create(RotinasApi::class.java)
                val response = if (rotinaId == null) {
                    api.create(CreateRotinaRequest(nome, binding.etDescricao.text.toString(), gatilhosList, acoesList))
                } else {
                    api.update(rotinaId!!, UpdateRotinaRequest(nome, binding.etDescricao.text.toString(), true, gatilhosList, acoesList))
                }

                withContext(Dispatchers.Main) {
                    binding.btnSalvar.isEnabled = true
                    if (response.isSuccessful) {
                        Toast.makeText(this@EditarRotinaActivity, "Rotina salva com sucesso!", Toast.LENGTH_SHORT).show()
                        finish()
                    } else {
                        Toast.makeText(this@EditarRotinaActivity, "Erro ao salvar", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    binding.btnSalvar.isEnabled = true
                    Toast.makeText(this@EditarRotinaActivity, "Erro de conexão", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    // Adapter genérico para as listas de configuração
    class ConfigAdapter<T>(
        private val items: List<T>,
        private val textProvider: (T) -> String,
        private val onRemove: (Int) -> Unit
    ) : RecyclerView.Adapter<ConfigAdapter.ViewHolder>() {

        class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
            val tvTexto: TextView = view.findViewById(R.id.tvTexto)
            val btnRemove: ImageButton = view.findViewById(R.id.btnRemove)
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
            val view = LayoutInflater.from(parent.context).inflate(R.layout.item_rotina_config, parent, false)
            return ViewHolder(view)
        }

        override fun onBindViewHolder(holder: ViewHolder, position: Int) {
            holder.tvTexto.text = textProvider(items[position])
            holder.btnRemove.setOnClickListener { onRemove(holder.adapterPosition) }
        }

        override fun getItemCount() = items.size
    }
}
