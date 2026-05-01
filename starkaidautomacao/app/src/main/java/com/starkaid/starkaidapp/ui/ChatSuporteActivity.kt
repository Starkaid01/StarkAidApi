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
import com.starkaid.starkaidapp.base.BaseActivity
import android.content.Intent
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.ApiClient
import io.reactivex.rxjava3.core.Single
import org.json.JSONObject
import kotlinx.coroutines.launch
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers

class ChatSuporteActivity : BaseActivity() {
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
        supportActionBar?.hide()

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

        try {
            hubConnection = HubConnectionBuilder.create("${ApiConfig.webBaseUrl}/hubs/support-chat?origem=app")
                .withAccessTokenProvider(Single.defer { Single.just(token) })
                .build()

            // Event handlers
            hubConnection?.on("ChatSessionStarted", {
                runOnUiThread {
                    statusText.text = "Sessão Iniciada"
                    statusText.setTextColor(0xFF00FF00.toInt())
                    btnSend.isEnabled = true
                }
            })

            hubConnection?.on("ChatSessionEnded", {
                runOnUiThread {
                    statusText.text = "Sessão Finalizada"
                    statusText.setTextColor(0xFFFFA500.toInt())
                    btnSend.isEnabled = false
                    addMessage("Sessão finalizada.", "system")
                }
            })

            // Event handler - 1 Argumento (ChatMessage object or String)
            hubConnection?.on("ReceiveMessage", { data ->
                runOnUiThread {
                    try {
                        val strData = data.toString()
                        // Evitar mensagens duplicadas do servidor se já foram mostradas (opcional)
                        if (strData.startsWith("{")) {
                            val json = JSONObject(strData)
                            val message = json.optString("message", "")
                            val sender = json.optString("sender", "ia")
                            addMessage(message, sender)
                        } else {
                             addMessage(strData, "ia")
                        }
                    } catch (e: Exception) {
                        addMessage(data.toString(), "ia")
                    }
                }
            }, Any::class.java)

            hubConnection?.on("ChatError", { error ->
                runOnUiThread {
                    Toast.makeText(this, "Erro: $error", Toast.LENGTH_SHORT).show()
                }
            }, String::class.java)

            // New: Handle Remote Actions from Support Agent
            hubConnection?.on("ExecuteAction", { action ->
                runOnUiThread {
                    com.starkaid.starkaidapp.maintenance.MaintenanceManager.executeAction(this, action.toString(), null)
                    addMessage("[Executando Ação: $action...]", "system")
                }
            }, String::class.java)

            hubConnection?.start()?.blockingAwait()
            
            // Inicia sessão explicitamente uma única vez após conexão
            try {
                 hubConnection?.invoke("StartChatSession")
            } catch(e: Exception) { Log.e("ChatSuporte", "Erro start session", e)}

            statusText.text = "Conectado"
            statusText.setTextColor(0xFF00FF00.toInt())
            btnConnect.isEnabled = false
            btnDisconnect.isEnabled = true
            
            // Força habilitação para garantir que o usuário possa tentar enviar
            btnSend.isEnabled = true 
            messageInput.isEnabled = true
            
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
            // queueStatusText.visibility = android.view.View.GONE
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

        Log.d("ChatSuporte", "Tentando enviar mensagem: $message")

        // Validação estrita de conexão
        if (hubConnection == null) {
             Toast.makeText(this, "Erro: Conexão nula", Toast.LENGTH_SHORT).show()
             return
        }
        
        if (hubConnection?.connectionState != HubConnectionState.CONNECTED) {
            Toast.makeText(this, "Conexão instável (${hubConnection?.connectionState}). Reconectando...", Toast.LENGTH_SHORT).show()
            Log.e("ChatSuporte", "Estado inválido para envio: ${hubConnection?.connectionState}")
            return
        }

        // Executar envio em background para não bloquear UI
        CoroutineScope(Dispatchers.IO).launch {
            try {
                Log.d("ChatSuporte", "Invocando SendMessage no hub...")
                hubConnection?.invoke("SendMessage", message)
                
                Log.d("ChatSuporte", "Mensagem enviada com sucesso (invoke).")
                runOnUiThread {
                    messageInput.setText("")
                    addMessage(message, "user")
                }
            } catch (e: Exception) {
                Log.e("ChatSuporte", "EXCEÇÃO ao enviar mensagem", e)
                runOnUiThread {
                    Toast.makeText(this@ChatSuporteActivity, "Falha no envio: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    private fun addMessage(message: String, sender: String) {
        val timestamp = java.text.SimpleDateFormat("HH:mm", java.util.Locale.getDefault())
            .format(java.util.Date())

        if (sender == "system") {
             val systemView = TextView(this)
             systemView.text = message
             systemView.setTextColor(0xFF888888.toInt())
             systemView.textSize = 12f
             systemView.gravity = android.view.Gravity.CENTER
             systemView.setPadding(16, 8, 16, 8)
             chatMessagesContainer.addView(systemView)
        } else {
            val layoutId = if (sender == "user") {
                R.layout.item_chat_sent
            } else {
                R.layout.item_chat_received
            }

            val messageView = layoutInflater.inflate(layoutId, chatMessagesContainer, false)
            val messageBody = messageView.findViewById<TextView>(R.id.messageBody)
            
            messageBody.text = "$message   $timestamp"
            
            chatMessagesContainer.addView(messageView)
        }

        // Scroll para o final
        val scrollView = findViewById<ScrollView>(R.id.messageScrollView)
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
        // ID formulario_limite_layout é um ScrollView no XML, não LinearLayout
        val formularioScrollView = findViewById<ScrollView>(R.id.formulario_limite_layout)
        if (formularioScrollView != null) {
            formularioScrollView.visibility = android.view.View.VISIBLE
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
                    findViewById<ScrollView>(R.id.formulario_limite_layout)?.visibility = android.view.View.GONE
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
