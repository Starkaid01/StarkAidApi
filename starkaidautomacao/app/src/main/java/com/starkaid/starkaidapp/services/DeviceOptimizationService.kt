package com.starkaid.starkaidapp.services

import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder
import android.util.Log
import com.starkaid.starkaidapp.util.NotificationUtils
import androidx.localbroadcastmanager.content.LocalBroadcastManager

@Suppress("DEPRECATION")
class DeviceOptimizationService : Service() {

    private lateinit var blockingVoiceSynthesizer: BlockingVoiceSynthesizer

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        blockingVoiceSynthesizer = BlockingVoiceSynthesizer(this)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        val notification = NotificationUtils.createOptimizationNotification(this)

        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                startForeground(OPTIMIZATION_NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC)
            } else {
                startForeground(OPTIMIZATION_NOTIFICATION_ID, notification)
            }
        } catch (e: Exception) {
            Log.e("DeviceOptimizationService", "Erro ao iniciar foreground service", e)
            startForeground(OPTIMIZATION_NOTIFICATION_ID, notification)
        }

        // Executar otimização em uma thread separada
        Thread {
            try {
                optimizeDevice()
            } catch (e: Exception) {
                Log.e("OptimizationService", "Erro na otimização", e)
            } finally {
                stopForeground(true)
                stopSelf()
            }
        }.start()

        return START_NOT_STICKY
    }

    private fun optimizeDevice() {
        try {
            // Simular processo de otimização com fala sincronizada
            publishProgress("Fechando aplicativos em segundo plano...")
            blockingVoiceSynthesizer.speakAwait("Fechando aplicativos em segundo plano")
            Thread.sleep(1000)

            publishProgress("Limpando cache de memória...")
            blockingVoiceSynthesizer.speakAwait("Limpando cache de memória")
            Thread.sleep(1000)

            publishProgress("Otimizando uso de bateria...")
            blockingVoiceSynthesizer.speakAwait("Otimizando uso de bateria")
            Thread.sleep(1000)

            publishProgress("Verificando conexões...")
            blockingVoiceSynthesizer.speakAwait("Verificando conexões")
            Thread.sleep(1000)

            // Mensagem final
            publishProgress("Otimização concluída com sucesso!")
            blockingVoiceSynthesizer.speakAwait("Otimização concluída com sucesso")

            // Enviar broadcast local de conclusão
            val localIntent = Intent(ACTION_OPTIMIZATION_COMPLETE).apply {
                putExtra(EXTRA_OPTIMIZATION_RESULT, "Otimização concluída com sucesso!")
            }
            LocalBroadcastManager.getInstance(this).sendBroadcast(localIntent)
        } catch (e: Exception) {
            Log.e("OptimizationService", "Erro durante a otimização", e)
        }
    }

    private fun publishProgress(message: String) {
        // Atualizar notificação com progresso
        val notification = NotificationUtils.createOptimizationNotification(this, message)
        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        notificationManager.notify(OPTIMIZATION_NOTIFICATION_ID, notification)
    }

    override fun onDestroy() {
        super.onDestroy()
        blockingVoiceSynthesizer.shutdown()
    }

    companion object {
        const val ACTION_OPTIMIZATION_COMPLETE = "com.starkaid.starkaidapp.ACTION_OPTIMIZATION_COMPLETE"
        const val EXTRA_OPTIMIZATION_RESULT = "optimization_result"
        const val OPTIMIZATION_NOTIFICATION_ID = 1001
    }
}