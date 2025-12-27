package com.starkaid.starkaidapp.ui

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import android.text.Spannable
import android.text.SpannableString
import android.text.style.ForegroundColorSpan
import android.util.Log
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.lifecycle.lifecycleScope
import com.google.firebase.messaging.FirebaseMessaging
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.services.*
import com.starkaid.starkaidapp.util.SessionExpiredHandler
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.json.JSONObject

class LoginActivity : AppCompatActivity() {
    private lateinit var authService: AuthService
    private lateinit var sessionManager: SessionManager

    private val permissionRequests = mutableListOf<PermissionRequest>()
    private var currentPermissionIndex = 0

    private lateinit var requestPermissionLauncher: ActivityResultLauncher<Array<String>>
    private lateinit var requestOverlayPermissionLauncher: ActivityResultLauncher<Intent>

    private data class PermissionRequest(
        val permission: String? = null,
        val intent: Intent? = null,
        val isOverlay: Boolean = false
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        installSplashScreen()
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_login)

        sessionManager = SessionManager(this)
        authService = AuthService(this)

        // Inicializar launchers de permissão
        setupPermissionLaunchers()

        // Construir lista de permissões a serem solicitadas
        buildPermissionRequests()

        // Solicitar permissões sequencialmente
        if (permissionRequests.isNotEmpty()) {
            requestNextPermission()
        }

        // Primeiro: verificar token válido
        checkAuthToken()

        // Configuração de campos de UI
        val editEmail = findViewById<EditText>(R.id.editEmail)
        val editPassword = findViewById<EditText>(R.id.editPassword)
        val buttonLogin = findViewById<Button>(R.id.buttonLogin)

        buttonLogin.setOnClickListener {
            if (!checkAllPermissionsGranted()) {
                Toast.makeText(this, "Por favor, conceda todas as permissões", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            val email = editEmail.text.toString()
            val password = editPassword.text.toString()
            Log.d("LoginActivity", "Login button clicked with email=$email")

            lifecycleScope.launch(Dispatchers.IO) {

                try {
                    val response = authService.login(email, password)
                    Log.d("LoginActivity", "Login response: $response")
                    runOnUiThread {
                        if (response != null) {
                            handleSuccessfulLogin(response)
                        } else {
                            Toast.makeText(this@LoginActivity, "Falha no login", Toast.LENGTH_SHORT).show()
                        }
                    }
                } catch (e: Exception) {
                    Log.e("LoginActivity", "Erro no login", e)
                    runOnUiThread {
                        Toast.makeText(this@LoginActivity, "Erro no login: ${e.message}", Toast.LENGTH_SHORT).show()
                    }
                }
            }
        }

        setupTextViews()
    }

    // --- Funções de permissão e fluxo ---

    private fun setupPermissionLaunchers() {
        requestPermissionLauncher = registerForActivityResult(
            ActivityResultContracts.RequestMultiplePermissions()
        ) { permissionsResult ->
            permissionsResult.entries.forEach {
                if (it.value) Log.d("StarkAid", "Permissão concedida: ${it.key}")
                else Log.w("StarkAid", "Permissão negada: ${it.key}")
            }
            requestNextPermission()
        }

        requestOverlayPermissionLauncher = registerForActivityResult(
            ActivityResultContracts.StartActivityForResult()
        ) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && Settings.canDrawOverlays(this)) {
                Log.d("StarkAid", "Permissão de sobreposição concedida")
            } else Log.w("StarkAid", "Permissão de sobreposição negada")
            requestNextPermission()
        }
    }

    private fun buildPermissionRequests() {
        permissionRequests.clear()

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) !=
            PackageManager.PERMISSION_GRANTED) {
            permissionRequests.add(PermissionRequest(Manifest.permission.POST_NOTIFICATIONS))
        }

        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) !=
            PackageManager.PERMISSION_GRANTED) {
            permissionRequests.add(PermissionRequest(Manifest.permission.RECORD_AUDIO))
        }

        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) !=
            PackageManager.PERMISSION_GRANTED) {
            permissionRequests.add(PermissionRequest(Manifest.permission.ACCESS_FINE_LOCATION))
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && !Settings.canDrawOverlays(this)) {
            val intent = Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:$packageName"))
            permissionRequests.add(PermissionRequest(intent = intent, isOverlay = true))
        }

        val notificationManager = getSystemService(NOTIFICATION_SERVICE) as android.app.NotificationManager
        if (!notificationManager.isNotificationPolicyAccessGranted) {
            val intent = Intent(Settings.ACTION_NOTIFICATION_POLICY_ACCESS_SETTINGS)
            permissionRequests.add(PermissionRequest(intent = intent, isOverlay = true))
        }
    }

    private fun requestNextPermission() {
        if (currentPermissionIndex >= permissionRequests.size) return
        val request = permissionRequests[currentPermissionIndex]
        currentPermissionIndex++
        if (request.isOverlay && request.intent != null) requestOverlayPermissionLauncher.launch(request.intent)
        else if (request.permission != null) requestPermissionLauncher.launch(arrayOf(request.permission))
        else requestNextPermission()
    }

    private fun checkAllPermissionsGranted(): Boolean {
        val permissions = mutableListOf<String>()
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) !=
            PackageManager.PERMISSION_GRANTED) permissions.add(Manifest.permission.RECORD_AUDIO)
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) !=
            PackageManager.PERMISSION_GRANTED) permissions.add(Manifest.permission.ACCESS_FINE_LOCATION)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) !=
            PackageManager.PERMISSION_GRANTED) permissions.add(Manifest.permission.POST_NOTIFICATIONS)

        val overlayOk = Build.VERSION.SDK_INT < Build.VERSION_CODES.M || Settings.canDrawOverlays(this)
        return permissions.isEmpty() && overlayOk
    }

    // --- Login e sessão ---

    private fun checkAuthToken(): Boolean {
        val savedToken = sessionManager.fetchAuthToken()
        if (!savedToken.isNullOrEmpty()) {
            if (sessionManager.isSessionExpired()) {
                sessionManager.clearSession()
                sessionManager.clearSessionExpired()
                return false
            } else {
                goToMain()
                return true
            }
        }
        return false
    }

    private fun handleSuccessfulLogin(response: AuthResponse) {
        SessionExpiredHandler.reset()
        Toast.makeText(this, "Login OK", Toast.LENGTH_SHORT).show()
        Log.d("AuthTokenStarkAid", "Token JWT: ${response.token}")

        sessionManager.saveAuthToken(response.token)
        sessionManager.saveRefreshToken(response.refreshToken)
        
        // Salvar dados do usuário do objeto user retornado
        response.user?.let { user ->
            sessionManager.saveUserId(user.id)
            sessionManager.saveApiKey(user.apiKey)
        } ?: run {
            // Fallback para compatibilidade com versões antigas
            response.id?.let { sessionManager.saveUserId(it) }
            response.apiKey?.let { sessionManager.saveApiKey(it) }
        }

        // Extrair role do token JWT em vez de fazer chamada à API
        lifecycleScope.launch(Dispatchers.IO) {
            val role = extractRoleFromToken(response.token)
            role?.let { sessionManager.saveUserRole(it) }
        }

        sessionManager.clearSessionExpired()

        FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
            if (task.isSuccessful) {
                val fcmToken = task.result
                sessionManager.saveFcmToken(fcmToken)

                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val retrofit = ApiClient.getClient(this@LoginActivity)
                        val api = retrofit.create(AuthApi::class.java)
                        api.registrarToken(RegistrarTokenRequest(fcmToken))
                        Log.d("LoginActivity", "Token FCM registrado após login")
                    } catch (e: Exception) {
                        Log.e("LoginActivity", "Erro ao registrar token FCM", e)
                    } finally {
                        runOnUiThread { goToMain() }
                    }
                }
            } else {
                Log.e("LoginActivity", "Falha ao obter token FCM", task.exception)
                goToMain()
            }
        }
    }

    private fun extractRoleFromToken(token: String): String? {
        return try {
            val parts = token.split(".")
            if (parts.size < 2) return null

            val payload = String(
                android.util.Base64.decode(parts[1], android.util.Base64.URL_SAFE or android.util.Base64.NO_WRAP or android.util.Base64.NO_PADDING),
                Charsets.UTF_8
            )
            val json = org.json.JSONObject(payload)
            if (json.has("role")) json.getString("role") else null
        } catch (e: Exception) {
            Log.e("LoginActivity", "Erro ao extrair role do token", e)
            null
        }
    }

    private fun goToMain() {
        startActivity(Intent(this, MainActivity::class.java))
        finish()
    }

    // --- TextViews clicáveis ---

    private fun setupTextViews() {
        val textForgotPassword = findViewById<TextView>(R.id.textForgotPassword)
        val fullText1 = "Esqueceu sua senha? Clique aqui!"
        val spannableForgot = SpannableString(fullText1)
        val start = fullText1.indexOf("Clique aqui!")
        val end = start + "Clique aqui!".length
        spannableForgot.setSpan(
            ForegroundColorSpan(ContextCompat.getColor(this, R.color.cardsMain)),
            start, end, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE
        )
        textForgotPassword.text = spannableForgot
        textForgotPassword.setOnClickListener { showForgotPasswordDialog() }

        val textRegister = findViewById<TextView>(R.id.textRegister)
        val fullText = "Não tem cadastro? Cadastre-se aqui!"
        val spannable = SpannableString(fullText)
        val startIndex = fullText.indexOf("Cadastre-se aqui!")
        val endIndex = startIndex + "Cadastre-se aqui!".length
        spannable.setSpan(
            ForegroundColorSpan(ContextCompat.getColor(this, R.color.cardsMain)),
            startIndex, endIndex, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE
        )
        textRegister.text = spannable
        textRegister.setOnClickListener { startActivity(Intent(this, RegisterActivity::class.java)) }
    }

    // --- Redefinir senha ---

    private fun showForgotPasswordDialog() {
        val builder = android.app.AlertDialog.Builder(this)
        builder.setTitle("Redefinir senha")
        val input = EditText(this)
        input.hint = "Digite seu e-mail"
        input.inputType = android.text.InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS
        builder.setView(input)
        builder.setPositiveButton("Redefinir") { _, _ ->
            val email = input.text.toString().trim()
            if (email.isNotEmpty()) sendPasswordResetRequest(email)
            else Toast.makeText(this, "Informe o e-mail", Toast.LENGTH_SHORT).show()
        }
        builder.setNegativeButton("Cancelar") { dialog, _ -> dialog.cancel() }
        builder.show()
    }

    private fun sendPasswordResetRequest(email: String) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@LoginActivity)
                val api = retrofit.create(AuthApi::class.java)
                val response = api.requestPasswordReset(mapOf("email" to email))
                runOnUiThread {
                    if (response.isSuccessful)
                        Toast.makeText(this@LoginActivity, "Instruções enviadas para o e-mail.", Toast.LENGTH_LONG).show()
                    else
                        Toast.makeText(this@LoginActivity, "Erro ao enviar solicitação. Verifique o e-mail.", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Log.e("LoginActivity", "Erro ao solicitar redefinição de senha", e)
                runOnUiThread {
                    Toast.makeText(this@LoginActivity, "Erro: ${e.localizedMessage}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }
}
