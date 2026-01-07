package com.starkaid.starkaidapp.util

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.ContentResolver
import android.content.Context
import android.media.AudioAttributes
import android.os.Build
import androidx.core.net.toUri
import com.starkaid.starkaidapp.R

object NotificationHelper {

    // --Commented out by Inspection (20/08/2025 14:15):private const val CHANNEL_COM_SOM = "starkaid_channel_som_v4"
    // --Commented out by Inspection (20/08/2025 14:15):private const val CHANNEL_SEM_SOM = "starkaid_channel_silencioso"

    fun criarCanais(context: Context) {

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val notificationManager =
                context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

            val soundUri =
                (ContentResolver.SCHEME_ANDROID_RESOURCE + "://" + context.packageName + "/" + R.raw.sirene).toUri()

            val somChannel = NotificationChannel(
                "starkaid_channel_som_v4",
                "Alarme com Som",
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                setSound(soundUri, AudioAttributes.Builder()
                    .setUsage(AudioAttributes.USAGE_NOTIFICATION)
                    .build())
                enableVibration(true)
            }

            val silenciosoChannel = NotificationChannel(
                "starkaid_channel_silencioso",
                "Alarme Silencioso",
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                setSound(null, null)
                enableVibration(true)
            }

            val generalChannel = NotificationChannel(
                "starkaid_general_channel",
                "Notificações Gerais",
                NotificationManager.IMPORTANCE_DEFAULT
            ).apply {
                enableVibration(true)
            }

            notificationManager.createNotificationChannel(somChannel)
            notificationManager.createNotificationChannel(silenciosoChannel)
            notificationManager.createNotificationChannel(generalChannel)
        }

    }

}
