package com.starkaid.starkaidapp.ui

import android.os.Bundle
import android.util.Log
import android.util.Patterns
import android.widget.Button
import android.widget.EditText
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.models.UserRegisterRequest
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.UsuarioApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class RegisterActivity : AppCompatActivity() {
    private lateinit var editName: EditText
    private lateinit var editEmail: EditText
    private lateinit var editPassword: EditText
    private lateinit var editConfirmPassword: EditText
    private lateinit var editEstado: EditText
    private lateinit var editCidade: EditText
    private lateinit var editBairro: EditText
    private lateinit var buttonRegister: Button

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_register)

        editName = findViewById(R.id.editName)
        editEmail = findViewById(R.id.editEmail)
        editPassword = findViewById(R.id.editPassword)
        editConfirmPassword = findViewById(R.id.editConfirmPassword)
        editEstado = findViewById(R.id.editEstado)
        editCidade = findViewById(R.id.editCidade)
        editBairro = findViewById(R.id.editBairro)
        buttonRegister = findViewById(R.id.buttonRegister)

        buttonRegister.setOnClickListener {
            val name = editName.text.toString().trim()
            val email = editEmail.text.toString().trim()
            val password = editPassword.text.toString()
            val confirmPassword = editConfirmPassword.text.toString()
            val estado = editEstado.text.toString().trim()
            val cidade = editCidade.text.toString().trim()
            val bairro = editBairro.text.toString().trim()

            // Verificar se campos obrigatórios estão preenchidos
            if (name.isEmpty() || email.isEmpty() || password.isEmpty() || confirmPassword.isEmpty()) {
                Toast.makeText(this, "Preencha todos os campos obrigatórios", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            // Validar formato de email
            if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                Toast.makeText(this, "Informe um e-mail válido", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            // Verificar se senhas coincidem
            if (password != confirmPassword) {
                Toast.makeText(this, "As senhas não conferem", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            // Tudo certo → prosseguir com cadastro
            registerUser(name, email, password, estado, cidade, bairro)
        }
    }

    private fun registerUser(name: String, email: String, password: String, estado: String, cidade: String, bairro: String) {
        Log.d("REGISTER_DEBUG", "Dados antes do envio: name=$name, email=$email, estado=$estado, cidade=$cidade, bairro=$bairro")

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@RegisterActivity)
                val api = retrofit.create(UsuarioApi::class.java)

                val request = UserRegisterRequest(
                    name = name,
                    email = email,
                    password = password,
                    origem = "app",
                    estado = if (estado.isNotEmpty()) estado else null,
                    cidade = if (cidade.isNotEmpty()) cidade else null,
                    bairro = if (bairro.isNotEmpty()) bairro else null
                )

                request.ensureFields()
                Log.d("REGISTER_DEBUG", "Request object: $request")

                val response = api.registerUser(request)
                Log.d("REGISTER_DEBUG", "Response code: ${response.code()}")

                runOnUiThread {
                    if (response.isSuccessful) {
                        Toast.makeText(this@RegisterActivity, "Cadastro realizado com sucesso!", Toast.LENGTH_LONG).show()
                        finish() // volta para LoginActivity
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("REGISTER_ERROR", "Erro no cadastro: $errorBody")
                        Toast.makeText(this@RegisterActivity, "Erro no cadastro: ${response.code()}", Toast.LENGTH_LONG).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("RegisterActivity", "Erro no cadastro", e)
                runOnUiThread {
                    Toast.makeText(this@RegisterActivity, "Erro ao conectar no servidor", Toast.LENGTH_LONG).show()
                }
            }
        }
    }
}
