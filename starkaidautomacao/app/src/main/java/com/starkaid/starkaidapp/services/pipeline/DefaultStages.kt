package com.starkaid.starkaidapp.services.pipeline

import android.util.Log
import com.starkaid.starkaidapp.services.AnalizaTexto
import com.starkaid.starkaidapp.services.RadioPlayerService
import java.text.Normalizer

// --- 1. StopTalkingStage ---
class StopTalkingStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        val text = ctx.input.cleanText.lowercase().trim()
        val stopCommands = listOf(
            "para de falar", "parar de falar", "pare de falar",
            "cala a boca", "cale a boca", "calar a boca",
            "cala boca", "cale boca", "calar boca",
            "cale-se", "cala-se", "calar-se",
            "fica quieto", "fique quieto", "ficar quieto",
            "fica calado", "fique calado", "ficar calado",
            "silencio", "silêncio",
            "para com isso", "pare com isso", "parar com isso",
            "chega de falar", "basta de falar", "silêncio agora"
        )

        // Normalização agressiva remove pontuação
        val normalized = text.replace(Regex("[^a-z0-9\\s]"), "").trim()
        val isStopCommand = stopCommands.any { normalized.contains(it) || it.contains(normalized) && normalized.length > 5 } ||
                ((normalized.contains("para") || normalized.contains("pare")) && (normalized.contains("falar") || normalized.contains("falando"))) ||
                (normalized.contains("cala") && normalized.contains("boca"))

        if (isStopCommand) {
             Log.d("Pipeline", "StopTalkingStage: Comando de INTERRUPÇÃO detectado: '$normalized'")
        }

        // Anti-Echo Check (Similarity) - Only if NOT a stop command
        if (!isStopCommand) {
            val lastMsg = ctx.lastSystemMessage.lowercase().trim().replace(Regex("[^a-z0-9\\s]"), "")
            if (lastMsg.length > 4 && normalized.length > 3) { // Lower threshold to catch "bom dia" loops
                 val dist = levenshtein(normalized, lastMsg)
                 val maxLen = kotlin.math.max(normalized.length, lastMsg.length)
                 val similarity = 1.0 - (dist.toDouble() / maxLen)
                 
                 if (similarity > 0.75) { // Slightly increased threshold for shorter texts safety
                     Log.d("Pipeline", "StopTalkingStage: ECO DETECTADO (Similaridade: $similarity). Ignorando: '$normalized'")
                     return StageResult.StopPipeline
                 }
            }
        }

        // Se o sistema está falando (TTS) OU recebemos um comando de parada explícito
        if (ctx.voice.isSystemSpeaking || isStopCommand) {
            if (isStopCommand) {
                Log.d("Pipeline", "StopTalkingStage: Executando EMERGENCY STOP")
                ctx.actions.stopSpeaking()
                ctx.kind = CommandKind.SYSTEM
                return StageResult.Handled
            }
            
            // Se está falando e NÃO é comando de parar, interrompe o pipeline (evita eco)
            if (ctx.voice.isSystemSpeaking) {
                Log.d("Pipeline", "StopTalkingStage: Ignorando comando enquanto sistema fala (anti-eco)")
                return StageResult.StopPipeline
            }
        }
        
        return StageResult.Pass
    }
}

// --- 2. StopListeningStage ---
class StopListeningStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (!ctx.input.cleanText.contains("parar de ouvir")) {
            return StageResult.Pass
        }

        Log.d("Pipeline", "StopListeningStage: Parando escuta.")
        ctx.session.escutando.set(false)
        ctx.actions.updateAvatarSleepingState()
        ctx.actions.speak("Ok, se precisar só chamar!")
        ctx.kind = CommandKind.SYSTEM
        return StageResult.Handled
    }
}

// --- 3. WhatsappConfirmationStage ---
class WhatsappConfirmationStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (!ctx.session.confirmContato.get()) return StageResult.Pass

        val text = ctx.input.cleanText
        Log.d("Pipeline", "WhatsappConfirmationStage: Verificando confirmação '$text'")

        val isSim = text.contains("sim") || text.contains("pode sim") || 
                    text.contains("pode mandar") || text.contains("pode manda") || 
                    text.contains("sim pode") || text.contains("isso mesmo") || 
                    text.contains("esta certo") || text.contains("exatamente")

        val isNao = text.contains("nao") || text.contains("esta errado") || 
                    text.contains("contato errado") || text.contains("numero errado") || 
                    text.contains("aborte") || text.contains("abortar") || 
                    text.contains("cancela") || text.contains("cancelar")

        if (isSim) {
             ctx.actions.sendWhatsappMessage("", "", "") // Parâmetros serão pegos do estado interno da UI
             ctx.session.confirmContato.set(false)
             ctx.kind = CommandKind.SOCIAL
             return StageResult.Handled
        }

        if (isNao) {
            ctx.session.confirmContato.set(false)
            ctx.actions.speak("Ok, não vou enviar a mensagem.")
            ctx.kind = CommandKind.SOCIAL
            return StageResult.Handled
        }

        return StageResult.Pass
    }
}

// --- 4. AvatarStage (Matrix) ---
class AvatarStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        // Lógica de UI do Avatar
        if (ctx.input.isPartial || ctx.voice.isUserSpeaking) return StageResult.Pass
        
        // Atualiza status do matrix
        ctx.actions.updateAvatarProcessingState("Comando recebido...", 2000)
        return StageResult.Pass // Não "handle", apenas observa side-effect
    }
}

// --- 5. SleepModeStage ---
class SleepModeStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        val escutando = ctx.session.escutando.get()
        val assistantName = ctx.actions.getAssistantName().lowercase().trim()
        val text = ctx.input.cleanText.lowercase().trim()

        Log.d("Pipeline", "SleepModeStage: escutando=$escutando, name='$assistantName', text='$text'")

        // Caso 1: Se o texto for EXATAMENTE o nome do assistente (ou muito próximo)
        // Queremos que ele responda "Sim?" ou a resposta padrão, esteja ele dormindo ou não.
        if (text == assistantName || (text.startsWith(assistantName) && text.length <= assistantName.length + 2)) {
            Log.d("Pipeline", "SleepModeStage: Nome do assistente detectado isoladamente.")
            ctx.actions.onWakeWordDetected()
            return StageResult.Handled
        }

        // Caso 2: Se não estiver ouvindo, ele só acorda se o texto CONTIVER o nome
        if (!escutando) {
            if (text.contains(assistantName)) {
                Log.d("Pipeline", "SleepModeStage: Wake word detectada no meio da frase!")
                ctx.actions.onWakeWordDetected()
                
                // Se o nome está no início, podemos limpar e continuar o processamento do resto da frase
                // Mas por simplicidade agora, apenas deixamos passar para o resto do pipeline
                return StageResult.Pass
            }

            Log.d("Pipeline", "SleepModeStage: Bloqueado (dormindo e nome não detectado)")
            return StageResult.StopPipeline
        }

        // Se já está ouvindo e não foi apenas o nome, continua o processamento normal
        return StageResult.Pass
    }
}

// --- 6. StarkCoinsStage ---
class StarkCoinsStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (!ctx.actions.isStarkCoinsConfirmationPending()) return StageResult.Pass

        Log.d("Pipeline", "StarkCoinsStage: Processando confirmação.")
        val normalized = ctx.input.cleanText
        
        val isPositive = normalized.contains("sim") || normalized.contains("pode") || normalized.contains("quero") || normalized.contains("aceito")
        val isNegative = normalized.contains("nao") || normalized.contains("não") || normalized.contains("cancelar")
        
        if (isPositive) {
            ctx.actions.handleStarkCoinsResponse(true)
            ctx.kind = CommandKind.SYSTEM
            return StageResult.Handled
        }
        if (isNegative) {
            ctx.actions.handleStarkCoinsResponse(false)
            ctx.kind = CommandKind.SYSTEM
            return StageResult.Handled
        }
        
        return StageResult.Pass
    }
}

// --- 7. AnalyzeTextStage ---
class AnalyzeTextStage(private val analyzer: AnalizaTexto) : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (ctx.input.isPartial) return StageResult.Pass 
        
        val analysis = analyzer.analisarTexto(ctx.input.rawText)
        ctx.analysis = analysis
        return StageResult.Pass
    }
}

// --- 8. MusicStage ---
class MusicStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (ctx.input.isPartial) return StageResult.Pass
        val text = ctx.input.cleanText.lowercase()
        
        // 1. Gatilhos de PESQUISA EXPLÍCITOS (Obrigatório começar com toca/tocar/toque/musica/ouvir/reproduzir/colocar/solta)
        val searchTriggers = listOf("tocar ", "toca ", "toque ", "musica ", "ouvir ", "reproduzir ", "colocar ", "solta ")
        val trigger = searchTriggers.find { text.startsWith(it) }

        if (trigger != null) {
            val query = text.substringAfter(trigger).trim()
            if (query.isNotEmpty()) {
                Log.d("Pipeline", "MusicStage: PESQUISA YouTube iniciada para '$query'")
                ctx.actions.resolveAndPlayMusic(text) // O resultado não importa aqui para o pipeline
                return StageResult.Handled // Sempre bloqueia para o AI não responder "música ..."
            }
        }

        // 2. Comandos de CONTROLE e VOLUME
        // Lista restrita para evitar capturar conversas aleatórias
        val musicControlKeywords = listOf(
            "volume", "abaixa", "baixa", "aumenta", "mais alto", "mais baixo", "aumenta mais", "baixa mais",
            "pausa", "pause", "pausar", "continua", "resume", "retoma",
            "parar música", "para música", "pare a música", "para a música", "parar o som", "parar som",
            "quem está cantando", "que música é essa", "que música está tocando"
        )
        
        val isExplicitControl = musicControlKeywords.any { text.contains(it) } || 
                                text == "parar" || text == "para" || text == "pare" || text == "stop"
        
        if (isExplicitControl) {
            Log.d("Pipeline", "MusicStage: CONTROLE/VOLUME detectado em '$text'")
            ctx.actions.resolveAndPlayMusic(text)
            return StageResult.Handled // Bloqueia para ser atômico e gratuito
        }
        
        return StageResult.Pass
    }
}

// --- 9. ProcessCommandStage (Wrapper para lógica legada) ---
class ProcessCommandStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        val text = ctx.input.rawText
        
        if (ctx.actions.processSocial(text)) {
            ctx.kind = CommandKind.SOCIAL
            return StageResult.Handled
        }
        if (ctx.actions.processDirect(text)) {
            ctx.kind = CommandKind.AUTOMATION // ou DIRECT
            return StageResult.Handled
        }
        if (ctx.actions.processAutomation(text)) {
            ctx.kind = CommandKind.AUTOMATION
            return StageResult.Handled
        }
        
        return StageResult.Pass
    }
}

// --- 9.5 DeviceControlStage ---
class DeviceControlStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (ctx.input.isPartial) return StageResult.Pass
        
        val text = ctx.input.cleanText.lowercase()
        val normalizedText = text.removeAccents()
        val state = ctx.actions.getRoomState()

        // 1. Confirmação pendente (PRIORIDADE MÁXIMA)
        if (state.awaitingConfirmation) {
            
            // Cancelamento explícito
            if (text.contains("cancela") || text.contains("esquece") || text.contains("nao")) {
                ctx.actions.setAwaitingConfirmation(false)
                ctx.actions.speak("Ok, cancelado.")
                ctx.kind = CommandKind.SYSTEM
                return StageResult.Handled
            }
            
            val comodos = ctx.actions.getComodos()
            // Ordenar por tamanho decrescente para pegar "quarto do casal" > "quarto"
            val comodoEncontrado = comodos.sortedByDescending { it.length }
                .find { normalizedText.contains(it.removeAccents()) }

            if (comodoEncontrado != null) {
                // Atualizar RoomState.active & Resetar TTL
                ctx.actions.updateActiveRoom(comodoEncontrado)
                ctx.actions.setAwaitingConfirmation(false)
                
                // Executar ação pendente
                val pending = state.pendingCommand
                if (pending != null) {
                    ctx.actions.executeDeviceCommand(comodoEncontrado, pending.deviceType, pending.action)
                }
                
                ctx.kind = CommandKind.DEVICE
                return StageResult.Handled
            }

            // Se aind não entendeu, aborta explicitamente para não deixar o user no vácuo
            // e para não deixar o pipeline seguir para a IA.
            ctx.actions.speak("Não entendi o cômodo.")
            return StageResult.Handled
        }

        // 2. Detectar intenção (determinística)
        val turnOnTriggers = listOf("acende", "liga", "ligar", "ligue")
        val turnOffTriggers = listOf("apaga", "desliga", "desligar", "desligue")

        val isTurnOn = turnOnTriggers.any { text.contains(it) }
        val isTurnOff = turnOffTriggers.any { text.contains(it) }

        if (!isTurnOn && !isTurnOff) return StageResult.Pass

        val finalAction = if (isTurnOff) "desligar" else "ligar"
        
        // 2.5 Blacklist Semântica de Comunicação (Gatekeeper)
        // Bloqueia "liga para...", "chama..." antes de tentar achar device
        val communicationPatterns = listOf(
            "liga para", "ligar para", "ligue para", 
            "telefone para", "chama ", "chamar "
        )

        if (communicationPatterns.any { normalizedText.contains(it) }) {
            Log.d("Pipeline", "DeviceControl: Padrão de comunicação detectado. Ignorando.")
            return StageResult.Pass
        }



        // 3. Extrair tipo de dispositivo (local)
        // Lista fechada de devices conhecidos
        val knownDevices = listOf("luz", "lampada", "portao", "ar condicionado", "tv", "ventilador", "tomada", "abajur")
        // Usa normalizedText para robustez (ex: "lâmpada" -> "lampada")
        var deviceType: String? = knownDevices.find { normalizedText.contains(it.removeAccents()) }

        // Lógica de Default condicional e bloqueio
        // Se não achou device...
        if (deviceType == null) {
            // Se o verbo for MUITO específico de luz ("acende", "apaga"), podemos assumir "luz".
            // Mas se for "liga", "desliga", pode ser "liga para pessoa".
            if (text.contains("acende") || text.contains("apaga")) {
                deviceType = "luz"
            } else {
                // "Liga..." sem device -> NÃO PROCESSA AQUI. Passa para Social ou IA.
                Log.d("Pipeline", "DeviceControl: PASS - Verbo de ação detectado, mas nenhum device explícito ou inferível.")
                return StageResult.Pass
            }
        }
        
        // Se chegou aqui, temos um device válido (ou default seguro para acende/apaga)
        val finalDevice = deviceType ?: "luz"

        Log.d("Pipeline", "DeviceControlStage Check: Action=$finalAction, Device=$finalDevice, Text='$text'")


        // 4. Extrair cômodo explícito (se existir)
        val comodos = ctx.actions.getComodos()
        val explicitRoom = comodos.sortedByDescending { it.length }
            .find { normalizedText.contains(it.removeAccents()) }

        if (explicitRoom != null) {
            ctx.actions.updateActiveRoom(explicitRoom)
            // Reset TTL acontece dentro do updateActiveRoom
        }

        // RELOAD STATE obrigatório após updateActiveRoom
        val updatedState = ctx.actions.getRoomState()

        // 5. Resolver cômodo final
        // Se temos explicitRoom, usamos ele. Se não, tentamos o ativo (agora garantido atualizado).
        val finalRoom = explicitRoom ?: updatedState.active?.room


        // 6. Ambiguidade (ÚNICO ponto onde pergunta)
        // 6. Ambiguidade (ÚNICO ponto onde pergunta)
        if (finalRoom == null) {
            ctx.actions.savePendingCommand(finalAction, finalDevice)
            ctx.actions.setAwaitingConfirmation(true)
            ctx.actions.speak("Em qual cômodo?")
            
            ctx.kind = CommandKind.DEVICE
            return StageResult.Handled
        }


        // 7. Execução (UMA ÚNICA VEZ)
        ctx.actions.executeDeviceCommand(finalRoom, finalDevice, finalAction)
        
        ctx.kind = CommandKind.DEVICE
        return StageResult.Handled
    }
}

// --- 10. IaFallbackStage ---
class IaFallbackStage : CommandStage {
    override suspend fun process(ctx: CommandContext): StageResult {
        if (ctx.input.isPartial) return StageResult.StopPipeline
        
        if (ctx.actions.processIaFallback(ctx.input.cleanText)) {
            ctx.kind = CommandKind.IA
            return StageResult.Handled
        }
        return StageResult.StopPipeline
    }
}

// Extension function to remove accents
fun String.removeAccents(): String {
    val temp = Normalizer.normalize(this, Normalizer.Form.NFD)
    return Regex("\\p{InCombiningDiacriticalMarks}+").replace(temp, "")
}

fun levenshtein(lhs: CharSequence, rhs: CharSequence): Int {
    val lhsLen = lhs.length
    val rhsLen = rhs.length
    var cost = IntArray(lhsLen + 1) { it }
    var newCost = IntArray(lhsLen + 1) { 0 }

    for (i in 1..rhsLen) {
        newCost[0] = i
        for (j in 1..lhsLen) {
            val match = if (lhs[j - 1] == rhs[i - 1]) 0 else 1
            val costReplace = cost[j - 1] + match
            val costInsert = cost[j] + 1
            val costDelete = newCost[j - 1] + 1
            newCost[j] = kotlin.math.min(kotlin.math.min(costInsert, costDelete), costReplace)
        }
        val swap = cost
        cost = newCost
        newCost = swap
    }
    return cost[lhsLen]
}
