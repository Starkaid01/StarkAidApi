package com.starkaid.starkaidapp.ui

import android.content.Intent
import android.media.MediaPlayer
import android.os.Bundle
import android.util.Log
import android.widget.Button
import android.widget.TextView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.DisparoApi
import com.starkaid.starkaidapp.util.SoundManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class DisparoAlertActivity : BaseActivity()  {

    private lateinit var sessionManager: SessionManager
    private var mediaPlayer: MediaPlayer? = null  // 👈 media player

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_disparo_alert)

        sessionManager = SessionManager(this)

        val textViewInfo = findViewById<TextView>(R.id.textViewDisparoInfo)
        val buttonConfirmar = findViewById<Button>(R.id.buttonConfirmar)

        val disparoId = intent.getStringExtra("disparoId") ?: return
        textViewInfo.text = "Disparo pendente:\nID: $disparoId"

        // 👈 Toca a sirene
        if (sessionManager.isSireneAtivada()) {
            mediaPlayer = MediaPlayer.create(this, R.raw.sirene)
            mediaPlayer?.isLooping = true
            mediaPlayer?.start()
        }

        buttonConfirmar.setOnClickListener {
            confirmarDisparo(disparoId)
        }
    }

    private fun confirmarDisparo(disparoId: String) {
        sessionManager.fetchAuthToken() ?: return
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)

        SoundManager.restorePreviousMode(this)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = api.confirmarDisparo(disparoId)
                if (response.isSuccessful) {
                    Log.d("DisparoAlert", "Disparo confirmado.")
                    stopSirene()
                    runOnUiThread {
                        val intent = Intent(this@DisparoAlertActivity, MainActivity::class.java)
                        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_NEW_TASK)
                        startActivity(intent)
                        finish()
                    }
                } else {
                    Log.e("DisparoAlert", "Erro ao confirmar: ${response.code()}")
                }
            } catch (e: Exception) {
                Log.e("DisparoAlert", "Erro: ${e.message}")
            }
        }
    }

    private fun stopSirene() {
        mediaPlayer?.stop()
        mediaPlayer?.release()
        mediaPlayer = null
    }

    override fun onDestroy() {
        super.onDestroy()
        stopSirene() // segurança para evitar som travado
    }
}