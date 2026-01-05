package com.starkaid.starkaidapp.services

import android.accessibilityservice.AccessibilityService
import android.accessibilityservice.AccessibilityServiceInfo
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo
import android.content.Intent
import androidx.localbroadcastmanager.content.LocalBroadcastManager
import android.util.Log

class MediaAccessibilityService : AccessibilityService() {

    companion object {
        private const val TAG = "MediaAccessibility"


    }

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        event ?: return
        val pkg = event.packageName?.toString() ?: return

        // Processa apenas eventos do Online Provider (YouTube)
        if (pkg != "com.google.android.youtube") return

        // Monitorar mais tipos de eventos
        if (event.eventType == AccessibilityEvent.TYPE_WINDOW_CONTENT_CHANGED ||
            event.eventType == AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED ||
            event.eventType == AccessibilityEvent.TYPE_VIEW_CLICKED) {
            detectOnlinePlaybackState()
        }
    }
    private var lastDetectionTime = 0L
    private fun detectOnlinePlaybackState() {
        try {
            val now = System.currentTimeMillis()
            if (now - lastDetectionTime < 1000) {
                return // Não verificar mais de uma vez por segundo
            }
            lastDetectionTime = now

            Log.d(TAG, "Detectando estado de reprodução externa")

            val rootNode = rootInActiveWindow ?: return

            // Buscar por qualquer elemento que possa indicar estado de reprodução
            val allNodes = mutableListOf<AccessibilityNodeInfo>()
            collectAllNodes(rootNode, allNodes)

            var isPlaying = false
            var isPaused = false

            // Procurar por textos ou descrições que indiquem play/pause
            for (node in allNodes) {
                if (node.isVisibleToUser) {
                    val text = node.text?.toString()?.lowercase() ?: ""
                    val contentDesc = node.contentDescription?.toString()?.lowercase() ?: ""
                    val className = node.className?.toString() ?: ""
                    val viewId = node.viewIdResourceName ?: ""

                    // Log para debug - mostre todos os elementos visíveis
                    if (text.isNotEmpty() || contentDesc.isNotEmpty()) {
                        Log.d(TAG, "Elemento: classe=$className, id=$viewId, texto='$text', desc='$contentDesc'")
                    }

                    // Verificar por indicadores de play
                    if (text.contains("play") || contentDesc.contains("play") ||
                        text.contains("reproduzir") || contentDesc.contains("reproduzir") ||
                        viewId.contains("play")) {
                        isPaused = true
                        Log.d(TAG, "Encontrado indicador de PLAY/REPRODUZIR")
                    }

                    // Verificar por indicadores de pause
                    if (text.contains("pause") || contentDesc.contains("pause") ||
                        text.contains("pausar") || contentDesc.contains("pausar") ||
                        viewId.contains("pause")) {
                        isPlaying = true
                        Log.d(TAG, "Encontrado indicador de PAUSE/PAUSAR")
                    }
                }
            }

            // Limpar os nós coletados
            allNodes.forEach { it.recycle() }

            if (isPlaying) {
                Log.d(TAG, "Media externa está pausada")
                sendMediaEvent("com.google.android.youtube", false)
            } else if (isPaused) {
                Log.d(TAG, "Media externa está tocando")
                sendMediaEvent("com.google.android.youtube", true)
            } else {
                Log.d(TAG, "Estado da media não determinado")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao detectar estado da media", e)
        }
    }

    private fun collectAllNodes(node: AccessibilityNodeInfo, nodes: MutableList<AccessibilityNodeInfo>) {
        nodes.add(node)
        for (i in 0 until node.childCount) {
            val child = node.getChild(i)
            if (child != null) {
                collectAllNodes(child, nodes)
            }
        }
    }

    private fun logVisibleElements(node: AccessibilityNodeInfo, depth: Int = 0) {
        if (depth > 3) return // Limitar profundidade para não gerar logs excessivos

        if (node.isVisibleToUser) {
            val indent = "  ".repeat(depth)
            Log.d(TAG, "$indent ${node.viewIdResourceName ?: node.className}")
        }

        for (i in 0 until node.childCount) {
            val child = node.getChild(i)
            if (child != null) {
                logVisibleElements(child, depth + 1)
            }
        }
    }

    private fun sendMediaEvent(app: String, playing: Boolean) {
        val intent = Intent("com.starkaid.MEDIA_EVENT")
        intent.putExtra("app", app)
        intent.putExtra("playing", playing)
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
    }

    override fun onServiceConnected() {
        super.onServiceConnected()
        Log.i(TAG, "Serviço de acessibilidade conectado")

        // Configurar o serviço para monitorar mais tipos de eventos
        val info = AccessibilityServiceInfo().apply {
            eventTypes = AccessibilityEvent.TYPE_WINDOW_CONTENT_CHANGED or
                    AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED or
                    AccessibilityEvent.TYPE_VIEW_CLICKED or
                    AccessibilityEvent.TYPE_VIEW_SCROLLED or
                    AccessibilityEvent.TYPE_VIEW_TEXT_CHANGED
            feedbackType = AccessibilityServiceInfo.FEEDBACK_GENERIC
            notificationTimeout = 100
            packageNames = arrayOf("com.google.android.youtube")
            flags = AccessibilityServiceInfo.DEFAULT or
                    AccessibilityServiceInfo.FLAG_INCLUDE_NOT_IMPORTANT_VIEWS or
                    AccessibilityServiceInfo.FLAG_REPORT_VIEW_IDS
        }

        this.serviceInfo = info
    }

    override fun onInterrupt() {
        Log.w(TAG, "Serviço de acessibilidade interrompido")
    }

    override fun onUnbind(intent: Intent?): Boolean {
        Log.w(TAG, "Serviço de acessibilidade desvinculado")
        return super.onUnbind(intent)
    }
}