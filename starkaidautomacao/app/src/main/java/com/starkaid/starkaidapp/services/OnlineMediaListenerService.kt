package com.starkaid.starkaidapp.services

import android.app.Service
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.media.session.MediaController
import android.media.session.MediaSessionManager
import android.media.session.PlaybackState
import android.os.Build
import android.os.IBinder
import android.util.Log

class OnlineMediaListenerService : Service() {

    companion object {
        private const val TAG = "OnlineMedia"
    }

    private lateinit var mediaSessionManager: MediaSessionManager
    private val mediaControllers = mutableListOf<MediaController>()
    private val controllerCallbacks = mutableMapOf<MediaController, MediaController.Callback>()

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            mediaSessionManager = getSystemService(Context.MEDIA_SESSION_SERVICE) as MediaSessionManager
            startListening()
        }
    }

    private fun startListening() {
        val sessionsChangedListener = object : MediaSessionManager.OnActiveSessionsChangedListener {
            override fun onActiveSessionsChanged(controllers: List<MediaController>?) {
                // Limpa callbacks antigos
                controllerCallbacks.forEach { (ctrl, cb) -> ctrl.unregisterCallback(cb) }
                controllerCallbacks.clear()
                mediaControllers.clear()

                controllers?.forEach { controller ->
                    if (controller.packageName == "com.google.android.youtube") {
                        val ctrlCallback = object : MediaController.Callback() {
                            override fun onPlaybackStateChanged(state: android.media.session.PlaybackState?) {
                                when (state?.state) {
                                    PlaybackState.STATE_PLAYING ->
                                        Log.i(TAG, "OnlineMediaListenerService: midia externa tocando no yyyy")
                                    PlaybackState.STATE_PAUSED,
                                    PlaybackState.STATE_STOPPED ->
                                        Log.i(TAG, "nenhuma midia tocando")
                                    
                                    // ... other states
                                    else -> {} 
                                }
                            }
                        }
                        controller.registerCallback(ctrlCallback)
                        mediaControllers.add(controller)
                        controllerCallbacks[controller] = ctrlCallback

                        // Verifica o estado atual imediatamente
                        controller.playbackState?.let { state ->
                            when (state.state) {
                                PlaybackState.STATE_PLAYING ->
                                    Log.i(TAG, "OnlineMediaListenerService: midia externa tocando no ttt222")
                                PlaybackState.STATE_PAUSED,
                                PlaybackState.STATE_STOPPED ->
                                    Log.i(TAG, "nenhuma midia tocando")
                                
                                // ... other states
                                else -> {}
                            }
                        }
                    }
                }
            }
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            val component = ComponentName(this, OnlineMediaListenerService::class.java)
            mediaSessionManager.addOnActiveSessionsChangedListener(sessionsChangedListener, component)

            // Inicializa imediatamente com sessões ativas
            sessionsChangedListener.onActiveSessionsChanged(mediaSessionManager.getActiveSessions(component))
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        controllerCallbacks.forEach { (ctrl, cb) -> ctrl.unregisterCallback(cb) }
        controllerCallbacks.clear()
        mediaControllers.clear()
    }
}