package com.starkaid.starkaidapp.ui

import android.content.DialogInterface
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import android.widget.ImageButton
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.services.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class ComodosActivity : AppCompatActivity() {

    private lateinit var rvComodos: RecyclerView
    private lateinit var btnCriarComodo: Button
    private lateinit var api: ComodosApi
    private val comodosList = mutableListOf<ComodoDto>()
    private lateinit var adapter: ComodosAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_comodos)

        api = ApiClient.getClient(this).create(ComodosApi::class.java)

        rvComodos = findViewById(R.id.rvComodos)
        btnCriarComodo = findViewById(R.id.btnCriarComodo)

        rvComodos.layoutManager = LinearLayoutManager(this)
        adapter = ComodosAdapter(
            comodosList, 
            onExpandClick = { pos -> adapter.toggleExpand(pos) },
            onDeleteClick = { dto -> deleteComodo(dto) },
            onAddDeviceClick = { dto -> showAddDeviceDialog(dto) },
            onRemoveDeviceClick = { comodoId, devId -> removeDevice(comodoId, devId) }
        )
        rvComodos.adapter = adapter

        btnCriarComodo.setOnClickListener {
            showCreateDialog()
        }

        loadComodos()
    }

    private fun loadComodos() {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val response = api.getAll()
                if (response.isSuccessful && response.body() != null) {
                    val list = response.body()!!
                    withContext(Dispatchers.Main) {
                        comodosList.clear()
                        comodosList.addAll(list)
                        adapter.notifyDataSetChanged()
                    }
                } else {
                    showToast("Erro ao carregar cômodos")
                }
            } catch (e: Exception) {
                showToast("Erro de conexão: ${e.message}")
            }
        }
    }

    private fun showCreateDialog() {
        val input = EditText(this)
        input.hint = "Nome do Cômodo"
        AlertDialog.Builder(this)
            .setTitle("Novo Cômodo")
            .setView(input)
            .setPositiveButton("Criar") { _, _ ->
                val name = input.text.toString()
                if (name.isNotEmpty()) createComodo(name)
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun createComodo(nome: String) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val res = api.create(CreateComodoRequest(nome))
                if (res.isSuccessful) {
                    loadComodos()
                } else {
                    showToast("Erro ao criar")
                }
            } catch (e: Exception) {
                showToast("Erro: ${e.message}")
            }
        }
    }

    private fun deleteComodo(dto: ComodoDto) {
        AlertDialog.Builder(this)
            .setTitle("Excluir ${dto.nome}?")
            .setPositiveButton("Sim") { _, _ ->
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val res = api.delete(dto.id)
                        if (res.isSuccessful) loadComodos() else showToast("Erro ao excluir")
                    } catch (e: Exception) {
                        showToast("Erro: ${e.message}")
                    }
                }
            }
            .setNegativeButton("Não", null)
            .show()
    }

    private fun showAddDeviceDialog(comodo: ComodoDto) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val res = api.getAvailableDevices()
                if (res.isSuccessful && res.body() != null) {
                    val devices = res.body()!!
                    withContext(Dispatchers.Main) {
                        showDeviceSelectionDialog(comodo, devices)
                    }
                } else {
                    showToast("Erro ao buscar dispositivos")
                }
            } catch (e: Exception) {
                showToast("Erro: ${e.message}")
            }
        }
    }

    private fun showDeviceSelectionDialog(comodo: ComodoDto, devices: List<DeviceSelectionDto>) {
        val names = devices.map { "${it.name} (${it.tipo})" }.toTypedArray()
        val checkedItems = BooleanArray(devices.size)
        
        AlertDialog.Builder(this)
            .setTitle("Adicionar ao ${comodo.nome}")
            .setMultiChoiceItems(names, checkedItems) { _, which, isChecked ->
                checkedItems[which] = isChecked
            }
            .setPositiveButton("Adicionar") { _, _ ->
                val selected = devices.filterIndexed { index, _ -> checkedItems[index] }
                selected.forEach { dev ->
                     val papel = if (dev.name.lowercase().contains("luz") || dev.name.lowercase().contains("lampada")) "luz" else "genérico"
                     associateDevice(comodo.id, dev, papel)
                }
                // Refresh list after added
                rvComodos.postDelayed({ loadComodos() }, 1000)
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun associateDevice(comodoId: String, dev: DeviceSelectionDto, papel: String) {
        lifecycleScope.launch(Dispatchers.IO) {
             try {
                val req = AssociateDeviceRequest(dev.dispositivoId, dev.tipo, papel)
                api.addDevice(comodoId, req)
             } catch(e: Exception) {
                 e.printStackTrace()
             }
        }
    }
    
    // Placeholder for simplified logic matching the prompt's simplicity request
    private fun removeDevice(comodoId: String, devId: String) {
        AlertDialog.Builder(this)
            .setTitle("Confirmar")
            .setMessage("Deseja remover dispositivo do comodo?")
            .setPositiveButton("Sim") { _, _ ->
                lifecycleScope.launch(Dispatchers.IO) {
                    val res = api.removeDevice(comodoId, devId)
                    if (res.isSuccessful) loadComodos() else showToast("Erro ao remover")
                }
            }
            .setNegativeButton("Não", null)
            .show()
    }

    private fun toggleDeviceState(dispositivoId: String, tipo: String) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val res = api.toggleDevice(dispositivoId, tipo)
                if (res.isSuccessful) {
                    loadComodos() // Refresh UI states
                } else {
                    showToast("Erro ao trocar estado")
                }
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }

    private fun showToast(msg: String) {
        runOnUiThread { Toast.makeText(this, msg, Toast.LENGTH_SHORT).show() }
    }
    
    inner class ComodosAdapter(
        private val list: List<ComodoDto>,
        private val onExpandClick: (Int) -> Unit,
        private val onDeleteClick: (ComodoDto) -> Unit,
        private val onAddDeviceClick: (ComodoDto) -> Unit,
        private val onRemoveDeviceClick: (String, String) -> Unit
    ) : RecyclerView.Adapter<ComodosAdapter.ViewHolder>() {

        private val expandedStates = mutableMapOf<String, Boolean>()

        fun toggleExpand(pos: Int) {
            val id = list[pos].id
            expandedStates[id] = !(expandedStates[id] ?: false)
            notifyItemChanged(pos)
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
            val v = LayoutInflater.from(parent.context).inflate(R.layout.item_comodo, parent, false)
            return ViewHolder(v)
        }

        override fun onBindViewHolder(holder: ViewHolder, position: Int) {
            val item = list[position]
            holder.tvNome.text = item.nome
            val isExpanded = expandedStates[item.id] ?: false
            
            holder.layoutDevices.visibility = if (isExpanded) View.VISIBLE else View.GONE
            holder.btnExpand.rotation = if (isExpanded) 180f else 0f
            
            holder.layoutHeader.setOnClickListener { onExpandClick(position) }
            holder.btnExpand.setOnClickListener { onExpandClick(position) }
            holder.btnDelete.setOnClickListener { onDeleteClick(item) }
            holder.btnAddDevice.setOnClickListener { onAddDeviceClick(item) }

            // Nested Adapter for Devices
            holder.rvDevices.layoutManager = LinearLayoutManager(holder.itemView.context)
            holder.rvDevices.adapter = ComodoDevicesAdapter(item.id, item.dispositivos, onRemoveDeviceClick)
        }

        override fun getItemCount() = list.size

        inner class ViewHolder(v: View) : RecyclerView.ViewHolder(v) {
            val layoutHeader: View = v.findViewById(R.id.layoutHeader)
            val tvNome: TextView = v.findViewById(R.id.tvNomeComodo)
            val btnExpand: ImageButton = v.findViewById(R.id.btnExpand)
            val btnDelete: ImageButton = v.findViewById(R.id.btnDeleteComodo)
            val layoutDevices: LinearLayout = v.findViewById(R.id.layoutDevices)
            val btnAddDevice: Button = v.findViewById(R.id.btnAddDevice)
            val rvDevices: RecyclerView = v.findViewById(R.id.rvComodoDevices)
        }
    }
    
    inner class ComodoDevicesAdapter(
        private val comodoId: String,
        private val list: List<ComodoDispositivoDto>,
        private val onRemove: (String, String) -> Unit
    ) : RecyclerView.Adapter<ComodoDevicesAdapter.DevHolder>() {
        
        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): DevHolder {
             val v = LayoutInflater.from(parent.context).inflate(R.layout.item_comodo_device, parent, false)
             return DevHolder(v)
        }
        
        override fun onBindViewHolder(holder: DevHolder, position: Int) {
            val d = list[position]
            holder.tvName.text = d.nomeDispositivo
            holder.tvRole.text = "[${d.papel ?: ""}]"
            
            // Visual feedback for On/Off
            val color = if (d.isOn) 0xFF4CAF50.toInt() else 0xFFF44336.toInt() // Green vs Red
            holder.viewStatus.setBackgroundColor(color)
            
            holder.btnRemove.setOnClickListener { onRemove(comodoId, d.dispositivoId) }
            
            holder.itemView.setOnClickListener {
                toggleDeviceState(d.dispositivoId, d.tipo)
            }
        }
        
        override fun getItemCount() = list.size

        inner class DevHolder(v: View) : RecyclerView.ViewHolder(v) {
            val tvName: TextView = v.findViewById(R.id.tvDeviceName)
            val tvRole: TextView = v.findViewById(R.id.tvRole)
            val btnRemove: ImageButton = v.findViewById(R.id.btnRemoveDevice)
            val viewStatus: View = v.findViewById(R.id.viewStatusIndicator)
        }
    }
}
