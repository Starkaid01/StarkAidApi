package com.starkaid.starkaidapp.services

import android.app.*
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.IBinder
import android.support.v4.media.session.MediaSessionCompat
import android.support.v4.media.session.PlaybackStateCompat
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.localbroadcastmanager.content.LocalBroadcastManager
import com.google.android.exoplayer2.ExoPlayer
import com.google.android.exoplayer2.MediaItem
import com.google.android.exoplayer2.Player
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.models.MusicStation

class RadioPlayerService : Service() {

    companion object {
        private const val TAG = "RadioPlayerService"
        private const val CHANNEL_ID = "radio_player_channel"
        private const val NOTIFICATION_ID = 9001
        
        const val ACTION_PLAY = "com.starkaid.ACTION_PLAY"
        const val ACTION_PAUSE = "com.starkaid.ACTION_PAUSE"
        const val ACTION_STOP = "com.starkaid.ACTION_STOP"
        const val ACTION_NEXT = "com.starkaid.ACTION_NEXT"
        const val ACTION_DUCK = "com.starkaid.ACTION_DUCK"
        const val ACTION_UNDUCK = "com.starkaid.ACTION_UNDUCK"
        
        const val EXTRA_STATION_NAME = "station_name"
        const val EXTRA_STREAM_URL = "stream_url"
        const val ACTION_UPDATE_METADATA = "com.starkaid.ACTION_UPDATE_METADATA"
        const val EXTRA_SOURCE = "extra_source"
        
        private var isRunning = false
        fun isRunning() = isRunning

        private var isPlaying = false
        fun isPlaying() = isPlaying
    }

    private var player: ExoPlayer? = null
    private var mediaSession: MediaSessionCompat? = null
    private var currentStation: MusicStation? = null

    override fun onCreate() {
        super.onCreate()
        isRunning = true
        setupPlayer()
        setupMediaSession()
        createNotificationChannel()
    }

    private fun setupPlayer() {
        player = ExoPlayer.Builder(this).build()
        player?.addListener(object : Player.Listener {
            override fun onPlaybackStateChanged(state: Int) {
                when (state) {
                    Player.STATE_IDLE -> Log.d(TAG, "ExoPlayer: STATE_IDLE")
                    Player.STATE_BUFFERING -> Log.d(TAG, "ExoPlayer: STATE_BUFFERING")
                    Player.STATE_READY -> Log.d(TAG, "ExoPlayer: STATE_READY")
                    Player.STATE_ENDED -> Log.d(TAG, "ExoPlayer: STATE_ENDED")
                }
                updateNotification(if (currentStation?.streamUrl?.contains("googlevideo") == true) "ONLINE" else "RADIO")
            }

            override fun onIsPlayingChanged(isPlayingNow: Boolean) {
                Log.d(TAG, "ExoPlayer: isPlaying=$isPlayingNow")
                isPlaying = isPlayingNow
                val source = if (currentStation?.streamUrl?.contains("googlevideo") == true) "ONLINE" else "RADIO"
                updateNotification(source)
                updatePlaybackState()
                
                // Broadcast state to UI
                val intent = Intent("com.starkaid.MUSIC_STATE_CHANGED")
                intent.putExtra("isPlaying", isPlayingNow)
                LocalBroadcastManager.getInstance(this@RadioPlayerService).sendBroadcast(intent)
            }

            override fun onPlayerError(error: com.google.android.exoplayer2.PlaybackException) {
                Log.e(TAG, "ExoPlayer Error: ${error.message}", error)
                // Implementar lógica de retry ou notificar UI para retry
                val intent = Intent("com.starkaid.MUSIC_ERROR")
                intent.putExtra("error", error.message)
                LocalBroadcastManager.getInstance(this@RadioPlayerService).sendBroadcast(intent)
            }
        })
    }

    private fun setupMediaSession() {
        mediaSession = MediaSessionCompat(this, TAG)
        mediaSession?.setCallback(object : MediaSessionCompat.Callback() {
            override fun onPlay() { play() }
            override fun onPause() { pause() }
            override fun onStop() { stopPlayer() }
        })
        mediaSession?.isActive = true
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_PLAY -> {
                val name = intent.getStringExtra(EXTRA_STATION_NAME) ?: "Rádio"
                val url = intent.getStringExtra(EXTRA_STREAM_URL)
                LocalBroadcastManager.getInstance(this).sendBroadcast(Intent("com.starkaid.MUSIC_PLAY"))
                if (url != null) {
                    currentStation = MusicStation(name, url)
                    playStation(url)
                } else {
                    play()
                }
            }
            ACTION_PAUSE -> {
                pause()
                LocalBroadcastManager.getInstance(this).sendBroadcast(Intent("com.starkaid.MUSIC_PAUSE"))
            }
            ACTION_STOP -> {
                stopPlayer()
                LocalBroadcastManager.getInstance(this).sendBroadcast(Intent("com.starkaid.MUSIC_STOP"))
            }
            ACTION_NEXT -> {
                LocalBroadcastManager.getInstance(this).sendBroadcast(Intent("com.starkaid.MUSIC_NEXT"))
            }
            ACTION_UPDATE_METADATA -> {
                val name = intent.getStringExtra(EXTRA_STATION_NAME) ?: "Música"
                val source = intent.getStringExtra(EXTRA_SOURCE) ?: "RADIO"
                
                // Se mudamos para Online ou outra fonte externa, pausamos o player interno (Rádio)
                if (source != "RADIO") {
                    player?.pause()
                }
                
                currentStation = MusicStation(name, "")
                isPlaying = true // Assumimos que se atualizou é porque está tocando (Online)
                updateNotification(source)
            }
            ACTION_DUCK -> {
                Log.d(TAG, "🔈 Ducking player volume")
                player?.volume = 0.15f
            }
            ACTION_UNDUCK -> {
                Log.d(TAG, "🔊 Unducking player volume")
                player?.volume = 1.0f
            }
        }
        return START_STICKY
    }

    private fun playStation(url: String) {
        Log.d(TAG, "ExoPlayer: setMediaItem(uri=$url)")
        val mediaItem = MediaItem.fromUri(Uri.parse(url))
        player?.setMediaItem(mediaItem)
        player?.prepare()
        Log.d(TAG, "ExoPlayer: prepare()")
        player?.play()
        updateNotification()
    }

    private fun play() {
        player?.play()
    }

    private fun pause() {
        player?.pause()
    }

    private fun stopPlayer() {
        player?.stop()
        stopForeground(true)
        stopSelf()
    }

    private fun updateNotification(source: String = "RADIO") {
        val isPlayingLocal = player?.isPlaying == true || (source == "ONLINE" && isPlaying)
        val stationName = currentStation?.name ?: "Música StarkAid"
        val content = if (source == "ONLINE") "Tocando via Stream Online" else "Tocando rádio online"

        val playPauseAction = if (isPlayingLocal) {
            NotificationCompat.Action(
                android.R.drawable.ic_media_pause, "Pause",
                getPendingIntent(ACTION_PAUSE)
            )
        } else {
            NotificationCompat.Action(
                android.R.drawable.ic_media_play, "Play",
                getPendingIntent(ACTION_PLAY)
            )
        }

        val stopAction = NotificationCompat.Action(
            android.R.drawable.ic_menu_close_clear_cancel, "Stop",
            getPendingIntent(ACTION_STOP)
        )

        val notification = NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle(stationName)
            .setContentText("Ouvindo rádio online")
            .setSmallIcon(android.R.drawable.ic_lock_silent_mode_off)
            .addAction(playPauseAction)
            .addAction(stopAction)
            .setStyle(androidx.media.app.NotificationCompat.MediaStyle()
                .setMediaSession(mediaSession?.sessionToken)
                .setShowActionsInCompactView(0, 1))
            .setOngoing(isPlaying)
            .build()

        startForeground(NOTIFICATION_ID, notification)
    }

    private fun updatePlaybackState() {
        val state = if (player?.isPlaying == true) PlaybackStateCompat.STATE_PLAYING else PlaybackStateCompat.STATE_PAUSED
        mediaSession?.setPlaybackState(
            PlaybackStateCompat.Builder()
                .setState(state, player?.currentPosition ?: 0, 1.0f)
                .setActions(PlaybackStateCompat.ACTION_PLAY or PlaybackStateCompat.ACTION_PAUSE or PlaybackStateCompat.ACTION_STOP)
                .build()
        )
    }

    private fun getPendingIntent(action: String): PendingIntent {
        val intent = Intent(this, RadioPlayerService::class.java).apply { this.action = action }
        return PendingIntent.getService(this, 0, intent, PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID, "Reprodução de Rádio",
                NotificationManager.IMPORTANCE_LOW
            )
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }

    override fun onDestroy() {
        isRunning = false
        player?.release()
        mediaSession?.release()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null
}
