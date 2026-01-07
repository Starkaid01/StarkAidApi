package com.starkaid.starkaidapp.services.pipeline

import com.starkaid.starkaidapp.models.AnaliseTexto
import java.util.concurrent.atomic.AtomicBoolean

data class CommandContext(
    val input: InputState,
    val voice: VoiceState,
    val session: SessionState,
    
    // Interface para ações de "Efeito Colateral" (Side Effects)
    val actions: AssistantActions,

    // Estado do pipeline
    var shouldStop: Boolean = false,
    var analysis: AnaliseTexto? = null,
    
    // Classificação do comando (Aprimoramento)
    var kind: CommandKind = CommandKind.UNKNOWN
) {
    companion object {
        fun from(
            rawText: String, 
            escutando: AtomicBoolean, 
            confirmContato: AtomicBoolean,
            roomsConfirmationPending: AtomicBoolean,
            isTtsSpeaking: Boolean,
            actions: AssistantActions
        ): CommandContext {
            val isPayment = rawText.lowercase().contains("parcial:")
            val isSpeaking = rawText.lowercase().contains("speaking:")
            
            // Limpeza básica inicial do texto
            var clean = rawText
            if (isPayment) clean = clean.replace("parcial:", "", ignoreCase = true)
            if (isSpeaking) clean = clean.replace("speaking:", "", ignoreCase = true)
            
            val inputState = InputState(
                rawText = rawText,
                cleanText = clean.trim(),
                isPartial = isPayment
            )
            
            val voiceState = VoiceState(
                isUserSpeaking = isSpeaking,
                isSystemSpeaking = isTtsSpeaking
            )
            
            val sessionState = SessionState(
                escutando = escutando,
                confirmContato = confirmContato,
                roomsConfirmationPending = roomsConfirmationPending
            )

            return CommandContext(
                input = inputState,
                voice = voiceState,
                session = sessionState,
                actions = actions
            )
        }
    }
}

data class InputState(
    val rawText: String,
    val cleanText: String,
    val isPartial: Boolean
)

data class VoiceState(
    val isUserSpeaking: Boolean, // Usuário ainda falando
    val isSystemSpeaking: Boolean // TTS
)

data class SessionState(
   val escutando: AtomicBoolean,
   val confirmContato: AtomicBoolean,
   val roomsConfirmationPending: AtomicBoolean
)

enum class CommandKind {
    UNKNOWN,
    SYSTEM,      // Parar, Ligar, etc
    SOCIAL,
    AUTOMATION,
    DEVICE,
    IA
}

interface AssistantActions {
    fun speak(text: String)
    fun stopSpeaking()
    fun updateAvatarSleepingState()
    fun updateAvatarProcessingState(text: String, duration: Long)
    fun sendWhatsappMessage(name: String, number: String, message: String)
    suspend fun processSocial(text: String): Boolean
    suspend fun processDirect(text: String): Boolean
    suspend fun processAutomation(text: String): Boolean
    suspend fun processDevices(text: String): Boolean
    suspend fun processIaFallback(text: String): Boolean
    
    // Gerenciamento de StarkCoins
    fun isStarkCoinsConfirmationPending(): Boolean
    fun setStarkCoinsConfirmationPending(pending: Boolean)
    fun handleStarkCoinsResponse(positive: Boolean) 
    
    // Wake Word e Estado
    fun getAssistantName(): String
    fun onWakeWordDetected()

    // Telemetria
    fun sendTelemetry(evento: String, categoria: String, metadata: Map<String, Any>? = null)
    fun sendAiTelemetry(
        textoOriginal: String, 
        resultado: String, 
        latenciaMs: Int, 
        chamouIaExterna: Boolean = false,
        similarityScore: Double? = null,
        aprendizadoTipo: String? = null,
        aprendizadoId: String? = null
    )

    // Música e Rádio
    suspend fun resolveAndPlayMusic(text: String): Boolean
    fun stopMusic()
    fun pauseMusic()
    fun resumeMusic()
    fun nextMusic()
    fun setMusicVolume(up: Boolean)
    fun unduckMusic()
    
    // Comodos
    suspend fun processDeviceControl(text: String, deviceType: String?, isConfirmation: Boolean): Boolean
    fun setRoomsConfirmationPending(pending: Boolean)
    fun isRoomsConfirmationPending(): Boolean
}

