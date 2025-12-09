package com.starkaid.starkaidapp.ui

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import android.speech.RecognitionListener
import android.speech.RecognizerIntent
import android.speech.SpeechRecognizer
import android.util.Log
import android.view.MotionEvent
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.databinding.ActivitySetupAssistantNameBinding
import kotlinx.coroutines.launch

class SetupAssistantNameActivity : AppCompatActivity() {

    private lateinit var binding: ActivitySetupAssistantNameBinding
    private lateinit var sessionManager: SessionManager
    private var speechRecognizer: SpeechRecognizer? = null
    private var isRecording = false

    companion object {
        private const val PERMISSION_REQUEST_CODE = 100
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySetupAssistantNameBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        setupUI()
        checkPermissions()
    }

    private fun setupUI() {
        // Configurar botão de microfone (segurar para gravar)
        binding.btnMicrophone.setOnTouchListener { _, event ->
            when (event.action) {
                MotionEvent.ACTION_DOWN -> {
                    startVoiceRecognition()
                    true
                }
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                    stopVoiceRecognition()
                    true
                }
                else -> false
            }
        }

        // Botão salvar
        binding.btnSave.setOnClickListener {
            val name = binding.etAssistantName.text.toString().trim()
            if (name.isEmpty()) {
                binding.etAssistantName.error = "Digite ou fale um nome para o assistente"
                Toast.makeText(this, "Digite ou fale um nome para o assistente", Toast.LENGTH_SHORT).show()
            } else if (name.equals("assistente", ignoreCase = true)) {
                binding.etAssistantName.error = "Escolha um nome diferente de 'Assistente'"
                Toast.makeText(this, "Por favor, escolha um nome diferente de 'Assistente'", Toast.LENGTH_SHORT).show()
            } else {
                saveAssistantName(name)
            }
        }
    }

    private fun checkPermissions() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO)
            != PackageManager.PERMISSION_GRANTED
        ) {
            ActivityCompat.requestPermissions(
                this,
                arrayOf(Manifest.permission.RECORD_AUDIO),
                PERMISSION_REQUEST_CODE
            )
        } else {
            initializeSpeechRecognizer()
        }
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == PERMISSION_REQUEST_CODE) {
            if (grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                initializeSpeechRecognizer()
            } else {
                Toast.makeText(
                    this,
                    "Permissão de microfone necessária para usar o reconhecimento de voz",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }

    private fun initializeSpeechRecognizer() {
        if (!SpeechRecognizer.isRecognitionAvailable(this)) {
            Toast.makeText(this, "Reconhecimento de voz não disponível", Toast.LENGTH_SHORT).show()
            return
        }

        speechRecognizer = SpeechRecognizer.createSpeechRecognizer(this).apply {
            setRecognitionListener(object : RecognitionListener {
                override fun onReadyForSpeech(params: Bundle?) {
                    binding.btnMicrophone.alpha = 0.5f
                    binding.tvStatus.text = "Ouvindo..."
                }

                override fun onBeginningOfSpeech() {
                    binding.tvStatus.text = "Falando..."
                }

                override fun onRmsChanged(rmsdB: Float) {
                    // Feedback visual opcional
                }

                override fun onBufferReceived(buffer: ByteArray?) {}

                override fun onEndOfSpeech() {
                    binding.tvStatus.text = "Processando..."
                }

                override fun onError(error: Int) {
                    binding.btnMicrophone.alpha = 1.0f
                    binding.tvStatus.text = "Segure o microfone e fale o nome"
                    isRecording = false
                    
                    val errorMessage = when (error) {
                        SpeechRecognizer.ERROR_AUDIO -> "Erro de áudio"
                        SpeechRecognizer.ERROR_CLIENT -> "Erro do cliente"
                        SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS -> "Permissões insuficientes"
                        SpeechRecognizer.ERROR_NETWORK -> "Erro de rede"
                        SpeechRecognizer.ERROR_NETWORK_TIMEOUT -> "Timeout de rede"
                        SpeechRecognizer.ERROR_NO_MATCH -> "Não foi possível reconhecer. Tente novamente."
                        SpeechRecognizer.ERROR_RECOGNIZER_BUSY -> "Reconhecedor ocupado"
                        SpeechRecognizer.ERROR_SERVER -> "Erro do servidor"
                        SpeechRecognizer.ERROR_SPEECH_TIMEOUT -> "Tempo de fala esgotado"
                        else -> "Erro desconhecido"
                    }
                    
                    if (error != SpeechRecognizer.ERROR_NO_MATCH && error != SpeechRecognizer.ERROR_SPEECH_TIMEOUT) {
                        Toast.makeText(this@SetupAssistantNameActivity, errorMessage, Toast.LENGTH_SHORT).show()
                    }
                }

                override fun onResults(results: Bundle?) {
                    binding.btnMicrophone.alpha = 1.0f
                    binding.tvStatus.text = "Segure o microfone e fale o nome"
                    isRecording = false

                    val matches = results?.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                    if (!matches.isNullOrEmpty()) {
                        val recognizedText = matches[0]
                        binding.etAssistantName.setText(recognizedText)
                        binding.etAssistantName.setSelection(recognizedText.length)
                    }
                }

                override fun onPartialResults(partialResults: Bundle?) {
                    val matches = partialResults?.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                    if (!matches.isNullOrEmpty()) {
                        binding.etAssistantName.setText(matches[0])
                        binding.etAssistantName.setSelection(matches[0].length)
                    }
                }

                override fun onEvent(eventType: Int, params: Bundle?) {}
            })
        }
    }

    private fun startVoiceRecognition() {
        if (!isRecording && speechRecognizer != null) {
            isRecording = true
            val intent = Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH).apply {
                putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM)
                putExtra(RecognizerIntent.EXTRA_LANGUAGE, "pt-BR")
                putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true)
                putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1)
            }
            speechRecognizer?.startListening(intent)
        }
    }

    private fun stopVoiceRecognition() {
        if (isRecording) {
            speechRecognizer?.stopListening()
            isRecording = false
            binding.btnMicrophone.alpha = 1.0f
            binding.tvStatus.text = "Segure o microfone e fale o nome"
        }
    }

    private fun saveAssistantName(name: String) {
        try {
            // Salvar o nome do assistente (síncrono para garantir que seja salvo antes de navegar)
            sessionManager.saveAssistantName(name.trim())
            
            // Salvar resposta padrão "Estou ouvindo."
            sessionManager.saveDefaultResponse("Estou ouvindo.")
            
            // Verificar se foi salvo corretamente
            val savedName = sessionManager.fetchAssistantName()
            Log.d("SetupAssistantName", "Nome salvo: '$savedName', Nome esperado: '${name.trim()}'")
            
            if (savedName != null && savedName.equals(name.trim(), ignoreCase = true)) {
                Toast.makeText(this, "Nome salvo com sucesso!", Toast.LENGTH_SHORT).show()
                
                // Pequeno delay para garantir que o salvamento foi persistido
                android.os.Handler(android.os.Looper.getMainLooper()).postDelayed({
                    // Voltar para MainActivity
                    val intent = Intent(this, MainActivity::class.java)
                    intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                    startActivity(intent)
                    finish()
                }, 100)
            } else {
                Toast.makeText(this, "Erro: Nome não foi salvo corretamente", Toast.LENGTH_SHORT).show()
            }
        } catch (e: Exception) {
            Log.e("SetupAssistantName", "Erro ao salvar nome: ${e.message}", e)
            Toast.makeText(this, "Erro ao salvar: ${e.message}", Toast.LENGTH_SHORT).show()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        speechRecognizer?.destroy()
        speechRecognizer = null
    }
}

