package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.widget.ArrayAdapter
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.starkaid.starkaidapp.adapters.AgendamentoAdapter
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.databinding.ActivityAgendamentosBinding
import com.starkaid.starkaidapp.databinding.DialogCriarAgendamentoEspBinding
import com.starkaid.starkaidapp.databinding.DialogCriarAgendamentoEwelinkBinding
import com.starkaid.starkaidapp.databinding.DialogCriarAgendamentoStarkswitchBinding
import com.starkaid.starkaidapp.services.AgendamentoResponse
import com.starkaid.starkaidapp.services.AgendamentosApi
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.CriarAgendamentoEspRequest
import com.starkaid.starkaidapp.services.CriarAgendamentoEwelinkRequest
import com.starkaid.starkaidapp.services.CriarAgendamentoStarkswitchRequest
import com.starkaid.starkaidapp.services.DeviceApi
import com.starkaid.starkaidapp.services.DeviceResponse
import com.starkaid.starkaidapp.services.DispositivoEspApi
import com.starkaid.starkaidapp.services.DispositivoEspResponse
import com.starkaid.starkaidapp.services.EwelinkApi
import com.starkaid.starkaidapp.services.EwelinkDeviceResponse
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.util.Calendar
import java.text.SimpleDateFormat
import java.util.Locale

class AgendamentosActivity : AppCompatActivity() {
    private lateinit var binding: ActivityAgendamentosBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var adapter: AgendamentoAdapter
    private var agendamentosList = mutableListOf<AgendamentoResponse>()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityAgendamentosBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        setupRecyclerView()
        setupButtons()
        loadAgendamentos()
    }

    override fun onResume() {
        super.onResume()
        loadAgendamentos()
    }

    private fun setupRecyclerView() {
        adapter = AgendamentoAdapter(agendamentosList) { agendamentoId ->
            excluirAgendamento(agendamentoId)
        }
        binding.agendamentosRecyclerView.layoutManager = LinearLayoutManager(this)
        binding.agendamentosRecyclerView.adapter = adapter
    }

    private fun setupButtons() {
        binding.btnCriarAgendamentoESP.setOnClickListener {
            mostrarDialogCriarAgendamentoESP()
        }
        
        binding.btnCriarAgendamentoStarkswitch.setOnClickListener {
            mostrarDialogCriarAgendamentoStarkswitch()
        }
        
        binding.btnCriarAgendamentoEwelink.setOnClickListener {
            mostrarDialogCriarAgendamentoEwelink()
        }
    }

    private fun loadAgendamentos() {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(AgendamentosApi::class.java)
                val response = api.listarAgendamentos()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        agendamentosList.clear()
                        agendamentosList.addAll(response.body()!!)
                        adapter.notifyDataSetChanged()
                        
                        if (agendamentosList.isEmpty()) {
                            binding.emptyState.visibility = View.VISIBLE
                            binding.agendamentosRecyclerView.visibility = View.GONE
                        } else {
                            binding.emptyState.visibility = View.GONE
                            binding.agendamentosRecyclerView.visibility = View.VISIBLE
                        }
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("Agendamentos", "Erro ao buscar agendamentos: ${response.code()} - $errorBody")
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Erro ao carregar agendamentos: ${response.code()}",
                            Toast.LENGTH_LONG
                        ).show()
                        binding.emptyState.visibility = View.VISIBLE
                        binding.agendamentosRecyclerView.visibility = View.GONE
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao buscar agendamentos", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao buscar agendamentos: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                    binding.emptyState.visibility = View.VISIBLE
                    binding.agendamentosRecyclerView.visibility = View.GONE
                }
            }
        }
    }

    private fun excluirAgendamento(agendamentoId: String) {
        AlertDialog.Builder(this)
            .setTitle("Excluir Agendamento")
            .setMessage("Tem certeza que deseja excluir este agendamento?")
            .setPositiveButton("Excluir") { _, _ ->
                CoroutineScope(Dispatchers.IO).launch {
                    try {
                        val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                        val api = retrofit.create(AgendamentosApi::class.java)
                        val response = api.deletarAgendamento(agendamentoId)

                        withContext(Dispatchers.Main) {
                            if (response.isSuccessful) {
                                Toast.makeText(
                                    this@AgendamentosActivity,
                                    "Agendamento excluído com sucesso",
                                    Toast.LENGTH_SHORT
                                ).show()
                                loadAgendamentos()
                            } else {
                                Toast.makeText(
                                    this@AgendamentosActivity,
                                    "Erro ao excluir agendamento: ${response.code()}",
                                    Toast.LENGTH_LONG
                                ).show()
                            }
                        }
                    } catch (e: Exception) {
                        Log.e("Agendamentos", "Erro ao excluir agendamento", e)
                        withContext(Dispatchers.Main) {
                            Toast.makeText(
                                this@AgendamentosActivity,
                                "Erro ao excluir agendamento: ${e.localizedMessage}",
                                Toast.LENGTH_LONG
                            ).show()
                        }
                    }
                }
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun mostrarDialogCriarAgendamentoESP() {
        val dialogBinding = DialogCriarAgendamentoEspBinding.inflate(LayoutInflater.from(this))
        
        // Preencher campos com data/hora atual
        val calendar = Calendar.getInstance()
        dialogBinding.etDia.setText(calendar.get(Calendar.DAY_OF_MONTH).toString())
        dialogBinding.etMes.setText((calendar.get(Calendar.MONTH) + 1).toString())
        dialogBinding.etAno.setText(calendar.get(Calendar.YEAR).toString())
        dialogBinding.etHora.setText(calendar.get(Calendar.HOUR_OF_DAY).toString())
        dialogBinding.etMinuto.setText(calendar.get(Calendar.MINUTE).toString())
        
        // Popular spinner de recorrência
        val recorrencias = arrayOf("NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno")
        val recorrenciaAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, recorrencias)
        recorrenciaAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        dialogBinding.spinnerRecorrencia.adapter = recorrenciaAdapter
        
        val dialog = AlertDialog.Builder(this)
            .setView(dialogBinding.root)
            .setTitle("Criar Agendamento ESP")
            .setPositiveButton("Criar", null)
            .setNegativeButton("Cancelar", null)
            .create()

        // Carregar dispositivos ESP
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(DispositivoEspApi::class.java)
                val response = api.listarDispositivosEsp()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val dispositivos = response.body()!!
                        val adapter = ArrayAdapter(
                            this@AgendamentosActivity,
                            android.R.layout.simple_spinner_item,
                            dispositivos.map { "${it.nome} (${it.ip})" }
                        )
                        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
                        dialogBinding.spinnerDispositivoEsp.adapter = adapter
                        
                        // Armazenar IDs para uso posterior
                        dialogBinding.spinnerDispositivoEsp.tag = dispositivos
                    } else {
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Erro ao carregar dispositivos ESP",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao carregar dispositivos ESP", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao carregar dispositivos ESP: ${e.localizedMessage}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }

        dialog.setOnShowListener {
            val positiveButton = dialog.getButton(AlertDialog.BUTTON_POSITIVE)
            positiveButton.setOnClickListener {
                val dispositivos = dialogBinding.spinnerDispositivoEsp.tag as? List<DispositivoEspResponse>
                if (dispositivos == null || dispositivos.isEmpty()) {
                    Toast.makeText(this, "Nenhum dispositivo ESP disponível", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                val selectedIndex = dialogBinding.spinnerDispositivoEsp.selectedItemPosition
                if (selectedIndex < 0 || selectedIndex >= dispositivos.size) {
                    Toast.makeText(this, "Selecione um dispositivo ESP", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                val dispositivo = dispositivos[selectedIndex]
                val dia = dialogBinding.etDia.text.toString().toIntOrNull()
                val mes = dialogBinding.etMes.text.toString().toIntOrNull()
                val ano = dialogBinding.etAno.text.toString().toIntOrNull()
                val hora = dialogBinding.etHora.text.toString().toIntOrNull()
                val minuto = dialogBinding.etMinuto.text.toString().toIntOrNull()
                val recorrencia = dialogBinding.spinnerRecorrencia.selectedItem.toString()

                if (dia == null || dia !in 1..31) {
                    Toast.makeText(this, "Dia inválido (1-31)", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (mes == null || mes !in 1..12) {
                    Toast.makeText(this, "Mês inválido (1-12)", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (ano == null || ano < Calendar.getInstance().get(Calendar.YEAR)) {
                    Toast.makeText(this, "Ano inválido", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (hora == null || hora !in 0..23) {
                    Toast.makeText(this, "Hora deve estar entre 0 e 23", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (minuto == null || minuto !in 0..59) {
                    Toast.makeText(this, "Minuto deve estar entre 0 e 59", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                // Construir data no formato YYYY-MM-DD
                val data = String.format(Locale.getDefault(), "%04d-%02d-%02d", ano, mes, dia)
                criarAgendamentoESP(dispositivo.id, data, hora, minuto, recorrencia)
                dialog.dismiss()
            }
        }

        dialog.show()
    }

    private fun mostrarDialogCriarAgendamentoStarkswitch() {
        val dialogBinding = DialogCriarAgendamentoStarkswitchBinding.inflate(LayoutInflater.from(this))
        
        // Preencher campos com data/hora atual
        val calendar = Calendar.getInstance()
        dialogBinding.etDia.setText(calendar.get(Calendar.DAY_OF_MONTH).toString())
        dialogBinding.etMes.setText((calendar.get(Calendar.MONTH) + 1).toString())
        dialogBinding.etAno.setText(calendar.get(Calendar.YEAR).toString())
        dialogBinding.etHora.setText(calendar.get(Calendar.HOUR_OF_DAY).toString())
        dialogBinding.etMinuto.setText(calendar.get(Calendar.MINUTE).toString())
        
        // Popular spinner de ação
        val acoes = arrayOf("ligar", "desligar")
        val acaoAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, acoes)
        acaoAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        dialogBinding.spinnerAcao.adapter = acaoAdapter
        
        // Popular spinner de recorrência
        val recorrencias = arrayOf("NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno")
        val recorrenciaAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, recorrencias)
        recorrenciaAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        dialogBinding.spinnerRecorrencia.adapter = recorrenciaAdapter
        
        val dialog = AlertDialog.Builder(this)
            .setView(dialogBinding.root)
            .setTitle("Criar Agendamento Starkswitch")
            .setPositiveButton("Criar", null)
            .setNegativeButton("Cancelar", null)
            .create()

        // Carregar dispositivos Starkswitch
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(DeviceApi::class.java)
                val response = api.getDevices()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val dispositivos = response.body()!!
                        val adapter = ArrayAdapter(
                            this@AgendamentosActivity,
                            android.R.layout.simple_spinner_item,
                            dispositivos.map { it.name }
                        )
                        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
                        dialogBinding.spinnerDispositivoStarkswitch.adapter = adapter
                        
                        // Armazenar IDs para uso posterior
                        dialogBinding.spinnerDispositivoStarkswitch.tag = dispositivos
                    } else {
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Erro ao carregar dispositivos Starkswitch",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao carregar dispositivos Starkswitch", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao carregar dispositivos Starkswitch: ${e.localizedMessage}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }

        dialog.setOnShowListener {
            val positiveButton = dialog.getButton(AlertDialog.BUTTON_POSITIVE)
            positiveButton.setOnClickListener {
                val dispositivos = dialogBinding.spinnerDispositivoStarkswitch.tag as? List<DeviceResponse>
                if (dispositivos == null || dispositivos.isEmpty()) {
                    Toast.makeText(this, "Nenhum dispositivo Starkswitch disponível", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                val selectedIndex = dialogBinding.spinnerDispositivoStarkswitch.selectedItemPosition
                if (selectedIndex < 0 || selectedIndex >= dispositivos.size) {
                    Toast.makeText(this, "Selecione um dispositivo Starkswitch", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                val dispositivo = dispositivos[selectedIndex]
                val acao = dialogBinding.spinnerAcao.selectedItem.toString().lowercase()
                val dia = dialogBinding.etDia.text.toString().toIntOrNull()
                val mes = dialogBinding.etMes.text.toString().toIntOrNull()
                val ano = dialogBinding.etAno.text.toString().toIntOrNull()
                val hora = dialogBinding.etHora.text.toString().toIntOrNull()
                val minuto = dialogBinding.etMinuto.text.toString().toIntOrNull()
                val recorrencia = dialogBinding.spinnerRecorrencia.selectedItem.toString()

                if (dia == null || dia !in 1..31) {
                    Toast.makeText(this, "Dia inválido (1-31)", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (mes == null || mes !in 1..12) {
                    Toast.makeText(this, "Mês inválido (1-12)", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (ano == null || ano < Calendar.getInstance().get(Calendar.YEAR)) {
                    Toast.makeText(this, "Ano inválido", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (hora == null || hora !in 0..23) {
                    Toast.makeText(this, "Hora deve estar entre 0 e 23", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (minuto == null || minuto !in 0..59) {
                    Toast.makeText(this, "Minuto deve estar entre 0 e 59", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                // Construir data no formato YYYY-MM-DD
                val data = String.format(Locale.getDefault(), "%04d-%02d-%02d", ano, mes, dia)
                criarAgendamentoStarkswitch(dispositivo.id, acao, data, hora, minuto, recorrencia)
                dialog.dismiss()
            }
        }

        dialog.show()
    }

    private fun mostrarDialogCriarAgendamentoEwelink() {
        val dialogBinding = DialogCriarAgendamentoEwelinkBinding.inflate(LayoutInflater.from(this))
        
        // Preencher campos com data/hora atual
        val calendar = Calendar.getInstance()
        dialogBinding.etDia.setText(calendar.get(Calendar.DAY_OF_MONTH).toString())
        dialogBinding.etMes.setText((calendar.get(Calendar.MONTH) + 1).toString())
        dialogBinding.etAno.setText(calendar.get(Calendar.YEAR).toString())
        dialogBinding.etHora.setText(calendar.get(Calendar.HOUR_OF_DAY).toString())
        dialogBinding.etMinuto.setText(calendar.get(Calendar.MINUTE).toString())
        
        // Popular spinner de ação
        val acoes = arrayOf("ligar", "desligar")
        val acaoAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, acoes)
        acaoAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        dialogBinding.spinnerAcao.adapter = acaoAdapter
        
        // Popular spinner de recorrência
        val recorrencias = arrayOf("NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno")
        val recorrenciaAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, recorrencias)
        recorrenciaAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        dialogBinding.spinnerRecorrencia.adapter = recorrenciaAdapter
        
        val dialog = AlertDialog.Builder(this)
            .setView(dialogBinding.root)
            .setTitle("Criar Agendamento Ewelink")
            .setPositiveButton("Criar", null)
            .setNegativeButton("Cancelar", null)
            .create()

        // Carregar dispositivos Ewelink
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(EwelinkApi::class.java)
                val response = api.listarDispositivos()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val dispositivos = response.body()!!
                        val adapter = ArrayAdapter(
                            this@AgendamentosActivity,
                            android.R.layout.simple_spinner_item,
                            dispositivos.map { it.name }
                        )
                        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
                        dialogBinding.spinnerDispositivoEwelink.adapter = adapter
                        
                        // Armazenar IDs para uso posterior
                        dialogBinding.spinnerDispositivoEwelink.tag = dispositivos
                    } else {
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Erro ao carregar dispositivos Ewelink",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao carregar dispositivos Ewelink", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao carregar dispositivos Ewelink: ${e.localizedMessage}",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }

        dialog.setOnShowListener {
            val positiveButton = dialog.getButton(AlertDialog.BUTTON_POSITIVE)
            positiveButton.setOnClickListener {
                val dispositivos = dialogBinding.spinnerDispositivoEwelink.tag as? List<EwelinkDeviceResponse>
                if (dispositivos == null || dispositivos.isEmpty()) {
                    Toast.makeText(this, "Nenhum dispositivo Ewelink disponível", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                val selectedIndex = dialogBinding.spinnerDispositivoEwelink.selectedItemPosition
                if (selectedIndex < 0 || selectedIndex >= dispositivos.size) {
                    Toast.makeText(this, "Selecione um dispositivo Ewelink", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                val dispositivo = dispositivos[selectedIndex]
                val acao = dialogBinding.spinnerAcao.selectedItem.toString().lowercase()
                val dia = dialogBinding.etDia.text.toString().toIntOrNull()
                val mes = dialogBinding.etMes.text.toString().toIntOrNull()
                val ano = dialogBinding.etAno.text.toString().toIntOrNull()
                val hora = dialogBinding.etHora.text.toString().toIntOrNull()
                val minuto = dialogBinding.etMinuto.text.toString().toIntOrNull()
                val recorrencia = dialogBinding.spinnerRecorrencia.selectedItem.toString()

                if (dia == null || dia !in 1..31) {
                    Toast.makeText(this, "Dia inválido (1-31)", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (mes == null || mes !in 1..12) {
                    Toast.makeText(this, "Mês inválido (1-12)", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (ano == null || ano < Calendar.getInstance().get(Calendar.YEAR)) {
                    Toast.makeText(this, "Ano inválido", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (hora == null || hora !in 0..23) {
                    Toast.makeText(this, "Hora deve estar entre 0 e 23", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                if (minuto == null || minuto !in 0..59) {
                    Toast.makeText(this, "Minuto deve estar entre 0 e 59", Toast.LENGTH_SHORT).show()
                    return@setOnClickListener
                }

                // Construir data no formato YYYY-MM-DD
                val data = String.format(Locale.getDefault(), "%04d-%02d-%02d", ano, mes, dia)
                criarAgendamentoEwelink(dispositivo.deviceId, acao, data, hora, minuto, recorrencia)
                dialog.dismiss()
            }
        }

        dialog.show()
    }

    private fun criarAgendamentoESP(dispositivoEspId: String, data: String, hora: Int, minuto: Int, recorrencia: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(AgendamentosApi::class.java)
                val request = CriarAgendamentoEspRequest(
                    dispositivoEspId = dispositivoEspId,
                    data = data,
                    hora = hora,
                    minuto = minuto,
                    recorrencia = recorrencia
                )
                val response = api.criarAgendamentoEsp(request)

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Agendamento ESP criado com sucesso",
                            Toast.LENGTH_SHORT
                        ).show()
                        loadAgendamentos()
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("Agendamentos", "Erro ao criar agendamento ESP: ${response.code()} - $errorBody")
                        if (response.code() == 400 && (errorBody?.contains("Limite de agendamentos") == true)) {
                            mostrarDialogLimiteAgendamentos()
                        } else {
                            Toast.makeText(
                                this@AgendamentosActivity,
                                "Erro ao criar agendamento ESP: ${response.code()}",
                                Toast.LENGTH_LONG
                            ).show()
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao criar agendamento ESP", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao criar agendamento ESP: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    private fun criarAgendamentoStarkswitch(deviceId: String, acao: String, data: String, hora: Int, minuto: Int, recorrencia: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(AgendamentosApi::class.java)
                val request = CriarAgendamentoStarkswitchRequest(
                    deviceId = deviceId,
                    acao = acao,
                    data = data,
                    hora = hora,
                    minuto = minuto,
                    recorrencia = recorrencia
                )
                val response = api.criarAgendamentoStarkswitch(request)

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Agendamento Starkswitch criado com sucesso",
                            Toast.LENGTH_SHORT
                        ).show()
                        loadAgendamentos()
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("Agendamentos", "Erro ao criar agendamento Starkswitch: ${response.code()} - $errorBody")
                        if (response.code() == 400 && (errorBody?.contains("Limite de agendamentos") == true)) {
                            mostrarDialogLimiteAgendamentos()
                        } else {
                            Toast.makeText(
                                this@AgendamentosActivity,
                                "Erro ao criar agendamento Starkswitch: ${response.code()}",
                                Toast.LENGTH_LONG
                            ).show()
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao criar agendamento Starkswitch", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao criar agendamento Starkswitch: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    private fun criarAgendamentoEwelink(ewelinkDeviceId: String, acao: String, data: String, hora: Int, minuto: Int, recorrencia: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@AgendamentosActivity)
                val api = retrofit.create(AgendamentosApi::class.java)
                val request = CriarAgendamentoEwelinkRequest(
                    ewelinkDeviceId = ewelinkDeviceId,
                    acao = acao,
                    data = data,
                    hora = hora,
                    minuto = minuto,
                    recorrencia = recorrencia
                )
                val response = api.criarAgendamentoEwelink(request)

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(
                            this@AgendamentosActivity,
                            "Agendamento Ewelink criado com sucesso",
                            Toast.LENGTH_SHORT
                        ).show()
                        loadAgendamentos()
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("Agendamentos", "Erro ao criar agendamento Ewelink: ${response.code()} - $errorBody")
                        if (response.code() == 400 && (errorBody?.contains("Limite de agendamentos") == true)) {
                            mostrarDialogLimiteAgendamentos()
                        } else {
                            Toast.makeText(
                                this@AgendamentosActivity,
                                "Erro ao criar agendamento Ewelink: ${response.code()}",
                                Toast.LENGTH_LONG
                            ).show()
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("Agendamentos", "Erro ao criar agendamento Ewelink", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@AgendamentosActivity,
                        "Erro ao criar agendamento Ewelink: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    private fun mostrarDialogLimiteAgendamentos() {
        runOnUiThread {
            AlertDialog.Builder(this)
                .setTitle("Limite atingido")
                .setMessage("Limite de agendamentos do seu plano atingido. Para adicionar mais um dispositivo/agendamento, use seus créditos StarkCoins.")
                .setPositiveButton("Usar Crédito") { _, _ ->
                    // Levar usuário para adicionar StarkCoins
                    startActivity(Intent(this, AddStarkcoinsActivity::class.java))
                }
                .setNegativeButton("Fechar", null)
                .show()
        }
    }
}
