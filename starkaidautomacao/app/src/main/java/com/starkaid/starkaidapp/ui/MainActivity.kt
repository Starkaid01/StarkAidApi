package com.starkaid.starkaidapp.ui

import android.Manifest
import android.annotation.SuppressLint
import android.app.Activity
import android.app.ActivityManager
import android.app.AlertDialog
import android.app.NotificationManager
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.SharedPreferences
import android.content.pm.PackageManager
import android.graphics.BitmapFactory
import android.graphics.Color
import android.graphics.Typeface
import android.location.Geocoder
import android.location.Location
import android.location.LocationListener
import android.location.LocationManager
import android.media.AudioManager
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import android.speech.SpeechRecognizer
import android.text.Editable
import android.text.TextWatcher
import android.text.method.ScrollingMovementMethod
import android.transition.AutoTransition
import android.transition.TransitionManager
import android.util.Log
import android.view.GestureDetector
import android.view.Gravity
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import android.view.WindowInsets
import android.view.animation.AccelerateInterpolator
import android.widget.Button
import android.widget.FrameLayout
import android.widget.ImageButton
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts

import android.webkit.JavascriptInterface
import android.webkit.WebChromeClient
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.appcompat.widget.SwitchCompat
import androidx.cardview.widget.CardView
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.core.content.edit
import androidx.core.net.toUri
import androidx.core.view.GravityCompat
import androidx.drawerlayout.widget.DrawerLayout
import androidx.lifecycle.lifecycleScope
import androidx.localbroadcastmanager.content.LocalBroadcastManager
import androidx.recyclerview.widget.DividerItemDecoration
import androidx.recyclerview.widget.GridLayoutManager
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.gms.ads.AdRequest
import com.google.android.gms.ads.FullScreenContentCallback
import com.google.android.gms.ads.LoadAdError
import com.google.android.gms.ads.MobileAds
import com.google.android.gms.ads.interstitial.InterstitialAd
import com.google.android.gms.ads.interstitial.InterstitialAdLoadCallback
import com.google.android.gms.common.ConnectionResult
import com.google.android.gms.common.GoogleApiAvailability
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.floatingactionbutton.FloatingActionButton
import com.google.android.material.navigation.NavigationView
import com.google.common.reflect.TypeToken
import com.google.gson.Gson
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.adapters.DeviceAdapter
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.data.AppDatabase
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.ewelink.EwelinkDeviceService
import com.starkaid.starkaidapp.ewelink.EwelinkLoginActivity
import com.starkaid.starkaidapp.ewelink.EwelinkVoiceControl
import com.starkaid.starkaidapp.ewelink.adapter.DeviceEwelinkAdapter
import com.starkaid.starkaidapp.ewelink.models.EwelinkDevice
import com.starkaid.starkaidapp.extensions.createHoverEffect
import com.starkaid.starkaidapp.config.ApiConfig
import com.starkaid.starkaidapp.models.ComandoSocialDao
import com.starkaid.starkaidapp.models.ComandoSocialEntity
import com.starkaid.starkaidapp.models.Device
import com.starkaid.starkaidapp.models.HubListener
import com.starkaid.starkaidapp.models.EconomicPayload
import com.starkaid.starkaidapp.services.ApiClient
import com.starkaid.starkaidapp.services.AuthService
import com.starkaid.starkaidapp.services.CommandApi
import com.starkaid.starkaidapp.services.CommandRequest
import com.starkaid.starkaidapp.services.DeviceApi
import com.starkaid.starkaidapp.services.DeviceOptimizationService
import com.starkaid.starkaidapp.services.DeviceResponse
import com.starkaid.starkaidapp.services.DeviceStatus
import com.starkaid.starkaidapp.services.DisparoApi
import com.starkaid.starkaidapp.services.FloatingButtonService
import com.starkaid.starkaidapp.services.FloatingButtonService.Companion.FloatingButtonServiceInstance
import com.starkaid.starkaidapp.services.FullDuplexAssistantAdvancedService
import com.starkaid.starkaidapp.services.FullDuplexAssistantAdvancedService.Companion.isAdShowing
import com.starkaid.starkaidapp.services.HealthApi
import com.starkaid.starkaidapp.services.HealthCheckApi
import com.starkaid.starkaidapp.services.HubService
import com.starkaid.starkaidapp.services.StatusApi
import com.starkaid.starkaidapp.services.UsersApi
import com.starkaid.starkaidapp.services.EwelinkApi
import com.starkaid.starkaidapp.services.AssinaturasApi
import com.starkaid.starkaidapp.services.DispositivosEspApi
import com.starkaid.starkaidapp.models.DispositivoEsp
import com.starkaid.starkaidapp.models.EnviarComandoRequest
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.starkaid.starkaidapp.models.ExternalAudioStreamResult
import io.reactivex.rxjava3.core.Single
import com.starkaid.starkaidapp.services.PlanoAtivoResponse
import com.starkaid.starkaidapp.services.VoiceSynthesizer
import com.starkaid.starkaidapp.services.WebSocketManager
import com.starkaid.starkaidapp.services.ErrorLoggerService
import com.starkaid.starkaidapp.services.ErrorLogSyncService
import com.starkaid.starkaidapp.services.ErrorCodes
import com.starkaid.starkaidapp.util.NotificationUtils
import com.starkaid.starkaidapp.util.SessionExpiredHandler
import com.unity3d.ads.IUnityAdsInitializationListener
import com.unity3d.ads.UnityAds
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.Runnable
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import kotlinx.coroutines.withTimeout
import okhttp3.Call
import okhttp3.Callback
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import org.json.JSONObject
import java.io.IOException
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.SocketTimeoutException
import java.io.File
import java.net.URLEncoder
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import com.starkaid.starkaidapp.models.ContatoEntity
import com.starkaid.starkaidapp.models.IaRequest
import com.starkaid.starkaidapp.models.MusicaDto
import com.starkaid.starkaidapp.models.RespostasAleatoriasDto
import com.starkaid.starkaidapp.security.SecureStorageManager
import com.starkaid.starkaidapp.services.AddNameRequest
import com.starkaid.starkaidapp.services.SpotifyService
import com.starkaid.starkaidapp.services.AnalizaTexto
import com.starkaid.starkaidapp.services.CriarSessaoRequest
import com.starkaid.starkaidapp.services.EnviarMensagemRequest
import com.starkaid.starkaidapp.services.ListarContatosRequest
import com.starkaid.starkaidapp.services.NlpApi
import com.starkaid.starkaidapp.services.NlpExtractRequest
import com.starkaid.starkaidapp.services.StatusSessaoRequest
import com.starkaid.starkaidapp.services.UsuarioApi
import com.starkaid.starkaidapp.services.WhatsappApi
import com.starkaid.starkaidapp.utils.StringUtils
import kotlinx.coroutines.Deferred
import kotlinx.coroutines.async
import java.text.Normalizer
import java.util.concurrent.atomic.AtomicBoolean
import kotlin.jvm.java
import com.unity3d.ads.BuildConfig
import com.starkaid.starkaidapp.services.pipeline.*
import com.starkaid.starkaidapp.models.AnaliseTexto
import com.starkaid.starkaidapp.models.MusicResolveRequest
import com.starkaid.starkaidapp.services.ComodosApi
import com.starkaid.starkaidapp.services.MusicApi
import com.starkaid.starkaidapp.services.RadioPlayerService

class MainActivity : BaseActivity(), DeviceAdapter.OnDeviceClickListener, HubListener {



    private lateinit var sessionManager: SessionManager
    private lateinit var prefs: SharedPreferences

    private lateinit var deviceAdapter: DeviceAdapter
    private val deviceList = mutableListOf<Device>()

    // Componentes de reconhecimento de voz
    private lateinit var tvSpeechText: TextView
    private lateinit var btnMicrophone: ImageButton
    private var speechRecognizer: SpeechRecognizer? = null
    private var isListening = false
    private val clearTextHandler = Handler(Looper.getMainLooper())
    private var clearTextRunnable: Runnable? = null
    // --Commented out by Inspection (20/08/2025 14:14):private val CLEAR_TEXT_DELAY = 40000L // 40 segundos

    private lateinit var voiceSynthesizer: VoiceSynthesizer
    private lateinit var authService: AuthService
    private lateinit var audioManager: AudioManager
    private var previousRingerMode: Int = AudioManager.RINGER_MODE_NORMAL
    private var contnvl1 = 0
    private lateinit var drawerLayout: DrawerLayout

    private lateinit var db: AppDatabase
    private lateinit var comandoDao: ComandoSocialDao
    private var comandosLocais = listOf<ComandoSocialEntity>()

    // Adicione este coletor de Flow
    private var comandosFlowJob: Job? = null

    // Variáveis para anúncios
    private var mInterstitialAd: InterstitialAd? = null
    private var adCounter = 0
    private val AD_FREQUENCYNivel2 = 4
    private val AD_FREQUENCYNivel1 = 3
    private val MIN_TIME_BETWEEN_ADS = 20000L // 20 segundos entre anúncios
    private var lastAdShowTime = 0L

    private var lastResumeTime = 0L
    private var appOpenCount = 0
    private val APP_OPEN_THRESHOLD = 2 // Exibir após 2 fechamentos completos
    private val APP_CLOSE_THRESHOLD = 30000L // 30 segundos para considerar fechamento

    private lateinit var wsManager: WebSocketManager
    private lateinit var hubService: HubService
    private lateinit var errorLogger: ErrorLoggerService
    private lateinit var errorLogSync: ErrorLogSyncService

    // Mutex para controle de concorrência de comandos
    private val commandMutex = Mutex()
    private var udpListenerJob: Job? = null

    // variável para controlar se os serviços foram inicializados
    private var servicesInitialized = false

    // mapa global para armazenar IPs dos dispositivos
    private val deviceIpMap = mutableMapOf<String, String>() // deviceId -> IP


    // --Commented out by Inspection START (20/08/2025 14:14):
    //    // Adicione estas constantes no topo da classe
    //    private val AD_RETRY_DELAY = 30000L // 30 segundos entre tentativas
    // --Commented out by Inspection STOP (20/08/2025 14:14)
    private var adRetryRunnable: Runnable? = null
    private val adHandler = Handler(Looper.getMainLooper())

    private val UNITY_GAME_ID = "5921564"

    // Adicione estas variáveis na classe MainActivity
    private lateinit var deviceCountView: TextView
    private lateinit var commandCountView: TextView
    private var commandCounter = 0

    var confirmContato = AtomicBoolean(false)

    var contato = ""
    var numero = ""
    var messageenviar = ""

    private val requestAudioPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            if (granted) {
                Log.i("FullDuplexAssistant", "Permissão de áudio concedida")
            } else {
                Toast.makeText(this, "Permissão de microfone necessária", Toast.LENGTH_LONG).show()
            }
        }

    private var isSpeechReceiverRegistered = false

    private lateinit var usuarioApi: UsuarioApi
    private lateinit var musicApi: MusicApi


    // Adicione esta constante
    private companion object {
        const val REQUEST_GOOGLE_PLAY_SERVICES = 1001
        const val NETWORK_PERMISSION_REQUEST_CODE = 1002
    }

    private var pendingVideoId: String? = null // Mantido mas não usado, poderia ser removido se garantirmos limpeza completa

    private var ultimoContextoUser: String = ""
    private var ultimoContextoIA: String = ""

    private lateinit var spotifyService: SpotifyService
    private var currentSource = "radio" // "radio" or "online"

    private lateinit var tvStarkcoins: TextView
    private lateinit var recogActive: TextView //recognition_active

    private lateinit var tvPlanLimitsTitle: TextView
    private lateinit var tvPlanFreeLine: TextView
    private lateinit var tvPlanPremiumLine: TextView

    private var starkCoins: Float = 0.0F
    private var saldoStarkcoinsInt: Int = 0
    private var iaLimitReached: Boolean = false
    private var aguardandoLiberarConsumoStarkcoins: Boolean = false
    private var iaUsandoStarkCoins: Boolean = false
    private var isSwitchIaChangingProgrammatically: Boolean = false
    private var recogInitialized = false

    private lateinit var switchSpotify: SwitchCompat
    private lateinit var switchWhatsapp: SwitchCompat

    private lateinit var switchIa: SwitchCompat

    // ---------------- Análise de texto ----------------
    private lateinit var analizaTexto: AnalizaTexto
    
    // ---------------- Pipeline de Comandos ----------------
    private lateinit var commandPipeline: PipelineEngine
    private lateinit var pipelineActions: AssistantActions

    private fun setupMiniPlayer() {
        miniPlayerContainer = findViewById(R.id.miniPlayerContainer)
        tvMiniPlayerStation = findViewById(R.id.tvMiniPlayerStation)
        btnMiniPlayerPlayPause = findViewById(R.id.btnMiniPlayerPlayPause)
        btnMiniPlayerStop = findViewById(R.id.btnMiniPlayerStop)

        


        btnMiniPlayerPlayPause.setOnClickListener {
            lifecycleScope.launch {
                if (RadioPlayerService.isRunning()) {
                    val action = if (RadioPlayerService.isPlaying()) RadioPlayerService.ACTION_PAUSE else RadioPlayerService.ACTION_PLAY
                    Log.d("MiniPlayer", "Toggling playback: current isPlaying=${RadioPlayerService.isPlaying()}, sending action=$action")
                    startService(Intent(this@MainActivity, RadioPlayerService::class.java).apply { this.action = action })
                }
            }
        }

        btnMiniPlayerStop.setOnClickListener {
            pipelineActions.stopMusic()
        }
    }

    private fun updateMiniPlayer(stationName: String?, isVisible: Boolean, sourceType: String = "RADIO") {
        runOnUiThread {
            if (isVisible && stationName != null) {
                val prefix = if (sourceType == "ONLINE") "📺 " else "📻 "
                tvMiniPlayerStation.text = prefix + stationName
                miniPlayerContainer.visibility = View.VISIBLE
            } else {
                miniPlayerContainer.visibility = View.GONE
            }
        }
    }

    



    private var contatosCache: List<ContatoEntity> = emptyList()

    private var btnAddStarkcoins: Button? = null

    private lateinit var nomeAssistent: String

    private lateinit var personalidade: String

    // Mini Player
    private lateinit var miniPlayerContainer: CardView
    private lateinit var tvMiniPlayerStation: TextView
    private lateinit var btnMiniPlayerPlayPause: ImageButton
    private lateinit var btnMiniPlayerStop: ImageButton

    // Variáveis para eWeLink
    private lateinit var tvExpandEwelink: TextView
    private lateinit var rvEwelinkDevices: RecyclerView
    private var isEwelinkExpanded = false
    private lateinit var ewelinkAdapter: DeviceEwelinkAdapter
    private lateinit var ewelinkDeviceService: EwelinkDeviceService
    private var ewelinkDeviceCount = 0
    private var ewelinkDevices: List<EwelinkDevice> = emptyList()
    private lateinit var ewelinkVoiceControl: EwelinkVoiceControl
    private val ewelinkOriginalText = "⚡ Dispositivos eWeLink"
    
    // Cache de dispositivos ESP para processamento de comandos de voz
    private var dispositivosEsp: List<DispositivoEsp> = emptyList()
    // WebSocket Hub para receber respostas de dispositivos ESP
    private var espHubConnection: HubConnection? = null

    private lateinit var tvExpandDevices: TextView
    private var isDevicesExpanded = false
    private lateinit var rvDevices: RecyclerView
    private val devicesOriginalText = "⚡ StarkSwitch"

    private var isIaResponsing = AtomicBoolean(false)
    private var lastDeviceType: String? = null
    private var lastTurnOnIntent: Boolean = true

    private lateinit var ewelinkSecureStorage: SecureStorageManager

    @SuppressLint("SetJavaScriptEnabled", "UseKtx")
    @Suppress("DEPRECATION")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        try {

            setContentView(R.layout.activity_main)
            setupMiniPlayer()
            logNetworkEnvironment()


            setupViews()
            authService = AuthService(this)
            sessionManager = SessionManager(this)
            audioManager = getSystemService(AUDIO_SERVICE) as AudioManager
            voiceSynthesizer = VoiceSynthesizer(this)
            errorLogger = ErrorLoggerService(this)
            errorLogSync = ErrorLogSyncService(this)
            isListening = false
            contnvl1 = sessionManager.fetchContNv1()
            previousRingerMode = audioManager.ringerMode // Salva o estado anterior
            drawerLayout = findViewById(R.id.drawer_layout)
            // Inicializar contador (sem incrementar)
            adCounter = sessionManager.fetchAdCounter()
            lastResumeTime = System.currentTimeMillis()
            // Inicializar contadores
            appOpenCount = sessionManager.fetchAppOpenCount()
            lastResumeTime = System.currentTimeMillis()
            tvStarkcoins = findViewById(R.id.tvStarkcoins)
            recogActive = findViewById(R.id.recognition_active)

            tvPlanLimitsTitle = findViewById(R.id.tvPlanLimitsTitle)
            tvPlanFreeLine = findViewById(R.id.tvPlanFreeLine)
            tvPlanPremiumLine = findViewById(R.id.tvPlanPremiumLine)

            switchSpotify = findViewById(R.id.switchSpotify)
            switchWhatsapp = findViewById(R.id.switchWhatsapp)
            switchIa = findViewById(R.id.switchIa)


            if (!recogInitialized) {
                recogActive.text = "Rec: STARKAID"
                recogInitialized = true
            }
            // Verificar conexão imediatamente
            if (!isOnline()) {
                Log.d("Network", "App iniciando offline")
                // Não tentar fazer nenhuma requisição de rede
            }


            if (isOnline()) {
                // busca e salva o role assincronamente se estiver online
                lifecycleScope.launch(Dispatchers.IO) {
                    val role = fetchUserRoleFromEndpoint()
                    role?.let { sessionManager.saveUserRole(it) }
                }
                // Marcar usuário como online
                lifecycleScope.launch(Dispatchers.IO) {
                    setUserOnline()
                }
            } else {
                // Offline: verifica se temos dados locais
                val localRole = sessionManager.fetchUserRole()
                if (localRole == null) {
                    runOnUiThread {
                        speakTextFromService("Dados de usuário não encontrados. Conecte-se à internet para atualizar seus dados.")
                        Toast.makeText(this@MainActivity, "Dados de usuário não encontrados. Conecte-se à internet para atualizar.", Toast.LENGTH_LONG).show()
                    }
                }
            }

            db = AppDatabase.getInstance(this)
            comandoDao = db.comandoSocialDao()

            val contatoDao = db.contatoDao()

            if (isOnline()) {
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val userId = sessionManager.fetchUserId()
                        val token = sessionManager.fetchAuthToken()

                        if (!userId.isNullOrEmpty() && !token.isNullOrEmpty()) {
                            val retrofit = ApiClient.getClient(this@MainActivity)
                            val whatsappApi = retrofit.create(WhatsappApi::class.java)

                            val response = whatsappApi.listarContatos(
                                ListarContatosRequest(userId, userId),
                                "Bearer $token"
                            )

                            if (response.isSuccessful && response.body() != null) {
                                val contatosServidor = response.body()!!.contatos.map {
                                    ContatoEntity(
                                        numero = it.numero,
                                        nome = StringUtils.normalizarNome(it.nome)
                                    )
                                }

                                val contatosLocal = contatoDao.getAll()
                                val numerosLocal = contatosLocal.map { it.numero }.toSet()

                                val novos = contatosServidor.filter { it.numero !in numerosLocal }

                                if (novos.isNotEmpty()) {
                                    contatoDao.insertAll(novos)
                                    Log.d("Contatos", "Inseridos ${novos.size} novos contatos.")
                                    novos.forEach { contato ->
                                        saveContactToBackend(contato.nome)
                                    }

                                } else {
                                    Log.d("Contatos", "Nenhum contato novo encontrado.")
                                }
                            } else {
                                Log.e("Contatos", "Erro ao buscar contatos: ${response.code()}")
                            }
                        }
                    } catch (e: Exception) {
                        Log.e("Contatos", "Falha ao sincronizar contatos", e)
                    }
                }
            } else {
                Log.d("Contatos", "Sem internet — usando contatos locais.")
            }

            getContactsFromList()


            // Substitua o carregamento inicial por esta observação contínua
            observarComandosLocais()


            // Verificar permissões antes de continuar
            checkNetworkPermissions()
            checkGooglePlayServices()

            prefs = getSharedPreferences("starkaid_prefs", Context.MODE_PRIVATE)

            // sessionManager já foi inicializado acima, não precisa inicializar novamente
            spotifyService = SpotifyService(this)

            analizaTexto = AnalizaTexto()
            tryStartAssistantService()
            initializePipeline()

            val assistentName = sessionManager.fetchAssistantName()
            val defaltResponse = sessionManager.fetchDefaultResponse()
            val personal = sessionManager.fetchAssistantPerson()

            if (personal != null){
                personalidade = personal
            }

            // Log detalhado para debug
            Log.d("MainActivity", "=== Verificação de Setup ===")
            Log.d("MainActivity", "Nome recuperado: '$assistentName'")
            Log.d("MainActivity", "É null? ${assistentName == null}")
            Log.d("MainActivity", "É blank? ${assistentName?.isBlank() ?: true}")
            if (assistentName != null) {
                Log.d("MainActivity", "É 'assistente'? ${assistentName.trim().equals("assistente", ignoreCase = true)}")
            }

            // Verificar se precisa mostrar tela de configuração inicial
            // Só mostrar se: nome for null, vazio, ou for "assistente" (case insensitive)
            val needsSetup = assistentName == null 
                    || assistentName.isBlank() 
                    || assistentName.trim().equals("assistente", ignoreCase = true)
            
            Log.d("MainActivity", "Needs setup? $needsSetup")
            
            if (needsSetup) {
                Log.d("MainActivity", "Nome do assistente não configurado ou é 'assistente'. Mostrando tela de setup. Nome atual: '$assistentName'")
                val intent = Intent(this, SetupAssistantNameActivity::class.java)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                startActivity(intent)
                finish()
                return
            }

            // Nome válido encontrado, usar ele
            nomeAssistent = assistentName?.lowercase()?.trim() ?: "assistente"
            Log.d("MainActivity", "✅ Nome do assistente carregado com sucesso: '$nomeAssistent'")

            try {
                GoogleApiAvailability.getInstance().makeGooglePlayServicesAvailable(this)
            } catch (e: Exception) {
                Log.e("GooglePlay", "Google Play Services unavailable", e)
            }

            val status = GoogleApiAvailability.getInstance().isGooglePlayServicesAvailable(this)
            if (status != ConnectionResult.SUCCESS) {
                Log.e("GPS", "Google Play Services indisponível")
            }

            SessionExpiredHandler.onSessionExpired = {
                runOnUiThread {
                    Toast.makeText(this, "Sessão expirada", Toast.LENGTH_SHORT).show()
                    val intent = Intent(this, LoginActivity::class.java)
                    intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                    startActivity(intent)
                    finish()
                }
            }

            if (sessionManager.fetchAuthToken().isNullOrEmpty()) {
                redirectToLogin()
                return
            }

            // busca e salva o role assincronamente
            if (isOnline()) {
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val role = fetchUserRoleFromEndpoint()
                        role?.let { sessionManager.saveUserRole(it) }
                    } catch (e: Exception) {
                        errorLogger.logError(
                            e,
                            ErrorCodes.ERR_104,
                            "ao buscar role do usuário",
                            null,
                            null,
                            null
                        )
                        // Não faz nada - usa o role local se disponível
                    }
                }
            } else {
                // Offline: verifica se temos um role local
                val localRole = sessionManager.fetchUserRole()
                if (localRole == null) {
                    runOnUiThread {
                        if (::voiceSynthesizer.isInitialized) {
                            speakTextFromService("Dados de usuário não encontrados. Conecte-se à internet para atualizar seus dados.")
                        }
                        Toast.makeText(this@MainActivity,
                            "Dados de usuário não encontrados. Conecte-se à internet para atualizar.",
                            Toast.LENGTH_LONG).show()
                    }
                }
            }


            val retrofit = ApiClient.getClient(this)
            usuarioApi = retrofit.create(UsuarioApi::class.java)
            musicApi = retrofit.create(MusicApi::class.java)

            // Verificar se está em processo de resolução de suporte
            lifecycleScope.launch(Dispatchers.IO) {
                if (isOnline()) {
                    verificarResolvendoSuporte()
                }
            }

            // Restaurar estado salvo
            val isSpotifyEnabled = prefs.getBoolean("spotify_enabled", false)
            val isWhatsappEnabled = prefs.getBoolean("whatsapp_enabled", false)
            val isIaEnabled = prefs.getBoolean("ia_enabled", false)

            switchIa.isChecked = isIaEnabled
            switchIa.setOnCheckedChangeListener { _, isChecked ->
                // Se o switch está sendo alterado programaticamente, não resetar flags
                if (isSwitchIaChangingProgrammatically) {
                    Log.d("TestandoIA", "⚠️ Switch alterado programaticamente - ignorando listener")
                    return@setOnCheckedChangeListener
                }
                
                // Se atingiu limite, perguntar antes de reativar com StarkCoins
                if (isChecked && iaLimitReached) {
                    isSwitchIaChangingProgrammatically = true
                    switchIa.isChecked = false
                    isSwitchIaChangingProgrammatically = false
                    mostrarDialogLimiteIa()
                    return@setOnCheckedChangeListener
                }

                prefs.edit().putBoolean("ia_enabled", isChecked).apply()
                if (isChecked) {
                    lifecycleScope.launch {
                        chamarIaSuper("ativar inteligencia", true)
                    }
                } else {
                    Log.d("TestandoIA", "⚠️ Switch IA desativado manualmente pelo usuário - resetando flags")
                    iaLimitReached = false
                    aguardandoLiberarConsumoStarkcoins = false
                    iaUsandoStarkCoins = false
                    speakTextFromService("Inteligencia desativada.")
                    Toast.makeText(this, "IA desativada", Toast.LENGTH_SHORT).show()
                }
            }

            switchWhatsapp.isChecked = isWhatsappEnabled
            switchWhatsapp.setOnCheckedChangeListener { _, isChecked ->
                prefs.edit().putBoolean("whatsapp_enabled", isChecked).apply()
                if (isChecked) {
                    //
                    statusSessionGet()
                } else {
                    speakTextFromService("whatsapp desativado.")
                    Toast.makeText(this, "whatsapp desativado", Toast.LENGTH_SHORT).show()
                }
            }

            switchSpotify.isChecked = isSpotifyEnabled

            switchSpotify.setOnCheckedChangeListener { _, isChecked ->
                prefs.edit().putBoolean("spotify_enabled", isChecked).apply()
                if (isChecked) {
                    Toast.makeText(this, "Em manutenção!", Toast.LENGTH_SHORT).show()

                    runOnUiThread {
                        speakTextFromService("Em manutençao! mas se quizer ouvir alguma musica especifica é só dizer: toque. mais o nome da musica que deseja!")
                    }
                    switchSpotify.isChecked = false
//                    val token = prefs.getString("spotify_access_token", null)
//                    if (token.isNullOrEmpty()) {
//                        startSpotifyLogin()
//                    } else {
//                        speakTextFromService("Spotify ativado.")
//                        Toast.makeText(this, "Spotify já conectado", Toast.LENGTH_SHORT).show()
//                    }
                } else {
                    speakTextFromService("Spotify desativado.")
                    Toast.makeText(this, "Spotify desativado", Toast.LENGTH_SHORT).show()
                }
            }

            getStarkcoins()

            val today = SimpleDateFormat("yyyy-MM-dd", Locale("pt", "BR")).format(Date())
            val lastReset = sessionManager.fetchLastResetDate()

            if (lastReset != today) {
                contnvl1 = 0
                sessionManager.saveContNv1(0)
                sessionManager.saveLastResetDate(today)
            }


            // Setup Comodos Button reusing the old TextView or finding a new button
            // Modifying tvExpandDevices to act as "Dispositivos" button
            tvExpandDevices = findViewById(R.id.tvExpandDevices) // Assuming ID exists
            tvExpandDevices.text = "🏠 Dispositivos (Cômodos)"
            tvExpandDevices.setOnClickListener {
                startActivity(Intent(this, ComodosActivity::class.java))
            }
            
            // Remove/Hide other lists
            findViewById<View>(R.id.rvDevices).visibility = View.GONE
            findViewById<View>(R.id.rvEwelinkDevices).visibility = View.GONE
            findViewById<View>(R.id.tvExpandEwelink).visibility = View.GONE


            window.decorView.postDelayed({
                findViewById<FloatingActionButton>(R.id.btnMicrophone).createHoverEffect()
            }, 300)

            // Atualização dinâmica de status
            lifecycleScope.launch {
                while (isActive) {
                    updateConnectionStatus()
                    delay(5000) // Atualiza a cada 5 segundos
                }
            }

            val notificationManager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager

            if (notificationManager.isNotificationPolicyAccessGranted) {
                audioManager.ringerMode = AudioManager.RINGER_MODE_VIBRATE
            } else {
                Toast.makeText(this, "Permissão de modo Não Perturbe não concedida", Toast.LENGTH_SHORT).show()
                val intent = Intent(Settings.ACTION_NOTIFICATION_POLICY_ACCESS_SETTINGS)
                startActivity(intent)
            }

            val intent = Intent(this, FloatingButtonService::class.java)
            startService(intent)

            val toolbar = findViewById<MaterialToolbar>(R.id.topAppBar)
            val navView = findViewById<NavigationView>(R.id.nav_view)


            navView.menu.findItem(R.id.home_page).isVisible = false

            // Deixa o ícone de hambúrguer funcional
            toolbar.setNavigationOnClickListener {
                drawerLayout.openDrawer(GravityCompat.START)
            }

            // Menu lateral (Navigation Drawer)
            navView.setNavigationItemSelectedListener { menuItem ->
                when (menuItem.itemId) {
                    R.id.nav_close -> {
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.home_page -> {
                        startActivity(Intent(this, MainActivity::class.java))
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_config -> {
                        startActivity(Intent(this, ConfiguracoesGeraisActivity::class.java))
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_rotinas -> {
                        startActivity(Intent(this, RotinasActivity::class.java))
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_chat_suporte -> {
                        val intent = Intent(this, ChatSuporteActivity::class.java)
                        startActivity(intent)
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_ewelink -> {
                        Toast.makeText(this, "Acesso rápido EweLink", Toast.LENGTH_SHORT).show()
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_starkswitch -> {
                        Toast.makeText(this, "Acesso rápido StarkSwitch", Toast.LENGTH_SHORT).show()
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_planos -> {
                        Toast.makeText(this, "Ver Meus Planos", Toast.LENGTH_SHORT).show()
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    R.id.nav_logout -> {
                        logout()
                        drawerLayout.closeDrawer(GravityCompat.START)
                        true
                    }
                    else -> false
                }
            }

            startUdpListener()

            // Verificar se foi um fechamento completo
            val lastCloseTime = sessionManager.fetchLastCloseTime()
            if (lastCloseTime > 0 && System.currentTimeMillis() - lastCloseTime > APP_CLOSE_THRESHOLD) {
                appOpenCount++
                Log.d("AppOpen", "contador fechamento: $appOpenCount")
                sessionManager.saveAppOpenCount(appOpenCount)
            }

            // MODIFICAÇÃO: Inicializar serviços apenas após validação do token
            initializeServicesAfterValidation()


            // Criar canal de notificação para otimização
            NotificationUtils.createOptimizationChannel(this)

            checkPermissionsAndInitAds(this)

            // Sincronizar configurações do banco para SessionManager (apenas se não existir no SessionManager)
            lifecycleScope.launch {
                val nomeBanco = getAssistantName()
                val respostaBanco = getDefaultResponse()
                val persBanco = getAssistantPerson()
                
                // Só salvar no SessionManager se:
                // 1. O valor do banco não for o padrão "Assistente"
                // 2. E não existir valor no SessionManager (ou for "Assistente")
                val nomeAtual = sessionManager.fetchAssistantName()
                if (nomeBanco.isNotEmpty() 
                    && !nomeBanco.equals("Assistente", ignoreCase = true)
                    && (nomeAtual == null || nomeAtual.isBlank() || nomeAtual.equals("Assistente", ignoreCase = true))) {
                    sessionManager.saveAssistantName(nomeBanco)
                }
                
                val respostaAtual = sessionManager.fetchDefaultResponse()
                if (respostaBanco.isNotEmpty() && respostaAtual == null) {
                    sessionManager.saveDefaultResponse(respostaBanco)
                }
                
                val persAtual = sessionManager.fetchAssistantPerson()
                if (persBanco.isNotEmpty() && persAtual == null) {
                    sessionManager.saveAssistantPerson(persBanco)
                }
            }

            adsGet()

            // Registrar receptor para "Próxima Música" vindo do rádio
            LocalBroadcastManager.getInstance(this).registerReceiver(object : BroadcastReceiver() {
                override fun onReceive(context: Context?, intent: Intent?) {
                    lifecycleScope.launch {
                        pipelineActions.resolveAndPlayMusic("próxima rádio")
                    }
                }
            }, IntentFilter("com.starkaid.MUSIC_NEXT"))

            LocalBroadcastManager.getInstance(this).registerReceiver(object : BroadcastReceiver() {
                override fun onReceive(context: Context?, intent: Intent?) {
                    val isPlaying = intent?.getBooleanExtra("isPlaying", false) ?: false
                    runOnUiThread {
                        btnMiniPlayerPlayPause.setImageResource(
                            if (isPlaying) android.R.drawable.ic_media_pause else android.R.drawable.ic_media_play
                        )
                    }
                }
            }, IntentFilter("com.starkaid.MUSIC_STATE_CHANGED"))

            LocalBroadcastManager.getInstance(this).registerReceiver(object : BroadcastReceiver() {
                override fun onReceive(context: Context?, intent: Intent?) {
                    // Resume logic handled by service notification actions or voice commands
                }
            }, IntentFilter("com.starkaid.MUSIC_PLAY"))



            LocalBroadcastManager.getInstance(this).registerReceiver(object : BroadcastReceiver() {
                override fun onReceive(context: Context?, intent: Intent?) {
                     updateMiniPlayer(null, false)
                }
            }, IntentFilter("com.starkaid.MUSIC_STOP"))

            setupUnityAdsFullScreen()

            btnAddStarkcoins = findViewById<Button>(R.id.btnRemoveAds)
            btnAddStarkcoins?.setOnClickListener {
                val intent = Intent(this, AddStarkcoinsActivity::class.java)
                startActivity(intent)
            }
            
            // Verificar planos ativos para desabilitar anúncios
            verificarPlanosAtivosParaAds()

            escutando.set(true)
            updateAvatarSleepingState()
            iaativa.set(true)
            // Só iniciar timer se o reconhecimento estiver ativo
            if (isListening) {
            iniciarTimerDesativacaoEscutando()
            }
            iniciarTimerIaDesativacao()

            val emailUser = sessionManager.fetchUserEmail()
            val userId = sessionManager.fetchUserId()
            obterDadosUser()

            // Sincronizar logs de erro após login (uma única vez ao iniciar)
            if (userId != null && !sessionManager.fetchAuthToken().isNullOrEmpty()) {
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        errorLogSync.syncLogsToBackend()
                    } catch (e: Exception) {
                        Log.e("MainActivity", "Erro ao sincronizar logs", e)
                        // Não bloqueia o app se a sincronização falhar
                    }
                }
            }

            // 🔥 CORREÇÃO: Inicialização ÚNICA do SecureStorage
            ewelinkSecureStorage = SecureStorageManager(this)

            // 🔥 CORREÇÃO: Inicializar serviços com a MESMA instância
            ewelinkDeviceService = EwelinkDeviceService(ewelinkSecureStorage)
            ewelinkVoiceControl = EwelinkVoiceControl(this, ewelinkDeviceService)

            // Pré-carregar dispositivos eWeLink automaticamente ao iniciar
            preCarregarDispositivos()

            // Verificar se há tokens eWeLink para log adicional
            val ewelinkTokens = ewelinkSecureStorage.getEwelinkTokens()
            if (ewelinkTokens != null) {
                Log.d("EWE_MAIN", "✅ Usuário logado no eWeLink - Tokens encontrados")
                Log.d("EWE_MAIN", "Access Token: ${ewelinkTokens.accessToken.take(10)}...")
                Log.d("EWE_MAIN", "Expira em: ${Date(ewelinkTokens.atExpiredTime)}")
            } else {
                Log.d("EWE_MAIN", "❌ Usuário não logado no eWeLink")
            }

//            try {



        } catch (e: Exception) {
            // Registrar erro usando ErrorLoggerService se disponível
            if (::errorLogger.isInitialized) {
                errorLogger.logError(
                    e,
                    ErrorCodes.ERR_801,
                    "ao inicializar aplicativo no onCreate"
                )
            } else {
                Log.e("MainActivity", "Erro fatal no onCreate", e)
            }
            Toast.makeText(this, "Erro ao inicializar o aplicativo", Toast.LENGTH_LONG).show()
            // Não fecha o app, apenas mostra mensagem de erro
        }

    }



    private fun setupViews() {
        checkPermissionsRecog()

        // Configurar os novos botões de ação rápida
        val btnOptimize = findViewById<CardView>(R.id.btnOptimize)
        val btnCleanCache = findViewById<CardView>(R.id.btnCleanCache)
        val btnRefresh = findViewById<CardView>(R.id.btnRefresh)

        btnOptimize.setOnClickListener {
            optimizePhone()
        }

        btnCleanCache.setOnClickListener {
            cleanCache()
        }

        btnRefresh.setOnClickListener {
            refreshData()
        }

        tvExpandDevices = findViewById(R.id.tvExpandDevices)
        rvDevices = findViewById(R.id.rvDevices)

        // Configurar o clique para expandir/recolher
        tvExpandDevices.setOnClickListener {
            toggleDevicesSection()
        }

        rvDevices = findViewById(R.id.rvDevices)
        rvDevices.layoutManager = GridLayoutManager(this, 2)

        // Configurar o RecyclerView
        setupRecyclerView()


        // NOVA CONFIGURAÇÃO: Seção eWeLink
        tvExpandEwelink = findViewById(R.id.tvExpandEwelink)
        rvEwelinkDevices = findViewById(R.id.rvEwelinkDevices)
        tvExpandEwelink.setOnClickListener { toggleEwelinkSection() }
        setupEwelinkRecyclerView()

        Log.d("Teste_Toggle", "eWeLink Section - TextView: $tvExpandEwelink, RecyclerView: $rvEwelinkDevices")

        tvSpeechText = findViewById(R.id.tvSpeechText)
        // Habilita rolagem
        tvSpeechText.movementMethod = ScrollingMovementMethod()

        // Rolagem automática
        tvSpeechText.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}

            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}

            override fun afterTextChanged(s: Editable?) {
                val layout = tvSpeechText.layout
                if (layout != null) {
                    val scrollDelta = layout.getLineBottom(tvSpeechText.lineCount - 1) - tvSpeechText.height
                    if (scrollDelta > 0) {
                        tvSpeechText.scrollTo(0, scrollDelta)
                    }
                }
            }
        })
        
        // Clique no tvSpeechText para abrir caixa de texto
        tvSpeechText.setOnClickListener {
            showTextInputDialog()
        }

        btnMicrophone = findViewById(R.id.btnMicrophone)
        deviceCountView = findViewById(R.id.deviceCount)
        commandCountView = findViewById(R.id.commandCount)

        btnMicrophone.setImageResource(R.drawable.ic_mic_off)

        btnMicrophone.setOnClickListener {
            toggleSpeechRecognition()
        }

        // Inicialize os contadores
        deviceCountView.text = "0"
        commandCountView.text = commandCounter.toString()

        switchAvatar = findViewById(R.id.switchAvatar)
        avatarOverlayContainer = findViewById(R.id.avatarOverlayContainer)

        switchAvatar.setOnCheckedChangeListener { _, isChecked ->
            if (isUpdatingAvatarSwitch) return@setOnCheckedChangeListener
            setAvatarEnabled(isChecked)
        }
    }

    private fun setAvatarEnabled(enabled: Boolean) {
        avatarEnabled = enabled
        avatarAutoOpenJob?.cancel()
        avatarAutoOpenJob = null
        if (enabled) {
            showAvatarOverlay()
        } else {
            hideAvatarOverlay()
            drawerLayout.setDrawerLockMode(DrawerLayout.LOCK_MODE_UNLOCKED)
            sendAvatarSpeaking(false)
            sendAvatarAudioLevel(0)
        }
    }

    private fun scheduleAvatarAutoOpen(delayMs: Long) {
        avatarAutoOpenJob?.cancel()
        avatarAutoOpenJob = lifecycleScope.launch {
            delay(delayMs)
            if (avatarEnabled) {
                showAvatarOverlay()
            }
        }
    }

    private fun showAvatarOverlay() {
        if (!avatarEnabled) return
        drawerLayout.closeDrawer(GravityCompat.START, false)
        drawerLayout.setDrawerLockMode(DrawerLayout.LOCK_MODE_LOCKED_CLOSED)
        ensureAvatarWebView()
        avatarOverlayContainer.visibility = View.VISIBLE
        sendAvatarSpeaking(isTtsSpeaking)
        updateAvatarSleepingState()
        sendAvatarRestartReconstruction()
    }

    private fun hideAvatarOverlay() {
        avatarOverlayContainer.visibility = View.GONE
    }

    @SuppressLint("SetJavaScriptEnabled")
    private fun ensureAvatarWebView() {
        if (avatarWebView != null) return

        val webView = WebView(this)
        webView.layoutParams = FrameLayout.LayoutParams(
            FrameLayout.LayoutParams.MATCH_PARENT,
            FrameLayout.LayoutParams.MATCH_PARENT
        )
        webView.setBackgroundColor(Color.BLACK)
        webView.webViewClient = object : WebViewClient() {
            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                if (!avatarEnabled) return
                sendAvatarSpeaking(isTtsSpeaking)
                updateAvatarSleepingState()
                sendAvatarRestartReconstruction()
            }
        }
        webView.webChromeClient = WebChromeClient()
        webView.settings.javaScriptEnabled = true
        webView.settings.domStorageEnabled = true
        webView.settings.allowFileAccess = true
        webView.settings.allowContentAccess = true
        webView.addJavascriptInterface(object {
            @JavascriptInterface
            fun closeAvatar() {
                runOnUiThread { closeAvatarFromOverlay() }
            }
        }, "AndroidAvatar")

        val gestureDetector = GestureDetector(
            this,
            object : GestureDetector.SimpleOnGestureListener() {
                override fun onDoubleTap(e: MotionEvent): Boolean {
                    closeAvatarFromOverlay()
                    return true
                }
            }
        )
        avatarGestureDetector = gestureDetector
        webView.setOnTouchListener { _, event ->
            gestureDetector.onTouchEvent(event)
            false
        }

        avatarOverlayContainer.removeAllViews()
        avatarOverlayContainer.addView(webView)
        avatarWebView = webView

        webView.loadUrl("file:///android_asset/index.html")
    }

    private fun closeAvatarFromOverlay() {
        if (!avatarEnabled) return
        hideAvatarOverlay()
        scheduleAvatarAutoOpen(2 * 60 * 1000L)
    }

    private fun runOnMainThread(action: () -> Unit) {
        if (Looper.myLooper() == Looper.getMainLooper()) {
            action()
        } else {
            Handler(Looper.getMainLooper()).post { action() }
        }
    }

    private fun sendAvatarSpeaking(speaking: Boolean) {
        val js = "window.StarkaidAvatar && StarkaidAvatar.setSpeaking(${if (speaking) "true" else "false"})"
        runOnMainThread {
            val webView = avatarWebView ?: return@runOnMainThread
            webView.evaluateJavascript(js, null)
        }
    }

    private fun sendAvatarAudioLevel(level: Int) {
        val bounded = level.coerceIn(0, 100)
        val js = "window.StarkaidAvatar && StarkaidAvatar.setAudioLevel($bounded)"
        runOnMainThread {
            val webView = avatarWebView ?: return@runOnMainThread
            webView.evaluateJavascript(js, null)
        }
    }

    private fun sendAvatarBeat() {
        val js = "window.StarkaidAvatar && StarkaidAvatar.beat()"
        runOnMainThread {
            val webView = avatarWebView ?: return@runOnMainThread
            webView.evaluateJavascript(js, null)
        }
    }

    private fun sendAvatarRestartReconstruction() {
        val js = "window.StarkaidAvatar && StarkaidAvatar.restartReconstruction()"
        runOnMainThread {
            val webView = avatarWebView ?: return@runOnMainThread
            webView.evaluateJavascript(js, null)
        }
    }

    private fun sendAvatarSleeping(sleeping: Boolean) {
        val js = "window.StarkaidAvatar && StarkaidAvatar.setSleeping(${if (sleeping) "true" else "false"})"
        runOnMainThread {
            val webView = avatarWebView ?: return@runOnMainThread
            webView.evaluateJavascript(js, null)
        }
    }

    private fun sendAvatarMatrixStatus(message: String?, ttlMs: Int = 1600) {
        if (!avatarEnabled) return
        val text = message?.trim().orEmpty()
        val quoted = org.json.JSONObject.quote(text)
        val boundedTtl = ttlMs.coerceIn(0, 15000)
        val js = "window.StarkaidAvatar && StarkaidAvatar.setMatrixStatus($quoted, $boundedTtl)"
        runOnMainThread {
            val webView = avatarWebView ?: return@runOnMainThread
            webView.evaluateJavascript(js, null)
        }
    }

    private fun updateAvatarSleepingState() {
        if (!avatarEnabled) return
        sendAvatarSleeping(!escutando.get())
    }

    // NOVA FUNÇÃO: Configurar RecyclerView dos dispositivos eWeLink
    private fun setupEwelinkRecyclerView() {
        ewelinkAdapter = DeviceEwelinkAdapter(
            devices = emptyList(),
            onDeviceToggle = { device, isOn ->
                controlarDispositivoEwelink(device, isOn)
            },
            onBrightnessChange = { device, brightness ->
                controlarBrilhoEwelink(device, brightness)
            }
        )
        rvEwelinkDevices.adapter = ewelinkAdapter
        rvEwelinkDevices.layoutManager = LinearLayoutManager(this)
    }

    // NOVA FUNÇÃO: Alternar expansão da seção eWeLink
    // Chamar esta função quando o usuário expandir a seção eWeLink
    private fun toggleEwelinkSection() {
        isEwelinkExpanded = !isEwelinkExpanded

        Log.d("Teste_Toggle", "Toggle eWeLink - Expandido: $isEwelinkExpanded, ItemCount: ${ewelinkAdapter.itemCount}")

        TransitionManager.beginDelayedTransition(
            rvEwelinkDevices.parent as ViewGroup,
            AutoTransition().apply { duration = 300 }
        )

        if (isEwelinkExpanded) {
            // Mantém o texto original e apenas altera o ícone da seta
            tvExpandEwelink.text = ewelinkOriginalText
            tvExpandEwelink.setCompoundDrawablesRelativeWithIntrinsicBounds(
                null, null, 
                ContextCompat.getDrawable(this, R.drawable.ic_arrow_down), 
                null
            )
            rvEwelinkDevices.visibility = View.VISIBLE
            rvEwelinkDevices.layoutParams.height = ViewGroup.LayoutParams.WRAP_CONTENT

            // 🔥 ATUALIZAR: Sempre atualizar status quando expandir
            Log.d("Teste_Toggle", "Carregando/atualizando dispositivos eWeLink")
            carregarDispositivosEwelink()


        } else {
            // Mantém o texto original e apenas altera o ícone da seta
            tvExpandEwelink.text = ewelinkOriginalText
            tvExpandEwelink.setCompoundDrawablesRelativeWithIntrinsicBounds(
                null, null, 
                ContextCompat.getDrawable(this, R.drawable.ic_arrow_right), 
                null
            )
            rvEwelinkDevices.visibility = View.GONE
            rvEwelinkDevices.layoutParams.height = 0
        }
    }

    // 🔥 CORREÇÃO: Função robusta para carregar dispositivos usando API do backend
    private fun carregarDispositivosEwelink() {
        Log.d("EWE_MAIN", "🔄 Iniciando carregamento de dispositivos eWeLink via backend")

        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Log.e("EWE_MAIN", "❌ Credenciais não encontradas")
            runOnUiThread {
                Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_SHORT).show()
            }
            return
        }

        // Verificar status no backend primeiro
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val ewelinkApi = retrofit.create(com.starkaid.starkaidapp.services.EwelinkApi::class.java)
                
                val statusResponse = ewelinkApi.getStatus()
                
                withContext(Dispatchers.Main) {
                    if (statusResponse.isSuccessful && statusResponse.body() != null) {
                        val status = statusResponse.body()!!
                        if (status.isLoggedIn) {
                            Log.d("EWE_MAIN", "✅ Usuário conectado no backend - Carregando dispositivos...")
                            carregarDispositivosEwelinkDaApi(showErrors = true)
                        } else {
                            Log.e("EWE_MAIN", "❌ Usuário não conectado no backend")
                            runOnUiThread {
                                Toast.makeText(this@MainActivity, "Você precisa conectar sua conta Ewelink primeiro", Toast.LENGTH_LONG).show()
                            }
                        }
                    } else {
                        Log.e("EWE_MAIN", "❌ Erro ao verificar status: ${statusResponse.code()}")
                        runOnUiThread {
                            Toast.makeText(this@MainActivity, "Erro ao verificar status Ewelink", Toast.LENGTH_SHORT).show()
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_MAIN", "❌ Erro ao verificar status", e)
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    // Nova função para carregar dispositivos da API do backend
    private fun carregarDispositivosEwelinkDaApi(showErrors: Boolean = true) {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()

        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            return
        }

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val ewelinkApi = retrofit.create(com.starkaid.starkaidapp.services.EwelinkApi::class.java)
                
                val response = ewelinkApi.listarDispositivos()
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val devicesApi = response.body()!!
                        val dispositivos = devicesApi.map { device ->
                            val paramsJson = JSONObject().apply {
                                // Adicionar params recebidos se existirem
                                device.params?.forEach { (key, value) ->
                                    // Não adicionar switch se for array vazio ou valor inválido
                                    if (key == "switch") {
                                        val switchValue = when (value) {
                                            is List<*> -> if (value.isEmpty()) null else value.toString()
                                            is String -> if (value.isEmpty() || value == "[]") null else value
                                            else -> value.toString()
                                        }
                                        if (switchValue != null && switchValue != "[]" && switchValue != "off" && switchValue != "on") {
                                            // Se não for "on" ou "off", usar isOn do backend
                                            put("switch", if (device.isOn) "on" else "off")
                                        } else if (switchValue != null) {
                                            put(key, switchValue)
                                        } else {
                                            // Array vazio ou valor inválido, usar isOn do backend
                                            put("switch", if (device.isOn) "on" else "off")
                                        }
                                    } else {
                                        put(key, value)
                                    }
                                }
                                
                                // Se não tiver o campo switch nos params ou se for inválido, usar o isOn do backend
                                if (!has("switch")) {
                                    put("switch", if (device.isOn) "on" else "off")
                                    Log.d("EWE_MAIN", "Campo switch não encontrado nos params, usando isOn: ${device.isOn}")
                                } else {
                                    // Verificar se o switch é válido
                                    val currentSwitch = optString("switch", "")
                                    if (currentSwitch.isEmpty() || currentSwitch == "[]" || (currentSwitch != "on" && currentSwitch != "off")) {
                                        put("switch", if (device.isOn) "on" else "off")
                                        Log.d("EWE_MAIN", "Campo switch inválido ($currentSwitch), usando isOn: ${device.isOn}")
                                    }
                                }
                            }
                            
                            Log.d("EWE_MAIN", "Dispositivo: ${device.name}, isOn: ${device.isOn}, params recebidos: ${device.params}, paramsJson: ${paramsJson.toString()}")
                            
                            EwelinkDevice(
                                id = device.deviceId, // Usar deviceId (ID real do Ewelink) em vez de id (ID do banco)
                                name = device.name,
                                online = device.online,
                                params = paramsJson,
                                type = device.type?.toIntOrNull() ?: 0,
                                uiid = 0,
                                familyId = "",
                                roomId = ""
                            )
                        }

                        Log.d("EWE_MAIN", "✅ ${dispositivos.size} dispositivos eWeLink recebidos")

                        var onlineCount = 0
                        var offlineCount = 0

                        dispositivos.forEach { dispositivo ->
                            val status = dispositivo.params.optString("switch", "off")
                            val onlineStatus = if (dispositivo.online) "✅ ONLINE" else "❌ OFFLINE"
                            Log.d("EWE_STATUS", "📊 ${dispositivo.name}: $status | $onlineStatus")

                            if (dispositivo.online) onlineCount++ else offlineCount++
                        }

                        Log.d("EWE_STATUS", "📈 Resumo: $onlineCount online, $offlineCount offline")

                        ewelinkDevices = dispositivos
                        ewelinkAdapter.updateDevices(dispositivos)
                        ewelinkDeviceCount = dispositivos.size
                        atualizarContadorDispositivos()

                        // Atualizar o controle de voz
                        ewelinkVoiceControl.setDispositivos(dispositivos)

                        if (dispositivos.isEmpty() && showErrors) {
                            Log.d("EWE_MAIN", "❌ Nenhum dispositivo eWeLink encontrado")
                            Toast.makeText(this@MainActivity, "Nenhum dispositivo eWeLink encontrado", Toast.LENGTH_SHORT).show()
                        } else {
                            val mensagem = when {
                                offlineCount > 0 -> "$onlineCount online, $offlineCount offline"
                                else -> "${dispositivos.size} dispositivos carregados"
                            }
                            Log.d("EWE_MAIN", "🎯 $mensagem")
                        }
                    } else {
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE_MAIN", "Erro ao carregar dispositivos: ${response.code()} - $errorBody")
                        if (showErrors) {
                            Toast.makeText(this@MainActivity, "Erro ao carregar dispositivos: ${response.code()}", Toast.LENGTH_LONG).show()
                        } else {
                            Unit
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_MAIN", "Erro ao carregar dispositivos", e)
                withContext(Dispatchers.Main) {
                    if (showErrors) {
                        Toast.makeText(this@MainActivity, "Erro: ${e.message}", Toast.LENGTH_LONG).show()
                    } else {
                        Unit
                    }
                }
            }
        }
    }

    // 🔥 CORREÇÃO: Função separada para carregamento geral
    private fun carregarDispositivosEwelinkGeral() {
        Log.d("EWE", "🔄 Iniciando carregamento geral de dispositivos eWeLink")

        val tokens = ewelinkSecureStorage.getEwelinkTokens()
        if (tokens == null) {
            Log.e("EWE", "❌ Tokens desapareceram durante o carregamento")
            return
        }

        // Obter a família atual
        obterFamiliaAtualEwelink(tokens.accessToken, tokens.region) { familyId ->
            ewelinkDeviceService.listarDispositivos(
                familyId,
                onSuccess = { dispositivos ->
                    runOnUiThread {
                        Log.d("EWE", "✅ ${dispositivos.size} dispositivos eWeLink recebidos")

                        // 🔥 LOG DETALHADO DO STATUS
                        var onlineCount = 0
                        var offlineCount = 0

                        dispositivos.forEach { dispositivo ->
                            val status = dispositivo.params.optString("switch", "off")
                            val onlineStatus = if (dispositivo.online) "✅ ONLINE" else "❌ OFFLINE"
                            Log.d("EWE_STATUS", "📊 ${dispositivo.name}: $status | $onlineStatus")

                            if (dispositivo.online) onlineCount++ else offlineCount++
                        }

                        Log.d("EWE_STATUS", "📈 Resumo: $onlineCount online, $offlineCount offline")

                        ewelinkDevices = dispositivos
                        ewelinkAdapter.updateDevices(dispositivos)
                        ewelinkDeviceCount = dispositivos.size
                        atualizarContadorDispositivos()

                        // 🔥 ATUALIZAR O CONTROLE DE VOZ
                        ewelinkVoiceControl.setDispositivos(dispositivos)

                        if (dispositivos.isEmpty()) {
                            Log.d("EWE", "❌ Nenhum dispositivo eWeLink encontrado")
                            Toast.makeText(this, "Nenhum dispositivo eWeLink encontrado", Toast.LENGTH_SHORT).show()
                        } else {
                            val mensagem = when {
                                offlineCount > 0 -> "$onlineCount online, $offlineCount offline"
                                else -> "${dispositivos.size} dispositivos carregados"
                            }
                            Log.d("EWE", "🎯 $mensagem")
                            //Toast.makeText(this, mensagem, Toast.LENGTH_SHORT).show()
                        }
                    }
                },
                onError = { error ->
                    runOnUiThread {
                        Log.e("EWE", "❌ Erro ao carregar dispositivos: $error")
                        Toast.makeText(this, "Erro ao carregar dispositivos: $error", Toast.LENGTH_LONG).show()
                    }
                }
            )
        }
    }

    // NOVA FUNÇÃO: Obter família atual do eWeLink
    private fun obterFamiliaAtualEwelink(accessToken: String, region: String, onSuccess: (String) -> Unit) {
        val timestamp = System.currentTimeMillis()
        val nonce = generateNonce(8)

        val url = when(region) {
            "us" -> "https://us-apia.coolkit.cc/v2/family"
            "eu" -> "https://eu-apia.coolkit.cc/v2/family"
            "cn" -> "https://cn-apia.coolkit.cn/v2/family"
            else -> "https://as-apia.coolkit.cc/v2/family"
        }

        val request = okhttp3.Request.Builder()
            .url(url)
            .get()
            .addHeader("Authorization", "Bearer $accessToken")
            .addHeader("X-CK-Appid", "qPNNDkWlhKwh4xn41bteq2qD02aiGs3D")
            .addHeader("X-CK-Nonce", nonce)
            .addHeader("X-CK-Timestamp", timestamp.toString())
            .build()

        okhttp3.OkHttpClient().newCall(request).enqueue(object : okhttp3.Callback {
            override fun onFailure(call: okhttp3.Call, e: java.io.IOException) {
                Log.e("EWE", "Erro ao obter família: ${e.message}")
                runOnUiThread {
                    Toast.makeText(this@MainActivity, "Erro ao carregar família eWeLink", Toast.LENGTH_LONG).show()
                }
            }

            override fun onResponse(call: okhttp3.Call, response: okhttp3.Response) {
                val responseBody = response.body?.string() ?: ""

                try {
                    val json = org.json.JSONObject(responseBody)
                    if (json.optInt("error", 0) == 0) {
                        val data = json.getJSONObject("data")
                        val currentFamilyId = data.getString("currentFamilyId")
                        onSuccess(currentFamilyId)
                    } else {
                        runOnUiThread {
                            Toast.makeText(this@MainActivity, "Erro ao obter família eWeLink", Toast.LENGTH_LONG).show()
                        }
                    }
                } catch (e: Exception) {
                    Log.e("EWE", "Erro ao processar família: ${e.message}")
                    runOnUiThread {
                        Toast.makeText(this@MainActivity, "Erro ao processar família eWeLink", Toast.LENGTH_LONG).show()
                    }
                }
            }
        })
    }

    // NOVA FUNÇÃO: Controlar dispositivo eWeLink
    private fun controlarDispositivoEwelink(device: EwelinkDevice, isOn: Boolean) {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val ewelinkApi = retrofit.create(com.starkaid.starkaidapp.services.EwelinkApi::class.java)
                
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = isOn
                )
                
                val response = ewelinkApi.controlarDispositivo(device.id, request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val updatedDevice = response.body()!!
                        Log.d("EWE_MAIN", "Dispositivo atualizado - params: ${updatedDevice.params}")
                        
                        // Atualizar o estado local do dispositivo
                        val updatedDevices = ewelinkDevices.map { d ->
                            if (d.id == device.id) {
                                val newParams = JSONObject().apply {
                                    // Copiar params existentes primeiro
                                    val keys = d.params.keys()
                                    while (keys.hasNext()) {
                                        val key = keys.next()
                                        put(key, d.params.get(key))
                                    }
                                    
                                    // Atualizar com params da resposta se existirem
                                    if (updatedDevice.params != null && updatedDevice.params.isNotEmpty()) {
                                        updatedDevice.params.forEach { (key, value) ->
                                            put(key, value)
                                        }
                                    }
                                    
                                    // Garantir que o switch está atualizado com o valor correto
                                    put("switch", if (isOn) "on" else "off")
                                }
                                
                                Log.d("EWE_MAIN", "Novos params: ${newParams.toString()}")
                                
                                d.copy(
                                    params = newParams,
                                    online = updatedDevice.online
                                )
                            } else {
                                d
                            }
                        }
                        ewelinkDevices = updatedDevices
                        ewelinkAdapter.updateDevices(updatedDevices)

                        val action = if (isOn) "ligado" else "desligado"
                        Toast.makeText(this@MainActivity, "${device.name} $action", Toast.LENGTH_SHORT).show()
                        
                        // Criar mensagem de resposta para WebSocket e TTS
                        // Verificar se é feminino (luz, lâmpada, etc)
                        val deviceNameLower = device.name.lowercase()
                        val isFeminino = deviceNameLower.contains("luz") || 
                                        deviceNameLower.contains("lampada") || 
                                        deviceNameLower.contains("lâmpada") ||
                                        deviceNameLower.contains("cafeteira") || 
                                        deviceNameLower.contains("torradeira") ||
                                        deviceNameLower.contains("chapa") || 
                                        deviceNameLower.contains("geladeira") ||
                                        deviceNameLower.contains("tomada")
                        
                        val mensagemResposta = if (isFeminino) {
                            if (isOn) "Liguei a ${device.name}" else "Desliguei a ${device.name}"
                        } else {
                            if (isOn) "Liguei o ${device.name}" else "Desliguei o ${device.name}"
                        }
                        
                        // Falar a mensagem
                        speakTextFromService(mensagemResposta)
                        
                        // Enviar resposta via WebSocket com prefixo "toSoft:"
                        enviarRespostaWebSocket(mensagemResposta)
                    } else {
                        // Reverter a mudança no UI
                        ewelinkAdapter.updateDevices(ewelinkDevices)
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE_MAIN", "Erro ao controlar dispositivo: ${response.code()} - $errorBody")
                        Toast.makeText(this@MainActivity, "Erro ao controlar ${device.name}: ${response.code()}", Toast.LENGTH_LONG).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_MAIN", "Erro ao controlar dispositivo", e)
                withContext(Dispatchers.Main) {
                    // Reverter a mudança no UI
                    ewelinkAdapter.updateDevices(ewelinkDevices)
                    Toast.makeText(this@MainActivity, "Erro: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    // NOVA FUNÇÃO: Controlar brilho dispositivo eWeLink
    private fun controlarBrilhoEwelink(device: EwelinkDevice, brightness: Int) {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Toast.makeText(this, "Credenciais não encontradas", Toast.LENGTH_LONG).show()
            return
        }

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val ewelinkApi = retrofit.create(com.starkaid.starkaidapp.services.EwelinkApi::class.java)
                
                // Para controlar brilho, precisamos enviar switch on
                val request = com.starkaid.starkaidapp.services.EwelinkControlRequest(
                    switch = true
                )
                
                val response = ewelinkApi.controlarDispositivo(device.id, request)
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && response.body() != null) {
                        val updatedDevice = response.body()!!
                        // Atualizar o estado local do dispositivo
                        val updatedDevices = ewelinkDevices.map { d ->
                            if (d.id == device.id) {
                                val newParams = JSONObject().apply {
                                    if (updatedDevice.params != null && updatedDevice.params.isNotEmpty()) {
                                        updatedDevice.params.forEach { (key, value) ->
                                            put(key, value)
                                        }
                                    } else {
                                        // Copiar params existentes
                                        val keys = d.params.keys()
                                        while (keys.hasNext()) {
                                            val key = keys.next()
                                            put(key, d.params.get(key))
                                        }
                                    }
                                    put("brightness", brightness)
                                    put("switch", "on")
                                }
                                d.copy(
                                    params = newParams,
                                    online = updatedDevice.online
                                )
                            } else {
                                d
                            }
                        }
                        ewelinkDevices = updatedDevices
                        ewelinkAdapter.updateDevices(updatedDevices)

                        Toast.makeText(this@MainActivity, "Brilho de ${device.name} ajustado para $brightness%", Toast.LENGTH_SHORT).show()
                    } else {
                        // Reverter a mudança no UI
                        ewelinkAdapter.updateDevices(ewelinkDevices)
                        val errorBody = response.errorBody()?.string()
                        Log.e("EWE_MAIN", "Erro ao ajustar brilho: ${response.code()} - $errorBody")
                        Toast.makeText(this@MainActivity, "Erro ao ajustar brilho: ${response.code()}", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE_MAIN", "Erro ao ajustar brilho", e)
                withContext(Dispatchers.Main) {
                    // Reverter a mudança no UI
                    ewelinkAdapter.updateDevices(ewelinkDevices)
                    Toast.makeText(this@MainActivity, "Erro: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }


    // ATUALIZE a função atualizarContadorDispositivos para incluir eWeLink
    private fun atualizarContadorDispositivos() {
        runOnUiThread {
            val totalDevices = deviceList.size + ewelinkDeviceCount
            deviceCountView.text = totalDevices.toString()
        }
    }



    // ADICIONE esta função utilitária (se não existir)
    private fun generateNonce(length: Int): String {
        val allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        return (1..length).map { allowedChars.random() }.joinToString("")
    }


    // NOVA FUNÇÃO: Controlar dispositivo eWeLink por voz
    private fun controlarDispositivoEwelink(comando: String): Boolean {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        Log.d("EWE", "entrou controlarDispositivoEwelink")

        // Verificar credenciais do backend
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Log.d("EWE", "Credenciais do backend não encontradas")
            return false
        }

        // Verificar se temos dispositivos carregados
        if (ewelinkDevices.isEmpty()) {
            Log.d("EWE", "Nenhum dispositivo eWeLink carregado")
            return false
        }

        // Verificar se o comando parece ser um comando de dispositivo
        // Se não parecer, retornar false para que a Super IA seja chamada
        val comandoLower = comando.lowercase().trim()
        val palavrasDispositivo = listOf(
            "ligar", "desligar", "acender", "apagar", "luz", "lampada", "lâmpada",
            "liga", "desliga", "acende", "apaga", "ligue", "desligue", "acenda", "apague",
            "interruptor", "tomada", "dispositivo"
        )
        
        val pareceComandoDispositivo = palavrasDispositivo.any { comandoLower.contains(it) }
        if (!pareceComandoDispositivo) {
            Log.d("EWE", "Comando não parece ser de dispositivo: $comando")
            return false
        }

        // Mostrar que está processando o comando
        Log.d("EWE", "Comando enviado para eWeLink: $comando")

        ewelinkVoiceControl.controlarDispositivoPorComandoAsync(comando) { resultado: String ->
            runOnUiThread {
                processarResultadoDispositivoEwelink(resultado, comando)
            }
        }

        return true
    }

    // Método para controlar dispositivos ESP via comando de voz
    private suspend fun controlarDispositivoEsp(comando: String): Boolean {
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        Log.d("ESP_VOICE", "🔍 Verificando comando ESP: $comando")
        
        // Verificar credenciais
        if (token.isNullOrEmpty() || apiKey.isNullOrEmpty()) {
            Log.d("ESP_VOICE", "Credenciais não encontradas")
            return false
        }
        
        // Buscar dispositivos ESP se o cache estiver vazio
        if (dispositivosEsp.isEmpty()) {
            try {
                val retrofit = ApiClient.getClient(this)
                val api = retrofit.create(DispositivosEspApi::class.java)
                val response = api.listarDispositivos()
                
                if (response.isSuccessful && response.body() != null) {
                    dispositivosEsp = response.body()!!
                    Log.d("ESP_VOICE", "✅ ${dispositivosEsp.size} dispositivos ESP carregados")
                } else {
                    Log.d("ESP_VOICE", "❌ Erro ao carregar dispositivos ESP: ${response.code()}")
                    return false
                }
            } catch (e: Exception) {
                Log.e("ESP_VOICE", "Erro ao buscar dispositivos ESP", e)
                return false
            }
        }
        
        // Limpar e normalizar o comando para comparação
        val comandoLimpo = cleanText(comando.lowercase().trim())
        
        // Procurar dispositivo ESP com comando correspondente
        val dispositivoEncontrado = dispositivosEsp.firstOrNull { dispositivo ->
            dispositivo.comando?.let { comandoDispositivo ->
                val comandoDispositivoLimpo = cleanText(comandoDispositivo.lowercase().trim())
                // Verificar se o comando contém o comando do dispositivo ou vice-versa
                comandoLimpo.contains(comandoDispositivoLimpo) || 
                comandoDispositivoLimpo.contains(comandoLimpo) ||
                comandoLimpo == comandoDispositivoLimpo
            } ?: false
        }
        
        if (dispositivoEncontrado == null) {
            Log.d("ESP_VOICE", "❌ Nenhum dispositivo ESP encontrado para o comando: $comando")
            return false
        }
        
        Log.d("ESP_VOICE", "✅ Dispositivo ESP encontrado: ${dispositivoEncontrado.nome} - Comando: ${dispositivoEncontrado.comando}")
        
        // Usar o comando de voz do dispositivo (igual ao botão "Enviar")
        // O backend fará a conversão para comandToEsp internamente
        val comandoParaEnviar = dispositivoEncontrado.comando ?: comando
        
        Log.d("ESP_VOICE", "📤 Enviando comando: $comandoParaEnviar")
        
        // Enviar comando via API (igual ao botão "Enviar")
        try {
            val retrofit = ApiClient.getClient(this)
            val api = retrofit.create(DispositivosEspApi::class.java)
            val request = EnviarComandoRequest(comandoParaEnviar)
            val response = api.enviarComando(request)
            
            if (response.isSuccessful) {
                Log.d("ESP_VOICE", "✅ Comando ESP enviado com sucesso para: ${dispositivoEncontrado.nome}")
                // A resposta será recebida via WebSocket e falada automaticamente
                return true
            } else {
                val errorBody = response.errorBody()?.string()
                Log.e("ESP_VOICE", "❌ Erro ao enviar comando ESP: ${response.code()} - $errorBody")
                return false
            }
        } catch (e: Exception) {
            Log.e("ESP_VOICE", "❌ Erro ao enviar comando ESP", e)
            return false
        }
    }

    // Função auxiliar para enviar resposta via SignalR Hub com prefixo "toSoft:"
    private fun enviarRespostaWebSocket(resposta: String) {
        try {
            val mensagemComPrefixo = "toSoft:$resposta"
            
            // Enviar via SignalR Hub usando método "EnviarMensagemToSoft" 
            // que envia diretamente para o grupo "type_software"
            if (espHubConnection != null) {
                try {
                    // Verificar se está conectado
                    val connectionState = espHubConnection?.connectionState
                    if (connectionState == com.microsoft.signalr.HubConnectionState.CONNECTED) {
                        // Enviar via método "EnviarMensagemToSoft" que envia para grupo "type_software"
                        espHubConnection?.invoke("EnviarMensagemToSoft", mensagemComPrefixo)
                        Log.d("WebSocket", "✅ Resposta enviada via SignalR Hub (ToSoft): $mensagemComPrefixo")
                    } else {
                        Log.d("WebSocket", "⚠️ HubConnection não está conectado. Estado: $connectionState")
                        // Tentar reconectar se não estiver conectado
                        if (connectionState == com.microsoft.signalr.HubConnectionState.DISCONNECTED) {
                            connectEspWebSocketHub()
                        }
                    }
                } catch (e: Exception) {
                    Log.e("WebSocket", "Erro ao enviar via SignalR Hub", e)
                }
            } else {
                Log.d("WebSocket", "⚠️ HubConnection não inicializado")
            }
        } catch (e: Exception) {
            Log.e("WebSocket", "Erro ao enviar resposta via WebSocket", e)
        }
    }

    // Conectar ao WebSocket Hub de dispositivos ESP para receber respostas
    private fun connectEspWebSocketHub() {
        val token = sessionManager.fetchAuthToken() ?: return
        val userId = sessionManager.fetchUserId() ?: return

        try {
            espHubConnection = HubConnectionBuilder.create("${ApiConfig.webBaseUrl}/hubs/dispositivo-esp?type=app")
                .withAccessTokenProvider(Single.defer { Single.just(token) })
                .build()

            // Listener para receber respostas dos dispositivos ESP
            espHubConnection?.on("RespostaDispositivo", { data: Any ->
                try {
                    Log.d("ESP_HUB_MAIN", "Resposta recebida (raw): $data")
                    
                    // Processar como Map (LinkedTreeMap do SignalR)
                    val resposta: String = when (data) {
                        is Map<*, *> -> {
                            // Extrair a resposta do Map
                            val respostaRaw = data["resposta"]?.toString() ?: ""
                            
                            // Verificar se contém "toApp:" e remover o prefixo
                            if (respostaRaw.startsWith("toApp:")) {
                                respostaRaw.substringAfter("toApp:").trim()
                            } else {
                                respostaRaw
                            }
                        }
                        is String -> {
                            // Se for string, verificar se contém "toApp:"
                            if (data.startsWith("toApp:")) {
                                data.substringAfter("toApp:").trim()
                            } else {
                                data
                            }
                        }
                        else -> {
                            val str = data.toString()
                            if (str.startsWith("toApp:")) {
                                str.substringAfter("toApp:").trim()
                            } else {
                                str
                            }
                        }
                    }
                    
                    Log.d("ESP_HUB_MAIN", "Resposta processada: $resposta")
                    
                    // Filtrar mensagens "toSoft:" - essas são para o software, não para o app
                    if (resposta.startsWith("toSoft:", ignoreCase = true)) {
                        Log.d("ESP_HUB_MAIN", "⚠️ Mensagem 'toSoft:' ignorada (destinada ao software)")
                        return@on
                    }
                    
                    // Falar apenas a resposta (sem prefixos)
                    if (resposta.isNotEmpty()) {
                        runOnUiThread {
                            speakTextFromService(resposta)
                        }
                    }
                } catch (e: Exception) {
                    Log.e("ESP_HUB_MAIN", "Erro ao processar resposta", e)
                }
            }, Any::class.java)

            espHubConnection?.start()?.blockingAwait()
            Log.d("ESP_HUB_MAIN", "✅ Conectado ao DispositivoESP Hub na MainActivity")
            
            // Identificar cliente
            espHubConnection?.invoke("IdentificarCliente", "app", userId)
        } catch (e: Exception) {
            Log.e("ESP_HUB_MAIN", "Erro ao conectar ao Hub de dispositivos ESP", e)
        }
    }

    // NOVA FUNÇÃO: Atualizar status local do dispositivo após comando
    private fun atualizarStatusLocalDispositivo(dispositivoName: String, novoStatus: String) {
        val updatedDevices = ewelinkDevices.map { dispositivo ->
            if (dispositivo.name == dispositivoName) {
                val newParams = JSONObject(dispositivo.params.toString())
                newParams.put("switch", novoStatus)
                dispositivo.copy(params = newParams)
            } else {
                dispositivo
            }
        }

        ewelinkDevices = updatedDevices
        ewelinkAdapter.updateDevices(updatedDevices)
        ewelinkVoiceControl.setDispositivos(updatedDevices)

        Log.d("EWE_STATUS_UPDATE", "🔄 Status atualizado: $dispositivoName -> $novoStatus")
    }

    // NOVA FUNÇÃO: Processar resultado do comando do dispositivo eWeLink - VERSÃO CORRIGIDA
    private fun processarResultadoDispositivoEwelink(resultado: String, comandoOriginal: String) {
        Log.d("EWE_VOICE", "📨 Resultado eWeLink: $resultado")

        when {
            resultado.contains("dispositivoName:") && resultado.contains("acaoExecutada:sim") -> {
                // Extrair informações do resultado
                val dispositivoName = resultado.substringAfter("dispositivoName:").substringBefore(" acao:")
                val acao = resultado.substringAfter("acao:").substringBefore(" status:")
                val status = resultado.substringAfter("status:").substringBefore(" acaoExecutada:")

                // 🔥 ATUALIZAR STATUS LOCAL
                val novoStatus = when (acao) {
                    "ligar" -> "on"
                    "desligar" -> "off"
                    else -> status
                }
                atualizarStatusLocalDispositivo(dispositivoName, novoStatus)

                var feminino = false
                if(comandoOriginal.contains("luz") || comandoOriginal.contains("lampada") ||
                    comandoOriginal.contains("cafeteira") || comandoOriginal.contains("torradeira") ||
                    comandoOriginal.contains("chapa") || comandoOriginal.contains("geladeira") ||
                    comandoOriginal.contains("tomada") || comandoOriginal.contains("acende a ") ||
                    comandoOriginal.contains("acenda a ") || comandoOriginal.contains("acender a ") ||
                    comandoOriginal.contains("ligar a ") || comandoOriginal.contains("liga a ") ||
                    comandoOriginal.contains("ligue a ") || comandoOriginal.contains("apagar a ") ||
                    comandoOriginal.contains("apaga a ") || comandoOriginal.contains("apague a ") ||
                    comandoOriginal.contains("desligar a ") || comandoOriginal.contains("desliga a ") ||
                    comandoOriginal.contains("desligue a ")) {
                    feminino = true
                }

                Log.d("EWE_VOICE_TEST_ACAO", "🎯 Ação: $acao | dispositivo: $dispositivoName | Status: $status")

                var mensagem = when (acao) {
                    "ligar" -> "Liguei o $dispositivoName"
                    "desligar" -> "Desliguei o $dispositivoName"
                    "ajustar_brilho" -> {
                        val brilho = resultado.substringAfter("brilho:").substringBefore(" acaoExecutada:")
                        "Ajustei o brilho do $dispositivoName para $brilho%"
                    }
                    else -> "Ação executada no $dispositivoName"
                }

                if (feminino) {
                    mensagem = when (acao) {
                        "ligar" -> "Liguei a $dispositivoName"
                        "desligar" -> "Desliguei a $dispositivoName"
                        "ajustar_brilho" -> {
                            val brilho = resultado.substringAfter("brilho:").substringBefore(" acaoExecutada:")
                            "Ajustei o brilho da $dispositivoName para $brilho%"
                        }
                        else -> "Ação executada na $dispositivoName"
                    }
                }

                //tvSpeechText.text = mensagem
                speakTextFromService(mensagem)
                
                // Enviar resposta via WebSocket com prefixo "toSoft:"
                enviarRespostaWebSocket(mensagem)
            }

            // 🔥 CASO: Dispositivo já está no estado desejado
            resultado.contains("ja_estado:") -> {
                val dispositivoName = resultado.substringAfter("dispositivoName:").substringBefore(" acao:")
                val acao = resultado.substringAfter("acao:").substringBefore(" status:")
                val status = resultado.substringAfter("status:").substringBefore(" acaoExecutada:")

                var feminino = false
                if(comandoOriginal.contains("luz") || comandoOriginal.contains("lampada") ||
                    comandoOriginal.contains("cafeteira") || comandoOriginal.contains("torradeira") ||
                    comandoOriginal.contains("chapa") || comandoOriginal.contains("geladeira") ||
                    comandoOriginal.contains("tomada") || comandoOriginal.contains("acende a ") ||
                    comandoOriginal.contains("acenda a ") || comandoOriginal.contains("acender a ") ||
                    comandoOriginal.contains("ligar a ") || comandoOriginal.contains("liga a ") ||
                    comandoOriginal.contains("ligue a ") || comandoOriginal.contains("apagar a ") ||
                    comandoOriginal.contains("apaga a ") || comandoOriginal.contains("apague a ") ||
                    comandoOriginal.contains("desligar a ") || comandoOriginal.contains("desliga a ") ||
                    comandoOriginal.contains("desligue a ")) {
                    feminino = true
                }

                var mensagem = when (acao) {
                    "ligar" -> "O $dispositivoName já estava ligado"
                    "desligar" -> "O $dispositivoName já estava desligado"
                    else -> "O $dispositivoName já estava no estado desejado"
                }

                if (feminino) {
                    mensagem = when (acao) {
                        "ligar" -> "A $dispositivoName já estava ligada"
                        "desligar" -> "A $dispositivoName já estava desligada"
                        else -> "A $dispositivoName já estava no estado desejado"
                    }
                }

                Log.d("EWE_VOICE_TEST_ACAO", "ℹ️ Status atual: $mensagem")
                //tvSpeechText.text = mensagem
                speakTextFromService(mensagem)
                
                // Enviar resposta via WebSocket com prefixo "toSoft:"
                enviarRespostaWebSocket(mensagem)
            }

            resultado.startsWith("erro:") -> {
                val mensagemErro = resultado.substringAfter("erro:")
                Log.e("EWE_VOICE", "❌ Erro no comando eWeLink: $mensagemErro")

                // Se a IA está no modo FULL e não encontrou dispositivo, chamar Super IA
                if (switchIa.isChecked && iaativa.get()) {
                    Log.d("EWE_VOICE", "IA no modo FULL - Chamando Super IA para: $comandoOriginal")
                    lifecycleScope.launch {
                        chamarIaSuper(comandoOriginal, true)
                    }
                    return@processarResultadoDispositivoEwelink
                }

                // 🔥 CORREÇÃO: Feedback de voz para erros
                val mensagemUsuario = when {
                    mensagemErro.contains("offline") -> "O dispositivo está offline e não pode ser controlado"
                    mensagemErro.contains("não responde") -> "O dispositivo não está respondendo"
                    mensagemErro.contains("4002") -> "Erro de comunicação com o dispositivo"
                    else -> "Erro ao controlar o dispositivo"
                }

                //tvSpeechText.text = mensagemUsuario
                if(!mensagemUsuario.contains("Erro"))
                    speakTextFromService(mensagemUsuario)
            }
            else -> {
                Log.d("EWE_VOICE", "⚠️ Comando não executado no eWeLink")
                
                // Se a IA está no modo FULL e não executou nada, chamar Super IA
                if (switchIa.isChecked && iaativa.get()) {
                    Log.d("EWE_VOICE", "IA no modo FULL - Chamando Super IA para: $comandoOriginal")
                    lifecycleScope.launch {
                        chamarIaSuper(comandoOriginal, true)
                    }
                }
            }
        }
    }



    /////////////////////////////////////////////////////

    // Pré-carregar dispositivos quando o app inicia
    // ATUALIZE a função preCarregarDispositivos para incluir eWeLink
    fun preCarregarDispositivos() {
        Log.d("Preload", "🔄 Iniciando pré-carregamento de dispositivos eWeLink")

        // Pré-carrega eWeLink via backend se o usuário estiver logado
        val token = sessionManager.fetchAuthToken()
        val apiKey = sessionManager.fetchApiKey()
        
        if (!token.isNullOrEmpty() && !apiKey.isNullOrEmpty()) {
            // Usar a API do backend para pré-carregar
            lifecycleScope.launch(Dispatchers.IO) {
                try {
                    val retrofit = ApiClient.getClient(this@MainActivity)
                    val ewelinkApi = retrofit.create(com.starkaid.starkaidapp.services.EwelinkApi::class.java)
                    
                    // Verificar status primeiro
                    val statusResponse = ewelinkApi.getStatus()
                    
                    withContext(Dispatchers.Main) {
                        if (statusResponse.isSuccessful && statusResponse.body() != null) {
                            val status = statusResponse.body()!!
                            if (status.isLoggedIn) {
                                Log.d("Preload", "✅ Usuário conectado no backend - Pré-carregando dispositivos...")
                                // Carregar dispositivos via backend (silencioso, sem mostrar erros)
                                carregarDispositivosEwelinkDaApi(showErrors = false)
                            } else {
                                Log.d("Preload", "⚠️ Usuário não conectado no backend - Tentando API direta...")
                                // Fallback para API direta se não estiver logado no backend
                                preCarregarDispositivosViaApiDireta()
                            }
                        } else {
                            Log.d("Preload", "⚠️ Erro ao verificar status - Tentando API direta...")
                            // Fallback para API direta
                            preCarregarDispositivosViaApiDireta()
                        }
                    }
                } catch (e: Exception) {
                    Log.e("Preload", "❌ Erro ao pré-carregar via backend - Tentando API direta", e)
                    // Fallback para API direta em caso de erro
                    preCarregarDispositivosViaApiDireta()
                }
            }
        } else {
            Log.d("Preload", "⚠️ Credenciais não encontradas - Tentando API direta...")
            // Fallback para API direta se não tiver credenciais do backend
            preCarregarDispositivosViaApiDireta()
        }
    }
    
    // Função auxiliar para pré-carregar via API direta do eWeLink (fallback)
    private fun preCarregarDispositivosViaApiDireta() {
        val secureStorage = SecureStorageManager(this)
        val tokens = secureStorage.getEwelinkTokens()
        if (tokens != null) {
            obterFamiliaAtualEwelink(tokens.accessToken, tokens.region) { familyId ->
                ewelinkDeviceService.listarDispositivos(familyId,
                    onSuccess = { dispositivos ->
                        runOnUiThread {
                            Log.d("Preload", "✅ ${dispositivos.size} dispositivos eWeLink pré-carregados via API direta")
                            ewelinkDevices = dispositivos
                            ewelinkAdapter.updateDevices(dispositivos)
                            ewelinkDeviceCount = dispositivos.size
                            atualizarContadorDispositivos()

                            // 🔥 ATUALIZAR O CONTROLE DE VOZ
                            ewelinkVoiceControl.setDispositivos(dispositivos)
                        }
                    },
                    onError = { error ->
                        Log.e("Preload", "❌ Erro ao pré-carregar eWeLink via API direta: $error")
                    }
                )
            }
        } else {
            Log.d("Preload", "❌ Tokens eWeLink não encontrados")
        }
    }




    private fun toggleDevicesSection() {
        isDevicesExpanded = !isDevicesExpanded

        Log.d("STARKSWITCH_UI", "Toggle StarkSwitch - Expandido: $isDevicesExpanded, ItemCount: ${deviceAdapter.itemCount}")

        TransitionManager.beginDelayedTransition(
            rvDevices.parent as ViewGroup,
            AutoTransition().apply { duration = 300 }
        )

        if (isDevicesExpanded) {
            // Mantém o texto original e apenas altera o ícone da seta
            tvExpandDevices.text = devicesOriginalText
            tvExpandDevices.setCompoundDrawablesRelativeWithIntrinsicBounds(
                null, null, 
                ContextCompat.getDrawable(this, R.drawable.ic_arrow_down), 
                null
            )
            rvDevices.visibility = View.VISIBLE
            rvDevices.layoutParams.height = ViewGroup.LayoutParams.WRAP_CONTENT

            // Verificar se precisa carregar dispositivos
            if (deviceAdapter.itemCount == 0) {
                Log.d("STARKSWITCH_UI", "Carregando dispositivos StarkSwitch")
                loadDevices()
            }
        } else {
            // Mantém o texto original e apenas altera o ícone da seta
            tvExpandDevices.text = devicesOriginalText
            tvExpandDevices.setCompoundDrawablesRelativeWithIntrinsicBounds(
                null, null, 
                ContextCompat.getDrawable(this, R.drawable.ic_arrow_right), 
                null
            )
            rvDevices.visibility = View.GONE
            rvDevices.layoutParams.height = 0
        }
    }

    @SuppressLint("NotifyDataSetChanged")
    private fun loadDevices() {

        if (!isOnline()) {
            Log.d("Network", "Carregando dispositivos offline")
            // Offline: usa dados locais se disponíveis
            val localDevices = loadDevicesFromLocal()
            if (localDevices.isNotEmpty()) {
                deviceList.clear()
                deviceList.addAll(localDevices)
                deviceAdapter.notifyDataSetChanged()
                val totalDevices = deviceList.size
                deviceCountView.text = totalDevices.toString()

                runOnUiThread {
                    Toast.makeText(this@MainActivity,
                        "Modo offline: ${deviceList.size} dispositivos carregados localmente",
                        Toast.LENGTH_SHORT).show()
                }
            } else {
                runOnUiThread {
                    Toast.makeText(this@MainActivity,
                        "Nenhum dispositivo encontrado localmente. Conecte-se à internet para atualizar.",
                        Toast.LENGTH_LONG).show()
                }
            }
            return
        }

        // Restante do código original para online...
        val role = sessionManager.fetchUserRole()

        if (role != null) {
            if (role == "UserNivel3") {
                AlertDialog.Builder(this)
                    .setTitle("Atenção")
                    .setMessage("Seu pagamento esta atrasado! \nregularize-o para desbloquear os serviços do StarkAid nivel 2!")
                    .setPositiveButton("OK", null)
                    .show()
            }

            lifecycleScope.launch(Dispatchers.IO) {
                try {
                    Log.d("SignalR", "Entrou na CoroutineScope")
                    val retrofit = ApiClient.getClient(this@MainActivity)
                    val api = retrofit.create(DeviceApi::class.java)
                    val response = api.getDevices()

                    Log.d("SignalR", "Response headers: ${response.headers()}")
                    Log.d("SignalR", "Response body: ${response.body()}")
                    Log.d("SignalR", "Response message: ${response.message()}")
                    Log.d("SignalR", "Response error body: ${response.errorBody()?.string()}")

                    if (response.isSuccessful) {
                        response.body()?.let { deviceResponses ->
                            deviceList.clear()
                            // val fake = DeviceResponse(...) // Removed fake or update if needed

                            val devices: List<Device> = deviceResponses.map { response ->
                                Device(
                                    id = response.id,
                                    deviceId = response.deviceId ?: response.id,
                                    name = response.name,
                                    type = response.type ?: "Switch",
                                    online = response.online,
                                    isOn = response.isOn,
                                    familyId = response.familyId,
                                    roomId = response.roomId,
                                    apiKey = response.apiKey,
                                    userId = response.userId,
                                    mqttTopic = response.mqttTopic,
                                    comando = response.comando
                                )
                            }

                            deviceList.addAll(devices)


                            withContext(Dispatchers.Main) {
                                deviceAdapter.notifyDataSetChanged()
                                // ATUALIZAR CONTADOR DE DISPOSITIVOS
                                deviceCountView.text = deviceList.size.toString()
                            }



                            devices.forEach { device ->
                                checkAndUpdateDeviceStatus(device)
                            }

                            saveDevicesLocally(devices)
                        }
                    } else {
                        Log.e("SignalR", "Erro ao carregar dispositivos: ${response.code()}")
                        withContext(Dispatchers.Main) {
                            Toast.makeText(this@MainActivity, "Erro ao carregar dispositivos. Tente mais tarde.", Toast.LENGTH_SHORT).show()
                        }
                    }
                } catch (e: Exception) {
                    Log.e("SignalR", "Exceção: ${e.message}")
                    withContext(Dispatchers.Main) {
                        Toast.makeText(this@MainActivity, "Dispositivos não carregados. Tente mais tarde.", Toast.LENGTH_SHORT).show()
                    }
                }
                }
            }
        }


    private fun saveDevicesLocally(devices: List<Device>) {
        try {
            val sharedPrefs = getSharedPreferences("device_cache", MODE_PRIVATE)
            sharedPrefs.edit {
                val devicesJson = Gson().toJson(devices)
                putString("devices", devicesJson)
            }
        } catch (e: Exception) {
            Log.e("MainActivity", "Erro ao salvar dispositivos localmente", e)
        }
    }


    private fun loadDevicesFromLocal(): List<Device> {
        return try {
            // Recupera dispositivos do SharedPreferences ou Room
            val sharedPrefs = getSharedPreferences("device_cache", MODE_PRIVATE)
            val devicesJson = sharedPrefs.getString("devices", "[]")
            val type = object : TypeToken<List<Device>>() {}.type
            Gson().fromJson(devicesJson, type) ?: emptyList()
        } catch (e: Exception) {
            Log.e("MainActivity", "Erro ao carregar dispositivos locais", e)
            emptyList()
        }
    }

    private fun setupRecyclerView() {
        // Configurar seu adapter e layout manager aqui
        deviceAdapter = DeviceAdapter(deviceList, this)
        rvDevices.adapter = deviceAdapter

        val adapter = deviceAdapter // Seu adapter personalizado
        rvDevices.adapter = adapter
        rvDevices.layoutManager = GridLayoutManager(this, 2)

        // Adicionar divisórias entre os itens (opcional)
        rvDevices.addItemDecoration(
            DividerItemDecoration(this, GridLayoutManager.VERTICAL)
        )
        rvDevices.addItemDecoration(
            DividerItemDecoration(this, GridLayoutManager.HORIZONTAL)
        )
    }

    private fun cleanCache() {
        Toast.makeText(this, "Limpando cache...", Toast.LENGTH_SHORT).show()

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val cacheDirs = listOf(cacheDir, externalCacheDir).filterNotNull()
                var deletedFiles = 0
                var totalSize = 0L

                cacheDirs.forEach { dir ->
                    if (dir.exists() && dir.isDirectory) {
                        dir.walkBottomUp().forEach { file ->
                            if (file.isFile) {
                                totalSize += file.length()
                            }
                            if (file.delete()) {
                                deletedFiles++
                            }
                        }
                    }
                }

                val resultMessage = if (deletedFiles > 0) {
                    val sizeMB = totalSize.toDouble() / (1024 * 1024)
                    "Cache limpo: $deletedFiles arquivos, ${"%.2f".format(sizeMB)} MB liberados"
                } else {
                    "Nenhum arquivo de cache encontrado"
                }

                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, resultMessage, Toast.LENGTH_LONG).show()
                    speakTextFromService(resultMessage)
                }

            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, "Erro ao limpar cache: ${e.message}", Toast.LENGTH_SHORT).show()
                    speakTextFromService("Falha ao limpar cache")
                    Log.e("MainActivity", "Erro ao limpar cache", e)
                }
            }
        }
    }


    private fun obterDadosUser(){
        val userId = sessionManager.fetchUserId()
        val authToken = sessionManager.fetchAuthToken()

        CoroutineScope(Dispatchers.IO).launch {
            val response = usuarioApi.obterUsuario(userId.toString())
            if (response.isSuccessful) {
                val usuario = response.body()!!
                runOnUiThread {
                    sessionManager.saveUserEmail(usuario.email)
                    sessionManager.saveUserName(usuario.name)
                }
            }
        }
    }


    fun searchContato(query: String, message: String): Boolean {
        val termo = query.trim().lowercase()
        Log.d("WhatsappLog", "Termo de busca: $termo")

        if (termo.isEmpty()) {
            speakTextFromService("Por favor, diga o nome do contato para buscar.")
            return false
        }

        for (contato in contatosCache){
            Log.d("WhatsappLog", "Contato: ${contato.nome} - ${contato.numero}")
        }
        // ✅ Filtra todos os contatos que contenham o termo informado
        val resultados = contatosCache.filter {
            it.nome.lowercase().contains(termo)
        }

        when {
            resultados.isEmpty() -> {
                // ❌ Nenhum contato encontrado
                speakTextFromService("Não encontrei nenhum contato chamado $query.")
                Log.d("WhatsappLog", "Contato não encontrado: $query")
                return false
            }

            resultados.size > 1 -> {
                // ⚠️ Vários contatos encontrados — não enviar mensagem
                val qtd = resultados.size
                speakTextFromService("Você tem $qtd contatos com o nome $query. Seja mais específico, por favor.")
                Log.d("WhatsappLog", "Múltiplos contatos encontrados para $query: ${resultados.map { it.nome }}")
                return false
            }

            else -> {
                // ✅ Apenas um contato encontrado — enviar mensagem
                val resultado = resultados.first()
                speakTextFromService("Número encontrado. ${resultado.nome}, posso enviar a mensagem para este contato?")

                confirmContato.set(true)
                contato = resultado.nome
                numero = resultado.numero
                messageenviar = message

                Log.d("WhatsappLog", "Encontrado: ${resultado.nome} - ${resultado.numero}")
                return true

            }
        }
    }

    private fun sendMessageWpp(nome: String, numero: String, message: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val userId = sessionManager.fetchUserId()
                val token = sessionManager.fetchAuthToken()

                if (userId.isNullOrEmpty() || token.isNullOrEmpty()) {
                    Log.e("WhatsAppSession", "Token ou userId ausente.")
                    withContext(Dispatchers.Main) {
                        speakTextFromService("Erro: sessão inválida. Faça login novamente.")
                    }
                    return@launch
                }

                val retrofit = ApiClient.getClient(this@MainActivity)
                val whatsappApi = retrofit.create(WhatsappApi::class.java)

                // Remove caracteres não numéricos e garante DDI
                val numeroLimpo = numero.replace(Regex("[^\\d]"), "")
                val numeroFinal = if (numeroLimpo.startsWith("55")) numeroLimpo else "55$numeroLimpo"

                val message = "mensagem para $nome $message"
                val body = EnviarMensagemRequest(
                    userId = userId,
                    sessionName = userId,
                    phoneNumber = numeroFinal,
                    message = message,
                    isGroup = false,
                    isNewsletter = false,
                    isLid = false
                )

                val response = whatsappApi.enviarMensagem(body, "Bearer $token")

                if (response.isSuccessful && response.body() != null) {
                    val resp = response.body()!!
                    Log.d("sendMessageWpp", "Mensagem enviada com sucesso: ${resp.status}")

                    withContext(Dispatchers.Main) {
                        if (resp.status == "success") {
                            speakTextFromService("Mensagem enviada para $nome.")
                        } else {
                            speakTextFromService("Não foi possível enviar a mensagem.")
                        }
                    }
                } else {
                    Log.e("sendMessageWpp", "Falha HTTP: ${response.code()} - ${response.errorBody()?.string()}")
                    withContext(Dispatchers.Main) {
                        speakTextFromService("Erro ao enviar mensagem. Código ${response.code()}.")
                    }
                }
            } catch (e: Exception) {
                Log.e("sendMessageWpp", "Erro ao enviar mensagem", e)
                withContext(Dispatchers.Main) {
                    speakTextFromService("Falha ao enviar mensagem. Verifique sua conexão.")
                }
            }
        }
    }

    private fun getEntitiesFromText(text: String, callback: (List<String>?) -> Unit) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val texto = cleanText(text)
                val retrofit = ApiClient.getClient(this@MainActivity)
                val nlpService = retrofit.create(NlpApi::class.java)

                val token = sessionManager.fetchAuthToken()
                val userId = sessionManager.fetchUserId()

                if (token.isNullOrEmpty() || userId.isNullOrEmpty()) {
                    Log.e("WhatsAppSession", "Token ou userId ausente.")
                    withContext(Dispatchers.Main) {
                        callback(null)
                    }
                    return@launch
                }

                val request = NlpExtractRequest(texto)
                val response = nlpService.extractEntities(
                    userId = userId,
                    token = "Bearer $token",
                    body = request
                )

                if (response.isSuccessful) {
                    val entities = response.body()?.entities?.get("PER")
                    Log.d("WhatsAppSession", "Entidades encontradas: $entities")
                    withContext(Dispatchers.Main) {
                        callback(entities)
                    }
                } else {
                    Log.e("WhatsAppSession", "Erro HTTP ${response.code()}: ${response.errorBody()?.string()}")
                    withContext(Dispatchers.Main) {
                        callback(null)
                    }
                }

            } catch (e: Exception) {
                e.printStackTrace()
                Log.e("WhatsAppSession", "Erro ao extrair entidades", e)
                withContext(Dispatchers.Main) {
                    callback(null)
                }
            }
        }
    }

    private fun statusSessionGet() {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val userId = sessionManager.fetchUserId()
                val token = sessionManager.fetchAuthToken()

                if (userId.isNullOrEmpty() || token.isNullOrEmpty()) {
                    Log.e("WhatsAppSession", "Token ou userId ausente.")
                    withContext(Dispatchers.Main) {
                        speakTextFromService("Erro: sessão inválida. Faça login novamente.")
                    }
                    return@launch
                }

                val retrofit = ApiClient.getClient(this@MainActivity)
                val whatsappApi = retrofit.create(WhatsappApi::class.java)

                val body = StatusSessaoRequest(
                    userId = userId,
                    sessionName = userId
                )

                val response = whatsappApi.statusSessao(body, "Bearer $token")

                if (response.isSuccessful) {
                    val bodyResponse = response.body()

                    if (bodyResponse == null) {
                        Log.w("WhatsAppSession", "Resposta vazia do servidor. Criando nova sessão.")
                        criarSessaoWhatsapp()
                        return@launch
                    }

                    Log.d("WhatsAppSession", "Status da sessão: ${bodyResponse.status}")

                    when (bodyResponse.status.uppercase()) {
                        "CONNECTED" -> {
                            Log.i("WhatsAppSession", "Sessão conectada com sucesso.")
                            withContext(Dispatchers.Main) {
                                speakTextFromService("Sessão WhatsApp conectada com sucesso.")
                            }
                        }
                        "QRCODE" -> {
                            Log.w("WhatsAppSession", "Sessão aguardando QR Code.")
                            if (!bodyResponse.qrCode.isNullOrEmpty()) {
                                withContext(Dispatchers.Main) {
                                    val intent = Intent(this@MainActivity, QrActivityWppConnect::class.java)
                                    startActivity(intent)
                                }
                            } else {
                                Log.w("WhatsAppSession", "Sessão não conectada (${bodyResponse.status}). Criando nova sessão.")
                                withContext(Dispatchers.Main) {
                                    val intent = Intent(this@MainActivity, QrActivityWppConnect::class.java)
                                    startActivity(intent)
                                }
                            }
                        }
                        else -> {
                            Log.w("WhatsAppSession", "Sessão não conectada (${bodyResponse.status}). Criando nova sessão.")
                            withContext(Dispatchers.Main) {
                                val intent = Intent(this@MainActivity, QrActivityWppConnect::class.java)
                                startActivity(intent)
                            }
                        }
                    }
                } else {
                    Log.e("WhatsAppSession", "Erro HTTP ${response.code()}: ${response.errorBody()?.string()}")

                    withContext(Dispatchers.Main) {
                        val intent = Intent(this@MainActivity, QrActivityWppConnect::class.java)
                        startActivity(intent)
                    }
                }

            } catch (e: Exception) {
                Log.e("WhatsAppSession", "Erro ao buscar status da sessão", e)
                withContext(Dispatchers.Main) {
                    switchWhatsapp.isChecked = false
                    speakTextFromService("Erro ao consultar o status do WhatsApp.")
                }
            }
        }
    }

    private fun criarSessaoWhatsapp() {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val token = sessionManager.fetchAuthToken()
                val userId = sessionManager.fetchUserId()

                if (token.isNullOrEmpty() || userId.isNullOrEmpty()) return@launch

                val retrofit = ApiClient.getClient(this@MainActivity)
                val api = retrofit.create(WhatsappApi::class.java)

                val request = CriarSessaoRequest(
                    userId = userId,
                    sessionName = userId,
                    waitQrCode = true
                )

                val response = api.criarSessao(
                    body = request,
                    token = "Bearer $token"
                )

                if (response.isSuccessful) {
                    val body = response.body()
                    val qrBase64 = body?.qrcode
                    if (!qrBase64.isNullOrEmpty()) {
                        // converter Base64 para Bitmap
                        val decodedBytes = android.util.Base64.decode(qrBase64, android.util.Base64.DEFAULT)
                        val bitmap = BitmapFactory.decodeByteArray(decodedBytes, 0, decodedBytes.size)

                        withContext(Dispatchers.Main) {
                            // mostrar no ImageView
                            withContext(Dispatchers.Main) {
                                val intent = Intent(this@MainActivity, QrActivityWppConnect::class.java)
                                intent.putExtra("qrBase64", qrBase64)
                                startActivity(intent)
                            }
                        }
                    }
                } else {
                    Log.e("WhatsAppSession", "Erro HTTP ${response.code()}: ${response.errorBody()?.string()}")
                }

            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }


    private fun saveContactToBackend(nomeContato: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val api = retrofit.create(NlpApi::class.java)

                val request = AddNameRequest(full_name = nomeContato)
                val response = api.salvarContatosBackend(request)

                if (response.isSuccessful) {
                    val body = response.body()
                    Log.d("ContatoBackend", "Enviado: $nomeContato - Resposta: ${body?.message}")
                } else {
                    Log.e("ContatoBackend", "Erro HTTP ${response.code()}: ${response.errorBody()?.string()}")
                }

            } catch (e: Exception) {
                Log.e("ContatoBackend", "Falha ao enviar nome", e)
            }
        }
    }


    private fun getContactsFromList() {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val contatoDao = db.contatoDao()
                val contatos = contatoDao.getAll()

                contatosCache = contatos.sortedBy { it.nome.lowercase() }

                Log.d("Contatos", "Contatos carregados na memória: ${contatosCache.size}")
            } catch (e: Exception) {
                Log.e("Contatos", "Erro ao carregar contatos do banco local", e)
            }
        }
    }

    fun getStarkcoins() {
        val authToken = sessionManager.fetchAuthToken() ?: return

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val usersApi = retrofit.create(UsersApi::class.java)
                val response = usersApi.getCurrentUser()
                
                if (response.isSuccessful && response.body() != null) {
                    val user = response.body()!!
                    val economy = user.economy ?: EconomicPayload()
                    val saldo = economy.balance().toDouble()
                    starkCoins = saldo.toFloat()
                    saldoStarkcoinsInt = economy.balance()

                    runOnUiThread {
                        tvStarkcoins.text = String.format("%.0f SC", saldo)
                        Log.d("SaldoUI", "Saldo atualizado na UI: $saldo SC, saldoStarkcoinsInt: $saldoStarkcoinsInt")
                        updatePlanLimitsCard(economy)
                    }
                } else {
                    val errorBody = response.errorBody()?.string() ?: "Erro desconhecido"
                    Log.e("MainActivity", "Erro ao buscar saldo: ${response.code()} - $errorBody")
                runOnUiThread {
                        Toast.makeText(this@MainActivity, "Erro ao atualizar saldo (${response.code()})", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                Log.e("MainActivity", "Erro ao buscar saldo", e)
                runOnUiThread {
                    Toast.makeText(this@MainActivity, "Erro ao atualizar saldo: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    var adsReturn: AtomicBoolean = AtomicBoolean(false)
    private var isAdsRequestInProgress = AtomicBoolean(false)

    private fun adsGet() {
        // Previne múltiplas chamadas simultâneas
        if (!isAdsRequestInProgress.compareAndSet(false, true)) {
            Log.d("UnityAds", "Requisição de ads já em andamento, ignorando")
            return
        }
        lifecycleScope.launch {
            try {
                if (isOnline()) {
                    val retrofit = ApiClient.getClient(this@MainActivity)
                    val api = retrofit.create(UsersApi::class.java)

                    val response = api.getAds()
                    if (response.isSuccessful) {
                        val ads = response.body()?.adsAtiv ?: "Desativado"
                        // Define adsReturn com base no valor da resposta
                        adsReturn.set(ads == "Desativado") // true se "Desativado", false caso contrário
                        Log.d("UnityAds", "Status dos anúncios: $ads")
                    } else if (response.code() == 401) {
                        adsReturn.set(false)
                        Log.w("UnityAds", "Não autorizado para verificar anúncios")
                    }
                } else {
                    adsReturn.set(false)
                    Log.w("UnityAds", "Offline - não foi possível verificar anúncios")
                }
                
                // Verificar planos ativos - se tiver Nível 2 ativo, desabilitar anúncios
                verificarPlanosAtivosParaAds()
            } catch (e: Exception) {
                Log.e("UnityAds", "Falha ao verificar ads", e)
                adsReturn.set(false)
            } finally {
                // Sempre libere a flag no final
                isAdsRequestInProgress.set(false)
            }
        }
    }
    
    private fun verificarPlanosAtivosParaAds() {
        lifecycleScope.launch {
            try {
                if (isOnline()) {
                    val retrofit = ApiClient.getClient(this@MainActivity)
                    val assinaturasApi = retrofit.create(AssinaturasApi::class.java)
                    val response = assinaturasApi.listarAtivas()
                    
                    if (response.isSuccessful && response.body() != null) {
                        val planos = response.body()!!
                        // Verificar se existe plano Nível 2 ativo
                        val temPlanoNivel2 = planos.any { it.nivel == 2 && it.status.lowercase() == "ativa" }
                        
                        if (temPlanoNivel2) {
                            // Se tiver plano Nível 2 ativo, desabilitar anúncios
                            adsReturn.set(false)
                            Log.d("UnityAds", "Plano Nível 2 ativo detectado - anúncios desabilitados")
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("UnityAds", "Erro ao verificar planos ativos", e)
            }
        }
    }

    private fun logNetworkEnvironment() {
        // Proxy padrão do Android
        val host = android.net.Proxy.getDefaultHost()
        val port = android.net.Proxy.getDefaultPort()
        Log.i("NetworkEnv", "Default proxy: $host:$port")

        // Proxies via system properties (http/https)
        val httpProxy = "${System.getProperty("http.proxyHost")}:${System.getProperty("http.proxyPort")}"
        val httpsProxy = "${System.getProperty("https.proxyHost")}:${System.getProperty("https.proxyPort")}"
        Log.i("NetworkEnv", "System proxy http: $httpProxy, https: $httpsProxy")

        // Conectividade e VPN
        val cm = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        val active = cm.activeNetwork
        val caps = cm.getNetworkCapabilities(active)
        val transports = mutableListOf<String>()
        if (caps != null) {
            if (caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)) transports.add("WIFI")
            if (caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR)) transports.add("CELLULAR")
            if (caps.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET)) transports.add("ETHERNET")
            if (caps.hasTransport(NetworkCapabilities.TRANSPORT_VPN)) transports.add("VPN")
        }
        Log.i("NetworkEnv", "Active transports: ${transports.joinToString()}")
    }

    private fun initAds(context: Context) {
        try {
            // Verificar conectividade antes de inicializar
            if (!isOnline()) {
                Log.w("UnityAds", "Sem conexão - Ads não inicializados")
                return
            }

            // Inicializar Mobile Ads primeiro
            MobileAds.initialize(this) { initializationStatus ->
                Log.d("UnityAds", "Google Mobile Ads SDK inicializado")

                // Inicializar Unity Ads com tratamento de erro melhorado
                initializeUnityAds()
            }

        } catch (e: Exception) {
            Log.e("UnityAds", "Erro na inicialização de ads", e)
        }
    }

    private fun initializeUnityAds() {
        try {
            // Setar como false para producao
            val testMode = true //BuildConfig.DEBUG // Usar modo de teste em desenvolvimento

            UnityAds.initialize(applicationContext, UNITY_GAME_ID, testMode, object :
                IUnityAdsInitializationListener {
                override fun onInitializationComplete() {
                    Log.d("UnityAds", "Unity Ads inicializado - Test mode: $testMode")
                    // Agora podemos carregar anúncios
                    loadInterstitialAd()
                }

                override fun onInitializationFailed(error: UnityAds.UnityAdsInitializationError, message: String) {
                    Log.e("UnityAds", "Falha na inicialização: $message (erro: $error)")
                    // Tentar novamente após um delay
                    adHandler.postDelayed({
                        initializeUnityAds()
                    }, 30000L) // 30 segundos
                }
            })
        } catch (e: Exception) {
            Log.e("UnityAds", "Exceção na inicialização do Unity Ads", e)
        }
    }

    private fun tryStartAssistantService() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED) {
            FullDuplexAssistantAdvancedService.start(this)
        } else {
            Log.w("Permissions", "Tentativa de iniciar FullDuplexAssistant sem RECORD_AUDIO")
        }
    }

    private val recogReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            val source = intent?.getStringExtra("source") ?: ""
            Log.d("MainActivity", "Broadcast recebido: $source")
            runOnUiThread {
                updaterecogActive(source)
            }
        }
    }

    fun updaterecogActive(text: String) {
        recogActive.text = text
        Log.d("MainActivity", "atualizado: $text")
    }

    var partialCount = 0
    var iaativa = AtomicBoolean(false)
    private var resetJob: Job? = null
    var escutando = AtomicBoolean(false)
    private var resetescutandoJob: Job? = null
    // Receiver para detectar quando TTS está falando
    private var isTtsSpeaking = false
    private var soundWaveView: SoundWaveView? = null
    private lateinit var switchAvatar: SwitchCompat
    private lateinit var avatarOverlayContainer: FrameLayout
    private var avatarWebView: WebView? = null
    private var avatarGestureDetector: GestureDetector? = null
    private var isUpdatingAvatarSwitch = false
    private var avatarEnabled = false
    private var avatarAutoOpenJob: Job? = null

    private var pendingMatrixToken = 0L
    private var pendingMatrixReceivedAt = 0L
    private var pendingMatrixAwaitUntil = 0L
    private var pendingMatrixProcessingShown = false
    private var pendingMatrixTtsStarted = false
    private var pendingMatrixJob: Job? = null

    private var lastTtsEndTime = 0L
    private val TTS_COOLDOWN_MS = 600L // Reduzido de 1200ms para ser mais responsivo


    private val ttsReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {

            val agora = System.currentTimeMillis()

            when (intent?.action) {

                FullDuplexAssistantAdvancedService.BROADCAST_TTS_STARTED -> {
                    isTtsSpeaking = true
                    runOnUiThread {
                        showSoundWaves()
                        if (avatarEnabled) {
                            sendAvatarSpeaking(true)
                        }
                    }

                    val now = System.currentTimeMillis()
                    if (avatarEnabled && pendingMatrixToken != 0L && now <= pendingMatrixAwaitUntil) {
                        pendingMatrixTtsStarted = true
                        if (pendingMatrixProcessingShown) {
                            sendAvatarMatrixStatus("Comando processado.", 1400)
                            pendingMatrixJob?.cancel()
                            pendingMatrixJob = null
                            pendingMatrixToken = 0L
                            pendingMatrixProcessingShown = false
                            pendingMatrixTtsStarted = false
                        }
                    }

                    if (escutando.get() && isListening) {
                        iniciarTimerDesativacaoEscutando()
                        Log.d("MainActivity", "Timer resetado porque TTS começou a falar")
                    }
                }

                FullDuplexAssistantAdvancedService.BROADCAST_TTS_STOPPED -> {
                    isTtsSpeaking = false
                    lastTtsEndTime = agora

                    runOnUiThread {
                        hideSoundWaves()
                        if (avatarEnabled) {
                            sendAvatarSpeaking(false)
                            sendAvatarAudioLevel(0)
                        }
                    }

                    Log.d("MainActivity", "TTS marcado como parado (validação inteligente já feita no serviço)")
                }

                FullDuplexAssistantAdvancedService.BROADCAST_TTS_AUDIO_LEVEL -> {
                    val audioLevel = intent.getIntExtra(
                        FullDuplexAssistantAdvancedService.EXTRA_AUDIO_LEVEL,
                        0
                    )

                    runOnUiThread {
                        soundWaveView?.updateAudioLevel(audioLevel)
                        if (avatarEnabled) {
                            sendAvatarAudioLevel(audioLevel)
                        }
                    }

                    // 🔒 Reforço de "falando", mas NÃO reabre após STOP
                    if (audioLevel > 5 && agora - lastTtsEndTime > 50) {
                        isTtsSpeaking = true
                    }
                }
            }
        }
    }

    private val speechReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            if (intent?.action == FullDuplexAssistantAdvancedService.BROADCAST_SPEECH_RESULT) {
                val text = intent.getStringExtra(FullDuplexAssistantAdvancedService.EXTRA_RECOGNIZED_TEXT)
                
                if (text.isNullOrBlank()) return

                // Visual Feedback Básico (mantendo atualização de UI)
                val displayText = if(text.contains("parcial:")) text.replace("parcial:","") else text
                if (displayText.isNotBlank() && !displayText.contains("speaking:")) {
                    runOnUiThread { tvSpeechText.text = displayText }
                }

                 // Delega TODO o processamento para o Pipeline
                 lifecycleScope.launch {
                     processCommandViaPipeline(text)
                 }
            }
        }
    }



    private fun responseNameAssistent(){
        val defaltResponse = sessionManager.fetchDefaultResponse()

        if (defaltResponse == null)
            return

        if (!defaltResponse.isEmpty()){
            ultimaRespostaIA = defaltResponse
            speakTextFromService(defaltResponse)
        }

    }

    private suspend fun getNameAssistent(): String {
        val nameAssistent = db.appConfigDao().getConfig("assistant_name") ?: "Jarvis"
        val respostaPadrao = db.appConfigDao().getConfig("default_response") ?: "estou ouvindo, como posso ajudar?"
        return "nome: $nameAssistent\nresposta padrão: $respostaPadrao"
    }

    // Funções individuais
    private suspend fun getAssistantName(): String {
        return db.appConfigDao().getConfig("assistant_name") ?: "Assistente"
    }

    private suspend fun getAssistantPerson(): String {
        return db.appConfigDao().getConfig("personality") ?: "Descolado, carioca"
    }

    // Marcar usuário como online na API
    private suspend fun setUserOnline() {
        try {
            Log.d("MainActivity", "[setUserOnline] Iniciando chamada para marcar usuário como online")
            
            val token = sessionManager.fetchAuthToken()
            if (token.isNullOrEmpty()) {
                Log.w("MainActivity", "[setUserOnline] Token não disponível para marcar usuário como online")
                return
            }
            
            Log.d("MainActivity", "[setUserOnline] Token disponível, criando cliente Retrofit")

            val retrofit = ApiClient.getClient(this)
            val api = retrofit.create(UsuarioApi::class.java)
            val request = com.starkaid.starkaidapp.services.SetUserOnlineRequest(origem = "app")
            
            Log.d("MainActivity", "[setUserOnline] Enviando requisição para API...")
            val response = api.setUserOnline(request)
            
            if (response.isSuccessful) {
                val responseBody = response.body()
                Log.d("MainActivity", "[setUserOnline] ✅ Usuário marcado como online com sucesso! Resposta: ${responseBody?.message}")
            } else {
                val errorBody = response.errorBody()?.string()
                Log.e("MainActivity", "[setUserOnline] ❌ Erro ao marcar usuário como online: ${response.code()} - $errorBody")
            }
        } catch (e: Exception) {
            Log.e("MainActivity", "[setUserOnline] ❌ Exceção ao marcar usuário como online: ${e.message}", e)
            e.printStackTrace()
        }
    }

    // Marcar usuário como offline na API
    private suspend fun setUserOffline() {
        try {
            Log.d("MainActivity", "[setUserOffline] Iniciando chamada para marcar usuário como offline")
            
            val token = sessionManager.fetchAuthToken()
            if (token.isNullOrEmpty()) {
                Log.w("MainActivity", "[setUserOffline] Token não disponível para marcar usuário como offline")
                return
            }
            
            Log.d("MainActivity", "[setUserOffline] Token disponível, criando cliente Retrofit")

            val retrofit = ApiClient.getClient(this)
            val api = retrofit.create(UsuarioApi::class.java)
            val request = com.starkaid.starkaidapp.services.SetUserOfflineRequest(origem = "app")
            
            Log.d("MainActivity", "[setUserOffline] Enviando requisição para API...")
            val response = api.setUserOffline(request)
            
            if (response.isSuccessful) {
                val responseBody = response.body()
                Log.d("MainActivity", "[setUserOffline] ✅ Usuário marcado como offline com sucesso! Resposta: ${responseBody?.message}")
            } else {
                val errorBody = response.errorBody()?.string()
                Log.e("MainActivity", "[setUserOffline] ❌ Erro ao marcar usuário como offline: ${response.code()} - $errorBody")
            }
        } catch (e: Exception) {
            Log.e("MainActivity", "[setUserOffline] ❌ Exceção ao marcar usuário como offline: ${e.message}", e)
            e.printStackTrace()
        }
    }

    private suspend fun getDefaultResponse(): String {
        return db.appConfigDao().getConfig("default_response") ?: "Estou ouvindo!"
    }

    private suspend fun getPersonality(): String {
        return db.appConfigDao().getConfig("personality") ?: "Descolada"
    }

    private var permissionsDialogShown = false

    private fun updateConnectionStatus() {
        val connectionStatus = findViewById<TextView>(R.id.connectionStatus)
        val connectionStrength = findViewById<TextView>(R.id.connectionStrength)

        CoroutineScope(Dispatchers.IO).launch {
            val isOnline = isOnline()
            var apiOk = false
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val api = retrofit.create(UsersApi::class.java)
                val resp = api.getCurrentUser()
                apiOk = resp.isSuccessful
            } catch (_: Exception) {
                apiOk = false
            }

            val ok = isOnline && apiOk
            val strength = if (ok) 100 else 0

            runOnUiThread {
                connectionStatus.text = if (ok) "●" else "○"
                connectionStatus.setTextColor(if (ok) getColor(R.color.green_active) else getColor(R.color.red_inactive))
                connectionStrength.text = "$strength%"
            }
        }
    }



    // Add this variable declaration with the other ad-related variables
    private var adRetryCount = 0

    private fun calculateBackoffDelay(): Long {
        return minOf(adRetryCount * 30000L, 300000L) // Max 5 minutes
    }

    fun checkPermissionsAndInitAds(context: Context) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            val networkPermission = ContextCompat.checkSelfPermission(
                context, android.Manifest.permission.ACCESS_NETWORK_STATE
            )
            if (networkPermission != PackageManager.PERMISSION_GRANTED) {
                ActivityCompat.requestPermissions(
                    context as Activity,
                    arrayOf(android.Manifest.permission.ACCESS_NETWORK_STATE),
                    1001
                )
                return
            }
        }

        initAds(context)
    }

    private val AD_UNIT_ID = "ca-app-pub-8791322668156076/9828885335"

    private fun loadInterstitialAd() {
        if (isDestroyed || isFinishing) return
        
        // Verificar se o usuário tem o plano "remove ads" ativo - não carregar anúncios se tiver
        if (!adsReturn.get()) {
            Log.d("Ads", "Plano Remove Ads ativo - não carregando anúncios")
            return
        }
        
        if (System.currentTimeMillis() - lastAdShowTime < MIN_TIME_BETWEEN_ADS) return

        // VERIFICAÇÃO CORRIGIDA - sem parênteses
        if (!UnityAds.isInitialized) {
            Log.w("UnityAds", "Unity Ads não inicializado - tentando inicializar")
            initializeUnityAds()
            return
        }

        val adRequest = AdRequest.Builder().build()

        InterstitialAd.load(this, AD_UNIT_ID, adRequest,
            object : InterstitialAdLoadCallback() {
                override fun onAdLoaded(interstitialAd: InterstitialAd) {
                    mInterstitialAd = interstitialAd
                    adRetryCount = 0
                    setupAdListeners()
                    Log.d("Ads", "Anúncio intersticial carregado com sucesso")
                }

                override fun onAdFailedToLoad(loadAdError: LoadAdError) {
                    mInterstitialAd = null
                    adRetryCount++
                    Log.e("Ads", "Falha ao carregar anúncio: ${loadAdError.message}")

                    val delay = calculateBackoffDelay()
                    Log.d("Ads", "Tentando novamente em ${delay/1000} segundos")

                    // Verificar novamente antes de tentar recarregar
                    if (adsReturn.get()) {
                        adHandler.postDelayed({ loadInterstitialAd() }, delay)
                    } else {
                        Log.d("Ads", "Plano Remove Ads ativo - cancelando recarregamento de anúncios")
                    }
                }
            })
    }


    private fun enableFullScreenMode() {
        runOnUiThread {
            try {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                    // Android 11+ (API 30+)
                    window.insetsController?.hide(WindowInsets.Type.navigationBars())
                    window.insetsController?.hide(WindowInsets.Type.statusBars())
                    window.setDecorFitsSystemWindows(false)
                } else {
                    // Versões anteriores
                    @Suppress("DEPRECATION")
                    window.decorView.systemUiVisibility = (
                            View.SYSTEM_UI_FLAG_LAYOUT_STABLE or
                                    View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION or
                                    View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN or
                                    View.SYSTEM_UI_FLAG_HIDE_NAVIGATION or
                                    View.SYSTEM_UI_FLAG_FULLSCREEN or
                                    View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                            )
                }
            } catch (e: Exception) {
                Log.e("FullScreen", "Erro ao ativar modo tela cheia", e)
            }
        }
    }

    private fun disableFullScreenMode() {
        runOnUiThread {
            try {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                    // Android 11+ (API 30+)
                    window.insetsController?.show(WindowInsets.Type.navigationBars())
                    window.insetsController?.show(WindowInsets.Type.statusBars())
                    window.setDecorFitsSystemWindows(true)
                } else {
                    // Versões anteriores
                    @Suppress("DEPRECATION")
                    window.decorView.systemUiVisibility = (
                            View.SYSTEM_UI_FLAG_LAYOUT_STABLE or
                                    View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION or
                                    View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                            )
                }
            } catch (e: Exception) {
                Log.e("FullScreen", "Erro ao desativar modo tela cheia", e)
            }
        }
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)

        if (requestCode == REQUEST_GOOGLE_PLAY_SERVICES) {
            // Verifique novamente se os serviços estão disponíveis
            if (GoogleApiAvailability.getInstance().isGooglePlayServicesAvailable(this) != ConnectionResult.SUCCESS) {
                // Usuário não resolveu o problema
                Toast.makeText(this, "Google Play Services ainda não disponível", Toast.LENGTH_LONG).show()
                finish()
            }
        }
    }

    // Modifique o método para retornar void e lidar com o resultado adequadamente
    private fun checkGooglePlayServices() {
        val status = GoogleApiAvailability.getInstance().isGooglePlayServicesAvailable(this)

        if (status != ConnectionResult.SUCCESS) {
            if (GoogleApiAvailability.getInstance().isUserResolvableError(status)) {
                // Mostra diálogo para usuário resolver
                GoogleApiAvailability.getInstance()
                    .getErrorDialog(this, status, REQUEST_GOOGLE_PLAY_SERVICES)
                    ?.show()
            } else {
                // Não é resolvível pelo usuário
                Toast.makeText(this, "Google Play Services não disponível", Toast.LENGTH_LONG).show()
                finish()
            }
        }
        // Se status == SUCCESS, não precisa fazer nada
    }


    private fun setupAdListeners() {
        mInterstitialAd?.fullScreenContentCallback = object : FullScreenContentCallback() {
            override fun onAdDismissedFullScreenContent() {
                lastAdShowTime = System.currentTimeMillis()
                isAdShowing.set(false)

                // RESTAURAR UI NORMAL APÓS O ANÚNCIO
                disableFullScreenMode()

                Handler(Looper.getMainLooper()).postDelayed({
                    loadInterstitialAd()
                }, 10000)
            }

            override fun onAdFailedToShowFullScreenContent(adError: com.google.android.gms.ads.AdError) {
                isAdShowing.set(false)
                // RESTAURAR UI SE O ANÚNCIO FALHAR
                disableFullScreenMode()
            }

            override fun onAdShowedFullScreenContent() {
                // Garantir que está em tela cheia completa
                Handler(Looper.getMainLooper()).postDelayed({
                    enableFullScreenMode()
                }, 100)
            }
        }
    }



    private fun showAdIfReady() {
        if (!adsReturn.get()) {
            return
        }

        val currentTime = System.currentTimeMillis()
        val role = sessionManager.fetchUserRole()

        // Verifica cooldown
        val canShowAd = (currentTime - lastAdShowTime > MIN_TIME_BETWEEN_ADS)
        if (canShowAd) {
            // Lógica baseada em navegação no app
            val shouldShowAd = when (role) {
                "UserNivel2", "UserNivel3", "UserNivel4", "UserNivel5", "UserNivel6", "UserNivel7" ->
                    adCounter >= AD_FREQUENCYNivel2 && mInterstitialAd != null
                "UserNivel1" ->
                    adCounter >= AD_FREQUENCYNivel1 && mInterstitialAd != null
                else -> false
            }

            if (shouldShowAd) {
                adCounter = 0
                sessionManager.saveAdCounter(0)
                isAdShowing.set(true)
                // FORÇAR MODO TELA CHEIA COMPLETA ANTES DO ANÚNCIO
                enableFullScreenMode()

                mInterstitialAd?.show(this)
                return
            }

            // Lógica baseada em fechamentos completos
            if (appOpenCount >= APP_OPEN_THRESHOLD && mInterstitialAd != null) {
                isAdShowing.set(true)

                // FORÇAR MODO TELA CHEIA COMPLETA ANTES DO ANÚNCIO
                enableFullScreenMode()

                mInterstitialAd?.show(this)
                appOpenCount = 0
                sessionManager.saveAppOpenCount(0)
            }
        }
    }

    private fun setupUnityAdsFullScreen() {
        try {
            // Configurar o UnityAds para usar tela cheia imersiva
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                window.setDecorFitsSystemWindows(false)
            }
        } catch (e: Exception) {
            Log.e("UnityAds", "Erro na configuração de tela cheia", e)
        }
    }

    // NOVO MÉTODO: Inicializar serviços após validação do token
    private fun initializeServicesAfterValidation() {
        lifecycleScope.launch {
            try {
                // Valida o token extraindo o role dele
                val token = sessionManager.fetchAuthToken()
                val role = extractRoleFromToken(token)
                
                if (role != null) {
                    sessionManager.saveUserRole(role)
                    initializeHubAndWebSocket()
                } else {
                    // Token inválido ou sem role
                    sessionManager.clearTokens()
                    redirectToLogin()
                }
            } catch (e: Exception) {
                Log.e("Services", "Falha crítica na inicialização", e)
                // Modo offline - não crasha o app
                runOnUiThread {
                    Toast.makeText(
                        this@MainActivity,
                        "App iniciado em modo offline",
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
        }
    }

    private fun initializeHubAndWebSocket() {
        try {
            hubService = HubService(sessionManager, this@MainActivity, this@MainActivity)
            wsManager = WebSocketManager(sessionManager) { message ->
                runOnUiThread {
                    Log.d("WebSocket", "WS: $message")
                }
            }

            hubService.start()
            wsManager.start()
            connectEspWebSocketHub() // Conectar ao Hub de dispositivos ESP
            servicesInitialized = true
            Log.d("Services", "Serviços SignalR e WebSocket inicializados com sucesso")

            // Buscar saldo de StarkCoins após inicialização bem-sucedida
            getStarkcoins()

        } catch (e: Exception) {
            errorLogger.logError(
                e,
                ErrorCodes.ERR_901,
                "ao inicializar SignalR/WebSocket",
                null,
                null,
                null
            )
            // Não deixe o app crashar - continue em modo offline
            runOnUiThread {
                Toast.makeText(
                    this@MainActivity,
                    "Funcionalidade online temporariamente indisponível",
                    Toast.LENGTH_SHORT
                ).show()
            }
        }
    }


    @SuppressLint("UseKtx")
    private fun logout() {
        try {
            Log.d("Suporte", "Iniciando logout...")
            
            // Tentar enviar resposta de suporte ANTES de limpar tokens
            try {
                enviarRespostaAcaoSuporte("logout", true, "Logout executado. Retornando à tela de login.")
            } catch (e: Exception) {
                Log.d("Suporte", "Não foi possível enviar resposta de suporte: ${e.message}")
            }
            
            // Parar serviços
            try {
                if (::hubService.isInitialized) {
                    hubService.stop()
                    Log.d("Suporte", "HubService parado")
                }
            } catch (e: Exception) {
                Log.e("Suporte", "Erro ao parar HubService", e)
            }
            
            try {
                wsManager?.stop()
                Log.d("Suporte", "WebSocketManager parado")
            } catch (e: Exception) {
                Log.e("Suporte", "Erro ao parar WebSocketManager", e)
            }
            
            try {
                espHubConnection?.stop()?.blockingAwait()
                Log.d("Suporte", "ESP Hub parado")
            } catch (e: Exception) {
                Log.e("Suporte", "Erro ao parar ESP Hub", e)
            }
            
            // Limpar preferências
            val prefs = getSharedPreferences("starkaid_prefs", MODE_PRIVATE)
            prefs.edit().clear().apply()
            
            // Limpar SessionManager
            sessionManager.clearSession()
            
            Toast.makeText(this, "Saindo da conta...", Toast.LENGTH_SHORT).show()
            
            // Volta para a tela de login
            val intent = Intent(this, LoginActivity::class.java)
            intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            startActivity(intent)
            finish()
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao fazer logout", e)
            // Mesmo com erro, tentar fazer logout
            val prefs = getSharedPreferences("starkaid_prefs", MODE_PRIVATE)
            prefs.edit().clear().apply()
            sessionManager.clearSession()
            val intent = Intent(this, LoginActivity::class.java)
            intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            startActivity(intent)
            finish()
        }
    }

    private fun requestMissingPermissions(missing: List<String>) {
        // Separar permissões normais e especiais
        val normalPermissions = missing.filter {
            it.startsWith("android.permission.") &&
                    it != "android.permission.health.READ_HEART_RATE"
        }.toTypedArray()

        val specialPermissions = missing.filter {
            !it.startsWith("android.permission.") ||
                    it == "android.permission.health.READ_HEART_RATE"
        }

        // Solicitar permissões normais de uma vez
        if (normalPermissions.isNotEmpty()) {
            ActivityCompat.requestPermissions(this, normalPermissions, 101)
        }

        // Processar permissões especiais uma a uma
        specialPermissions.forEach { permission ->
            when (permission) {
                "overlay" -> {
                    val intent = Intent(
                        Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
                        "package:$packageName".toUri()
                    )
                    startActivity(intent)
                }
                "dnd" -> {
                    AlertDialog.Builder(this)
                        .setTitle("Permissão 'Não Perturbe'")
                        .setMessage(
                            "Para que o app controle notificações sonoras, é necessário permitir acesso ao modo 'Não Perturbe'.\n\n" +
                                    "Na próxima tela, procure por 'StarkAid Automação' e ative o acesso. Após conceder, reinicie o aplicativo para que as alterações sejam aplicadas."
                        )
                        .setPositiveButton("Ir para configurações") { _, _ ->
                            val intent = Intent(Settings.ACTION_NOTIFICATION_POLICY_ACCESS_SETTINGS)
                            startActivity(intent)
                        }
                        .setNegativeButton("Cancelar", null)
                        .show()
                }
            }
        }
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray,
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)

        when (requestCode) {
            101 -> {
                val allGranted = grantResults.all { it == PackageManager.PERMISSION_GRANTED }
                if (!allGranted) {
                    permissions.forEachIndexed { index, permission ->
                        if (grantResults[index] != PackageManager.PERMISSION_GRANTED) {
                            when (permission) {
                                Manifest.permission.RECORD_AUDIO -> {
                                    Toast.makeText(this,
                                        "Microfone é necessário para comandos de voz",
                                        Toast.LENGTH_LONG).show()
                                }
                                Manifest.permission.ACCESS_FINE_LOCATION -> {
                                    Toast.makeText(this,
                                        "Localização precisa é necessária para funcionalidades de clima e localização",
                                        Toast.LENGTH_LONG).show()
                                }
                                Manifest.permission.ACCESS_COARSE_LOCATION -> {
                                    Toast.makeText(this,
                                        "Localização aproximada é necessária para algumas funcionalidades",
                                        Toast.LENGTH_LONG).show()
                                }
                                Manifest.permission.ACCESS_BACKGROUND_LOCATION -> {
                                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                                        Toast.makeText(this,
                                            "Localização em segundo plano é necessária para funcionalidades contínuas",
                                            Toast.LENGTH_LONG).show()
                                    }
                                }
                                Manifest.permission.POST_NOTIFICATIONS -> {
                                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                                        Toast.makeText(this,
                                            "Notificações são necessárias para alertas e comunicações importantes",
                                            Toast.LENGTH_LONG).show()
                                    }
                                }
                                Manifest.permission.ACCESS_WIFI_STATE -> {
                                    Toast.makeText(this,
                                        "Acesso ao estado WiFi é necessário para controle de dispositivos",
                                        Toast.LENGTH_LONG).show()
                                }
                                Manifest.permission.CHANGE_WIFI_STATE -> {
                                    Toast.makeText(this,
                                        "Alterar estado WiFi é necessário para algumas funcionalidades de rede",
                                        Toast.LENGTH_LONG).show()
                                }
                                // Adicione outras permissões conforme necessário
                            }
                        }
                    }
                }

            }

        }

        // Verificar permissões de rede separadamente
        if (requestCode == NETWORK_PERMISSION_REQUEST_CODE) {
            val allGranted = grantResults.all { it == PackageManager.PERMISSION_GRANTED }
            if (!allGranted) {
                Toast.makeText(
                    this,
                    "Algumas permissões de rede não foram concedidas. O app pode não funcionar corretamente.",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }


    private fun checkNetworkPermissions() {
        val permissionsToRequest = mutableListOf<String>()

        // Verificar e solicitar apenas permissões perigosas
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_WIFI_STATE)
            != PackageManager.PERMISSION_GRANTED) {
            permissionsToRequest.add(Manifest.permission.ACCESS_WIFI_STATE)
        }

        // Adicione outras permissões de rede se necessário
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CHANGE_WIFI_STATE)
            != PackageManager.PERMISSION_GRANTED) {
            permissionsToRequest.add(Manifest.permission.CHANGE_WIFI_STATE)
        }

        // Solicitar permissões se houver alguma para solicitar
        if (permissionsToRequest.isNotEmpty()) {
            ActivityCompat.requestPermissions(
                this,
                permissionsToRequest.toTypedArray(),
                NETWORK_PERMISSION_REQUEST_CODE
            )
        }
    }

    private fun getMissingPermissions(): List<String> {
        val missing = mutableListOf<String>()

        // Permissões normais
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
            missing.add(Manifest.permission.RECORD_AUDIO)
        }

        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            missing.add(Manifest.permission.ACCESS_FINE_LOCATION)
        }

        // Permissão de notificações (Android 13+)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS)
                != PackageManager.PERMISSION_GRANTED) {
                missing.add(Manifest.permission.POST_NOTIFICATIONS)
            }
        }

        // Permissão de localização em background (Android 10+)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_BACKGROUND_LOCATION)
                != PackageManager.PERMISSION_GRANTED) {
                missing.add(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
            }
        }

        // Sobreposição de tela
        val overlayPermissionOk = Settings.canDrawOverlays(this)
        if (!overlayPermissionOk) {
            missing.add("overlay")
        }

        // DND
        val notificationManager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager
        if (!notificationManager.isNotificationPolicyAccessGranted) {
            missing.add("dnd")
        }

        // Permissões de rede (já estão sendo verificadas em checkNetworkPermissions)
        // mas podemos adicioná-las aqui também se necessário
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_WIFI_STATE)
            != PackageManager.PERMISSION_GRANTED) {
            missing.add(Manifest.permission.ACCESS_WIFI_STATE)
        }

        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CHANGE_WIFI_STATE)
            != PackageManager.PERMISSION_GRANTED) {
            missing.add(Manifest.permission.CHANGE_WIFI_STATE)
        }

        return missing
    }

    private fun redirectToLogin() {
        val intent = Intent(this, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }


    private fun optimizePhone() {
        if (isServiceRunning(DeviceOptimizationService::class.java)) {
            Toast.makeText(this, "Otimização já em andamento", Toast.LENGTH_SHORT).show()
            return
        }

        Toast.makeText(this, "Otimizando telefone...", Toast.LENGTH_SHORT).show()

        val intent = Intent(this, DeviceOptimizationService::class.java)
        startForegroundService(intent)

        speakTextFromService("Otimização do telefone iniciada")
    }


    // Função para verificar se o serviço está em execução
    @Suppress("DEPRECATION")
    private fun isServiceRunning(serviceClass: Class<*>): Boolean {
        val manager = getSystemService(ACTIVITY_SERVICE) as ActivityManager
        return manager.getRunningServices(Integer.MAX_VALUE)
            .any { it.service.className == serviceClass.name }
    }


    private fun refreshData() {
        Toast.makeText(this, "Atualizando dados...", Toast.LENGTH_SHORT).show()

        // Mostrar indicador de carregamento
        val progressBar = findViewById<ProgressBar>(R.id.progressBar)
        progressBar?.visibility = View.VISIBLE

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // Recarregar dispositivos do backend
                loadDevices()
                
                // Aguardar um pouco para garantir que os dispositivos foram carregados
                delay(500)
                
                // Atualizar contador de dispositivos após carregar
                withContext(Dispatchers.Main) {
                    atualizarContadorDispositivos()
                }
                
                // Carregar dispositivos eWeLink
                carregarDispositivosEwelink()
                
                // Aguardar um pouco para garantir que os dispositivos eWeLink foram carregados
                delay(500)
                
                // Atualizar contador de dispositivos novamente (incluindo eWeLink)
                withContext(Dispatchers.Main) {
                    atualizarContadorDispositivos()
                }
                
                // Recarregar comandos do banco local (que devem ter sido atualizados do backend)
                recarregarComandosLocais()
                
                // Observar mudanças nos comandos
                observarComandosLocais()
                
                // Recarregar outros dados
                preCarregarDispositivos()
                getContactsFromList()
                getStarkcoins()
                obterDadosUser()

                // Simular um tempo mínimo de carregamento para melhor UX
                delay(1000)

                withContext(Dispatchers.Main) {
                    // Garantir que os contadores estão atualizados
                    atualizarContadorDispositivos()
                    recarregarComandosLocais()
                    
                    progressBar?.visibility = View.GONE
                    Toast.makeText(this@MainActivity, "Dados atualizados com sucesso", Toast.LENGTH_SHORT).show()
                    speakTextFromService("Dados atualizados")
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    progressBar?.visibility = View.GONE
                    Toast.makeText(this@MainActivity, "Erro ao atualizar dados: ${e.message}", Toast.LENGTH_SHORT).show()
                    speakTextFromService("Falha ao atualizar dados")
                    Log.e("MainActivity", "Erro ao atualizar dados", e)
                }
            }
        }
    }



    // ⚡ Corrigido: assinatura correta (Intent, não Intent?)
    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent) // Importante para que getIntent() retorne o intent correto


        val uri = intent.data
        if (uri != null && uri.toString().startsWith("starkaid://spotifycallback")) {
            Log.d("SpotifyService", "Spotify callback recebido")
            val code = uri.getQueryParameter("code")
            if (code != null) {
                Log.d("SpotifyService", "Spotify code callback: $code")
                exchangeCodeWithBackend(code)
            }
        }

        val data = intent?.data
        if (data != null && data.scheme == "starkaid" && data.host == "ewelink") {
            Log.d("EWE", "MainActivity: Redirecionando deep link para EwelinkLoginActivity")
            val redirectIntent = Intent(this, EwelinkLoginActivity::class.java).apply {
                this.data = data
                flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            }
            startActivity(redirectIntent)
        }

        // Tratar retorno de pagamento (deep link starkaid://payment)
        if (uri != null && uri.scheme == "starkaid" && uri.host == "payment") {
            val fundsStatus = uri.getQueryParameter("funds")
            Log.d("Payment", "Deep link de pagamento recebido: funds=$fundsStatus")
            
            if (fundsStatus == "success") {
                // Pagamento bem-sucedido - atualizar dados do usuário
                Toast.makeText(this, "Pagamento confirmado! Atualizando saldo...", Toast.LENGTH_SHORT).show()
                // Aguardar um pouco para o webhook processar
                Handler(Looper.getMainLooper()).postDelayed({
                    getStarkcoins()
                }, 2000)
            } else if (fundsStatus == "cancel") {
                Toast.makeText(this, "Pagamento cancelado", Toast.LENGTH_SHORT).show()
            }
        }

    }



    private fun startSpotifyLogin() {
        val clientId = "b777ae2408054cebafda44c36a80be31"
        val redirectUri = "starkaid://spotifycallback"
        val scopes = "user-read-playback-state user-modify-playback-state user-read-private"

        val url = "https://accounts.spotify.com/authorize?" +
                "client_id=$clientId" +
                "&response_type=code" +
                "&redirect_uri=$redirectUri" +
                "&scope=$scopes"

        val intent = Intent(Intent.ACTION_VIEW, Uri.parse(url))
        this.startActivity(intent)
    }




    private fun exchangeCodeWithBackend(code: String) {
        val userId = sessionManager.fetchUserId()
        val url = "${ApiConfig.apiBaseUrl}/v1/spotifyauth/exchange"

        val json = JSONObject()
        json.put("code", code)
        json.put("userId", userId.toString())

        Log.d("SpotifyService", "exchangeCode: $code")
        Log.d("SpotifyService", "exchangeCode userId: $userId")

        val body = json.toString().toRequestBody("application/json".toMediaType())

        Log.d("SpotifyService", "body: $body")

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val res = OkHttpClient().newCall(
                    Request.Builder().url(url).post(body).build()
                ).execute()

                if (res.isSuccessful) {
                    val data = JSONObject(res.body?.string() ?: "")
                    val accessToken = data.getString("accessToken")
                    val refreshToken = data.getString("refreshToken")
                    val expiresIn = data.getLong("expiresIn")

                    Log.d("SpotifyService", "exchangeCode accessToken: $accessToken")
                    Log.d("SpotifyService", "exchangeCode refreshToken: $refreshToken")
                    Log.d("SpotifyService", "exchangeCode expiresIn: $expiresIn")

                    Log.d("SpotifyService", "exchangeCode data: $data")


                    prefs.edit()
                        .putString("spotify_access_token", accessToken)
                        .putString("spotify_refresh_token", refreshToken)
                        .putLong("spotify_expires_at", System.currentTimeMillis() + expiresIn * 1000)
                        .apply()

                    Log.d("SpotifyService", "exchangeCode prefs: $prefs")

                    Log.d("SpotifyService", "exchangeCode accessToken: $accessToken")

                    spotifyService.updateUserProduct()

                } else {
                    Log.e("SpotifyService", "exchangeCode Erro no backend: ${res.code}")
                }
            } catch (e: Exception) {
                Log.e("SpotifyService", "exchangeCode Falha: ${e.message}", e)
            }
        }
    }


    /** Toca música pelo nome */
    /** Toca música pelo nome */
    private var erroMusicaFalado = false
    fun playTrackByName(trackName: String) {
        val userId = sessionManager.fetchUserId() ?: return
        val authToken = sessionManager.fetchAuthToken() ?: return

        val isSpotifyEnabled = prefs.getBoolean("spotify_enabled", false)
        if (!isSpotifyEnabled) {
            Log.w("AnalizaTexto", "Spotify desativado, não será tocada nenhuma música.")
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = usuarioApi.tocarMusica(
                    "Bearer $authToken",
                    "e2fcdf11548e4aa18e5660aec85f96fe", // sua Api-Key
                    MusicaDto(nomeMusica = trackName)
                )

                Log.d("AnalizaTexto", "Resposta API tocar música: $response")

                if (response.isSuccessful) {
                    val result = response.body()!!

                    // Atualiza saldo na UI
                    runOnUiThread {
                        tvStarkcoins.text = String.format("%.2f SC", result.saldoAtual)
                    }

                    if (result.autorizado) {
                        erroMusicaFalado = false // reset caso tenha sucesso
                        Log.d("AnalizaTexto", "Tocando música: $trackName")
                        lifecycleScope.launch {
                            spotifyService.play(trackName)
                        }
                    } else {
                        if (!erroMusicaFalado) {
                            erroMusicaFalado = true
                            speakTextFromService(result.message ?: "Saldo insuficiente para tocar música.")
                        }
                    }
                } else {
                    if (!erroMusicaFalado) {
                        erroMusicaFalado = true
                        Log.e("AnalizaTexto", "Erro API tocar música: ${response.code()}")
                        speakTextFromService("Erro ao tocar música. Tente mais tarde.")
                    }
                }
            } catch (e: Exception) {
                Log.e("AnalizaTexto", "Exceção ao chamar API tocar música", e)
                if (!erroMusicaFalado) {
                    erroMusicaFalado = true
                    speakTextFromService("Falha de conexão. Tente novamente.")
                }
            }
        }
    }

    fun stopPlayback() {
        //FullDuplexAssistantAdvancedService.musicaParadaIntencionalmente.set(true)

        lifecycleScope.launch {
            Log.d("AnalizaTextoMusica", "Parando música...")

            // Parar música primeiro
            spotifyService.stopMusic()

            // Delay maior para garantir que a música pare completamente
            delay(5000) // Aumente para 5 segundos


            // Só então resetar a flag
            // FullDuplexAssistantAdvancedService.musicaParadaIntencionalmente.set(false)
            Log.d("AnalizaTextoMusica", "Flag de música parada resetada")
        }

//        val intent = Intent(this, FullDuplexAssistantAdvancedService::class.java).apply {
//            action = FullDuplexAssistantAdvancedService.ACTION_STOP_LISTENING_AWS
//        }
//        startService(intent)


    }

    fun cleanText(text: String): String {
        // 🔹 Converte tudo pra minúsculas
        var cleanText = text.lowercase(Locale.getDefault())

        // 🔹 Remove acentos e caracteres especiais de Unicode
        cleanText = Normalizer.normalize(cleanText, Normalizer.Form.NFD)
            .replace("\\p{InCombiningDiacriticalMarks}+".toRegex(), "") // remove acentos

        // 🔹 Remove caracteres inválidos (mantém letras, números e alguns símbolos)
        cleanText = cleanText
            .replace("[^a-z0-9+\\-*/x ]".toRegex(), "")
            .trim()

        return cleanText
    }



    // Função auxiliar para verificar se é um comando de parar de falar
    private fun isStopTalkingCommand(text: String): Boolean {
        // Remover prefixos como "parcial:" e "speaking:" antes de verificar
        var textToCheck = text.lowercase().trim()
        textToCheck = textToCheck.replace("parcial:", "").replace("speaking:", "").trim()
        
        // Verificar tanto o texto original quanto o texto limpo
        val original = textToCheck
        val clean = cleanText(original)
        
        // Lista completa de comandos de parar de falar
        val stopCommands = listOf(
            "parar de falar", "pare de falar", "para de falar",
            "cale a boca", "cala a boca", "calar a boca",
            "cala boca", "cale boca", "calar boca",
            "cale-se", "cala-se", "calar-se",
            "fica quieto", "fique quieto", "ficar quieto",
            "fica calado", "fique calado", "ficar calado",
            "silencio", "silêncio",
            "para com isso", "pare com isso", "parar com isso",
            "chega de falar", "basta de falar",
            "calece", "cale se", "cala se"
        )
        
        // Verificar se algum comando está presente no texto original ou limpo
        val foundInList = stopCommands.any { command ->
            original.contains(command) || clean.contains(command)
        }
        
        // Verificações adicionais com combinações (mais permissivas)
        val foundInCombinations = 
            (original.contains("parar") && original.contains("falar")) ||
            (original.contains("pare") && original.contains("falar")) ||
            (original.contains("para") && original.contains("falar")) ||
            (original.contains("falar") && (original.contains("parar") || original.contains("pare") || original.contains("para"))) ||
            (clean.contains("parar") && clean.contains("falar")) ||
            (clean.contains("pare") && clean.contains("falar")) ||
            (clean.contains("para") && clean.contains("falar")) ||
            (clean.contains("falar") && (clean.contains("parar") || clean.contains("pare") || clean.contains("para"))) ||
            (clean.contains("boca") && (clean.contains("calar") || clean.contains("cale") || clean.contains("cala"))) ||
            (original.contains("cala") && original.contains("boca")) ||
            (original.contains("cale") && original.contains("boca"))
        
        val result = foundInList || foundInCombinations
        
        if (result) {
            Log.d("MainActivity", "✅ Comando de parar detectado - Original: '$original', Limpo: '$clean'")
        }
        
        return result
    }

    private suspend fun processSpeechResultWithAvatarStages(result: String): Boolean {
        val isPartial = result.lowercase().contains("parcial:")
        val isSpeakingCaptured = result.lowercase().contains("speaking:")
        val shouldShow = avatarEnabled && !isPartial && !isSpeakingCaptured
        if (!shouldShow) return processSpeechResult(result)

        val token = System.currentTimeMillis()
        pendingMatrixToken = token
        pendingMatrixReceivedAt = token
        pendingMatrixAwaitUntil = token + 12000L
        pendingMatrixProcessingShown = false
        pendingMatrixTtsStarted = false

        pendingMatrixJob?.cancel()
        pendingMatrixJob = lifecycleScope.launch {
            delay(1000)
            if (pendingMatrixToken != token || !avatarEnabled) return@launch
            pendingMatrixProcessingShown = true
            sendAvatarMatrixStatus("Processando comando...", 6000)

            if (pendingMatrixTtsStarted) {
                delay(150)
                if (pendingMatrixToken != token || !avatarEnabled) return@launch
                sendAvatarMatrixStatus("Comando processado.", 1400)
                pendingMatrixToken = 0L
                pendingMatrixProcessingShown = false
                pendingMatrixTtsStarted = false
            }
        }

        sendAvatarMatrixStatus("Comando recebido...", 2000)

        return try {
            processSpeechResult(result)
        } finally {
            if (!pendingMatrixTtsStarted && pendingMatrixToken == token && avatarEnabled) {
                lifecycleScope.launch {
                    delay(1200)
                    if (pendingMatrixToken == token && !pendingMatrixTtsStarted && avatarEnabled) {
                        sendAvatarMatrixStatus("", 0)
                        pendingMatrixJob?.cancel()
                        pendingMatrixJob = null
                        pendingMatrixToken = 0L
                        pendingMatrixProcessingShown = false
                    }
                }
            }
        }
    }

    private suspend fun processSpeechResult(result: String): Boolean {
        val userId = sessionManager.fetchUserId()
        val currentTime = System.currentTimeMillis()

        // IMPORTANTE: Se o comando tem prefixo "speaking:", IGNORAR completamente
        // Isso significa que foi capturado durante TTS e não deve ser processado
        if (result.lowercase().contains("speaking:")) {
            Log.d("TestandoIA", "🚫 Ignorando comando capturado durante TTS (speaking:): '$result'")
            return false
        }

        // Remover prefixos "parcial:" ANTES de processar o texto
        var textToProcess = result
        if (result.lowercase().contains("parcial:"))
            textToProcess = result.replace("parcial:", "", ignoreCase = true)
        
        // Agora processar o texto limpo
        var cleanText = cleanText(textToProcess).trim()

        Log.d("TestandoIA", "Comando reconhecido processSpeechResult: $cleanText")
        Log.d("TestandoIA", "aguardandoLiberarConsumoStarkcoins: $aguardandoLiberarConsumoStarkcoins")
        
        // Verificar se está aguardando resposta sobre usar StarkCoins (PRIORIDADE MÁXIMA)
        if (aguardandoLiberarConsumoStarkcoins) {
            Log.d("TestandoIA", "✅ Verificando resposta sobre StarkCoins. Texto original: '$result', limpo: '$cleanText'")
            val normalized = cleanText
            val isPositive = isPositiveAnswer(normalized)
            val isNegative = isNegativeAnswer(normalized)
            Log.d("TestandoIA", "isPositiveAnswer: $isPositive, isNegativeAnswer: $isNegative")
            
            if (isPositive) {
                Log.d("TestandoIA", "✅ Resposta POSITIVA detectada! Reativando IA com StarkCoins.")
                aguardandoLiberarConsumoStarkcoins = false
                if (saldoStarkcoinsInt > 0) {
                    iaLimitReached = false
                    iaUsandoStarkCoins = true
                    runOnUiThread {
                        isSwitchIaChangingProgrammatically = true
                        switchIa.isChecked = true
                        prefs.edit().putBoolean("ia_enabled", true).apply()
                        isSwitchIaChangingProgrammatically = false
                    }
                    speakTextFromService("Ok, inteligência reativada usando StarkCoins.")
                } else {
                    Log.d("TestandoIA", "❌ Saldo insuficiente detectado em processSpeechResult: $saldoStarkcoinsInt SC")
                    speakTextFromService("Saldo insuficiente. Você tem apenas $saldoStarkcoinsInt StarkCoins. Adicione mais e tente novamente.")
                }
                return true
            }
            if (isNegative) {
                Log.d("TestandoIA", "✅ Resposta NEGATIVA detectada! Desativando IA e resetando flags.")
                aguardandoLiberarConsumoStarkcoins = false
                iaLimitReached = true
                iaUsandoStarkCoins = false // Garantir que flag está false quando usuário recusa
                runOnUiThread {
                    isSwitchIaChangingProgrammatically = true
                    switchIa.isChecked = false
                    prefs.edit().putBoolean("ia_enabled", false).apply()
                    isSwitchIaChangingProgrammatically = false
                }
                speakTextFromService("Ok, inteligência não será ativada.")
                return true
            } else {
                Log.d("TestandoIA", "⚠️ Resposta não reconhecida como positiva nem negativa. Texto normalizado: '$normalized'")
            }
        }
        
        // Se assistente está dormindo (escutando = false), não processar nenhum comando
        // (exceto o nome do assistente que já foi tratado no speechReceiver)
        if (!escutando.get()) {
            Log.d("TestandoIA", "Assistente está dormindo - ignorando comando: $cleanText")
            return false
        }
        
        // Se TTS está falando, só processar comandos de parar de falar
        if (isTtsSpeaking) {
            if (isStopTalkingCommand(result)) {
                Log.d("TestandoIA", "TTS falando mas comando é para parar - processando")
                // Permite processar o comando de parar de falar
            } else {
                Log.d("TestandoIA", "TTS está falando - ignorando comando: $cleanText")
                return false
            }
        }
        
        // Primeiro tenta processar comandos (dispositivos, automações, etc.)
        val comandosExecutados = processandoComandos(result)
        if(comandosExecutados){
            Log.d("TestandoIA","Executou processandoComandos")
            return true
        }
        else{
            Log.d("TestandoIA","NAO Executou processandoComandos ${cleanText}")
            // Só chama a Super IA se nenhum comando foi executado
            // Nota: "speaking:" já foi filtrado no início da função
            if (!result.lowercase().contains("parcial:")){
                if(getIaResponse(cleanText)) {
                    Log.d("TestandoIA", "Executou Comandos IA")
                    return true
                }
                else{
                    return false
                }
            }
            return false
        }
    }


    private var lastDirectCommandTime = 0L
    private val DIRECT_COMMAND_COOLDOWN = 5000L

    var passou = false
    private suspend fun processandoComandos(
        text: String
    ): Boolean {
        Log.e("MainActivityLog", "Ultimo comando reconhecido: $text")
        Log.d("TuyaComando", "Ultimo comando reconhecido: $text")

        // Se assistente está dormindo (escutando = false), não processar nenhum comando
        if (!escutando.get()) {
            Log.d("MainActivityLog", "Assistente está dormindo - ignorando comando em processandoComandos: $text")
            return false
        }

        val agora = System.currentTimeMillis()

        // 🔒 BLOQUEIO pós-TTS (anti-eco)
        if (agora - lastTtsEndTime < TTS_COOLDOWN_MS) {
            Log.d(
                "MainActivityLog",
                "Ignorando STT durante cooldown pós-TTS: $text"
            )
            return false
        }

        // Se TTS está falando, só processar comandos de parar de falar
        if (isTtsSpeaking) {
            if (isStopTalkingCommand(text)) {
                Log.d("MainActivityLog", "TTS falando mas comando é para parar - processando em processandoComandos")
                // Permite processar o comando de parar de falar
            } else {
                Log.d("MainActivityLog", "TTS está falando - ignorando comando em processandoComandos: $text")
                return false
            }
        }

        if(comandosSocialGet(text)){
            Log.e("MainActivityLog", "Executou Comandos Sociais")
            return true
        }
        else if (processDirectCommands(text)) {
            Log.e("MainActivityLog", "Executou Comandos Diretos")
            return true
        }
        else if(execAutomacao(text)){
            Log.e("MainActivityLog", "Executou automacao")
            return true
        }
        else{
            // Tentar controlar dispositivos ANTES de retornar false
            // Isso garante que dispositivos sejam tentados antes da Super IA
            if (!text.contains("parcial") && !text.lowercase().contains("speaking:")) {
                Log.d("EWE_VOICE", "Tentando controlar dispositivos: $text")
                
                // Tentar ESP primeiro (comandos mais específicos)
                val executadoEsp = controlarDispositivoEsp(text)
                if (executadoEsp) {
                    Log.d("ESP_VOICE", "Dispositivo ESP controlado com sucesso")
                    return true
                }
                
                // Tentar eWeLink
                val executadoEwelink = controlarDispositivoEwelink(text)
                if (executadoEwelink) {
                    Log.d("EWE_VOICE", "Dispositivo eWeLink controlado com sucesso")
                    return true
                }
                
                // Tentar StarkSwitch (se houver método específico)
                // Por enquanto, retorna false para que a Super IA seja chamada
                Log.d("MainActivityLog", "Nenhum dispositivo foi acionado")
            }
            return false
        }
    }

    // --- Definições fixas (iniciais, fora do listener) ---
    private val palavrasPerguntas = listOf(
        // Palavras interrogativas básicas
        "quem", "o que", "qual", "quais", "onde", "aonde",
        "de onde", "pra onde", "para onde", "quando", "que horas",
        "como", "por que", "porque", "por qual motivo", "por qual razao",
        "quanto", "quantos", "quantas", "pra que", "para que",

        // Expressões interrogativas
        "sera que", "eh verdade que", "tem como", "sabe se",

        // Solicitações de informação
        "voce sabe", "me diz", "me fale", "fala pra mim", "pode me dizer",
        "me conta", "queria saber", "to querendo saber", "da pra saber",
        "consegue me dizer", "poderia me dizer", "daria pra saber",
        "da pra ver", "pode ver se", "tem como saber", "diz pra mim",

        // Comandos que indicam pergunta
        "explica", "me explica", "me mostra", "mostra pra mim",
        "fala como", "fala quando", "fala onde", "fala quem",
        "fala o que", "fala qual", "fala quanto"
    )

    // Regex pré-compilado para palavras curtas (mantido fora da função)
    private val regexPerguntasCurta = Regex("\\b(quem|como|onde|quando|porque|qual)\\b")

    // --- Função leve chamada dentro do onResults/onPartialResults ---
    fun isPergunta(text: String): Boolean {
        if (text.isBlank()) return false
        val t = text.lowercase().trim()

        // Interrogação direta
        if (t.endsWith("?")) return true

        // Verificação rápida via regex (palavras curtas)
        if (regexPerguntasCurta.containsMatchIn(t)) return true

        // Verificação via lista para expressões compostas
        return palavrasPerguntas.any { t.contains(it) }
    }



    suspend fun getIaResponse(comand: String): Boolean {
        val text = comand.lowercase().trim()
        Log.d("TestandoIA","entrou getIaResponse: $text")

        if (switchIa.isChecked){
            Log.d("TestandoIA","switchIa.isChecked: $text")
            if (iaativa.get()){
                if(text.split(" ").size > 1){
                    Log.d("TestandoIA","entrou iaativa: ${iaativa.get()}")
                    chamarIaSuper(text, true)
                    return true
                }
                else{
                    return false
                }
            }
            else {
                if(text.split(" ").size > 1) {
                    // Se IA está no modo pergunta (iaativa=false), mas o usuário ligou o switch,
                    // devemos ser mais permissivos. Se não foi comando de automação ou social (já verificado),
                    // envia para IA se tiver um tamanho mínimo.
                    Log.d("TestandoIA", "iaativa false, enviando para chamarIaSuper por ser switchIa.isChecked")
                    chamarIaSuper(text, true)
                    return true
                } else {
                    Log.d("TestandoIA","texto muito pequeno")
                    return false
                }
            }
        } else {
            Log.d("WhatsAppSession", "Ia desativada - verificando rotinas/comandos locais")
            return chamarIaSuper(text, true, skipAi = true)
        }
    }

    private fun iniciarTimerDesativacaoEscutando() {
        // Só iniciar timer se o reconhecimento estiver ativo
        if (!isListening) {
            Log.d("MainActivity", "Timer não iniciado - reconhecimento não está ativo")
            return
        }
        
        // Cancela qualquer timer anterior (reinicia o contador)
        resetescutandoJob?.cancel()

        // Cria um novo timer coroutine que desativa após 3 minutos sem TTS falar
        resetescutandoJob = CoroutineScope(Dispatchers.Default).launch {
            delay(3 * 60 * 1000L) // 3 minutos = 180.000 ms
            // Só desativar se ainda estiver escutando e reconhecimento estiver ativo
            if (escutando.get() && isListening) {
            escutando.set(false)
            updateAvatarSleepingState()
            runOnUiThread {
                    // Só mostrar "Dormindo" se o reconhecimento estiver ativo
                    if (isListening) {
                tvSpeechText.text = "Dormindo...->(Chame pelo Assistente)"
            }
                }
                Log.d("MainActivity", "⏱️ Assistente dormindo após 3 minutos sem TTS falar")
            }
        }
    }

    private fun iniciarTimerIaDesativacao() {
        // Cancela qualquer timer anterior (reinicia o contador)
        resetJob?.cancel()

        // Cria um novo timer coroutine que desativa após 4 minutos
        resetJob = lifecycleScope.launch(Dispatchers.Default) {
            delay(4 * 60 * 1000L) // 4 minutos = 240.000 ms
            iaativa.set(false)
            if (switchIa.isChecked){
                runOnUiThread {
                    tvSpeechText.text = "IA modo pergunta, para modo completo->(Chame pelo Assistente)"
                }
            }
            println("⏱️ IA desativada automaticamente após 4 minutos.")
        }
    }

    fun ComandoSocialEntity.getRespostasAleatoriasList(): List<String> {
        if (respostasAleatorias.isNullOrBlank()) return emptyList()
        return try {
            val gson = Gson()
            var jsonStr = respostasAleatorias.trim()

            // Remove aspas extras caso o JSON tenha sido salvo como string literal
            if (jsonStr.startsWith("\"") && jsonStr.endsWith("\"")) {
                jsonStr = jsonStr.substring(1, jsonStr.length - 1)
                    .replace("\\\"", "\"")
            }

            val dto = gson.fromJson(jsonStr, RespostasAleatoriasDto::class.java)
            dto.alternativas
        } catch (e: Exception) {
            Log.e("ComandosSociais", "Erro ao parsear respostasAleatorias: ${e.message}")
            emptyList()
        }
    }
    suspend fun comandosSocialGet(comando: String): Boolean {
        var passouSocial = false
        var cmmd = comando
        if (comando.contains("speaking:"))
            return false

        if(comando.contains("parcial:"))
            return false

        cmmd = cleanText(comando)
        for (comand in comandosLocais) {
            Log.d("Speech", "Comando local: ${comand.comando}")
            if (cmmd.contains(cleanText(comand.comando))) {

                lastDirectCommandTime = System.currentTimeMillis()
                passouComandos = true
                passou = true
                passouSocial = true
                val resposta = comand.getRespostaAleatoria() ?: comand.resposta
                Log.d("ComandosSociais", "Resposta escolhida: $resposta")

                speakTextFromService(resposta)
            }
        }
        return passouSocial
    }
    fun ComandoSocialEntity.getRespostaAleatoria(): String? {
        // Verificar se respostasAleatorias está vazio ou null
        if (respostasAleatorias.isNullOrBlank()) {
            return null
        }

        // Verificar se contém "Erro" antes de parsear (verificação rápida)
        val respostasAleatoriasLower = respostasAleatorias.lowercase()
        if (respostasAleatoriasLower.contains("erro")) {
            // Se contém "Erro", usar apenas a resposta padrão
            return null
        }

        var lista: MutableList<String> = mutableListOf()
        lista = getRespostasAleatoriasList() as MutableList<String>

        if (lista.isEmpty()) return null

        // Verificar se alguma alternativa contém "Erro" (verificação adicional)
        val isErro = lista.any { alternativa ->
            alternativa.isNotBlank() && 
            alternativa.trimStart().lowercase().startsWith("erro")
        }

        if (isErro) {
            // Se for erro, usar apenas a resposta padrão
            return null
        }

        // Adicionar a resposta padrão à lista
        lista.add(resposta)

        // Selecionar uma resposta aleatória
        return lista.random()
    }
    var musicControl = "nada"
    var ultimoDispositivo = "nada"
    var acaoUltimoDispositivo = "nada"
    var passouDisp = false
    private var ultimaMensagemProcessada: String? = null
    private var ultimoTempoMensagem: Long = 0
    private suspend fun execAutomacao(text: String): Boolean {
        var comando = text.lowercase().trim()
        if (text.contains("parcial:")){
            comando = text.replace("parcial:","").trim()
        }

        if (text.contains("speaking:")){
            comando = text.replace("speaking:","").trim()
        }




        var acao = ""
        var dispositivo = ""

        if (comando.contains("acender ")){
            acao = "ligar"
            dispositivo = comando.substringAfter("acender").trim()
        }
        if (comando.contains("acende ")){
            acao = "ligar"
            dispositivo = comando.substringAfter("acende").trim()
        }
        if (comando.contains("acenda ")){
            acao = "ligar"
            dispositivo = comando.substringAfter("acenda").trim()
        }
        //apagar
        if (comando.contains("apagar ")){
            acao = "desligar"
            dispositivo = comando.substringAfter("acender").trim()
        }
        if (comando.contains("apaga ")){
            acao = "desligar"
            dispositivo = comando.substringAfter("apaga").trim()
        }
        if (comando.contains("apague ")){
            acao = "desligar"
            dispositivo = comando.substringAfter("apague").trim()
        }

        //ligar
        if (comando.contains("liga ")){
            acao = "ligar"
            dispositivo = comando.substringAfter("liga").trim()
        }
        if (comando.contains("ligar ")){
            acao = "ligar"
            dispositivo = comando.substringAfter("ligar").trim()
        }
        if (comando.contains("ligue ")){
            acao = "ligar"
            dispositivo = comando.substringAfter("ligue").trim()
        }
        ///Desligar
        if (comando.contains("desliga ")){
            acao = "desligar"
            dispositivo = comando.substringAfter("desliga").trim()
        }
        if (comando.contains("desligar ")){
            acao = "desligar"
            dispositivo = comando.substringAfter("desligar").trim()
        }
        if (comando.contains("desligue ")){
            acao = "desligar"
            dispositivo = comando.substringAfter("desligue").trim()
        }

        //sair
        if (comando.contains("sai ")){
            comando = comando.replace("sai", "sair")
        }
        if (comando.contains("saia ") && comando.contains("frente")){
            comando = comando.replace("saia", "sair")
        }

        //fecha
        if (comando.contains("fecha ")){
            comando = comando.replace("fecha", "fechar")
        }
        if (comando.contains("feche ")){
            comando = comando.replace("feche", "fechar")
        }
        if (comando.contains("fexa ")){
            comando = comando.replace("fexa", "fechar")
        }
        if (comando.contains("fexe ")){
            comando = comando.replace("fexe", "fechar")
        }

        //abrir
        if (comando.contains("abre ")){
            comando = comando.replace("abre", "abrir")
        }
        if (comando.contains("abra ")){
            comando = comando.replace("abra", "abrir")
        }



        if (comando.contains("tocar ")
            ||comando.contains("toca ")
            ||comando.contains("toque ")

            ||comando.contains("para ") && comando.contains("musica")
            ||comando.contains("pare ") && comando.contains("musica")
            ||comando.contains("parar ") && comando.contains("musica")

            ||comando.contains("para ") && comando.contains("som")
            ||comando.contains("pare ") && comando.contains("som")
            ||comando.contains("parar ") && comando.contains("som")

            ||acao.contains("desligar ")
            && comando.contains("musica ")
            && comando.contains("som ")

            ||comando.contains("pausar ")
            ||comando.contains("pausa ")
            ||comando.contains("pause ")
            ){

            if(!text.lowercase().contains("parcial:")){
                if (comando.contains("tocar ")){
                    dispositivo = comando.split("tocar")[1]
                    acao = "tocar"
                }

                if (comando.contains("toca ")){
                    comando = comando.replace("toca", "tocar")
                    dispositivo = comando.split("toca")[1]
                    acao = "tocar"
                }
                if (comando.contains("toque ")){
                    comando = text.replace("toque", "tocar")
                    dispositivo = comando.split("toque")[1]
                    acao = "tocar"
                }
                if(comando.contains("parar ") && text.contains(" musica")
                    ||comando.contains("para ") && text.contains(" musica")
                    ||comando.contains("pare ") && text.contains(" musica")

                    ||comando.contains("parar ") && text.contains(" tocar")
                    ||comando.contains("para ") && text.contains(" tocar")
                    ||comando.contains("para ") && text.contains(" toca")

                    ||comando.contains("pare ") && text.contains(" tocar")
                    ||comando.contains("pare ") && text.contains(" toca")

                    || comando.contains("parar ") && text.contains(" o som")
                    || comando.contains("para ") && text.contains(" o som")
                    || comando.contains("pare ") && text.contains(" o som")

                    || comando.contains("desligar ") && text.contains(" musica")
                    || comando.contains("desliga ") && text.contains(" musica")
                    || comando.contains("desligue ") && text.contains(" musica")

                    || comando.contains("desligar ") && text.contains(" som")
                    || comando.contains("desliga ") && text.contains(" som")
                    || comando.contains("desligue ") && text.contains(" som")

                    ||comando.contains("pausar ") && text.contains(" musica")
                    ||comando.contains("pausa ") && text.contains(" musica")
                    ||comando.contains("pause ") && text.contains(" musica")

                    ||comando.contains("pausar ") && text.contains(" som")
                    ||comando.contains("pausa ") && text.contains(" som")
                    ||comando.contains("pause ") && text.contains(" som")
                ){
                    dispositivo = "musica"
                    acao = "parar"
                }

                if(executAcaoMusica(acao, dispositivo)){
                    musicControl = acao
                    acao = ""
                    dispositivo = ""
                    Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
                }

            }
            else{
                Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
                return false
            }
            Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
            return true
        }
        else if (
            comando.contains("mensagem") && !comando.contains("enviada") && !comando.contains("numero encontrado")
        ) {
            val txt = comando.trim()
            val agora = System.currentTimeMillis()
            val comandClean = cleanText(comando)

            if(!text.lowercase().contains("parcial:")){

                acao = "enviar"

                // Evita mensagens duplicadas em menos de 2 segundos
                if (ultimaMensagemProcessada == txt && (agora - ultimoTempoMensagem < 2000)) {

                    Log.d("WhatsappLog", "Ignorando duplicata: $txt")
                }

                if (switchWhatsapp.isChecked) {
                    // Usa o texto completo para extrair entidade (ex: "manda mensagem para Rebeca dizendo oi")
                    getEntitiesFromText(txt) { entities ->
                        if (entities != null && entities.isNotEmpty()) {
                            val pessoa = entities.first()
                            Log.d("WhatsappLog", "Entidade: $pessoa | Mensagem: $txt")

                            if(searchContato(pessoa, comandClean)){
                                Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
                                Log.e("WhatsappLog", "numero encontrado")

                            }
                        } else {
                            Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
                            Log.d("WhatsappLog", "Nenhuma entidade detectada.")
                        }
                    }

                } else {
                    speakTextFromService("O WhatsApp está desativado.")
                }
            }
            else{
                Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
                return false
            }


            ultimaMensagemProcessada = txt
            ultimoTempoMensagem = agora
            return true
        }
        if(executAcaoInterna(comando)){
            Log.d("AnalizaTextoMusica", "Comando executado: $comando")
            return true
        }
        else if(acionaDispositivos(acao, dispositivo, text)){
            Log.d("AnalizaTextoMusica", "Comando acionaDispositivos: $comando")
            Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
            return true
        }
        else if(otherCases(comando)){
            Log.d("TuyaComando", "executado antes de controlarDispositivoTuya")
            return true
        }
        else{
            // Não chamar controlarDispositivoEwelink aqui pois já foi chamado em processandoComandos
            // Isso evita processamento duplicado
            return false
        }
    }

    fun otherCases(comand: String): Boolean {

        var comando = comand.lowercase().trim()
        if (comand.contains("parcial:")){
            comando = comand.replace("parcial:","").trim()
        }

        if (comand.contains("speaking:")){
            comando = comand.replace("speaking:","").trim()
        }

        if (comand.contains("liga ")){
            comando = comand.replace("liga","ligar").trim()
        }

        if (comand.contains("ligue")){
            comando = comand.replace("ligue","ligar").trim()
        }

        if (comand.contains("desligue")){
            comando = comand.replace("desligue","desligar").trim()
        }

        if (comand.contains("desliga ")){
            comando = comand.replace("desliga","desligar").trim()
        }


        if (comando.contains("ligar") ||
            comando.contains("desligar") ||
            comando.contains("fechar") ||
            comando.contains("abre") ||
            comando.contains("abra") ||
            comando.contains("ativar") ||
            comando.contains("desativar") ||
            comando.contains("parar")
        ){
            if (comando.contains("ligar") && !comando.contains("desligar")
                || comando.contains("ligaria")){
                if(comando.contains("inteligencia")
                    ||comando.contains("ia")
                    ||comando.contains(" e ia")
                    ||comando.contains(" ai")
                    ||comando.contains(" e a")
                    ||comando.contains("ligaria")){
                    switchIa.isChecked = true
                    prefs.edit().putBoolean("ia_enabled", true).apply()

                    return true
                }

            }

            if (comando.contains("desligar")
                || comando.contains("desligaria")){
                if(comando.contains("inteligencia")
                    ||comando.contains("ia")
                    ||comando.contains(" e ia")
                    ||comando.contains(" ai")
                    ||comando.contains(" e a")
                    ||comando.contains("desligaria")){
                    switchIa.isChecked = false
                    prefs.edit().putBoolean("ia_enabled", false).apply()
                    return true
                }
            }

            if (comando.contains("ativar") && !comando.contains("desativa")
                || comando.contains("ativaria")){
                if(comando.contains("inteligencia")
                    ||comando.contains("ia")
                    ||comando.contains(" e ia")
                    ||comando.contains(" ai")
                    ||comando.contains(" e a")
                    ||comando.contains("ativaria")){
                    switchIa.isChecked = true
                    prefs.edit().putBoolean("ia_enabled", true).apply()

                    return true
                }

            }

            if (comando.contains("desativar")
                || comando.contains("desativaria")){
                if(comando.contains("inteligencia")
                    ||comando.contains("ia")
                    ||comando.contains(" e ia")
                    ||comando.contains(" ai")
                    ||comando.contains(" e a")
                    ||comando.contains("desativaria")){
                    switchIa.isChecked = false
                    prefs.edit().putBoolean("ia_enabled", false).apply()
                    return true
                }
            }

            if(comando.contains("parar") && comando.contains("musica")
                ||comando.contains("para") && comando.contains("musica")
                ||comando.contains("pare") && comando.contains("musica")
                ||comando.contains("pausa") && comando.contains("musica")
                ||comando.contains("pause") && comando.contains("musica")
                ){
                Log.d("AnalizaTextoMusica", "Dispositivo não encontrado musica musica.")
                stopPlayback()
                return true
            }
            else if(comando.contains("falar") || comando.contains("boca")) {
                Log.d("AnalizaTextoMusica", "[parar de falar]")
                return true
            }
            else{
                return false
            }
        }
        else{
            return false
        }
    }
    suspend fun acionaDispositivos(acao: String, dispositivo: String, comandComplet: String): Boolean {
        var response = false

        Log.d("acionaDispositivos","recebido acionaDispositivos:")
        Log.d("acionaDispositivos","Acao: $acao")
        Log.d("acionaDispositivos","dispositivo: $dispositivo")

        if (comandComplet.contains("parcial:") || comandComplet.contains("speaking:"))
            return false


        val comandoLower = "${acao.lowercase().trim()} ${dispositivo.lowercase().trim()}"

        // Comandos para dispositivos
        val comandosDispositivos = listOf(
            "ligar", "desligar", "acender", "apagar", "luz", "lâmpada", "interruptor",
            "liga", "desliga", "acende", "apaga","lampada",
            "ligue", "desligue", "acenda", "apague"
        )


        if (comandosDispositivos.contains(acao.lowercase().trim())){
            for (device in deviceList) {
                Log.d("acionaDispositivos","device in deviceList (dispositivo: $dispositivo)")
                Log.d("acionaDispositivos","device in deviceList (device.name: ${device.name})")
                if (dispositivo.lowercase().trim().contains(device.name.lowercase().trim())){
                    passouDisp = true
                    val sucesso = sendCommand(acao, device).await()
                    if (sucesso){
                        ultimoDispositivo = device.toString()
                        acaoUltimoDispositivo = acao
                        
                        // Criar mensagem de resposta para StarkSwitch
                        val mensagemResposta = when (acao.lowercase()) {
                            "ligar" -> "${device.name} ligado"
                            "desligar" -> "${device.name} desligado"
                            "abrir" -> "${device.name} aberto"
                            "fechar" -> "${device.name} fechado"
                            else -> "${device.name} ${acao}"
                        }
                        
                        // Enviar resposta via WebSocket com prefixo "toSoft:"
                        enviarRespostaWebSocket(mensagemResposta)
                        
                        response = true
                    }
                    else{
                        passouDisp = false
                        response = false
                    }
                }
                else{
                    response = false
                }
            }
        }

        return response
    }




    fun speakTextFromService(text: String) {
        if (avatarEnabled) {
            sendAvatarBeat()
        }
        val intent = Intent(this, FullDuplexAssistantAdvancedService::class.java)
        intent.action = "SPEAK_TEXT"
        intent.putExtra("text", text)

        startForegroundService(intent)
    }

    // Comandos de música
    private suspend fun  executAcaoMusica(acao: String,text: String): Boolean {

        val dispositivo = text.substringAfter("tocar").trim()
        var returning = false
        if (acao.contains("tocar")){
            if(starkCoins >= 0.2){
                erroMusicaFalado = false
//                val intent = Intent(this, FullDuplexAssistantAdvancedService::class.java).apply {
//                    action = FullDuplexAssistantAdvancedService.ACTION_START_LISTENING_AWS
//                }
//                startService(intent)
                playTrackByName(dispositivo)
                returning = true
            }
            else{
                speakTextFromService("Você não tem StarkCoins suficientes para tocar músicas.")
                returning = false
            }
        }

        if (acao.contains("parar") && dispositivo.contains("musica")){
            Log.i("AnalizaTexto", "acao recebida parar MUSICA acao: $acao dispositivo: $dispositivo")

            if(dispositivo.contains("musica") || dispositivo.contains("som") || dispositivo.contains("tocar") || dispositivo.contains("audio")){
                stopPlayback()
                returning = true
            }
            else{
                returning = false
            }
        }

        if (acao.contains("diminuir")){
            if(dispositivo.contains("musica") || dispositivo.contains("som") || dispositivo.contains("volume")  || dispositivo.contains("audio") || dispositivo.contains("mais")){
                spotifyService.decreaseVolume()
                returning = true
            }
            else{
                returning = false
            }
        }

        if (acao.contains("aumentar")){
            if(dispositivo.contains("musica") || dispositivo.contains("som") || dispositivo.contains("volume")  || dispositivo.contains("audio") || dispositivo.contains("mais")){
                spotifyService.increaseVolume()
                returning = true
            }
            else
            {
                returning = false
            }
        }
        return returning
    }


    private suspend fun executAcaoInterna(comand: String): Boolean {

        var comando = comand.lowercase().trim()
        if (comand.contains("parcial:")){
            comando = comand.replace("parcial:","").trim()
        }

        if (comand.contains("speaking:")){
            comando = comand.replace("speaking:","").trim()
        }

        comando = cleanText(comando)


        val currentTime = System.currentTimeMillis()

        if(comando.contains("limpar") && comando.contains("cache")
            ||comando.contains("limpar") && comando.contains("cash")
            ||comando.contains("limpar") && comando.contains("memoria")
            ||comando.contains("limpar") && comando.contains("dados")) {
            passouComandos = true
            cleanCache()

            return true
        }
        else if(comando == "atualizar"
            ||comando == "atualize"
            ||comando == "atualiza"
            ){
            passouComandos = true
            refreshData()

            return true
        }
        else if(comando == "otimizar"
            || comando == "otimiza"
            || comando == "otimize"
            || comando == "optimizar"
            ){
            passouComandos = true
            optimizePhone()

            return true
        }
        else if(comando.contains("sair") && comando.contains("frente")
            ||comando.contains("sair") && comando.contains("frente")
            ||comando.contains("sair") && comando.contains("frente")){
            passouComandos = true
            lastDirectCommandTime = System.currentTimeMillis()
            speakTextFromService("Tudo bem!")
            moveTaskToBack(true) // Minimiza o app

            return true
        }
        // Comando "Cadê você" (Trazer para frente)
        else if(comando.contains("cade") && comando.contains("voce")
            || comando.contains("onde") && comando.contains("esta") && comando.contains("voce")
            || comando.contains(nomeAssistent) && comando.contains("aparece")
            || comando.contains(nomeAssistent) && comando.contains("apareca")
            ){
            passouComandos = true
            lastDirectCommandTime = System.currentTimeMillis()

            speakTextFromService("Estou aqui!")

            val intent = Intent(this, MainActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_REORDER_TO_FRONT
            }
            startActivity(intent)

            return true
        }
        else if(comando.contains("abrir")){
            if(comando.contains("facebook")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime
                openAppOrBrowser("com.facebook.katana", "https://www.facebook.com", "Ok, abrindo o Facebook")

                return true

            }
            else if(comando.contains("whatsapp")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime
                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("Ok, abrindo o WhatsApp")
                    val packageManager = this.packageManager
                    val whatsappIntent = packageManager.getLaunchIntentForPackage("com.whatsapp")
                    val whatsappBusinessIntent = packageManager.getLaunchIntentForPackage("com.whatsapp.w4b")

                    when {
                        whatsappIntent != null -> {
                            startActivity(whatsappIntent)
                        }
                        whatsappBusinessIntent != null -> {
                            startActivity(whatsappBusinessIntent)
                        }
                        else -> {
                            speakTextFromService("Nenhum aplicativo WhatsApp encontrado.")
                        }
                    }
                }

                return true

            }

            else if(comando.contains("youtube") ||
                comando.contains("you tube") ||
                comando.contains("o youtube") ||
                comando.contains("o you tube")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime
                openAppOrBrowser("com.google.android.youtube", "https://www.youtube.com", "Ok, abrindo o YouTube")

                return true
            }

            else if(comando.contains("instagram")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime
                openAppOrBrowser("com.instagram.android", "https://www.instagram.com", "Ok, abrindo o Instagram")

                return true
            }

            else if(comando.contains("tiktok") ||
                comando.contains("tik tok")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime
                openAppOrBrowser("com.zhiliaoapp.musically", "https://www.tiktok.com", "Ok, abrindo o TikTok")

                return true

            }

            else if(comando.contains("mercadolivre")||
                comando.contains("mercado livre")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime //com.mercadolibre

                speakTextFromService("Ok, abrindo o Mercado Livre")
                val intent = Intent().apply {
                    setClassName("com.mercadolibre", "com.mercadolibre.splash.SplashActivity")
                    flags = Intent.FLAG_ACTIVITY_NEW_TASK
                }
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                startActivity(intent)

                return true
            }

            else if(comando.contains("shopee")||
                comando.contains("chopp")
            ){
                passouComandos = true
                lastDirectCommandTime = currentTime
                openAppOrBrowser("com.shopee.br", "https://shopee.com.br", "Ok, abrindo a Shopee")

                return true
            }
            else{
                if (!comando.contains("speaking:") && !comando.contains("parcial:")){
                    val acaofinal = "ligar"
                    for (device in deviceList) {

                        if (comand.contains(device.name)){
                            Log.d("acionaDispositivos","executAcaoInterna.device in deviceList ${device.name}")
                            val sucesso = sendCommand(acaofinal, device).await()
                            if (sucesso){
                                ultimoDispositivo = device.toString()
                                acaoUltimoDispositivo = "fechar"
                                
                                // Criar mensagem de resposta para StarkSwitch
                                val mensagemResposta = "${device.name} fechado"
                                enviarRespostaWebSocket(mensagemResposta)
                                
                                return true
                            }
                        }

                    }

                }
                else{
                    return false
                }

            }
            return false
        }

        // Comando "Fechar app"
        else if(comando.contains("fechar")
            || comando.contains("fecha")
            || comando.contains("feche")
            ){
            if(comando.contains("aplicativo")
            ){
                passouComandos = true
                lastDirectCommandTime = System.currentTimeMillis()
                speakTextFromService("Até logo!")

                // Delay para garantir que a fala termine:
                Handler(Looper.getMainLooper()).postDelayed({
                    finishAffinity()
                    finishAndRemoveTask()
                }, 1200) // 1.2 segundos

            }
            else if(comando.contains("youtube")
            ){
                passouComandos = true
                val intent = Intent(Intent.ACTION_MAIN)
                intent.addCategory(Intent.CATEGORY_HOME)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)

                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("ok youtube fechado")
                }
                return true
            }
            else if(comando.contains("instagram")
            ){
                passouComandos = true
                val intent = Intent(Intent.ACTION_MAIN)
                intent.addCategory(Intent.CATEGORY_HOME)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)

                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("ok instagram fechado")
                }
                return true
            }

            else if(comando.contains("tiktok") || comando.contains("tik tok")
            ){
                passouComandos = true
                val intent = Intent(Intent.ACTION_MAIN)
                intent.addCategory(Intent.CATEGORY_HOME)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)

                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("ok tiktok fechado")
                }
                return true
            }
            else if(comando.contains("mercadolivre") || comando.contains("mercado livre")
            ){
                passouComandos = true

                val intent = Intent(Intent.ACTION_MAIN)
                intent.addCategory(Intent.CATEGORY_HOME)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)

                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("ok mercado livre fechado")
                }
                return true
            }
            else if(comando.contains("shopee") ||
                comando.contains("chopp")
            ){
                passouComandos = true
                val intent = Intent(Intent.ACTION_MAIN)
                intent.addCategory(Intent.CATEGORY_HOME)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)

                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("ok shopee fechado")
                }
                return true
            }
            else if(comando.contains("facebook")
            ){
                passouComandos = true
                val intent = Intent(Intent.ACTION_MAIN)
                intent.addCategory(Intent.CATEGORY_HOME)
                intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)

                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("ok facebook fechado")
                }
                return true
            }
            else{
                if (!comand.lowercase().contains("speaking:") && !comand.lowercase().contains("parcial:")){
                    val acaofinal = "desligar"
                    Log.d("acionaDispositivos","executAcaoInterna.acaofinal desligar")
                    for (device in deviceList) {

                        if (comand.contains(device.name)){
                            Log.d("acionaDispositivos","executAcaoInterna.device in deviceList ${device.name}")
                            val sucesso = sendCommand(acaofinal, device).await()
                            if (sucesso){
                                ultimoDispositivo = device.toString()
                                acaoUltimoDispositivo = "fechar"
                                
                                // Criar mensagem de resposta para StarkSwitch
                                val mensagemResposta = "${device.name} fechado"
                                enviarRespostaWebSocket(mensagemResposta)
                                
                                return true
                            }
                        }

                    }
                }
                else{
                    return false
                }

            }
            return true
        }
        else {
            return false
        }
    }

    var passouComandos = false
    @Suppress("DEPRECATION")
    private fun processDirectCommands(
        cleanComand: String): Boolean {
        val comandNoCleanText = cleanComand.lowercase().trim()
        val comandCleanText = cleanText(cleanComand)
        val currentTime = System.currentTimeMillis()
        passouComandos = false


        return when {
            comandNoCleanText.contains("boa tarde")
                    || comandNoCleanText.contains("boa noite")
                    || comandNoCleanText.contains("bom dia")
                     -> {



                if (currentTime - lastDirectCommandTime < DIRECT_COMMAND_COOLDOWN) {
                    Log.d("Speech", "Ignorando comando direto por cooldown")
                }
                else {
                    if (!comandNoCleanText.contains("speaking")){
                        passouComandos = true

                        val horaAtual = SimpleDateFormat("HH:mm", Locale("pt", "BR")).format(Date())

                        val hora = horaAtual.split(":")[0].toInt()
                        if(hora >= 3 && hora < 12) {
                            if(comandNoCleanText.contains("bom dia")){
                                lastDirectCommandTime = currentTime

                                Handler(Looper.getMainLooper()).post {
                                    speakTextFromService("Bom dia!")
                                }
                                true
                            }
                            if(comandNoCleanText.contains("boa tarde")){
                                lastDirectCommandTime = currentTime

                                val responseCorrection = "você está adiantado, agora são ${horaAtual} da manhã. Bom dia!"
                                Handler(Looper.getMainLooper()).post {
                                    speakTextFromService(responseCorrection)
                                }
                                runOnUiThread {
                                    showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23")
                                }
                                true
                            }
                            if(comandNoCleanText.contains("boa noite")){
                                lastDirectCommandTime = currentTime

                                val responseCorrection = "você está adiantado, agora são ${horaAtual} da manhã. Bom dia!"
                                Handler(Looper.getMainLooper()).post {
                                    speakTextFromService(responseCorrection)
                                }
                                runOnUiThread {
                                    showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23")
                                }
                                true
                            }
                        }
                        else if(hora >= 12 && hora < 18) {

                            if(comandNoCleanText.contains("boa tarde")){
                                lastDirectCommandTime = currentTime
                                Handler(Looper.getMainLooper()).post {
                                    speakTextFromService("Boa tarde!")
                                }
                                runOnUiThread {
                                    showEmojiBaloes("\uD83D\uDD70\uFE0F\uD83D\uDE03")
                                }
                                true
                            }
                            if(comandNoCleanText.contains("bom dia")){
                                lastDirectCommandTime = currentTime
                                val responseCorrection = "você está atrazado, agora são ${horaAtual} da tarde. Boa Tarde"
                                Handler(Looper.getMainLooper()).post {
                                    speakTextFromService(responseCorrection)
                                }
                                runOnUiThread {
                                    showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23")
                                }
                                true
                            }
                            if(comandNoCleanText.contains("boa noite")){
                                lastDirectCommandTime = currentTime

                                val responseCorrection = "você está adiantado, agora são ${horaAtual} da tarde. Boa Tarde"
                                Handler(Looper.getMainLooper()).post {
                                    speakTextFromService(responseCorrection)
                                }
                                runOnUiThread {
                                    showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23")
                                }
                                true
                            }
                            true
                        }
                        else {
                            if(hora >= 0 && hora < 4){

                                if(comandNoCleanText.contains("boa noite")){
                                    lastDirectCommandTime = currentTime
                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService("Para mim agora é madrugada, ${horaAtual}. mas tudo bem! Boa noite!")
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23\uD83D\uDE02")
                                    }

                                    true
                                }
                                if(comandNoCleanText.contains("bom dia")){
                                    lastDirectCommandTime = currentTime
                                    val responseCorrection = "Para mim agora é madrugada, ${horaAtual}. mas tudo bem! bom dia!"

                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService(responseCorrection)
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23")
                                    }
                                    true
                                }
                                if(comandNoCleanText.contains("boa tarde")){
                                    lastDirectCommandTime = currentTime
                                    val responseCorrection = "Para mim agora é madrugada, ${horaAtual}. mas tudo bem, voce manda! boa tarde!"

                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService(responseCorrection)
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23\uD83D\uDE02")
                                    }
                                    true
                                }

                                if(comandNoCleanText.contains("boa madrugada")){
                                    lastDirectCommandTime = currentTime
                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService("Tenha uma ótima madrugada!")
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDE2A\uD83D\uDE34")
                                    }

                                    true
                                }
                            }else{
                                if(comandNoCleanText.contains("boa noite")){
                                    lastDirectCommandTime = currentTime
                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService("Boa noite!")
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDE2A\uD83D\uDE34")
                                    }
                                    true
                                }
                                if(comandNoCleanText.contains("bom dia")){
                                    lastDirectCommandTime = currentTime
                                    val responseCorrection = "você está atrazado, agora são ${horaAtual} da noite. Boa noite"
                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService(responseCorrection)
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDE01\uD83E\uDD23")
                                    }
                                    true
                                }
                                if(comandNoCleanText.contains("boa tarde")){
                                    lastDirectCommandTime = currentTime
                                    val responseCorrection = "você está atrazado, agora são ${horaAtual} da noite. Boa noite"
                                    Handler(Looper.getMainLooper()).post {
                                        speakTextFromService(responseCorrection)
                                    }
                                    runOnUiThread {
                                        showEmojiBaloes("\uD83D\uDD70\uFE0F\uD83D\uDE03")
                                    }
                                    true
                                }
                            }

                            true
                        }
                    }
                }

                true
            }

            comandCleanText.contains("como está o tempo")
                    || comandCleanText.contains("como esta o tempo")
                    || comandCleanText.contains("como o tempo esta")
                    || comandCleanText.contains("como ta o tempo")
                    || comandCleanText.contains("como esta o tempo")
                    || comandCleanText.contains("hoje vai chover")
                    || comandCleanText.contains("vai fazer frio hoje")
                    || comandCleanText.contains("vai fazer calor hoje")
                    || comandCleanText.contains("qual a previsao do tempo ")
                    || comandCleanText.contains("qual previsao do tempo")-> {
                passouComandos = true


                if (currentTime - lastDirectCommandTime < DIRECT_COMMAND_COOLDOWN) {
                    Log.d("Speech", "Ignorando comando direto por cooldown")
                }else{
                    Handler(Looper.getMainLooper()).post {
                        getUserCityName { cityName ->
                            Log.d("Weather", "Cidade do usuário: $cityName")
                            if (cityName != null) {
                                getWeatherForecast(cityName) { forecast ->
                                    if (forecast != null) {
                                        lastDirectCommandTime = currentTime
                                        speakTextFromService(forecast)
                                    } else {
                                        speakTextFromService("Não consegui obter a previsão do tempo para $cityName.")
                                    }
                                }
                            } else {
                                speakTextFromService("Não consegui localizar sua cidade no momento.")
                            }
                        }
                    }
                }

                true
            }

            comandCleanText.contains("que horas são")
                    || comandCleanText.contains("que horas sao")
                    || comandCleanText.contains("que horas e agora")
                    || comandCleanText.contains("quantas horas sao")
                    || comandCleanText.contains("que horas e agora")
                    || comandCleanText.contains("quantas horas") -> {
                if (!comandNoCleanText.contains("speaking:")){
                    if(comandCleanText == "que horas sao" || comandCleanText == "quantas horas"){
                        passouComandos = true
                        lastDirectCommandTime = currentTime
                        val horaAtual = SimpleDateFormat("HH:mm", Locale("pt", "BR")).format(Date())
                        Handler(Looper.getMainLooper()).post {
                            speakTextFromService("Agora são $horaAtual")
                        }
                        true
                    }
                }
                else{
                    passouComandos = true
                    lastDirectCommandTime = currentTime
                    val horaAtual = SimpleDateFormat("HH:mm", Locale("pt", "BR")).format(Date())
                    Handler(Looper.getMainLooper()).post {
                        speakTextFromService("Agora são $horaAtual")
                    }

                }
                true
            }
            comandCleanText.contains("que dia é hoje")
                    || comandCleanText.contains("que dia e hoje")
                    || comandCleanText.contains("qual a data de hoje")
                    || comandCleanText.contains("hoje e que dia")
                    || comandCleanText.contains("hoje é que dia") -> {
                passouComandos = true
                lastDirectCommandTime = currentTime
                val dataAtual = SimpleDateFormat("EEEE, d 'de' MMMM 'de' yyyy", Locale("pt", "BR")).format(Date())
                Handler(Looper.getMainLooper()).post {
                    speakTextFromService("Hoje é $dataAtual")
                }
                true
            }
            else ->{
                false
            }
        }
    }

    var ultimaRespostaIA = ""
    private suspend fun chamarIaSuper(
        pergunta: String,
        ePergunta: Boolean,
        skipAi: Boolean = false
    ): Boolean {
        if (pergunta.isBlank()) {
            Log.d("TestandoIA", "Comando vazio ou nulo recebido, ignorando chamada.")
            return true // Continua o fluxo sem erro
        }
        var naoPodeContinuar = false
        val defaltResponse = sessionManager.fetchDefaultResponse().toString()
        var defResp = ""
        var listDefaultResp = listOf<String>()

        if (!defaltResponse.isEmpty()){
            defResp = defaltResponse
            listDefaultResp = defResp.split(" ")
        }

        val comand = cleanText(pergunta)
        val ultRespia = cleanText(ultimaRespostaIA)

        val listWordPergunt = comand.split(" ")
        val listWordUltimaResp = ultRespia.split(" ")


        var worCount = 0
        // Lista de palavras irrelevantes para verificação de loop (stopwords)
        val stopWords = listOf("o", "a", "os", "as", "um", "uma", "de", "da", "do", "em", "na", "no", "por", "para", "com", "e", "que", "é", "eh", "do", "da", "dos", "das")

        for (word in listWordPergunt) {
            val w = word.trim()
            if (w.length > 2 && !stopWords.contains(w) && listWordUltimaResp.contains(w)) {
                worCount++
            }
            if (w.length > 2 && !stopWords.contains(w) && listDefaultResp.contains(w)) {
                 worCount++
            }
        }
        
        // Aumentado threshold para 8 e usando apenas palavras significativas
        if (worCount >= 8) {
             Log.d("TestandoIA", "Loop detectado: $worCount palavras coincidentes (Threshold: 8). Bloqueando resposta.")
             naoPodeContinuar = true
        }

        if (comand == " ")
            return false

        // Lista reduzida de bloqueios diretos para evitar falsos positivos
        if (comand == "a noite"
            ||comand == "boa noite"
            ||comand == "a tarde"
            ||comand == "boa tarde"
            ||comand == "bom dia"
            // Mantendo verificação de hora, mas permitindo variações mais complexas passarem
            ||(comand == "agora sao" && listWordPergunt.size < 3)
            ||(comand == "que horas sao" && listWordPergunt.size < 4)
            )
            return false


        if(comand.contains("o tempo esta")
            ||comand.contains("abrindo ")
            ||comand.contains("fechando ")
            // Removidos bloqueios genéricos demais
            //||comand.contains("a previsao ") 
            //||comand.contains("hoje e ")
            ||(defResp.length > 5 && comand.contains(defResp)) // Só bloqueia se conter a resposta padrão E ela for significativa
            )
            return false

        if (naoPodeContinuar) {
            return false
        }

        val api = ApiClient.getClient(this).create(UsuarioApi::class.java)
        val token = sessionManager.fetchAuthToken() ?: return false
        var person = "Descolado, carioca"


        Log.d("TestandoIA","entrou chamarIaSuper: $pergunta")
        val startTime = System.currentTimeMillis()

        // Verificar se personalidade está inicializada antes de usar
        if (this::personalidade.isInitialized && personalidade.isNotEmpty()){
            Log.d("TestandoIA", "iasuper personalidade: $personalidade")
            person = personalidade
        }

        return try {
            // Se o usuário autorizou uso de StarkCoins, enviar flag para o backend
            val dto = IaRequest(pergunta, person, ultimoContextoUser, ultimoContextoIA, iaUsandoStarkCoins, skipAi)
            val response = api.chamarSuperIA(dto)

            if (response.code() == 402) {
                var requiredCoins: Int? = null
                try {
                    val errJson = response.errorBody()?.string()
                    if (!errJson.isNullOrBlank()) {
                        val obj = JSONObject(errJson)
                        if (obj.has("requiredCoins")) requiredCoins = obj.optInt("requiredCoins")
                    }
                } catch (_: Exception) { }

                // Se já está usando StarkCoins e recebeu 402, significa que o saldo acabou
                if (iaUsandoStarkCoins) {
                    Log.d("TestandoIA", "⚠️ Saldo StarkCoins acabou - resetando flags")
                    iaLimitReached = true
                    iaUsandoStarkCoins = false
                    aguardandoLiberarConsumoStarkcoins = false
                    runOnUiThread {
                        isSwitchIaChangingProgrammatically = true
                        switchIa.isChecked = false
                        prefs.edit().putBoolean("ia_enabled", false).apply()
                        isSwitchIaChangingProgrammatically = false
                    }
                    speakTextFromService("Seus tokens e StarkCoins acabaram. Adicione fundos para continuar usando a IA.")
                    return false
                }

                // Primeiro 402: perguntar se quer usar StarkCoins
                iaLimitReached = true
                aguardandoLiberarConsumoStarkcoins = true
                Log.d("TestandoIA", "🔴 402 recebido - Flag aguardandoLiberarConsumoStarkcoins SETADA = $aguardandoLiberarConsumoStarkcoins")
                runOnUiThread {
                    isSwitchIaChangingProgrammatically = true // Marcar que estamos alterando programaticamente
                    switchIa.isChecked = false
                    prefs.edit().putBoolean("ia_enabled", false).apply()
                    isSwitchIaChangingProgrammatically = false // Desmarcar após alterar
                    // Verificar novamente após atualizar UI
                    Log.d("TestandoIA", "🔴 [UI Thread] Flag aguardandoLiberarConsumoStarkcoins após UI update = $aguardandoLiberarConsumoStarkcoins")
                }
                val msg = "Os tokens limite acabaram, você precisa usar seus StarkCoins para continuar usando a IA. Deseja reativar a IA?"
                speakTextFromService(msg)
                // Verificar novamente após speakTextFromService
                Log.d("TestandoIA", "🔴 [Após speakText] Flag aguardandoLiberarConsumoStarkcoins = $aguardandoLiberarConsumoStarkcoins")
                return false
            }

            if (response.isSuccessful) {
                val iaResponse = response.body()
                if (iaResponse != null) {
                        val resposta = iaResponse.resultado?.texto ?: ""
                        if (resposta.isBlank()) {
                            Log.d("TestandoIA", "⚠️ Resposta IA vazia - resetando flags")
                            iaLimitReached = false
                            aguardandoLiberarConsumoStarkcoins = false
                            ultimaRespostaIA = ""
                            // Se foi um comando local (ex: rotina), consideramos como handled true mesmo sem texto
                            return iaResponse.resultado?.hitResult == "LocalCommand"
                        }
                        val economy = iaResponse.economy ?: EconomicPayload(
                            planType = iaResponse.planType ?: "Free",
                            starkCoinBalance = iaResponse.starkCoinBalance ?: 0,
                            tokensConsumidosSemana = iaResponse.tokensConsumidosSemana ?: 0,
                            tokensSemanaMax = iaResponse.tokensSemanaMax ?: 0,
                            tokensRestantes = iaResponse.tokensRestantes ?: 0,
                            adsEnabled = iaResponse.adsEnabled ?: true,
                            agendamentosMax = iaResponse.agendamentosMax ?: 0,
                            agendamentosRestantes = iaResponse.agendamentosMax ?: 0,
                            rate = iaResponse.rate ?: 100
                        )
                        saldoStarkcoinsInt = economy.balance()
                        updatePlanLimitsCard(economy)
                        iaLimitReached = false
                        Log.d("TestandoIA", "⚠️ Resposta IA bem-sucedida - resetando aguardandoLiberarConsumoStarkcoins")
                        aguardandoLiberarConsumoStarkcoins = false

                        // TELEMETRIA: Enviar evento detalhado
                        val duration = (System.currentTimeMillis() - startTime).toInt()
                        pipelineActions.sendAiTelemetry(
                            textoOriginal = pergunta,
                            resultado = iaResponse.resultado?.hitResult ?: "UNKNOWN",
                            latenciaMs = duration,
                            chamouIaExterna = iaResponse.resultado?.modelo != "Aprendizado-Local",
                            similarityScore = iaResponse.resultado?.similarityScore,
                            aprendizadoTipo = iaResponse.resultado?.aprendizadoTipo,
                            aprendizadoId = iaResponse.resultado?.aprendizadoId
                        )

                        // Se o saldo zerou e estava usando StarkCoins, desativar
                        if (iaUsandoStarkCoins && saldoStarkcoinsInt <= 0) {
                            iaUsandoStarkCoins = false
                            runOnUiThread {
                                switchIa.isChecked = false
                                prefs.edit().putBoolean("ia_enabled", false).apply()
                            }
                            speakTextFromService("Seu saldo de StarkCoins acabou. Adicione fundos para continuar usando a inteligência.")
                        }
                    val novoSaldo = economy?.balance() ?: iaResponse.novoSaldo
                    Log.d("TestandoIA", "iasuper: $resposta")
                    // 🔹 Verifica saldo insuficiente
                    if (resposta.contains("saldo insuficiente", ignoreCase = true)) {
                        if (!ePergunta) {
                            ultimaRespostaIA = "Você não tem StarkCoins suficientes."
                            isIaResponsing.set(true)
                            speakTextFromService("Você não tem StarkCoins suficientes.")
                        }
                        false
                    } else {
                        // 🔹 Atualiza contexto local
                        ultimoContextoUser = pergunta
                        ultimoContextoIA = resposta

                        val textoParaFalar = resposta.removeEmojis()
                        val emojisParaMostrar = resposta.extractEmojis()
                        ultimaRespostaIA = textoParaFalar
                        speakTextFromService(textoParaFalar)

                        // Mostra os emojis com efeito na tela
                        if (emojisParaMostrar.isNotEmpty()) {
                            runOnUiThread {
                                showEmojiBaloes(emojisParaMostrar)
                            }
                        }


                        // 🔹 Atualiza UI do saldo
                        novoSaldo?.let { atualizarSaldoUI(it.toDouble()) }
                        economy?.let {
                            runOnUiThread {
                                tvStarkcoins.text = "${it.balance()} SC"
                            }
                        }

                        true
                    }
                } else {
                    Log.d("TestandoIA","Resposta vazia da API")
                    false
                }
            } else {
                val errorBody = response.errorBody()?.string()
                Log.d("TestandoIA", "Erro HTTP: ${response.code()} - $errorBody")
                
                if (!errorBody.isNullOrEmpty()) {
                    try {
                        val obj = JSONObject(errorBody)
                        if (obj.has("message")) {
                            val msg = obj.getString("message")
                            speakTextFromService(msg)
                        }
                    } catch (_: Exception) { }
                }
                false
            }
        } catch (e: Exception) {
            Log.d("TestandoIA", "Erro ao chamar IA", e)
            false
        }
    }

    fun showEmojiBaloes(emojis: String) {
        val rootLayout = findViewById<FrameLayout>(R.id.emojiContainer)

        if (rootLayout == null) {
            Log.e("EmojiDebug", "emojiContainer não encontrado!")
            return
        }

        Log.d("EmojiDebug", "Mostrando emojis: $emojis, Quantidade real: ${emojis.length}")

        // Extrai os emojis completos usando a nova função
        val emojiList = extractCompleteEmojis(emojis)
        Log.d("EmojiDebug", "Emojis extraídos: $emojiList, Quantidade: ${emojiList.size}")

        for (emoji in emojiList) {
            val textView = TextView(this).apply {
                text = emoji
                textSize = 42f
                setTextColor(Color.WHITE)
                setShadowLayer(4f, 2f, 2f, Color.BLACK)
                gravity = Gravity.CENTER
                alpha = 0.9f

                // IMPORTANTE: Use uma fonte que suporte emojis
                typeface = Typeface.create(Typeface.SANS_SERIF, Typeface.NORMAL)

                layoutParams = FrameLayout.LayoutParams(
                    FrameLayout.LayoutParams.WRAP_CONTENT,
                    FrameLayout.LayoutParams.WRAP_CONTENT
                )
            }

            rootLayout.addView(textView)

            // Posição inicial aleatória
            textView.x = (0..(rootLayout.width - 200)).random().toFloat()
            textView.y = rootLayout.height.toFloat()

            // Animação
            textView.animate()
                .translationYBy(-rootLayout.height * 1.5f)
                .alpha(0f)
                .setDuration(4000L + (0..2000).random())
                .setInterpolator(AccelerateInterpolator())
                .withEndAction {
                    rootLayout.removeView(textView)
                    Log.d("EmojiDebug", "Emoji removido: $emoji")
                }
                .start()

            Log.d("EmojiDebug", "Emoji adicionado: '$emoji' na posição X: ${textView.x}")
        }
    }
    fun String.extractEmojis(): String {
        return extractCompleteEmojis(this).joinToString("")
    }

    fun extractCompleteEmojis(text: String): List<String> {
        val emojiList = mutableListOf<String>()
        var i = 0
        while (i < text.length) {
            val codePoint = Character.codePointAt(text, i)
            val charCount = Character.charCount(codePoint)

            // Verifica se é um emoji
            if (isEmoji(codePoint)) {
                val emoji = text.substring(i, i + charCount)
                emojiList.add(emoji)
            }

            i += charCount
        }
        return emojiList
    }

    fun isEmoji(codePoint: Int): Boolean {
        return when (codePoint) {
            in 0x1F600..0x1F64F -> true // Emoticons
            in 0x1F300..0x1F5FF -> true // Misc Symbols and Pictographs
            in 0x1F680..0x1F6FF -> true // Transport and Map
            in 0x1F700..0x1F77F -> true // Alchemical Symbols
            in 0x1F780..0x1F7FF -> true // Geometric Shapes
            in 0x1F800..0x1F8FF -> true // Supplemental Arrows-C
            in 0x1F900..0x1F9FF -> true // Supplemental Symbols and Pictographs
            in 0x2600..0x26FF -> true   // Misc symbols
            in 0x2700..0x27BF -> true   // Dingbats
            in 0xFE00..0xFE0F -> true   // Variation Selectors
            in 0x1F1E6..0x1F1FF -> true // Flags
            else -> false
        }
    }

    fun String.removeEmojis(): String {
        val emojiRegex = Regex("[\\p{So}\\p{Cn}]")
        return this.replace(emojiRegex, "").trim()
    }

    private fun atualizarSaldoUI(novoSaldo: Double) {
        runOnUiThread {
            tvStarkcoins.text = String.format("%.2f SC", novoSaldo)
        }
    }

    private fun openAppOrBrowser(packageName: String, url: String, message: String) {
        Handler(Looper.getMainLooper()).post {
            speakTextFromService(message)

            try {
                // tenta abrir direto o app
                val intent = packageManager.getLaunchIntentForPackage(packageName)
                if (intent != null) {
                    intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                    startActivity(intent)
                } else {
                    // se não tiver app instalado, abre no navegador
                    val browserIntent = Intent(Intent.ACTION_VIEW, url.toUri())
                    browserIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                    startActivity(browserIntent)
                }
            } catch (_: Exception) {
                // fallback final para navegador
                val browserIntent = Intent(Intent.ACTION_VIEW, url.toUri())
                browserIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                startActivity(browserIntent)
            }
        }
    }

    private fun recarregarComandosLocais() {
        lifecycleScope.launch(Dispatchers.IO) {
            comandosLocais = comandoDao.getAll()
            withContext(Dispatchers.Main) {
                // Atualiza o TextView com a quantidade de comandos
                findViewById<TextView>(R.id.commandCount).text = comandosLocais.size.toString()
            }
        }

    }

    private fun observarComandosLocais() {
        comandosFlowJob?.cancel() // Cancela qualquer observação anterior

        comandosFlowJob = lifecycleScope.launch(Dispatchers.IO) {
            comandoDao.getAllFlow().collect { novosComandos ->
                comandosLocais = novosComandos
                Log.d("MainActivity", "Comandos locais atualizados: ${novosComandos.size} itens")
            }
        }
    }


    @Suppress("DEPRECATION")
    @SuppressLint("MissingPermission")
    private fun getUserCityName(callback: (String?) -> Unit) {
        Log.d("Weather", "entrou no getUserCityName")
        val locationManager = getSystemService(LOCATION_SERVICE) as LocationManager

        val isGpsEnabled = locationManager.isProviderEnabled(LocationManager.GPS_PROVIDER)
        val isNetworkEnabled = locationManager.isProviderEnabled(LocationManager.NETWORK_PROVIDER)

        if (!isGpsEnabled && !isNetworkEnabled) {
            Log.d("Weather", "GPS e rede desabilitados")
            callback(null)
            return
        }

        val locationListener = object : LocationListener {
            override fun onLocationChanged(location: Location) {
                // Correção: Executar geocoding em background com timeout
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val geocoder = Geocoder(this@MainActivity, Locale.getDefault())
                        val addresses = withTimeout(5000) {
                            geocoder.getFromLocation(location.latitude, location.longitude, 1)
                        }
                        Log.d("Weather", "Geocoding result: ${addresses?.firstOrNull()?.subAdminArea}")

                        var cityName = addresses?.firstOrNull()?.locality

                        if (cityName == null) {
                            cityName = addresses?.firstOrNull()?.subAdminArea
                        }

                        withContext(Dispatchers.Main) {
                            callback(cityName)
                        }
                    } catch (e: Exception) {
                        Log.e("Weather", "Geocoding error", e)
                        withContext(Dispatchers.Main) {
                            callback(null)
                        }
                    }
                }
                locationManager.removeUpdates(this)
            }

            override fun onStatusChanged(provider: String?, status: Int, extras: Bundle?) {}
            override fun onProviderEnabled(provider: String) {}
            override fun onProviderDisabled(provider: String) {}
        }

        if (isNetworkEnabled) {
            locationManager.requestSingleUpdate(LocationManager.NETWORK_PROVIDER, locationListener, null)
        } else {
            try {
                locationManager.requestSingleUpdate(LocationManager.GPS_PROVIDER, locationListener, null)
            } catch (e: Exception) {
                Toast.makeText(this, "Erro ao obter cidade localizaçao desativada:", Toast.LENGTH_SHORT).show()
                Log.e("Weather", "Erro ao obter cidade Erro ao obter cidade localizaçao desativada: ${e.message}")
            }
        }
    }



    private fun toggleSpeechRecognition() {

        if (isListening) {
            stopSpeechRecognition()
        } else {
            startSpeechRecognition()
        }
    }

    private fun checkPermissionsRecog() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            // Android 12+ precisa de FOREGROUND_SERVICE
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO)
                != PackageManager.PERMISSION_GRANTED
            ) {
                requestAudioPermissionLauncher.launch(Manifest.permission.RECORD_AUDIO)
                return
            }
        } else {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO)
                != PackageManager.PERMISSION_GRANTED
            ) {
                requestAudioPermissionLauncher.launch(Manifest.permission.RECORD_AUDIO)
                return
            }
        }
    }

    private fun startSpeechRecognition() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) !=
            PackageManager.PERMISSION_GRANTED
        ) {
            Toast.makeText(this, "Permissão de microfone necessária", Toast.LENGTH_SHORT).show()
            return
        }

        isListening = true
        btnMicrophone.setImageResource(R.drawable.ic_mic_on)

        val intent = Intent(this, FullDuplexAssistantAdvancedService::class.java).apply {
            action = FullDuplexAssistantAdvancedService.ACTION_START_LISTENING
        }
        startService(intent)
    }

    private fun stopSpeechRecognition() {
        isListening = false
        btnMicrophone.setImageResource(R.drawable.ic_mic_off)

        val intent = Intent(this, FullDuplexAssistantAdvancedService::class.java).apply {
            action = FullDuplexAssistantAdvancedService.ACTION_STOP_LISTENING
        }
        startService(intent)
    }

    private fun cancelClearText() {
        clearTextRunnable?.let {
            clearTextHandler.removeCallbacks(it)
            clearTextRunnable = null
        }
    }

    private fun getWeatherForecast(cityName: String, callback: (String?) -> Unit) {
        val apiKey = "f37079b14022c6246e2e2a771e109e34"
        val cityEncoded = URLEncoder.encode(cityName.lowercase(), "UTF-8")
        val url = "https://api.openweathermap.org/data/2.5/weather?q=$cityEncoded&appid=$apiKey&lang=pt_br&units=metric"

        val client = OkHttpClient()
        val request = Request.Builder().url(url).build()

        client.newCall(request).enqueue(object : Callback {
            override fun onFailure(call: Call, e: IOException) {
                Log.e("Weather", "Erro: ${e.message}")
                callback(null)
            }

            override fun onResponse(call: Call, response: Response) {
                if (response.isSuccessful) {
                    val body = response.body?.string()
                    val json = JSONObject(body ?: "")
                    val weatherArray = json.getJSONArray("weather")
                    val description = weatherArray.getJSONObject(0).getString("description")
                    val temp = json.getJSONObject("main").getDouble("temp")
                    val result = "O tempo em $cityName está $description com ${temp.toInt()} graus."

                    // Atualizar UI com dados do clima
                    runOnUiThread {
                        updateWeatherUI(cityName, temp.toInt(), description)
                    }

                    callback(result)
                } else {
                    callback(null)
                }
            }
        })
    }

    private fun isPositiveAnswer(text: String): Boolean {
        return text.contains("sim") ||
                text.contains("pode sim") ||
                text.contains("pode usar") ||
                text.contains("claro") ||
                text.contains("ativar") ||
                text.contains("reativar")
    }

    private fun isNegativeAnswer(text: String): Boolean {
        return text.contains("nao") ||
                text.contains("não") ||
                text.contains("pode nao") ||
                text.contains("não precisa") ||
                text.contains("não obrigado") ||
                text.contains("quero nao") ||                
                text.contains("não quero") ||
                text.contains("nao quero") ||
                text.contains("desativar") ||
                text.contains("cancelar")
    }

    private fun updatePlanLimitsCard(economy: EconomicPayload) {
        val activeColor = ContextCompat.getColor(this, R.color.jarvis_cyan)
        val inactiveColor = ContextCompat.getColor(this, R.color.jarvis_text_secondary)
        val primaryColor = ContextCompat.getColor(this, R.color.jarvis_text_primary)

        val planRaw = economy.planType?.trim() ?: ""
        val role = sessionManager.fetchUserRole()?.trim() ?: ""
        val plan = planRaw.lowercase()
        val roleLower = role.lowercase()
        // Considera Premium se planType indicar premium/nivel2/removal ads, se agendamentos for ilimitado, ou se role for UserNivel2
        val isPremium = plan.contains("premium") ||
                plan.contains("nivel2") ||
                plan.contains("removal") ||
                economy.agendamentosMax == -1 ||
                roleLower.contains("nivel2")

        runOnUiThread {
            tvPlanLimitsTitle.text = if (isPremium) "Plano atual: Premium" else "Plano atual: Free"

            tvPlanPremiumLine.setTextColor(if (isPremium) activeColor else inactiveColor)
            tvPlanPremiumLine.setTypeface(null, if (isPremium) Typeface.BOLD else Typeface.NORMAL)

            tvPlanFreeLine.setTextColor(if (!isPremium) activeColor else inactiveColor)
            tvPlanFreeLine.setTypeface(null, if (!isPremium) Typeface.BOLD else Typeface.NORMAL)

            // título em cor primária
            tvPlanLimitsTitle.setTextColor(primaryColor)
        }
    }
    private fun mostrarDialogLimiteIa() {
        runOnUiThread {
            AlertDialog.Builder(this)
                .setTitle("Limite atingido")
                .setMessage("Você atingiu os limites do seu plano. Deseja usar seu saldo de StarkCoins para continuar usando a Inteligência?")
                .setPositiveButton("Usar StarkCoins para Inteligência") { _, _ ->
                    iaLimitReached = false
                    aguardandoLiberarConsumoStarkcoins = false
                    iaUsandoStarkCoins = true
                    switchIa.isChecked = true
                    prefs.edit().putBoolean("ia_enabled", true).apply()
                    speakTextFromService("IA reativada usando StarkCoins.")
                }
                .setNegativeButton("Cancelar") { _, _ ->
                    iaLimitReached = true
                    switchIa.isChecked = false
                    prefs.edit().putBoolean("ia_enabled", false).apply()
                    speakTextFromService("IA permanece desativada.")
                }
                .show()
        }
    }

    // Função para atualizar UI do card de clima
    private fun updateWeatherUI(cityName: String, temp: Int, description: String) {
        try {
            val cardWeather = findViewById<androidx.cardview.widget.CardView>(R.id.cardWeather)
            val weatherCity = findViewById<TextView>(R.id.weatherCity)
            val weatherTemp = findViewById<TextView>(R.id.weatherTemp)
            val weatherDescription = findViewById<TextView>(R.id.weatherDescription)
            
            cardWeather?.visibility = View.VISIBLE
            weatherCity?.text = cityName
            weatherTemp?.text = "${temp}°C"
            weatherDescription?.text = description.replaceFirstChar { if (it.isLowerCase()) it.titlecase(Locale.getDefault()) else it.toString() }
        } catch (e: Exception) {
            Log.e("Weather", "Erro ao atualizar UI do clima: ${e.message}")
        }
    }

    // Função para carregar previsão do tempo automaticamente ao iniciar
    private fun loadWeatherForecast() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) 
            == PackageManager.PERMISSION_GRANTED) {
            getUserCityName { cityName ->
                if (cityName != null) {
                    getWeatherForecast(cityName) { forecast ->
                        // Forecast já atualiza a UI automaticamente
                    }
                }
            }
        }
    }

    // Funções para controlar ondas sonoras
    private fun showSoundWaves() {
        try {
            val container = findViewById<FrameLayout>(R.id.soundWavesContainer)
            if (container != null && soundWaveView == null) {
                soundWaveView = SoundWaveView(this)
                container.addView(soundWaveView)
                // Limitar tamanho - apenas parte inferior da tela, não tampar tudo
                val params = FrameLayout.LayoutParams(
                    FrameLayout.LayoutParams.MATCH_PARENT,
                    (resources.displayMetrics.heightPixels * 0.3f).toInt() // 30% da altura
                ).apply {
                    gravity = android.view.Gravity.BOTTOM
                }
                soundWaveView?.layoutParams = params
                container.visibility = View.VISIBLE
                soundWaveView?.startAnimation()
            }
        } catch (e: Exception) {
            Log.e("SoundWaves", "Erro ao mostrar ondas sonoras: ${e.message}")
        }
    }

    private fun hideSoundWaves() {
        try {
            val container = findViewById<FrameLayout>(R.id.soundWavesContainer)
            soundWaveView?.stopAnimation()
            container?.removeAllViews()
            container?.visibility = View.GONE
            soundWaveView = null
        } catch (e: Exception) {
            Log.e("SoundWaves", "Erro ao ocultar ondas sonoras: ${e.message}")
        }
    }

    // Função para mostrar diálogo de entrada de texto
    private fun showTextInputDialog() {
        val dialogView = layoutInflater.inflate(R.layout.dialog_text_input, null)
        val editText = dialogView.findViewById<com.google.android.material.textfield.TextInputEditText>(R.id.editTextInput)
        val btnSend = dialogView.findViewById<android.widget.Button>(R.id.btnSend)
        
        val dialog = android.app.AlertDialog.Builder(this)
            .setView(dialogView)
            .setCancelable(true)
            .create()
        
        // Fechar ao clicar fora
        dialog.setCanceledOnTouchOutside(true)
        
        // Enviar ao clicar no botão
        btnSend.setOnClickListener {
            val text = editText.text.toString().trim()
            if (text.isNotEmpty()) {
                // Simular broadcast como se fosse do FullDuplexAssistantService
                val intent = Intent(FullDuplexAssistantAdvancedService.BROADCAST_SPEECH_RESULT).apply {
                    putExtra(FullDuplexAssistantAdvancedService.EXTRA_RECOGNIZED_TEXT, text)
                }
                LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
                
                // Fechar diálogo
                dialog.dismiss()
            }
        }
        
        // Enviar ao pressionar Enter no teclado
        editText.setOnEditorActionListener { _, actionId, _ ->
            if (actionId == android.view.inputmethod.EditorInfo.IME_ACTION_SEND) {
                val text = editText.text.toString().trim()
                if (text.isNotEmpty()) {
                    val intent = Intent(FullDuplexAssistantAdvancedService.BROADCAST_SPEECH_RESULT).apply {
                        putExtra(FullDuplexAssistantAdvancedService.EXTRA_RECOGNIZED_TEXT, text)
                    }
                    LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
                    dialog.dismiss()
                }
                true
            } else {
                false
            }
        }
        
        // Mostrar teclado automaticamente
        dialog.setOnShowListener {
            editText.requestFocus()
            val imm = getSystemService(Context.INPUT_METHOD_SERVICE) as android.view.inputmethod.InputMethodManager
            imm.showSoftInput(editText, android.view.inputmethod.InputMethodManager.SHOW_IMPLICIT)
        }
        
        dialog.show()
    }

    override fun onResume() {
        super.onResume()
        
        // Marcar usuário como online quando o app volta ao foreground
        if (isOnline()) {
            lifecycleScope.launch(Dispatchers.IO) {
                setUserOnline()
            }
        }
        
        // Verificar deep link quando o app retorna do background
        intent?.data?.let { uri ->
            if (uri.scheme == "starkaid" && uri.host == "payment") {
                val fundsStatus = uri.getQueryParameter("funds")
                Log.d("Payment", "Deep link de pagamento detectado no onResume: funds=$fundsStatus")
                
                if (fundsStatus == "success") {
                    Toast.makeText(this, "Pagamento confirmado! Atualizando saldo...", Toast.LENGTH_SHORT).show()
                    Handler(Looper.getMainLooper()).postDelayed({
                        getStarkcoins()
                    }, 2000)
                } else if (fundsStatus == "cancel") {
                    Toast.makeText(this, "Pagamento cancelado", Toast.LENGTH_SHORT).show()
                }
                
                // Limpar o intent para evitar processamento duplicado
                intent = Intent()
            }
        }
        
        val currentTime = System.currentTimeMillis()

        // Registrar receiver de conectividade
        if (!Settings.canDrawOverlays(this)) {
            // Permissão ainda não concedida
            Toast.makeText(this, "Permissão de sobreposição necessária", Toast.LENGTH_SHORT).show()
        }



        // Incrementar contador de atividade se passou tempo suficiente
        if (currentTime - lastResumeTime > 10000) {
            adCounter = sessionManager.fetchAdCounter()
            adCounter++
            sessionManager.saveAdCounter(adCounter)
            Log.d("UnityAds", "Contador incrementado adcounter: $adCounter")

            if (adCounter >= 7) {
                sessionManager.saveAdCounter(0)
            }

            lifecycleScope.launch {
                // Sincronizar configurações do banco para SessionManager (apenas se não existir no SessionManager)
                val nomeBanco = getAssistantName()
                val respostaBanco = getDefaultResponse()
                val persBanco = getAssistantPerson()
                
                // Só salvar no SessionManager se:
                // 1. O valor do banco não for o padrão "Assistente"
                // 2. E não existir valor no SessionManager (ou for "Assistente")
                val nomeAtual = sessionManager.fetchAssistantName()
                if (nomeBanco.isNotEmpty() 
                    && !nomeBanco.equals("Assistente", ignoreCase = true)
                    && (nomeAtual == null || nomeAtual.isBlank() || nomeAtual.equals("Assistente", ignoreCase = true))) {
                    sessionManager.saveAssistantName(nomeBanco)
                    Log.d("MainActivity", "✅ Nome sincronizado do banco para SessionManager: $nomeBanco")
                } else if (nomeAtual != null && !nomeAtual.equals("Assistente", ignoreCase = true)) {
                    // Se já existe um nome válido no SessionManager, usar ele
                    nomeAssistent = nomeAtual.lowercase().trim()
                    Log.d("MainActivity", "✅ Nome do assistente recarregado do SessionManager: $nomeAssistent")
                } else if (nomeBanco.isNotEmpty() && !nomeBanco.equals("Assistente", ignoreCase = true)) {
                    nomeAssistent = nomeBanco.lowercase().trim()
                    Log.d("MainActivity", "✅ Nome do assistente recarregado do banco: $nomeAssistent")
                }
                
                val respostaAtual = sessionManager.fetchDefaultResponse()
                if (respostaBanco.isNotEmpty() && respostaAtual == null) {
                    sessionManager.saveDefaultResponse(respostaBanco)
                }
                
                val persAtual = sessionManager.fetchAssistantPerson()
                if (persBanco.isNotEmpty() && persAtual == null) {
                    sessionManager.saveAssistantPerson(persBanco)
                }
            }

            getStarkcoins()
        }

        lastResumeTime = currentTime

        // Recarrega o anúncio se necessário apenas se estiver online
        if (isOnline() && System.currentTimeMillis() - lastAdShowTime > MIN_TIME_BETWEEN_ADS) {
            // Verifica se Unity Ads está inicializado antes de carregar
            if (UnityAds.isInitialized) {
                loadInterstitialAd()
            } else {
                // Se não estiver inicializado, tenta inicializar
                initializeUnityAds()
            }
        }
        // Mostrar anúncio se atingiu a frequência
        showAdIfReady()

        recarregarComandosLocais()


        FloatingButtonServiceInstance?.hideButton()
        verificarDisparosPendentes()
        loadDevices()
        preCarregarDispositivos()
        adsGet()
        
        // Carregar previsão do tempo
        loadWeatherForecast()

        // Recarrega os serviços se necessário (validação do token para SignalR)
        if (!servicesInitialized && !sessionManager.fetchAuthToken().isNullOrEmpty()) {
            initializeServicesAfterValidation()
        }
    }

    override fun onStart() {
        super.onStart()

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            LocalBroadcastManager.getInstance(this)
                .registerReceiver(recogReceiver, IntentFilter("starkaid.UPDATE_RECOG"))
        }

        if (!isSpeechReceiverRegistered) {
            LocalBroadcastManager.getInstance(this).registerReceiver(
                speechReceiver,
                IntentFilter(FullDuplexAssistantAdvancedService.BROADCAST_SPEECH_RESULT)
            )
            isSpeechReceiverRegistered = true
        }
        
        // Registrar receiver de TTS
        val ttsFilter = IntentFilter().apply {
            addAction(FullDuplexAssistantAdvancedService.BROADCAST_TTS_STARTED)
            addAction(FullDuplexAssistantAdvancedService.BROADCAST_TTS_STOPPED)
            addAction(FullDuplexAssistantAdvancedService.BROADCAST_TTS_AUDIO_LEVEL)
        }
        LocalBroadcastManager.getInstance(this).registerReceiver(ttsReceiver, ttsFilter)
    }

    override fun onStop() {
        super.onStop()

        //unregisterReceiver(speechReceiver)
        // Registrar tempo de fechamento
        sessionManager.saveLastCloseTime(System.currentTimeMillis())
    }

    private val startupTime = System.currentTimeMillis()
    override fun onPause() {
        super.onPause()

        // Marcar usuário como offline quando o app vai para background
        if (isOnline()) {
            lifecycleScope.launch(Dispatchers.IO) {
                setUserOffline()
            }
        }

        val totalTime = System.currentTimeMillis() - startupTime
        Log.d("Perf", "Activity lifetime: $totalTime ms")
        FloatingButtonServiceInstance?.showButton()
    }


    override fun onDestroy() {
        super.onDestroy()

        // Fechar conexão do Hub de dispositivos ESP
        try {
            espHubConnection?.stop()?.blockingAwait()
            espHubConnection = null
            Log.d("ESP_HUB_MAIN", "HubConnection de dispositivos ESP fechado")
        } catch (e: Exception) {
            Log.e("ESP_HUB_MAIN", "Erro ao fechar HubConnection", e)
        }

        LocalBroadcastManager.getInstance(this).unregisterReceiver(recogReceiver)

        try {
            LocalBroadcastManager.getInstance(this).unregisterReceiver(speechReceiver)
        } catch (_: Exception) {
            // Receiver não estava registrado
        }
        
        try {
            LocalBroadcastManager.getInstance(this).unregisterReceiver(ttsReceiver)
        } catch (_: Exception) {
            // Receiver não estava registrado
        }
        
        soundWaveView?.stopAnimation()
        soundWaveView = null

        avatarWebView?.let { wv ->
            try {
                (wv.parent as? ViewGroup)?.removeView(wv)
            } catch (_: Exception) {
            }
            try {
                wv.destroy()
            } catch (_: Exception) {
            }
        }
        avatarWebView = null

        FullDuplexAssistantAdvancedService.stop(this)
        comandosFlowJob?.cancel()
        udpListenerJob?.cancel()
        adHandler.removeCallbacksAndMessages(null)

        // Cancelar qualquer tentativa de repetição de anúncio
        adRetryRunnable?.let { adHandler.removeCallbacks(it) }
        mInterstitialAd = null

        // Parar serviços apenas se foram inicializados
        if (servicesInitialized) {
            hubService.stop()
            wsManager.stop()
        }

        val stopIntent = Intent(this, FloatingButtonService::class.java)
        stopService(stopIntent)


        cancelClearText()
        speechRecognizer = null
        audioManager.ringerMode = previousRingerMode
        SessionExpiredHandler.onSessionExpired = null
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




    private fun checkAndUpdateDeviceStatus(device: Device) {
        val authToken = sessionManager.fetchAuthToken() ?: return
        val apiKey = sessionManager.fetchApiKey() ?: return

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val api = retrofit.create(StatusApi::class.java)
                val response: retrofit2.Response<DeviceStatus> = api.getStatus(
                    device.id,
                    "Bearer $authToken",
                    apiKey
                )

                if (response.isSuccessful) {
                    response.body()?.let { status ->
                        Log.e("STATE_UPDATE", "verificar status: ${status.status}")

                        val resp01 = status.status
                        var stat = false
                        if (resp01.contains("resposta:")) {
                            if (resp01.split("resposta:")[1] == "ja_ligado") {
                                stat = true
                            }
                            if (resp01.split("resposta:")[1] == "ja_desligado") {
                                stat = false
                            }
                        }
                        if (resp01.contains("status:")) {
                            if (resp01.split("status:")[1] == "ligado") {
                                stat = true
                            }
                            if (resp01.split("status:")[1] == "desligado") {
                                stat = false
                            }
                        }

                        if (resp01 == "conectado") {
                            stat = false
                        }

                        val isOn = stat
                        val position = deviceList.indexOfFirst { it.id == device.id }
                        if (position != -1) {
                            deviceList[position].isOn = isOn

                            withContext(Dispatchers.Main) {
                                deviceAdapter.notifyItemChanged(position)
                            }
                        }
                    }
                } else {
                    Log.e("SignalR", "Erro status ${device.name}: ${response.code()}")
                }
            } catch (e: Exception) {
                Log.e("SignalR", "Exceção status ${device.name}: ${e.message}")
            }
        }
    }

    override fun onDeviceClick(device: Device) {
        lifecycleScope.launch {
            try {
                // Verificar status do MQTT primeiro
                val isMqttConnected = withContext(Dispatchers.IO) {
                    checkMqttStatus()
                }

                if (!isMqttConnected) {
                    Toast.makeText(this@MainActivity, "Serviço MQTT offline. Comando não enviado.", Toast.LENGTH_SHORT).show()
                    return@launch
                }

                // Se MQTT está online, verificar status do dispositivo
                checkDeviceStatus(device)
            } catch (e: Exception) {
                Log.e("SignalR", "Erro ao verificar MQTT: ${e.message}")
                Toast.makeText(this@MainActivity, "Erro ao verificar status do serviço", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun checkDeviceStatus(device: Device) {
        val authToken = sessionManager.fetchAuthToken() ?: return
        val apiKey = sessionManager.fetchApiKey() ?: return

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val retrofit = ApiClient.getClient(this@MainActivity)
                val api = retrofit.create(StatusApi::class.java)
                val response: retrofit2.Response<DeviceStatus> = api.getStatus(
                    device.id,
                    "Bearer $authToken",
                    apiKey
                )

                if (response.isSuccessful) {
                    response.body()?.let { status ->
                        val resp01 = status.status
                        if (resp01.contains("resposta:")) {
                            if (resp01.split("resposta:")[1] == "ja_ligado") {
                                val sucesso = sendCommand("desligar", device).await()
                                if (sucesso){
                                    ultimoDispositivo = device.toString()
                                    acaoUltimoDispositivo = "desligar"
                                    enviarRespostaWebSocket("${device.name} desligado")
                                }
                            }
                            if (resp01.split("resposta:")[1] == "ja_desligado") {

                                val sucesso = sendCommand("ligar", device).await()
                                if (sucesso){
                                    ultimoDispositivo = device.toString()
                                    acaoUltimoDispositivo = "ligar"
                                    enviarRespostaWebSocket("${device.name} ligado")
                                }
                            }
                        }
                        if (resp01.contains("status:")) {
                            if (resp01.split("status:")[1] == "ligado") {
                                val sucesso = sendCommand("desligar", device).await()
                                if (sucesso){
                                    ultimoDispositivo = device.toString()
                                    acaoUltimoDispositivo = "desligar"
                                    enviarRespostaWebSocket("${device.name} desligado")
                                }
                            }
                            if (resp01.split("status:")[1] == "desligado") {
                                val sucesso = sendCommand("ligar", device).await()
                                if (sucesso){
                                    ultimoDispositivo = device.toString()
                                    acaoUltimoDispositivo = "ligar"
                                    enviarRespostaWebSocket("${device.name} ligado")
                                }
                            }
                        }
                        if (resp01 == "conectado") {
                            val sucesso = sendCommand("ligar", device).await()
                            if (sucesso){
                                ultimoDispositivo = device.toString()
                                acaoUltimoDispositivo = "ligar"
                            }
                        }

                        Log.e("STATE_UPDATE", "verificar status STATE_UPDATE: ${status.status}")

                    }
                } else {
                    val sucesso = sendCommand("ligar", device).await()
                    if (sucesso){
                        ultimoDispositivo = device.toString()
                        acaoUltimoDispositivo = "ligar"
                    }
                }
            } catch (e: Exception) {
                Log.e("SignalR", "Exceção ao verificar status: ${e.message}")
                val sucesso = sendCommand("ligar", device).await()
                if (sucesso){
                    ultimoDispositivo = device.toString()
                    acaoUltimoDispositivo = "ligar"
                }
            }
        }
    }

    private suspend fun checkMqttStatus(): Boolean {
        return try {
            val retrofit = ApiClient.getClient(this@MainActivity)
            val api = retrofit.create(HealthCheckApi::class.java)
            val response = api.checkMqttStatus()
            response.isSuccessful && response.body()?.status == "OK"
        } catch (_: Exception) {
            false
        }
    }
    private fun sendCommand(command: String, device: Device): Deferred<Boolean> {
        return lifecycleScope.async(Dispatchers.IO) {
            commandMutex.withLock {
                if (checkApiHealth() && sendViaApi(device, command)) {
                    return@async true
                }
                if (isOnWifi() && !sendUdpCommand(device, command)) {
                    sendUdpBroadcast(device, command)
                    return@async true
                }
                return@async false
            }
        }
    }

    private suspend fun checkApiHealth(): Boolean {
        val authToken = sessionManager.fetchAuthToken() ?: return false
        val apiKey = sessionManager.fetchApiKey() ?: return false

        return try {
            val retrofit = ApiClient.getClient(this@MainActivity)
            val api = retrofit.create(HealthApi::class.java) // Você precisa criar esta interface

            val response = api.checkMqttHealth("Bearer $authToken", apiKey)
            response.isSuccessful && response.body()?.status == "OK"
        } catch (e: Exception) {
            Log.e("SignalR", "Falha no health check da API: ${e.message}")
            false
        }
    }

    private suspend fun sendViaApi(device: Device, command: String): Boolean {
        val authToken = sessionManager.fetchAuthToken() ?: return false
        val apiKey = sessionManager.fetchApiKey() ?: return false

        return try {
            val retrofit = ApiClient.getClient(this@MainActivity)
            val api = retrofit.create(CommandApi::class.java)

            val response = api.sendCommand(
                CommandRequest(device.id, command),
                "Bearer $authToken",
                apiKey
            )

            if (response.isSuccessful) {
                response.body()?.let { commandResponse ->
                    Log.d("API", "✅ Message: ${commandResponse.message}, Topic: ${commandResponse.topic}")
                }
                true
            } else {
                Log.e("SignalR", "API Erro ao enviar comando: ${response.code()}")
                false
            }
        } catch (e: Exception) {
            Log.e("SignalR", "API Exceção ao enviar comando: ${e.message}")
            false
        }
    }


    override fun onDeviceStatusUpdated(deviceId: String, statusResponse: String) {
        runOnUiThread {
            updateDeviceState(deviceId, statusResponse)
            Log.d("SignalR", "Device $deviceId -> $statusResponse")
        }
    }

    override fun onDeviceCommandReceived(deviceId: String, command: String) {
        runOnUiThread {
            Log.d("SignalR", "Comando recebido $command para $deviceId")
        }
    }

    override fun onSuporteComandoReceived(comando: String) {
        runOnUiThread {
            Log.d("Suporte", "Comando de suporte recebido: $comando")
            
            val acao = if (comando.startsWith("suporteToApp:")) {
                // Se começar com "suporteToApp:", remover o prefixo
                comando.replace("suporteToApp:", "")
            } else {
                // Comando direto (ex: "limparcache", "logout")
                comando
            }
            
            executarAcaoSuporte(acao)
        }
    }

    override fun onOpenUrl(url: String) {
        runOnUiThread {
            try {
                val intent = android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse(url))
                intent.flags = android.content.Intent.FLAG_ACTIVITY_NEW_TASK
                startActivity(intent)
            } catch (e: Exception) {
                Log.e("SignalR", "Erro ao abrir URL: $url", e)
            }
        }
    }

    override fun onNotificationReceived(titulo: String, mensagem: String) {
        Log.d("HubListener", "Notificação recebida: $titulo - $mensagem")
        runOnUiThread {
            // Se for resposta da IA, usar TTS
            if (titulo == "IA" || titulo == "StarkAid") {
                lifecycleScope.launch {
                    voiceSynthesizer.speak(mensagem)
                }
                
                // Exibir no TextView de fala também
                tvSpeechText.text = mensagem
            }

            val builder = androidx.core.app.NotificationCompat.Builder(this, "starkaid_general_channel")
                .setSmallIcon(R.drawable.logo02)
                .setContentTitle(titulo)
                .setContentText(mensagem)
                .setPriority(androidx.core.app.NotificationCompat.PRIORITY_DEFAULT)
                .setAutoCancel(true)

            val nm = getSystemService(android.content.Context.NOTIFICATION_SERVICE) as android.app.NotificationManager
            nm.notify(System.currentTimeMillis().toInt(), builder.build())
            
            Toast.makeText(this, "$titulo: $mensagem", Toast.LENGTH_LONG).show()
        }
    }

    override fun onAssistantCommandReceived(comando: String) {
        Log.d("HubListener", "Comando de assistente recebido da rotina: $comando")
        lifecycleScope.launch {
            processCommandViaPipeline(comando)
        }
    }

    private fun executarAcaoSuporte(acao: String) {
        when (acao.lowercase()) {
            "limparcache" -> {
                limparCacheApp()
            }
            "limpardados" -> {
                limparDadosApp()
            }
            "logout" -> {
                logout()
            }
            "atualizardados" -> {
                atualizarDadosApp()
            }
            else -> {
                Log.w("Suporte", "Ação desconhecida: $acao")
            }
        }
    }

    private fun limparCacheApp() {
        try {
            // Limpar cache do app
            val cacheDir = cacheDir
            if (cacheDir.exists()) {
                deleteRecursive(cacheDir)
                cacheDir.mkdirs()
            }
            
            // Limpar cache de SharedPreferences temporários
            val prefs = getSharedPreferences("temp_cache", Context.MODE_PRIVATE)
            prefs.edit().clear().apply()
            
            Log.d("Suporte", "Cache limpo com sucesso")
            enviarRespostaAcaoSuporte("limparcache", true, "Cache limpo com sucesso")
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao limpar cache", e)
            enviarRespostaAcaoSuporte("limparcache", false, "Erro ao limpar cache: ${e.message}")
        }
    }

    private fun limparDadosApp() {
        try {
            // Limpar dados do app (exceto login)
            val prefs = getSharedPreferences("app_data", Context.MODE_PRIVATE)
            val token = prefs.getString("auth_token", null)
            val refreshToken = prefs.getString("refresh_token", null)
            val userId = prefs.getString("user_id", null)
            
            prefs.edit().clear().apply()
            
            // Restaurar tokens
            if (token != null && refreshToken != null && userId != null) {
                prefs.edit()
                    .putString("auth_token", token)
                    .putString("refresh_token", refreshToken)
                    .putString("user_id", userId)
                    .apply()
            }
            
            // Limpar logs de erro locais
            lifecycleScope.launch(Dispatchers.IO) {
                try {
                    errorLogger.clearAllLogs()
                } catch (e: Exception) {
                    Log.e("Suporte", "Erro ao limpar logs", e)
                }
            }
            
            Log.d("Suporte", "Dados limpos com sucesso")
            enviarRespostaAcaoSuporte("limpardados", true, "Dados limpos com sucesso")
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao limpar dados", e)
            enviarRespostaAcaoSuporte("limpardados", false, "Erro ao limpar dados: ${e.message}")
        }
    }

    private fun atualizarDadosApp() {
        try {
            // Forçar atualização de dados
            loadDevices()
            // Recarregar outros dados necessários
            
            Log.d("Suporte", "Dados atualizados")
            enviarRespostaAcaoSuporte("atualizardados", true, "Dados atualizados com sucesso")
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao atualizar dados", e)
            enviarRespostaAcaoSuporte("atualizardados", false, "Erro ao atualizar dados: ${e.message}")
        }
    }

    private fun deleteRecursive(fileOrDirectory: File) {
        if (fileOrDirectory.isDirectory) {
            fileOrDirectory.listFiles()?.forEach { child ->
                deleteRecursive(child)
            }
        }
        fileOrDirectory.delete()
    }

    private fun enviarRespostaAcaoSuporte(acao: String, sucesso: Boolean, mensagem: String) {
        // Enviar resposta via SignalR para o chat de suporte
        try {
            val supportHub = HubConnectionBuilder.create("${ApiConfig.webBaseUrl}/hubs/support-chat?origem=app")
                .withAccessTokenProvider(Single.defer { 
                    Single.just(sessionManager.fetchAuthToken() ?: "") 
                })
                .build()
            
            supportHub.start()?.blockingAwait()
            supportHub.invoke("AcaoExecutada", acao, sucesso, mensagem)
            supportHub.stop()?.blockingAwait()
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao enviar resposta", e)
        }
    }

    private fun sendUdpCommand(device: Device, command: String): Boolean {
        return try {
            Log.d("SignalR", "UDP Enviando para ${device.name} via UDP")

            // 1. Verificar se temos IP registrado para este dispositivo
            val deviceIp = deviceIpMap[device.id]
            if (deviceIp == null) {
                Log.w("SignalR", "UDP IP não registrado para ${device.name}. Enviando discovery...")
                sendUdpDiscovery(device.id)
                return false
            }

            // 2. Construir payload com identificação
            val payload = "${device.id}|$command"
            val dataToSend = payload.toByteArray()

            // 3. Enviar via UNICAST para IP específico
            val socket = DatagramSocket().apply { reuseAddress = true }
            val targetAddress = InetAddress.getByName(deviceIp)
            val packet = DatagramPacket(dataToSend, dataToSend.size, targetAddress, 12345)

            socket.send(packet)
            socket.close()

            Log.d("SignalR", "✅ UDP Enviado UNICAST para $deviceIp: $payload")
            true
        } catch (e: Exception) {
            Log.e("SignalR", "❌ UDP Falha no envio UNICAST: ${e.message}")
            false
        }
    }

    private fun sendUdpDiscovery(deviceId: String) {
        try {
            val socket = DatagramSocket()
            val discoveryMsg = "DISCOVER:$deviceId".toByteArray()
            val packet = DatagramPacket(
                discoveryMsg,
                discoveryMsg.size,
                InetAddress.getByName("255.255.255.255"),
                12345
            )
            socket.send(packet)
            socket.close()
            Log.d("SignalR", "🔎 UDP Discovery enviado para $deviceId")
        } catch (e: Exception) {
            Log.e("SignalR", " UDP Falha no discovery", e)
        }
    }

    // Atualize a função existente
    private fun sendUdpBroadcast(device: Device, command: String): Boolean {
        // Se conhecemos o IP, usa UNICAST. Senão, usa broadcast como fallback
        return if (deviceIpMap.containsKey(device.id)) {
            sendUdpCommand(device, command)
        } else {
            try {
                Log.d("SignalR", "UDP Usando broadcast como fallback para ${device.name}")

                val socket = DatagramSocket().apply { broadcast = true }
                val payload = "${device.id}|$command".toByteArray()
                val packet = DatagramPacket(payload, payload.size, InetAddress.getByName("255.255.255.255"), 12345)

                socket.send(packet)
                socket.close()
                true
            } catch (_: Exception) {
                false
            }
        }
    }

    private fun updateDeviceState(deviceId: String, statusResponse: String) {
        // Encontra o dispositivo pelo ID sem loop manual
        val device = deviceList.find { it.id == deviceId }
        val position = deviceList.indexOfFirst { it.id == deviceId }
        val deviceName = device?.name



        if (statusResponse.contains("resposta:")){
            val resposta = statusResponse.split("resposta:")[1]
            Log.d("STATE_UPDATE", "Resposta: $resposta")
            when (resposta) {
                "ja_desligado" -> {
                    val mensagem = "$deviceName já está desligado"
                    speakTextFromService(mensagem)
                    enviarRespostaWebSocket(mensagem)
                }
                "ja_ligado" -> {
                    val mensagem = "$deviceName já está ligado"
                    speakTextFromService(mensagem)
                    enviarRespostaWebSocket(mensagem)
                }
                else -> {
                    speakTextFromService(resposta)
                    // Enviar outras respostas também via WebSocket
                    enviarRespostaWebSocket(resposta)
                }
            }
        }

        if (statusResponse.contains("status:")){
            val status = statusResponse.split("status:")[1]
            Log.d("STATE_UPDATE", "Status: $status")
            val isOn = status == "ligado"

            if (device != null && position != -1) {
                val oldState = device.isOn
                device.isOn = isOn

                // Fala apenas se houve mudança real
                if (oldState != isOn) {
                    val message = if (isOn) "${device.name} ligado" else "${device.name} desligado"
                    speakTextFromService(message)
                    deviceAdapter.notifyItemChanged(position)
                    Log.d("STATE_UPDATE", "🔄 STATE_UPDATE $deviceId: $oldState -> $isOn")
                }
            } else {
                Log.e("STATE_UPDATE", "❌ STATE_UPDATE Dispositivo $deviceId não encontrado")
            }
        }

    }

    private fun isOnWifi(): Boolean {
        val connectivityManager = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        val network = connectivityManager.activeNetwork ?: return false
        val capabilities = connectivityManager.getNetworkCapabilities(network) ?: return false
        return capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)
    }

    private suspend fun verificarResolvendoSuporte() {
        try {
            val retrofit = ApiClient.getClient(this)
            val suporteApi = retrofit.create(com.starkaid.starkaidapp.services.SuporteApi::class.java)
            val response = suporteApi.verificarResolvendoSuporte("app")
            
            if (response.isSuccessful && response.body() != null) {
                val body = response.body()!!
                val ativo = body.optBoolean("ativo", false)
                
                if (ativo) {
                    val mensagem = body.optString("message", "Você estava em processo de resolução de suporte. O problema foi resolvido?")
                    
                    runOnUiThread {
                        android.app.AlertDialog.Builder(this)
                            .setTitle("Suporte")
                            .setMessage(mensagem)
                            .setPositiveButton("Sim") { _, _ ->
                                lifecycleScope.launch(Dispatchers.IO) {
                                    marcarResolvido()
                                }
                            }
                            .setNegativeButton("Não", null)
                            .show()
                    }
                }
            }
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao verificar resolvendo suporte", e)
        }
    }

    private suspend fun marcarResolvido() {
        try {
            val retrofit = ApiClient.getClient(this)
            val suporteApi = retrofit.create(com.starkaid.starkaidapp.services.SuporteApi::class.java)
            suporteApi.marcarResolvido(com.starkaid.starkaidapp.services.MarcarResolvidoRequest("app"))
        } catch (e: Exception) {
            Log.e("Suporte", "Erro ao marcar como resolvido", e)
        }
    }

    private fun startUdpListener() {
        udpListenerJob = lifecycleScope.launch(Dispatchers.IO) {
            val socket = try {
                DatagramSocket(12345).apply {
                    broadcast = true
                    reuseAddress = true
                    soTimeout = 3000
                }
            } catch (e: Exception) {
                Log.e("UDP", "Erro ao criar socket", e)
                return@launch
            }

            val buffer = ByteArray(1024)
            Log.d("UDP", "UDP listener iniciado")

            while (isActive) {
                try {
                    val packet = DatagramPacket(buffer, buffer.size)
                    socket.receive(packet)

                    val message = String(packet.data, 0, packet.length).trim()
                    val senderIp = packet.address.hostAddress
                    Log.d("UDP", "Pacote recebido de $senderIp: '$message'")

                    when {
                        // 1. Registro de IP do dispositivo
                        message.startsWith("DEVICE_ID:") -> {
                            val deviceId = message.substringAfter("DEVICE_ID:").trim()
                            if (senderIp != null) {
                                deviceIpMap[deviceId] = senderIp
                            }
                            Log.d("UDP", "📝 Mapeado $deviceId -> $senderIp")

                            // Atualizar lista de dispositivos se necessário
                            deviceList.find { it.id == deviceId }?.ip = senderIp
                        }

                        // 2. Comandos específicos (deviceId|comando)
                        message.contains('|') -> {
                            val parts = message.split('|')
                            if (parts.size == 2) {
                                val deviceId = parts[0]
                                val command = parts[1]
                                runOnUiThread {
                                    updateDeviceState(deviceId, command)
                                }
                            }
                        }

                        // 3. Mensagens de discovery
                        message.startsWith("DISCOVER:") -> {
                            val deviceId = message.substringAfter("DEVICE_ID:").trim()
                            val targetId = message.substringAfter("DISCOVER:").trim()
                            Log.d("UDP", "Discovered $deviceId -> $targetId")
                        }
                    }
                } catch (_: SocketTimeoutException) {
                    // Timeout esperado
                } catch (e: Exception) {
                    Log.e("UDP", "Erro no listener: ${e.message}")
                }
            }

            socket.close()
            Log.d("UDP", "UDP listener encerrado")
        }
    }


    private fun playOnlineAudio(externalId: String?, title: String?) {
        if (externalId == null) return
        
        Log.d("Music", "Resolvendo stream de áudio online para ID: $externalId")
        
        CoroutineScope(Dispatchers.IO).launch {
             try {
                 val response = musicApi.getAudioStream(externalId)
                 if (response.isSuccessful && response.body() != null) {
                     val streamUrl = response.body()!!.streamUrl
                     Log.d("Music", "Stream URL resolvido: $streamUrl")
                     
                     withContext(Dispatchers.Main) {
                         val intent = Intent(this@MainActivity, RadioPlayerService::class.java).apply {
                            action = RadioPlayerService.ACTION_PLAY
                            putExtra(RadioPlayerService.EXTRA_STATION_NAME, title ?: "Música Online")
                            putExtra(RadioPlayerService.EXTRA_STREAM_URL, streamUrl)
                            putExtra(RadioPlayerService.EXTRA_SOURCE, "ONLINE")
                        }
                        startService(intent)
                        updateMiniPlayer(title ?: "Música", true, "ONLINE")
                     }
                  } else {
                      val errorBody = response.errorBody()?.string() ?: ""
                      Log.d("Music", "Falha ao resolver stream: ${response.code()} - $errorBody")
                      
                      val msg = if (response.code() == 404) {
                          "Essa música não está disponível para extração de áudio. Tente outra."
                      } else {
                          "Erro no servidor ao carregar áudio. Verifique os logs do sistema."
                      }
                      speakTextFromService(msg)
                  }
             } catch (e: Exception) {
                 Log.e("Music", "Erro de rede ao resolver stream", e)
                 speakTextFromService("Houve um erro de conexão ao tentar tocar a música.")
             }
        }
    }

    private val roomsConfirmationPending = AtomicBoolean(false)

    // ---------------- PIPELINE INITIALIZATION & EXECUTION ----------------
    private fun initializePipeline() {
        pipelineActions = object : AssistantActions {
            override fun speak(text: String) {
                 speakTextFromService(text)
            }
            override fun stopSpeaking() {
                 val stopIntent = Intent(this@MainActivity, FullDuplexAssistantAdvancedService::class.java).apply {
                    action = FullDuplexAssistantAdvancedService.ACTION_STOP_SPEAKING
                }
                startService(stopIntent)
            }
            override fun updateAvatarSleepingState() {
                 this@MainActivity.updateAvatarSleepingState()
            }
            override fun updateAvatarProcessingState(text: String, duration: Long) {
                 sendAvatarMatrixStatus(text, duration.toInt())
            }
            override fun sendWhatsappMessage(name: String, number: String, message: String) {
                 sendMessageWpp(name, number, message)
            }
            
            override suspend fun processSocial(text: String): Boolean = comandosSocialGet(text)
            override suspend fun processDirect(text: String): Boolean = processDirectCommands(text)
            override suspend fun processAutomation(text: String): Boolean = execAutomacao(text)
            
            override suspend fun processDevices(text: String): Boolean {
                 // Tenta controlar dispositivos (cópia da lógica de processandoComandos)
                 if (controlarDispositivoEsp(text)) return true
                 if (controlarDispositivoEwelink(text)) return true
                 return false
            }
            
            override suspend fun processIaFallback(text: String): Boolean = getIaResponse(text)
            
            override fun isStarkCoinsConfirmationPending() = aguardandoLiberarConsumoStarkcoins
            override fun setStarkCoinsConfirmationPending(pending: Boolean) { 
                aguardandoLiberarConsumoStarkcoins = pending 
            }
            
            override fun handleStarkCoinsResponse(positive: Boolean) {
                if (positive) {
                    Log.d("Pipeline", "✅ Resposta POSITIVA StarkCoins detectada no pipeline")
                    aguardandoLiberarConsumoStarkcoins = false
                    if (saldoStarkcoinsInt > 0) {
                        iaLimitReached = false
                        iaUsandoStarkCoins = true
                        runOnUiThread {
                            isSwitchIaChangingProgrammatically = true
                            switchIa.isChecked = true
                            prefs.edit().putBoolean("ia_enabled", true).apply()
                            isSwitchIaChangingProgrammatically = false
                        }
                        speakTextFromService("Ok, inteligência reativada usando StarkCoins.")
                    } else {
                        speakTextFromService("Saldo insuficiente. Você tem apenas $saldoStarkcoinsInt StarkCoins.")
                    }
                } else {
                    Log.d("Pipeline", "✅ Resposta NEGATIVA StarkCoins detectada no pipeline")
                    aguardandoLiberarConsumoStarkcoins = false
                    iaLimitReached = true
                    iaUsandoStarkCoins = false
                    runOnUiThread {
                        isSwitchIaChangingProgrammatically = true
                        switchIa.isChecked = false
                        prefs.edit().putBoolean("ia_enabled", false).apply()
                        isSwitchIaChangingProgrammatically = false
                    }
                    speakTextFromService("Ok, inteligência não será ativada.")
                }
            }

            override fun getAssistantName(): String = nomeAssistent

            override fun onWakeWordDetected() {
                escutando.set(true)
                iaativa.set(true)
                runOnUiThread {
                    updateAvatarSleepingState()
                    iniciarTimerDesativacaoEscutando()
                    iniciarTimerIaDesativacao()
                    responseNameAssistent()
                }
            }
            
            override fun sendTelemetry(evento: String, categoria: String, metadata: Map<String, Any>?) {
                com.starkaid.starkaidapp.services.TelemetryClient.sendEvent(this@MainActivity, evento, categoria, metadata)
            }

            override fun sendAiTelemetry(
                textoOriginal: String, 
                resultado: String, 
                latenciaMs: Int, 
                chamouIaExterna: Boolean,
                similarityScore: Double?,
                aprendizadoTipo: String?,
                aprendizadoId: String?
            ) {
                com.starkaid.starkaidapp.services.TelemetryClient.sendAiEvent(
                    this@MainActivity,
                    textoOriginal,
                    resultado,
                    latenciaMs,
                    chamouIaExterna,
                    similarityScore,
                    aprendizadoTipo,
                    aprendizadoId
                )
            }

            override suspend fun resolveAndPlayMusic(text: String): Boolean {
                return try {
                    val response = musicApi.resolveMusic(MusicResolveRequest(text))
                    if (response.isSuccessful && response.body() != null) {
                        val body = response.body()!!
                        if (!body.tts.isNullOrBlank()) {
                            speakTextFromService(body.tts)
                        }

                        if (body.type == "none" || body.type.isNullOrBlank()) {
                            return false
                        }

                        when (body.type) {
                            "radio_two" -> {
                                currentSource = "online"
                                playOnlineAudio(body.externalId, body.title)
                                true
                            }
                            "error" -> {
                                true
                            }
                            else -> {
                                // Control commands
                                when (body.type) {
                                    "stop" -> stopMusic()
                                    "pause" -> pauseMusic()
                                    "resume" -> resumeMusic()
                                    "volume_up" -> setMusicVolume(true)
                                    "volume_down" -> setMusicVolume(false)
                                    "status" -> {
                                        val playing = if (currentSource == "online") "Música Online" else tvMiniPlayerStation.text
                                        speakTextFromService("Está tocando $playing.")
                                    }
                                }
                                true
                            }
                        }
                    } else {
                        false
                    }
                } catch (e: Exception) {
                    Log.e("Music", "Error resolving music", e)
                    false
                }
            }
            
            override suspend fun processDeviceControl(text: String, deviceType: String?, isConfirmation: Boolean): Boolean {
                val api = ApiClient.getClient(this@MainActivity).create(ComodosApi::class.java)
                try {
                    val actualType = if (isConfirmation) lastDeviceType ?: "luz" else deviceType ?: "luz"
                    val comodoParam = if (isConfirmation) text else null
                    
                    // Determine intent
                    val commandLower = text.lowercase()
                    val currentTurnOn = if (isConfirmation) {
                        lastTurnOnIntent
                    } else {
                        !commandLower.contains("apaga") && !commandLower.contains("desliga")
                    }
                    
                    // We can pass a flag or the original command that had the intent
                    // For simplicity, let's pass a synthetic command if it's a confirmation to preserve intent
                    val commandToSend = if (isConfirmation) {
                        if (lastTurnOnIntent) "ligar" else "desligar"
                    } else {
                        text
                    }

                    val response = api.resolverDispositivo(actualType, commandToSend, comodoParam)
                    if (response.isSuccessful && response.body() != null) {
                        val result = response.body()!!
                        speakTextFromService(result.mensagemVoz)
                        setRoomsConfirmationPending(result.requerConfirmacao)
                        
                        if (result.requerConfirmacao) {
                             lastDeviceType = actualType
                             lastTurnOnIntent = currentTurnOn
                             // Force listening if waiting for answer
                             escutando.set(true)
                             runOnUiThread {
                                 iniciarTimerDesativacaoEscutando()
                                 updateAvatarSleepingState()
                             }
                        } else {
                             lastDeviceType = null
                        }
                        return true
                    }
                } catch (e: Exception) {
                    Log.e("DeviceControl", "Error", e)
                }
                return false
            }

            override fun setRoomsConfirmationPending(pending: Boolean) {
                roomsConfirmationPending.set(pending)
            }
            
            override fun isRoomsConfirmationPending(): Boolean {
                return roomsConfirmationPending.get()
            }



            override fun stopMusic() {
                // Unified Stop logic (works for both Radio and Online Stream)
                val intent = Intent(this@MainActivity, RadioPlayerService::class.java).apply {
                    action = RadioPlayerService.ACTION_STOP
                }
                startService(intent)
                updateMiniPlayer(null, false)
            }

            override fun pauseMusic() {
                // Unified Pause logic
                val intent = Intent(this@MainActivity, RadioPlayerService::class.java).apply {
                    action = RadioPlayerService.ACTION_PAUSE
                }
                startService(intent)
            }

            override fun resumeMusic() {
                 // Unified Resume logic
                val intent = Intent(this@MainActivity, RadioPlayerService::class.java).apply {
                    action = RadioPlayerService.ACTION_PLAY
                }
                startService(intent)
            }

            override fun nextMusic() {
                val intent = Intent(this@MainActivity, RadioPlayerService::class.java).apply {
                    action = RadioPlayerService.ACTION_NEXT
                }
                startService(intent)
            }

            override fun setMusicVolume(up: Boolean) {
                val am = getSystemService(AUDIO_SERVICE) as AudioManager
                val direction = if (up) AudioManager.ADJUST_RAISE else AudioManager.ADJUST_LOWER
                am.adjustStreamVolume(AudioManager.STREAM_MUSIC, direction, AudioManager.FLAG_SHOW_UI)
            }

            override fun unduckMusic() {
                val intent = Intent(this@MainActivity, FullDuplexAssistantAdvancedService::class.java).apply {
                    action = FullDuplexAssistantAdvancedService.ACTION_UNDUCK
                }
                startService(intent)
            }
        }
        
        val stages = listOf(
            StopTalkingStage(),
            StopListeningStage(),
            StarkCoinsStage(), // Check before Sleep Mode (Priority)
            AvatarStage(),
            SleepModeStage(),
            MusicStage(),
            WhatsappConfirmationStage(), // Requires listening
            DeviceControlStage(), // Enhanced Room Control Stage
            AnalyzeTextStage(analizaTexto),
            ProcessCommandStage(),
            IaFallbackStage()
        )
        
        commandPipeline = PipelineEngine(stages)
        Log.d("Pipeline", "Pipeline inicializado com ${stages.size} stages.")
    }

    private suspend fun processCommandViaPipeline(text: String) {
         if (!::commandPipeline.isInitialized) {
            Log.e("Pipeline", "Pipeline não inicializado! Usando fallback.")
            processSpeechResultWithAvatarStages(text)
            return
        }
        
        val ctx = CommandContext.from(
            rawText = text,
            escutando = escutando,
            confirmContato = confirmContato,
            roomsConfirmationPending = roomsConfirmationPending,
            isTtsSpeaking = isTtsSpeaking,
            actions = pipelineActions
        )
        
        // Executar pipeline
        commandPipeline.execute(ctx)
        
        // Se após o pipeline o sistema NÃO estiver falando e o comando foi finalizado, 
        // garantir que restauramos o volume (caso tenha sido baixado)
        if (!ctx.input.isPartial && !isTtsSpeaking) {
            pipelineActions.unduckMusic()
        }
    }

    private fun verificarDisparosPendentes() {
        SessionManager(this).fetchAuthToken() ?: return
        val retrofit = ApiClient.getClient(this)
        val api = retrofit.create(DisparoApi::class.java)

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val response = api.listarDisparos()
                if (response.isSuccessful) {
                    val disparos = response.body() ?: emptyList()
                    val disparoPendente = disparos.firstOrNull { !it.confirmado }
                    disparoPendente?.let {
                        val intent = Intent(this@MainActivity, DisparoAlertActivity::class.java).apply {
                            putExtra("disparoId", it.id)
                            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                        }
                        startActivity(intent)
                    }
                } else {
                    Log.e("MainActivity", "Erro ao buscar disparos: ${response.code()}")
                }
            } catch (e: Exception) {
                Log.e("MainActivity", "Erro: ${e.message}")
            }
        }
    }

    // NOVO MÉTODO: Extrair role do token JWT
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
            Log.e("MainActivity", "Erro ao extrair role do token", e)
            null
        }
    }
    
    // Método mantido para compatibilidade, mas agora extrai do token
    private suspend fun fetchUserRoleFromEndpoint(): String? {
        val token = sessionManager.fetchAuthToken()
        return extractRoleFromToken(token)
    }


}
