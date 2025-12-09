package com.starkaid.starkaidapp.services

import android.content.Context
import android.util.Log
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.models.HubListener
import io.reactivex.rxjava3.core.Single

class HubService(
    private val sessionManager: SessionManager,
    private val listener: HubListener? = null,
    private val context: Context
) {
    private var hubConnection: HubConnection? = null

    fun start() {

        sessionManager.fetchUserId() ?: return
        val token = sessionManager.fetchAuthToken() ?: return

        hubConnection = HubConnectionBuilder.create("https://starkaid.runasp.net/hubs/dispositivo-esp?type=app")
            .withAccessTokenProvider(Single.defer { Single.just(token) })
            .build()

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

        hubConnection?.start()?.blockingAwait()
        Log.d("SignalR", "Conexão iniciada")
    }

    fun stop() {
        hubConnection?.stop()?.blockingAwait()
        hubConnection = null
        Log.d("SignalR", "Conexão finalizada")
    }

}