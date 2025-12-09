package com.starkaid.starkaidapp.util

import android.app.NotificationManager
import android.content.Context
import android.content.Intent
import android.media.AudioManager
import android.os.Build
import android.provider.Settings

object SoundManager {

    private var previousRingerMode: Int? = null

    fun allowSound(context: Context) {
        val audioManager = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager

        if (previousRingerMode == null) {
            previousRingerMode = audioManager.ringerMode
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            val notificationManager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            if (!notificationManager.isNotificationPolicyAccessGranted) {
                val intent = Intent(Settings.ACTION_NOTIFICATION_POLICY_ACCESS_SETTINGS)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                context.startActivity(intent)
                return
            }
        }

        audioManager.ringerMode = AudioManager.RINGER_MODE_NORMAL
    }

    fun restorePreviousMode(context: Context) {
        val audioManager = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
        previousRingerMode?.let {
            audioManager.ringerMode = it
        }
        previousRingerMode = null
    }
}
