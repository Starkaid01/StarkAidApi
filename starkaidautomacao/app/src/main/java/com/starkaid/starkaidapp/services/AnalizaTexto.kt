package com.starkaid.starkaidapp.services

import android.util.Log
import com.starkaid.starkaidapp.models.AnaliseTexto
import com.starkaid.starkaidapp.models.ComandoAutomacao
import java.text.Normalizer
import java.util.regex.Pattern

class AnalizaTexto {
    fun analisarTexto(text: String): AnaliseTexto {
        return analisarTextoInterno(text)
    }

    companion object {
        private const val TAG = "AnalizaTexto"

        private val PALAVRAS_INICIAIS = setOf(
            "quem", "que", "qual", "quais", "quando", "onde", "como",
            "porque", "por", "quanto", "quanta", "quantos", "quantas", "aonde", "sera"
        )

        private val FRASES_INICIAIS = setOf(
            "o que é", "pra que", "para que", "sobre o que", "do que estavamos falando",
            "voce sabe", "onde fica", "onde se localiza", "em qual", "em que", "para onde",
            "pesquise", "busque", "encontre"
        )

        private val PALAVRAS_MEIO = setOf("como", "por", "qual", "quais", "quanto", "quanta", "quantos", "quantas")
        
        private val PADROES_DUV = setOf(
            "saber", "querer", "preciso", "diga", "explique", "conte", "mostre", "me", "poderia", "sera"
        )

        private val SOCIAIS = mapOf(
            "saudacao" to listOf("oi", "ola", "e ai", "fala ai", "bom dia", "boa tarde", "boa noite", "opa", "salve"),
            "despedida" to listOf("tchau", "ate mais", "falou", "ate logo", "ate breve", "boa noite", "durma bem"),
            "pessoal" to listOf("tudo bem", "como vai voce", "eu me chamo", "meu nome e", "este e meu amigo"),
            "sentimento" to listOf("me siento", "estou feliz", "estou triste", "estou cansado", "estou chateado", "estou animado", "estou nervoso", "tenho medo", "tenho vergonha"),
            "humor" to listOf("conte uma piada", "me conta uma piada", "piadinha", "brinca comigo", "fala uma curiosidade"),
            "identidade" to listOf("quem e voce", "quem te criou", "do que voce gosta", "voce gosta de", "me fala sobre voce"),
            "pedido" to listOf("fala comigo", "me ajuda", "me explica", "me ensina", "responde", "me diga"),
            "hobby" to listOf("eu gosto de", "adoro", "prefiro", "meu hobby", "curto", "sou fã de", "eu jogo", "eu assisto", "eu leio")
        )

        private val ACOES = mapOf(
            "atualizar" to listOf("atualizar", "atualiza", "atualize"),
            "ligar" to listOf("ligar", "liga", "ligue", "acender", "acenda", "acende", "ativar", "ative", "acionar", "iniciar", "ativacao"),
            "desligar" to listOf("desligar", "desliga", "desligue", "apagar", "apague", "apaga", "desativar", "desative", "parar", "desacionar"), // 'parar' moved from here to unique handling if needed, but preserved as requested
            "aumentar" to listOf("aumentar", "aumenta", "subir", "elevar", "intensificar", "aumente"),
            "diminuir" to listOf("diminuir", "diminui", "baixar", "baixo", "reduzir", "abaixar", "baixa", "baixe", "abaixe", "abaixa"),
            "abrir" to listOf("abrir", "abre", "destravar", "liberar", "abra"),
            "fechar" to listOf("fechar", "fecha", "travar", "bloquear", "feche"),
            "sair" to listOf("sair", "sai", "saia"),
            "tocar" to listOf("tocar", "toca", "toque", "reproduzir"),
            "mensagem" to listOf("mensagem", "mensage"),
            "mandar" to listOf("mandar", "manda", "mande"),
            "parar" to listOf("pausar", "pare", "para", "parar", "interromper"), // Consolidated 'parar' related
            "resetar" to listOf("resetar", "reiniciar", "reinicie", "reinicializar"),
            "limpar" to listOf("limpar", "limpa", "limpe"),
            "cade" to listOf("cade", "onde esta", "onde voce"),
            "escrever" to listOf("escrever", "escreve", "escreva"),
            "otimizar" to listOf("otimizar", "otimize", "optmizar")
        )

        // Cache de Regex compilados para evitar recriação a cada chamada
        // Lista de (Ação -> Regex)
        private val ACTION_MATCHERS: List<Pair<String, Regex>> by lazy {
            val list = mutableListOf<Pair<String, Regex>>()
            for ((acao, sinonimos) in ACOES) {
                for (s in sinonimos) {
                    val pattern = "\\b${Pattern.quote(s)}\\b"
                    list.add(acao to pattern.toRegex(RegexOption.IGNORE_CASE))
                }
            }
            // Ordena por tamanho do sinônimo decrescente para priorizar "onde esta" sobre "onde" (se houvesse)
            list.sortedByDescending { it.second.pattern.length }
        }
    }

    private fun analisarTextoInterno(text: String): AnaliseTexto {
        Log.i(TAG, "Texto recebido: $text")

        var textoProcessado = text
        var eParcial = false

        if (text.contains("parcial:")) {
            eParcial = true
            textoProcessado = text.replace("parcial:", "")
        }

        // 1. Normalização Eficiente
        val textoLimpo = normalizeText(textoProcessado)

        // 2. Cálculo de Score de Pergunta
        val (ehPergunta, nivelPergunta) = calculateQuestionScore(textoLimpo)

        // 3. Identificação Social
        val (ehSocial, tipoSocial) = identifySocial(textoLimpo)

        // 4. Identificação de Ações e Dispositivos (Otimizado)
        val (comando, _) = identifyActionAndDevice(textoLimpo)

        if (eParcial) {
            textoProcessado = "parcial:$textoLimpo"
        } else {
            textoProcessado = textoLimpo
        }

        return AnaliseTexto(
            textoLimpo = textoProcessado,
            ehPergunta = ehPergunta,
            nivelPergunta = nivelPergunta,
            comandoAutomacao = comando,
            ehSocial = ehSocial,
            tipoSocial = tipoSocial,
            eParcial = eParcial
        )
    }

    private fun normalizeText(text: String): String {
        val semAcento = Normalizer.normalize(text, Normalizer.Form.NFD)
            .replace(Regex("\\p{M}"), "") // Remove diacríticos
        
        val sb = StringBuilder(semAcento.length)
        for (c in semAcento) {
            when {
                c.isLetterOrDigit() -> sb.append(c.lowercaseChar())
                c.isWhitespace() -> if (sb.isNotEmpty() && sb.last() != ' ') sb.append(' ')
            }
        }
        return sb.toString().trim()
    }

    private fun calculateQuestionScore(text: String): Pair<Boolean, Double> {
        var score = 0.0
        
        // Verificação rápida de prefixo
        if (FRASES_INICIAIS.any { text.startsWith(it) }) {
            score += 0.7
        }

        val palavras = text.split(" ")
        for ((index, palavra) in palavras.withIndex()) {
            when {
                index == 0 && PALAVRAS_INICIAIS.contains(palavra) -> score += 0.7
                index > 0 && PALAVRAS_MEIO.contains(palavra) -> score += 0.2
                PADROES_DUV.contains(palavra) -> score += 0.1
            }
        }

        if (score > 1.0) score = 1.0
        return Pair(score >= 0.4, score)
    }

    private fun identifySocial(text: String): Pair<Boolean, String?> {
        for ((categoria, frases) in SOCIAIS) {
            // Checkar se alguma frase está contida no texto
            // Otimização: verifica contains simples primeiro
            if (frases.any { text.contains(it) }) {
                return Pair(true, categoria)
            }
        }
        return Pair(false, null)
    }

    private fun identifyActionAndDevice(texto: String): Pair<ComandoAutomacao?, String?> {
        val acoesEncontradas = mutableSetOf<String>()
        var dispositivoEncontrado: String? = null
        var lastMatchEnd = -1

        // Loop otimizado com Matchers pré-compilados
        for ((acao, regex) in ACTION_MATCHERS) {
            val match = regex.find(texto)
            if (match != null) {
                Log.d(TAG, "✅ Ação encontrada: $acao")
                acoesEncontradas.add(acao)

                // Captura o dispositivo após a ação encontrada
                // Prioriza o dispositivo da última ação encontrada no texto (maior índice)
                if (match.range.last > lastMatchEnd) {
                    lastMatchEnd = match.range.last
                    
                    val restoTexto = texto.substring(match.range.last + 1).trim()
                    if (restoTexto.isNotBlank()) {
                         dispositivoEncontrado = restoTexto
                             .replaceFirst("^\\s*(a|o|os|as|um|uma|uns|umas|de|da|do|das|dos)\\s+".toRegex(), "")
                             .trim()
                    }
                }
            }
        }

        // Caso especial "cade" se dispositivo estiver vazio mas ação detectada
        if (dispositivoEncontrado.isNullOrBlank() && acoesEncontradas.contains("cade")) {
            val cadeRegex = "\\b(cade|onde esta|onde voce)\\s+(.+)$".toRegex()
             val cadeMatch = cadeRegex.find(texto)
             if (cadeMatch != null && cadeMatch.groupValues.size > 2) {
                 dispositivoEncontrado = cadeMatch.groupValues[2].trim()
             }
        }

        val acaoFinal = if (acoesEncontradas.isNotEmpty()) acoesEncontradas.joinToString("|") else null
        
        if (acaoFinal != null) {
            Log.i(TAG, "🔧 Ações: $acaoFinal, Dispositivo: $dispositivoEncontrado")
             return Pair(
                 ComandoAutomacao(acao = acaoFinal, dispositivo = dispositivoEncontrado),
                 dispositivoEncontrado
             )
        }
        
        Log.i(TAG, "❌ Nenhuma ação encontrada")
        return Pair(null, null)
    }
}