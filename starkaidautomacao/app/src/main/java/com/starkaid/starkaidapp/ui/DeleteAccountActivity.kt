package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Button
import android.widget.ProgressBar
import android.widget.Toast
import com.google.android.material.appbar.MaterialToolbar
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.DeleteAccountResponse
import com.starkaid.starkaidapp.services.UsersApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class DeleteAccountActivity : BaseActivity()  {

    private lateinit var sessionManager: SessionManager
    private lateinit var progressBar: ProgressBar
    private lateinit var btnDelete: Button // Declaração correta
    private lateinit var toolbar: MaterialToolbar

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_delete_account)

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

        progressBar = findViewById(R.id.progressBar)
        btnDelete = findViewById(R.id.btnDeleteAccount)

        btnDelete.setOnClickListener {
            deleteAccount()
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }
    private fun deleteAccount() {
        AlertDialog.Builder(this)
            .setTitle("Confirmação Final")
            .setMessage(
                "ATENÇÃO FINAL:\n\n" +
                        "✅ Sua conta e todos os dados associados serão PERMANENTEMENTE apagados\n\n" +
                        "⚠️ Qualquer assinatura ativa será CANCELADA automaticamente\n\n" +
                        "Deseja continuar com a exclusão?"
            )
            .setPositiveButton("Sim, Deletar") { _, _ ->
                performAccountDeletion()
            }
            .setNegativeButton("Cancelar", null)
            .show()
    }

    private fun performAccountDeletion() {
        btnDelete.isEnabled = false
        progressBar.visibility = View.VISIBLE

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@DeleteAccountActivity)
                val api = retrofit.create(UsersApi::class.java)
                val response = api.deleteAccount()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        val deleteResponse: DeleteAccountResponse? = response.body()
                        val message = deleteResponse?.message ?: "Conta deletada com sucesso"

                        // Sucesso: mostrar mensagem e redirecionar
                        Toast.makeText(
                            this@DeleteAccountActivity,
                            message,
                            Toast.LENGTH_SHORT
                        ).show()

                        // Limpar sessão e redirecionar
                        sessionManager.clearSession()
                        val intent = Intent(this@DeleteAccountActivity, LoginActivity::class.java).apply {
                            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                        }
                        startActivity(intent)
                        finishAffinity()
                    } else {
                        // Tratar erros da API
                        val errorMessage = when (response.code()) {
                            400 -> "Token inválido"
                            404 -> "Usuário não encontrado"
                            else -> "Erro ao deletar conta: ${response.errorBody()?.string()}"
                        }
                        Toast.makeText(
                            this@DeleteAccountActivity,
                            errorMessage,
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@DeleteAccountActivity,
                        "Erro de rede: ${e.localizedMessage}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
            finally {
                withContext(Dispatchers.Main) {
                    btnDelete.isEnabled = true
                    progressBar.visibility = View.GONE
                }
            }
        }
    }
}