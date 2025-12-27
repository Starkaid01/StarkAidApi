package com.starkaid.starkaidapp.util

import android.content.Intent
import android.os.Handler
import android.os.Looper
import com.starkaid.starkaidapp.StarkAidApp
import com.starkaid.starkaidapp.ui.LoginActivity

object SessionExpiredHandler {
    var onSessionExpired: (() -> Unit)? = null
    private var isHandled = false

    // --Commented out by Inspection (20/08/2025 14:16):private var isHandling = false

    fun notifySessionExpired() {
        if (isHandled) return
        
        val current = StarkAidApp.currentActivity
        if (current is LoginActivity) {
            android.util.Log.d("SessionExpiredHandler", "Sessão expirada, mas já estamos em LoginActivity. Ignorando redirecionamento.")
            return
        }

        isHandled = true
        val context = StarkAidApp.getAppContext()
        Handler(Looper.getMainLooper()).post {
             android.util.Log.d("SessionExpiredHandler", "Redirecionando para LoginActivity devido a sessão expirada")
            val intent = Intent(context, LoginActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
            context.startActivity(intent)
        }
    }

// --Commented out by Inspection START (20/08/2025 14:16):
//    private fun showDialog(activity: Activity) {
//        if (activity.isFinishing || activity.isDestroyed) return
//
//        AlertDialog.Builder(activity)
//            .setTitle("Sessão Expirada")
//            .setMessage("Sua sessão expirou. Por favor, faça login novamente.")
//            .setPositiveButton("OK") { _, _ ->
//                val intent = Intent(activity, LoginActivity::class.java).apply {
//                    flags = Intent.FLAG_ACTIVITY_CLEAR_TASK or Intent.FLAG_ACTIVITY_NEW_TASK
//                }
//                activity.startActivity(intent)
//                activity.finish()
//            }
//            .setCancelable(false)
//            .show()
//    }
// --Commented out by Inspection STOP (20/08/2025 14:16)

    fun reset() {
        isHandled = false
    }
}