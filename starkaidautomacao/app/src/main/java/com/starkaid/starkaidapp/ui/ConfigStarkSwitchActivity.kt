package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.floatingactionbutton.FloatingActionButton
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.AddDeviceRequest
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.DeviceApi
import com.starkaid.starkaidapp.services.DeviceResponse
import com.starkaid.starkaidapp.services.RenameRequest
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class ConfigStarkSwitchActivity : BaseActivity()  {

    private lateinit var sessionManager: SessionManager
    private lateinit var recyclerView: RecyclerView
    private lateinit var progressBar: ProgressBar
    private lateinit var emptyState: TextView
    private lateinit var fabAddDevice: FloatingActionButton
    private var deviceList = mutableListOf<DeviceResponse>()

    private lateinit var toolbar: MaterialToolbar

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_config_stark_switch)

        sessionManager = SessionManager(this)
        // Checagem de integridade de sessão
        val userId = sessionManager.fetchUserId()
        val apiKey = sessionManager.fetchApiKey()
        val authToken = sessionManager.fetchAuthToken()
        android.util.Log.d("Sessao", "userId = $userId, apiKey = $apiKey, token = $authToken")
        if (userId.isNullOrEmpty() || apiKey.isNullOrEmpty() || authToken.isNullOrEmpty()) {
            Toast.makeText(this, "Sessão inválida, faça login novamente.", Toast.LENGTH_LONG).show()
            finish()
            return
        }
        recyclerView = findViewById(R.id.recyclerViewDevices)
        progressBar = findViewById(R.id.progressBar)
        emptyState = findViewById(R.id.textEmptyState)
        fabAddDevice = findViewById(R.id.fabAddDevice)

        recyclerView.layoutManager = LinearLayoutManager(this)
        recyclerView.adapter = DeviceAdapter(deviceList, object : DeviceAdapter.OnDeviceClickListener {
            override fun onDeviceClick(device: DeviceResponse) {
                showDeviceDetails(device)
            }
        })

        fabAddDevice.setOnClickListener {
            showAddDeviceDialog()
        }

        // Configurar a Toolbar e botão de voltar
        toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)

        // Configurar o clique no botão de voltar
        toolbar.setNavigationOnClickListener {
            onBackPressed()
        }

        loadDevices()
    }

    // Adicione este método para garantir o comportamento correto do botão de voltar
    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }

    private fun loadDevices() {
        val authToken = sessionManager.fetchAuthToken() ?: return
        val apiKey = sessionManager.fetchApiKey() ?: return

        Log.d("AuthTokenStarkAid", "AuthToken: $authToken")
        Log.d("AuthTokenStarkAid", "ApiKey: $apiKey")

        progressBar.visibility = View.VISIBLE
        emptyState.visibility = View.GONE

        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DeviceApi::class.java)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.getDevices()
                if (response.isSuccessful) {
                    deviceList.clear()
                    response.body()?.let { deviceList.addAll(it) }
                    runOnUiThread {
                        recyclerView.adapter?.notifyDataSetChanged()
                        progressBar.visibility = View.GONE
                        if (deviceList.isEmpty()) {
                            emptyState.visibility = View.VISIBLE
                        }
                    }
                } else {
                    runOnUiThread {
                        progressBar.visibility = View.GONE
                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            "Erro ao carregar dispositivos: ${response.code()}",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                runOnUiThread {
                    progressBar.visibility = View.GONE
                    Toast.makeText(
                        this@ConfigStarkSwitchActivity,
                        "Erro: ${e.message}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }
    }

    private fun showDeviceDetails(device: DeviceResponse) {
        val dialog = AlertDialog.Builder(this, R.style.FullScreenDialog).create()
        val view = LayoutInflater.from(this).inflate(R.layout.dialog_device_details, null)

        // Configurar o diálogo para ocupar a tela inteira
        dialog.window?.setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
        dialog.window?.setBackgroundDrawableResource(android.R.color.transparent)

        view.findViewById<TextView>(R.id.textDeviceName).text = device.name
        view.findViewById<TextView>(R.id.textDeviceId).text = device.id
        view.findViewById<TextView>(R.id.textMqttTopic).text = device.mqttTopic

        view.findViewById<Button>(R.id.buttonEdit).setOnClickListener {
            dialog.dismiss()
            showEditDeviceDialog(device)
        }

        view.findViewById<Button>(R.id.buttonDelete).setOnClickListener {
            dialog.dismiss()
            confirmDeleteDevice(device.id)
        }

        view.findViewById<Button>(R.id.buttonClose).setOnClickListener {
            dialog.dismiss()
        }

        dialog.setView(view)
        dialog.show()
    }

    private fun showEditDeviceDialog(device: DeviceResponse) {
        val dialog = AlertDialog.Builder(this).create()
        val view = LayoutInflater.from(this).inflate(R.layout.dialog_edit_device, null)

        val editName = view.findViewById<EditText>(R.id.editDeviceName)
        editName.setText(device.name)

        val editDevice = view.findViewById<EditText>(R.id.editComandoDeviceText)
        editDevice.setText(device.comando)

        view.findViewById<Button>(R.id.buttonSave).setOnClickListener {
            val newName = editName.text.toString().trim()
            val newComandoDevice = editDevice.text.toString().trim()
            if (newName.isNotEmpty()) {
                renameDevice(device.id, newName, newComandoDevice)
                dialog.dismiss()
            } else {
                Toast.makeText(this, "Digite um nome válido", Toast.LENGTH_SHORT).show()
            }
        }

        view.findViewById<Button>(R.id.buttonCancel).setOnClickListener {
            dialog.dismiss()
        }

        dialog.setView(view)
        dialog.show()
    }

    private fun showAddDeviceDialog() {
        val dialog = AlertDialog.Builder(this).create()
        val view = LayoutInflater.from(this).inflate(R.layout.dialog_add_device, null)

        val editName = view.findViewById<EditText>(R.id.editDeviceName)
        val editComandoDevice = view.findViewById<EditText>(R.id.editComandoDeviceText)

        view.findViewById<Button>(R.id.buttonAdd).setOnClickListener {
            val name = editName.text.toString().trim()
            val comandoDevice = editComandoDevice.text.toString().trim()
            if (name.isNotEmpty()) {
                addDevice(name, comandoDevice)
                dialog.dismiss()
            } else {
                Toast.makeText(this, "Digite um nome para o dispositivo", Toast.LENGTH_SHORT).show()
            }
        }

        view.findViewById<Button>(R.id.buttonCancel).setOnClickListener {
            dialog.dismiss()
        }

        dialog.setView(view)
        dialog.show()
    }

    private fun renameDevice(deviceId: String, newName: String, newComandoDevice: String?) {
        sessionManager.fetchAuthToken() ?: return
        sessionManager.fetchApiKey() ?: return

        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DeviceApi::class.java)

        val request = RenameRequest(newName, newComandoDevice)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.renameDevice(deviceId, request)
                runOnUiThread {
                    if (response.isSuccessful) {
                        // Extraia a resposta como string simples
                        val successMessage = response.body()?.string() ?: "Dispositivo renomeado"
                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            successMessage,
                            Toast.LENGTH_SHORT
                        ).show()
                        loadDevices()
                    } else {
                        // Trate erros de forma mais detalhada
                        val errorCode = response.code()
                        val errorMessage = response.errorBody()?.string() ?: "Erro desconhecido"

                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            "Erro $errorCode: $errorMessage",
                            Toast.LENGTH_LONG
                        ).show()

                        Log.e("RenameDevice", "Erro $errorCode: $errorMessage")
                    }
                }
            } catch (e: Exception) {
                runOnUiThread {
                    Toast.makeText(
                        this@ConfigStarkSwitchActivity,
                        "Erro: ${e.message}",
                        Toast.LENGTH_SHORT
                    ).show()
                    Log.e("RenameDevice", "Erro ao renomear dispositivo", e)
                }
            }
        }
    }

    private fun addDevice(name: String, comandoDevice: String) {
        sessionManager.fetchAuthToken() ?: return
        sessionManager.fetchApiKey() ?: return
        val userId = sessionManager.fetchUserId() ?: return

        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DeviceApi::class.java)

        val request = AddDeviceRequest(name, comandoDevice, userId)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.addDevice(request)
                runOnUiThread {
                    if (response.isSuccessful) {
                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            "Dispositivo adicionado com sucesso",
                            Toast.LENGTH_SHORT
                        ).show()
                        loadDevices()
                    } else {
                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            "Erro ao adicionar dispositivo: ${response.code()}",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                runOnUiThread {
                    Toast.makeText(
                        this@ConfigStarkSwitchActivity,
                        "Erro: ${e.message}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }
    }

    private fun confirmDeleteDevice(deviceId: String) {
        AlertDialog.Builder(this)
            .setTitle("Confirmar Exclusão")
            .setMessage("Tem certeza que deseja excluir este dispositivo?")
            .setPositiveButton("Excluir") { _, _ ->
                deleteDevice(deviceId)
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun deleteDevice(deviceId: String) {
        sessionManager.fetchAuthToken() ?: return
        sessionManager.fetchApiKey() ?: return

        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DeviceApi::class.java)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.deleteDevice(deviceId)
                runOnUiThread {
                    if (response.isSuccessful) {
                        val successMessage = response.body()?.string() ?: "Dispositivo removido"

                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            successMessage,
                            Toast.LENGTH_SHORT
                        ).show()
                        loadDevices()
                    } else {
                        Toast.makeText(
                            this@ConfigStarkSwitchActivity,
                            "Erro ao excluir dispositivo: ${response.code()}",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                runOnUiThread {
                    Toast.makeText(
                        this@ConfigStarkSwitchActivity,
                        "Erro: ${e.message}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }
    }

    class DeviceAdapter(
        private val devices: List<DeviceResponse>,
        private val listener: OnDeviceClickListener
    ) : RecyclerView.Adapter<DeviceAdapter.DeviceViewHolder>() {

        interface OnDeviceClickListener {
            fun onDeviceClick(device: DeviceResponse)
        }

        inner class DeviceViewHolder(view: View) : RecyclerView.ViewHolder(view) {
            val deviceName: TextView = view.findViewById(R.id.textDeviceName)
            val mqttTopic: TextView = view.findViewById(R.id.textMqttTopic)

            init {
                view.setOnClickListener {
                    listener.onDeviceClick(devices[adapterPosition])
                }
            }
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): DeviceViewHolder {
            val view = LayoutInflater.from(parent.context)
                .inflate(R.layout.item_device, parent, false)
            return DeviceViewHolder(view)
        }

        override fun onBindViewHolder(holder: DeviceViewHolder, position: Int) {
            val device = devices[position]
            holder.deviceName.text = device.name
            holder.mqttTopic.text = device.mqttTopic
        }

        override fun getItemCount() = devices.size
    }
}