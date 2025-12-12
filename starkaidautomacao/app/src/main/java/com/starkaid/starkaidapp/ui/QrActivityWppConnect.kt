package com.starkaid.starkaidapp.ui

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager

class QrActivityWppConnect : AppCompatActivity() {

    private lateinit var txtQrLink: TextView
    private lateinit var btnCopiar: Button
    private lateinit var btnCompartilhar: Button

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_qr_wppconnect)

        txtQrLink = findViewById(R.id.txtQrLink)
        btnCopiar = findViewById(R.id.btnCopiarLink)
        btnCompartilhar = findViewById(R.id.btnCompartilharLink)

        val sessionManager = SessionManager.getInstance(this@QrActivityWppConnect)
        val userId = sessionManager.fetchUserId()

        if (userId.isNullOrEmpty()) {
            Toast.makeText(this, "Erro: ID de usuário não informado", Toast.LENGTH_SHORT).show()
            finish()
            return
        }

        val qrLink = "${ApiConfig.apiBaseUrl}/v1/wpp/$userId"
        txtQrLink.text = qrLink

        // Copiar link
        btnCopiar.setOnClickListener {
            val clipboard = getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
            val clip = ClipData.newPlainText("QR Link", qrLink)
            clipboard.setPrimaryClip(clip)
            Toast.makeText(this, "Link copiado para a área de transferência", Toast.LENGTH_SHORT).show()
        }

        // Compartilhar link
        btnCompartilhar.setOnClickListener {
            val shareIntent = Intent().apply {
                action = Intent.ACTION_SEND
                putExtra(Intent.EXTRA_TEXT, "Acesse este link para conectar seu WhatsApp à StarkAid:\n$qrLink")
                type = "text/plain"
            }
            startActivity(Intent.createChooser(shareIntent, "Compartilhar link do QR Code"))
        }
    }
}
