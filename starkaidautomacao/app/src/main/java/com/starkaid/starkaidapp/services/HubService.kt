package com.starkaid.starkaidapp.services

import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.models.HubListener
import io.reactivex.rxjava3.core.Single
import com.google.gson.JsonElement

data class HubClientInfo(
    val tipo: String? = null,
    val identificador: String? = null
)

class HubService(
    private val sessionManager: SessionManager,
    private val listener: HubListener? = null,
    private val context: Context
) {
    private var hubConnection: HubConnection? = null
    private var deviceHubConnection: HubConnection? = null

    fun start() {

        sessionManager.fetchUserId() ?: return
        val token = sessionManager.fetchAuthToken() ?: return

        hubConnection = HubConnectionBuilder.create("${ApiConfig.webBaseUrl}/hubs/dispositivo-esp?type=app")
            .withAccessTokenProvider(Single.defer { Single.just(token) })
            .build()

        // Eventos de conexão / identificação (backend envia objeto { tipo, identificador })
        hubConnection?.on("Connected", { info: Any ->
            Log.d("SignalR", "Evento Connected payload: $info")
        }, Any::class.java)

        hubConnection?.on("Identificado", { info: Any ->
            Log.d("SignalR", "Evento Identificado payload: $info")
        }, Any::class.java)

        // Comandos recebidos
        hubConnection?.on("ReceiveCommand", { deviceId: String, command: String ->
            Log.d("SignalR", "Comando recebido: $command para $deviceId")

            // Enviar para Activity via listener
            listener?.onDeviceCommandReceived(deviceId, command)

        }, String::class.java, String::class.java)

        // Status recebido
        hubConnection?.on("ReceiveStatus", { deviceId: String, status: String ->
            Log.d("SignalR", "Status recebido: $status do device $deviceId")

            val statusResponse = status
            // Enviar para Activity via listener
            listener?.onDeviceStatusUpdated(deviceId, statusResponse)
            Log.d("SignalR", "Status enviado para Activity: $statusResponse")

        }, String::class.java, String::class.java)

        // Comando de suporte recebido
        hubConnection?.on("SuporteComando", { comando: String ->
            Log.d("SignalR", "Comando de suporte recebido: $comando")
            listener?.onSuporteComandoReceived(comando)
        }, String::class.java)

        // Device Hub Connection (New)
        deviceHubConnection = HubConnectionBuilder.create("${ApiConfig.webBaseUrl}/hubs/device")
            .withAccessTokenProvider(Single.defer { Single.just(token) })
            .build()

        deviceHubConnection?.on("OpenUrl", { url: String ->
            Log.d("SignalR", "Solicitação de abertura de URL: $url")
            listener?.onOpenUrl(url)
        }, String::class.java)

        deviceHubConnection?.on("ReceiveNotification", { titulo: String, mensagem: String ->
            Log.d("SignalR", "Notificação recebida: $titulo - $mensagem")
            listener?.onNotificationReceived(titulo, mensagem)
        }, String::class.java, String::class.java)

        deviceHubConnection?.on("ReceiveAssistantCommand", { comando: String ->
            Log.d("SignalR", "Comando de assistente recebido: $comando")
            listener?.onAssistantCommandReceived(comando)
        }, String::class.java)

        deviceHubConnection?.on("SpeakLembrete", { texto: String, id: String ->
            Log.d("SignalR", "Lembrete para falar recebido: $texto")
            listener?.onLembreteReceived(texto, id)
        }, String::class.java, String::class.java)

        deviceHubConnection?.on("Connected", { message: String ->
            Log.d("SignalR", "DeviceHub Connected: $message")
        }, String::class.java)

        hubConnection?.start()?.blockingAwait()
        deviceHubConnection?.start()?.blockingAwait()
        
        // Adicionar tratamento de reconexão manual com o método correto 'onClosed'
        hubConnection?.onClosed { exception: Exception? ->
            Log.w("SignalR", "hubConnection fechado. Tentando reconectar em 5s...", exception)
            Handler(Looper.getMainLooper()).postDelayed({
                try {
                    hubConnection?.start()?.blockingAwait()
                    Log.i("SignalR", "hubConnection reconectado!")
                } catch (e: Exception) {
                    Log.e("SignalR", "Erro ao reconectar hubConnection", e)
                }
            }, 5000)
        }

        deviceHubConnection?.onClosed { exception: Exception? ->
            Log.w("SignalR", "deviceHubConnection fechado. Tentando reconectar em 5s...", exception)
            Handler(Looper.getMainLooper()).postDelayed({
                try {
                    deviceHubConnection?.start()?.blockingAwait()
                    Log.i("SignalR", "deviceHubConnection reconectado!")
                } catch (e: Exception) {
                    Log.e("SignalR", "Erro ao reconectar deviceHubConnection", e)
                }
            }, 5000)
        }

        Log.d("SignalR", "Conexões SignalR iniciadas")
    }

    fun stop() {
        hubConnection?.stop()?.blockingAwait()
        deviceHubConnection?.stop()?.blockingAwait()
        hubConnection = null
        deviceHubConnection = null
        Log.d("SignalR", "Conexões SignalR finalizadas")
    }

}