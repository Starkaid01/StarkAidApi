package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.os.Bundle
import android.util.Patterns
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.core.content.ContextCompat
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.textfield.TextInputLayout
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.ResetSenhaRequest
import com.starkaid.starkaidapp.services.SenhaRequest
import com.starkaid.starkaidapp.services.UsuarioApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class ConfigUsuarioActivity : BaseActivity()  {
    private lateinit var sessionManager: SessionManager
    private lateinit var usuarioApi: UsuarioApi

    private lateinit var toolbar: MaterialToolbar

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_config_usuario)

        // Configurar a Toolbar e botão de voltar
        toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)

        // Configurar o clique no botão de voltar
        toolbar.setNavigationOnClickListener {
            onBackPressed()
        }

        sessionManager = SessionManager(this)
        // Checagem de integridade de sessão
        val userId = sessionManager.fetchUserId()
        val apikey = sessionManager.fetchApiKey()
        val authToken = sessionManager.fetchAuthToken()
        android.util.Log.d("Sessao", "userId = $userId, apiKey = $apikey, token = $authToken")
        if (userId.isNullOrEmpty() || apikey.isNullOrEmpty() || authToken.isNullOrEmpty()) {
            Toast.makeText(this, "Sessão inválida, faça login novamente.", Toast.LENGTH_LONG).show()
            finish()
            return
        }

        val retrofit = ApiClient.getClient(this)
        usuarioApi = retrofit.create(UsuarioApi::class.java)

        // Referências para as novas TextViews
        val textViewNome = findViewById<TextView>(R.id.textViewNome)
        val textViewEmail = findViewById<TextView>(R.id.textViewEmail)
        val textViewCoins = findViewById<TextView>(R.id.textViewCoins)
        val textViewStatus = findViewById<TextView>(R.id.textViewStatus)

        val textViewUserId = findViewById<TextView>(R.id.textViewUserId)
        val textViewApiKey = findViewById<TextView>(R.id.textViewApiKey)

        textViewUserId.text = "ID: ${ocultarValor(userId)}   Copiar"
        textViewApiKey.text = "API Key: ${ocultarValor(apikey)}   Copiar"

        textViewUserId.setOnClickListener {
            copyToClipboard("ID do Usuário", userId)
        }

        textViewApiKey.setOnClickListener {
            copyToClipboard("API Key", apikey)
        }


        // Busca os dados do usuário
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = usuarioApi.obterUsuarioAtual(userId)
                if (response.isSuccessful && response.body() != null) {
                    val usuario = response.body()!!
                    runOnUiThread {
                        textViewNome.text = usuario.name
                        textViewEmail.text = usuario.email
                        textViewCoins.text = usuario.economy?.starkCoinBalance.toString()
                        textViewStatus.text = if (usuario.isActive) "Ativo" else "Inativo"
                        textViewStatus.setTextColor(
                            ContextCompat.getColor(this@ConfigUsuarioActivity,
                                if (usuario.isActive) R.color.green_active else R.color.red_inactive)
                        )
                    }
                } else {
                    val errorBody = response.errorBody()?.string()
                    android.util.Log.e("ConfigUsuario", "Erro ao buscar usuário: ${response.code()} - $errorBody")
                    runOnUiThread {
                        Toast.makeText(this@ConfigUsuarioActivity, "Erro ao carregar dados do usuário: ${response.code()}", Toast.LENGTH_LONG).show()
                    }
                }
            } catch (e: Exception) {
                android.util.Log.e("ConfigUsuario", "Erro ao buscar usuário", e)
                runOnUiThread {
                    Toast.makeText(this@ConfigUsuarioActivity, "Erro ao carregar dados: ${e.localizedMessage}", Toast.LENGTH_LONG).show()
                }
            }
        }

        // Botão alterar senha - CORREÇÃO: removido o estilo CustomAlertDialog
        findViewById<LinearLayout>(R.id.buttonAlterarSenha).setOnClickListener {
            val builder = AlertDialog.Builder(this) // Removido R.style.CustomAlertDialog
            val view = layoutInflater.inflate(R.layout.dialog_alterar_senha, null)
            builder.setView(view)
            val dialog = builder.create()

            // Personalize o diálogo
            dialog.window?.setBackgroundDrawableResource(R.drawable.dialog_background)

            val currentInput = view.findViewById<EditText>(R.id.editSenhaAtual)
            val newInput = view.findViewById<EditText>(R.id.editNovaSenha)

            // Adicione TextInputLayout para melhor UX
            view.findViewById<TextInputLayout>(R.id.currentPasswordLayout)
            view.findViewById<TextInputLayout>(R.id.newPasswordLayout)

            view.findViewById<Button>(R.id.buttonConfirmar).setOnClickListener {
                if (validatePasswordFields(currentInput, newInput)) {
                    val req = SenhaRequest(
                        currentPassword = currentInput.text.toString(),
                        newPassword = newInput.text.toString()
                    )

                    CoroutineScope(Dispatchers.IO).launch {
                        val response = usuarioApi.alterarSenha(req, "Bearer $authToken")
                        runOnUiThread {
                            if (response.isSuccessful) {
                                Toast.makeText(this@ConfigUsuarioActivity, "Senha alterada com sucesso!", Toast.LENGTH_SHORT).show()
                            } else {
                                Toast.makeText(this@ConfigUsuarioActivity, "Erro: ${response.errorBody()?.string()}", Toast.LENGTH_LONG).show()
                            }
                            dialog.dismiss()
                        }
                    }
                }
            }

            view.findViewById<Button>(R.id.buttonCancelar).setOnClickListener {
                dialog.dismiss()
            }

            dialog.show()
        }

        // Botão reset senha - CORREÇÃO: removido o estilo CustomAlertDialog
        findViewById<LinearLayout>(R.id.buttonResetSenha).setOnClickListener {
            val builder = AlertDialog.Builder(this) // Removido R.style.CustomAlertDialog
            val view = layoutInflater.inflate(R.layout.dialog_reset_senha, null)
            builder.setView(view)
            val dialog = builder.create()

            // Personalize o diálogo
            dialog.window?.setBackgroundDrawableResource(R.drawable.dialog_background)

            val emailInput = view.findViewById<EditText>(R.id.editEmailReset)
            view.findViewById<TextInputLayout>(R.id.emailLayout)

            view.findViewById<Button>(R.id.buttonEnviarReset).setOnClickListener {
                if (validateEmailField(emailInput)) {
                    CoroutineScope(Dispatchers.IO).launch {
                        val req = ResetSenhaRequest(emailInput.text.toString())
                        val response = usuarioApi.solicitarResetSenha(req)
                        runOnUiThread {
                            if (response.isSuccessful) {
                                Toast.makeText(this@ConfigUsuarioActivity, "E-mail de reset enviado!", Toast.LENGTH_LONG).show()
                            } else {
                                Toast.makeText(this@ConfigUsuarioActivity, "Erro: ${response.errorBody()?.string()}", Toast.LENGTH_LONG).show()
                            }
                            dialog.dismiss()
                        }
                    }
                }
            }

            view.findViewById<Button>(R.id.buttonCancelarReset).setOnClickListener {
                dialog.dismiss()
            }

            dialog.show()
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }

    private fun validatePasswordFields(current: EditText, new: EditText): Boolean {
        var valid = true

        if (current.text.toString().length < 6) {
            current.error = "Senha deve ter pelo menos 6 caracteres"
            valid = false
        }

        if (new.text.toString().length < 6) {
            new.error = "Nova senha deve ter pelo menos 6 caracteres"
            valid = false
        }

        return valid
    }

    private fun validateEmailField(email: EditText): Boolean {
        if (!Patterns.EMAIL_ADDRESS.matcher(email.text.toString()).matches()) {
            email.error = "Digite um e-mail válido"
            return false
        }
        return true
    }

    private fun copyToClipboard(label: String, text: String) {
        val clipboard = getSystemService(CLIPBOARD_SERVICE) as android.content.ClipboardManager
        val clip = android.content.ClipData.newPlainText(label, text)
        clipboard.setPrimaryClip(clip)
        Toast.makeText(this, "$label copiado para a área de transferência", Toast.LENGTH_SHORT).show()
    }

    fun ocultarValor(valor: String): String {
        return if (valor.length <= 6) "*".repeat(valor.length) // fallback seguro
        else "${valor.take(3)}*******${valor.takeLast(3)}"
    }
}
