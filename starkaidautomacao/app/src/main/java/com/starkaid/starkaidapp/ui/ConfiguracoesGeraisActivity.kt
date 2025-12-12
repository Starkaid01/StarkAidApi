package com.starkaid.starkaidapp.ui

import android.app.AlertDialog
import android.content.Context.CONNECTIVITY_SERVICE
import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.cardview.widget.CardView
import androidx.core.net.toUri
import androidx.drawerlayout.widget.DrawerLayout
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.navigation.NavigationView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.ewelink.EwelinkLoginActivity
import com.starkaid.starkaidapp.ewelink.EwelinkDevicesActivity
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import com.starkaid.starkaidapp.services.FloatingButtonService.Companion.FloatingButtonServiceInstance
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.EwelinkApi
import org.json.JSONObject
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class ConfiguracoesGeraisActivity : BaseActivity()  {
    private lateinit var sessionManager: SessionManager
    // --Commented out by Inspection (20/08/2025 14:05):private lateinit var buttonMenu: ImageButton

    private lateinit var drawerLayout: DrawerLayout
    // --Commented out by Inspection (20/08/2025 14:05):private lateinit var navView: NavigationView
    // --Commented out by Inspection (20/08/2025 14:05):private lateinit var toolbar: MaterialToolbar


    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_configuracoes_gerais)

        sessionManager = SessionManager(this)

        // Verificar se o role está disponível, caso contrário tentar buscar
        val role = sessionManager.fetchUserRole()
        if (role == null || role.isEmpty()) {
            // Tentar buscar o role de forma assíncrona
            CoroutineScope(Dispatchers.IO).launch {
                try {
                    val fetchedRole = fetchUserRoleFromEndpoint()
                    fetchedRole?.let { 
                        sessionManager.saveUserRole(it)
                        runOnUiThread {
                            setupCardClicks()
                        }
                    } ?: runOnUiThread {
                        // Se não conseguir buscar, ainda permite usar a activity
                        // mas mostra um aviso
                        Toast.makeText(this@ConfiguracoesGeraisActivity, 
                            "Aviso: Não foi possível carregar dados do usuário. Algumas funcionalidades podem estar limitadas.", 
                            Toast.LENGTH_LONG).show()
                        setupCardClicks()
                    }
                } catch (e: Exception) {
                    Log.e("ConfiguracoesGerais", "Erro ao buscar role", e)
                    runOnUiThread {
                        setupCardClicks()
                    }
                }
            }
        } else {
            setupCardClicks()
        }

        FloatingButtonServiceInstance?.hideButton()



        val toolbar = findViewById<MaterialToolbar>(R.id.topAppBar)
        val navView = findViewById<NavigationView>(R.id.navView)
        drawerLayout = findViewById(R.id.drawerLayout)

        setSupportActionBar(toolbar)

        // ADICIONE ESTAS LINHAS PARA CONFIGURAR A SETA DE VOLTAR
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)

        navView.menu.findItem(R.id.home_page).isVisible = true
        navView.menu.findItem(R.id.nav_config).isVisible = false

        // Configura apenas a seta de voltar
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)

        // Configura o clique na seta de voltar
        toolbar.setNavigationOnClickListener {
            onBackPressed() // Volta para a activity anterior
        }
    }

    private fun logout(){
        val prefs = getSharedPreferences("starkaid_prefs", MODE_PRIVATE)
        prefs.edit().clear().apply()
        Toast.makeText(this, "Saindo da conta...", Toast.LENGTH_SHORT).show()

        // Volta para a tela de login
        val intent = Intent(this, LoginActivity::class.java)
        intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        startActivity(intent)
        finish()
    }

    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }
    private fun setupCardClicks() {
        val role = sessionManager.fetchUserRole()
        
        // Se ainda não tiver role após tentar buscar, apenas avisa mas não faz logout
        // O role é usado apenas para algumas verificações específicas
        if (role == null || role.isEmpty()) {
            Log.w("ConfiguracoesGerais", "Role não disponível, continuando sem restrições")
        }

        findViewById<CardView>(R.id.cardEwelink).setOnClickListener {
            // Verificar status antes de navegar
            verificarStatusEwelinkEConectar()
        }

        findViewById<CardView>(R.id.privacy).setOnClickListener {
            val uri = "${ApiConfig.webBaseUrl}/starkaid-privacy/privacy.html".toUri()
            val intent = Intent(Intent.ACTION_VIEW, uri)
            startActivity(intent)
        }

        findViewById<CardView>(R.id.cardConfigUser).setOnClickListener {
            // Implemente a navegação para Config. Usuário
            val intent = Intent(this, ConfigUsuarioActivity::class.java)
            startActivity(intent)
        }

        findViewById<CardView>(R.id.cardConfigApp).setOnClickListener {
            // Implemente a navegação para Config. App
            val intent = Intent(this, ConfigAppActivity::class.java)
            startActivity(intent)
            Toast.makeText(this, "Config. App", Toast.LENGTH_SHORT).show()
        }

        findViewById<CardView>(R.id.cardConfigStarkSwitch).setOnClickListener {
            when (role) {
                "UserNivel3" -> {
                    AlertDialog.Builder(this)
                        .setTitle("Atenção")
                        .setMessage("Seu pagamento esta atrasado! \nregularize-o para desbloquear essa opção!")
                        .setPositiveButton("OK", null)
                        .show()

                    return@setOnClickListener
                }
                else -> {
                    val intent = Intent(this, ConfigStarkSwitchActivity::class.java)
                    startActivity(intent)
                }
            }

        }

        findViewById<CardView>(R.id.cardConfigStarkEagle).setOnClickListener {
            when (role) {
                "UserNivel3" -> {
                    AlertDialog.Builder(this)
                        .setTitle("Atenção")
                        .setMessage("Seu pagamento esta atrasado! \nregularize-o para desbloquear essa opção!")
                        .setPositiveButton("OK", null)
                        .show()

                    return@setOnClickListener
                }
                else -> {
                    // Mantemos a navegação existente para StarkEagle
                    val intent = Intent(this, ConfigStarkEagleActivity::class.java)
                    startActivity(intent)
                }
            }
        }

        findViewById<CardView>(R.id.cardConfigComandos).setOnClickListener {
            val intent = Intent(this, ComandosSociaisActivity::class.java)
            startActivity(intent)
        }

        findViewById<CardView>(R.id.cardDispositivosEsp).setOnClickListener {
            val intent = Intent(this, DispositivosEspActivity::class.java)
            startActivity(intent)
        }

        findViewById<CardView>(R.id.cardBotWhatsapp).setOnClickListener {
            // Implemente a navegação para Bot-Whatsapp
            Toast.makeText(this, "Bot-Whatsapp", Toast.LENGTH_SHORT).show()
        }

        findViewById<CardView>(R.id.cardAgendamentos).setOnClickListener {
            val intent = Intent(this, AgendamentosActivity::class.java)
            startActivity(intent)
        }

        findViewById<CardView>(R.id.cardMeusPlanos).setOnClickListener {
            // Implemente a navegação para Meus Planos
            val intent = Intent(this, PlanosActivity::class.java)
            startActivity(intent)
        }

        findViewById<CardView>(R.id.cardContato).setOnClickListener {
            // Implemente a navegação para Contato
            Toast.makeText(this, "Entre em Contato", Toast.LENGTH_SHORT).show()
        }

        findViewById<CardView>(R.id.deleteAcount).setOnClickListener {
            val intent = Intent(this, DeleteAccountActivity::class.java)
            startActivity(intent)
        }

    }

    @Suppress("DEPRECATION")
    private fun isOnline(): Boolean {
        val connectivityManager = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = connectivityManager.activeNetwork
        val capabilities = connectivityManager.getNetworkCapabilities(network)
        return capabilities != null && (capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) ||
                capabilities.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) ||
                capabilities.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET))
    }

    private fun extractRoleFromToken(token: String?): String? {
        if (token.isNullOrEmpty()) return null
        
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
            Log.e("ConfiguracoesGerais", "Erro ao extrair role do token", e)
            null
        }
    }
    
    private suspend fun fetchUserRoleFromEndpoint(): String? {
        val token = sessionManager.fetchAuthToken()
        return extractRoleFromToken(token)
    }
    
    private fun verificarStatusEwelinkEConectar() {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            // Se não tem credenciais, abrir tela de login
            val intent = Intent(this, EwelinkLoginActivity::class.java)
            startActivity(intent)
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@ConfiguracoesGeraisActivity)
                val ewelinkApi = retrofit.create(EwelinkApi::class.java)
                
                val response = ewelinkApi.getStatus()
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val status = response.body()!!
                        if (status.isLoggedIn) {
                            // Usuário está conectado, abrir tela de dispositivos
                            val intent = Intent(this@ConfiguracoesGeraisActivity, EwelinkDevicesActivity::class.java)
                            startActivity(intent)
                        } else {
                            // Usuário não está conectado, abrir tela de login
                            val intent = Intent(this@ConfiguracoesGeraisActivity, EwelinkLoginActivity::class.java)
                            startActivity(intent)
                        }
                    } else {
                        // Erro ao verificar, abrir tela de login
                        Log.e("ConfigEwelink", "Erro ao verificar status: ${response.code()}")
                        val intent = Intent(this@ConfiguracoesGeraisActivity, EwelinkLoginActivity::class.java)
                        startActivity(intent)
                    }
                }
            } catch (e: Exception) {
                Log.e("ConfigEwelink", "Erro ao verificar status", e)
                withContext(Dispatchers.Main) {
                    // Em caso de erro, abrir tela de login
                    val intent = Intent(this@ConfiguracoesGeraisActivity, EwelinkLoginActivity::class.java)
                    startActivity(intent)
                }
            }
        }
    }
}