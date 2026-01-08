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
        if (ctx.actions.processDevices(text)) {
            ctx.kind = CommandKind.DEVICE
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
        
        // 0. Confirmação Pendente (Ex: "Na sala", "Quarto")
        if (ctx.session.roomsConfirmationPending.get()) {
            // Verificar se a resposta contém um cômodo válido do texto
            val comodos = ctx.actions.getComodos()
            val normalizedText = text.removeAccents()
            val comodoEncontrado = comodos.find { normalizedText.contains(it.removeAccents()) }
            
            if (comodoEncontrado != null) {
                // Atualiza contexto e reprocessa
                ctx.actions.setActiveRoom(comodoEncontrado)
                // Retenta processar o comando original com o novo contexto? 
                // Simplificação: Passa o cômodo como "texto" para a API confirmar
                if (ctx.actions.processDeviceControl(comodoEncontrado, null, isConfirmation = true)) {
                     ctx.session.roomsConfirmationPending.set(false)
                     ctx.kind = CommandKind.DEVICE
                     return StageResult.Handled
                }
            } else {
                // Se não entendeu o cômodo, talvez seja cancelamento
                if (text.contains("cancela") || text.contains("esquece")) {
                    ctx.session.roomsConfirmationPending.set(false)
                    ctx.actions.speak("Ok, cancelado.")
                    ctx.kind = CommandKind.SYSTEM
                    return StageResult.Handled
                }
                // Senão, repassa para API tentar entender (fallback antigo)
                 if (ctx.actions.processDeviceControl(text, null, isConfirmation = true)) {
                     ctx.session.roomsConfirmationPending.set(false)
                     ctx.kind = CommandKind.DEVICE
                     return StageResult.Handled
                }
            }
        }

        // 1. Prioridade DESLIGAR (Break Flow)
        val turnOffTriggers = listOf("apaga", "desliga", "desligar", "desligue")
        val isTurnOff = turnOffTriggers.any { text.contains(it) }
        
        // 2. LIGAR (só se não for desligar, mas aqui processamos ambos com intent flag implícita no texto)
        val turnOnTriggers = listOf("acende", "liga", "ligar", "ligue")
        val isTurnOn = turnOnTriggers.any { text.contains(it) }

        if (!isTurnOff && !isTurnOn) return StageResult.Pass

        // Se for Desligar, garantimos que a API receba explicitamente (ou processamos aqui)
        // O user pediu explicitamente: "assim que desligar e responder fluxo deve break"
        // Nossa implementação chama processDeviceControl que retorna true/false.
        
        // 3. Identificação de Cômodo
        val comodos = ctx.actions.getComodos()
        val normalizedMainText = text.removeAccents()
        // Ordenar por tamanho decrescente para pegar "quarto do casal" antes de "quarto"
        val comodoEncontrado = comodos.sortedByDescending { it.length }.find { normalizedMainText.contains(it.removeAccents()) }
        
        var targetRoom = comodoEncontrado
        
        if (targetRoom != null) {
            // Se encontrou no texto, atualiza o contexto ativo
            ctx.actions.setActiveRoom(targetRoom)
        } else {
            // Se não encontrou, usa o ativo
            targetRoom = ctx.actions.getActiveRoom()
        }

        // 4. Extração do Dispositivo (Tipo)
        // Remover gatilhos e cômodos para achar o "objeto" (ex: "luz", "ventilador")
        // Simplificado: Se contém "luz", "lampada" -> luz. 
        // A função extractDeviceType tenta fazer isso genericamente.
        val mainTrigger = if (isTurnOff) turnOffTriggers.find { text.contains(it) } else turnOnTriggers.find { text.contains(it) }
        val deviceType = extractDeviceType(text, mainTrigger ?: "")

        // LOGICA DE AMBIGUIDADE (Simulada, pois a API faz a busca real)
        // Se targetRoom é NULL e deviceType é genérico (ex: "luz"), 
        // deveríamos checar se há multiplas.
        // Como não temos a lista completa de devices aqui com facilidade (temos getDevicesInRoom mas não All),
        // vamos confiar na API para perguntar "Em qual cômodo?" SE enviarmos sem cômodo.
        // MAS, se tivermos contexto ativo, enviamos o contexto!
        
        // Construir comando final ou deixar API resolver?
        // Se temos targetRoom, injetamos no texto ou passamos parametro? 
        // A função processDeviceControl na MainActivity usa o texto raw OU comodoParam se confirmation.
        // Vamos modificar a estratégia: Append room if found/active implicitamente?
        // Melhor: Passar o texto original. A API (com a mudança na MainActivity que fizemos) deveria saber do Contexto?
        // Ah, MainActivity não manda comodoContext para a ComodosApi :(
        // A ComodosApi espera (tipo, comando, comodoOpcional).
        
        // HACK: Se temos targetRoom e ele não está no texto, adicionamos artificialmente para a API achar?
        // Ou melhor, a API provavelmente já suporta receber o cômodo.
        // Vamos manter a chamada e confiar que se a API retornar "confirmação necessária", 
        // a gente trata no passo 0 na próxima vez.
        
        // No entanto, o user pediu para atualizar a flag `comodoAtivo`. Já fizemos acima.
        
        // Reforço da prioridade Desligar:
        if (isTurnOff) {
             Log.d("Pipeline", "DeviceControlStage: Prioridade DESLIGAR - $text [Room: $targetRoom]")
             // Workaround: Injetar o cômodo ativo no texto se ele não estiver lá, para ajudar a API
             var textoFinal = text
             if (targetRoom != null && !text.contains(targetRoom)) {
                 textoFinal = "$text no $targetRoom"
             }
             
             if (ctx.actions.processDeviceControl(textoFinal, deviceType, isConfirmation = false)) {
                 ctx.kind = CommandKind.DEVICE
                 return StageResult.Handled
             }
        } 
        
        // Se não era desligar (ou falhou?), tenta Ligar
        if (isTurnOn && !isTurnOff) {
             Log.d("Pipeline", "DeviceControlStage: LIGAR - $text [Room: $targetRoom]")
             var textoFinal = text
             if (targetRoom != null && !text.contains(targetRoom)) {
                 textoFinal = "$text no $targetRoom"
             }

             if (ctx.actions.processDeviceControl(textoFinal, deviceType, isConfirmation = false)) {
                 ctx.kind = CommandKind.DEVICE
                 return StageResult.Handled
             }
        }

        return StageResult.Pass
    }

    private fun extractDeviceType(text: String, trigger: String): String {
        try {
            if (trigger.isEmpty()) return "luz" // Default seguro
            val afterTrigger = text.substringAfter(trigger).trim()
            if (afterTrigger.isEmpty()) return "luz"
            
            // Tenta pegar a primeira palavra significativa
            val parts = afterTrigger.split(" ")
            // Pular artigos
            val ignore = listOf("a", "o", "as", "os", "um", "uma", "da", "do", "de", "no", "na")
            
            for (part in parts) {
                if (!ignore.contains(part) && part.length > 2) {
                    return part // "luz", "ventilador", "abajur"
                }
            }
            return "luz"
        } catch (e: Exception) {
            return "luz"
        }
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
