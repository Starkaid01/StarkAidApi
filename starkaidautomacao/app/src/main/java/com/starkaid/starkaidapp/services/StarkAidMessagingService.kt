package com.starkaid.starkaidapp.services

import android.app.NotificationManager
import android.app.PendingIntent
import android.content.ContentResolver
import android.content.Intent
import android.net.Uri
import android.util.Log
import androidx.core.app.NotificationCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.StarkAidApp
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.ui.DisparoAlertActivity
import com.starkaid.starkaidapp.util.SoundManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class StarkAidMessagingService : FirebaseMessagingService() {

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        Log.d("StarkAidFCM", "Novo token: $token")

        val sessionManager = SessionManager(this)
        sessionManager.saveFcmToken(token)

        val authToken = sessionManager.fetchAuthToken()
        if (!authToken.isNullOrEmpty()) {
            val retrofit = ApiClient.getClient(this)
            val api = retrofit.create(AuthApi::class.java)

            CoroutineScope(Dispatchers.IO).launch {
                try {
                    val response = api.registrarToken(RegistrarTokenRequest(token))
                    if (response.isSuccessful) {
                        Log.d("StarkAidFCM", "Token registrado com sucesso.")
                    } else {
                        Log.e("StarkAidFCM", "Erro ao registrar token: ${response.code()}")
                    }
                } catch (e: Exception) {
                    Log.e("StarkAidFCM", "Exception ao registrar token", e)
                }
            }
        } else {
            Log.w("StarkAidFCM", "Sem authToken — salvou local mas não enviou pro backend")
        }
    }

    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        super.onMessageReceived(remoteMessage)
        Log.d("StarkAidFCM", "Mensagem recebida de: ${remoteMessage.from}")

        val sessionManager = SessionManager(this)  // 👈 precisa disso aqui
        val disparoId = remoteMessage.data["disparoId"]
        val titulo = remoteMessage.data["titulo"] ?: "StarkAid"
        val corpo = remoteMessage.data["corpo"] ?: ""
        val tipo = remoteMessage.data["tipo"] ?: "disparo"

        Log.d("StarkAidFCM", "Tipo de mensagem: $tipo")

        if (tipo == "rotina") {
            mostrarNotificacao(titulo, corpo, null)
            return
        }

        if (tipo == "lembrete") {
            // Se app visível, SignalR já tratou (fala). Se não, notifica.
            if (!StarkAidApp.isAppVisible) {
                mostrarNotificacaoLembrete(titulo, corpo, disparoId)
            }
            return
        }

        if (StarkAidApp.isAppVisible) {
            // App tá aberto — chama diretamente a activity
            val intent = Intent(this, DisparoAlertActivity::class.java).apply {
                putExtra("disparoId", disparoId)
                flags = Intent.FLAG_ACTIVITY_NEW_TASK
            }
            startActivity(intent)
        } else {
            // App em background — exibe notificação via método customizado
            mostrarNotificacao(titulo, corpo, disparoId)

            if (sessionManager.isSireneAtivada()) {
                SoundManager.allowSound(this)
            }
        }
    }


    private fun mostrarNotificacao(titulo: String, corpo: String, disparoId: String?) {
        val sessionManager = SessionManager(this)
        val notificationManager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager

        Log.d("StarkAidFCM", "Sirene ativada? ${sessionManager.isSireneAtivada()}")

        val canal = if (sessionManager.isSireneAtivada()) {
            "starkaid_channel_som_v4"
        } else {
            "starkaid_channel_silencioso"
        }

        val intent = Intent(this, DisparoAlertActivity::class.java).apply {
            putExtra("disparoId", disparoId)
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }

        val pendingIntent = PendingIntent.getActivity(
            this, 0, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val soundUri = Uri.parse(ContentResolver.SCHEME_ANDROID_RESOURCE + "://" + packageName + "/" + R.raw.sirene)

        val notification = NotificationCompat.Builder(this, canal)
            .setSmallIcon(R.drawable.ic_launcher_foreground)
            .setContentTitle(titulo)
            .setContentText(corpo)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(pendingIntent)
            .setAutoCancel(true)
            .setSound(soundUri)
            .build()

        // Usar ID fixo para substituir notificações anteriores ou um ID único
        val notificationId = 12345

        notificationManager.notify(notificationId, notification)
    }

    private fun mostrarNotificacaoLembrete(titulo: String, corpo: String, id: String?) {
        val notificationManager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager
        val canal = "starkaid_general_channel" 

        val intent = Intent(this, com.starkaid.starkaidapp.ui.MainActivity::class.java).apply {
            putExtra("lembreteId", id)
            putExtra("texto", corpo)
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }

        val pendingIntent = PendingIntent.getActivity(
            this, id?.hashCode() ?: 0, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(this, canal)
            .setSmallIcon(R.drawable.logo02) 
            .setContentTitle(titulo)
            .setContentText(corpo)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(pendingIntent)
            .setAutoCancel(true)
            .build()

        notificationManager.notify((id?.hashCode() ?: 0), notification)
    }
}
