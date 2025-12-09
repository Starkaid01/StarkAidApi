package com.starkaid.starkaidapp.services

import android.accessibilityservice.AccessibilityService
import android.util.Log
import android.view.accessibility.AccessibilityEvent

class YouTubeAccessibilityService : AccessibilityService() {

    companion object {
        private const val TAG = "YouTubeService"
    }

    private var lastWindowState: String? = null

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (event == null) return
        val packageName = event.packageName?.toString() ?: return
        if (packageName != "com.google.android.youtube") return

        val currentWindow = event.className?.toString() ?: return

        // Detecta mudança de tela
        if (currentWindow != lastWindowState) {
            lastWindowState = currentWindow

            when (currentWindow) {
                "com.google.android.youtube.app.watchwhile.WatchWhileActivity" -> {
                    Log.i(TAG, "YouTubeAccessibilityService, midia tocando: YouTube111")
                }
                else -> {
                    Log.i(TAG, "nenhuma midia tocando")
                }
            }
        }
    }

    override fun onInterrupt() {}

}