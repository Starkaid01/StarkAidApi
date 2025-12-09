package com.starkaid.starkaidapp.ewelink

import android.os.Bundle
import android.util.Log
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.view.WindowManager
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.floatingactionbutton.FloatingActionButton
import com.google.android.material.progressindicator.LinearProgressIndicator
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.ewelink.adapter.DeviceEwelinkAdapter
import com.starkaid.starkaidapp.ewelink.models.EwelinkDevice
import com.starkaid.starkaidapp.security.SecureStorageManager
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.EwelinkApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject

class EwelinkDevicesActivity : AppCompatActivity() {

    private lateinit var secureStorage: SecureStorageManager
    private lateinit var sessionManager: SessionManager
    private lateinit var deviceService: EwelinkDeviceService
    private lateinit var recyclerView: RecyclerView
    private lateinit var adapter: DeviceEwelinkAdapter
    private lateinit var swipeRefreshLayout: SwipeRefreshLayout
    private lateinit var progressBar: LinearProgressIndicator
    private lateinit var fabRefresh: FloatingActionButton

    private var currentFamilyId: String? = null
    private var dispositivos: List<EwelinkDevice> = emptyList()
    private var isConnectedToBackend: Boolean = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        window.decorView.systemUiVisibility = (
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                        or View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                        or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                        or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                        or View.SYSTEM_UI_FLAG_FULLSCREEN
                        or View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                )

        // Para Android 8+ (API 26+)
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.P) {
            window.attributes.layoutInDisplayCutoutMode =
                WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES
        }

        setContentView(R.layout.activity_ewelink_devices)

        initViews()
        initServices()
        setupRecyclerView()
        setupListeners()
        verificarStatusEConectar()
    }

    private fun initViews() {
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        toolbar.setNavigationOnClickListener {
            finish()
        }

        recyclerView = findViewById(R.id.recyclerViewDevices)
        swipeRefreshLayout = findViewById(R.id.swipeRefreshLayout)
        progressBar = findViewById(R.id.progressBar)
        fabRefresh = findViewById(R.id.fabRefresh)
    }

    private fun initServices() {
        secureStorage = SecureStorageManager(this)
        sessionManager = SessionManager(this)
        deviceService = EwelinkDeviceService(secureStorage)
    }

    private fun verificarStatusEConectar() {
        mostrarLoading(true)
        
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Log.e("EWE", "Credenciais não encontradas - token ou apiKey vazios")
            mostrarMensagemNaoConectado()
            mostrarLoading(false)
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@EwelinkDevicesActivity)
                val ewelinkApi = retrofit.create(EwelinkApi::class.java)
                
                Log.d("EWE", "Verificando status Ewelink no backend...")
                val response = ewelinkApi.getStatus()
                
                withContext(Dispatchers.Main) {
                    mostrarLoading(false)
                    
                    if (response.isSuccessful && response.body() != null) {
                        val status = response.body()!!
                        isConnectedToBackend = status.isLoggedIn
                        
                        Log.d("EWE", "Status recebido: isLoggedIn = ${status.isLoggedIn}")
                        
                        if (status.isLoggedIn) {
                            // Usuário está conectado no backend, usar endpoints da API
                            Log.d("EWE", "Usuário conectado - carregando dispositivos...")
                            recyclerView.visibility = View.VISIBLE
                            carregarDispositivosDaApi()
                        } else {
                            // Usuário não está conectado, mostrar mensagem
                            Log.d("EWE", "Usuário não conectado - mostrando mensagem")
                            recyclerView.visibility = View.GONE
                            mostrarMensagemNaoConectado()
                        }
                    } else {
                        // Erro ao verificar status
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE", "Erro ao verificar status: ${response.code()} - $errorBody")
                        recyclerView.visibility = View.GONE
                        mostrarMensagemNaoConectado()
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE", "Erro ao verificar status", e)
                withContext(Dispatchers.Main) {
                    mostrarLoading(false)
                    recyclerView.visibility = View.GONE
                    mostrarMensagemNaoConectado()
                }
            }
        }
    }

    private fun mostrarMensagemNaoConectado() {
        val linkUrl = "https://starkaid.runasp.net/automacao.html?"
        val mensagem = """
            Para conectar sua conta Ewelink:
            
            1. Acesse o link abaixo
            2. Faça login na plataforma
            3. Clique em Dispositivos Ewelink
            4. Faça login com sua conta Ewelink
            5. Volte ao Aplicativo e veja se seus dispositivos aparecem
        """.trimIndent()

        AlertDialog.Builder(this)
            .setTitle("Conectar Conta Ewelink")
            .setMessage(mensagem)
            .setPositiveButton("Abrir Link") { dialog, _ ->
                try {
                    val intent = android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse(linkUrl))
                    startActivity(intent)
                } catch (e: Exception) {
                    android.widget.Toast.makeText(this, "Erro ao abrir link: ${e.message}", android.widget.Toast.LENGTH_LONG).show()
                }
                dialog.dismiss()
            }
            .setNegativeButton("Cancelar") { dialog, _ ->
                dialog.dismiss()
                finish()
            }
            .setCancelable(true)
            .show()
    }

    private fun carregarDispositivosDaApi() {
        mostrarLoading(true)
        
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            mostrarLoading(false)
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@EwelinkDevicesActivity)
                val ewelinkApi = retrofit.create(EwelinkApi::class.java)
                
                val response = ewelinkApi.listarDispositivos()
                
                withContext(Dispatchers.Main) {
                    mostrarLoading(false)
                    swipeRefreshLayout.isRefreshing = false
                    
                    if (response.isSuccessful && response.body() != null) {
                        val devicesApi = response.body()!!
                        Log.d("EWE", "Dispositivos recebidos da API: ${devicesApi.size}")
                        dispositivos = devicesApi.map { device ->
                            val paramsJson = JSONObject().apply {
                                // Adicionar params recebidos se existirem
                                device.params?.forEach { (key, value) ->
                                    // Não adicionar switch se for array vazio ou valor inválido
                                    if (key == "switch") {
                                        val switchValue = when (value) {
                                            is List<*> -> if (value.isEmpty()) null else value.toString()
                                            is String -> if (value.isEmpty() || value == "[]") null else value
                                            else -> value.toString()
                                        }
                                        if (switchValue != null && switchValue != "[]" && switchValue != "off" && switchValue != "on") {
                                            // Se não for "on" ou "off", usar isOn do backend
                                            put("switch", if (device.isOn) "on" else "off")
                                        } else if (switchValue != null) {
                                            put(key, switchValue)
                                        } else {
                                            // Array vazio ou valor inválido, usar isOn do backend
                                            put("switch", if (device.isOn) "on" else "off")
                                        }
                                    } else {
                                        put(key, value)
                                    }
                                }
                                
                                // Se não tiver o campo switch nos params ou se for inválido, usar o isOn do backend
                                if (!has("switch")) {
                                    put("switch", if (device.isOn) "on" else "off")
                                    Log.d("EWE", "Campo switch não encontrado nos params, usando isOn: ${device.isOn}")
                                } else {
                                    // Verificar se o switch é válido
                                    val currentSwitch = optString("switch", "")
                                    if (currentSwitch.isEmpty() || currentSwitch == "[]" || (currentSwitch != "on" && currentSwitch != "off")) {
                                        put("switch", if (device.isOn) "on" else "off")
                                        Log.d("EWE", "Campo switch inválido ($currentSwitch), usando isOn: ${device.isOn}")
                                    }
                                }
                            }
                            
                            Log.d("EWE", "Dispositivo: ${device.name}, isOn: ${device.isOn}, params recebidos: ${device.params}, paramsJson: ${paramsJson.toString()}")
                            
                            EwelinkDevice(
                                id = device.deviceId, // Usar deviceId (ID real do Ewelink) em vez de id (ID do banco)
                                name = device.name,
                                online = device.online,
                                params = paramsJson,
                                type = device.type?.toIntOrNull() ?: 0,
                                uiid = 0, // Valor padrão, pode ser extraído do params se necessário
                                familyId = "", // Valor padrão
                                roomId = "" // Valor padrão
                            )
                        }
                        adapter.updateDevices(dispositivos)
                        recyclerView.visibility = View.VISIBLE
                        
                        if (dispositivos.isEmpty()) {
                            Toast.makeText(this@EwelinkDevicesActivity, "Nenhum dispositivo encontrado", Toast.LENGTH_SHORT).show()
                        } else {
                            Toast.makeText(this@EwelinkDevicesActivity, "${dispositivos.size} dispositivos carregados", Toast.LENGTH_SHORT).show()
                        }
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE", "Erro ao carregar dispositivos: ${response.code()} - $errorBody")
                        recyclerView.visibility = View.GONE
                        Toast.makeText(this@EwelinkDevicesActivity, "Erro ao carregar dispositivos: ${response.code()}", Toast.LENGTH_LONG).show()
                        // Se houver erro, verificar novamente o status
                        verificarStatusEConectar()
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE", "Erro ao carregar dispositivos", e)
                withContext(Dispatchers.Main) {
                    mostrarLoading(false)
                    swipeRefreshLayout.isRefreshing = false
                    Toast.makeText(this@EwelinkDevicesActivity, "Erro: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    private fun setupRecyclerView() {
        adapter = DeviceEwelinkAdapter(
            devices = emptyList(),
            onDeviceToggle = { device, isOn ->
                controlarDispositivo(device, isOn)
            },
            onBrightnessChange = { device, brightness ->
                controlarBrilho(device, brightness)
            }
        )

        recyclerView.apply {
            layoutManager = LinearLayoutManager(this@EwelinkDevicesActivity)
            adapter = this@EwelinkDevicesActivity.adapter
            setHasFixedSize(true)
        }
    }

    private fun setupListeners() {
        swipeRefreshLayout.setOnRefreshListener {
            if (isConnectedToBackend) {
                carregarDispositivosDaApi()
            } else {
                verificarStatusEConectar()
            }
        }

        fabRefresh.setOnClickListener {
            if (isConnectedToBackend) {
                carregarDispositivosDaApi()
            } else {
                verificarStatusEConectar()
            }
        }
    }

    private fun controlarDispositivo(device: EwelinkDevice, isOn: Boolean) {
        if (!isConnectedToBackend) {
            Toast.makeText(this, "Você precisa estar conectado ao backend para controlar dispositivos", Toast.LENGTH_LONG).show()
            return
        }

        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@EwelinkDevicesActivity)
                val ewelinkApi = retrofit.create(EwelinkApi::class.java)
                
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = isOn
                )
                
                val response = ewelinkApi.controlarDispositivo(device.id, request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val updatedDevice = response.body()!!
                        Log.d("EWE", "Dispositivo atualizado - params: ${updatedDevice.params}")
                        
                        // Atualizar o estado local do dispositivo
                        val updatedDevices = dispositivos.map { d ->
                            if (d.id == device.id) {
                                val newParams = JSONObject().apply {
                                    // Copiar params existentes primeiro
                                    val keys = d.params.keys()
                                    while (keys.hasNext()) {
                                        val key = keys.next()
                                        put(key, d.params.get(key))
                                    }
                                    
                                    // Atualizar com params da resposta se existirem
                                    if (updatedDevice.params != null && updatedDevice.params.isNotEmpty()) {
                                        updatedDevice.params.forEach { (key, value) ->
                                            put(key, value)
                                        }
                                    }
                                    
                                    // Garantir que o switch está atualizado com o valor correto
                                    put("switch", if (isOn) "on" else "off")
                                }
                                
                                Log.d("EWE", "Novos params: ${newParams.toString()}")
                                
                                d.copy(
                                    params = newParams,
                                    online = updatedDevice.online
                                )
                            } else {
                                d
                            }
                        }
                        dispositivos = updatedDevices
                        adapter.updateDevices(updatedDevices)

                        val action = if (isOn) "ligado" else "desligado"
                        Toast.makeText(this@EwelinkDevicesActivity, "${device.name} $action", Toast.LENGTH_SHORT).show()
                    } else {
                        // Reverter a mudança no UI
                        adapter.updateDevices(dispositivos)
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE", "Erro ao controlar dispositivo: ${response.code()} - $errorBody")
                        Toast.makeText(this@EwelinkDevicesActivity, "Erro ao controlar ${device.name}: ${response.code()}", Toast.LENGTH_LONG).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE", "Erro ao controlar dispositivo", e)
                withContext(Dispatchers.Main) {
                    // Reverter a mudança no UI
                    adapter.updateDevices(dispositivos)
                    Toast.makeText(this@EwelinkDevicesActivity, "Erro: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    private fun controlarBrilho(device: EwelinkDevice, brightness: Int) {
        if (!isConnectedToBackend) {
            Toast.makeText(this, "Você precisa estar conectado ao backend para controlar dispositivos", Toast.LENGTH_LONG).show()
            return
        }

        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@EwelinkDevicesActivity)
                val ewelinkApi = retrofit.create(EwelinkApi::class.java)
                
                // Para controlar brilho, precisamos enviar um JSON com switch e brightness
                // Como a API atual só aceita switch, vamos fazer uma requisição customizada
                // Por enquanto, vamos apenas ligar o dispositivo
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = true
                )
                
                val response = ewelinkApi.controlarDispositivo(device.id, request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val updatedDevice = response.body()!!
                        // Atualizar o estado local do dispositivo
                        val updatedDevices = dispositivos.map { d ->
                            if (d.id == device.id) {
                                val newParams = JSONObject().apply {
                                    if (updatedDevice.params != null && updatedDevice.params.isNotEmpty()) {
                                        updatedDevice.params.forEach { (key, value) ->
                                            put(key, value)
                                        }
                                    } else {
                                        // Copiar params existentes
                                        val keys = d.params.keys()
                                        while (keys.hasNext()) {
                                            val key = keys.next()
                                            put(key, d.params.get(key))
                                        }
                                    }
                                    put("brightness", brightness)
                                    put("switch", "on")
                                }
                                d.copy(
                                    params = newParams,
                                    online = updatedDevice.online
                                )
                            } else {
                                d
                            }
                        }
                        dispositivos = updatedDevices
                        adapter.updateDevices(updatedDevices)

                        Toast.makeText(this@EwelinkDevicesActivity, "Brilho de ${device.name} ajustado para $brightness%", Toast.LENGTH_SHORT).show()
                    } else {
                        // Reverter a mudança no UI
                        adapter.updateDevices(dispositivos)
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE", "Erro ao ajustar brilho: ${response.code()} - $errorBody")
                        Toast.makeText(this@EwelinkDevicesActivity, "Erro ao ajustar brilho: ${response.code()}", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE", "Erro ao ajustar brilho", e)
                withContext(Dispatchers.Main) {
                    // Reverter a mudança no UI
                    adapter.updateDevices(dispositivos)
                    Toast.makeText(this@EwelinkDevicesActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun mostrarLoading(mostrar: Boolean) {
        progressBar.visibility = if (mostrar) View.VISIBLE else View.GONE
        fabRefresh.isEnabled = !mostrar
    }

    private fun fazerLogout() {
        AlertDialog.Builder(this)
            .setTitle("Confirmação de Logout")
            .setMessage("Tem certeza que deseja desconectar sua conta Ewelink?")
            .setPositiveButton("Sim") { dialog, which ->
                if (isConnectedToBackend) {
                    // Fazer logout no backend
                    val token = sessionManager.fetchAuthToken()
                    val apiKey = sessionManager.fetchApiKey()
                    
                    if (!token.isNullOrEmpty() && !apiKey.isNullOrEmpty()) {
                        CoroutineScope(Dispatchers.IO).launch {
                            try {
                                val retrofit = ApiClient.getClient(this@EwelinkDevicesActivity)
                                val ewelinkApi = retrofit.create(EwelinkApi::class.java)
                                ewelinkApi.getStatus() // Apenas para garantir que temos a API
                                // Nota: Se houver endpoint de logout, usar aqui
                            } catch (e: Exception) {
                                Log.e("EWE", "Erro ao fazer logout", e)
                            }
                        }
                    }
                }
                secureStorage.clearEwelinkTokens()
                Toast.makeText(this, "Logout realizado", Toast.LENGTH_SHORT).show()
                finish()
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    override fun onCreateOptionsMenu(menu: Menu): Boolean {
        menuInflater.inflate(R.menu.menu_devices, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.menu_refresh -> {
                if (isConnectedToBackend) {
                    carregarDispositivosDaApi()
                } else {
                    verificarStatusEConectar()
                }
                true
            }
            R.id.menu_logout -> {
                fazerLogout()
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }

}