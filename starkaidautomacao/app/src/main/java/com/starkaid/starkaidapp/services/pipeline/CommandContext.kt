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
    var kind: CommandKind = CommandKind.UNKNOWN,
    
    // Mensagem anterior do sistema (para evitar eco)
    val lastSystemMessage: String = ""
) {
    companion object {
        fun from(
            rawText: String, 
            escutando: AtomicBoolean, 
            confirmContato: AtomicBoolean,
            roomsConfirmationPending: AtomicBoolean,
            isTtsSpeaking: Boolean,
            actions: AssistantActions,
            lastSystemMessage: String = ""
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
                actions = actions,
                lastSystemMessage = lastSystemMessage
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

interface AssistantActions : LegacyAssistantActions {
    fun speak(text: String)
    fun stopSpeaking()
    fun updateAvatarSleepingState()
    fun updateAvatarProcessingState(text: String, duration: Long)
    fun sendWhatsappMessage(name: String, number: String, message: String)
    suspend fun processSocial(text: String): Boolean
    suspend fun processDirect(text: String): Boolean
    suspend fun processAutomation(text: String): Boolean
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
    suspend fun processDeviceControl(
        text: String,
        deviceType: String?,
        isConfirmation: Boolean
    ): Boolean

    fun setRoomsConfirmationPending(pending: Boolean)
    fun isRoomsConfirmationPending(): Boolean

    // Room Awareness State Machine
    fun getComodos(): List<String>
    
    // New Deterministic State Access
    fun getRoomState(): RoomState
    fun updateActiveRoom(room: String)
    fun setAwaitingConfirmation(awaiting: Boolean)
    fun savePendingCommand(action: String, deviceType: String)
    
    // Deterministic Execution
    suspend fun executeDeviceCommand(room: String, deviceType: String, action: String): Boolean
    // Atividade do Usuário (Sincronização com Backend)
    fun updateActivity(
        tipo: String, // "IA", "SOCIAL", "ESP", "EWELINK", "STARKSWITCH"
        comando: String,
        resposta: String? = null
    )
}

// --- New State Classes ---
data class RoomContext(
    val room: String,
    val expiresAt: Long
)

data class PendingCommand(
    val action: String,
    val deviceType: String
)

object GlobalRoomState {
    @Volatile var active: RoomContext? = null
    @Volatile var awaitingConfirmation: Boolean = false
    @Volatile var pendingCommand: PendingCommand? = null
    
    fun reset() {
        active = null
        awaitingConfirmation = false
        pendingCommand = null
    }
}

// Typealias for easier usage in interface if needed, or stick to direct object access in implementation 
// But interface allows mocking/isolation.
interface RoomState {
    val active: RoomContext?
    val awaitingConfirmation: Boolean
    val pendingCommand: PendingCommand?
}


interface LegacyAssistantActions {
    suspend fun processDevices(text: String): Boolean
}
