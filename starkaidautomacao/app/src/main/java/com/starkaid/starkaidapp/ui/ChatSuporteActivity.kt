package com.starkaid.starkaidapp.ui

import android.os.Bundle
import android.util.Log
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import android.content.Intent
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.ApiClient
import io.reactivex.rxjava3.core.Single
import org.json.JSONObject
import kotlinx.coroutines.launch
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class ChatSuporteActivity : AppCompatActivity() {
    private lateinit var sessionManager: SessionManager
    private var hubConnection: HubConnection? = null
    private lateinit var chatMessagesContainer: LinearLayout
    private lateinit var messageInput: EditText
    private lateinit var btnSend: Button
    private lateinit var btnConnect: Button
    private lateinit var btnDisconnect: Button
    private lateinit var statusText: TextView
    private lateinit var queueStatusText: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_chat_suporte)

        sessionManager = SessionManager(this)

        // Inicializar views do layout XML
        statusText = findViewById(R.id.statusText)
        queueStatusText = findViewById(R.id.queueStatusText)
        chatMessagesContainer = findViewById(R.id.chatMessagesContainer)
        messageInput = findViewById(R.id.messageInput)
        btnSend = findViewById(R.id.btnSend)
        btnConnect = findViewById(R.id.btnConnect)
        btnDisconnect = findViewById(R.id.btnDisconnect)

        setupClickListeners()
    }

    private fun setupClickListeners() {
        btnConnect.setOnClickListener {
            connectToChat()
        }

        btnDisconnect.setOnClickListener {
            disconnectFromChat()
        }

        btnSend.setOnClickListener {
            sendMessage()
        }

        messageInput.setOnEditorActionListener { _, _, _ ->
            sendMessage()
            true
        }
    }

    private fun connectToChat() {
        val token = sessionManager.fetchAuthToken()
        if (token.isNullOrEmpty()) {
            Toast.makeText(this, "Você precisa estar logado", Toast.LENGTH_SHORT).show()
            return
        }

        val baseUrl = "https://starkaid.runasp.net"
        
        try {
            hubConnection = HubConnectionBuilder.create("$baseUrl/hubs/support-chat?origem=app")
                .withAccessTokenProvider(Single.defer { Single.just(token) })
                .build()

            // Event handlers
            hubConnection?.on("QueuePosition", { data ->
                runOnUiThread {
                    try {
                        val json = JSONObject(data.toString())
                        updateQueueStatus(json.toString())
                    } catch (e: Exception) {
                        updateQueueStatus(data.toString())
                    }
                }
            }, Any::class.java)

            hubConnection?.on("NextInQueue", { data ->
                runOnUiThread {
                    try {
                        val json = JSONObject(data.toString())
                        updateQueueStatus(json.toString())
                    } catch (e: Exception) {
                        updateQueueStatus(data.toString())
                    }
                }
            }, Any::class.java)

            hubConnection?.on("ReceiveMessage", { data ->
                runOnUiThread {
                    try {
                        // O hub envia um objeto ChatMessageDto
                        val json = JSONObject(data.toString())
                        val message = json.optString("message", "")
                        val sender = json.optString("sender", "ia")
                        addMessage(message, sender)
                    } catch (e: Exception) {
                        // Se falhar, tentar como string direta
                        addMessage(data.toString(), "ia")
                    }
                }
            }, Any::class.java)

            hubConnection?.on("Error", { error ->
                runOnUiThread {
                    Toast.makeText(this, "Erro: $error", Toast.LENGTH_SHORT).show()
                }
            }, String::class.java)

            hubConnection?.on("LimiteAtingido", {
                runOnUiThread {
                    messageInput.isEnabled = false
                    messageInput.hint = "Limite de contexto atingido. Preencha o formulário abaixo."
                    btnSend.isEnabled = false
                    mostrarFormularioLimite()
                }
            })

            hubConnection?.start()?.blockingAwait()

            statusText.text = "Conectado"
            statusText.setTextColor(0xFF00FF00.toInt())
            btnConnect.isEnabled = false
            btnDisconnect.isEnabled = true
            btnSend.isEnabled = true

            addMessage("Conectado ao chat de suporte. Aguarde sua vez na fila...", "system")
        } catch (e: Exception) {
            Log.e("ChatSuporte", "Erro ao conectar", e)
            Toast.makeText(this, "Erro ao conectar: ${e.message}", Toast.LENGTH_SHORT).show()
            statusText.text = "Erro ao conectar"
            statusText.setTextColor(0xFFFF0000.toInt())
        }
    }

    private fun disconnectFromChat() {
        try {
            hubConnection?.stop()?.blockingAwait()
            hubConnection = null

            statusText.text = "Desconectado"
            statusText.setTextColor(0xFFFFA500.toInt())
            btnConnect.isEnabled = true
            btnDisconnect.isEnabled = false
            btnSend.isEnabled = false
            queueStatusText.visibility = android.view.View.GONE

            addMessage("Desconectado do chat de suporte.", "system")
        } catch (e: Exception) {
            Log.e("ChatSuporte", "Erro ao desconectar", e)
        }
    }

    private fun sendMessage() {
        val message = messageInput.text.toString().trim()
        if (message.isEmpty()) {
            return
        }

        if (hubConnection == null || hubConnection?.connectionState?.name != "Connected") {
            Toast.makeText(this, "Você precisa estar conectado", Toast.LENGTH_SHORT).show()
            return
        }

        try {
            hubConnection?.invoke("SendMessage", message)
            messageInput.setText("")
            addMessage(message, "user")
        } catch (e: Exception) {
            Log.e("ChatSuporte", "Erro ao enviar mensagem", e)
            Toast.makeText(this, "Erro ao enviar mensagem: ${e.message}", Toast.LENGTH_SHORT).show()
        }
    }

    private fun addMessage(message: String, sender: String) {
        val timestamp = java.text.SimpleDateFormat("HH:mm:ss", java.util.Locale.getDefault())
            .format(java.util.Date())

        val messageView = TextView(this).apply {
            text = when (sender) {
                "user" -> "[$timestamp] Você: $message"
                "ia" -> "[$timestamp] Assistente: $message"
                "support" -> "[$timestamp] Suporte: $message"
                else -> "[$timestamp] $message"
            }
            setTextColor(
                when (sender) {
                    "user" -> 0xFF87CEEB.toInt()
                    "ia" -> 0xFF90EE90.toInt()
                    "support" -> 0xFFFFFF00.toInt()
                    else -> 0xFF888888.toInt()
                }
            )
            textSize = 14f
            setPadding(16, 8, 16, 8)
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                bottomMargin = 8
            }
        }

        chatMessagesContainer.addView(messageView)

        // Scroll para o final
        val scrollView = chatMessagesContainer.parent as? ScrollView
        scrollView?.post {
            scrollView.fullScroll(android.view.View.FOCUS_DOWN)
        }
    }

    private fun updateQueueStatus(data: String) {
        try {
            val json = JSONObject(data)
            val message = json.optString("message", "")
            val posicao = json.optInt("posicao", -1)

            if (message.isNotEmpty()) {
                queueStatusText.text = message
                queueStatusText.visibility = android.view.View.VISIBLE
            } else if (posicao > 0) {
                queueStatusText.text = "Aguarde, você está na fila. Posição: $posicao"
                queueStatusText.visibility = android.view.View.VISIBLE
            }
        } catch (e: Exception) {
            queueStatusText.text = "Aguardando na fila..."
            queueStatusText.visibility = android.view.View.VISIBLE
        }
    }

    private fun mostrarFormularioLimite() {
        val formularioLayout = findViewById<LinearLayout>(R.id.formulario_limite_layout)
        if (formularioLayout != null) {
            formularioLayout.visibility = android.view.View.VISIBLE
            val btnEnviar = findViewById<Button>(R.id.btnEnviarFormulario)
            btnEnviar?.setOnClickListener {
                CoroutineScope(Dispatchers.IO).launch {
                    enviarFormularioLimite()
                }
            }
        }
    }

    private suspend fun enviarFormularioLimite() {
        try {
            val mensagemInput = findViewById<EditText>(R.id.formulario_mensagem)
            val mensagem = mensagemInput?.text?.toString()?.trim() ?: ""

            if (mensagem.isEmpty()) {
                runOnUiThread {
                    Toast.makeText(this, "Por favor, descreva o problema", Toast.LENGTH_SHORT).show()
                }
                return
            }

            val retrofit = ApiClient.getClient(this)
            val suporteApi = retrofit.create(com.starkaid.starkaidapp.services.SuporteApi::class.java)
            val response = suporteApi.enviarFormularioLimite(
                com.starkaid.starkaidapp.services.FormularioLimiteRequest(mensagem)
            )

            runOnUiThread {
                if (response.isSuccessful) {
                    Toast.makeText(this, "Formulário enviado com sucesso!", Toast.LENGTH_LONG).show()
                    mensagemInput?.setText("")
                    findViewById<LinearLayout>(R.id.formulario_limite_layout)?.visibility = android.view.View.GONE
                } else {
                    Toast.makeText(this, "Erro ao enviar formulário", Toast.LENGTH_SHORT).show()
                }
            }
        } catch (e: Exception) {
            Log.e("ChatSuporte", "Erro ao enviar formulário", e)
            runOnUiThread {
                Toast.makeText(this, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
            }
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        try {
            hubConnection?.stop()?.blockingAwait()
            hubConnection = null
        } catch (e: Exception) {
            Log.e("ChatSuporte", "Erro ao desconectar no destroy", e)
        }
    }
}
