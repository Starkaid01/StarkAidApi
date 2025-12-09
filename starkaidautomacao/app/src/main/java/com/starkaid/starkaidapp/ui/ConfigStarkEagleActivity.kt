package com.starkaid.starkaidapp.ui

import android.animation.Animator
import android.animation.AnimatorListenerAdapter
import android.animation.ObjectAnimator
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.button.MaterialButton
import com.google.android.material.bottomsheet.BottomSheetDialog
import com.google.android.material.card.MaterialCardView
import com.google.android.material.floatingactionbutton.FloatingActionButton
import com.google.android.material.switchmaterial.SwitchMaterial
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.adapters.DispositivoAdapter
import com.starkaid.starkaidapp.adapters.HistoricoDisparoAdapter
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.models.DisparoResponse
import com.starkaid.starkaidapp.models.DispositivoDisparoResponse
import com.starkaid.starkaidapp.models.DispositivoRequest
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.DisparoApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.UUID

class ConfigStarkEagleActivity : BaseActivity() {
    private lateinit var sessionManager: SessionManager
    private lateinit var recyclerView: RecyclerView
    private lateinit var adapter: DispositivoAdapter
    private var dispositivos = mutableListOf<DispositivoDisparoResponse>()

    private lateinit var checkDesativarSirene: SwitchMaterial
    private lateinit var menuDispositivos: MaterialCardView
    private lateinit var overlay: View
    private lateinit var toolbar: MaterialToolbar

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_config_starkeagle)

        sessionManager = SessionManager(this)

        // Configurar a Toolbar e botão de voltar
        toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)

        // Configurar o clique no botão de voltar
        toolbar.setNavigationOnClickListener {
            onBackPressed()
        }

        // Configurar views
        menuDispositivos = findViewById(R.id.menuDispositivos)
        overlay = findViewById(R.id.overlay)

        // Configurar RecyclerView dentro do menu
        recyclerView = findViewById(R.id.recyclerDispositivos)
        recyclerView.layoutManager = LinearLayoutManager(this)

        val btnExpandir = findViewById<MaterialButton>(R.id.buttonExpandir)
        val btnAdicionar = findViewById<FloatingActionButton>(R.id.buttonAdicionar)
        val btnHistorico = findViewById<MaterialButton>(R.id.buttonHistoricoDisparos)

        checkDesativarSirene = findViewById(R.id.checkDesativarSirene)
        checkDesativarSirene.isChecked = !sessionManager.isSireneAtivada()

        checkDesativarSirene.setOnCheckedChangeListener { _, isChecked ->
            sessionManager.setSireneAtivada(!isChecked)
            NotificationManagerCompat.from(this).cancelAll()
        }

        // Configurar o clique do botão expandir
        btnExpandir.setOnClickListener {
            mostrarMenuDispositivos()
        }

        btnAdicionar.setOnClickListener { abrirDialogAdicionar() }

        btnHistorico.setOnClickListener {
            abrirHistoricoDisparos()
        }

        // Configurar clique do botão fechar
        val btnFecharMenu = findViewById<MaterialButton>(R.id.buttonFecharMenu)
        btnFecharMenu.setOnClickListener {
            fecharMenuDispositivos()
        }

        // Configurar clique no overlay para fechar o menu
        overlay.setOnClickListener {
            fecharMenuDispositivos()
        }

        carregarDispositivos()
    }

    // Adicione este método para garantir o comportamento correto do botão de voltar
    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }

    fun fecharMenuDispositivos(view: View? = null) {
        val animator = ObjectAnimator.ofFloat(menuDispositivos, "translationX", 0f, menuDispositivos.width.toFloat())
        animator.duration = 300
        animator.addListener(object : AnimatorListenerAdapter() {
            override fun onAnimationEnd(animation: Animator) {
                menuDispositivos.visibility = View.GONE
            }
        })
        animator.start()

        overlay.animate()
            .alpha(0f)
            .setDuration(300)
            .setListener(object : AnimatorListenerAdapter() {
                override fun onAnimationEnd(animation: Animator) {
                    overlay.visibility = View.GONE
                }
            })
    }

    private fun mostrarMenuDispositivos() {
        menuDispositivos.visibility = View.VISIBLE
        menuDispositivos.translationX = menuDispositivos.width.toFloat()

        val animator = ObjectAnimator.ofFloat(menuDispositivos, "translationX", menuDispositivos.width.toFloat(), 0f)
        animator.duration = 300
        animator.start()

        overlay.alpha = 0f
        overlay.visibility = View.VISIBLE
        overlay.animate()
            .alpha(1f)
            .setDuration(300)
            .start()
    }

    private fun abrirHistoricoDisparos() {
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)
        sessionManager.fetchAuthToken() ?: return

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.listarDisparos()
                if (response.isSuccessful) {
                    val disparos = response.body()!!

                    runOnUiThread {
                        val dialog = BottomSheetDialog(this@ConfigStarkEagleActivity)
                        val view = layoutInflater.inflate(R.layout.dialog_historico_disparos, null)
                        dialog.setContentView(view)

                        val recyclerView = view.findViewById<RecyclerView>(R.id.recyclerHistorico)
                        recyclerView.layoutManager = LinearLayoutManager(this@ConfigStarkEagleActivity)

                        val adapter = HistoricoDisparoAdapter(disparos) { disparo ->
                            abrirDetalhesDisparo(disparo)
                            dialog.dismiss()
                        }
                        recyclerView.adapter = adapter

                        dialog.show()
                    }
                }
            } catch (e: Exception) {
                Log.e("HistoricoDisparos", "Erro: ${e.message}")
            }
        }
    }

    private fun abrirDetalhesDisparo(disparo: DisparoResponse) {
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)
        sessionManager.fetchAuthToken() ?: return

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val dispositivoResp = api.buscarDispositivo(disparo.dispositivoId)
                if (dispositivoResp.isSuccessful) {
                    val dispositivo = dispositivoResp.body()!!

                    runOnUiThread {
                        val mensagem = """
                            📡 Dispositivo: ${dispositivo.nome}
                            📅 Disparado em: ${disparo.disparadoEm}
                            📢 Confirmado: ${disparo.confirmado}
                            📅 Confirmação: ${disparo.confirmadoEm ?: "Pendente"}
                        """.trimIndent()

                        val dialog = AlertDialog.Builder(this@ConfigStarkEagleActivity, R.style.AlertDialogTheme)
                            .setTitle("Detalhes do Disparo")
                            .setMessage(mensagem)
                            .setPositiveButton("Excluir") { _, _ -> excluirDisparo(disparo.id) }
                            .setNegativeButton("Fechar", null)
                            .create()

                        dialog.setOnShowListener {
                            dialog.getButton(AlertDialog.BUTTON_POSITIVE)?.setTextColor(
                                ContextCompat.getColor(this@ConfigStarkEagleActivity, R.color.colorPrimary))
                            dialog.getButton(AlertDialog.BUTTON_NEGATIVE)?.setTextColor(ContextCompat.getColor(this@ConfigStarkEagleActivity, android.R.color.darker_gray))
                        }

                        dialog.show()
                    }
                }
            } catch (e: Exception) {
                Log.e("DetalhesDisparo", "Erro: ${e.message}")
            }
        }
    }

    private fun excluirDisparo(id: String) {
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)
        sessionManager.fetchAuthToken() ?: return

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.deletarDisparo(id)
                if (response.isSuccessful) {
                    runOnUiThread {
                        Toast.makeText(this@ConfigStarkEagleActivity, "Disparo removido.", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("ExcluirDisparo", "Erro: ${e.message}")
            }
        }
    }

    private fun carregarDispositivos() {
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)
        sessionManager.fetchAuthToken() ?: return

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.listarDispositivos()
                if (response.isSuccessful) {
                    dispositivos = response.body()!!.toMutableList()
                    runOnUiThread { atualizarRecycler() }
                }
            } catch (e: Exception) {
                Log.e("ConfigStark", "Erro ao carregar dispositivos: ${e.message}")
            }
        }
    }

    private fun atualizarRecycler() {
        // Mantenha a implementação ORIGINAL desta função
        adapter = DispositivoAdapter(dispositivos) { dispositivo ->
            // Fechar o menu antes de abrir os detalhes
            fecharMenuDispositivos()

            // Manter a chamada original
            abrirDialogDetalhes(dispositivo)
        }
        recyclerView.adapter = adapter

        // Opcional: Atualizar o texto do botão
        val btnExpandir = findViewById<MaterialButton>(R.id.buttonExpandir)
        btnExpandir.text = "Dispositivos (${dispositivos.size})"
    }

    private fun abrirDialogDetalhes(dispositivo: DispositivoDisparoResponse) {
        val dialog = BottomSheetDialog(this)
        val view = layoutInflater.inflate(R.layout.dialog_dispositivo_detalhe, null)
        dialog.setContentView(view)

        val instant = Instant.parse(dispositivo.dataCadastro)
        val formatter = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm")
            .withZone(ZoneId.systemDefault())

        val dataFormatada = formatter.format(instant)

        val txtJson = view.findViewById<TextView>(R.id.textViewJson)
        val textoDetalhado = """
            📅 Data Cadastro: $dataFormatada
            🆔 ID: ${dispositivo.id}
            📡 Nome: ${dispositivo.nome}
            🔗 Mqtt Topic: ${dispositivo.mqttTopic}
            📶 Status Topic: ${dispositivo.statusTopic}
            👤 User ID: ${dispositivo.userId}
        """.trimIndent()
        txtJson.text = textoDetalhado

        val btnEditar = view.findViewById<Button>(R.id.buttonEditar)
        val btnExcluir = view.findViewById<Button>(R.id.buttonExcluir)

        btnEditar.setOnClickListener {
            dialog.dismiss()
            abrirDialogEditar(dispositivo)
        }
        btnExcluir.setOnClickListener {
            dialog.dismiss()
            excluirDispositivo(dispositivo.id)
        }
        dialog.show()
    }

    private fun abrirDialogEditar(dispositivo: DispositivoDisparoResponse) {
        val builder = AlertDialog.Builder(this)
        builder.setTitle("Alterar nome")

        val input = EditText(this)
        input.setText(dispositivo.nome.substringBeforeLast("-id"))
        builder.setView(input)

        builder.setPositiveButton("Salvar") { _, _ ->
            val nomeNovo = input.text.toString().replace(" ", "-") + "-id${UUID.randomUUID()}"
            atualizarDispositivo(dispositivo.id, nomeNovo)
        }
        builder.setNegativeButton("Cancelar", null)
        builder.show()
    }

    private fun atualizarDispositivo(id: String, novoNome: String) {
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)
        sessionManager.fetchAuthToken() ?: return

        CoroutineScope(Dispatchers.IO).launch {
            try {
                api.atualizarDispositivo(id, DispositivoRequest(novoNome))
                carregarDispositivos()
            } catch (e: Exception) {
                Log.e("AtualizarDispositivo", "Erro: ${e.message}")
            }
        }
    }

    private fun excluirDispositivo(id: String) {
        AlertDialog.Builder(this)
            .setTitle("Deseja mesmo deletar?")
            .setPositiveButton("Sim") { _, _ ->
                val retrofit = ApiClient.getClient(this)
                val api = retrofit.create(DisparoApi::class.java)
                sessionManager.fetchAuthToken() ?: return@setPositiveButton

                CoroutineScope(Dispatchers.IO).launch {
                    try {
                        api.deletarDispositivo(id)
                        carregarDispositivos()
                    } catch (e: Exception) {
                        Log.e("ExcluirDispositivo", "Erro: ${e.message}")
                    }
                }
            }
            .setNegativeButton("Não", null)
            .show()
    }

    private fun abrirDialogAdicionar() {
        val builder = AlertDialog.Builder(this)
        builder.setTitle("Adicionar novo StarkEagle-Alarm")

        val input = EditText(this)
        builder.setView(input)

        builder.setPositiveButton("Salvar") { _, _ ->
            val nomeNovo = input.text.toString().replace(" ", "-") + "-id${UUID.randomUUID()}"
            adicionarDispositivo(nomeNovo)
        }
        builder.setNegativeButton("Cancelar", null)
        builder.show()
    }

    private fun adicionarDispositivo(nome: String) {
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)
        sessionManager.fetchAuthToken() ?: return

        CoroutineScope(Dispatchers.IO).launch {
            try {
                api.cadastrarDispositivo(DispositivoRequest(nome))
                carregarDispositivos()
            } catch (e: Exception) {
                Log.e("AdicionarDispositivo", "Erro: ${e.message}")
            }
        }
    }
}