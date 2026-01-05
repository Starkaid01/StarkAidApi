package com.starkaid.starkaidapp.services

import android.accessibilityservice.AccessibilityService
import android.util.Log
import android.view.accessibility.AccessibilityEvent

class OnlineWatcherService : AccessibilityService() {
    private val TAG = "OnlineWatcher"

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        event ?: return
        Log.i(TAG, "Evento recebido: ${event.eventType} - package: ${event.packageName} - class: ${event.className}")
    }

    override fun onInterrupt() {}
}
